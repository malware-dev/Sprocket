using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// Something on the left, something on the right, and whatever is in the middle taking the slack.
    /// <para>
    /// <see cref="Dock" /> lying down, and it exists for the same reason: a header wants back on one side
    /// and a switch on the other with the title between them, and a footer wants the navigation on one
    /// side and the way forward on the other. A stack cannot do that, because a stack gives every child
    /// exactly what it asked for and there is no way to say "and you take the rest".
    /// </para>
    /// <para>
    /// The middle is measured against what the other two left behind, so a long title gives way to the
    /// controls rather than pushing one of them off the end - which is the failure worth designing
    /// against, because a control that has been pushed out of sight leaves nothing to say it existed.
    /// </para>
    /// </summary>
    public class Bar : Element
    {
        private float _spacing;

        private readonly ElementCollection _children;

        private Element _left;
        private Element _fill;
        private Element _right;

        public Bar()
        {
            _children = new ElementCollection(this);
        }

        public Element Left
        {
            get { return _left; }
            set { _left = Adopt(_left, value); }
        }

        /// <summary>The middle. Gets the width the other two did not take.</summary>
        public Element Fill
        {
            get { return _fill; }
            set { _fill = Adopt(_fill, value); }
        }

        public Element Right
        {
            get { return _right; }
            set { _right = Adopt(_right, value); }
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

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            float used = 0f;
            float height = 0f;

            // A collapsed part is skipped along with its spacing, the way a stack skips one. Counting the
            // spacing anyway would hold open a gap the width of the room between two controls, on every
            // row that happens not to have the control - which is the reserved emptiness this layout is
            // supposed to be rid of.
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
                float left = available.X - used;
                _fill.Measure(g, new Vector2(left > 0f ? left : 0f, available.Y));
                height = Taller(height, _fill.DesiredSize.Y);
            }

            // The whole width on offer, because the point of this is to span it.
            return new Vector2(available.X, height);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            float left = bounds.X;
            float right = bounds.X + bounds.Width;

            if (Shows(_left))
            {
                float width = _left.DesiredSize.X;
                _left.Arrange(Centred(left, bounds, width, _left.DesiredSize.Y));
                left += width + Spacing;
            }

            if (Shows(_right))
            {
                float width = _right.DesiredSize.X;
                _right.Arrange(Centred(right - width, bounds, width, _right.DesiredSize.Y));
                right -= width + Spacing;
            }

            if (_fill != null)
            {
                float width = right - left;
                _fill.Arrange(Centred(left, bounds, width > 0f ? width : 0f, _fill.DesiredSize.Y));
            }
        }

        protected override void DrawContent(IGraphics g)
        {
            if (_fill != null)
                _fill.Draw(g);

            if (_left != null)
                _left.Draw(g);

            if (_right != null)
                _right.Draw(g);
        }

        /// <summary>Whether a part is there at all and is asking to be seen.</summary>
        private static bool Shows(Element part)
        {
            return part != null && !part.Collapsed;
        }

        /// <summary>
        /// Vertically centred against the bar rather than stretched to it, so a small control beside a
        /// large one sits on its middle instead of on its top edge.
        /// </summary>
        private static RectangleF Centred(float x, RectangleF bounds, float width, float height)
        {
            if (height > bounds.Height || height <= 0f)
                return new RectangleF(x, bounds.Y, width, bounds.Height);

            return new RectangleF(x, bounds.Y + (bounds.Height - height) * 0.5f, width, height);
        }

        private static float Taller(float a, float b)
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
