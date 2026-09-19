using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Terminal
{
    /// <summary>
    /// The button on a programmable block that opens Sprocket.
    /// </summary>
    /// <remarks>
    /// Controls are global per block type rather than per block, so this runs exactly once per mod load,
    /// driven off the first programmable block to come alive. See <see cref="SprocketBlock" /> for why it
    /// cannot be done any earlier than that.
    /// </remarks>
    static class SprocketControls
    {
        static bool _done;

        public static void DoOnce()
        {
            if (_done)
                return;

            _done = true;

            IMyTerminalControlButton open = MyAPIGateway.TerminalControls
                .CreateControl<IMyTerminalControlButton, IMyProgrammableBlock>("Sprocket_Open");

            open.Title = MyStringId.GetOrCompute("Automation");
            open.Tooltip = MyStringId.GetOrCompute("Open the automation editor.");
            open.SupportsMultipleBlocks = false;
            open.Visible = _ => true;
            open.Enabled = _ => true;
            open.Action = SprocketSession.Open;

            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(open);
        }
    }
}
