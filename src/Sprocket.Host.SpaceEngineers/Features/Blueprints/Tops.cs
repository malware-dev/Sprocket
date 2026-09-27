using Sprocket.Host.SpaceEngineers.Ui.Layout;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Blueprints
{
    /// <summary>Lists side by side, each standing against the top however long the others are.</summary>
    /// <remarks>Two lists read down from the same line, so the shorter one is not moved down to the middle of the longer.</remarks>
    public class Tops : HStack
    {
        /// <inheritdoc />
        protected override void ArrangeContent(RectangleF bounds)
        {
            base.ArrangeContent(bounds);

            for (var i = 0; i < Children.Count; i++)
            {
                var child = Children[i];
                if (child.IsVisible && child.Bounds.Y > bounds.Y)
                    child.Arrange(new RectangleF(child.Bounds.X, bounds.Y, child.Bounds.Width, child.Bounds.Height));
            }
        }
    }
}
