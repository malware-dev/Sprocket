using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>One line of editable text, with a caret and a selection.</summary>
    /// <remarks>
    ///     It knows nothing about keyboards, sessions or the network. Every edit is a method somebody else
    ///     calls, so the same box can be driven by a key, by a script, or by something that is not a keyboard
    ///     at all. Whatever owns the screen decides which.
    /// </remarks>
    public class TextBox : Element
    {
        /// <summary>The room held for the way to empty it, where there is one.</summary>
        /// <remarks>
        ///     A fixed number rather than the glyph's own width, so what is typed does not shift sideways
        ///     because a symbol font measured a hair wider than the last one did. The same number the
        ///     chips in a sentence hold for the same mark.
        /// </remarks>
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
        ///     What to show while the box is empty, in place of the words that are not there yet.
        /// </summary>
        /// <remarks>
        ///     Says what the box is for without spending a line on a caption above it, and without an example
        ///     specific enough to read as a requirement. Drawn in its own colour so it cannot be mistaken for
        ///     something somebody typed.
        /// </remarks>
        public string Hint { get; set; }

        public Color HintColor { get; set; }

        /// <summary>
        ///     Whether the caret is drawn this frame. Blinking is somebody else's business, because a widget with
        ///     its own clock would need to know what a tick is, and this one deliberately does not.
        /// </summary>
        public bool CaretVisible { get; set; }

        /// <summary>Whether an edit session currently owns this box. Set by whatever holds the focus.</summary>
        public bool IsEditing { get; internal set; }

        /// <summary>
        ///     What is typed, as a string, made once per change rather than once per frame.
        /// </summary>
        /// <remarks>
        ///     The text is kept in a builder so that typing a letter does not rebuild it, and drawing
        ///     needs a string - so asking the builder on the draw path allocated one every frame a box
        ///     was on screen, whether or not anything had been typed. Cleared wherever the builder is
        ///     written to, which is the only way to know.
        /// </remarks>
        string Written => _written ?? (_written = _text.ToString());

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
        public int Caret { get; private set; }

        public bool HasSelection => Caret != _anchor;

        int SelectionStart => Caret < _anchor ? Caret : _anchor;

        int SelectionEnd => Caret < _anchor ? _anchor : Caret;

        /// <summary>
        ///     The way to empty this box, drawn inside it at the right, or nothing.
        /// </summary>
        /// <remarks>
        ///     Inside rather than beside, because what it empties is this box: outside, it is a control
        ///     sitting in a row with one, which is a different claim. It is also why the room for it is
        ///     held from the moment there is one to hold it for, whether or not it is showing - a box
        ///     that grew a margin the instant somebody typed would shove itself sideways under the caret.
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
        /// <remarks>
        ///     Set by whoever knows the rule; this only wears it. Drawn as a line under the words rather
        ///     than a border round them, which a box already has and which would sit against the first
        ///     letter with no room to give it.
        /// </remarks>
        public bool Wrong { get; set; }

        /// <summary>The line under the words while <see cref="Wrong" />.</summary>
        public Color WrongEdge { get; set; }

        /// <summary>Whether what is in the box sits against its right-hand end rather than its left.</summary>
        /// <remarks>
        ///     For numbers, which are read from the right: a column of them lines up on its units, and a
        ///     value typed into a box the width of its largest grows the way a number grows.
        /// </remarks>
        public bool Trails { get; set; }

        public void InsertChar(char c)
        {
            InvalidateMeasure();
            DeleteSelection();
            _text.Insert(Caret, c);
            _written = null;
            Caret++;
            _anchor = Caret;
        }

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

        public void MoveCaretLeft(bool select)
        {
            MoveCaretTo(Caret - 1, select);
        }

        public void MoveCaretRight(bool select)
        {
            MoveCaretTo(Caret + 1, select);
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

            // Moving without holding shift collapses the selection onto wherever the caret lands, which is
            // what every text field anywhere does and therefore what nobody has to be taught.
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

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var height = LineOf(g);

            _clear?.Measure(g, new Vector2(Clearing, height));

            if (_text.Length == 0)
                return new Vector2(0f, height);

            var size = g.MeasureText(Font, _text, Scale);
            return new Vector2(size.X, height > size.Y ? height : size.Y);
        }

        /// <summary>
        ///     The line height, kept between frames. Drawing must ask the font nothing it can remember.
        /// </summary>
        /// <remarks>
        ///     A lookup rather than a measurement, so it is cheap - but it is cheap sixty times a second for
        ///     every one of these on screen, to be told a number that only changes when the font or the scale
        ///     does. Draw is the one path invalidation cannot help, because it always runs.
        /// </remarks>
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

        protected override void ArrangeContent(RectangleF bounds)
        {
            if (_clear == null)
                return;

            // In the room the padding held for it: against the right edge, on the middle of the line
            // the words sit on.
            var wanted = _clear.DesiredSize;
            var x = Bounds.X + Bounds.Width - Padding.Left * 0.5f - wanted.X;
            var y = bounds.Y + (bounds.Height - wanted.Y) * 0.5f;

            _clear.Arrange(new RectangleF(x, y, wanted.X, wanted.Y));
        }

        /// <summary>The cross answers before the box it sits in, so it can be pressed at all.</summary>
        protected override Element UnderContent(Vector2 point)
        {
            return _clear?.Under(point);
        }

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

            // Drawn over the text rather than under it, so it stays visible inside a selection.
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

        /// <summary>How far in the words start, which is nothing until they are made to trail.</summary>
        /// <remarks>
        ///     Never negative: what is too long for the box starts at its left-hand end and runs past the
        ///     right, which is what a box that clips its content does.
        /// </remarks>
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