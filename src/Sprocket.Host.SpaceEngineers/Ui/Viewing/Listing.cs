using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Viewing
{
    /// <summary>A long piece of text, line by line, and a window onto it that moves both ways.</summary>
    /// <remarks>
    /// It takes the pointer itself: a press on a thumb drags that thumb, a press on a track puts the thumb there first, and a
    /// press on the text drags the text about. How far there is to go sideways is the widest line seen so far, so the
    /// sideways thumb settles as the text is explored.
    /// </remarks>
    public class Listing : Element, IPadded, IScrolls
    {
        static readonly string[] Endings = { "\r\n", "\n", "\r" };

        readonly Rail _across = new Rail();
        readonly Rail _down = new Rail();
        readonly List<string> _lines = new List<string>();
        readonly List<string> _numbers = new List<string>();
        readonly StringBuilder _scratch = new StringBuilder(128);

        Thickness _padding;
        float _barWidth;
        float _digit;
        float _digitAt = -1f;
        string _digitIn;
        string _font;
        float _fontSize;
        bool _numbered;
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

        /// <summary>An empty listing in white, with lines 24 units tall.</summary>
        public Listing()
        {
            _font = MyFontEnum.White;
            _fontSize = 24f;
            Color = Color.White;
        }

        /// <summary>The whole of it, split into lines as it is set.</summary>
        public string Text
        {
            get { return _text; }
            set
            {
                var incoming = value ?? string.Empty;
                if (_text == incoming)
                    return;

                _text = incoming;
                _lines.Clear();
                if (incoming.Length > 0)
                    _lines.AddRange(incoming.Split(Endings, StringSplitOptions.None));

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

        /// <inheritdoc />
        public override void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            _from = point;
            _held = Grab(Held.Down, point) ? Held.Down
                : Grab(Held.Across, point) ? Held.Across
                : Inside(View, point) && (_down.CanMove || _across.CanMove) ? Held.Text
                : Held.Nothing;
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

            _from = point;
        }

        /// <inheritdoc />
        public override void OnPointerUp(Vector2 point)
        {
            _held = Held.Nothing;
        }

        /// <summary>Takes all the room it is given.</summary>
        /// <remarks>Works out how tall the text stands and how wide the numbers are here, because arranging has no font to ask.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _tall = _lines.Count > 0 ? _lines.Count * _fontSize : _fontSize;
            _gutter = _numbered && _lines.Count > 0 ? (Digits(_lines.Count) + 2) * DigitOf(g) : 0f;
            return available;
        }

        /// <summary>Fits both rails to the room and holds each offset to its rail.</summary>
        /// <remarks>Room for the bars is kept even with nothing to scroll, so the text does not move when it starts to.</remarks>
        protected override void ArrangeContent(RectangleF bounds)
        {
            _down.Extent = _tall;
            _across.Extent = _widest;
            _down.Viewport = Rail.Room(bounds.Height, _barWidth);
            _across.Viewport = Rail.Room(Rail.Room(bounds.Width, _barWidth), _gutter);
            _down.Offset = _down.Clamp(_down.Offset);
            _across.Offset = _across.Clamp(_across.Offset);
        }

        /// <summary>Only the lines the window is over, then the numbers beside them, then the thumbs.</summary>
        protected override void DrawContent(IGraphics g)
        {
            var view = View;
            if (_lines.Count > 0 && _fontSize > 0f && view.Height > 0f)
            {
                var first = Math.Max(0, (int)(_down.Offset / _fontSize));
                var last = Math.Min(_lines.Count - 1, (int)((_down.Offset + view.Height) / _fontSize));
                var top = view.Y - _down.Offset + first * _fontSize;

                Gutter(g, view, first, last, top);

                var words = new RectangleF(view.X + _gutter, view.Y, Rail.Room(view.Width, _gutter), view.Height);
                var x = words.X - _across.Offset;
                var y = top;

                g.PushClip(words);
                for (var i = first; i <= last; i++)
                {
                    var said = _lines[i];
                    if (said.Length > 0)
                    {
                        if (i < _shownFrom || i > _shownTo)
                            Widen(g, said);

                        g.DrawText(_font, said, new Vector2(x, y), _fontSize, Color);
                    }

                    y += _fontSize;
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

        /// <summary>Measures a line coming into view, and reaches further where it is the longest yet.</summary>
        /// <remarks>
        /// Measures while drawing on purpose: it is asked only about lines that were not on show last frame, and measuring the
        /// whole listing up front was timed at 11ms for five hundred lines and 221ms for ten thousand, against a frame of under
        /// 17. The reach goes straight onto the rail without invalidating, because only the thumb is drawn from it, after this.
        /// </remarks>
        void Widen(IGraphics g, string said)
        {
            _scratch.Length = 0;
            _scratch.Append(said);

            var wide = g.MeasureText(_font, _scratch, _fontSize).X;
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
        }

        /// <summary>Which of its parts the pointer is holding, where it holds one.</summary>
        enum Held
        {
            Nothing,
            Down,
            Across,
            Text
        }
    }
}
