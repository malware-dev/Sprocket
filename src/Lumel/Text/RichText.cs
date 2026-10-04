using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using Lumel.Images;
using VRage.Game;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>A document, wrapped to the room it is given: its headings, paragraphs, list items, rules, tables and images, each run set apart as its kind says.</summary>
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

        Document _document;
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

        bool _stale = true;
        float _laidOutFor = -1f;
        Vector2 _laidOut;

        float _pieceX;
        string _pieceFont;
        float _pieceSize;
        Tone _pieceTone;

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
                Relayout();
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
                    where.Width = room.Width;

                g.Fill(new RectangleF(room.X + where.X, room.Y + where.Y, where.Width, where.Height), RuleColor);
            }

            for (var i = 0; i < _pieces.Count; i++)
            {
                var piece = _pieces[i];
                if (piece.Image != null)
                    piece.Image.Draw(g, new RectangleF(room.X + piece.At.X, room.Y + piece.At.Y, piece.Box.X, piece.Box.Y), Color.White);
                else
                    g.DrawText(piece.Font, piece.Text, new Vector2(room.X + piece.At.X, room.Y + piece.At.Y), piece.Size,
                        ColorOf(piece.Tone));
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

            var y = 0f;
            var widest = 0f;

            if (_document == null)
            {
                _laidOut = Vector2.Zero;
                return;
            }

            for (var b = 0; b < _document.Blocks.Count; b++)
            {
                var block = _document.Blocks[b];

                if (b > 0)
                    y += _spacing;

                if (block.Kind == BlockKind.Rule)
                {
                    _marks.Add(new Mark { Where = new RectangleF(0f, y + _fontSize * 0.5f, 0f, 1f), Across = true });
                    y += _fontSize;
                    continue;
                }

                if (block.Kind == BlockKind.Table)
                {
                    Table(g, block, width, ref y, ref widest);
                    continue;
                }

                var size = block.Kind == BlockKind.Heading ? HeadingSize(block.Level) : _fontSize;
                var start = 0f;

                if (block.Kind == BlockKind.Bullet || block.Kind == BlockKind.Numbered)
                {
                    start = _indent;

                    if (block.Kind == BlockKind.Bullet)
                    {
                        var side = size / 5f;
                        _marks.Add(new Mark { Where = new RectangleF((_indent - side) * 0.5f, y + (size - side) * 0.5f, side, side) });
                    }
                    else
                        _pieces.Add(new Piece { Text = block.Level + ".", At = new Vector2(0f, y), Font = _font, Size = size, Tone = Tone.Plain });
                }

                Flow(g, block, start, size, width, Alignment.Left, ref y, ref widest);
            }

            _laidOut = new Vector2(widest, y);
        }

        /// <summary>Lays a table out from line <paramref name="y" />, a row at a time, moving <paramref name="y" /> below it.</summary>
        /// <remarks>
        ///     Each column is as wide as its widest cell on one line, a gap of one indent apart. Spare width is
        ///     shared among the starred columns by their stars; with none starred the table is only as wide as it
        ///     needs. A table too wide for its room takes the width back from its widest columns, whose cells then
        ///     wrap. A heading row is drawn in the heading color, with a rule under it.
        /// </remarks>
        void Table(IGraphics g, Block table, float width, ref float y, ref float widest)
        {
            var count = table.Columns.Count;
            var gap = _indent;
            Widths(g, table, width, gap);

            var across = gap * (count - 1);
            for (var c = 0; c < count; c++)
                across += _wides[c];

            for (var r = 0; r < table.Blocks.Count; r++)
            {
                var row = table.Blocks[r];
                var bottom = y + _fontSize;
                var x = 0f;

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
                    _marks.Add(new Mark { Where = new RectangleF(0f, y + gap * 0.25f, across, 1f) });
                    y += gap * 0.5f;
                }
            }

            if (across > widest)
                widest = across;
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
            var least = _fontSize * 3f;
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
                    var box = SizeOf(shown.Picture, image, size);
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

                var tone = heading ? Tone.Heading
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
                            if (_piece.Length == 0)
                                Begin(x, font, size, tone);

                            _piece.Append(' ');
                        }

                        i++;
                        continue;
                    }

                    var end = text.IndexOf(' ', i);
                    if (end < 0)
                        end = text.Length;

                    if (_piece.Length > 0 && (_pieceTone != tone || _pieceFont != font))
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
                        Begin(x, font, size, tone);

                    _piece.Append(text, i, end - i);
                    empty = false;
                    i = end;
                }
            }

            End(g, y, true, ref x, ref widest);
            Settle(line, x, width, align, y, tall, pictured);
            y += tall;
        }

        /// <summary>How big an image of <paramref name="picture" /> is drawn: as written, a side not written taken from <paramref name="image" />'s shape, and as tall as a line of <paramref name="size" /> where neither is written.</summary>
        static Vector2 SizeOf(Picture picture, IImageSource image, float size)
        {
            var width = picture.Width;
            var height = picture.Height;

            if (float.IsNaN(width) && float.IsNaN(height))
                height = size;

            if (float.IsNaN(width))
                width = Shape.WidthFor(image.Size, height);
            else if (float.IsNaN(height))
                height = Shape.HeightFor(image.Size, width);

            return new Vector2(width, height);
        }

        /// <summary>Starts a piece at <paramref name="x" />.</summary>
        void Begin(float x, string font, float size, Tone tone)
        {
            _pieceX = x;
            _pieceFont = font;
            _pieceSize = size;
            _pieceTone = tone;
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

            piece.Text = words;
            _pieces[last] = piece;

            _probe.Clear();
            _probe.Append(words);
            x = piece.At.X + g.MeasureText(piece.Font, _probe, piece.Size).X;
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
            _pieces.Add(new Piece { Text = _piece.ToString(), At = new Vector2(_pieceX, y), Font = _pieceFont, Size = _pieceSize, Tone = _pieceTone });
            _piece.Clear();

            x = _pieceX + advance;
            if (x > widest)
                widest = x;
        }

        float HeadingSize(int level)
        {
            return level <= 1 ? _heading1 : level == 2 ? _heading2 : _heading3;
        }

        enum Tone
        {
            Plain,
            Strong,
            Emphasis,
            Heading
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

            /// <summary>How big an image is drawn.</summary>
            public Vector2 Box;
        }

        /// <summary>A rule, a bullet's mark, or the rule under a table's heading row; a rule between blocks reaches across whatever width the text is given.</summary>
        struct Mark
        {
            public RectangleF Where;
            public bool Across;
        }
    }
}
