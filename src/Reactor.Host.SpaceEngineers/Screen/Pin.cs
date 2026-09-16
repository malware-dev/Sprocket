using VRageMath;
using Reactor.Host.SpaceEngineers.Drawing;
using Reactor.Host.SpaceEngineers.Layout;

namespace Reactor.Host.SpaceEngineers.Screen
{
    /// <summary>An element that is nothing but a rectangle somebody put somewhere.</summary>
    public sealed class Pin : Element
    {
        protected override Vector2 MeasureContent(IGraphics g, Vector2 available)
        {
            return Vector2.Zero;
        }

        protected override void DrawContent(IGraphics g)
        {
        }
    }
}
