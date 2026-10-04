using Lumel;
using Lumel.Drawing;
using Sprocket.Host.SpaceEngineers.Features.Look;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Home
{
    /// <summary>A line down a gutter that turns in towards what stands beside it at the middle of <see cref="Ends" />, as a branch's rail does on a sequence.</summary>
    /// <remarks>The turn is drawn past its own width, into the room between the gutter and what it holds.</remarks>
    public class Rail : Element
    {
        const float Foot = 12f;

        /// <summary>The element the rail ends beside, or null for a line the whole height of the rail.</summary>
        public Element Ends { get; set; }

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return Vector2.Zero;
        }

        /// <inheritdoc />
        protected override void DrawContent(IGraphics g)
        {
            var width = Bounds.Width;
            if (Ends == null || !Ends.IsVisible)
            {
                g.Fill(Bounds, Palette.Line);
                return;
            }

            var middle = Ends.Bounds.Y + Ends.Bounds.Height * 0.5f;
            g.Fill(new RectangleF(Bounds.X, Bounds.Y, width, middle + width - Bounds.Y), Palette.Line);
            g.Fill(new RectangleF(Bounds.X, middle, Foot, width), Palette.Line);
        }
    }
}
