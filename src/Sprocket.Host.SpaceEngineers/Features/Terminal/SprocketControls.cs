using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Features.Terminal
{
    /// <summary>
    /// The button on a programmable block that opens Sprocket, set apart by a separator on each side, and in developer mode the label on the block keeping the automation.
    /// </summary>
    public static class SprocketControls
    {
        const string CustomData = "CustomData";

        // Static because every block of the type asks on its first frame and the controls are added once for the world;
        // the game clears every control as a world unloads, and the mod is compiled again for the next, so this starts
        // over with it.
        static bool _done;

        static IMyTerminalControlSeparator _above;
        static IMyTerminalControlButton _open;
        static IMyTerminalControlLabel _hero;
        static IMyTerminalControlSeparator _below;

        /// <summary>
        /// Makes the button once, in <paramref name="texts" />, which the game keeps for the session; and where
        /// <paramref name="developer" /> is set, the label that marks the block keeping the automation.
        /// </summary>
        public static void DoOnce(Texts texts, bool developer)
        {
            if (_done)
                return;

            _done = true;

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

            // Shown on the block whose storage holds the program: the hero on the server, and on a client as the server tells it.
            if (developer)
            {
                _hero = MyAPIGateway.TerminalControls
                    .CreateControl<IMyTerminalControlLabel, IMyProgrammableBlock>("Sprocket_Hero");

                _hero.Label = MyStringId.GetOrCompute(texts.TerminalHero);
                _hero.Visible = BlockStorage.HasProgram;
                MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(_hero);
            }

            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(_below);
        }

        /// <summary>
        /// Moves the button and its separators to stand under Custom Data, in the list of controls a
        /// programmable block is about to show.
        /// </summary>
        /// <remarks>
        /// Called as the terminal's <c>CustomControlGetter</c> each time a block's controls are about to be shown; see THDigi's
        /// terminal control examples.
        /// </remarks>
        public static void Arrange(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (_open == null || !(block is IMyProgrammableBlock))
                return;

            controls.Remove(_above);
            controls.Remove(_open);
            controls.Remove(_below);
            if (_hero != null)
                controls.Remove(_hero);

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
            if (_hero != null)
                controls.Insert(at, _hero);

            controls.Insert(at, _open);
            controls.Insert(at, _above);
        }
    }
}
