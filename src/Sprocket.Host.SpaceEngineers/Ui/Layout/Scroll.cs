using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>Shows as much of something taller than its room as fits, moved up and down by a thumb on a bar at its side.</summary>
    /// <remarks>The bar takes the pointer itself: a press on the thumb drags it, and a press on the track puts the thumb there first.</remarks>
    public class Scroll : Element
    {
        readonly ElementCollection _children;
        readonly Rail _rail = new Rail();

        float _barWidth;
        Element _content;
        bool _held;

        /// <summary>Where the pointer was when the thumb last moved under it.</summary>
        float _heldAt;

        /// <summary>An empty scroll, cut to its bounds.</summary>
        public Scroll()
        {
            _children = new ElementCollection(this);
            ClipsContent = true;
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

        /// <summary>How wide the bar is.</summary>
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

        /// <summary>The thumb's color.</summary>
        public Color Thumb { get; set; }

        /// <summary>How short the thumb may get.</summary>
        public float LeastThumb
        {
            get { return _rail.MinThumb; }
            set { _rail.MinThumb = value; }
        }

        /// <summary>Whether the thumb is held by the pointer.</summary>
        public bool IsHeld
        {
            get { return _held; }
            private set
            {
                if (_held == value)
                    return;

                _held = value;
                Restyle();
            }
        }

        /// <summary>How far down the content has been moved, in UI units.</summary>
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

        /// <summary>Whether any of the content is out of sight.</summary>
        public bool CanScroll => _rail.CanMove;

        /// <inheritdoc />
        protected override bool TakesPointer => true;

        bool HasBar => _barWidth > 0f && Thumb.A > 0 && _rail.CanMove;

        /// <summary>The whole of the bar, thumb and empty track alike.</summary>
        RectangleF Track
        {
            get
            {
                var room = ContentBounds;
                return new RectangleF(room.X + room.Width - _barWidth, room.Y, _barWidth, room.Height);
            }
        }

        /// <summary>The part of the track that stands for what is on show.</summary>
        RectangleF ThumbBounds
        {
            get
            {
                var track = Track;
                return new RectangleF(track.X, track.Y + _rail.ThumbStart, track.Width, _rail.ThumbLength);
            }
        }

        /// <summary>Moves the content by <paramref name="amount" /> and keeps it inside itself.</summary>
        public void ScrollBy(float amount)
        {
            Scrolled = _rail.Moved(amount);
        }

        /// <inheritdoc />
        protected override bool Hits(Vector2 point)
        {
            return HasBar && Inside(Track, point);
        }

        /// <inheritdoc />
        protected internal override void OnPointerDown(Vector2 point, int presses)
        {
            if (!Hits(point))
                return;

            if (!Inside(ThumbBounds, point))
                Scrolled = _rail.ThumbedTo(point.Y, Track.Y);

            _heldAt = point.Y;
            IsHeld = true;
        }

        /// <inheritdoc />
        protected internal override void OnPointerMove(Vector2 point)
        {
            if (!_held)
                return;

            Scrolled = _rail.Dragged(point.Y - _heldAt);
            _heldAt = point.Y;
        }

        /// <inheritdoc />
        protected internal override void OnPointerUp(Vector2 point)
        {
            IsHeld = false;
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_content != null)
                _content.Measure(g, new Vector2(Rail.Room(available.X, _barWidth), Unbounded));

            return available;
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            _rail.Viewport = bounds.Height;

            if (_content == null)
            {
                _rail.Extent = 0f;
                _rail.Offset = 0f;
                return;
            }

            _rail.Extent = _content.DesiredSize.Y;
            _rail.Offset = _rail.Clamp(_rail.Offset);

            _content.Arrange(new RectangleF(bounds.X, bounds.Y - _rail.Offset, Rail.Room(bounds.Width, _barWidth),
                _rail.Extent));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (_content != null)
                _content.Draw(g);

            // A thumb on no groove, as the game draws its own.
            if (HasBar)
                g.Fill(ThumbBounds, Thumb);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            return _content != null ? _content.Under(point) : null;
        }

        static bool Inside(RectangleF bounds, Vector2 point)
        {
            return point.X >= bounds.X && point.X <= bounds.X + bounds.Width
                                       && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height;
        }
    }
}
