using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using Lumel.Images;
using VRage.Game;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>A document, wrapped to the room it is given: its headings, paragraphs, list items, rules, tables, quotes and images, each run set apart as its kind says.</summary>
    /// <remarks>
    ///     Laid out when the document, its sizes or the room change, into pieces that each draw in one call;
    ///     drawing measures nothing. Colors are read as it draws, so changing one lays out nothing. A document
    ///     read by <see cref="Template.Read" /> has slots, which take their words from <see cref="Arguments" />:
    ///     changing one lays the words out again and reads nothing again.
    /// </remarks>
    public class RichText : Element, IPadded, ITextual
    {
        Thickness _padding;
        readonly List<Piece> _pieces = new List<Piece>();
        readonly List<Mark> _marks = new List<Mark>();

        readonly StringBuilder _piece = new StringBuilder(64);

        readonly StringBuilder _probe = new StringBuilder(64);

        readonly List<float> _wides = new List<float>();
        readonly List<Spot> _spots = new List<Spot>();

        Document _document;
        string _markdown;
        Arguments _arguments;
        IImageCatalog _images;
        string _font = MyFontEnum.White;
        string _codeFont = MyFontEnum.White;
        float _fontSize = 24f;
        float _heading1 = 24f;
        float _heading2 = 24f;
        float _heading3 = 24f;
        float _spacing;
        float _indent = 24f;
        Alignment _alignment;
        bool _fit;
        QuoteStyles _quotes;
        Action<Link> _follow;
        Link _pressed;

        bool _stale = true;
        float _laidOutFor = -1f;
        float _laidOutHigh = -1f;
        float _scale = 1f;
        Vector2 _laidOut;

        float _pieceX;
        string _pieceFont;
        float _pieceSize;
        Tone _pieceTone;
        Link _pieceLink;

        /// <summary>An empty document in white, with lines 24 units tall.</summary>
        public RichText()
        {
            Color = Color.White;
            StrongColor = Color.White;
            EmphasisColor = Color.White;
            HeadingColor = Color.White;
            RuleColor = Color.White;
        }

        /// <summary>What is shown, or null for nothing.</summary>
        public Document Document
        {
            get { return _document; }
            set
            {
                if (_document == value)
                    return;

                _document = value;
                _markdown = null;
                Relayout();
            }
        }

        /// <summary>The markdown-lite the document was read from, or null where the document was given as one; read again only when it changes.</summary>
        public string Markdown
        {
            get { return _markdown; }
            set
            {
                if (value == _markdown)
                    return;

                Document = value == null ? null : Text.Markdown.Parse(value);
                _markdown = value;
            }
        }

        /// <summary>The words that stand in the document's slots, or null for none; slots without words say nothing.</summary>
        public Arguments Arguments
        {
            get { return _arguments; }
            set
            {
                if (_arguments == value)
                    return;

                if (_arguments != null)
                    _arguments.Changed -= Relayout;

                _arguments = value;

                if (_arguments != null)
                    _arguments.Changed += Relayout;

                Relayout();
            }
        }

        /// <summary>Where the pictures the document's images name are found, or null for the surface's own: an image whose picture is not found says its description instead.</summary>
        public IImageCatalog Images
        {
            get { return _images; }
            set
            {
                if (_images == value)
                    return;

                _images = value;
                Relayout();
            }
        }

        /// <summary>Which face everything but code is drawn in.</summary>
        public string Font
        {
            get { return _font; }
            set
            {
                if (_font == value)
                    return;

                _font = value;
                Relayout();
            }
        }

        /// <summary>Which face code is drawn in.</summary>
        public string CodeFont
        {
            get { return _codeFont; }
            set
            {
                if (_codeFont == value)
                    return;

                _codeFont = value;
                Relayout();
            }
        }

        /// <summary>How tall a line of anything but a heading is, in UI units.</summary>
        public float FontSize
        {
            get { return _fontSize; }
            set { Resize(ref _fontSize, value); }
        }

        /// <summary>How tall a first-level heading's line is.</summary>
        public float Heading1Size
        {
            get { return _heading1; }
            set { Resize(ref _heading1, value); }
        }

        /// <summary>How tall a second-level heading's line is.</summary>
        public float Heading2Size
        {
            get { return _heading2; }
            set { Resize(ref _heading2, value); }
        }

        /// <summary>How tall the line of a heading at the third level or deeper is.</summary>
        public float Heading3Size
        {
            get { return _heading3; }
            set { Resize(ref _heading3, value); }
        }

        /// <summary>The room between one block and the next.</summary>
        public float BlockSpacing
        {
            get { return _spacing; }
            set { Resize(ref _spacing, value); }
        }

        /// <summary>How far a list item's words stand in from its mark.</summary>
        public float Indent
        {
            get { return _indent; }
            set { Resize(ref _indent, value); }
        }

        /// <summary>Where every line sits across the room, and a table narrower than the room with it; a table's cells sit as their columns say.</summary>
        /// <remarks>Lines set anywhere but the left take the whole width they are given, so that they have room to be set in.</remarks>
        public Alignment Alignment
        {
            get { return _alignment; }
            set
            {
                if (_alignment == value)
                    return;

                _alignment = value;
                Relayout();
            }
        }

        /// <summary>Whether the document, where it would run past the height it is given, shrinks as a whole until it fits; it never grows past its own sizes.</summary>
        public bool Fit
        {
            get { return _fit; }
            set
            {
                if (_fit == value)
                    return;

                _fit = value;
                Relayout();
            }
        }

        /// <summary>The color of ordinary words, and of a numbered item's number.</summary>
        public Color Color { get; set; }

        /// <summary>The color of strong words.</summary>
        public Color StrongColor { get; set; }

        /// <summary>The color of emphasized words that are not also strong.</summary>
        public Color EmphasisColor { get; set; }

        /// <summary>The color of headings.</summary>
        public Color HeadingColor { get; set; }

        /// <summary>The color of rules and of a bullet's mark.</summary>
        public Color RuleColor { get; set; }

        /// <summary>The color of a link's words, or null to draw them as emphasized words are.</summary>
        public Color? LinkColor { get; set; }

        /// <summary>The fill behind a link's words, or null for none.</summary>
        public Color? LinkFill { get; set; }

        /// <summary>What a press on a link does, given the link; null leaves links as words that take no press.</summary>
        /// <remarks>With it set, the text takes the pointer only where a link is, and a press anywhere else reaches what is beneath it.</remarks>
        public Action<Link> Follow
        {
            get { return _follow; }
            set
            {
                _follow = value;
                _pressed = null;
            }
        }

        /// <summary>What the pointer resting on a link says, given the link, or null to say what the text's own <see cref="Element.Tip" /> says.</summary>
        /// <remarks>Asked while the pointer rests on a link, so a tip it hands back is one made once and kept.</remarks>
        public Func<Link, Tip> LinkTip { get; set; }

        /// <summary>The looks tagged quotes take, or null for every quote to look alike.</summary>
        public QuoteStyles Quotes
        {
            get { return _quotes; }
            set
            {
                if (_quotes == value)
                    return;

                _quotes = value;
                Relayout();
            }
        }

        /// <summary>Room held clear between the edge and the text.</summary>
        public Thickness Padding
        {
            get { return _padding; }
            set
            {
                if (_padding == value)
                    return;

                _padding = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <inheritdoc />
        protected override bool TakesPointer => _follow != null && _spots.Count > 0;

        /// <summary>The link whose words stand at <paramref name="point" />, in the coordinates <see cref="Element.Holds" /> takes, or null for none.</summary>
        /// <remarks>Looked for among the links' own places, worked out when the text was laid out, so asking costs no layout.</remarks>
        public Link LinkAt(Vector2 point)
        {
            var room = ContentBounds;
            var x = point.X - room.X;
            var y = point.Y - room.Y;

            for (var i = 0; i < _spots.Count; i++)
            {
                var where = _spots[i].Where;
                if (x >= where.X && x <= where.X + where.Width && y >= where.Y && y <= where.Y + where.Height)
                    return _spots[i].Link;
            }

            return null;
        }

        /// <inheritdoc />
        public override Tip TipAt(Vector2 point)
        {
            var link = LinkTip == null ? null : LinkAt(point);
            return link == null ? Tip : LinkTip(link) ?? Tip;
        }

        /// <inheritdoc />
        public override void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            _pressed = LinkAt(point);
        }

        /// <inheritdoc />
        /// <remarks>Follows the link the press began on, where it also ends on it.</remarks>
        public override void OnPointerUp(Vector2 point)
        {
            var pressed = _pressed;
            _pressed = null;

            if (pressed != null && _follow != null && LinkAt(point) == pressed)
                _follow(pressed);
        }

        /// <inheritdoc />
        /// <remarks>Only a link, where links can be followed, so that a press anywhere else reaches what is beneath the text.</remarks>
        protected override bool Hits(Vector2 point)
        {
            return _follow == null ? base.Hits(point) : LinkAt(point) != null;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_stale || _laidOutFor != available.X || _fit && _laidOutHigh != available.Y)
                Lay(g, available.X, available.Y);

            return _laidOut;
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;

            for (var i = 0; i < _marks.Count; i++)
            {
                var where = _marks[i].Where;
                if (_marks[i].Across)
                    where.Width = room.Width - where.X;

                g.Fill(new RectangleF(room.X + where.X, room.Y + where.Y, where.Width, where.Height), _marks[i].Color ?? RuleColor);
            }

            if (LinkFill.HasValue)
            {
                for (var i = 0; i < _spots.Count; i++)
                {
                    var where = _spots[i].Where;
                    g.Fill(new RectangleF(room.X + where.X, room.Y + where.Y, where.Width, where.Height), LinkFill.Value);
                }
            }

            for (var i = 0; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];
                if (piece.Image != null)
                    piece.Image.Draw(g, new RectangleF(room.X + piece.At.X, room.Y + piece.At.Y, piece.Box.X, piece.Box.Y), Color.White);
                else
                    g.DrawText(piece.Font, piece.Text, new Vector2(room.X + piece.At.X, room.Y + piece.At.Y), piece.Size,
                        piece.Tone == Tone.Plain && piece.Ink.HasValue ? piece.Ink.Value : ColorOf(piece.Tone));
            }
        }

        Color ColorOf(Tone tone)
        {
            switch (tone)
            {
                case Tone.Strong:
                    return StrongColor;
                case Tone.Emphasis:
                    return EmphasisColor;
                case Tone.Heading:
                    return HeadingColor;
                case Tone.Link:
                    return LinkColor ?? EmphasisColor;
                default:
                    return Color;
            }
        }

        void Resize(ref float field, float value)
        {
            if (field == value)
                return;

            field = value;
            Relayout();
        }

        void Relayout()
        {
            _stale = true;
            InvalidateMeasure();
        }

        /// <summary>Lays the document out <paramref name="width" /> wide, and where it shrinks to fit, small enough to be no taller than <paramref name="height" />.</summary>
        /// <remarks>
        ///     Smaller words only ever fit more of themselves on a line, so the document laid out at the scale
        ///     that makes its first height fit is certain to fit; four halvings between that and full size then
        ///     find the largest that does. Each is a whole layout, and none of them happens unless the
        ///     document, its sizes or its room change.
        /// </remarks>
        void Lay(IGraphics g, float width, float height)
        {
            _stale = false;
            _laidOutFor = width;
            _laidOutHigh = height;
            _scale = 1f;
            Pass(g, width);

            if (!_fit || height >= Unbounded || _laidOut.Y <= height || _laidOut.Y <= 0f)
                return;

            var fits = height / _laidOut.Y;
            var over = 1f;
            for (var i = 0; i < 4; i++)
            {
                _scale = (fits + over) * 0.5f;
                Pass(g, width);
                if (_laidOut.Y <= height)
                    fits = _scale;
                else
                    over = _scale;
            }

            if (_scale == fits)
                return;

            _scale = fits;
            Pass(g, width);
        }

        /// <summary>Breaks the document into lines no wider than <paramref name="width" />, every size taken at the current scale, and the lines into pieces that each draw in one call.</summary>
        void Pass(IGraphics g, float width)
        {
            _pieces.Clear();
            _marks.Clear();
            _spots.Clear();

            var y = 0f;
            var widest = 0f;

            if (_document == null)
            {
                _laidOut = Vector2.Zero;
                return;
            }

            Stack(g, _document.Blocks, 0f, width, ref y, ref widest);

            if (_alignment != Alignment.Left && width < Unbounded && widest < width)
                widest = width;

            _laidOut = new Vector2(widest, y);

            for (var i = 0; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];
                if (piece.Link != null)
                    _spots.Add(new Spot { Where = new RectangleF(piece.At.X, piece.At.Y, piece.Box.X, piece.Box.Y), Link = piece.Link });
            }
        }

        /// <summary>Lays <paramref name="blocks" /> out one under another from line <paramref name="y" />, between <paramref name="left" /> and <paramref name="width" />, moving <paramref name="y" /> below the last.</summary>
        void Stack(IGraphics g, IList<Block> blocks, float left, float width, ref float y, ref float widest)
        {
            var body = _fontSize * _scale;
            var indent = _indent * _scale;

            for (var b = 0; b < blocks.Count; b++)
            {
                var block = blocks[b];

                if (b > 0)
                    y += _spacing * _scale;

                if (block.Kind == BlockKind.Rule)
                {
                    _marks.Add(new Mark { Where = new RectangleF(left, y + body * 0.5f, 0f, 1f), Across = true });
                    y += body;
                    continue;
                }

                if (block.Kind == BlockKind.Table)
                {
                    Table(g, block, left, width, ref y, ref widest);
                    continue;
                }

                if (block.Kind == BlockKind.Quote)
                {
                    Quote(g, block, left, width, ref y, ref widest);
                    continue;
                }

                var size = block.Kind == BlockKind.Heading ? HeadingSize(block.Level) : body;
                var start = left;

                if (block.Kind == BlockKind.Bullet || block.Kind == BlockKind.Numbered)
                {
                    start = left + indent;

                    if (block.Kind == BlockKind.Bullet)
                    {
                        var side = size / 5f;
                        _marks.Add(new Mark { Where = new RectangleF(left + (indent - side) * 0.5f, y + (size - side) * 0.5f, side, side) });
                    }
                    else
                        _pieces.Add(new Piece { Text = block.Level + ".", At = new Vector2(left, y), Font = _font, Size = size, Tone = Tone.Plain });
                }

                Flow(g, block, start, size, width, _alignment, ref y, ref widest);
            }
        }

        /// <summary>Lays a quote out from line <paramref name="y" />, its blocks set in from a bar down its edge at <paramref name="left" />, moving <paramref name="y" /> below it.</summary>
        /// <remarks>
        ///     A quote looks as the style its tag finds says, part by part, and as any other quote for whatever the
        ///     style leaves alone or where its tag finds none. A fill reaches across the room behind it, with a
        ///     little room above and below its words.
        /// </remarks>
        void Quote(IGraphics g, Block quote, float left, float width, ref float y, ref float widest)
        {
            var style = _quotes == null ? null : _quotes.Find(quote.Tag);
            var body = _fontSize * _scale;
            var top = y;
            var behind = _marks.Count;
            var first = _pieces.Count;
            var filled = style != null && style.Fill.HasValue;
            var margin = filled ? body * 0.25f : 0f;

            y += margin;
            Stack(g, quote.Blocks, left + _indent * _scale, filled && width < Unbounded ? width - _indent * _scale * 0.5f : width, ref y, ref widest);
            y += margin;

            if (filled)
                _marks.Insert(behind, new Mark { Where = new RectangleF(left, top, 0f, y - top), Across = true, Color = style.Fill });

            _marks.Add(new Mark { Where = new RectangleF(left, top, Math.Max(1f, body / 8f), y - top), Color = style == null ? null : style.Bar });

            if (style == null || !style.Color.HasValue)
                return;

            for (var i = first; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];
                piece.Ink = style.Color;
                _pieces[i] = piece;
            }
        }

        /// <summary>Lays a table out from line <paramref name="y" />, a row at a time, moving <paramref name="y" /> below it.</summary>
        /// <remarks>
        ///     Each column is as wide as its widest cell on one line, a gap of one indent apart. Spare width is
        ///     shared among the starred columns by their stars; with none starred the table is only as wide as it
        ///     needs. A table too wide for its room takes the width back from its widest columns, whose cells then
        ///     wrap. A heading row is drawn in the heading color, with a rule under it. A table narrower than its
        ///     room stands where <see cref="Alignment" /> says.
        /// </remarks>
        void Table(IGraphics g, Block table, float from, float width, ref float y, ref float widest)
        {
            var count = table.Columns.Count;
            var gap = _indent * _scale;
            var room = width >= Unbounded ? Unbounded : width - from;
            Widths(g, table, room, gap);

            var across = gap * (count - 1);
            for (var c = 0; c < count; c++)
                across += _wides[c];

            var left = from;
            if (_alignment != Alignment.Left && room < Unbounded && across < room)
                left += _alignment == Alignment.Center ? (room - across) * 0.5f : room - across;

            for (var r = 0; r < table.Blocks.Count; r++)
            {
                var row = table.Blocks[r];
                var bottom = y + _fontSize * _scale;
                var x = left;

                for (var c = 0; c < count && c < row.Blocks.Count; c++)
                {
                    var line = y;
                    var right = x + _wides[c];
                    var ignored = 0f;

                    Flow(g, row.Blocks[c], x, _fontSize * _scale, right, table.Columns[c].Align, ref line, ref ignored);
                    if (line > bottom)
                        bottom = line;

                    x = right + gap;
                }

                y = bottom;

                if (row.Level == 1)
                {
                    _marks.Add(new Mark { Where = new RectangleF(left, y + gap * 0.25f, across, 1f) });
                    y += gap * 0.5f;
                }
            }

            if (left + across > widest)
                widest = left + across;
        }

        /// <summary>Fills <see cref="_wides" /> with how wide each of a table's columns is laid out within <paramref name="width" />, <paramref name="gap" /> apart.</summary>
        void Widths(IGraphics g, Block table, float width, float gap)
        {
            var count = table.Columns.Count;
            _wides.Clear();
            for (var c = 0; c < count; c++)
                _wides.Add(0f);

            for (var r = 0; r < table.Blocks.Count; r++)
            {
                var row = table.Blocks[r];
                for (var c = 0; c < count && c < row.Blocks.Count; c++)
                {
                    var pieces = _pieces.Count;
                    var line = 0f;
                    var natural = 0f;

                    Flow(g, row.Blocks[c], 0f, _fontSize * _scale, Unbounded, Alignment.Left, ref line, ref natural);
                    _pieces.RemoveRange(pieces, _pieces.Count - pieces);

                    if (natural > _wides[c])
                        _wides[c] = natural;
                }
            }

            if (width >= Unbounded)
                return;

            var across = gap * (count - 1);
            var shares = 0;
            for (var c = 0; c < count; c++)
            {
                across += _wides[c];
                shares += table.Columns[c].Shares;
            }

            if (across < width && shares > 0)
            {
                var spare = width - across;
                for (var c = 0; c < count; c++)
                    _wides[c] += spare * table.Columns[c].Shares / shares;
            }

            var over = across - width;
            var least = _fontSize * _scale * 3f;
            while (over > 0.5f)
            {
                var broadest = 0;
                for (var c = 1; c < count; c++)
                {
                    if (_wides[c] > _wides[broadest])
                        broadest = c;
                }

                var given = Math.Min(over, _wides[broadest] - least);
                if (given <= 0f)
                    break;

                _wides[broadest] -= given;
                over -= given;
            }
        }

        /// <summary>Lays one block's runs out from line <paramref name="y" /> and <paramref name="start" />, wrapping at <paramref name="width" />, each line set against the side <paramref name="align" /> says; moves <paramref name="y" /> below the last line.</summary>
        /// <remarks>A line is as tall as <paramref name="size" />, or as its tallest image, and everything on it stands on its foot.</remarks>
        void Flow(IGraphics g, Block block, float start, float size, float width, Alignment align, ref float y, ref float widest)
        {
            var heading = block.Kind == BlockKind.Heading || block.Kind == BlockKind.Cell && block.Level == 1;
            var x = start;
            var empty = true;
            var line = _pieces.Count;
            var tall = size;
            var pictured = false;

            for (var r = 0; r < block.Runs.Count; r++)
            {
                var run = block.Runs[r];

                if (run.IsBreak)
                {
                    End(g, y, true, ref x, ref widest);
                    Settle(line, x, width, align, y, tall, pictured);
                    line = _pieces.Count;
                    y += tall;
                    tall = size;
                    pictured = false;
                    x = start;
                    empty = true;
                    continue;
                }

                var images = _images ?? g.Images;
                var shown = run as ImageRun;
                var image = shown != null && images != null ? images.Find(shown.Picture.Name) : null;
                if (image != null)
                {
                    var box = SizeOf(shown.Picture, image, size, _scale);
                    End(g, y, false, ref x, ref widest);

                    if (!empty && width < Unbounded && x + box.X > width)
                    {
                        Trim(g, line, ref x);
                        Settle(line, x, width, align, y, tall, pictured);
                        line = _pieces.Count;
                        y += tall;
                        tall = size;
                        x = start;
                    }

                    _pieces.Add(new Piece { Image = image, At = new Vector2(x, y), Box = box });
                    pictured = true;
                    x += box.X;
                    if (x > widest)
                        widest = x;
                    if (box.Y > tall)
                        tall = box.Y;

                    empty = false;
                    continue;
                }

                var linked = run as LinkRun;
                var link = linked == null ? null : linked.Link;
                var tone = link != null ? Tone.Link
                    : heading ? Tone.Heading
                    : (run.Kind & RunKind.Strong) != 0 ? Tone.Strong
                    : (run.Kind & RunKind.Emphasis) != 0 ? Tone.Emphasis
                    : Tone.Plain;
                var font = (run.Kind & RunKind.Code) != 0 ? _codeFont : _font;
                var text = run.Slot == null ? run.Text : _arguments == null ? "" : _arguments.Of(run.Slot);
                var i = 0;

                while (i < text.Length)
                {
                    if (text[i] == ' ')
                    {
                        if (!empty)
                        {
                            // A link's piece ends at its last word, so that its fill and the room that presses it
                            // stop there too.
                            if (_piece.Length > 0 && _pieceLink != link)
                                End(g, y, false, ref x, ref widest);

                            if (_piece.Length == 0)
                                Begin(x, font, size, tone, link);

                            _piece.Append(' ');
                        }

                        i++;
                        continue;
                    }

                    var end = text.IndexOf(' ', i);
                    if (end < 0)
                        end = text.Length;

                    if (_piece.Length > 0 && (_pieceTone != tone || _pieceFont != font || _pieceLink != link))
                        End(g, y, false, ref x, ref widest);

                    if (!empty && width < Unbounded && !Fits(g, text, i, end, font, size, x, width))
                    {
                        End(g, y, true, ref x, ref widest);
                        Settle(line, x, width, align, y, tall, pictured);
                        line = _pieces.Count;
                        y += tall;
                        tall = size;
                        pictured = false;
                        x = start;
                    }

                    if (_piece.Length == 0)
                        Begin(x, font, size, tone, link);

                    _piece.Append(text, i, end - i);
                    empty = false;
                    i = end;
                }
            }

            End(g, y, true, ref x, ref widest);
            Settle(line, x, width, align, y, tall, pictured);
            y += tall;
        }

        /// <summary>How big an image of <paramref name="picture" /> is drawn: as written, a side not written taken from <paramref name="image" />'s shape, and as tall as a line of <paramref name="size" /> where neither is written; what is written is taken at <paramref name="scale" />.</summary>
        static Vector2 SizeOf(Picture picture, IImageSource image, float size, float scale)
        {
            var width = picture.Width * scale;
            var height = picture.Height * scale;

            if (float.IsNaN(width) && float.IsNaN(height))
                height = size;

            if (float.IsNaN(width))
                width = Shape.WidthFor(image.Size, height);
            else if (float.IsNaN(height))
                height = Shape.HeightFor(image.Size, width);

            return new Vector2(width, height);
        }

        /// <summary>Starts a piece at <paramref name="x" />.</summary>
        void Begin(float x, string font, float size, Tone tone, Link link)
        {
            _pieceX = x;
            _pieceFont = font;
            _pieceSize = size;
            _pieceTone = tone;
            _pieceLink = link;
        }

        /// <summary>Drops the spaces ending the last piece of the line that starts at piece <paramref name="first" />, moving <paramref name="x" /> back to where its words end.</summary>
        /// <remarks>For a line that breaks before an image, whose words were ended before it was known the image would not fit.</remarks>
        void Trim(IGraphics g, int first, ref float x)
        {
            var last = _pieces.Count - 1;
            if (last < first || _pieces[last].Image != null)
                return;

            var piece = _pieces[last];
            var words = piece.Text.TrimEnd(' ');
            if (words.Length == piece.Text.Length)
                return;

            _probe.Clear();
            _probe.Append(words);

            piece.Text = words;
            piece.Box.X = g.MeasureText(piece.Font, _probe, piece.Size).X;
            _pieces[last] = piece;

            x = piece.At.X + piece.Box.X;
        }

        /// <summary>
        ///     Sets the pieces of the line from piece <paramref name="first" />, ending at <paramref name="end" />, on the foot
        ///     of a line <paramref name="tall" /> high from <paramref name="top" />, and against the side of the room up to
        ///     <paramref name="width" /> that <paramref name="align" /> says; <paramref name="pictured" /> is whether an image stands on it.
        /// </summary>
        void Settle(int first, float end, float width, Alignment align, float top, float tall, bool pictured)
        {
            var shift = 0f;
            if (align != Alignment.Left && width < Unbounded && end < width)
                shift = align == Alignment.Center ? (width - end) * 0.5f : width - end;

            // A line of words alone, set from the left, is where it was laid already: the common case, so it
            // costs nothing.
            if (shift == 0f && !pictured)
                return;

            for (var i = first; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];
                piece.At.X += shift;
                piece.At.Y = top + tall - (piece.Image != null ? piece.Box.Y : piece.Size);
                _pieces[i] = piece;
            }
        }

        /// <summary>Whether the word from <paramref name="start" /> to <paramref name="end" /> fits on the line after the piece being built.</summary>
        /// <remarks>Measures the piece with the word on it, because the game spaces a string as a whole and a sum of its words' widths is short of it.</remarks>
        bool Fits(IGraphics g, string text, int start, int end, string font, float size, float x, float width)
        {
            _probe.Clear();
            _probe.Append(_piece);
            _probe.Append(text, start, end - start);

            var from = _piece.Length > 0 ? _pieceX : x;
            return from + g.MeasureText(font, _probe, size).X <= width;
        }

        /// <summary>Ends the piece being built at line <paramref name="y" />, moving <paramref name="x" /> past it; a piece that ends a line drops its trailing spaces.</summary>
        void End(IGraphics g, float y, bool endsLine, ref float x, ref float widest)
        {
            if (endsLine)
            {
                while (_piece.Length > 0 && _piece[_piece.Length - 1] == ' ')
                    _piece.Length--;
            }

            if (_piece.Length == 0)
                return;

            var advance = g.MeasureText(_pieceFont, _piece, _pieceSize).X;
            _pieces.Add(new Piece { Text = _piece.ToString(), At = new Vector2(_pieceX, y), Font = _pieceFont, Size = _pieceSize, Tone = _pieceTone, Link = _pieceLink, Box = new Vector2(advance, _pieceSize) });
            _piece.Clear();

            x = _pieceX + advance;
            if (x > widest)
                widest = x;
        }

        float HeadingSize(int level)
        {
            return (level <= 1 ? _heading1 : level == 2 ? _heading2 : _heading3) * _scale;
        }

        enum Tone
        {
            Plain,
            Strong,
            Emphasis,
            Heading,
            Link
        }

        /// <summary>Words or an image, drawn in one call.</summary>
        struct Piece
        {
            public string Text;
            public Vector2 At;
            public string Font;
            public float Size;
            public Tone Tone;

            /// <summary>The picture an image draws, or null for words.</summary>
            public IImageSource Image;

            /// <summary>The link the words are part of, or null.</summary>
            public Link Link;

            /// <summary>How big the piece is drawn: an image's size, or the words' width and their line's height.</summary>
            public Vector2 Box;

            /// <summary>The color plain words take in place of the text's own, as a quote's style gives it, or null.</summary>
            public Color? Ink;
        }

        /// <summary>Where one piece of a link's words stands, for finding the link under the pointer.</summary>
        struct Spot
        {
            public RectangleF Where;
            public Link Link;
        }

        /// <summary>A rule, a bullet's mark, the rule under a table's heading row, or a quote's bar or fill; one that reaches across fills the room from where it starts.</summary>
        struct Mark
        {
            public RectangleF Where;
            public bool Across;

            /// <summary>The mark's own color, as a quote's style gives it, or null for the rule color.</summary>
            public Color? Color;
        }
    }
}
