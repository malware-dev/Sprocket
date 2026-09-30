using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Viewing;
using VRage.Game;
using VRage.Input;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>A long piece of text, line by line, in a window that moves both ways, edited or only read.</summary>
    /// <remarks>
    /// It takes the pointer itself: a press on a thumb drags that thumb and a press on a track puts the thumb there first.
    /// Read only, a press on the text drags the text about; edited, it places the caret and a drag selects. How far there
    /// is to go sideways is the widest line seen so far, so the sideways thumb settles as the text is explored.
    /// <para>
    /// Nothing is measured to draw a frame beyond the lines coming into view. Where the caret, the other end of the
    /// selection and a pressed line fall between characters is measured in the measure pass, for those lines only, so a
    /// press or a move from one line to another is settled there rather than where it happens.
    /// </para>
    /// </remarks>
    public class EditBox : Element, IPadded, IScrolls
    {
        const float CaretWidth = 2f;

        /// <summary>How many edits can be taken back.</summary>
        const int Remembered = 200;

        static readonly string[] Endings = { "\r\n", "\n", "\r" };

        readonly Rail _across = new Rail();
        readonly Rail _down = new Rail();
        readonly List<string> _lines = new List<string>();
        readonly List<float> _widths = new List<float>();
        readonly List<string> _numbers = new List<string>();
        readonly StringBuilder _scratch = new StringBuilder(128);
        readonly Dictionary<int, List<float>> _edges = new Dictionary<int, List<float>>();
        readonly List<List<float>> _spareEdges = new List<List<float>>();
        readonly List<int> _dropped = new List<int>();
        readonly List<Change> _undo = new List<Change>();
        readonly List<Change> _redo = new List<Change>();

        Thickness _padding;
        float _barWidth;
        float _digit;
        float _digitAt = -1f;
        string _digitIn;
        string _font;
        float _fontSize;
        bool _numbered;
        bool _readOnly;
        float _gutter;
        float _tall;
        float _widest;
        int _numbersFrom = int.MaxValue;
        int _numbersTo = int.MinValue;
        int _shownFrom = int.MaxValue;
        int _shownTo = int.MinValue;
        string _text = string.Empty;
        Held _held;
        Vector2 _from;
        Spot _caret;
        Spot _anchor;
        float _goal = -1f;
        bool _follow;
        Asked _asked;
        Vector2 _askedAt;
        int _askedLine;
        bool _askedSelects;

        /// <summary>An empty box in white, with lines 24 units tall, that takes the keyboard when pressed.</summary>
        public EditBox()
        {
            _font = MyFontEnum.White;
            _fontSize = 24f;
            _lines.Add(string.Empty);
            _widths.Add(0f);
            Color = Color.White;
            SelectionColor = new Color(40, 90, 150);
            CaretColor = Color.White;
            HintColor = Color.Gray;
            CaretVisible = true;
            Focusable = true;
        }

        /// <summary>Raised when the text is changed by typing, cutting, pasting, deleting, undoing or redoing, and not when <see cref="Text" /> is set.</summary>
        public event Action Edited;

        /// <summary>The whole of it; setting it replaces the lot, puts the caret at the start, and forgets what could be undone.</summary>
        public string Text
        {
            get { return _text ?? (_text = string.Join("\n", _lines)); }
            set
            {
                var incoming = value ?? string.Empty;
                if (_text == incoming)
                    return;

                _text = incoming;
                _lines.Clear();
                _widths.Clear();
                _lines.AddRange(incoming.Split(Endings, StringSplitOptions.None));
                for (var i = 0; i < _lines.Count; i++)
                    _widths.Add(-1f);

                _undo.Clear();
                _redo.Clear();
                _caret = new Spot(0, 0);
                _anchor = _caret;
                _goal = -1f;
                Forget();
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
                Forget();
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
                Forget();
                InvalidateMeasure();
            }
        }

        /// <summary>The color the text is drawn in.</summary>
        public Color Color { get; set; }

        /// <summary>The color painted behind the selected run.</summary>
        public Color SelectionColor { get; set; }

        /// <summary>The color of the caret.</summary>
        public Color CaretColor { get; set; }

        /// <summary>Whether the caret is drawn while the box holds the keyboard; blinking it is the host's.</summary>
        public bool CaretVisible { get; set; }

        /// <summary>What to show while the box is empty.</summary>
        public string Hint { get; set; }

        /// <summary>The color <see cref="Hint" /> is drawn in.</summary>
        public Color HintColor { get; set; }

        /// <summary>What Escape carries out while the box holds the keyboard, or null to let the keyboard go.</summary>
        public ICommand Cancel { get; set; }

        /// <summary>Whether it is only read: a press on the text drags it about, and nothing can be typed.</summary>
        public bool IsReadOnly
        {
            get { return _readOnly; }
            set
            {
                _readOnly = value;
                Focusable = !value;
            }
        }

        /// <summary>Whether each line is numbered down a gutter on the left.</summary>
        public bool Numbered
        {
            get { return _numbered; }
            set
            {
                if (_numbered == value)
                    return;

                _numbered = value;
                InvalidateMeasure();
            }
        }

        /// <summary>The color the numbers are drawn in.</summary>
        public Color NumberColor { get; set; }

        /// <summary>How thick the bars are, or nought for none.</summary>
        public float BarWidth
        {
            get { return _barWidth; }
            set
            {
                if (_barWidth == value)
                    return;

                _barWidth = value;
                InvalidateArrange();
            }
        }

        /// <summary>The thumbs' color.</summary>
        public Color Thumb { get; set; }

        /// <summary>A thumb's color while it is held.</summary>
        public Color HeldThumb { get; set; }

        /// <summary>How short a thumb may get.</summary>
        public float LeastThumb
        {
            get { return _down.MinThumb; }
            set
            {
                _down.MinThumb = value;
                _across.MinThumb = value;
            }
        }

        /// <inheritdoc />
        public Thickness Padding
        {
            get { return _padding; }
            set
            {
                if (_padding.Equals(value))
                    return;

                _padding = value;
                InvalidateMeasure();
            }
        }

        /// <summary>How many lines it holds.</summary>
        public int Lines => _lines.Count;

        /// <summary>How tall the whole of it stands, once it has been measured.</summary>
        public float Extent => _down.Extent;

        /// <summary>How wide the widest line shown so far is.</summary>
        public float Widest => _across.Extent;

        /// <summary>Whether the text itself is being dragged about.</summary>
        public bool IsPanning => _held == Held.Text;

        /// <summary>The line the caret is on, counted from zero.</summary>
        public int CaretLine => _caret.Line;

        /// <summary>How far into its line the caret is, in characters.</summary>
        public int CaretColumn => _caret.Column;

        /// <summary>Whether a run is selected.</summary>
        public bool HasSelection => !_caret.Equals(_anchor);

        /// <summary>Whether there is an edit to take back.</summary>
        public bool CanUndo => _undo.Count > 0;

        /// <summary>Whether there is an edit taken back to make again.</summary>
        public bool CanRedo => _redo.Count > 0;

        /// <summary>How far down the text has been moved, in UI units.</summary>
        public float Scrolled
        {
            get { return _down.Offset; }
            set
            {
                if (_down.Offset == value)
                    return;

                _down.Offset = value;
                InvalidateArrange();
            }
        }

        /// <summary>How far across the text has been moved, in UI units.</summary>
        public float Shifted
        {
            get { return _across.Offset; }
            set
            {
                if (_across.Offset == value)
                    return;

                _across.Offset = value;
                InvalidateArrange();
            }
        }

        /// <inheritdoc />
        public bool CanScroll => _down.CanMove;

        /// <summary>Whether there is anything out of sight to either side.</summary>
        public bool CanShift => _across.CanMove;

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        /// <inheritdoc />
        protected override bool Clips => true;

        /// <summary>The text's own room: what is left once the bars have had their share.</summary>
        RectangleF View
        {
            get
            {
                var room = ContentBounds;
                return new RectangleF(room.X, room.Y, Rail.Room(room.Width, _barWidth), Rail.Room(room.Height, _barWidth));
            }
        }

        /// <summary>Where the words start across, once the numbers have had their share.</summary>
        float WordsX => View.X + _gutter;

        Spot SelectionStart => _caret.Before(_anchor) ? _caret : _anchor;

        Spot SelectionEnd => _caret.Before(_anchor) ? _anchor : _caret;

        /// <summary>Whether the text is nothing at all: one line, and that one empty.</summary>
        bool IsEmpty => _lines.Count == 1 && _lines[0].Length == 0;

        /// <summary>Where the text ends.</summary>
        Spot End => new Spot(_lines.Count - 1, _lines[_lines.Count - 1].Length);

        /// <summary>How many lines the window shows at once, and at least one.</summary>
        int Page => Math.Max(1, _fontSize > 0f ? (int)(_down.Viewport / _fontSize) : 1);

        /// <summary>How far across the caret stands in its line, or zero before its line has been measured.</summary>
        float CaretX => XOf(_caret);

        /// <inheritdoc />
        public void ScrollBy(float amount)
        {
            Scrolled = _down.Moved(amount);
        }

        /// <summary>Moves across by <paramref name="amount" />, staying inside the text.</summary>
        public void ShiftBy(float amount)
        {
            Shifted = _across.Moved(amount);
        }

        /// <summary>Selects everything, with the caret at the end.</summary>
        public void SelectAll()
        {
            Place(End, new Spot(0, 0));
        }

        /// <summary>The selected run, lines joined by line breaks, or empty when there is none.</summary>
        public string SelectedText()
        {
            return HasSelection ? Slice(SelectionStart, SelectionEnd) : string.Empty;
        }

        /// <summary>Selects the whole of line <paramref name="line" />, counted from 0 and held inside the text, and brings it into view.</summary>
        /// <remarks>The caret goes to the start of the line, so a long line is shown from its beginning.</remarks>
        public void SelectLine(int line)
        {
            var start = Within(new Spot(line, 0));
            Place(start, new Spot(start.Line, _lines[start.Line].Length));
            Follow();
        }

        /// <summary>Moves the caret to a line and a column, held inside the text, extending the selection when <paramref name="select" /> is set.</summary>
        public void MoveCaret(int line, int column, bool select)
        {
            var at = Within(new Spot(line, column));
            Place(at, select ? _anchor : at);
        }

        /// <summary>Types <paramref name="words" /> at the caret, replacing any selection; a line break in them starts a new line.</summary>
        public void Insert(string words)
        {
            if (_readOnly || string.IsNullOrEmpty(words))
                return;

            Edit(SelectionStart, SelectionEnd, Plain(words), false);
        }

        /// <summary>Deletes the selection, or else the character before the caret, joining lines at the start of one.</summary>
        public void Backspace()
        {
            if (_readOnly)
                return;

            if (HasSelection)
            {
                Edit(SelectionStart, SelectionEnd, string.Empty, false);
                return;
            }

            if (_caret.Line == 0 && _caret.Column == 0)
                return;

            Edit(Step(_caret, -1), _caret, string.Empty, false);
        }

        /// <summary>Deletes the selection, or else the character after the caret, joining lines at the end of one.</summary>
        public void DeleteForward()
        {
            if (_readOnly)
                return;

            if (HasSelection)
            {
                Edit(SelectionStart, SelectionEnd, string.Empty, false);
                return;
            }

            if (_caret.Equals(End))
                return;

            Edit(_caret, Step(_caret, 1), string.Empty, false);
        }

        /// <summary>Takes back the last edit, putting the caret where it was.</summary>
        public void Undo()
        {
            if (_undo.Count == 0)
                return;

            var change = _undo[_undo.Count - 1];
            _undo.RemoveAt(_undo.Count - 1);
            Replace(change.At, Reach(change.At, change.Inserted), change.Removed);
            _redo.Add(change);
            Place(change.CaretBefore, change.AnchorBefore);
            Follow();
            OnEdited();
        }

        /// <summary>Makes the last edit taken back again.</summary>
        public void Redo()
        {
            if (_redo.Count == 0)
                return;

            var change = _redo[_redo.Count - 1];
            _redo.RemoveAt(_redo.Count - 1);
            var end = Replace(change.At, Reach(change.At, change.Removed), change.Inserted);
            _undo.Add(change);
            Place(end, end);
            Follow();
            OnEdited();
        }

        /// <inheritdoc />
        public override void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            _from = point;

            if (Grab(Held.Down, point))
            {
                _held = Held.Down;
                return;
            }

            if (Grab(Held.Across, point))
            {
                _held = Held.Across;
                return;
            }

            if (_readOnly)
            {
                _held = Inside(View, point) && (_down.CanMove || _across.CanMove) ? Held.Text : Held.Nothing;
                return;
            }

            _held = Held.Selecting;
            Ask(presses >= 3 ? Asked.Line : presses == 2 ? Asked.Word : Asked.Point, point, shift);
        }

        /// <inheritdoc />
        public override void OnPointerMove(Vector2 point)
        {
            if (_held == Held.Down)
                Scrolled = _down.Dragged(point.Y - _from.Y);
            else if (_held == Held.Across)
                Shifted = _across.Dragged(point.X - _from.X);
            else if (_held == Held.Text)
            {
                ScrollBy(_from.Y - point.Y);
                ShiftBy(_from.X - point.X);
            }
            else if (_held == Held.Selecting)
                Ask(Asked.Point, point, true);

            _from = point;
        }

        /// <inheritdoc />
        public override void OnPointerUp(Vector2 point)
        {
            _held = Held.Nothing;
        }

        /// <inheritdoc />
        public override bool OnKey(InputRoot input, MyKeys key, bool shift, bool ctrl)
        {
            if (_readOnly)
                return false;

            if (ctrl && OnShortcut(input, key, shift))
                return true;

            switch (key)
            {
                case MyKeys.Left:
                    Across(ctrl ? Word(_caret, -1) : !shift && HasSelection ? SelectionStart : Step(_caret, -1), shift);
                    return true;
                case MyKeys.Right:
                    Across(ctrl ? Word(_caret, 1) : !shift && HasSelection ? SelectionEnd : Step(_caret, 1), shift);
                    return true;
                case MyKeys.Up:
                    Vertical(_caret.Line - 1, shift);
                    return true;
                case MyKeys.Down:
                    Vertical(_caret.Line + 1, shift);
                    return true;
                case MyKeys.PageUp:
                    Vertical(_caret.Line - Page, shift);
                    return true;
                case MyKeys.PageDown:
                    Vertical(_caret.Line + Page, shift);
                    return true;
                case MyKeys.Home:
                    Across(ctrl ? new Spot(0, 0) : new Spot(_caret.Line, 0), shift);
                    return true;
                case MyKeys.End:
                    Across(ctrl ? End : new Spot(_caret.Line, _lines[_caret.Line].Length), shift);
                    return true;
                case MyKeys.Back:
                    Backspace();
                    return true;
                case MyKeys.Delete:
                    DeleteForward();
                    return true;
                case MyKeys.Enter:
                    Insert("\n");
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
            if (_readOnly || c < ' ' || c == (char)127)
                return;

            _scratch.Length = 0;
            _scratch.Append(c);
            Edit(SelectionStart, SelectionEnd, _scratch.ToString(), true);
        }

        /// <summary>Takes all the room it is given.</summary>
        /// <remarks>
        /// Works out how tall the text stands and how wide the numbers are here, because arranging has no font to ask, and
        /// settles where a press or a move to another line lands, measuring the lines the caret and the selection are on.
        /// </remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _tall = _lines.Count * _fontSize;
            _gutter = _numbered ? (Digits(_lines.Count) + 2) * DigitOf(g) : 0f;

            if (!_readOnly)
            {
                Answer(g);
                KeepEdges(g);
            }

            return available;
        }

        /// <summary>Fits both rails to the room and holds each offset to its rail, then brings the caret into view where it moved.</summary>
        /// <remarks>Room for the bars is kept even with nothing to scroll, so the text does not move when it starts to.</remarks>
        protected override void ArrangeContent(RectangleF bounds)
        {
            _down.Extent = _tall;
            _across.Extent = Math.Max(_widest, CaretX + CaretWidth);
            _down.Viewport = Rail.Room(bounds.Height, _barWidth);
            _across.Viewport = Rail.Room(Rail.Room(bounds.Width, _barWidth), _gutter);

            if (_follow)
            {
                _follow = false;
                BringCaretIntoView();
            }

            _down.Offset = _down.Clamp(_down.Offset);
            _across.Offset = _across.Clamp(_across.Offset);
        }

        /// <summary>Only the lines the window is over, with the selection behind them and the caret over them, then the numbers beside them, then the thumbs.</summary>
        protected override void DrawContent(IGraphics g)
        {
            var view = View;
            if (_fontSize > 0f && view.Height > 0f)
            {
                var first = Math.Max(0, (int)(_down.Offset / _fontSize));
                var last = Math.Min(_lines.Count - 1, (int)((_down.Offset + view.Height) / _fontSize));
                var top = view.Y - _down.Offset + first * _fontSize;

                Gutter(g, view, first, last, top);

                var words = new RectangleF(WordsX, view.Y, Rail.Room(view.Width, _gutter), view.Height);
                var x = words.X - _across.Offset;
                var y = top;
                var selecting = HasSelection && IsFocused;
                var start = SelectionStart;
                var end = SelectionEnd;

                g.PushClip(words);

                if (IsEmpty && !string.IsNullOrEmpty(Hint))
                    g.DrawText(_font, Hint, new Vector2(x, y), _fontSize, HintColor);

                for (var i = first; i <= last; i++)
                {
                    var said = _lines[i];
                    if (_widths[i] < 0f || i < _shownFrom || i > _shownTo)
                        Widen(g, i);

                    if (selecting && i >= start.Line && i <= end.Line)
                        Select(g, i, start, end, x, y);

                    if (said.Length > 0)
                        g.DrawText(_font, said, new Vector2(x, y), _fontSize, Color);

                    y += _fontSize;
                }

                // After the text, so the caret shows inside a selection.
                if (IsFocused && CaretVisible && !_readOnly && _caret.Line >= first && _caret.Line <= last)
                {
                    g.Fill(new RectangleF(x + CaretX, top + (_caret.Line - first) * _fontSize, CaretWidth, _fontSize),
                        CaretColor);
                }

                g.PopClip();
                _shownFrom = first;
                _shownTo = last;
            }

            Bar(g, Held.Down);
            Bar(g, Held.Across);
        }

        /// <summary>How many decimal digits a count runs to.</summary>
        static int Digits(int count)
        {
            var digits = 1;
            while (count >= 10)
            {
                count /= 10;
                digits++;
            }

            return digits;
        }

        /// <summary>Whether a point is inside a rectangle that is not any element's bounds.</summary>
        static bool Inside(RectangleF bounds, Vector2 point)
        {
            return point.X >= bounds.X && point.X <= bounds.X + bounds.Width
                                       && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height;
        }

        /// <summary><paramref name="words" /> with every line break spelled as one character, the way lines are joined here.</summary>
        static string Plain(string words)
        {
            return words.IndexOf('\r') < 0 ? words : words.Replace("\r\n", "\n").Replace('\r', '\n');
        }

        /// <summary>Where the end of <paramref name="words" /> lands, written from <paramref name="at" />.</summary>
        static Spot Reach(Spot at, string words)
        {
            var breaks = 0;
            var last = -1;
            for (var i = 0; i < words.Length; i++)
            {
                if (words[i] != '\n')
                    continue;

                breaks++;
                last = i;
            }

            return breaks == 0
                ? new Spot(at.Line, at.Column + words.Length)
                : new Spot(at.Line + breaks, words.Length - last - 1);
        }

        bool OnShortcut(InputRoot input, MyKeys key, bool shift)
        {
            switch (key)
            {
                case MyKeys.A:
                    SelectAll();
                    return true;
                case MyKeys.C:
                    if (HasSelection)
                        input.Clipboard = SelectedText();
                    return true;
                case MyKeys.X:
                    if (HasSelection)
                    {
                        input.Clipboard = SelectedText();
                        Backspace();
                    }

                    return true;
                case MyKeys.V:
                    Insert(input.Clipboard);
                    return true;
                case MyKeys.Z:
                    if (shift)
                        Redo();
                    else
                        Undo();
                    return true;
                case MyKeys.Y:
                    Redo();
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Moves the caret along its lines, forgetting the column Up and Down were keeping.</summary>
        void Across(Spot to, bool select)
        {
            _goal = -1f;
            Place(to, select ? _anchor : to);
            Follow();
        }

        /// <summary>Asks for the caret on <paramref name="line" />, as far across as the caret stood when this run of Up and Down began.</summary>
        void Vertical(int line, bool select)
        {
            if (_goal < 0f)
                _goal = CaretX;

            if (line < 0)
            {
                Place(new Spot(0, 0), select ? _anchor : new Spot(0, 0));
                Follow();
                return;
            }

            if (line >= _lines.Count)
            {
                Place(End, select ? _anchor : End);
                Follow();
                return;
            }

            _askedLine = line;
            _askedAt = new Vector2(_goal, 0f);
            _askedSelects = select;
            _asked = Asked.Across;
            Follow();
            InvalidateMeasure();
        }

        /// <summary>Asks for the caret where the pointer is, settled in the measure pass once its line's characters are known.</summary>
        void Ask(Asked what, Vector2 point, bool select)
        {
            _asked = what;
            _askedAt = point;
            _askedSelects = select;
            _goal = -1f;
            Follow();
            InvalidateMeasure();
        }

        /// <summary>Settles what was asked of the caret, measuring the line it lands on.</summary>
        void Answer(IGraphics g)
        {
            if (_asked == Asked.Nothing)
                return;

            var what = _asked;
            _asked = Asked.Nothing;

            int line;
            float x;
            if (what == Asked.Across)
            {
                line = _askedLine;
                x = _askedAt.X;
            }
            else
            {
                var view = View;
                line = _fontSize > 0f ? (int)Math.Floor((_askedAt.Y - view.Y + _down.Offset) / _fontSize) : 0;
                line = Math.Max(0, Math.Min(_lines.Count - 1, line));
                x = _askedAt.X - WordsX + _across.Offset;
            }

            var at = new Spot(line, ColumnAt(g, line, x));

            if (what == Asked.Word)
            {
                SelectWord(at);
                return;
            }

            if (what == Asked.Line)
            {
                var next = line + 1 < _lines.Count ? new Spot(line + 1, 0) : new Spot(line, _lines[line].Length);
                Settle(next, new Spot(line, 0));
                return;
            }

            Settle(at, _askedSelects ? _anchor : at);
        }

        /// <summary>Puts the caret and the other end of the selection where they belong, from inside the measure pass.</summary>
        /// <remarks>Invalidates nothing: the pass it is called from goes on to measure the lines they are on.</remarks>
        void Settle(Spot caret, Spot anchor)
        {
            _caret = caret;
            _anchor = anchor;
        }

        /// <summary>The boundary between characters on <paramref name="line" /> nearest <paramref name="x" />.</summary>
        int ColumnAt(IGraphics g, int line, float x)
        {
            var edges = EdgesOf(g, line);
            var best = 0;
            for (var i = 1; i < edges.Count; i++)
            {
                if (Math.Abs(edges[i] - x) < Math.Abs(edges[best] - x))
                    best = i;
            }

            return best;
        }

        /// <summary>Selects the run of letters and digits at <paramref name="at" />, or the one character there that is neither.</summary>
        void SelectWord(Spot at)
        {
            var said = _lines[at.Line];
            var start = at.Column;
            var end = at.Column;

            while (start > 0 && char.IsLetterOrDigit(said[start - 1]))
                start--;

            while (end < said.Length && char.IsLetterOrDigit(said[end]))
                end++;

            if (start == end && end < said.Length)
                end++;

            Settle(new Spot(at.Line, end), new Spot(at.Line, start));
        }

        /// <summary>Keeps the character boundaries of the lines the caret and the selection's other end are on, and lets go of the rest.</summary>
        void KeepEdges(IGraphics g)
        {
            EdgesOf(g, _caret.Line);
            EdgesOf(g, _anchor.Line);

            _dropped.Clear();
            foreach (var line in _edges.Keys)
            {
                if (line != _caret.Line && line != _anchor.Line)
                    _dropped.Add(line);
            }

            for (var i = 0; i < _dropped.Count; i++)
            {
                _spareEdges.Add(_edges[_dropped[i]]);
                _edges.Remove(_dropped[i]);
            }
        }

        /// <summary>Where every boundary between characters on <paramref name="line" /> falls, measured when it is first asked for.</summary>
        List<float> EdgesOf(IGraphics g, int line)
        {
            List<float> edges;
            if (_edges.TryGetValue(line, out edges))
                return edges;

            if (_spareEdges.Count > 0)
            {
                edges = _spareEdges[_spareEdges.Count - 1];
                _spareEdges.RemoveAt(_spareEdges.Count - 1);
                edges.Clear();
            }
            else
            {
                edges = new List<float>();
            }

            var said = _lines[line];
            edges.Add(0f);
            _scratch.Length = 0;
            for (var i = 0; i < said.Length; i++)
            {
                _scratch.Append(said[i]);
                edges.Add(g.MeasureText(_font, _scratch, _fontSize).X);
            }

            _edges[line] = edges;
            return edges;
        }

        /// <summary>How far across <paramref name="at" /> stands, or zero where its line has not been measured.</summary>
        float XOf(Spot at)
        {
            List<float> edges;
            return _edges.TryGetValue(at.Line, out edges) && at.Column < edges.Count ? edges[at.Column] : 0f;
        }

        /// <summary>Fills behind the selected part of one line, running on past its end where the selection carries on to the next.</summary>
        void Select(IGraphics g, int line, Spot start, Spot end, float x, float y)
        {
            var from = line == start.Line ? XOf(start) : 0f;
            var to = line == end.Line ? XOf(end) : Math.Max(0f, _widths[line]) + _fontSize * 0.25f;
            if (to > from)
                g.Fill(new RectangleF(x + from, y, to - from, _fontSize), SelectionColor);
        }

        /// <summary>Moves the window just far enough to show the caret.</summary>
        void BringCaretIntoView()
        {
            var top = _caret.Line * _fontSize;
            if (top < _down.Offset)
                _down.Offset = top;
            else if (top + _fontSize > _down.Offset + _down.Viewport)
                _down.Offset = top + _fontSize - _down.Viewport;

            var across = CaretX;
            if (across < _across.Offset)
                _across.Offset = across;
            else if (across + CaretWidth > _across.Offset + _across.Viewport)
                _across.Offset = across + CaretWidth - _across.Viewport;
        }

        /// <summary>Replaces what lies between <paramref name="from" /> and <paramref name="to" /> with <paramref name="words" />, remembered so it can be taken back.</summary>
        /// <remarks>Characters typed one after another are remembered as one edit, so taking them back takes back the word.</remarks>
        void Edit(Spot from, Spot to, string words, bool typed)
        {
            var removed = Slice(from, to);
            if (removed.Length == 0 && words.Length == 0)
                return;

            var last = _undo.Count > 0 ? _undo[_undo.Count - 1] : null;
            if (typed && removed.Length == 0 && last != null && last.Typed && last.Removed.Length == 0
                && Reach(last.At, last.Inserted).Equals(from))
            {
                last.Inserted += words;
            }
            else
            {
                _undo.Add(new Change(from, removed, words, typed, _caret, _anchor));
                if (_undo.Count > Remembered)
                    _undo.RemoveAt(0);
            }

            _redo.Clear();
            var end = Replace(from, to, words);
            _goal = -1f;
            Place(end, end);
            Follow();
            OnEdited();
        }

        /// <summary>Replaces what lies between two places with <paramref name="words" />, and answers where the words end.</summary>
        Spot Replace(Spot from, Spot to, string words)
        {
            var head = _lines[from.Line].Substring(0, from.Column);
            var tail = _lines[to.Line].Substring(to.Column);
            var pieces = words.Split('\n');

            _lines.RemoveRange(from.Line, to.Line - from.Line + 1);
            _widths.RemoveRange(from.Line, to.Line - from.Line + 1);

            for (var i = 0; i < pieces.Length; i++)
            {
                var said = pieces[i];
                if (i == 0)
                    said = head + said;
                if (i == pieces.Length - 1)
                    said += tail;

                _lines.Insert(from.Line + i, said);
                _widths.Insert(from.Line + i, -1f);
            }

            _text = null;
            _edges.Clear();
            _shownFrom = int.MaxValue;
            _shownTo = int.MinValue;
            _numbersFrom = int.MaxValue;
            _numbersTo = int.MinValue;
            InvalidateMeasure();

            return pieces.Length == 1
                ? new Spot(from.Line, from.Column + pieces[0].Length)
                : new Spot(from.Line + pieces.Length - 1, pieces[pieces.Length - 1].Length);
        }

        /// <summary>What lies between two places, lines joined by line breaks.</summary>
        string Slice(Spot from, Spot to)
        {
            if (from.Line == to.Line)
                return _lines[from.Line].Substring(from.Column, to.Column - from.Column);

            _scratch.Length = 0;
            _scratch.Append(_lines[from.Line], from.Column, _lines[from.Line].Length - from.Column);
            for (var i = from.Line + 1; i < to.Line; i++)
                _scratch.Append('\n').Append(_lines[i]);

            _scratch.Append('\n').Append(_lines[to.Line], 0, to.Column);
            return _scratch.ToString();
        }

        /// <summary>One character on from <paramref name="at" />, either way, crossing into the next or the last line at an end.</summary>
        Spot Step(Spot at, int way)
        {
            if (way < 0)
            {
                if (at.Column > 0)
                    return new Spot(at.Line, at.Column - 1);

                return at.Line > 0 ? new Spot(at.Line - 1, _lines[at.Line - 1].Length) : at;
            }

            if (at.Column < _lines[at.Line].Length)
                return new Spot(at.Line, at.Column + 1);

            return at.Line + 1 < _lines.Count ? new Spot(at.Line + 1, 0) : at;
        }

        /// <summary>The start of the next word either way, crossing lines where a line runs out.</summary>
        Spot Word(Spot at, int way)
        {
            var moved = Step(at, way);
            if (moved.Line != at.Line)
                return moved;

            var said = _lines[at.Line];
            var column = moved.Column;
            if (way < 0)
            {
                while (column > 0 && !char.IsLetterOrDigit(said[column]))
                    column--;
                while (column > 0 && char.IsLetterOrDigit(said[column - 1]))
                    column--;
            }
            else
            {
                while (column < said.Length && char.IsLetterOrDigit(said[column - 1]))
                    column++;
                while (column < said.Length && !char.IsLetterOrDigit(said[column]))
                    column++;
            }

            return new Spot(at.Line, column);
        }

        /// <summary><paramref name="at" />, held inside the text.</summary>
        Spot Within(Spot at)
        {
            var line = Math.Max(0, Math.Min(_lines.Count - 1, at.Line));
            return new Spot(line, Math.Max(0, Math.Min(_lines[line].Length, at.Column)));
        }

        /// <summary>Puts the caret and the other end of the selection where they now belong.</summary>
        /// <remarks>Measures again rather than only arranging, since either may now be on a line whose characters are not yet known.</remarks>
        void Place(Spot caret, Spot anchor)
        {
            if (_caret.Equals(caret) && _anchor.Equals(anchor))
                return;

            _caret = caret;
            _anchor = anchor;
            InvalidateMeasure();
        }

        /// <summary>Asks for the window to follow the caret the next time it is arranged.</summary>
        void Follow()
        {
            _follow = true;
            InvalidateArrange();
        }

        void OnEdited()
        {
            var edited = Edited;
            if (edited != null)
                edited();
        }

        /// <summary>The numbers of the lines on show, right against the words they belong to.</summary>
        void Gutter(IGraphics g, RectangleF view, int first, int last, float top)
        {
            if (_gutter <= 0f || NumberColor.A == 0)
                return;

            Number(first, last);

            var edge = view.X + _gutter - _digit;
            var y = top;

            g.PushClip(new RectangleF(view.X, view.Y, _gutter, view.Height));
            for (var i = 0; i < _numbers.Count; i++)
            {
                g.DrawText(_font, _numbers[i], new Vector2(edge, y), _fontSize, NumberColor, Align.Right);
                y += _fontSize;
            }

            g.PopClip();
        }

        /// <summary>Writes the numbers for a band of lines, only when the band has moved.</summary>
        void Number(int first, int last)
        {
            if (_numbersFrom == first && _numbersTo == last)
                return;

            _numbersFrom = first;
            _numbersTo = last;
            _numbers.Clear();

            for (var i = first; i <= last; i++)
                _numbers.Add((i + 1).ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>The widest of the ten digits, asked only when the face or the size has moved.</summary>
        float DigitOf(IGraphics g)
        {
            if (_digitAt == _fontSize && _digitIn == _font)
                return _digit;

            _digitAt = _fontSize;
            _digitIn = _font;
            _digit = 0f;

            for (var d = 0; d < 10; d++)
            {
                _scratch.Length = 0;
                _scratch.Append((char)('0' + d));
                _digit = Math.Max(_digit, g.MeasureText(_font, _scratch, _fontSize).X);
            }

            return _digit;
        }

        /// <summary>Takes hold of one bar where the press is on it, putting the thumb at the press where it landed off the thumb.</summary>
        bool Grab(Held way, Vector2 point)
        {
            if (!HasBar(way) || !Inside(Track(way), point))
                return false;

            if (!Inside(ThumbBounds(way), point))
            {
                var track = Track(way);
                if (way == Held.Across)
                    Shifted = _across.ThumbedTo(point.X, track.X);
                else
                    Scrolled = _down.ThumbedTo(point.Y, track.Y);
            }

            return true;
        }

        void Bar(IGraphics g, Held way)
        {
            if (HasBar(way))
                g.Fill(ThumbBounds(way), _held == way && HeldThumb.A > 0 ? HeldThumb : Thumb);
        }

        bool HasBar(Held way)
        {
            return _barWidth > 0f && Thumb.A > 0 && Of(way).CanMove;
        }

        /// <summary>The whole of a bar, thumb and empty track alike, in the room the text does not get.</summary>
        RectangleF Track(Held way)
        {
            var room = ContentBounds;
            return way == Held.Across
                ? new RectangleF(room.X, room.Y + room.Height - _barWidth, Rail.Room(room.Width, _barWidth), _barWidth)
                : new RectangleF(room.X + room.Width - _barWidth, room.Y, _barWidth, Rail.Room(room.Height, _barWidth));
        }

        RectangleF ThumbBounds(Held way)
        {
            var track = Track(way);
            var rail = Of(way);
            return way == Held.Across
                ? new RectangleF(track.X + rail.ThumbStart, track.Y, rail.ThumbLength, track.Height)
                : new RectangleF(track.X, track.Y + rail.ThumbStart, track.Width, rail.ThumbLength);
        }

        Rail Of(Held way)
        {
            return way == Held.Across ? _across : _down;
        }

        /// <summary>Measures a line coming into view or changed since, and reaches further where it is the longest yet.</summary>
        /// <remarks>
        /// Measures while drawing on purpose: it is asked only about lines that were not on show last frame or have been
        /// edited, and measuring the whole text up front was timed at 11ms for five hundred lines and 221ms for ten thousand,
        /// against a frame of under 17. The reach goes straight onto the rail without invalidating, because only the thumb is
        /// drawn from it, after this.
        /// </remarks>
        void Widen(IGraphics g, int line)
        {
            var said = _lines[line];
            var wide = 0f;
            if (said.Length > 0)
            {
                _scratch.Length = 0;
                _scratch.Append(said);
                wide = g.MeasureText(_font, _scratch, _fontSize).X;
            }

            _widths[line] = wide;
            if (wide <= _widest)
                return;

            _widest = wide;
            _across.Extent = _widest;
        }

        /// <summary>Drops what it learned about text, a face or a size that has since changed.</summary>
        void Forget()
        {
            _widest = 0f;
            _across.Extent = 0f;
            _shownFrom = int.MaxValue;
            _shownTo = int.MinValue;
            _numbersFrom = int.MaxValue;
            _numbersTo = int.MinValue;
            _edges.Clear();
            for (var i = 0; i < _widths.Count; i++)
                _widths[i] = -1f;
        }

        /// <summary>Which of its parts the pointer is holding, where it holds one.</summary>
        enum Held
        {
            Nothing,
            Down,
            Across,
            Text,
            Selecting
        }

        /// <summary>What has been asked of the caret that waits on a line's characters being measured.</summary>
        enum Asked
        {
            Nothing,
            Point,
            Word,
            Line,
            Across
        }

        /// <summary>A place between two characters: a line, and how far into it.</summary>
        struct Spot : IEquatable<Spot>
        {
            public readonly int Line;
            public readonly int Column;

            public Spot(int line, int column)
            {
                Line = line;
                Column = column;
            }

            public bool Before(Spot other)
            {
                return Line < other.Line || (Line == other.Line && Column < other.Column);
            }

            public bool Equals(Spot other)
            {
                return Line == other.Line && Column == other.Column;
            }
        }

        /// <summary>One edit, kept so it can be taken back and made again.</summary>
        sealed class Change
        {
            public readonly Spot At;
            public readonly string Removed;
            public readonly bool Typed;
            public readonly Spot CaretBefore;
            public readonly Spot AnchorBefore;
            public string Inserted;

            public Change(Spot at, string removed, string inserted, bool typed, Spot caretBefore, Spot anchorBefore)
            {
                At = at;
                Removed = removed;
                Inserted = inserted;
                Typed = typed;
                CaretBefore = caretBefore;
                AnchorBefore = anchorBefore;
            }
        }
    }
}
