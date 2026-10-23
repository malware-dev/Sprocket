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
    ///     Laid out only when the document, its sizes, its <see cref="Arguments" /> or the room change; drawing measures
    ///     nothing, and changing a color lays out nothing. An image whose locator the graphics'
    ///     <see cref="IGraphics.Images" /> cannot find says its description instead.
    /// </remarks>
    public class RichText : Element, IPadded, ITextual
    {
        /// <summary>How far down a line a link's underline lies unless a style says otherwise, as a fraction of the line's height: just under the words of the game's own faces.</summary>
        public const float UnderlineDrop = 0.85f;

        /// <summary>How thick the line under a link's words is unless a style says otherwise: none, so a link stands out by its color alone.</summary>
        public const float UnderlineThickness = 0f;

        /// <summary>How far the box behind code reaches past its words on either side unless a style says otherwise, in UI units.</summary>
        public const float CodeRoom = 4f;

        /// <summary>The box behind code unless a style says otherwise: a faint wash of white, lighter than whatever it lies on.</summary>
        public static readonly Color CodeWash = new Color(255, 255, 255, 10);

        Thickness _padding;
        readonly List<Piece> _pieces = new List<Piece>();
        readonly List<Mark> _marks = new List<Mark>();

        readonly StringBuilder _piece = new StringBuilder(64);

        readonly StringBuilder _probe = new StringBuilder(64);

        readonly StringBuilder _tail = new StringBuilder(64);

        readonly List<float> _wides = new List<float>();
        readonly List<Spot> _spots = new List<Spot>();

        Document _document;
        string _markdown;
        Arguments _arguments;
        string _font = MyFontEnum.White;
        string _codeFont = MyFontEnum.White;
        string _strongFont;
        string _emphasisFont;
        string _strongEmphasisFont;
        string _headingFont;
        float _fontSize = 24f;
        float _heading1 = 24f;
        float _heading2 = 24f;
        float _heading3 = 24f;
        float _spacing;
        int _ruled;
        float _indent = 24f;
        float _codeInset = CodeRoom;
        Alignment _alignment;
        BoxStyles _boxes;
        Action<Link> _follow;
        Link _pressed;

        bool _stale = true;
        float _laidOutFor = -1f;
        Vector2 _laidOut;

        float _pieceX;
        string _pieceFont;
        float _pieceSize;
        Tone _pieceTone;
        Link _pieceLink;
        bool _pieceCode;

        bool _italic;

        /// <summary>An empty document in white, with lines 24 units tall.</summary>
        public RichText()
        {
            UnderlineAt = UnderlineDrop;
            LinkUnderline = UnderlineThickness;
            CodeFill = CodeWash;
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

        /// <summary>Which face everything but code and strong or emphasized words is drawn in.</summary>
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

        /// <summary>Which face strong words are drawn in, or null for <see cref="Font" />; strong code stays in <see cref="CodeFont" />.</summary>
        public string StrongFont
        {
            get { return _strongFont; }
            set
            {
                if (_strongFont == value)
                    return;

                _strongFont = value;
                Relayout();
            }
        }

        /// <summary>Which face emphasized words are drawn in, or null for <see cref="Font" />; emphasized code stays in <see cref="CodeFont" />.</summary>
        public string EmphasisFont
        {
            get { return _emphasisFont; }
            set
            {
                if (_emphasisFont == value)
                    return;

                _emphasisFont = value;
                Relayout();
            }
        }

        /// <summary>Which face words both strong and emphasized are drawn in, or null for <see cref="StrongFont" />, then <see cref="EmphasisFont" />.</summary>
        public string StrongEmphasisFont
        {
            get { return _strongEmphasisFont; }
            set
            {
                if (_strongEmphasisFont == value)
                    return;

                _strongEmphasisFont = value;
                Relayout();
            }
        }

        /// <summary>Which face headings and a table's heading row are drawn in, or null for the strong face, which makes every heading bold.</summary>
        public string HeadingFont
        {
            get { return _headingFont; }
            set
            {
                if (_headingFont == value)
                    return;

                _headingFont = value;
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

        /// <summary>The room between one block and the next; twice that above a heading that is not the first.</summary>
        public float BlockSpacing
        {
            get { return _spacing; }
            set { Resize(ref _spacing, value); }
        }

        /// <summary>The deepest level of heading drawn with a rule under it, from 1, or zero for none.</summary>
        public int RuledHeadings
        {
            get { return _ruled; }
            set
            {
                if (_ruled == value)
                    return;

                _ruled = value;
                Relayout();
            }
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

        /// <summary>The box behind code, or null for none.</summary>
        public Color? CodeFill { get; set; }

        /// <summary>How far the box behind code reaches past its words on either side, in UI units; the words beside it stand clear of it.</summary>
        public float CodeInset
        {
            get { return _codeInset; }
            set
            {
                if (_codeInset == value)
                    return;

                _codeInset = value;
                Relayout();
            }
        }

        /// <summary>How thick the line under a link's words is, in UI units, or 0 for none.</summary>
        public float LinkUnderline { get; set; }

        /// <summary>How far down a line a link's underline lies, as a fraction of the line's height from its top.</summary>
        public float UnderlineAt { get; set; }

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

        /// <summary>What the attributes a box names ask for, or null for boxes to change nothing.</summary>
        public BoxStyles Boxes
        {
            get { return _boxes; }
            set
            {
                if (_boxes == value)
                    return;

                _boxes = value;
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
        protected override void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            _pressed = LinkAt(point);
        }

        /// <inheritdoc />
        protected override void OnPointerUp(Vector2 point)
        {
            var pressed = _pressed;
            _pressed = null;

            if (pressed != null && _follow != null && LinkAt(point) == pressed)
                _follow(pressed);
        }

        /// <inheritdoc />
        protected override bool Hits(Vector2 point)
        {
            return _follow == null ? base.Hits(point) : LinkAt(point) != null;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_stale || _laidOutFor != available.X)
                Lay(g, available.X);

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

            if (LinkUnderline > 0f)
            {
                var ink = ColorOf(Tone.Link);
                var thick = LinkUnderline;
                for (var i = 0; i < _spots.Count; i++)
                {
                    var where = _spots[i].Where;
                    g.Fill(new RectangleF(room.X + where.X, room.Y + where.Y + where.Height * UnderlineAt, where.Width, thick), ink);
                }
            }

            var inset = _codeInset;
            foreach (var piece in _pieces)
            {
                if (piece.Code && CodeFill.HasValue)
                    g.Fill(new RectangleF(room.X + piece.At.X - inset, room.Y + piece.At.Y + 1f, piece.Box.X + inset * 2f, piece.Box.Y - 2f), CodeFill.Value);

                if (piece.Image != null)
                    piece.Image.Draw(g, new RectangleF(room.X + piece.At.X + (piece.Box.X - piece.Drawn.X) * 0.5f,
                        room.Y + piece.At.Y + (piece.Box.Y - piece.Drawn.Y) * 0.5f, piece.Drawn.X, piece.Drawn.Y), Color.White, piece.Turn);
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

        /// <summary>Breaks the document into lines no wider than <paramref name="width" />, and the lines into pieces that each draw in one call.</summary>
        void Lay(IGraphics g, float width)
        {
            _stale = false;
            _laidOutFor = width;

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

            Stack(g, _document.Blocks, 0f, width, _alignment, ref y, ref widest);

            if (_alignment != Alignment.Left && width < Unbounded && widest < width)
                widest = width;

            _laidOut = new Vector2(widest, y);

            foreach (var piece in _pieces)
            {
                if (piece.Link != null)
                    _spots.Add(new Spot { Where = new RectangleF(piece.At.X, piece.At.Y, piece.Box.X, piece.Box.Y), Link = piece.Link });
            }
        }

        /// <summary>Lays <paramref name="blocks" /> out one under another from line <paramref name="y" />, between <paramref name="left" /> and <paramref name="width" />, their lines set where <paramref name="align" /> says, moving <paramref name="y" /> below the last.</summary>
        void Stack(IGraphics g, IList<Block> blocks, float left, float width, Alignment align, ref float y, ref float widest)
        {
            var body = _fontSize;
            var indent = _indent;

            for (var b = 0; b < blocks.Count; b++)
            {
                var block = blocks[b];

                if (b > 0)
                    y += _spacing * (block.Kind == BlockKind.Heading ? 2f : 1f);

                if (block.Kind == BlockKind.Rule)
                {
                    _marks.Add(new Mark { Where = new RectangleF(left, y + body * 0.5f, 0f, 1f), Across = true });
                    y += body;
                    continue;
                }

                if (block.Kind == BlockKind.Table)
                {
                    Table(g, block, left, width, align, ref y, ref widest);
                    continue;
                }

                if (block.Kind == BlockKind.Quote)
                {
                    Quote(g, block, left, width, align, ref y, ref widest);
                    continue;
                }

                if (block.Kind == BlockKind.Box)
                {
                    Box(g, block, left, width, align, ref y, ref widest);
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

                Flow(g, block, start, size, width, align, ref y, ref widest);

                if (block.Kind == BlockKind.Heading && block.Level <= _ruled)
                {
                    var under = _spacing * 0.5f;
                    _marks.Add(new Mark { Where = new RectangleF(left, y + under, 0f, 1f), Across = true });
                    y += under + 1f;
                }
            }
        }

        /// <summary>Lays a quote out from line <paramref name="y" />, its blocks set in from a bar down its edge at <paramref name="left" /> in the rule color, moving <paramref name="y" /> below it.</summary>
        void Quote(IGraphics g, Block quote, float left, float width, Alignment align, ref float y, ref float widest)
        {
            var top = y;
            Stack(g, quote.Blocks, left + _indent, width, align, ref y, ref widest);
            _marks.Add(new Mark { Where = new RectangleF(left, top, Math.Max(1f, _fontSize / 8f), y - top) });
        }

        /// <summary>Lays a box out from line <paramref name="y" /> as the attributes it names ask, moving <paramref name="y" /> below it.</summary>
        /// <remarks>
        ///     A later name's style wins over an earlier one's, and the box's color, face and alignment reach the boxes
        ///     inside it.
        /// </remarks>
        void Box(IGraphics g, Block box, float left, float width, Alignment align, ref float y, ref float widest)
        {
            Color? bar = null;
            Color? fill = null;
            Color? color = null;
            bool? italic = null;
            var inner = align;
            for (var i = 0; i < box.Names.Count; i++)
            {
                var name = box.Names[i];
                if (string.Equals(name, "left", StringComparison.OrdinalIgnoreCase))
                    inner = Alignment.Left;
                else if (string.Equals(name, "center", StringComparison.OrdinalIgnoreCase))
                    inner = Alignment.Center;
                else if (string.Equals(name, "right", StringComparison.OrdinalIgnoreCase))
                    inner = Alignment.Right;

                var style = _boxes?.Find(name);
                if (style == null)
                    continue;

                bar = style.Bar ?? bar;
                fill = style.Fill ?? fill;
                color = style.Color ?? color;
                italic = style.Italic ?? italic;
            }

            var top = y;
            var behind = _marks.Count;
            var first = _pieces.Count;
            var indent = _indent;
            var margin = fill.HasValue ? _fontSize * 0.25f : 0f;

            // Its face reaches the boxes inside it, as its color does, until one of them says otherwise.
            var italicWas = _italic;
            _italic = italic ?? _italic;

            y += margin;
            Stack(g, box.Blocks, bar.HasValue || fill.HasValue ? left + indent : left,
                fill.HasValue && width < Unbounded ? width - indent * 0.5f : width, inner, ref y, ref widest);
            y += margin;

            _italic = italicWas;

            if (fill.HasValue)
                _marks.Insert(behind, new Mark { Where = new RectangleF(left, top, 0f, y - top), Across = true, Color = fill });

            if (bar.HasValue)
                _marks.Add(new Mark { Where = new RectangleF(left, top, Math.Max(1f, _fontSize / 8f), y - top), Color = bar });

            if (inner != Alignment.Left && width < Unbounded && width > widest)
                widest = width;

            if (!color.HasValue)
                return;

            for (var i = first; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];
                if (piece.Ink.HasValue)
                    continue;

                piece.Ink = color;
                _pieces[i] = piece;
            }
        }

        /// <summary>Lays a table out from line <paramref name="y" />, a row at a time, moving <paramref name="y" /> below it.</summary>
        /// <remarks>
        ///     Starred columns share the room the others leave by their stars. A table too wide for its room takes the width
        ///     back from its widest columns, whose cells then wrap.
        /// </remarks>
        void Table(IGraphics g, Block table, float from, float width, Alignment align, ref float y, ref float widest)
        {
            var count = table.Columns.Count;
            var gap = _indent;
            var room = width >= Unbounded ? Unbounded : width - from;
            Widths(g, table, room, gap);

            var across = gap * (count - 1);
            for (var c = 0; c < count; c++)
                across += _wides[c];

            var left = from;
            if (align != Alignment.Left && room < Unbounded && across < room)
                left += align == Alignment.Center ? (room - across) * 0.5f : room - across;

            for (var r = 0; r < table.Blocks.Count; r++)
            {
                var row = table.Blocks[r];
                var bottom = y + _fontSize;
                var x = left;

                for (var c = 0; c < count && c < row.Blocks.Count; c++)
                {
                    var line = y;
                    var right = x + _wides[c];
                    var ignored = 0f;

                    Flow(g, row.Blocks[c], x, _fontSize, right, table.Columns[c].Align, ref line, ref ignored);
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

                    Flow(g, row.Blocks[c], 0f, _fontSize, Unbounded, Alignment.Left, ref line, ref natural);
                    _pieces.RemoveRange(pieces, _pieces.Count - pieces);

                    if (natural > _wides[c])
                        _wides[c] = natural;
                }
            }

            if (width >= Unbounded)
                return;

            var least = _fontSize * 3f;
            var fixedAcross = gap * (count - 1);
            var shares = 0;
            for (var c = 0; c < count; c++)
            {
                if (table.Columns[c].Shares > 0)
                    shares += table.Columns[c].Shares;
                else
                    fixedAcross += _wides[c];
            }

            if (shares > 0)
            {
                var room = Math.Max(0f, width - fixedAcross);
                for (var c = 0; c < count; c++)
                {
                    if (table.Columns[c].Shares > 0)
                        _wides[c] = Math.Max(least, room * table.Columns[c].Shares / shares);
                }
            }

            var across = gap * (count - 1);
            for (var c = 0; c < count; c++)
                across += _wides[c];

            var over = across - width;
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

                var shown = run as ImageRun;
                var image = shown != null && g.Images != null ? g.Images.Find(shown.Picture.Locator) : null;
                if (image != null)
                {
                    var drawn = SizeOf(shown.Picture, image, size);
                    var turn = MathHelper.ToRadians(shown.Picture.Rotation);
                    var box = Turned(drawn, turn);
                    End(g, y, false, ref x, ref widest);

                    if (!empty && width < Unbounded && x + box.X + Tail(g, block, r + 1, size, heading) > width)
                    {
                        Trim(g, line, ref x);
                        Settle(line, x, width, align, y, tall, pictured);
                        line = _pieces.Count;
                        y += tall;
                        tall = size;
                        x = start;
                    }

                    _pieces.Add(new Piece { Image = image, At = new Vector2(x, y), Box = box, Drawn = drawn, Turn = turn });
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
                var link = linked?.Link;
                var tone = link != null ? Tone.Link
                    : heading ? Tone.Heading
                    : (run.Kind & RunKind.Strong) != 0 ? Tone.Strong
                    : (run.Kind & RunKind.Emphasis) != 0 ? Tone.Emphasis
                    : Tone.Plain;
                var code = (run.Kind & RunKind.Code) != 0;
                var font = FaceFor(run, heading);
                var text = run.Slot == null ? run.Text : _arguments == null ? "" : _arguments.Of(run.Slot);
                var joins = run.Slot == null ? run.HoldsNoBreak : text.IndexOf(Run.NoBreak) >= 0;
                var i = 0;

                while (i < text.Length)
                {
                    if (text[i] == ' ')
                    {
                        if (!empty)
                        {
                            // A link's piece ends at its last word, so that its fill and the room that presses it
                            // stop there too; so does code's, so that its box does.
                            if (_piece.Length > 0 && (_pieceLink != link || _pieceCode != code))
                                End(g, y, false, ref x, ref widest);

                            if (_piece.Length == 0)
                                Begin(x, font, size, tone, link, code);

                            _piece.Append(' ');
                        }

                        i++;
                        continue;
                    }

                    var end = text.IndexOf(' ', i);
                    if (end < 0)
                        end = text.Length;

                    if (_piece.Length > 0 && (_pieceTone != tone || _pieceFont != font || _pieceLink != link || _pieceCode != code))
                        End(g, y, false, ref x, ref widest);

                    if (!empty && width < Unbounded && !Fits(g, text, i, end, joins, font, size, code, x, end == text.Length ? width - Tail(g, block, r + 1, size, heading) : width))
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
                        Begin(x, font, size, tone, link, code);

                    Shown(_piece, text, i, end, joins);
                    empty = false;
                    i = end;
                }
            }

            End(g, y, true, ref x, ref widest);
            Settle(line, x, width, align, y, tall, pictured);
            y += tall;
        }

        /// <summary>The room a picture <paramref name="size" /> big takes once turned <paramref name="turn" /> radians: the box around its turned outline.</summary>
        static Vector2 Turned(Vector2 size, float turn)
        {
            if (turn == 0f)
                return size;

            var cos = Math.Abs((float)Math.Cos(turn));
            var sin = Math.Abs((float)Math.Sin(turn));
            return new Vector2(size.X * cos + size.Y * sin, size.X * sin + size.Y * cos);
        }

        /// <summary>How big an image of <paramref name="picture" /> is drawn: as written, a side not written taken from <paramref name="image" />'s shape, and as tall as a line of <paramref name="size" /> where neither is written.</summary>
        static Vector2 SizeOf(Picture picture, IImageSource image, float size)
        {
            var width = picture.Width;
            var height = picture.Height;

            if (float.IsNaN(width) && float.IsNaN(height))
                height = size;

            if (float.IsNaN(width))
                width = AspectRatio.WidthFor(image.Size, height);
            else if (float.IsNaN(height))
                height = AspectRatio.HeightFor(image.Size, width);

            return new Vector2(width, height);
        }

        /// <summary>The face words of <paramref name="kind" /> are drawn in within a heading or a table's heading row.</summary>
        string HeadingFaceOf(RunKind kind)
        {
            if (_headingFont == null)
                return FaceOf(kind | RunKind.Strong);

            return (kind & (RunKind.Strong | RunKind.Emphasis)) == 0 ? _headingFont : FaceOf(kind);
        }

        /// <summary>The face words of <paramref name="kind" /> are drawn in, other than code, falling back to the nearest face that is set.</summary>
        string FaceOf(RunKind kind)
        {
            var strong = (kind & RunKind.Strong) != 0;
            var emphasized = (kind & RunKind.Emphasis) != 0;

            if (strong && emphasized)
                return _strongEmphasisFont ?? _strongFont ?? _emphasisFont ?? _font;

            if (strong)
                return _strongFont ?? _font;

            return emphasized ? _emphasisFont ?? _font : _font;
        }

        /// <summary>Starts a piece at <paramref name="x" />; code starts its words past its box's room.</summary>
        void Begin(float x, string font, float size, Tone tone, Link link, bool code)
        {
            _pieceX = code ? x + _codeInset : x;
            _pieceCode = code;
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

            x = piece.At.X + piece.Box.X + (piece.Code ? _codeInset : 0f);
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
        bool Fits(IGraphics g, string text, int start, int end, bool joins, string font, float size, bool code, float x, float width)
        {
            _probe.Clear();
            _probe.Append(_piece);
            Shown(_probe, text, start, end, joins);

            var room = code ? _codeInset : 0f;
            var from = _piece.Length > 0 ? _pieceX : x + room;
            return from + g.MeasureText(font, _probe, size).X + room <= width;
        }

        /// <summary>
        ///     How wide what follows run <paramref name="from" /> is up to the first place a line can break: the words and pictures
        ///     joined to the one being placed with no space between, which a line keeps together with it.
        /// </summary>
        /// <remarks>
        ///     Asked only of a word or picture that ends its run, and nothing is measured where the next run starts with a space,
        ///     which is the common case.
        /// </remarks>
        float Tail(IGraphics g, Block block, int from, float size, bool heading)
        {
            var tail = 0f;
            for (var r = from; r < block.Runs.Count; r++)
            {
                var run = block.Runs[r];
                if (run.IsBreak)
                    return tail;

                var shown = run as ImageRun;
                var image = shown != null && g.Images != null ? g.Images.Find(shown.Picture.Locator) : null;
                if (image != null)
                {
                    tail += Turned(SizeOf(shown.Picture, image, size), MathHelper.ToRadians(shown.Picture.Rotation)).X;
                    continue;
                }

                var text = run.Slot == null ? run.Text : _arguments == null ? "" : _arguments.Of(run.Slot);
                var joins = run.Slot == null ? run.HoldsNoBreak : text.IndexOf(Run.NoBreak) >= 0;
                var space = text.IndexOf(' ');
                var end = space < 0 ? text.Length : space;
                if (end > 0)
                {
                    _tail.Clear();
                    Shown(_tail, text, 0, end, joins);
                    tail += g.MeasureText(FaceFor(run, heading), _tail, size).X;
                }

                if (space >= 0)
                    return tail;
            }

            return tail;
        }

        /// <summary>
        ///     Appends <paramref name="text" /> from <paramref name="start" /> to <paramref name="end" /> to <paramref name="into" /> as it
        ///     is drawn: a no-break space as a space, which every font has.
        /// </summary>
        /// <remarks><paramref name="joins" /> says whether the text holds one at all, known once for its run, so a word without one is appended whole.</remarks>
        static void Shown(StringBuilder into, string text, int start, int end, bool joins)
        {
            if (!joins)
            {
                into.Append(text, start, end - start);
                return;
            }

            for (var i = start; i < end; i++)
                into.Append(text[i] == Run.NoBreak ? ' ' : text[i]);
        }

        /// <summary>The face <paramref name="run" /> is drawn in, within a heading where <paramref name="heading" /> says so.</summary>
        string FaceFor(Run run, bool heading)
        {
            if ((run.Kind & RunKind.Code) != 0)
                return _codeFont;

            var kind = _italic ? run.Kind | RunKind.Emphasis : run.Kind;
            return heading ? HeadingFaceOf(kind) : FaceOf(kind);
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
            _pieces.Add(new Piece { Text = _piece.ToString(), At = new Vector2(_pieceX, y), Font = _pieceFont, Size = _pieceSize, Tone = _pieceTone, Link = _pieceLink, Code = _pieceCode, Box = new Vector2(advance, _pieceSize) });
            _piece.Clear();

            x = _pieceX + advance + (_pieceCode ? _codeInset : 0f);
            if (x > widest)
                widest = x;
        }

        float HeadingSize(int level)
        {
            return (level <= 1 ? _heading1 : level == 2 ? _heading2 : _heading3);
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

            /// <summary>How big an image's picture is drawn before it is turned, centered in <see cref="Box" />.</summary>
            public Vector2 Drawn;

            /// <summary>How far an image's picture is turned clockwise, in radians.</summary>
            public float Turn;

            /// <summary>The link the words are part of, or null.</summary>
            public Link Link;

            /// <summary>Whether the words are code, drawn on a box of their own.</summary>
            public bool Code;

            /// <summary>How big the piece is drawn: an image's size, or the words' width and their line's height.</summary>
            public Vector2 Box;

            /// <summary>The color plain words take in place of the text's own, as a box's style gives it, or null.</summary>
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

            /// <summary>The mark's own color, as a box's style gives it, or null for the rule color.</summary>
            public Color? Color;
        }
    }
}
