using Lumel.Drawing;
using Lumel.Layout;
using VRageMath;

namespace Lumel.Images
{
    /// <summary>A picture, drawn across the whole of its bounds inside its padding.</summary>
    /// <remarks>
    ///     Given a size both ways by its placement, it is that size. Given one side, it takes the other from the
    ///     picture's shape. Given neither, it is as big as fits the room it is offered in that shape, or as big as
    ///     the picture was made where the room is unbounded both ways.
    /// </remarks>
    public class Image : Element, IPadded
    {
        Thickness _padding;
        IImageSource _source;

        /// <summary>An image drawing nothing yet, in its own colors.</summary>
        public Image()
        {
            Tint = Color.White;
        }

        /// <summary>What is drawn, or null for nothing.</summary>
        public IImageSource Source
        {
            get { return _source; }
            set
            {
                if (_source == value)
                    return;

                _source = value;
                InvalidateMeasure();
            }
        }

        /// <summary>The color the picture is multiplied by; white draws it as it is.</summary>
        public Color Tint { get; set; }

        /// <summary>Room held clear between the edge and the picture, which is drawn across within.</summary>
        public Thickness Padding
        {
            get { return _padding; }
            set
            {
                if (_padding == value)
                    return;

                _padding = value;
                InvalidateMeasure();
            }
        }

        /// <inheritdoc />
        protected override Thickness Inset => _padding;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (_source == null)
                return Vector2.Zero;

            var size = _source.Size;
            var sized = Placement as ISized;
            var wide = sized != null && !float.IsNaN(sized.Width);
            var high = sized != null && !float.IsNaN(sized.Height);

            if (wide && high)
                return available;

            if (wide)
                return new Vector2(available.X, Shape.HeightFor(size, available.X));

            if (high)
                return new Vector2(Shape.WidthFor(size, available.Y), available.Y);

            var acrossBounded = available.X < Unbounded;
            var downBounded = available.Y < Unbounded;

            if (!acrossBounded && !downBounded)
                return size;

            var across = acrossBounded ? available.X : Shape.WidthFor(size, available.Y);
            var down = Shape.HeightFor(size, across);
            if (downBounded && down > available.Y)
            {
                down = available.Y;
                across = Shape.WidthFor(size, down);
            }

            return new Vector2(across, down);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            _source?.Draw(g, ContentBounds, Tint, 0f);
        }
    }
}
