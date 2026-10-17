using Lumel;
using Lumel.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>A rectangle of one color, shown so that the color itself is the answer.</summary>
    public class Swatch : Element
    {
        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return Vector2.Zero;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
        }
    }
}
