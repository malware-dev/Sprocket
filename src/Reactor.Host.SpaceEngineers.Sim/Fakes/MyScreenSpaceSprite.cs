using Reactor.Sim.Simulation;
using VRageMath;

namespace Sandbox.ModAPI
{
    public static class MyScreenSpaceSprite
    {
        public static void DrawPixels(VRage.Utils.MyStringId material, Rectangle destination, Rectangle? source,
            Color color, float rotation, MyScreenSpaceLayer layer)
        {
            ScreenSpaceDevice.Current.DrawSprite(material, destination, source, color, rotation, layer);
        }
    }
}
