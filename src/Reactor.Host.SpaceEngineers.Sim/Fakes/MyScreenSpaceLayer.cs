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
