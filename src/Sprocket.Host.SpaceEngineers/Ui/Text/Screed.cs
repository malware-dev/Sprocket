using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using Sprocket.Host.SpaceEngineers.Ui.Layout;

namespace Sprocket.Host.SpaceEngineers.Ui.Text
{
    /// <summary>A long piece of text, and a window onto it that moves both ways.</summary>
    /// <remarks>
    /// <b>A viewport rather than something to put in one.</b> It takes the room it is given and moves the
    /// text behind it, which is what lets it hold thousands of lines: a <see cref="Scroll" /> arranges its
    /// content at full height and a listing arranged at full height is a draw call per line, all but a
    /// screenful of them thrown away by the scissor.
    /// <para>
    /// <b>Down is free.</b> Every line of one face at one scale stands the same height, so how tall the
    /// whole is comes out of a multiplication: five thousand lines cost what five do, and only the lines
    /// the window is actually over are drawn.
    /// </para>
    /// <para>
    /// <b>Across is learned, never swept.</b> How wide the widest line is cannot be had by arithmetic, and
    /// measuring every line to find out is not affordable - timed here at 11ms for five hundred lines and
    /// 221ms for ten thousand, against a frame of under 17. So a line is measured when it comes into view,
    /// and how far there is to go sideways is the widest seen so far. The consequence is deliberate: the
    /// sideways thumb settles as somebody explores rather than being right from the first frame. What it
    /// is never wrong about is the text on screen, because that is precisely what has been measured.
    /// </para>
    /// <para>
    /// <b>The text can be taken hold of and dragged</b>, either thumb or the words themselves, so reaching
    /// a bar is never the only way to get somewhere.
    /// </para>
    /// <para>
    /// <b>Lines are not wrapped.</b> A listing's structure is its lines, and folding the long ones would
    /// destroy exactly the thing somebody opened it to read. That is what the sideways axis is for.
    /// </para>
    /// <para>
    /// <b>The text is held as lines, not as one run.</b> Nothing yet changes one of them and this offers no
    /// way to - but a caret has to be able to land on a line, and a line has to be able to change without
    /// the rest being rebuilt, and holding the lines is what keeps that a later piece of work rather than a
    /// rewrite.
    /// </para>
    /// </remarks>
    public class Screed : Element
    {
        /// <summary>
        /// Every ending, because text arrives from whatever wrote it.
        /// </summary>
        /// <remarks>
        /// Held rather than made at the call, which would be an array per assignment.
        /// </remarks>
        static readonly string[] Endings = { "\r\n", "\n", "\r" };

        readonly List<string> _lines = new List<string>();

        /// <summary>
        /// One line at a time, because measuring is only offered a <c>StringBuilder</c>.
        /// </summary>
        /// <remarks>
        /// Kept and refilled rather than made per line, so one builder grown to the longest line is the
        /// whole allocation however much text goes through it.
        /// </remarks>
        readonly StringBuilder _scratch = new StringBuilder(128);

        /// <summary>
        /// The numbers down the gutter, for the lines on screen and no others.
        /// </summary>
        /// <remarks>
        /// Kept because <c>DrawText</c> wants a string and turning an int into one allocates. Held still
        /// this is refilled never; moving refills it once, which is a handful of very small strings for
        /// as long as the hand is moving.
        /// </remarks>
        readonly List<string> _numbers = new List<string>();

        readonly Rail _down = new Rail();
        readonly Rail _across = new Rail();

        string _font;
        float _scale;
        string _text = string.Empty;

        /// <summary>The widest line seen so far, which is how far sideways there is to go.</summary>
        float _widest;

        /// <summary>
        /// The lines that were on screen last time it was drawn.
        /// </summary>
        /// <remarks>
        /// The whole of what it remembers about measuring, and it is two numbers rather than one per
        /// line: a line is measured when it falls outside this, so a window held still measures nothing
        /// and moving down one line measures one line. Coming back over ground already covered measures
        /// it again, which is one line per step and the price of not keeping a note per line.
        /// </remarks>
        int _shownFrom = int.MaxValue;

        int _shownTo = int.MinValue;

        /// <summary>Which band <see cref="_numbers" /> was filled for, so it is only filled again if it moved.</summary>
        int _numbersFrom = int.MaxValue;

        int _numbersTo = int.MinValue;

        /// <summary>
        /// The widest of the ten digits, and how wide the gutter therefore is.
        /// </summary>
        /// <remarks>
        /// Ten measurements, once per face and scale, and the gutter is arithmetic from there: a number
        /// of <i>n</i> digits is never wider than <i>n</i> of the widest one. Measuring the numbers
        /// actually on screen instead would be exact and would change the gutter's width as somebody
        /// scrolled past a longer one, which moves every line of the listing sideways under their eyes.
        /// </remarks>
        float _digit;

        float _digitAt = -1f;
        string _digitIn;
        float _gutter;

        /// <summary>The line height and the whole height, kept: drawing cannot be invalidated.</summary>
        float _line;

        float _lineAt = -1f;
        string _lineIn;
        float _tall;

        float _barWidth;
        bool _numbered;

        /// <summary>Where the hand was when it was last heard from, while something is being dragged.</summary>
        Vector2 _from;

        bool _panning;

        public Screed()
        {
            Font = MyFontEnum.White;
            Scale = 1f;
            Color = Color.White;
            ClipsContent = true;
        }

        /// <summary>
        /// The whole of it, split into lines as it is set.
        /// </summary>
        /// <remarks>
        /// One pass over the characters, once, when somebody puts text in. Nothing reads the text again:
        /// every later question is answered from the lines.
        /// </remarks>
        public string Text
        {
            get { return _text; }
            set
            {
                string incoming = value ?? string.Empty;
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

        /// <summary>Which face it is drawn in. Anything the target knows.</summary>
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

        public float Scale
        {
            get { return _scale; }
            set
            {
                if (_scale == value)
                    return;

                _scale = value;
                Forget();
                InvalidateMeasure();
            }
        }

        public Color Color { get; set; }

        /// <summary>
        /// Whether each line is numbered down a gutter on the left.
        /// </summary>
        /// <remarks>
        /// The gutter belongs to the window rather than to the text: it holds still while the words are
        /// dragged sideways past it, because a line number that scrolled off the edge would be no use
        /// at exactly the moment somebody is following a long line across.
        /// </remarks>
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

        /// <summary>What the numbers are drawn in. Dimmer than the text, or they read as part of it.</summary>
        public Color NumberColor { get; set; }

        /// <summary>How thick the bars are. Zero for none, and then nothing is taken out of the room.</summary>
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

        /// <summary>The thumbs. Nothing is drawn while this is transparent, whatever the width says.</summary>
        public Color BarColor { get; set; }

        /// <summary>A thumb while it is being held. Falls back to <see cref="BarColor" />.</summary>
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

        /// <summary>Which thumb, if either, is being dragged right now. Only changes a colour.</summary>
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
        public int Lines
        {
            get { return _lines.Count; }
        }

        /// <summary>How tall the whole listing stands, once it has been measured.</summary>
        public float Extent
        {
            get { return _down.Extent; }
        }

        /// <summary>How wide its widest line is, once it has been measured.</summary>
        public float Widest
        {
            get { return _across.Extent; }
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
            RectangleF track = Track(way);
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

        /// <summary>
        /// Takes hold at <paramref name="point" />, of a thumb or of the text itself.
        /// </summary>
        /// <remarks>
        /// One question rather than three, so a host offers the press once and is told whether it was
        /// wanted. Thumbs first: they sit on top of the text's own room and a press there means the bar.
        /// </remarks>
        public bool Take(Vector2 point)
        {
            return Grab(Way.Down, point) || Grab(Way.Across, point) || Pan(point);
        }

        /// <summary>Where the hand has got to. Ignored while nothing is held.</summary>
        /// <remarks>
        /// Panning is direct: the text goes where the hand goes, so the window moves against it. Dragging
        /// downwards brings back what is above, which is what taking hold of a sheet of paper does.
        /// </remarks>
        public void Carry(Vector2 point)
        {
            if (Held != Way.None)
            {
                DragBy(Held, Held == Way.Across ? point.X - _from.X : point.Y - _from.Y);
                _from = point;
                return;
            }

            if (!_panning)
                return;

            MoveBy(Way.Down, _from.Y - point.Y);
            MoveBy(Way.Across, _from.X - point.X);
            _from = point;
        }

        /// <summary>Lets go of whatever was held.</summary>
        public void Drop()
        {
            Held = Way.None;
            _panning = false;
        }

        /// <summary>Whether the text itself is being dragged about right now.</summary>
        public bool IsPanning
        {
            get { return _panning; }
        }

        /// <summary>
        /// The room it was given. The text moves behind it rather than it growing to fit the text.
        /// </summary>
        /// <remarks>
        /// Both extents are worked out here, where the font can be asked questions, and read again during
        /// arranging, where it cannot.
        /// </remarks>
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            float line = LineOf(g);

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

        protected override void ArrangeContent(RectangleF bounds)
        {
            float bar = _barWidth > 0f ? _barWidth : 0f;

            _down.Extent = _tall;
            _across.Extent = _widest;

            // The room is taken whether or not there is anything to scroll, so the text does not shuffle
            // the moment it grows past its box.
            _down.Viewport = Rail.Room(bounds.Height, bar);

            // The gutter is not room the words get, so how far across there is to go is measured
            // against what is left beside it.
            _across.Viewport = Rail.Room(Rail.Room(bounds.Width, bar), _gutter);

            _down.Offset = _down.Clamp(_down.Offset);
            _across.Offset = _across.Clamp(_across.Offset);
        }

        /// <summary>Only the lines the window is over, and then the thumbs.</summary>
        protected override void DrawContent(IGraphics g)
        {
            RectangleF view = View;

            if (_lines.Count > 0 && _line > 0f && view.Height > 0f)
            {
                var first = (int)(_down.Offset / _line);
                if (first < 0)
                    first = 0;

                var last = (int)((_down.Offset + view.Height) / _line);
                if (last >= _lines.Count)
                    last = _lines.Count - 1;

                float top = view.Y - _down.Offset + first * _line;

                Gutter(g, view, first, last, top);

                var words = new RectangleF(view.X + _gutter, view.Y,
                    Rail.Room(view.Width, _gutter), view.Height);

                float x = words.X - _across.Offset;
                float y = top;

                // Narrowed to the words' own room, so a long line stops at the gutter and the bars
                // rather than running underneath them.
                g.PushClip(words);

                for (int i = first; i <= last; i++)
                {
                    string said = _lines[i];
                    if (said.Length > 0)
                    {
                        if (i < _shownFrom || i > _shownTo)
                            Widen(g, said);

                        g.DrawText(Font, said, new Vector2(x, y), Scale, Color);
                    }

                    y += _line;
                }

                _shownFrom = first;
                _shownTo = last;

                g.PopClip();
            }

            Bar(g, Way.Down);
            Bar(g, Way.Across);
        }

        /// <summary>
        /// The numbers of the lines on screen, right against the words they belong to.
        /// </summary>
        /// <remarks>
        /// It does not move with <see cref="Shifted" />. Dragging the words sideways leaves the numbers
        /// where they are, which is the only arrangement in which they are any use while following a
        /// long line across.
        /// </remarks>
        void Gutter(IGraphics g, RectangleF view, int first, int last, float top)
        {
            if (_gutter <= 0f || NumberColor.A == 0)
                return;

            Number(first, last);

            // Right against the words, every number ending a digit's width short of them. Placed by
            // its end rather than its start, so nothing here has to know how wide a number is.
            float edge = view.X + _gutter - _digit;
            float y = top;

            g.PushClip(new RectangleF(view.X, view.Y, _gutter, view.Height));

            for (int i = 0; i < _numbers.Count; i++)
            {
                g.DrawText(Font, _numbers[i], new Vector2(edge, y), Scale, NumberColor, Align.Right);
                y += _line;
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

            // One-based, because the first line of anything anybody reads is line one.
            for (int i = first; i <= last; i++)
                _numbers.Add((i + 1).ToString());
        }

        /// <summary>The widest of the ten digits, asked only when the face or the scale has moved.</summary>
        float DigitOf(IGraphics g)
        {
            if (_digitAt == Scale && _digitIn == Font)
                return _digit;

            _digitAt = Scale;
            _digitIn = Font;
            _digit = 0f;

            for (var d = 0; d < 10; d++)
            {
                _scratch.Length = 0;
                _scratch.Append((char)('0' + d));

                float wide = g.MeasureText(Font, _scratch, Scale).X;
                if (wide > _digit)
                    _digit = wide;
            }

            return _digit;
        }

        /// <summary>The text's own room: what is left once the bars have had their share.</summary>
        RectangleF View
        {
            get
            {
                RectangleF box = ContentBounds;
                float bar = _barWidth > 0f ? _barWidth : 0f;

                return new RectangleF(box.X, box.Y,
                    Rail.Room(box.Width, bar), Rail.Room(box.Height, bar));
            }
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

            _panning = true;
            _from = point;
            return true;
        }

        void Bar(IGraphics g, Way way)
        {
            if (!HasBar(way))
                return;

            // No groove behind it, the same as everywhere else: the thumb alone, against whatever the
            // panel already is.
            Color thumb = Held == way && BarHeldColor.A > 0 ? BarHeldColor : BarColor;
            g.DrawSprite(Solid, Thumb(way), thumb, 0f);
        }

        bool HasBar(Way way)
        {
            return _barWidth > 0f && BarColor.A > 0 && Of(way).CanMove;
        }

        /// <summary>The whole of a bar, thumb and empty part alike, inside the room the text does not get.</summary>
        RectangleF Track(Way way)
        {
            RectangleF box = ContentBounds;
            float bar = _barWidth > 0f ? _barWidth : 0f;

            if (bar <= 0f)
                return new RectangleF(box.X, box.Y, 0f, 0f);

            return way == Way.Across
                ? new RectangleF(box.X, box.Y + box.Height - bar, Rail.Room(box.Width, bar), bar)
                : new RectangleF(box.X + box.Width - bar, box.Y, bar, Rail.Room(box.Height, bar));
        }

        RectangleF Thumb(Way way)
        {
            RectangleF track = Track(way);
            Rail rail = Of(way);
            float start = rail.ThumbStart;
            float length = rail.ThumbLength;

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

        /// <summary>The line height, asked of the font only when the face or the scale has moved.</summary>
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

        /// <summary>
        /// Measures one line the first time it is ever drawn, and reaches further if it is the longest yet.
        /// </summary>
        /// <remarks>
        /// <b>The one place here that measures text while drawing, and it is deliberate.</b> The rule it
        /// bends is about work that repeats, and this does not: the caller only asks about lines that were
        /// not on screen last frame, so a window held still measures nothing and moving down one line
        /// measures one line. Measuring the whole listing up front instead was timed at 11ms for five
        /// hundred lines and 221ms for ten thousand, against a frame of under 17 - a frozen game rather
        /// than a cost.
        /// <para>
        /// The reach is put straight onto the rail rather than invalidating anything. Nothing is laid out
        /// from it; only the thumb is drawn from it, and that is drawn after this.
        /// </para>
        /// </remarks>
        void Widen(IGraphics g, string said)
        {
            _scratch.Length = 0;
            _scratch.Append(said);

            float wide = g.MeasureText(Font, _scratch, Scale).X;
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

        /// <summary>
        /// Whether a point is inside one of the bars' own rectangles.
        /// </summary>
        /// <remarks>
        /// Not <see cref="Element.Holds" />, which asks about this element's own bounds. A track and a
        /// thumb are worked out rather than arranged, so they are not any element's bounds.
        /// </remarks>
        static bool Inside(RectangleF bounds, Vector2 point)
        {
            return point.X >= bounds.X && point.X <= bounds.X + bounds.Width
                && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height;
        }
    }
}
