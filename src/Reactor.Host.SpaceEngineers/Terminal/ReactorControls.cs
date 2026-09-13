using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.Utils;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Terminal
{
    /// <summary>
    /// The button on a programmable block that opens Reactor.
    /// </summary>
    /// <remarks>
    /// Controls are global per block type rather than per block, so this runs exactly once per mod load,
    /// driven off the first programmable block to come alive. See <see cref="ReactorBlock" /> for why it
    /// cannot be done any earlier than that.
    /// </remarks>
    static class ReactorControls
    {
        static bool _done;

        public static void DoOnce()
        {
            if (_done)
                return;

            _done = true;

            IMyTerminalControlButton open = MyAPIGateway.TerminalControls
                .CreateControl<IMyTerminalControlButton, IMyProgrammableBlock>("Reactor_Open");

            open.Title = MyStringId.GetOrCompute("Automation");
            open.Tooltip = MyStringId.GetOrCompute("Open the automation editor.");
            open.SupportsMultipleBlocks = false;
            open.Visible = block => true;
            open.Enabled = block => true;
            open.Action = ReactorSession.Open;

            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(open);
        }
    }
}
