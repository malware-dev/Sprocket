using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using SpaceEngineers.Game.ModAPI;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "A button Sprocket listens to reads as wired", run in the real game on a second paste of the Sprocket Piston
    /// Status blueprint: what the game says of Three Button's slot, which Sprocket's program listens to, as pasted and after the
    /// slot is emptied as a player empties it.
    /// </summary>
    public sealed class ButtonWiringGameTest
    {
        readonly string _blueprint;
        readonly GameTestReport _report;

        /// <summary>The test on the scenario's blueprint in <paramref name="blueprints" />, answering into <paramref name="report" />.</summary>
        public ButtonWiringGameTest(string blueprints, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints, "Sprocket Piston Status", "bp.sbc");
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, new Vector3D(900, 0, 0));
            var button = pasted.Find("Three Button") as MyButtonPanel;
            var arrived = pasted.Find("At Three") as IMyFunctionalBlock;
            if (button == null || arrived == null)
            {
                _report.Fail("button wiring blueprint", "Three Button or At Three is missing after the paste");
                yield break;
            }

            var panel = (IMyButtonPanel)button;
            yield return 5 * PluginUpdates.PerSecond;
            _report.Check("a button Sprocket listens to, as pasted", panel.IsButtonAssigned(0),
                $"the game says button 1 is assigned: {panel.IsButtonAssigned(0)}, and the panel reads {(panel.IsButtonAssigned(0) ? "wired" : "not wired")}");

            // Emptied as a player drags the slot's item off the toolbar: Sprocket wires a button once and leaves a slot emptied by
            // hand empty, so the panel reads not wired and a press runs nothing.
            button.Toolbar.SetItemAtIndex(0, null);
            yield return 5 * PluginUpdates.PerSecond;
            arrived.Enabled = false;
            button.ActivateButton(0);
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("a button whose slot was emptied by hand", !panel.IsButtonAssigned(0) && !arrived.Enabled,
                $"5 s after the slot was emptied the game says button 1 is assigned: {panel.IsButtonAssigned(0)}, and a press switched At Three {(arrived.Enabled ? "on" : "nothing on")}");
        }
    }
}
