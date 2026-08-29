using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// A run of text, on one line or on as many as it needs.
    /// <para>
    /// It keeps a <c>StringBuilder</c> beside the string it was given, because measuring happens every layout
    /// pass and the only allocation-free way to measure is from one of those. Writing <see cref="Text" />
    /// syncs it, so the cost lands when somebody changes the words rather than sixty times a second.
    /// </para>
    /// <para>
    /// Wrapping is off by default and is not free: breaking a run costs a measurement per word, so the
    /// result is kept until the words, the size or the room they have to fit in actually change. A notice
    /// or a banner wants it; a row of one short phrase does not.
    /// </para>
    /// </summary>
    public class Label : Element
    {
        private readonly StringBuilder _measured = new StringBuilder(64);
        private readonly StringBuilder _scratch = new StringBuilder(64);
        private readonly List<string> _lines = new List<string>();

        private string _text = string.Empty;

        private float _laidOutFor = -1f;
        private float _laidOutAt = -1f;
        private string _laidOutIn;
        private bool _dirty = true;
        private Vector2 _wrapped;

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
            }
        }

        public string Font { get; set; }

        public float Scale { get; set; }

        public Color Color { get; set; }

        /// <summary>
        /// Whether a run too wide for its room continues on the next line rather than running off the end.
        /// </summary>
        public bool Wraps { get; set; }

        /// <summary>
        /// Whether a run too wide for its room is cut short with an ellipsis instead of overflowing.
        /// </summary>
        /// <remarks>
        /// For the things that must stay one line and must stay inside their box. A heading is the case:
        /// a long name that pushed past the edge would carry whatever sits beside it off the end, and a
        /// control pushed out of sight leaves nothing behind to say it was ever there.
        /// <para>
        /// Ignored when <see cref="Wraps" /> is set, since a run that can use another line has no need of
        /// this one.
        /// </para>
        /// </remarks>
        public bool Trims { get; set; }

        /// <summary>
        /// Whether the words sit in the middle of the room they were given rather than at its left edge.
        /// </summary>
        /// <remarks>
        /// For a control held to a width it did not choose. A switch labelled by what pressing it does
        /// says two different words in two states, and the honest way to stop the header stepping about
        /// as they swap is to give it the width of the longer one and put both in the middle of it -
        /// rather than padding one with spaces until it happens to match, which is arithmetic done by eye
        /// in a font that was never asked.
        /// </remarks>
        public bool Centres { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_text.Length == 0)
                return new Vector2(0f, g.LineHeight(Font, Scale));

            // A line, like the three answers around it, because a line is what this draws. DrawText places
            // a line box at ContentBounds.Y and the renderer puts the baseline inside it; how far the
            // particular letters reach has nothing to do with how much room the drawing takes.
            //
            // Measuring answered with the glyph cell instead, which in the game's face is 45 units against
            // a line of 37 - a fifth taller, for any text at all. So a label reported a size it did not
            // draw at, and anything that centred it put it a tenth of a line out. Only the width is worth
            // asking the letters about.
            if (!Wraps && !Trims)
                return new Vector2(g.MeasureText(Font, _measured, Scale).X, g.LineHeight(Font, Scale));

            if (!Wraps)
            {
                Retrim(g, available.X);
                return new Vector2(_wrapped.X, g.LineHeight(Font, Scale));
            }

            Rewrap(g, available.X);
            return _wrapped;
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

            float line = g.LineHeight(Font, Scale);
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

            _scratch.Clear();
            _scratch.Append(line);

            float width = g.MeasureText(Font, _scratch, Scale).X;
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

            float line = g.LineHeight(Font, Scale);
            float widest = 0f;

            _scratch.Clear();
            float committed = 0f;

            int i = 0;
            while (i < _text.Length)
            {
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

            while (i < _text.Length && _text[i] != ' ')
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
