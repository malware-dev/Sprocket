using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// Shows part of something taller than the room available, and cuts the rest off.
    /// <para>
    /// Built out of the two things that already exist rather than out of anything new: content arranged
    /// higher than the box it sits in, and the clip that stops it spilling. Which means it works on either
    /// target for the same reason a clipped panel does, and needs nothing of a backend that a panel could
    /// not also do.
    /// </para>
    /// <para>
    /// One direction at a time, chosen by <see cref="Horizontal" />. Both at once is a different thing
    /// with a different feel, and nothing here wants it: a list of rows scrolls down, and a strip of
    /// columns scrolls across.
    /// </para>
    /// </summary>
    public class Scroll : Element
    {
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

        public Scroll()
        {
            _children = new ElementCollection(this);
            ClipsContent = true;
        }

        /// <summary>Whether this scrolls across rather than down.</summary>
        public bool Horizontal { get; set; }

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
        public float Scrolled
        {
            get { return _offset; }
            set { _offset = value; }
        }

        /// <summary>The full height of the content, once measured.</summary>
        public float Extent
        {
            get { return _extent; }
        }

        /// <summary>The height on show.</summary>
        public float Viewport
        {
            get { return _viewport; }
        }

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
        public bool CanScroll
        {
            get { return MaxOffset > 0f; }
        }

        /// <summary>Moves by <paramref name="amount" /> and stays inside the content.</summary>
        public void ScrollBy(float amount)
        {
            _offset = Clamp(_offset + amount);
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_content != null)
            {
                _content.Measure(g, Horizontal
                    ? new Vector2(Unbounded, available.Y)
                    : new Vector2(available.X, Unbounded));
            }

            // Takes what it is given rather than what its content wants, which is what makes it a window
            // onto the content instead of a container that grows with it.
            return available;
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            _viewport = Horizontal ? bounds.Width : bounds.Height;

            if (_content == null)
            {
                _extent = 0f;
                _offset = 0f;
                return;
            }

            _extent = Horizontal ? _content.DesiredSize.X : _content.DesiredSize.Y;
            _offset = Clamp(_offset);

            _content.Arrange(Horizontal
                ? new RectangleF(bounds.X - _offset, bounds.Y, _extent, bounds.Height)
                : new RectangleF(bounds.X, bounds.Y - _offset, bounds.Width, _extent));
        }

        protected override void DrawContent(IGraphics g)
        {
            if (_content != null)
                _content.Draw(g);
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
