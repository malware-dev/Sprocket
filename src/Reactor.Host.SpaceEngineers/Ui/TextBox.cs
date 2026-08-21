using System;
using System.Text;
using VRage.Game;
using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// One line of editable text, with a caret and a selection.
    /// <para>
    /// It knows nothing about keyboards, sessions or the network. Every edit is a method somebody else calls,
    /// which is what lets the same box be driven by a key, by a test, or eventually by something that is not
    /// a keyboard at all. What drives it is <see cref="TextEditor" />.
    /// </para>
    /// </summary>
    public class TextBox : Element
    {
        private readonly StringBuilder _text = new StringBuilder(64);

        /// <summary>Reused for measuring a run of the text, so a caret position costs no allocation.</summary>
        private readonly StringBuilder _run = new StringBuilder(64);

        private int _caret;
        private int _anchor;

        public TextBox()
        {
            Font = MyFontEnum.White;
            Scale = 1f;
            Color = Color.White;
            SelectionColor = new Color(40, 90, 150);
            CaretColor = Color.White;
            CaretVisible = true;
        }

        /// <summary>Raised when an edit is committed, never while one is in progress.</summary>
        public event Action<TextBox> Committed;

        public string Font { get; set; }

        public float Scale { get; set; }

        public Color Color { get; set; }

        public Color SelectionColor { get; set; }

        public Color CaretColor { get; set; }

        /// <summary>
        /// Whether the caret is drawn this frame. Blinking is somebody else's business, because a widget with
        /// its own clock would need to know what a tick is, and this one deliberately does not.
        /// </summary>
        public bool CaretVisible { get; set; }

        /// <summary>Whether an edit session currently owns this box. Set by <see cref="TextEditor" />.</summary>
        public bool IsEditing { get; internal set; }

        public string Text
        {
            get { return _text.ToString(); }
            set
            {
                _text.Clear();
                if (!string.IsNullOrEmpty(value))
                    _text.Append(value);

                ClampCaret();
            }
        }

        public int Length
        {
            get { return _text.Length; }
        }

        /// <summary>Where the caret sits, between 0 and <see cref="Length" />.</summary>
        public int Caret
        {
            get { return _caret; }
        }

        public bool HasSelection
        {
            get { return _caret != _anchor; }
        }

        private int SelectionStart
        {
            get { return _caret < _anchor ? _caret : _anchor; }
        }

        private int SelectionEnd
        {
            get { return _caret < _anchor ? _anchor : _caret; }
        }

        // ---- editing ----

        public void InsertChar(char c)
        {
            DeleteSelection();
            _text.Insert(_caret, c);
            _caret++;
            _anchor = _caret;
        }

        public void Insert(string s)
        {
            if (string.IsNullOrEmpty(s))
                return;

            DeleteSelection();
            _text.Insert(_caret, s);
            _caret += s.Length;
            _anchor = _caret;
        }

        public void Backspace()
        {
            if (DeleteSelection())
                return;

            if (_caret == 0)
                return;

            _text.Remove(_caret - 1, 1);
            _caret--;
            _anchor = _caret;
        }

        public void DeleteForward()
        {
            if (DeleteSelection())
                return;

            if (_caret >= _text.Length)
                return;

            _text.Remove(_caret, 1);
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

        // ---- layout and drawing ----

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            float height = g.LineHeight(Font, Scale);

            if (_text.Length == 0)
                return new Vector2(0f, height);

            Vector2 size = g.MeasureText(Font, _text, Scale);
            return new Vector2(size.X, height > size.Y ? height : size.Y);
        }

        protected override void DrawContent(IGraphics g)
        {
            float height = g.LineHeight(Font, Scale);
            Vector2 at = new Vector2(Bounds.X, Bounds.Y);

            if (HasSelection)
            {
                float from = WidthTo(g, SelectionStart);
                float to = WidthTo(g, SelectionEnd);
                g.DrawSprite(Fill, new RectangleF(at.X + from, at.Y, to - from, height), SelectionColor, 0f);
            }

            if (_text.Length > 0)
                g.DrawText(Font, _text.ToString(), at, Scale, Color);

            // Drawn over the text rather than under it, so it stays visible inside a selection.
            if (IsEditing && CaretVisible)
                g.DrawSprite(Fill, new RectangleF(at.X + WidthTo(g, _caret), at.Y, 1.5f, height), CaretColor, 0f);
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
