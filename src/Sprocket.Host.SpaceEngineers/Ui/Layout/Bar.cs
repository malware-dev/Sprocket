using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>Something on the left, something on the right, and whatever is in the middle taking the slack.</summary>
    public class Bar : Element
    {
        readonly ElementCollection _children;
        Element _fill;

        Element _left;
        Element _right;
        float _spacing;
        bool _tops;

        /// <summary>An empty bar.</summary>
        public Bar()
        {
            _children = new ElementCollection(this);
        }

        /// <summary>The part at the left, given the width it asks for.</summary>
        public Element Left
        {
            get { return _left; }
            set { _left = Adopt(_left, value); }
        }

        /// <summary>The middle, given the width the other two did not take.</summary>
        public Element Fill
        {
            get { return _fill; }
            set { _fill = Adopt(_fill, value); }
        }

        /// <summary>The part at the right, given the width it asks for.</summary>
        public Element Right
        {
            get { return _right; }
            set { _right = Adopt(_right, value); }
        }

        /// <summary>Whether this reports what its parts need rather than the whole width on offer.</summary>
        /// <remarks>
        ///     Set for a bar inside anything sized by its contents, which a spanning bar would otherwise widen
        ///     to whatever it was offered.
        /// </remarks>
        public bool Tight { get; set; }

        /// <summary>Whether the parts sit along the top of the bar rather than on its middle.</summary>
        public bool Tops
        {
            get { return _tops; }
            set
            {
                if (_tops == value)
                    return;

                _tops = value;
                InvalidateArrange();
            }
        }

        /// <summary>Blank room between the parts.</summary>
        public float Spacing
        {
            get { return _spacing; }
            set
            {
                if (_spacing == value)
                    return;

                _spacing = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var used = 0f;
            var height = 0f;

            if (Shows(_left))
            {
                _left.Measure(g, available);
                used += _left.DesiredSize.X + Spacing;
                height = Taller(height, _left.DesiredSize.Y);
            }

            if (Shows(_right))
            {
                _right.Measure(g, available);
                used += _right.DesiredSize.X + Spacing;
                height = Taller(height, _right.DesiredSize.Y);
            }

            if (_fill != null)
            {
                var left = available.X - used;
                _fill.Measure(g, new Vector2(left > 0f ? left : 0f, available.Y));
                height = Taller(height, _fill.DesiredSize.Y);
            }

            if (!Tight)
            {
                return new Vector2(available.X, height);
            }

            var wide = used + (_fill != null ? _fill.DesiredSize.X : 0f);
            return new Vector2(wide < available.X ? wide : available.X, height);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var left = bounds.X;
            var right = bounds.X + bounds.Width;

            if (Shows(_left))
            {
                var width = _left.DesiredSize.X;
                _left.Arrange(Placed(left, bounds, width, _left.DesiredSize.Y));
                left += width + Spacing;
            }

            if (Shows(_right))
            {
                var width = _right.DesiredSize.X;
                _right.Arrange(Placed(right - width, bounds, width, _right.DesiredSize.Y));
                right -= width + Spacing;
            }

            if (_fill != null)
            {
                var width = right - left;
                _fill.Arrange(Placed(left, bounds, width > 0f ? width : 0f, _fill.DesiredSize.Y));
            }
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _fill?.Draw(g);
            _left?.Draw(g);
            _right?.Draw(g);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            var found = _right?.Under(point);

            if (found == null && _left != null)
                found = _left.Under(point);

            if (found == null && _fill != null)
                found = _fill.Under(point);

            return found;
        }

        /// <summary>Whether a part is there at all and is asking to be seen.</summary>
        static bool Shows(Element part)
        {
            return part != null && !part.Collapsed;
        }

        /// <summary>Where a part goes: along the top where the bar says so, and on its middle otherwise.</summary>
        RectangleF Placed(float x, RectangleF bounds, float width, float height)
        {
            if (!_tops || height <= 0f || height > bounds.Height)
                return Centred(x, bounds, width, height);

            return new RectangleF(x, bounds.Y, width, height);
        }

        /// <summary>Where a part goes when it sits on the middle of the bar.</summary>
        static RectangleF Centred(float x, RectangleF bounds, float width, float height)
        {
            if (height > bounds.Height || height <= 0f)
                return new RectangleF(x, bounds.Y, width, bounds.Height);

            return new RectangleF(x, bounds.Y + (bounds.Height - height) * 0.5f, width, height);
        }

        static float Taller(float a, float b)
        {
            return a > b ? a : b;
        }

        Element Adopt(Element current, Element incoming)
        {
            if (current == incoming)
                return current;

            if (current != null)
                _children.Remove(current);

            if (incoming != null)
                _children.Add(incoming);

            return incoming;
        }
    }
}