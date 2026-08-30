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
    /// Two members deep, and that is the whole of it. The real gateway is enormous; what reaches Phosphor
    /// is the viewport size, once a frame.
    /// </summary>
    public static class MyAPIGateway
    {
        public static IMySession Session;
    }
}
