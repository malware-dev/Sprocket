using Lumel.Drawing;
using Lumel.Layout;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Selector
{
    /// <summary>A list that hangs open, as wide as its last child and no wider, whatever the children above it would take.</summary>
    public class Menu : VStack
    {
        /// <inheritdoc />
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            if (Children.Count == 0)
                return base.MeasureContent(g, available);

            var entries = Children[Children.Count - 1];
            entries.Measure(g, available);

            return base.MeasureContent(g, new Vector2(System.Math.Min(entries.DesiredSize.X, available.X), available.Y));
        }
    }
}
