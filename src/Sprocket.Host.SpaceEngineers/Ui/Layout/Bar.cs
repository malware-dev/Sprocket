using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Layout
{
    /// <summary>
    ///     Something on the left, something on the right, and whatever is in the middle taking the slack.
    /// </summary>
    /// <remarks>
    ///     <see cref="Dock" /> lying down. A stack cannot do this, because it gives every child exactly what
    ///     it asked for and there is no way to say "and this one takes the rest".
    ///     <para>
    ///         The middle is measured against what the other two left behind, so a long title gives way to the
    ///         controls rather than pushing one off the end, where nothing would be left to say it existed.
    ///     </para>
    /// </remarks>
    public class Bar : Element
    {
        readonly ElementCollection _children;
        Element _fill;

        Element _left;
        Element _right;
        float _spacing;
        bool _tops;

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

        /// <summary>
        ///     Whether this reports what its parts need rather than the whole width on offer.
        /// </summary>
        /// <remarks>
        ///     Off by default, because spanning is what a bar is for: a row of a list reaches the edge
        ///     whatever is written in it, so a short name and a long one give rows of the same shape.
        ///     <para>
        ///         On for a bar inside something that is as wide as its contents. A menu holds its words
        ///         already, so how wide it is is a fact about it - and a spanning bar inside one answers
        ///         with whatever it was offered, which makes the menu as wide as whatever asked.
        ///     </para>
        /// </remarks>
        public bool Tight { get; set; }

        /// <summary>
        ///     Whether the parts sit along the top of the bar rather than on its middle.
        /// </summary>
        /// <remarks>
        ///     For a middle that grows downwards - words that wrap - beside a part that must stay where it
        ///     is while they do.
        /// </remarks>
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

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var used = 0f;
            var height = 0f;

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
                var left = available.X - used;
                _fill.Measure(g, new Vector2(left > 0f ? left : 0f, available.Y));
                height = Taller(height, _fill.DesiredSize.Y);
            }

            if (!Tight)
            {
                // The whole width on offer, because the point of this is to span it.
                return new Vector2(available.X, height);
            }

            // What the parts came to. The spacing between them is already in what was used, and the
            // middle is added here because it was measured against what the other two left.
            var wide = used + (_fill != null ? _fill.DesiredSize.X : 0f);
            return new Vector2(wide < available.X ? wide : available.X, height);
        }

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

        protected override void DrawContent(IGraphics g)
        {
            _fill?.Draw(g);
            _left?.Draw(g);
            _right?.Draw(g);
        }

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

        /// <summary>
        ///     Vertically centred against the bar rather than stretched to it, so a small control beside a
        ///     large one sits on its middle instead of on its top edge.
        /// </summary>
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