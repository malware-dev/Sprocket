using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>A band at the top, a band at the bottom, and everything left over in between.</summary>
    public class Dock : Element
    {
        readonly ElementCollection _children;
        Element _bottom;
        Element _fill;

        Element _top;

        /// <summary>An empty dock.</summary>
        public Dock()
        {
            _children = new ElementCollection(this);
        }

        /// <summary>The band at the top, given the height it asks for.</summary>
        public Element Top
        {
            get { return _top; }
            set { _top = Adopt(_top, value); }
        }

        /// <summary>The middle, given the height the other two did not take.</summary>
        public Element Fill
        {
            get { return _fill; }
            set { _fill = Adopt(_fill, value); }
        }

        /// <summary>The band at the bottom, given the height it asks for.</summary>
        public Element Bottom
        {
            get { return _bottom; }
            set { _bottom = Adopt(_bottom, value); }
        }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var used = 0f;
            var width = 0f;

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
                var left = available.Y - used;
                _fill.Measure(g, new Vector2(available.X, left > 0f ? left : 0f));
                width = Wider(width, _fill.DesiredSize.X);
            }

            // The whole height on offer, so that no parent can shrink the middle to fit its contents.
            return new Vector2(width, available.Y);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var top = bounds.Y;
            var bottom = bounds.Y + bounds.Height;

            if (_top != null)
            {
                var height = _top.DesiredSize.Y;
                _top.Arrange(new RectangleF(bounds.X, top, bounds.Width, height));
                top += height;
            }

            if (_bottom != null)
            {
                var height = _bottom.DesiredSize.Y;
                _bottom.Arrange(new RectangleF(bounds.X, bottom - height, bounds.Width, height));
                bottom -= height;
            }

            if (_fill != null)
            {
                var height = bottom - top;
                _fill.Arrange(new RectangleF(bounds.X, top, bounds.Width, height > 0f ? height : 0f));
            }
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            // The middle first, so the bands draw over it where they overlap.
            _fill?.Draw(g);
            _top?.Draw(g);
            _bottom?.Draw(g);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            var found = _bottom?.Under(point);

            if (found == null && _top != null)
                found = _top.Under(point);

            if (found == null && _fill != null)
                found = _fill.Under(point);

            return found;
        }

        static float Wider(float a, float b)
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