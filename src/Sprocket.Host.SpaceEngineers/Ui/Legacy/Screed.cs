using System;
using System.Collections.Generic;
using System.Text;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRage.Game;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Legacy
{
    /// <summary>A long piece of text, and a window onto it that moves both ways.</summary>
    /// <remarks>
    ///     How far there is to go sideways is the widest line seen so far, so the sideways thumb settles as
    ///     the text is explored; see <see cref="Widen" />.
    /// </remarks>
    public class Screed : Box
    {
        static readonly string[] Endings = { "\r\n", "\n", "\r" };

        readonly Rail _across = new Rail();

        readonly Rail _down = new Rail();

        readonly List<string> _lines = new List<string>();

        readonly List<string> _numbers = new List<string>();

        readonly StringBuilder _scratch = new StringBuilder(128);

        float _barWidth;

        float _digit;

        float _digitAt = -1f;
        string _digitIn;

        string _font;

        Vector2 _from;

        float _gutter;

        /// <summary>The line height, kept because drawing runs every frame.</summary>

        bool _numbered;

        int _numbersFrom = int.MaxValue;

        int _numbersTo = int.MinValue;

        float _fontSize;

        int _shownFrom = int.MaxValue;

        int _shownTo = int.MinValue;
        float _tall;
        string _text = string.Empty;

        float _widest;

        /// <summary>An empty screed in white, with lines 24 units tall, clipped to its bounds.</summary>
        public Screed()
        {
            Font = MyFontEnum.White;
            FontSize = 24f;
            Color = Color.White;
            ClipsContent = true;
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

        /// <summary>The colour the text is drawn in.</summary>
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

        /// <summary>What the numbers are drawn in.</summary>
        public Color NumberColor { get; set; }

        /// <summary>How thick the bars are, or zero for none.</summary>
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

        /// <summary>What colour the thumbs are, where transparent means there are no bars.</summary>
        public Color BarColor { get; set; }

        /// <summary>A thumb's colour while it is held, or transparent to use <see cref="BarColor" />.</summary>
        public Color BarHeldColor { get; set; }

        /// <summary>How short a thumb is allowed to get. See <see cref="Rail.MinThumb" />.</summary>
        public float BarMinLength
        {
            get { return _down.MinThumb; }
            set
            {
                _down.MinThumb = value;
                _across.MinThumb = value;
            }
        }

        /// <summary>Which thumb, if either, is being dragged right now.</summary>
        public Way Held { get; set; }

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

        /// <summary>How many lines it holds.</summary>
        public int Lines => _lines.Count;

        /// <summary>How tall the whole listing stands, once it has been measured.</summary>
        public float Extent => _down.Extent;

        /// <summary>How wide its widest line is, once it has been measured.</summary>
        public float Widest => _across.Extent;

        /// <summary>Whether the text itself is being dragged about right now.</summary>
        public bool IsPanning { get; private set; }

        /// <summary>The text's own room: what is left once the bars have had their share.</summary>
        RectangleF View
        {
            get
            {
                var box = ContentBounds;
                var bar = _barWidth > 0f ? _barWidth : 0f;

                return new RectangleF(box.X, box.Y,
                    Rail.Room(box.Width, bar), Rail.Room(box.Height, bar));
            }
        }

        /// <summary>Whether there is anything out of sight in a direction.</summary>
        public bool CanMove(Way way)
        {
            return Of(way).CanMove;
        }

        /// <summary>Moves by <paramref name="amount" /> and stays inside the text.</summary>
        public void MoveBy(Way way, float amount)
        {
            Put(way, Of(way).Moved(amount));
        }

        /// <summary>Moves as though the thumb had been dragged <paramref name="distance" /> units.</summary>
        public void DragBy(Way way, float distance)
        {
            Put(way, Of(way).Dragged(distance));
        }

        /// <summary>Puts the middle of a thumb at <paramref name="at" />, in screen units.</summary>
        public void ThumbTo(Way way, float at)
        {
            var track = Track(way);
            Put(way, Of(way).ThumbedTo(at, way == Way.Across ? track.X : track.Y));
        }

        /// <summary>Whether a point is anywhere on a bar, thumb and empty track alike.</summary>
        public bool OnBar(Way way, Vector2 point)
        {
            return HasBar(way) && Reaches(point) && Inside(Track(way), point);
        }

        /// <summary>Whether a point is on a thumb itself.</summary>
        public bool OnThumb(Way way, Vector2 point)
        {
            return HasBar(way) && Reaches(point) && Inside(Thumb(way), point);
        }

        /// <summary>Takes hold at <paramref name="point" />, of a thumb or of the text itself.</summary>
        public bool Take(Vector2 point)
        {
            return Grab(Way.Down, point) || Grab(Way.Across, point) || Pan(point);
        }

        /// <summary>Follows the hand to <paramref name="point" />, while something is held.</summary>
        public void Carry(Vector2 point)
        {
            if (Held != Way.None)
            {
                DragBy(Held, Held == Way.Across ? point.X - _from.X : point.Y - _from.Y);
                _from = point;
                return;
            }

            if (!IsPanning)
                return;

            MoveBy(Way.Down, _from.Y - point.Y);
            MoveBy(Way.Across, _from.X - point.X);
            _from = point;
        }

        /// <summary>Lets go of whatever was held.</summary>
        public void Drop()
        {
            Held = Way.None;
            IsPanning = false;
        }

        /// <summary>Takes all the room it was given.</summary>
        /// <remarks>Works out the extents here, because arranging has no font to ask.</remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var line = FontSize;

            _tall = _lines.Count > 0 ? _lines.Count * line : line;
            _gutter = Numbered && _lines.Count > 0 ? (Digits(_lines.Count) + 2) * DigitOf(g) : 0f;

            return available;
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

        /// <summary>Fits both rails to the room it was given and clamps each offset to its rail.</summary>
        protected override void ArrangeContent(RectangleF bounds)
        {
            var bar = _barWidth > 0f ? _barWidth : 0f;

            _down.Extent = _tall;
            _across.Extent = _widest;

            // Room for the bar is taken even with nothing to scroll, so the text does not move when it
            // starts to.
            _down.Viewport = Rail.Room(bounds.Height, bar);

            _across.Viewport = Rail.Room(Rail.Room(bounds.Width, bar), _gutter);

            _down.Offset = _down.Clamp(_down.Offset);
            _across.Offset = _across.Clamp(_across.Offset);
        }

        /// <summary>Only the lines the window is over, and then the thumbs.</summary>
        protected override void DrawContent(IGraphics g)
        {
            var view = View;

            if (_lines.Count > 0 && FontSize > 0f && view.Height > 0f)
            {
                var first = (int)(_down.Offset / FontSize);
                if (first < 0)
                    first = 0;

                var last = (int)((_down.Offset + view.Height) / FontSize);
                if (last >= _lines.Count)
                    last = _lines.Count - 1;

                var top = view.Y - _down.Offset + first * FontSize;

                Gutter(g, view, first, last, top);

                var words = new RectangleF(view.X + _gutter, view.Y,
                    Rail.Room(view.Width, _gutter), view.Height);

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

                        g.DrawText(Font, said, new Vector2(x, y), FontSize, Color);
                    }

                    y += FontSize;
                }

                _shownFrom = first;
                _shownTo = last;

                g.PopClip();
            }

            Bar(g, Way.Down);
            Bar(g, Way.Across);
        }

        /// <summary>The numbers of the lines on screen, right against the words they belong to.</summary>
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
                g.DrawText(Font, _numbers[i], new Vector2(edge, y), FontSize, NumberColor, Align.Right);
                y += FontSize;
            }

            g.PopClip();
        }

        /// <summary>Fills the numbers for a band of lines, only when the band has moved.</summary>
        void Number(int first, int last)
        {
            if (_numbersFrom == first && _numbersTo == last)
                return;

            _numbersFrom = first;
            _numbersTo = last;
            _numbers.Clear();

            for (var i = first; i <= last; i++)
                _numbers.Add((i + 1).ToString());
        }

        /// <summary>The widest of the ten digits, asked only when the face or the size has moved.</summary>
        float DigitOf(IGraphics g)
        {
            if (_digitAt == FontSize && _digitIn == Font)
                return _digit;

            _digitAt = FontSize;
            _digitIn = Font;
            _digit = 0f;

            for (var d = 0; d < 10; d++)
            {
                _scratch.Length = 0;
                _scratch.Append((char)('0' + d));

                var wide = g.MeasureText(Font, _scratch, FontSize).X;
                if (wide > _digit)
                    _digit = wide;
            }

            return _digit;
        }

        /// <summary>Takes hold of one bar, jumping the thumb to the press if it landed off it.</summary>
        bool Grab(Way way, Vector2 point)
        {
            if (!OnBar(way, point))
                return false;

            if (!OnThumb(way, point))
                ThumbTo(way, way == Way.Across ? point.X : point.Y);

            Held = way;
            _from = point;
            return true;
        }

        /// <summary>Takes hold of the text itself, where there is anywhere for it to go.</summary>
        bool Pan(Vector2 point)
        {
            if (!Reaches(point) || !Inside(View, point))
                return false;

            if (!_down.CanMove && !_across.CanMove)
                return false;

            IsPanning = true;
            _from = point;
            return true;
        }

        void Bar(IGraphics g, Way way)
        {
            if (!HasBar(way))
                return;

            var thumb = Held == way && BarHeldColor.A > 0 ? BarHeldColor : BarColor;
            g.Fill(Thumb(way), thumb);
        }

        bool HasBar(Way way)
        {
            return _barWidth > 0f && BarColor.A > 0 && Of(way).CanMove;
        }

        /// <summary>The whole of a bar, thumb and empty part alike, inside the room the text does not get.</summary>
        RectangleF Track(Way way)
        {
            var box = ContentBounds;
            var bar = _barWidth > 0f ? _barWidth : 0f;

            if (bar <= 0f)
                return new RectangleF(box.X, box.Y, 0f, 0f);

            return way == Way.Across
                ? new RectangleF(box.X, box.Y + box.Height - bar, Rail.Room(box.Width, bar), bar)
                : new RectangleF(box.X + box.Width - bar, box.Y, bar, Rail.Room(box.Height, bar));
        }

        RectangleF Thumb(Way way)
        {
            var track = Track(way);
            var rail = Of(way);
            var start = rail.ThumbStart;
            var length = rail.ThumbLength;

            return way == Way.Across
                ? new RectangleF(track.X + start, track.Y, length, track.Height)
                : new RectangleF(track.X, track.Y + start, track.Width, length);
        }

        Rail Of(Way way)
        {
            return way == Way.Across ? _across : _down;
        }

        void Put(Way way, float offset)
        {
            if (way == Way.Across)
                Shifted = offset;
            else
                Scrolled = offset;
        }

        /// <summary>Measures a line coming into view, and reaches further if it is the longest yet.</summary>
        /// <remarks>
        ///     Measures while drawing on purpose: it is asked only about lines that were not on screen last
        ///     frame, and measuring the whole listing up front was timed at 11ms for five hundred lines and
        ///     221ms for ten thousand, against a frame of under 17.
        ///     <para>
        ///         The reach goes straight onto the rail without invalidating, because only the thumb is drawn
        ///         from it, after this.
        ///     </para>
        /// </remarks>
        void Widen(IGraphics g, string said)
        {
            _scratch.Length = 0;
            _scratch.Append(said);

            var wide = g.MeasureText(Font, _scratch, FontSize).X;
            if (wide <= _widest)
                return;

            _widest = wide;
            _across.Extent = _widest;
        }

        /// <summary>Drops what it learned, because it was learned about something that has since changed.</summary>
        void Forget()
        {
            _widest = 0f;
            _across.Extent = 0f;
            _shownFrom = int.MaxValue;
            _shownTo = int.MinValue;
            _numbersFrom = int.MaxValue;
            _numbersTo = int.MinValue;
        }

        /// <summary>Whether a point is inside a rectangle that is not any element's bounds.</summary>
        static bool Inside(RectangleF bounds, Vector2 point)
        {
            return point.X >= bounds.X && point.X <= bounds.X + bounds.Width
                                       && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height;
        }
    }
}