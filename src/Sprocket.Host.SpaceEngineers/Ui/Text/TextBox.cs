using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>One line of editable text, with a caret and a selection.</summary>
    public class TextBox : Element
    {
        /// <summary>The room held for the way to empty it, where there is one.</summary>
        /// <remarks>Matches the room the chips in a sentence hold for the same mark.</remarks>
        const float Clearing = 20f;

        /// <summary>How thick the line under a box holding something that cannot be accepted is.</summary>
        const float Rule = 2f;

        /// <summary>Room kept after the words for the caret, so it is inside the box at the end of a full one.</summary>
        const float CaretRoom = 2f;

        readonly ElementCollection _children;

        /// <summary>Reused for measuring a run of the text, so a caret position costs no allocation.</summary>
        readonly StringBuilder _run = new StringBuilder(64);

        readonly StringBuilder _text = new StringBuilder(64);
        int _anchor;

        Element _clear;
        string _font;

        float _line;
        float _lineAt = -1f;
        string _lineIn;
        float _scale;

        string _written;

        /// <summary>An empty box in white, at a scale of one, with its caret shown.</summary>
        public TextBox()
        {
            _children = new ElementCollection(this);

            Font = MyFontEnum.White;
            Scale = 1f;
            Color = Color.White;
            SelectionColor = new Color(40, 90, 150);
            HintColor = new Color(110, 122, 138);
            CaretColor = Color.White;
            CaretVisible = true;
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

        /// <summary>The colour painted behind the selected run.</summary>
        public Color SelectionColor { get; set; }

        /// <summary>The colour of the caret.</summary>
        public Color CaretColor { get; set; }

        /// <summary>What to show while the box is empty.</summary>
        public string Hint { get; set; }

        /// <summary>The colour <see cref="Hint" /> is drawn in.</summary>
        public Color HintColor { get; set; }

        /// <summary>Whether the caret is drawn this frame.</summary>
        /// <remarks>Blinking is the caller's to do; this has no clock.</remarks>
        public bool CaretVisible { get; set; }

        /// <summary>Whether an edit session currently owns this box.</summary>
        /// <remarks>Set by whatever holds the focus.</remarks>
        public bool IsEditing { get; internal set; }

        /// <summary>What is typed, as a string, made once per change.</summary>
        /// <remarks>
        ///     Drawing needs a string every frame, so this is kept; anything that writes to the builder must
        ///     set <c>_written</c> to null.
        /// </remarks>
        string Written => _written ?? (_written = _text.ToString());

        /// <summary>What is in the box; setting it replaces the lot and keeps the caret inside it.</summary>
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

        /// <summary>How many characters are in the box.</summary>
        public int Length => _text.Length;

        /// <summary>Where the caret sits, between 0 and <see cref="Length" />.</summary>
        public int Caret { get; private set; }

        /// <summary>Whether a run is selected.</summary>
        public bool HasSelection => Caret != _anchor;

        int SelectionStart => Caret < _anchor ? Caret : _anchor;

        int SelectionEnd => Caret < _anchor ? _anchor : Caret;

        /// <summary>The way to empty this box, drawn inside it at the right, or nothing.</summary>
        /// <remarks>
        ///     Its room is held in the padding for as long as there is one, showing or not, so the text does
        ///     not shift when it appears.
        /// </remarks>
        public Element Clear
        {
            get { return _clear; }
            set
            {
                if (_clear == value)
                    return;

                if (_clear != null)
                    _children.Remove(_clear);

                if (value != null)
                    _children.Add(value);

                _clear = value;

                Padding = new Thickness(Padding.Left, Padding.Top,
                    value == null ? Padding.Left : Padding.Left + Clearing, Padding.Bottom);
            }
        }

        /// <summary>Whether what is in the box cannot be accepted as it stands.</summary>
        public bool Wrong { get; set; }

        /// <summary>The line under the words while <see cref="Wrong" />.</summary>
        public Color WrongEdge { get; set; }

        /// <summary>Whether what is in the box sits against its right-hand end.</summary>
        public bool Trails { get; set; }

        /// <summary>Types <paramref name="c" /> at the caret, replacing any selection.</summary>
        public void InsertChar(char c)
        {
            InvalidateMeasure();
            DeleteSelection();
            _text.Insert(Caret, c);
            _written = null;
            Caret++;
            _anchor = Caret;
        }

        /// <summary>Types <paramref name="s" /> at the caret, replacing any selection when there is something to type.</summary>
        public void Insert(string s)
        {
            InvalidateMeasure();
            if (string.IsNullOrEmpty(s))
                return;

            DeleteSelection();
            _text.Insert(Caret, s);
            _written = null;
            Caret += s.Length;
            _anchor = Caret;
        }

        /// <summary>Deletes the selection, or else the character before the caret.</summary>
        public void Backspace()
        {
            InvalidateMeasure();
            if (DeleteSelection())
                return;

            if (Caret == 0)
                return;

            _text.Remove(Caret - 1, 1);
            _written = null;
            Caret--;
            _anchor = Caret;
        }

        /// <summary>Deletes the selection, or else the character after the caret.</summary>
        public void DeleteForward()
        {
            InvalidateMeasure();
            if (DeleteSelection())
                return;

            if (Caret >= _text.Length)
                return;

            _text.Remove(Caret, 1);
            _written = null;
        }

        /// <summary>Moves the caret one character left, extending the selection when <paramref name="select" /> is set.</summary>
        public void MoveCaretLeft(bool select)
        {
            MoveCaretTo(Caret - 1, select);
        }

        /// <summary>Moves the caret one character right, extending the selection when <paramref name="select" /> is set.</summary>
        public void MoveCaretRight(bool select)
        {
            MoveCaretTo(Caret + 1, select);
        }

        /// <summary>Moves the caret to the start, extending the selection when <paramref name="select" /> is set.</summary>
        public void MoveCaretHome(bool select)
        {
            MoveCaretTo(0, select);
        }

        /// <summary>Moves the caret to the end, extending the selection when <paramref name="select" /> is set.</summary>
        public void MoveCaretEnd(bool select)
        {
            MoveCaretTo(_text.Length, select);
        }

        /// <summary>Selects everything in the box, with the caret at the end.</summary>
        public void SelectAll()
        {
            _anchor = 0;
            Caret = _text.Length;
        }

        /// <summary>The selected run, or empty when there is none.</summary>
        public string SelectedText()
        {
            if (!HasSelection)
                return string.Empty;

            return _text.ToString(SelectionStart, SelectionEnd - SelectionStart);
        }

        void MoveCaretTo(int index, bool select)
        {
            if (index < 0)
                index = 0;
            if (index > _text.Length)
                index = _text.Length;

            Caret = index;
            if (!select)
                _anchor = index;
        }

        bool DeleteSelection()
        {
            if (!HasSelection)
                return false;

            var start = SelectionStart;
            _text.Remove(start, SelectionEnd - start);
            _written = null;
            Caret = start;
            _anchor = start;
            return true;
        }

        void ClampCaret()
        {
            if (Caret > _text.Length)
                Caret = _text.Length;
            if (_anchor > _text.Length)
                _anchor = _text.Length;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var height = LineOf(g);

            _clear?.Measure(g, new Vector2(Clearing, height));

            if (_text.Length == 0)
                return new Vector2(0f, height);

            var size = g.MeasureText(Font, _text, Scale);
            return new Vector2(size.X, height > size.Y ? height : size.Y);
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
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_clear == null)
                return;

            var wanted = _clear.DesiredSize;
            var x = Bounds.X + Bounds.Width - Padding.Left * 0.5f - wanted.X;
            var y = bounds.Y + (bounds.Height - wanted.Y) * 0.5f;

            _clear.Arrange(new RectangleF(x, y, wanted.X, wanted.Y));
        }

        /// <summary>Only the way to empty the box answers, ahead of the box itself.</summary>
        protected override Element UnderContent(Vector2 point)
        {
            return _clear?.Under(point);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var height = LineOf(g);
            var at = new Vector2(ContentBounds.X + Shift(g), ContentBounds.Y);

            if (HasSelection)
            {
                var from = WidthTo(g, SelectionStart);
                var to = WidthTo(g, SelectionEnd);
                g.DrawSprite(Solid, new RectangleF(at.X + from, at.Y, to - from, height), SelectionColor, 0f);
            }

            if (_text.Length > 0)
                g.DrawText(Font, Written, at, Scale, Color);
            else if (!string.IsNullOrEmpty(Hint))
                g.DrawText(Font, Hint, at, Scale, HintColor);

            // After the text, so the caret shows inside a selection.
            if (IsEditing && CaretVisible)
                g.DrawSprite(Solid, new RectangleF(at.X + WidthTo(g, Caret), at.Y, 1.5f, height), CaretColor, 0f);

            if (Wrong && WrongEdge.A > 0)
            {
                var box = ContentBounds;
                g.DrawSprite(Solid, new RectangleF(box.X, box.Y + box.Height - Rule, box.Width, Rule),
                    WrongEdge, 0f);
            }

            _clear?.Draw(g);
        }

        /// <summary>How far in the words start.</summary>
        float Shift(IGraphics g)
        {
            if (!Trails)
                return 0f;

            var room = ContentBounds.Width - WidthTo(g, _text.Length) - CaretRoom;
            return room > 0f ? room : 0f;
        }

        /// <summary>The width of the first <paramref name="count" /> characters.</summary>
        float WidthTo(IGraphics g, int count)
        {
            if (count <= 0)
                return 0f;

            _run.Clear();
            for (var i = 0; i < count && i < _text.Length; i++)
                _run.Append(_text[i]);

            return g.MeasureText(Font, _run, Scale).X;
        }
    }
}