using VRageMath;
using Sprocket.Host.SpaceEngineers.Drawing;

namespace Sprocket.Host.SpaceEngineers.Layout
{
    /// <summary>Shows part of something taller than the room available, and cuts the rest off.</summary>
    /// <remarks>
    /// Content arranged higher than the box it sits in, and the clip that stops it spilling. It therefore
    /// needs nothing of a backend that a clipped panel could not also do.
    /// <para>
    /// One direction at a time, chosen by <see cref="Horizontal" />: a list of rows scrolls down, and a
    /// strip of columns scrolls across.
    /// </para>
    /// </remarks>
    public class Scroll : Element
    {
        private float _barWidth;
        private bool _horizontal;

        /// <summary>
        /// The height offered to content when measuring. Not infinity: a stack works out its height by
        /// subtracting as it goes, and infinity minus anything stays infinity, which is true but useless.
        /// </summary>
        private const float Unbounded = 1000000f;

        private readonly ElementCollection _children;

        private Element _content;
        private float _offset;
        private float _extent;
        private float _viewport;
        private float _across;

        public Scroll()
        {
            _children = new ElementCollection(this);
            ClipsContent = true;
        }

        /// <summary>
        /// How wide the bar is, across the direction of travel. Zero for no bar at all.
        /// </summary>
        /// <remarks>
        /// The room is taken from the content whether or not there is anything to scroll, so a list does
        /// not shuffle sideways the moment it grows past its box.
        /// </remarks>
        public float BarWidth
        {
            get { return _barWidth; }
            set
            {
                if (_barWidth == value)
                    return;

                _barWidth = value;
                InvalidateMeasure();
            }
        }

        /// <summary>The thumb. Nothing is drawn while this is transparent, whatever the width says.</summary>
        public Color BarColor { get; set; }

        /// <summary>The thumb while it is being held. Falls back to <see cref="BarColor" />.</summary>
        public Color BarHeldColor { get; set; }

        /// <summary>
        /// How short the thumb is allowed to get.
        /// </summary>
        /// <remarks>
        /// A thumb is the same fraction of the track that the viewport is of the content, which on a very
        /// long list is a few units of nothing. It stops shrinking here and starts lying instead, which is
        /// the right way round: a thumb nobody can grab tells you less than one slightly out of place.
        /// </remarks>
        public float BarMinLength { get; set; }

        /// <summary>Whether the thumb is being dragged right now, which only changes its colour.</summary>
        public bool BarHeld { get; set; }

        /// <summary>Whether this scrolls across rather than down.</summary>
        public bool Horizontal
        {
            get { return _horizontal; }
            set
            {
                if (_horizontal == value)
                    return;

                _horizontal = value;
                InvalidateMeasure();
            }
        }

        public Element Content
        {
            get { return _content; }
            set
            {
                if (_content == value)
                    return;

                if (_content != null)
                    _children.Remove(_content);

                if (value != null)
                    _children.Add(value);

                _content = value;
            }
        }

        /// <summary>How far down the content has been moved, in UI units. Never past its end.</summary>
        /// <remarks>
        /// Moving the content is an arrangement and not a size, so this says so. Nothing else does: the
        /// tree is only laid out when something has said it changed, and a scroller that moved in silence
        /// simply did not move - the offset was new and the arrangement that reads it never ran again.
        /// </remarks>
        public float Scrolled
        {
            get { return _offset; }
            set
            {
                if (_offset == value)
                    return;

                _offset = value;
                InvalidateArrange();
            }
        }

        /// <summary>The full height of the content, once measured.</summary>
        public float Extent => _extent;

        /// <summary>The height on show.</summary>
        public float Viewport => _viewport;

        /// <summary>How far it could still be scrolled. Zero when everything already fits.</summary>
        public float MaxOffset
        {
            get
            {
                float max = _extent - _viewport;
                return max > 0f ? max : 0f;
            }
        }

        /// <summary>Whether there is anything out of sight.</summary>
        public bool CanScroll => MaxOffset > 0f;

        /// <summary>Whether a thumb is being drawn: one was asked for, and there is something to scroll.</summary>
        private bool HasBar => BarWidth > 0f && BarColor.A > 0 && CanScroll;

        /// <summary>
        /// The whole of the bar, thumb and empty part alike. Where a press counts as being on it.
        /// </summary>
        /// <remarks>
        /// Answered even when there is nothing to scroll, which is what lets
        /// <see cref="OnBar" /> be the one question a caller has to ask.
        /// </remarks>
        private RectangleF Track
        {
            get
            {
                // ContentBounds and not Bounds, because that is what the viewport was measured against.
                // Padding would otherwise put the thumb's travel and the content's travel out of step by
                // exactly the padding, which reads as a thumb that stops short of the end of its own list.
                RectangleF box = ContentBounds;

                if (BarWidth <= 0f)
                    return new RectangleF(box.X, box.Y, 0f, 0f);

                return Horizontal
                    ? new RectangleF(box.X, box.Y + box.Height - BarWidth, box.Width, BarWidth)
                    : new RectangleF(box.X + box.Width - BarWidth, box.Y, BarWidth, box.Height);
            }
        }

        /// <summary>The part of the track that stands for what is on show.</summary>
        private RectangleF Thumb
        {
            get
            {
                RectangleF track = Track;
                float length = ThumbLength;

                return Horizontal
                    ? new RectangleF(track.X + Travel * Fraction, track.Y, length, track.Height)
                    : new RectangleF(track.X, track.Y + Travel * Fraction, track.Width, length);
            }
        }

        /// <summary>How far along the content is, from nothing to all of it.</summary>
        private float Fraction
        {
            get
            {
                float max = MaxOffset;
                return max > 0f ? _offset / max : 0f;
            }
        }

        private float ThumbLength
        {
            get
            {
                if (_extent <= 0f || _viewport <= 0f)
                    return _viewport;

                float length = _viewport * _viewport / _extent;

                if (length < BarMinLength)
                    length = BarMinLength;
                if (length > _viewport)
                    length = _viewport;

                return length;
            }
        }

        /// <summary>How much room the thumb has to move in.</summary>
        private float Travel
        {
            get
            {
                float travel = _viewport - ThumbLength;
                return travel > 0f ? travel : 0f;
            }
        }

        /// <summary>
        /// Moves the content as though the thumb had been dragged <paramref name="distance" /> units.
        /// </summary>
        /// <remarks>
        /// The conversion is the whole reason this lives here rather than in the caller: a thumb travels
        /// the track while the content travels its own length, and the two are the same only on a list one
        /// screen long. Doing it in the view would put the extent and the viewport in two places.
        /// </remarks>
        public void DragBy(float distance)
        {
            float travel = Travel;
            if (travel <= 0f)
                return;

            ScrollBy(distance * MaxOffset / travel);
        }

        /// <summary>Puts the middle of the thumb at <paramref name="at" />, in screen units.</summary>
        /// <remarks>
        /// What a press on the empty part of the track does. Jumping to where the pointer is beats paging
        /// towards it: it is one gesture rather than several, and it is what the press already looked like
        /// it was going to do.
        /// </remarks>
        public void ThumbTo(float at)
        {
            float travel = Travel;
            if (travel <= 0f)
                return;

            RectangleF track = Track;
            float start = Horizontal ? track.X : track.Y;

            Scrolled = Clamp((at - start - ThumbLength * 0.5f) * MaxOffset / travel);
        }

        /// <summary>Moves by <paramref name="amount" /> and stays inside the content.</summary>
        public void ScrollBy(float amount)
        {
            Scrolled = Clamp(_offset + amount);
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _across = BarWidth > 0f ? BarWidth : 0f;

            if (_content != null)
            {
                _content.Measure(g, Horizontal
                    ? new Vector2(Unbounded, Room(available.Y))
                    : new Vector2(Room(available.X), Unbounded));
            }

            // Takes what it is given rather than what its content wants, which is what makes it a window
            // onto the content instead of a container that grows with it.
            return available;
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            _viewport = Horizontal ? bounds.Width : bounds.Height;
            _across = BarWidth > 0f ? BarWidth : 0f;

            if (_content == null)
            {
                _extent = 0f;
                _offset = 0f;
                return;
            }

            _extent = Horizontal ? _content.DesiredSize.X : _content.DesiredSize.Y;
            _offset = Clamp(_offset);

            _content.Arrange(Horizontal
                ? new RectangleF(bounds.X - _offset, bounds.Y, _extent, Room(bounds.Height))
                : new RectangleF(bounds.X, bounds.Y - _offset, Room(bounds.Width), _extent));
        }

        protected override void DrawContent(IGraphics g)
        {
            if (_content != null)
                _content.Draw(g);

            if (!HasBar)
                return;

            // No groove behind it. The game draws the thumb alone against whatever the panel already is,
            // and a track under ours would be a line down every list saying nothing the thumb does not.
            Color thumb = BarHeld && BarHeldColor.A > 0 ? BarHeldColor : BarColor;
            g.DrawSprite(Solid, Thumb, thumb, 0f);
        }

        /// <summary>
        /// What is under a point in the content, or null.
        /// </summary>
        /// <remarks>
        /// The thumb is not offered here. A bar is taken hold of rather than pressed, which happens
        /// before a press is a press at all - see <c>Grab</c> on the screen.
        /// </remarks>
        protected override Element UnderContent(Vector2 point)
        {
            return _content != null ? _content.Under(point) : null;
        }

        /// <summary>
        /// Whether a point is anywhere on the bar, thumb and empty track alike.
        /// </summary>
        /// <remarks>
        /// False when there is no bar, so a caller has one question to ask rather than two - and false
        /// where this scroller is not itself on show, because a bar cut away by something above it is
        /// still a rectangle, and dragging one scrolls a list nobody can see.
        /// </remarks>
        public bool OnBar(Vector2 point)
        {
            return HasBar && Reaches(point) && Inside(Track, point);
        }

        /// <summary>Whether a point is on the thumb itself.</summary>
        public bool OnThumb(Vector2 point)
        {
            return HasBar && Reaches(point) && Inside(Thumb, point);
        }

        /// <summary>
        /// Whether a point is inside one of the bar's own rectangles.
        /// </summary>
        /// <remarks>
        /// Not <see cref="Element.Holds" />, which asks about this element's own bounds. The track and
        /// the thumb are worked out rather than arranged, so they are not any element's bounds.
        /// </remarks>
        static bool Inside(RectangleF bounds, Vector2 point)
        {
            return point.X >= bounds.X && point.X <= bounds.X + bounds.Width
                && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height;
        }

        /// <summary>Whatever is left of a side once the bar has had its share.</summary>
        private float Room(float whole)
        {
            float room = whole - _across;
            return room > 0f ? room : 0f;
        }

        private float Clamp(float value)
        {
            float max = MaxOffset;

            if (value > max)
                value = max;
            if (value < 0f)
                value = 0f;

            return value;
        }
    }
}
