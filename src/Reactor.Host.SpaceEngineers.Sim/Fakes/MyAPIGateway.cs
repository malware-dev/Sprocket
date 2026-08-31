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
    /// Two members deep, and that is the whole of it. The real gateway is enormous; what reaches the editor
    /// is the viewport size, once a frame.
    /// </summary>
    public static class MyAPIGateway
    {
        public static IMySession Session;
    }
}
