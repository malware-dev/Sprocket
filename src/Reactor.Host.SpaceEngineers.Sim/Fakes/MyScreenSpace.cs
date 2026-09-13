using Reactor.Sim.Simulation;
using VRageMath;

namespace Sandbox.ModAPI
{
    public static class MyScreenSpace
    {
        /// <summary>
        /// The scope of one scissor rectangle, shaped like the game's. A struct, so that holding one costs no
        /// allocation, and with a flag so that disposing one whose push was refused does nothing.
        /// </summary>
        public struct MyScissorScope : IDisposable
        {
            readonly MyScreenSpaceLayer _layer;
            readonly bool _pushed;

            internal MyScissorScope(MyScreenSpaceLayer layer)
            {
                _layer = layer;
                _pushed = true;
            }

            public void Dispose()
            {
                if (_pushed)
                    ScreenSpaceDevice.Current.PopScissor(_layer);
            }
        }

        /// <summary>
        /// Narrows drawing to a rectangle in window pixels until the scope is disposed, with nested scopes
        /// intersecting. The layer carries the game's own default, which is a default worth not taking:
        /// everything the editor draws names its layer, and a scissor on the wrong layer clips nothing.
        /// </summary>
        public static MyScissorScope UsingScissorRectanglePixels(Rectangle rect,
            MyScreenSpaceLayer layer = MyScreenSpaceLayer.BelowHud)
        {
            return ScreenSpaceDevice.Current.PushScissor(rect, layer)
                ? new MyScissorScope(layer)
                : default(MyScissorScope);
        }

        public static RectangleF GetSafeGuiArea()
        {
            return ScreenSpaceDevice.Current.SafeGuiArea;
        }

        public static Vector2 NormalizedSizeToPixels(Vector2 size)
        {
            return ScreenSpaceDevice.Current.NormalizedSizeToPixels(size);
        }
    }
}
