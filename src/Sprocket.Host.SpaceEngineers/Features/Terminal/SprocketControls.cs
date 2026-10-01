using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Terminal
{
    /// <summary>
    /// The button on a programmable block that opens Sprocket, set apart by a separator on each side.
    /// </summary>
    /// <remarks>
    /// Controls are global per block type rather than per block, so this runs exactly once per mod load,
    /// driven off the first programmable block to come alive. See <see cref="SprocketBlock" /> for why it
    /// cannot be done any earlier than that.
    /// <para>
    /// Static because the controls are: one set per block type for the whole game, which the terminal
    /// hands back through a callback that carries no state of the mod's. They die with the mod's load.
    /// </para>
    /// </remarks>
    static class SprocketControls
    {
        const string CustomData = "CustomData";

        static bool _done;

        static IMyTerminalControlSeparator _above;
        static IMyTerminalControlButton _open;
        static IMyTerminalControlSeparator _below;

        /// <summary>Makes the button once, in <paramref name="texts" />, which the game keeps for the session.</summary>
        public static void DoOnce(Texts texts)
        {
            if (_done)
                return;

            _done = true;

            // Added like any control, so other mods that read a block's controls see these too; where
            // they are shown is settled afterwards, in Arrange.
            _above = MyAPIGateway.TerminalControls
                .CreateControl<IMyTerminalControlSeparator, IMyProgrammableBlock>("Sprocket_Above");

            _open = MyAPIGateway.TerminalControls
                .CreateControl<IMyTerminalControlButton, IMyProgrammableBlock>("Sprocket_Open");

            _open.Title = MyStringId.GetOrCompute(texts.TerminalOpen);
            _open.Tooltip = MyStringId.GetOrCompute(texts.TerminalOpenAbout);
            _open.SupportsMultipleBlocks = false;
            _open.Visible = _ => true;
            _open.Enabled = _ => true;
            _open.Action = SprocketSession.Open;

            _below = MyAPIGateway.TerminalControls
                .CreateControl<IMyTerminalControlSeparator, IMyProgrammableBlock>("Sprocket_Below");

            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(_above);
            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(_open);
            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(_below);
        }

        /// <summary>
        /// Moves the button and its separators to stand under Custom Data, in the list of controls a
        /// programmable block is about to show.
        /// </summary>
        /// <remarks>
        /// The terminal's <c>CustomControlGetter</c>, which hands over the list each time a block's
        /// controls are about to be shown - see THDigi's terminal control examples. Only the list moves;
        /// the controls stay as they were added. Where Custom Data is missing, the three go last.
        /// </remarks>
        public static void Arrange(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (_open == null || !(block is IMyProgrammableBlock))
                return;

            controls.Remove(_above);
            controls.Remove(_open);
            controls.Remove(_below);

            var at = -1;
            for (var i = 0; i < controls.Count; i++)
            {
                if (controls[i].Id == CustomData)
                {
                    at = i + 1;
                    break;
                }
            }

            if (at < 0)
                at = controls.Count;

            controls.Insert(at, _below);
            controls.Insert(at, _open);
            controls.Insert(at, _above);
        }
    }
}
