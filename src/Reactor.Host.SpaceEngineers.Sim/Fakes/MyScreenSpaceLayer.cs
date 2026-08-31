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
    /// <summary>
    /// Which of the game's own layers a draw belongs to. Ordering between layers is the game's business and
    /// the simulator honours it by drawing them in this order.
    /// </summary>
    public enum MyScreenSpaceLayer
    {
        BelowHud,
        AboveHud,
        AboveEverything
    }
}
