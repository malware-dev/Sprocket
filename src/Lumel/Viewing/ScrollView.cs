using System;
using Lumel.Drawing;
using VRageMath;

namespace Lumel.Viewing
{
    /// <summary>Shows as much of something taller than its room as fits, moved up and down by a thumb on a bar at its side.</summary>
    /// <remarks>
    /// The bar takes the pointer itself: a press on the thumb drags it, and a press on the track puts the thumb there first.
    /// </remarks>
    public class ScrollView : ContentControl, IScrolls
    {
        readonly Rail _rail = new Rail();

        float _barWidth;
        bool _held;
        bool _fits;

        float _heldAt;
        Element _bringing;

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

        /// <summary>Whether it asks only for the room its content needs, up to what it is given, rather than all it is given.</summary>
        /// <remarks>For one hanging open, which is as tall as its content until that runs past the room there is.</remarks>
        public bool FitsContent
        {
            get { return _fits; }
            set
            {
                if (_fits == value)
                    return;

                _fits = value;
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
                StateChanged();
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void ScrollBy(float amount)
        {
            Scrolled = _rail.Moved(amount);
        }

        /// <summary>Scrolls as little as it takes to show all of <paramref name="element" />, somewhere inside what this shows, when it is next placed.</summary>
        public void BringIntoView(Element element)
        {
            _bringing = element;
            InvalidateArrange();
        }

        /// <inheritdoc />
        protected override bool Hits(Vector2 point)
        {
            return HasBar && Inside(Track, point);
        }

        /// <inheritdoc />
        public override void OnPointerDown(Vector2 point, int presses, bool shift, bool ctrl)
        {
            if (!Hits(point))
                return;

            if (!Inside(ThumbBounds, point))
                Scrolled = _rail.ThumbedTo(point.Y, Track.Y);

            _heldAt = point.Y;
            IsHeld = true;
        }

        /// <inheritdoc />
        public override void OnPointerMove(Vector2 point)
        {
            if (!_held)
                return;

            Scrolled = _rail.Dragged(point.Y - _heldAt);
            _heldAt = point.Y;
        }

        /// <inheritdoc />
        public override void OnPointerUp(Vector2 point)
        {
            IsHeld = false;
        }

        /// <inheritdoc />
        /// <remarks>Always: what it holds runs past its edges.</remarks>
        protected override bool Clips => true;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (Content == null)
                return _fits ? Vector2.Zero : available;

            Content.Measure(g, new Vector2(Rail.Room(available.X, _barWidth), Unbounded));
            if (!_fits)
                return available;

            var wanted = Content.DesiredSize;
            var runsPast = wanted.Y > available.Y;
            return new Vector2(Math.Min(wanted.X + (runsPast ? _barWidth : 0f), available.X), Math.Min(wanted.Y, available.Y));
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            _rail.Viewport = bounds.Height;

            if (Content == null)
            {
                _rail.Extent = 0f;
                _rail.Offset = 0f;
                return;
            }

            _rail.Extent = Content.DesiredSize.Y;
            _rail.Offset = _rail.Clamp(_rail.Offset);
            Place(bounds);

            var bringing = _bringing;
            _bringing = null;
            if (bringing == null)
                return;

            // Where it stands in the content, known only once the content has been placed.
            var top = bringing.Bounds.Y - (bounds.Y - _rail.Offset);
            var offset = _rail.Offset;
            if (top < offset)
                offset = top;
            else if (top + bringing.Bounds.Height > offset + bounds.Height)
                offset = top + bringing.Bounds.Height - bounds.Height;

            offset = _rail.Clamp(offset);
            if (offset == _rail.Offset)
                return;

            _rail.Offset = offset;
            Place(bounds);
        }

        /// <summary>Places the content at the height it is scrolled to, inside <paramref name="bounds" /> less the bar.</summary>
        void Place(RectangleF bounds)
        {
            Content.Arrange(new RectangleF(bounds.X, bounds.Y - _rail.Offset, Rail.Room(bounds.Width, _barWidth),
                _rail.Extent));
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            if (Content != null)
                Content.Draw(g);

            // A thumb on no groove, as the game draws its own.
            if (HasBar)
                g.Fill(ThumbBounds, Thumb);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            return Content != null ? Content.Under(point) : null;
        }

        static bool Inside(RectangleF bounds, Vector2 point)
        {
            return point.X >= bounds.X && point.X <= bounds.X + bounds.Width
                                       && point.Y >= bounds.Y && point.Y <= bounds.Y + bounds.Height;
        }
    }
}
