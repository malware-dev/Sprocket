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
    public static class MyScreenSpaceSprite
    {
        public static void DrawPixels(VRage.Utils.MyStringId material, Rectangle destination, Rectangle? source,
            Color color, float rotation, MyScreenSpaceLayer layer)
        {
            ScreenSpaceDevice.Current.DrawSprite(material, destination, source, color, rotation, layer);
        }
    }
}
