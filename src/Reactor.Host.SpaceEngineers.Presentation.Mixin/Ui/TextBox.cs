using System;
using System.Text;
using VRage.Game;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Graphics;

namespace Reactor.Host.SpaceEngineers.Presentation.Ui
{
    /// <summary>One line of editable text, with a caret and a selection.</summary>
    /// <remarks>
    /// It knows nothing about keyboards, sessions or the network. Every edit is a method somebody else
    /// calls, so the same box can be driven by a key, by a script, or by something that is not a keyboard
    /// at all. Whatever owns the screen decides which.
    /// </remarks>
    public class TextBox : Element
    {
        private string _font;
        private float _scale;

        private readonly StringBuilder _text = new StringBuilder(64);

        /// <summary>Reused for measuring a run of the text, so a caret position costs no allocation.</summary>
        private readonly StringBuilder _run = new StringBuilder(64);

        private float _line;
        private float _lineAt = -1f;
        private string _lineIn;

        private int _caret;
        private int _anchor;

        public TextBox()
        {
            Font = MyFontEnum.White;
            Scale = 1f;
            Color = Color.White;
            SelectionColor = new Color(40, 90, 150);
            HintColor = new Color(110, 122, 138);
            CaretColor = Color.White;
            CaretVisible = true;
        }

        /// <summary>Raised when an edit is committed, never while one is in progress.</summary>
        public event Action<TextBox> Committed;

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

        public Color SelectionColor { get; set; }

        public Color CaretColor { get; set; }

        /// <summary>
        /// What to show while the box is empty, in place of the words that are not there yet.
        /// </summary>
        /// <remarks>
        /// Says what the box is for without spending a line on a caption above it, and without an example
        /// specific enough to read as a requirement. Drawn in its own colour so it cannot be mistaken for
        /// something somebody typed.
        /// </remarks>
        public string Hint { get; set; }

        public Color HintColor { get; set; }

        /// <summary>
        /// Whether the caret is drawn this frame. Blinking is somebody else's business, because a widget with
        /// its own clock would need to know what a tick is, and this one deliberately does not.
        /// </summary>
        public bool CaretVisible { get; set; }

        /// <summary>Whether an edit session currently owns this box. Set by whatever holds the focus.</summary>
        public bool IsEditing { get; internal set; }

        /// <summary>
        /// What is typed, as a string, made once per change rather than once per frame.
        /// </summary>
        /// <remarks>
        /// The text is kept in a builder so that typing a letter does not rebuild it, and drawing
        /// needs a string - so asking the builder on the draw path allocated one every frame a box
        /// was on screen, whether or not anything had been typed. Cleared wherever the builder is
        /// written to, which is the only way to know.
        /// </remarks>
        private string Written
        {
            get { return _written ?? (_written = _text.ToString()); }
        }

        private string _written;

        public string Text
        {
            get { return Written; }
            set
            {
                _text.Clear();
                if (!string.IsNullOrEmpty(value))
                    _text.Append(value);

                _written = null;
                ClampCaret();
                InvalidateMeasure();
            }
        }

        public int Length => _text.Length;

        /// <summary>Where the caret sits, between 0 and <see cref="Length" />.</summary>
        public int Caret => _caret;

        public bool HasSelection => _caret != _anchor;

        private int SelectionStart => _caret < _anchor ? _caret : _anchor;

        private int SelectionEnd => _caret < _anchor ? _anchor : _caret;

        public void InsertChar(char c)
        {
            InvalidateMeasure();
            DeleteSelection();
            _text.Insert(_caret, c);
            _written = null;
            _caret++;
            _anchor = _caret;
        }

        public void Insert(string s)
        {
            InvalidateMeasure();
            if (string.IsNullOrEmpty(s))
                return;

            DeleteSelection();
            _text.Insert(_caret, s);
            _written = null;
            _caret += s.Length;
            _anchor = _caret;
        }

        public void Backspace()
        {
            InvalidateMeasure();
            if (DeleteSelection())
                return;

            if (_caret == 0)
                return;

            _text.Remove(_caret - 1, 1);
            _written = null;
            _caret--;
            _anchor = _caret;
        }

        public void DeleteForward()
        {
            InvalidateMeasure();
            if (DeleteSelection())
                return;

            if (_caret >= _text.Length)
                return;

            _text.Remove(_caret, 1);
            _written = null;
        }

        public void MoveCaretLeft(bool select)
        {
            MoveCaretTo(_caret - 1, select);
        }

        public void MoveCaretRight(bool select)
        {
            MoveCaretTo(_caret + 1, select);
        }

        public void MoveCaretHome(bool select)
        {
            MoveCaretTo(0, select);
        }

        public void MoveCaretEnd(bool select)
        {
            MoveCaretTo(_text.Length, select);
        }

        public void SelectAll()
        {
            _anchor = 0;
            _caret = _text.Length;
        }

        /// <summary>The selected run, or empty when there is none.</summary>
        public string SelectedText()
        {
            if (!HasSelection)
                return string.Empty;

            return _text.ToString(SelectionStart, SelectionEnd - SelectionStart);
        }

        /// <summary>Announces the current text as final. Called by the editor, never by an edit.</summary>
        internal void RaiseCommitted()
        {
            var handler = Committed;
            if (handler != null)
                handler(this);
        }

        private void MoveCaretTo(int index, bool select)
        {
            if (index < 0)
                index = 0;
            if (index > _text.Length)
                index = _text.Length;

            // Moving without holding shift collapses the selection onto wherever the caret lands, which is
            // what every text field anywhere does and therefore what nobody has to be taught.
            _caret = index;
            if (!select)
                _anchor = index;
        }

        private bool DeleteSelection()
        {
            if (!HasSelection)
                return false;

            int start = SelectionStart;
            _text.Remove(start, SelectionEnd - start);
            _written = null;
            _caret = start;
            _anchor = start;
            return true;
        }

        private void ClampCaret()
        {
            if (_caret > _text.Length)
                _caret = _text.Length;
            if (_anchor > _text.Length)
                _anchor = _text.Length;
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            float height = LineOf(g);

            if (_text.Length == 0)
                return new Vector2(0f, height);

            Vector2 size = g.MeasureText(Font, _text, Scale);
            return new Vector2(size.X, height > size.Y ? height : size.Y);
        }

        /// <summary>
        /// The line height, kept between frames. Drawing must ask the font nothing it can remember.
        /// </summary>
        /// <remarks>
        /// A lookup rather than a measurement, so it is cheap - but it is cheap sixty times a second for
        /// every one of these on screen, to be told a number that only changes when the font or the scale
        /// does. Draw is the one path invalidation cannot help, because it always runs.
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
            float height = LineOf(g);
            Vector2 at = new Vector2(ContentBounds.X, ContentBounds.Y);

            if (HasSelection)
            {
                float from = WidthTo(g, SelectionStart);
                float to = WidthTo(g, SelectionEnd);
                g.DrawSprite(Solid, new RectangleF(at.X + from, at.Y, to - from, height), SelectionColor, 0f);
            }

            if (_text.Length > 0)
                g.DrawText(Font, Written, at, Scale, Color);
            else if (!string.IsNullOrEmpty(Hint))
                g.DrawText(Font, Hint, at, Scale, HintColor);

            // Drawn over the text rather than under it, so it stays visible inside a selection.
            if (IsEditing && CaretVisible)
                g.DrawSprite(Solid, new RectangleF(at.X + WidthTo(g, _caret), at.Y, 1.5f, height), CaretColor, 0f);
        }

        /// <summary>The width of the first <paramref name="count" /> characters.</summary>
        private float WidthTo(IGraphics g, int count)
        {
            if (count <= 0)
                return 0f;

            _run.Clear();
            for (int i = 0; i < count && i < _text.Length; i++)
                _run.Append(_text[i]);

            return g.MeasureText(Font, _run, Scale).X;
        }
    }
}
