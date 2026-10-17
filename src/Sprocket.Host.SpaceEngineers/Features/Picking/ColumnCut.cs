using Lumel;
using Lumel.Drawing;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>A window a column is shown through as it comes out of the way back, only as wide as it has grown.</summary>
    /// <remarks>The column keeps the width it will have on its own, so nothing in it is measured again as the window widens.</remarks>
    public sealed class ColumnCut : ContentControl
    {
        /// <inheritdoc />
        protected override bool Clips => true;

        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (Content == null)
                return Vector2.Zero;

            Content.Measure(g, new Vector2(float.PositiveInfinity, available.Y));
            return new Vector2(0f, Content.DesiredSize.Y);
        }

        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            if (Content != null)
                Content.Arrange(new RectangleF(bounds.X, bounds.Y, Content.DesiredSize.X, bounds.Height));
        }
    }
}
