using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Legacy
{
    /// <summary>Puts its children in a line, down the screen or across it.</summary>
    public class Stack : Box
    {
        bool _horizontal;
        float _spacing;

        /// <summary>An empty stack that runs down.</summary>
        public Stack()
        {
            Children = new Collection(this);
        }

        /// <summary>What is in the line, in order.</summary>
        public Collection Children { get; }

        /// <summary>Whether the children run across rather than down.</summary>
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

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            var along = 0f;
            var across = 0f;
            var placed = false;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];

                var room = Horizontal
                    ? new Vector2(available.X - along, available.Y)
                    : new Vector2(available.X, available.Y - along);

                if (room.X < 0f)
                    room.X = 0f;
                if (room.Y < 0f)
                    room.Y = 0f;

                child.Measure(g, room);

                if (!child.IsVisible)
                    continue;

                var childAlong = Horizontal ? child.DesiredSize.X : child.DesiredSize.Y;
                var childAcross = Horizontal ? child.DesiredSize.Y : child.DesiredSize.X;

                if (placed)
                    along += Spacing;

                along += childAlong;
                placed = true;

                if (childAcross > across)
                    across = childAcross;
            }

            return Horizontal ? new Vector2(along, across) : new Vector2(across, along);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            var cursor = Horizontal ? bounds.X : bounds.Y;
            var placed = false;

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];

                if (!child.IsVisible)
                    continue;

                if (placed)
                    cursor += Spacing;

                placed = true;

                if (Horizontal)
                {
                    var height = child.DesiredSize.Y;
                    var y = height > 0f && height < bounds.Height
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

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            for (var i = 0; i < Children.Count; i++)
                Children[i].Draw(g);
        }

        /// <inheritdoc />
        protected override Element UnderContent(Vector2 point)
        {
            return OnTop(Children, point);
        }
    }
}