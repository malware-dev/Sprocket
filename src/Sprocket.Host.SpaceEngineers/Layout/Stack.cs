using VRageMath;
using Sprocket.Host.SpaceEngineers.Drawing;

namespace Sprocket.Host.SpaceEngineers.Layout
{
    /// <summary>Puts its children in a line, down the screen or across it.</summary>
    /// <remarks>
    /// Give it a <see cref="Element.Background" /> and it is a panel; set
    /// <see cref="Element.ClipsContent" /> and it is the beginnings of a scrolling one.
    /// </remarks>
    public class Stack : Element
    {
        private bool _horizontal;
        private float _spacing;

        private readonly ElementCollection _children;

        public Stack()
        {
            _children = new ElementCollection(this);
        }

        public ElementCollection Children => _children;

        /// <summary>Down the screen unless told otherwise, which is what a list of anything wants.</summary>
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

        /// <summary>Blank room left between one child and the next.</summary>
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
            float along = 0f;
            float across = 0f;
            bool placed = false;

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

                // A collapsed child takes no room and no spacing either. Spacing counted from whether
                // anything has actually been placed rather than from the index, or a collapsed first
                // child would leave a gap where it used to be.
                if (child.Collapsed)
                    continue;

                float childAlong = Horizontal ? child.DesiredSize.X : child.DesiredSize.Y;
                float childAcross = Horizontal ? child.DesiredSize.Y : child.DesiredSize.X;

                if (placed)
                    along += Spacing;

                along += childAlong;
                placed = true;

                if (childAcross > across)
                    across = childAcross;
            }

            return Horizontal ? new Vector2(along, across) : new Vector2(across, along);
        }

        protected override void ArrangeContent(RectangleF bounds)
        {
            float cursor = Horizontal ? bounds.X : bounds.Y;
            bool placed = false;

            for (int i = 0; i < _children.Count; i++)
            {
                Element child = _children[i];

                if (child.Collapsed)
                    continue;

                if (placed)
                    cursor += Spacing;

                placed = true;

                if (Horizontal)
                {
                    // Across the line a child sits on the middle rather than being stretched to the
                    // height of the tallest. A row is a line of words and controls of different sizes -
                    // a switch beside a name, a light beside a sentence - and hanging the short ones from
                    // the top makes them read as superscript rather than as part of the line.
                    float height = child.DesiredSize.Y;
                    float y = height > 0f && height < bounds.Height
                        ? bounds.Y + (bounds.Height - height) * 0.5f
                        : bounds.Y;

                    child.Arrange(new RectangleF(
                        cursor, y, child.DesiredSize.X, height > 0f ? height : bounds.Height));

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

        protected override Element UnderContent(Vector2 point)
        {
            return OnTop(_children, point);
        }
    }
}
