using System.Collections.Generic;
using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Look;
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
        /// <summary>How wide a colour is drawn in place of words.</summary>
        const float SampleWide = 44f;

        readonly List<string> _lines = new List<string>();

        readonly StringBuilder _measured = new StringBuilder(64);
        readonly StringBuilder _scratch = new StringBuilder(64);

        /// <summary>The line last centred, kept with its width because drawing runs every frame.</summary>
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
        float _line;
        float _lineAt = -1f;
        string _lineIn;

        Color? _sample;
        float _scale;

        string _text = string.Empty;
        bool _trims;
        Vector2 _wrapped;
        bool _wraps;

        /// <summary>An empty label in white, at a scale of one.</summary>
        public Label()
        {
            Font = MyFontEnum.White;
            Scale = 1f;
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

        /// <summary>The font scale it is drawn at.</summary>
        public float Scale
        {
            get { return _scale; }
            set
            {
                if (_scale == value)
                    return;

                _scale = value;
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
                return new Vector2(SampleWide, g.LineHeight(Font, Scale));

            if (_text.Length == 0)
                return new Vector2(0f, g.LineHeight(Font, Scale));

            // The line height, not the measured height: DrawText places a line box at ContentBounds.Y,
            // and the game's glyph cell is 45 against a line of 37, so the measured height is not what
            // is drawn.
            if (!Wraps && !Trims)
                return new Vector2(Flat(g), LineOf(g));

            if (!Wraps)
            {
                Retrim(g, available.X);
                return new Vector2(_wrapped.X, g.LineHeight(Font, Scale));
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
            if (!_flatDirty && _flatAt == Scale && _flatIn == Font)
                return _flat;

            _flatDirty = false;
            _flatAt = Scale;
            _flatIn = Font;
            _flat = g.MeasureText(Font, _measured, Scale).X;

            return _flat;
        }

        /// <summary>The line height, kept because drawing asks for it every frame.</summary>
        float LineOf(IGraphics g)
        {
            if (_lineAt != Scale || _lineIn != Font)
            {
                _lineAt = Scale;
                _lineIn = Font;
                _line = g.LineHeight(Font, Scale);
            }

            return _line;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (_sample.HasValue)
            {
                g.DrawSprite(Solid, ContentBounds, _sample.Value, 0f);
                Outline(g, ContentBounds, Palette.Line, Space.Hair);
                return;
            }

            if (_text.Length == 0)
                return;

            if (!Wraps)
            {
                var shown = Trims && _lines.Count > 0 ? _lines[0] : _text;
                g.DrawText(Font, shown, new Vector2(Left(g, shown), ContentBounds.Y), Scale, Color);
                return;
            }

            var line = LineOf(g);
            var y = ContentBounds.Y;

            for (var i = 0; i < _lines.Count; i++)
            {
                g.DrawText(Font, _lines[i], new Vector2(Left(g, _lines[i]), y), Scale, Color);
                y += line;
            }
        }

        /// <summary>Where a line starts, which is the left edge unless this is centring.</summary>
        float Left(IGraphics g, string line)
        {
            if (!Centres)
                return ContentBounds.X;

            if (_centred != line || _centredAt != Scale || _centredIn != Font)
            {
                _scratch.Clear();
                _scratch.Append(line);

                _centred = line;
                _centredAt = Scale;
                _centredIn = Font;
                _centredWidth = g.MeasureText(Font, _scratch, Scale).X;
            }

            var width = _centredWidth;
            if (width >= ContentBounds.Width)
                return ContentBounds.X;

            return ContentBounds.X + (ContentBounds.Width - width) * 0.5f;
        }

        /// <summary>Breaks the run into lines that fit <paramref name="width" />, if anything has changed since.</summary>
        void Rewrap(IGraphics g, float width)
        {
            if (!_dirty && _laidOutFor == width && _laidOutAt == Scale && _laidOutIn == Font)
                return;

            _dirty = false;
            _laidOutFor = width;
            _laidOutAt = Scale;
            _laidOutIn = Font;

            _lines.Clear();

            var line = LineOf(g);
            var widest = 0f;

            _scratch.Clear();
            var committed = 0f;

            var i = 0;
            while (i < _text.Length)
            {
                // An empty line between two newlines is kept as a line of its own.
                if (_text[i] == '\n')
                {
                    Commit(ref widest, committed);
                    committed = 0f;
                    i++;
                    continue;
                }

                var end = NextBreak(i);
                var length = end - i;

                _scratch.Append(_text, i, length);
                var candidate = g.MeasureText(Font, _scratch, Scale).X;

                if (candidate > width && committed > 0f)
                {
                    _scratch.Length -= length;
                    Commit(ref widest, committed);

                    _scratch.Append(_text, i, length);

                    TrimLeading();
                    committed = g.MeasureText(Font, _scratch, Scale).X;
                }
                else
                    committed = candidate;

                i = end;
            }

            if (_scratch.Length > 0)
                Commit(ref widest, committed);

            _wrapped = new Vector2(widest, _lines.Count * line);
        }

        /// <summary>Cuts the run down, a character at a time, to something that fits with an ellipsis.</summary>
        void Retrim(IGraphics g, float width)
        {
            if (!_dirty && _laidOutFor == width && _laidOutAt == Scale && _laidOutIn == Font)
                return;

            _dirty = false;
            _laidOutFor = width;
            _laidOutAt = Scale;
            _laidOutIn = Font;

            _lines.Clear();

            var full = g.MeasureText(Font, _measured, Scale).X;
            if (full <= width)
            {
                _lines.Add(_text);
                _wrapped = new Vector2(full, 0f);
                return;
            }

            var keep = _text.Length;
            while (keep > 0)
            {
                keep--;

                _scratch.Clear();
                _scratch.Append(_text, 0, keep);
                _scratch.Append("…");

                if (g.MeasureText(Font, _scratch, Scale).X <= width)
                    break;
            }

            _lines.Add(_scratch.ToString());
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

        void Commit(ref float widest, float width)
        {
            _lines.Add(_scratch.ToString());
            _scratch.Clear();

            if (width > widest)
                widest = width;
        }

        void TrimLeading()
        {
            while (_scratch.Length > 0 && _scratch[0] == ' ')
                _scratch.Remove(0, 1);
        }
    }
}