using VRageMath;
using Sprocket.Host.SpaceEngineers.Ui.Drawing;

namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>A band at the top, a band at the bottom, and everything left over in between.</summary>
    /// <remarks>
    /// Three named places rather than a side per child, which would need per-child metadata that
    /// <see cref="ElementCollection" /> does not carry.
    /// <para>
    /// The middle is given whatever height is left rather than what it asked for, which is what lets one
    /// region scroll while the header and footer stay put.
    /// </para>
    /// </remarks>
    public class Dock : Element
    {
        private readonly ElementCollection _children;

        private Element _top;
        private Element _fill;
        private Element _bottom;

        public Dock()
        {
            _children = new ElementCollection(this);
        }

        public Element Top
        {
            get { return _top; }
            set { _top = Adopt(_top, value); }
        }

        /// <summary>The middle. Gets the height the other two did not take.</summary>
        public Element Fill
        {
            get { return _fill; }
            set { _fill = Adopt(_fill, value); }
        }

        public Element Bottom
        {
            get { return _bottom; }
            set { _bottom = Adopt(_bottom, value); }
        }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            float used = 0f;
            float width = 0f;

            if (_top != null)
            {
                _top.Measure(g, available);
                used += _top.DesiredSize.Y;
                width = Wider(width, _top.DesiredSize.X);
            }

            if (_bottom != null)
            {
                _bottom.Measure(g, available);
                used += _bottom.DesiredSize.Y;
                width = Wider(width, _bottom.DesiredSize.X);
            }

            if (_fill != null)
            {
                float left = available.Y - used;
                _fill.Measure(g, new Vector2(available.X, left > 0f ? left : 0f));
                width = Wider(width, _fill.DesiredSize.X);
            }

            // The whole height on offer, not the sum of the parts. Asking for less would let a parent
            // shrink this to fit its contents, and then the middle would have nothing spare to fill.
            return new Vector2(width, available.Y);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            float top = bounds.Y;
            float bottom = bounds.Y + bounds.Height;

            if (_top != null)
            {
                float height = _top.DesiredSize.Y;
                _top.Arrange(new RectangleF(bounds.X, top, bounds.Width, height));
                top += height;
            }

            if (_bottom != null)
            {
                float height = _bottom.DesiredSize.Y;
                _bottom.Arrange(new RectangleF(bounds.X, bottom - height, bounds.Width, height));
                bottom -= height;
            }

            if (_fill != null)
            {
                float height = bottom - top;
                _fill.Arrange(new RectangleF(bounds.X, top, bounds.Width, height > 0f ? height : 0f));
            }
        }

        protected override void DrawContent(IGraphics g)
        {
            // The middle first, so chrome drawn over it wins where they overlap by a pixel.
            if (_fill != null)
                _fill.Draw(g);

            if (_top != null)
                _top.Draw(g);

            if (_bottom != null)
                _bottom.Draw(g);
        }

        protected override Element UnderContent(Vector2 point)
        {
            Element found = _bottom != null ? _bottom.Under(point) : null;

            if (found == null && _top != null)
                found = _top.Under(point);

            if (found == null && _fill != null)
                found = _fill.Under(point);

            return found;
        }

        private static float Wider(float a, float b)
        {
            return a > b ? a : b;
        }

        private Element Adopt(Element current, Element incoming)
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
