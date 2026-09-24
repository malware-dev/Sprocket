using Sprocket.Host.SpaceEngineers.Ui.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Ui.Controls
{
    /// <summary>The handle a row is dragged by: six small squares, two across and three down.</summary>
    public class Grip : Element
    {
        const int Across = 2;
        const int Down = 3;

        /// <summary>How big one square is.</summary>
        const float Square = 3f;

        const float Gap = 3f;

        /// <summary>Makes a grip drawn in <paramref name="color" />.</summary>
        public Grip(Color color)
        {
            Color = color;
        }

        /// <summary>The colour of its squares.</summary>
        public Color Color { get; set; }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(
                Across * Square + (Across - 1) * Gap,
                Down * Square + (Down - 1) * Gap);
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var width = Across * Square + (Across - 1) * Gap;
            var height = Down * Square + (Down - 1) * Gap;

            var left = ContentBounds.X + (ContentBounds.Width - width) * 0.5f;
            var top = ContentBounds.Y + (ContentBounds.Height - height) * 0.5f;

            for (var x = 0; x < Across; x++)
            for (var y = 0; y < Down; y++)
            {
                g.DrawSprite(Solid, new RectangleF(
                    left + x * (Square + Gap), top + y * (Square + Gap), Square, Square), Color, 0f);
            }
        }
    }
}