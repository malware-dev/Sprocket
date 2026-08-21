using VRageMath;

namespace Phosphor
{
    /// <summary>
    /// Puts its children in a line, down the screen or across it.
    /// <para>
    /// The only container to start with, because it is the one every other layout turns out to be a special
    /// case of, and because a second one written before the first has a consumer would be guesswork. Give it
    /// a <see cref="Element.Background" /> and it is a panel; set <see cref="Element.ClipsContent" /> and it
    /// is the beginnings of a scrolling one.
    /// </para>
    /// </summary>
    public class Stack : Element
    {
        private readonly ElementCollection _children;

        public Stack()
        {
            _children = new ElementCollection(this);
        }

        public ElementCollection Children
        {
            get { return _children; }
        }

        /// <summary>Down the screen unless told otherwise, which is what a list of anything wants.</summary>
        public bool Horizontal { get; set; }

        /// <summary>Blank room left between one child and the next.</summary>
        public float Spacing { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            float along = 0f;
            float across = 0f;

            for (int i = 0; i < _children.Count; i++)
            {
                Element child = _children[i];

                // Each child is offered what is left along the line and all of it across, so a child that
                // wraps its text knows the width it has to wrap into.
                Vector2 room = Horizontal
                    ? new Vector2(available.X - along, available.Y)
                    : new Vector2(available.X, available.Y - along);

                if (room.X < 0f)
                    room.X = 0f;
                if (room.Y < 0f)
                    room.Y = 0f;

                child.Measure(g, room);

                float childAlong = Horizontal ? child.DesiredSize.X : child.DesiredSize.Y;
                float childAcross = Horizontal ? child.DesiredSize.Y : child.DesiredSize.X;

                if (i > 0)
                    along += Spacing;

                along += childAlong;

                if (childAcross > across)
                    across = childAcross;
            }

            return Horizontal ? new Vector2(along, across) : new Vector2(across, along);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            float cursor = Horizontal ? bounds.X : bounds.Y;

            for (int i = 0; i < _children.Count; i++)
            {
                Element child = _children[i];

                if (i > 0)
                    cursor += Spacing;

                if (Horizontal)
                {
                    // Across the line a child is given the whole width, which is what makes a row of them
                    // line up rather than each sitting at its own height.
                    child.Arrange(new RectangleF(cursor, bounds.Y, child.DesiredSize.X, bounds.Height));
                    cursor += child.DesiredSize.X;
                }
                else
                {
                    child.Arrange(new RectangleF(bounds.X, cursor, bounds.Width, child.DesiredSize.Y));
                    cursor += child.DesiredSize.Y;
                }
            }
        }

        protected override void DrawContent(IGraphics g)
        {
            // In the order they were added, so a later child covers an earlier one. Children that fell
            // outside are drawn anyway and cut by the clip, which is cheaper than working out which.
            for (int i = 0; i < _children.Count; i++)
                _children[i].Draw(g);
        }
    }
}
