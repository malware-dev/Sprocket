using System;
using System.Collections.Generic;
using System.Text;
using Reactor.Sim.Simulation;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

namespace Sandbox.ModAPI
{
    public static class MyScreenSpaceText
    {
        public static void DrawPixels(string font, string text, Vector2 position, float scale, Color color,
            VRage.Utils.MyGuiDrawAlignEnum align, float maxWidth, MyScreenSpaceLayer layer)
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
