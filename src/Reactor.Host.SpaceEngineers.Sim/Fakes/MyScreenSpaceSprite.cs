using System;
using System.Collections.Generic;
using System.Text;
using Reactor.Sim.Simulation;
using VRageMath;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

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
