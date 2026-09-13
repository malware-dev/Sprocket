using System.Text;
using Reactor.Sim.Simulation;
using VRage.Utils;
using VRageMath;

namespace Sandbox.ModAPI
{
    public static class MyScreenSpaceText
    {
        public static void DrawPixels(string font, string text, Vector2 position, float scale, Color color,
            MyGuiDrawAlignEnum align, float maxWidth, MyScreenSpaceLayer layer)
        {
            ScreenSpaceDevice.Current.DrawText(font, text, position, scale, color, align, maxWidth, layer);
        }

        public static Vector2 MeasurePixels(string font, StringBuilder text, float scale)
        {
            return ScreenSpaceDevice.Current.MeasureText(font, text, scale);
        }

        public static float GetLineHeight(string font, float scale)
        {
            return ScreenSpaceDevice.Current.LineHeight(font, scale);
        }
    }
}
