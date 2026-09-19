using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Drawing;

namespace Sprocket.Host.SpaceEngineers.Layout
{
    /// <summary>A run of text, on one line or on as many as it needs.</summary>
    /// <remarks>
    /// It keeps a <c>StringBuilder</c> beside the string it was given, because the only allocation-free
    /// way to measure is from one of those. Writing <see cref="Text" /> syncs it, so the cost lands when
    /// somebody changes the words.
    /// <para>
    /// Wrapping is off by default and is not free: breaking a run costs a measurement per word, so the
    /// result is kept until the words, the size or the room they have to fit in change.
    /// </para>
    /// </remarks>
    public class Label : Element
    {
        private string _font;
        private float _scale;
        private bool _wraps;
        private bool _trims;

        private readonly StringBuilder _measured = new StringBuilder(64);
        private readonly StringBuilder _scratch = new StringBuilder(64);
        private readonly List<string> _lines = new List<string>();

        private string _text = string.Empty;

        private float _laidOutFor = -1f;
        private float _laidOutAt = -1f;
        private string _laidOutIn;
        private float _line;
        private float _lineAt = -1f;
        private string _lineIn;

        private bool _dirty = true;

        private float _flat;
        private float _flatAt = -1f;
        private string _flatIn;
        private bool _flatDirty = true;
        private Vector2 _wrapped;

        /// <summary>The width of the line last centred, kept so that centring does not cost a measurement.</summary>
        /// <remarks>
        /// Drawing is not invalidated - it happens on every frame by definition - so anything that asks
        /// the font a question here asks it sixty times a second no matter what the layout does.
        /// </remarks>
        private string _centred;

        private float _centredAt = -1f;
        private string _centredIn;
        private float _centredWidth;

        public Label()
        {
            Font = MyFontEnum.White;
            Scale = 1f;
            Color = Color.White;
        }

        public string Text
        {
            get { return _text; }
            set
            {
                string incoming = value ?? string.Empty;
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

        public Color Color { get; set; }

        /// <summary>
        /// Whether a run too wide for its room continues on the next line rather than running off the end.
        /// </summary>
        public bool Wraps
        {
            get { return _wraps; }
            set
            {
                if (_wraps == value)
                    return;

                _wraps = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Whether a run too wide for its room is cut short with an ellipsis instead of overflowing.
        /// </summary>
        /// <remarks>
        /// For the things that must stay one line and inside their box - a heading, where a long name
        /// pushing past the edge would carry whatever sits beside it off the end.
        /// <para>
        /// Ignored when <see cref="Wraps" /> is set.
        /// </para>
        /// </remarks>
        public bool Trims
        {
            get { return _trims; }
            set
            {
                if (_trims == value)
                    return;

                _trims = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Whether the words sit in the middle of the room they were given rather than at its left edge.
        /// </summary>
        /// <remarks>
        /// For a control held to a width it did not choose. A switch says two different words in two
        /// states, and the way to stop it stepping about as they swap is to give it the width of the
        /// longer one and put both in the middle of it.
        /// </remarks>
        public bool Centres { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_text.Length == 0)
                return new Vector2(0f, g.LineHeight(Font, Scale));

            // A line tall, because a line is what this draws: DrawText places a line box at
            // ContentBounds.Y and the renderer puts the baseline inside it, so how far the particular
            // letters reach has nothing to do with how much room the drawing takes. The glyph cell is not
            // the same number - in the game's face it is 45 against a line of 37 - so a label measured
            // that way reports a size it does not draw at. Only the width is worth asking the letters
            // about.
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

        /// <summary>
        /// The width of one unwrapped run, kept between frames.
        /// </summary>
        /// <remarks>
        /// For the same reason the line height is kept: a control that does not wrap - a button, a
        /// switch, a chip - is measured again whenever anything above it changes size, and the width of
        /// words that have not changed is a number already known. Text measurement is the dearest call
        /// in the layout and the one no picture can show being made.
        /// </remarks>
        private float Flat(IGraphics g)
        {
            if (!_flatDirty && _flatAt == Scale && _flatIn == Font)
                return _flat;

            _flatDirty = false;
            _flatAt = Scale;
            _flatIn = Font;
            _flat = g.MeasureText(Font, _measured, Scale).X;

            return _flat;
        }

        /// <summary>The line height, kept between frames.</summary>
        /// <remarks>
        /// Cheap, but cheap sixty times a second for every label on screen, to be told a number that only
        /// changes when the font or the scale does. Draw is the one path invalidation cannot help,
        /// because it always runs.
        /// </remarks>
        private float LineOf(IGraphics g)
        {
            if (_lineAt != Scale || _lineIn != Font)
            {
                _lineAt = Scale;
                _lineIn = Font;
                _line = g.LineHeight(Font, Scale);
            }

            return _line;
        }

        protected override void DrawContent(IGraphics g)
        {
            if (_text.Length == 0)
                return;

            if (!Wraps)
            {
                string shown = Trims && _lines.Count > 0 ? _lines[0] : _text;
                g.DrawText(Font, shown, new Vector2(Left(g, shown), ContentBounds.Y), Scale, Color);
                return;
            }

            float line = LineOf(g);
            float y = ContentBounds.Y;

            for (int i = 0; i < _lines.Count; i++)
            {
                g.DrawText(Font, _lines[i], new Vector2(Left(g, _lines[i]), y), Scale, Color);
                y += line;
            }
        }

        /// <summary>Where a line starts, which is the left edge unless this is centring.</summary>
        private float Left(IGraphics g, string line)
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

            float width = _centredWidth;
            if (width >= ContentBounds.Width)
                return ContentBounds.X;

            return ContentBounds.X + (ContentBounds.Width - width) * 0.5f;
        }

        /// <summary>
        /// Breaks the run into lines that fit <paramref name="width" />, if anything about the problem has
        /// changed since the last time.
        /// </summary>
        private void Rewrap(IGraphics g, float width)
        {
            if (!_dirty && _laidOutFor == width && _laidOutAt == Scale && _laidOutIn == Font)
                return;

            _dirty = false;
            _laidOutFor = width;
            _laidOutAt = Scale;
            _laidOutIn = Font;

            _lines.Clear();

            float line = LineOf(g);
            float widest = 0f;

            _scratch.Clear();
            float committed = 0f;

            int i = 0;
            while (i < _text.Length)
            {
                // A newline ends the line wherever it falls, which is what somebody typing one meant.
                // An empty line between two of them is a paragraph break and is kept as one: the
                // annotations people write have them, and squeezing them out runs two thoughts together.
                if (_text[i] == '\n')
                {
                    Commit(ref widest, committed);
                    committed = 0f;
                    i++;
                    continue;
                }

                int end = NextBreak(i);
                int length = end - i;

                _scratch.Append(_text, i, length);
                float candidate = g.MeasureText(Font, _scratch, Scale).X;

                if (candidate > width && committed > 0f)
                {
                    // It did not fit, so the line ends before this word and the word starts the next.
                    _scratch.Length -= length;
                    Commit(ref widest, committed);

                    _scratch.Append(_text, i, length);

                    // A leading space would then sit at the start of the new line, which reads as an
                    // indent nobody asked for.
                    TrimLeading();
                    committed = g.MeasureText(Font, _scratch, Scale).X;
                }
                else
                {
                    committed = candidate;
                }

                i = end;
            }

            if (_scratch.Length > 0)
                Commit(ref widest, committed);

            _wrapped = new Vector2(widest, _lines.Count * line);
        }

        /// <summary>
        /// Cuts the run down to something that fits, ending in an ellipsis.
        /// </summary>
        /// <remarks>
        /// By character rather than by word. A heading is often one long word, and trimming to the last
        /// whole word would leave a name showing nothing but its first syllable.
        /// </remarks>
        private void Retrim(IGraphics g, float width)
        {
            if (!_dirty && _laidOutFor == width && _laidOutAt == Scale && _laidOutIn == Font)
                return;

            _dirty = false;
            _laidOutFor = width;
            _laidOutAt = Scale;
            _laidOutIn = Font;

            _lines.Clear();

            float full = g.MeasureText(Font, _measured, Scale).X;
            if (full <= width)
            {
                _lines.Add(_text);
                _wrapped = new Vector2(full, 0f);
                return;
            }

            int keep = _text.Length;
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
        private int NextBreak(int from)
        {
            int i = from;

            while (i < _text.Length && _text[i] == ' ')
                i++;

            while (i < _text.Length && _text[i] != ' ' && _text[i] != '\n')
                i++;

            return i > from ? i : from + 1;
        }

        private void Commit(ref float widest, float width)
        {
            _lines.Add(_scratch.ToString());
            _scratch.Clear();

            if (width > widest)
                widest = width;
        }

        private void TrimLeading()
        {
            while (_scratch.Length > 0 && _scratch[0] == ' ')
                _scratch.Remove(0, 1);
        }
    }
}
