using System;
using System.Collections.Generic;
using System.Text;
using Lumel.Drawing;
using Lumel.Input;
using VRage.Game;
using VRage.Input;
using VRageMath;

namespace Lumel.Text
{
    /// <summary>One line of editable text, with a caret and a selection, edited from the keyboard and the pointer.</summary>
    /// <remarks>
    ///     The position of every boundary between characters is measured once each time the text or its font
    ///     changes, and the caret, the selection and the pointer read those, so moving the caret or selecting
    ///     measures nothing.
    /// </remarks>
    public class TextBox : Element, IPadded, ITextual
    {
        /// <summary>How wide the caret is drawn.</summary>
        protected const float CaretWidth = 2f;

        Thickness _padding;

        readonly List<float> _edges = new List<float>();

        readonly StringBuilder _run = new StringBuilder(64);

        readonly StringBuilder _text = new StringBuilder(64);

        string _before;
        int _beforeCaret;
        int _beforeAnchor;

        int _anchor;
        int _caret;

        bool _dragging;

        bool _edgesStale = true;
        string _font = MyFontEnum.White;
        float _fontSize = 24f;

        float _scroll;

        float _lead;

        string _written;

        /// <summary>An empty box in white, 24 units tall, that takes the keyboard when pressed.</summary>
        public TextBox()
        {
            Focusable = true;
            Color = Color.White;
            SelectionColor = new Color(40, 90, 150);
            CaretColor = Color.White;
            HintColor = Color.Gray;
            CaretVisible = true;
        }

        /// <summary>Raised when the text is changed by typing, cutting, pasting or deleting, and not when <see cref="Text" /> is set.</summary>
        public event Action Edited;

        /// <summary>Which face the text is drawn in.</summary>
        public string Font
        {
            get { return _font; }
            set
            {
                if (_font == value)
                    return;

                _font = value;
                Remeasure();
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
                Remeasure();
            }
        }

        /// <summary>The color the text is drawn in.</summary>
        public Color Color { get; set; }

        /// <summary>The color painted behind the selected run.</summary>
        public Color SelectionColor { get; set; }

        /// <summary>The color of the caret.</summary>
        public Color CaretColor { get; set; }

        /// <summary>What to show while the box is empty.</summary>
        public string Hint { get; set; }

        /// <summary>The color <see cref="Hint" /> is drawn in.</summary>
        public Color HintColor { get; set; }

        /// <summary>Whether the caret is drawn while the box holds the keyboard; blinking it is the host's.</summary>
        public bool CaretVisible { get; set; }

        /// <summary>What Enter carries out while the box holds the keyboard, or null for nothing.</summary>
        public ICommand Submit { get; set; }

        /// <summary>Whether the box may hold what is typed, or null for anything the kind of box takes.</summary>
        /// <remarks>Asked after every edit made by typing, cutting, pasting or deleting; one it refuses is undone as it is made.</remarks>
        public Func<string, bool> Accepts { get; set; }

        /// <summary>What Escape carries out while the box holds the keyboard, or null to let the keyboard go.</summary>
        public ICommand Cancel { get; set; }

        /// <summary>What is in the box; setting it replaces the lot and keeps the caret and selection inside it.</summary>
        public string Text
        {
            get { return _written ?? (_written = _text.ToString()); }
            set
            {
                _text.Clear();
                if (!string.IsNullOrEmpty(value))
                    _text.Append(value);

                Rewritten();
                Place(Math.Min(_caret, _text.Length), Math.Min(_anchor, _text.Length));
            }
        }

        /// <summary>How many characters are in the box.</summary>
        public int Length => _text.Length;

        /// <summary>Where the caret sits, between 0 and <see cref="Length" />.</summary>
        public int Caret => _caret;

        /// <summary>Whether a run is selected.</summary>
        public bool HasSelection => _caret != _anchor;

        int SelectionStart => Math.Min(_caret, _anchor);

        int SelectionEnd => Math.Max(_caret, _anchor);

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <summary>Whether what is in the box sits against its right-hand end rather than its left.</summary>
        /// <remarks>Said by the kind of box rather than set on one, so a box that holds a number cannot be left without it.</remarks>
        protected virtual bool Trails => false;

        /// <summary>Selects everything in the box, with the caret at the end.</summary>
        public void SelectAll()
        {
            Place(_text.Length, 0);
        }

        /// <summary>The selected run, or empty when there is none.</summary>
        public string SelectedText()
        {
            return HasSelection ? _text.ToString(SelectionStart, SelectionEnd - SelectionStart) : string.Empty;
        }

        /// <summary>Moves the caret to <paramref name="index" />, extending the selection when <paramref name="select" /> is set.</summary>
        public void MoveCaret(int index, bool select)
        {
            index = Math.Max(0, Math.Min(index, _text.Length));
            Place(index, select ? _anchor : index);
        }

        /// <summary>Types <paramref name="words" /> at the caret, replacing any selection.</summary>
        public void Insert(string words)
        {
            if (string.IsNullOrEmpty(words))
                return;

            Hold();
            DeleteSelection();
            _text.Insert(_caret, words);
            Rewritten();
            Place(_caret + words.Length, _caret + words.Length);
            OnEdited();
        }

        /// <summary>Deletes the selection, or else the character before the caret.</summary>
        public void Backspace()
        {
            Hold();
            if (DeleteSelection())
            {
                OnEdited();
                return;
            }

            if (_caret == 0)
                return;

            _text.Remove(_caret - 1, 1);
            Rewritten();
            Place(_caret - 1, _caret - 1);
            OnEdited();
        }

        /// <summary>Deletes the selection, or else the character after the caret.</summary>
        public void DeleteForward()
        {
            Hold();
            if (DeleteSelection())
            {
                OnEdited();
                return;
            }

            if (_caret >= _text.Length)
                return;

            _text.Remove(_caret, 1);
            Rewritten();
            OnEdited();
        }

        /// <inheritdoc />
        public override void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            var index = IndexAt(point.X);

            if (presses >= 3)
            {
                SelectAll();
                return;
            }

            if (presses == 2)
            {
                SelectWord(index);
                return;
            }

            _dragging = true;
            MoveCaret(index, false);
        }

        /// <inheritdoc />
        public override void OnPointerMove(Vector2 point)
        {
            if (_dragging)
                MoveCaret(IndexAt(point.X), true);
        }

        /// <inheritdoc />
        public override void OnPointerUp(Vector2 point)
        {
            _dragging = false;
        }

        /// <inheritdoc />
        public override bool OnKey(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            if (ctrl)
                return OnShortcut(input, key);

            switch (key)
            {
                case MyKeys.Left:
                    MoveCaret(!shift && HasSelection ? SelectionStart : _caret - 1, shift);
                    return true;
                case MyKeys.Right:
                    MoveCaret(!shift && HasSelection ? SelectionEnd : _caret + 1, shift);
                    return true;
                case MyKeys.Home:
                    MoveCaret(0, shift);
                    return true;
                case MyKeys.End:
                    MoveCaret(_text.Length, shift);
                    return true;
                case MyKeys.Back:
                    Backspace();
                    return true;
                case MyKeys.Delete:
                    DeleteForward();
                    return true;
                case MyKeys.Enter:
                    if (Submit != null && Submit.CanExecute)
                        Submit.Execute();
                    return true;
                case MyKeys.Escape:
                    if (Cancel == null || !Cancel.CanExecute)
                        return false;

                    Cancel.Execute();
                    return true;
                default:
                    return false;
            }
        }

        /// <inheritdoc />
        public override void OnText(char c)
        {
            if (c < ' ' || c == (char)127)
                return;

            Hold();
            DeleteSelection();
            _text.Insert(_caret, c);
            Rewritten();
            Place(_caret + 1, _caret + 1);
            OnEdited();
        }

        /// <inheritdoc />
        /// <remarks>Always: text longer than the box moves along inside it.</remarks>
        protected override bool Clips => true;

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

        /// <summary>Measures where every boundary between characters falls, when the text or its font has changed since.</summary>
        /// <remarks>
        ///     One measure per character, once per change; nothing else about the box measures. The box is never
        ///     narrower than it is tall, so an empty one is still somewhere to type.
        /// </remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_edgesStale)
            {
                MeasureEdges(g);
                _edgesStale = false;
            }

            var width = _edges[_edges.Count - 1] + CaretWidth;
            return new Vector2(width > _fontSize ? width : _fontSize, _fontSize);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            KeepCaretInView(bounds.Width);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var room = ContentBounds;
            var at = new Vector2(room.X - _scroll + _lead, room.Y);

            if (HasSelection && IsFocused)
            {
                var from = _edges[SelectionStart];
                g.Fill(new RectangleF(at.X + from, at.Y, _edges[SelectionEnd] - from, _fontSize), SelectionColor);
            }

            if (_text.Length > 0)
                g.DrawText(_font, Text, at, _fontSize, Color);
            else if (!string.IsNullOrEmpty(Hint))
                g.DrawText(_font, Hint, Trails ? new Vector2(room.X + room.Width, room.Y) : at, _fontSize, HintColor,
                    Trails ? TextAlign.Right : TextAlign.Left);

            // After the text, so the caret shows inside a selection.
            if (IsFocused && CaretVisible)
                g.Fill(new RectangleF(at.X + _edges[_caret], at.Y, CaretWidth, _fontSize), CaretColor);
        }

        /// <summary>Whether the box may hold <paramref name="text" />: whatever <see cref="Accepts" /> takes.</summary>
        /// <remarks>Asked after every edit made by typing, cutting, pasting or deleting; one it refuses is undone as it is made.</remarks>
        protected virtual bool Takes(string text)
        {
            return Accepts == null || Accepts(text);
        }

        bool OnShortcut(InputRoot input, MyKeys key)
        {
            switch (key)
            {
                case MyKeys.A:
                    SelectAll();
                    return true;
                case MyKeys.C:
                    if (HasSelection)
                        input.Clipboard.Set(SelectedText());
                    return true;
                case MyKeys.X:
                    if (HasSelection)
                    {
                        input.Clipboard.Set(SelectedText());
                        Backspace();
                    }

                    return true;
                case MyKeys.V:
                    Insert(input.Clipboard.Get());
                    return true;
                default:
                    return false;
            }
        }

        void MeasureEdges(IGraphics g)
        {
            _edges.Clear();
            _edges.Add(0f);
            _run.Clear();

            for (var i = 0; i < _text.Length; i++)
            {
                _run.Append(_text[i]);
                _edges.Add(g.MeasureText(_font, _run, _fontSize).X);
            }
        }

        /// <summary>The boundary between characters nearest <paramref name="x" />.</summary>
        int IndexAt(float x)
        {
            var local = x - ContentBounds.X + _scroll - _lead;
            var best = 0;

            for (var i = 1; i < _edges.Count; i++)
            {
                if (Math.Abs(_edges[i] - local) < Math.Abs(_edges[best] - local))
                    best = i;
            }

            return best;
        }

        /// <summary>Selects the run of letters and digits at <paramref name="index" />, or the one character there that is neither.</summary>
        void SelectWord(int index)
        {
            var start = index;
            var end = index;

            while (start > 0 && char.IsLetterOrDigit(_text[start - 1]))
                start--;

            while (end < _text.Length && char.IsLetterOrDigit(_text[end]))
                end++;

            if (start == end && end < _text.Length)
                end++;

            Place(end, start);
        }

        bool DeleteSelection()
        {
            if (!HasSelection)
                return false;

            var start = SelectionStart;
            _text.Remove(start, SelectionEnd - start);
            Rewritten();
            Place(start, start);
            return true;
        }

        /// <summary>Puts the caret and the other end of the selection where they now belong.</summary>
        /// <remarks>Arranges again rather than measuring, since only how far the text is moved can change.</remarks>
        void Place(int caret, int anchor)
        {
            if (_caret == caret && _anchor == anchor)
                return;

            _caret = caret;
            _anchor = anchor;
            InvalidateArrange();
        }

        void KeepCaretInView(float width)
        {
            if (_edgesStale || _edges.Count == 0)
                return;

            var caretAt = _edges[_caret];
            var end = _edges[_edges.Count - 1] + CaretWidth;

            if (caretAt - _scroll > width - CaretWidth)
                _scroll = caretAt - width + CaretWidth;

            if (caretAt < _scroll)
                _scroll = caretAt;

            if (end - _scroll < width)
                _scroll = Math.Max(0f, end - width);

            _lead = Trails && end < width ? width - end : 0f;
        }

        void Rewritten()
        {
            _written = null;
            Remeasure();
        }

        void Remeasure()
        {
            _edgesStale = true;
            InvalidateMeasure();
        }

        /// <summary>Keeps what is in the box and where the caret is, for an edit refused to be undone to.</summary>
        /// <remarks>On every edit made by hand, which is as often as somebody types.</remarks>
        void Hold()
        {
            _before = _text.ToString();
            _beforeCaret = _caret;
            _beforeAnchor = _anchor;
        }

        void OnEdited()
        {
            if (!Takes(_text.ToString()))
            {
                _text.Clear();
                _text.Append(_before);
                Rewritten();
                Place(_beforeCaret, _beforeAnchor);
                return;
            }

            var edited = Edited;
            if (edited != null)
                edited();
        }
    }
}
