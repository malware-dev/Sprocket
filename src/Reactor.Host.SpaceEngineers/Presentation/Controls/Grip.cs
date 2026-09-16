using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Graphics;
using Reactor.Host.SpaceEngineers.Presentation.Ui;

namespace Reactor.Host.SpaceEngineers.Presentation.Controls
{
    /// <summary>
    /// The handle a row is dragged by: six small squares, two across and three down.
    /// </summary>
    /// <remarks>
    /// Drawn rather than fetched. The game has no grip of its own to borrow - nothing in it is dragged
    /// the way a step in a sequence is - and six rectangles are crisp at any size where a picture
    /// squeezed to eleven pixels across would be six grey smudges.
    /// </remarks>
    public class Grip : Element
    {
        const int Across = 2;
        const int Down = 3;

        /// <summary>How big one square is, and how far apart they sit.</summary>
        const float Square = 3f;

        const float Gap = 3f;

        public Grip(Color color)
        {
            Color = color;
        }

        public Color Color { get; set; }

        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return new Vector2(
                Across * Square + (Across - 1) * Gap,
                Down * Square + (Down - 1) * Gap);
        }

        protected override void DrawContent(IGraphics g)
        {
            float width = Across * Square + (Across - 1) * Gap;
            float height = Down * Square + (Down - 1) * Gap;

            float left = ContentBounds.X + (ContentBounds.Width - width) * 0.5f;
            float top = ContentBounds.Y + (ContentBounds.Height - height) * 0.5f;

            for (int x = 0; x < Across; x++)
                for (int y = 0; y < Down; y++)
                    g.DrawSprite(Solid, new RectangleF(
                        left + x * (Square + Gap), top + y * (Square + Gap), Square, Square), Color, 0f);
        }
    }
}
