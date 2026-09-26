using System.Collections.Generic;
using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Look;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>A run of text, on one line or on as many as it needs.</summary>
    /// <remarks>
    ///     Keeps a <c>StringBuilder</c> beside the string, because <c>MeasureText</c> measures from one
    ///     without allocating.
    /// </remarks>
    public class Label : Element
    {
        const float SampleWide = 44f;

        readonly List<string> _lines = new List<string>();

        readonly List<int> _breaks = new List<int>();

        readonly StringBuilder _measured = new StringBuilder(64);
        readonly StringBuilder _scratch = new StringBuilder(64);

        string _centred;

        float _centredAt = -1f;
        string _centredIn;
        float _centredWidth;

        bool _dirty = true;

        float _flat;
        float _flatAt = -1f;
        bool _flatDirty = true;
        string _flatIn;
        string _font;
        float _laidOutAt = -1f;

        float _laidOutFor = -1f;
        string _laidOutIn;

        Color? _sample;

        float _fontSize;

        string _text = string.Empty;
        bool _trims;
        Vector2 _wrapped;
        bool _wraps;

        /// <summary>An empty label in white, with lines 24 units tall.</summary>
        public Label()
        {
            Font = MyFontEnum.White;
            FontSize = 24f;
            Color = Color.White;
        }

        /// <summary>The words shown.</summary>
        public string Text
        {
            get { return _text; }
            set
            {
                var incoming = value ?? string.Empty;
                if (_text == incoming)
                    return;

                _text = incoming;
                _measured.Clear();
                _measured.Append(incoming);
                _dirty = true;
                _flatDirty = true;
                InvalidateMeasure();
            }
        }

        /// <summary>Which face it is drawn in.</summary>
        public string Font
        {
            get { return _font; }
            set
            {
                if (_font == value)
                    return;

                _font = value;
                InvalidateMeasure();
            }
        }

        /// <summary>How tall one line is, in UI units.</summary>
        public float FontSize
        {
            get { return _fontSize; }
            set
            {
                if (_fontSize == value)
                    return;

                _fontSize = value;
                InvalidateMeasure();
            }
        }

        /// <summary>The colour the words are drawn in.</summary>
        public Color Color { get; set; }

        /// <summary>Whether a run too wide for its room continues on the next line.</summary>
        public bool Wraps
        {
            get { return _wraps; }
            set
            {
                if (_wraps == value)
                    return;

                _wraps = value;
                _dirty = true;
                InvalidateMeasure();
            }
        }

        /// <summary>The lines a wrapped or trimmed run was last laid out as.</summary>
        internal IList<string> Lines => _lines;

        /// <summary>Whether a run too wide for its room is cut short with an ellipsis.</summary>
        /// <remarks>Ignored when <see cref="Wraps" /> is set.</remarks>
        public bool Trims
        {
            get { return _trims; }
            set
            {
                if (_trims == value)
                    return;

                _trims = value;
                _dirty = true;
                InvalidateMeasure();
            }
        }

        /// <summary>Whether the words sit in the middle of the room they were given.</summary>
        public bool Centres { get; set; }

        /// <summary>A colour to show instead of the words, or nothing to show the words.</summary>
        public Color? Sample
        {
            get { return _sample; }
            set
            {
                if (_sample == value)
                    return;

                _sample = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_sample.HasValue)
                return new Vector2(SampleWide, FontSize);

            if (_text.Length == 0)
                return new Vector2(0f, FontSize);

            // The line height, not the measured height: DrawText places a line box at ContentBounds.Y,
            // and the game's glyph cell is 45 against a line of 37, so the measured height is not what
            // is drawn.
            if (!Wraps && !Trims)
                return new Vector2(Flat(g), FontSize);

            if (!Wraps)
            {
                Retrim(g, available.X);
                return new Vector2(_wrapped.X, FontSize);
            }

            Rewrap(g, available.X);
            return _wrapped;
        }

        /// <summary>The width of one unwrapped run, kept between measures.</summary>
        /// <remarks>
        ///     Kept because a measure re-runs whenever anything above it changes size, and measuring text is
        ///     the dearest call the layout makes.
        /// </remarks>
        float Flat(IGraphics g)
        {
            if (!_flatDirty && _flatAt == FontSize && _flatIn == Font)
                return _flat;

            _flatDirty = false;
            _flatAt = FontSize;
            _flatIn = Font;
            _flat = g.MeasureText(Font, _measured, FontSize).X;

            return _flat;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (_sample.HasValue)
            {
                g.Fill(ContentBounds, _sample.Value);
                Outline(g, ContentBounds, Palette.Line, Space.Hair);
                return;
            }

            if (_text.Length == 0)
                return;

            if (!Wraps)
            {
                var shown = Trims && _lines.Count > 0 ? _lines[0] : _text;
                g.DrawText(Font, shown, new Vector2(Left(g, shown), ContentBounds.Y), FontSize, Color);
                return;
            }

            var line = FontSize;
            var y = ContentBounds.Y;

            for (var i = 0; i < _lines.Count; i++)
            {
                g.DrawText(Font, _lines[i], new Vector2(Left(g, _lines[i]), y), FontSize, Color);
                y += line;
            }
        }

        /// <summary>Where a line starts, which is the left edge unless this is centring.</summary>
        float Left(IGraphics g, string line)
        {
            if (!Centres)
                return ContentBounds.X;

            if (_centred != line || _centredAt != FontSize || _centredIn != Font)
            {
                _scratch.Clear();
                _scratch.Append(line);

                _centred = line;
                _centredAt = FontSize;
                _centredIn = Font;
                _centredWidth = g.MeasureText(Font, _scratch, FontSize).X;
            }

            var width = _centredWidth;
            if (width >= ContentBounds.Width)
                return ContentBounds.X;

            return ContentBounds.X + (ContentBounds.Width - width) * 0.5f;
        }

        /// <summary>Breaks the run into lines that fit <paramref name="width" />, if anything has changed since.</summary>
        void Rewrap(IGraphics g, float width)
        {
            if (!_dirty && _laidOutFor == width && _laidOutAt == FontSize && _laidOutIn == Font)
                return;

            _dirty = false;
            _laidOutFor = width;
            _laidOutAt = FontSize;
            _laidOutIn = Font;

            _lines.Clear();

            var widest = 0f;
            var i = 0;

            while (i < _text.Length)
            {
                var end = _text.IndexOf('\n', i);
                if (end < 0)
                    end = _text.Length;

                Paragraph(g, i, end, width, ref widest);
                i = end + 1;
            }

            _wrapped = new Vector2(widest, _lines.Count * FontSize);
        }

        /// <summary>Breaks the text from <paramref name="start" /> to <paramref name="end" />, which holds no newline, into lines no wider than <paramref name="width" />.</summary>
        /// <remarks>
        ///     Each line is the furthest word end that fits, found by halving: a few measures a line, where
        ///     trying one word after another took one a word. A word too wide for any line has one to itself,
        ///     and an empty paragraph is an empty line.
        /// </remarks>
        void Paragraph(IGraphics g, int start, int end, float width, ref float widest)
        {
            _breaks.Clear();
            for (var b = start; b < end;)
            {
                b = NextBreak(b);
                _breaks.Add(b < end ? b : end);
            }

            if (_breaks.Count == 0)
            {
                _lines.Add("");
                return;
            }

            var from = start;
            var first = 0;

            while (first < _breaks.Count)
            {
                var best = first;
                var fits = Width(g, from, _breaks[first]);
                var last = _breaks.Count - 1;

                if (last > first)
                {
                    var whole = Width(g, from, _breaks[last]);
                    if (whole <= width)
                    {
                        best = last;
                        fits = whole;
                    }
                    else if (fits <= width)
                    {
                        var low = first + 1;
                        var high = last - 1;

                        while (low <= high)
                        {
                            var middle = (low + high) / 2;
                            var measured = Width(g, from, _breaks[middle]);

                            if (measured <= width)
                            {
                                best = middle;
                                fits = measured;
                                low = middle + 1;
                            }
                            else
                                high = middle - 1;
                        }
                    }
                }

                _lines.Add(Line(from, _breaks[best]));
                if (fits > widest)
                    widest = fits;

                from = _breaks[best];
                first = best + 1;
            }
        }

        /// <summary>How wide the text from <paramref name="from" /> to <paramref name="to" /> draws, without the spaces it starts with.</summary>
        float Width(IGraphics g, int from, int to)
        {
            while (from < to && _text[from] == ' ')
                from++;

            _scratch.Clear();
            _scratch.Append(_text, from, to - from);
            return g.MeasureText(Font, _scratch, FontSize).X;
        }

        /// <summary>The text from <paramref name="from" /> to <paramref name="to" />, without the spaces it starts with.</summary>
        string Line(int from, int to)
        {
            while (from < to && _text[from] == ' ')
                from++;

            return _text.Substring(from, to - from);
        }

        /// <summary>Cuts the run down to the most of it that fits with an ellipsis, found by halving.</summary>
        void Retrim(IGraphics g, float width)
        {
            if (!_dirty && _laidOutFor == width && _laidOutAt == FontSize && _laidOutIn == Font)
                return;

            _dirty = false;
            _laidOutFor = width;
            _laidOutAt = FontSize;
            _laidOutIn = Font;

            _lines.Clear();

            var full = g.MeasureText(Font, _measured, FontSize).X;
            if (full <= width)
            {
                _lines.Add(_text);
                _wrapped = new Vector2(full, 0f);
                return;
            }

            var keep = 0;
            var low = 1;
            var high = _text.Length - 1;

            while (low <= high)
            {
                var middle = (low + high) / 2;

                _scratch.Clear();
                _scratch.Append(_text, 0, middle);
                _scratch.Append("…");

                if (g.MeasureText(Font, _scratch, FontSize).X <= width)
                {
                    keep = middle;
                    low = middle + 1;
                }
                else
                    high = middle - 1;
            }

            _lines.Add(_text.Substring(0, keep) + "…");
            _wrapped = new Vector2(width, 0f);
        }

        /// <summary>Where the next word ends, counting the spaces that follow it as part of it.</summary>
        int NextBreak(int from)
        {
            var i = from;

            while (i < _text.Length && _text[i] == ' ')
                i++;

            while (i < _text.Length && _text[i] != ' ' && _text[i] != '\n')
                i++;

            return i > from ? i : from + 1;
        }

    }
}