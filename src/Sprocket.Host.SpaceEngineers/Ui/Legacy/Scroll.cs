using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Legacy
{
    /// <summary>Shows part of something taller than the room available, and cuts the rest off.</summary>
    public class Scroll : Element
    {
        readonly ElementCollection _children;

        /// <summary>Where along the content this sits.</summary>
        readonly Rail _rail = new Rail();

        float _across;
        float _barWidth;

        Element _content;
        bool _horizontal;

        /// <summary>An empty scroll that runs down, cut to its bounds.</summary>
        public Scroll()
        {
            _children = new ElementCollection(this);
            ClipsContent = true;
        }

        /// <summary>How wide the bar is, across the direction of travel.</summary>
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

        /// <summary>The thumb's colour.</summary>
        public Color BarColor { get; set; }

        /// <summary>The thumb's colour while it is being held.</summary>
        public Color BarHeldColor { get; set; }

        /// <summary>How short the thumb is allowed to get.</summary>
        public float BarMinLength
        {
            get { return _rail.MinThumb; }
            set { _rail.MinThumb = value; }
        }

        /// <summary>Whether the thumb is being dragged right now.</summary>
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

        /// <summary>The one thing scrolled.</summary>
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

        /// <summary>How far along the content has been moved, in UI units.</summary>
        public float Scrolled
        {
            get { return _rail.Offset; }
            set
            {
                if (_rail.Offset == value)
                    return;

                _rail.Offset = value;
                InvalidateArrange();
            }
        }

        /// <summary>The full height of the content, once measured.</summary>
        public float Extent => _rail.Extent;

        /// <summary>The height on show.</summary>
        public float Viewport => _rail.Viewport;

        /// <summary>How far it could still be scrolled.</summary>
        public float MaxOffset => _rail.Max;

        /// <summary>Whether there is anything out of sight.</summary>
        public bool CanScroll => _rail.CanMove;

        /// <summary>Whether a thumb is being drawn: one was asked for, and there is something to scroll.</summary>
        bool HasBar => BarWidth > 0f && BarColor.A > 0 && CanScroll;

        /// <summary>The whole of the bar, thumb and empty part alike.</summary>
        RectangleF Track
        {
            get
            {
                // ContentBounds, because that is what the viewport was measured against.
                var box = ContentBounds;

                if (BarWidth <= 0f)
                    return new RectangleF(box.X, box.Y, 0f, 0f);

                return Horizontal
                    ? new RectangleF(box.X, box.Y + box.Height - BarWidth, box.Width, BarWidth)
                    : new RectangleF(box.X + box.Width - BarWidth, box.Y, BarWidth, box.Height);
            }
        }

        /// <summary>The part of the track that stands for what is on show.</summary>
        RectangleF Thumb
        {
            get
            {
                var track = Track;
                var start = _rail.ThumbStart;
                var length = _rail.ThumbLength;

                return Horizontal
                    ? new RectangleF(track.X + start, track.Y, length, track.Height)
                    : new RectangleF(track.X, track.Y + start, track.Width, length);
            }
        }

        /// <summary>Moves the content as though the thumb had been dragged <paramref name="distance" /> units.</summary>
        public void DragBy(float distance)
        {
            Scrolled = _rail.Dragged(distance);
        }

        /// <summary>Puts the middle of the thumb at <paramref name="at" />, in screen units.</summary>
        public void ThumbTo(float at)
        {
            var track = Track;
            Scrolled = _rail.ThumbedTo(at, Horizontal ? track.X : track.Y);
        }

        /// <summary>Moves by <paramref name="amount" /> and stays inside the content.</summary>
        public void ScrollBy(float amount)
        {
            Scrolled = _rail.Moved(amount);
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            _across = BarWidth > 0f ? BarWidth : 0f;

            _content?.Measure(g, Horizontal
                ? new Vector2(Unbounded, Room(available.Y))
                : new Vector2(Room(available.X), Unbounded));

            return available;
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            _rail.Viewport = Horizontal ? bounds.Width : bounds.Height;
            _across = BarWidth > 0f ? BarWidth : 0f;

            if (_content == null)
            {
                _rail.Extent = 0f;
                _rail.Offset = 0f;
                return;
            }

            _rail.Extent = Horizontal ? _content.DesiredSize.X : _content.DesiredSize.Y;
            _rail.Offset = _rail.Clamp(_rail.Offset);

            var extent = _rail.Extent;
            var offset = _rail.Offset;

            _content.Arrange(Horizontal
                ? new RectangleF(bounds.X - offset, bounds.Y, extent, Room(bounds.Height))
                : new RectangleF(bounds.X, bounds.Y - offset, Room(bounds.Width), extent));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _content?.Draw(g);

            if (!HasBar)
                return;

            // No groove behind the thumb, as in the game's own scrollbars.
            var thumb = BarHeld && BarHeldColor.A > 0 ? BarHeldColor : BarColor;
            g.DrawSprite(Solid, Thumb, thumb, 0f);
        }

        /// <summary>What is under a point in the content, or null.</summary>
        /// <remarks>The bar is never offered: the screen asks whether it is grabbed before any press.</remarks>
        protected override Element UnderContent(Vector2 point)
        {
            return _content?.Under(point);
        }

        /// <summary>Whether a point is anywhere on the bar, thumb and empty track alike.</summary>
        public bool OnBar(Vector2 point)
        {
            return HasBar && Reaches(point) && Inside(Track, point);
        }

        /// <summary>Whether a point is on the thumb itself.</summary>
        public bool OnThumb(Vector2 point)
        {
            return HasBar && Reaches(point) && Inside(Thumb, point);
        }

        /// <summary>Whether a point is inside one of the bar's own rectangles.</summary>
        static bool Inside(RectangleF bounds, Vector2 point)
        {
            return point.X >= bounds.X && point.X <= bounds.X + bounds.Width
                                       && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height;
        }

        /// <summary>Whatever is left of a side once the bar has had its share.</summary>
        float Room(float whole)
        {
            return Rail.Room(whole, _across);
        }
    }
}