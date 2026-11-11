using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRageMath;
using VRageRender;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Sprocket's editor opened in the game client on a pasted programmable block, by the Automation button of its control panel,
    /// and a picture of the screen it opens on, for the visual rows of the Testing artifact.
    /// </summary>
    public sealed class EditorShotClientTest
    {
        // The id of Sprocket's button in a programmable block's control panel (SprocketControls).
        const string OpenButton = "Sprocket_Open";

        readonly string _shots;
        readonly GameTestReport _report;

        /// <summary>The test, writing pictures to <paramref name="shots" /> and answering into <paramref name="report" />.</summary>
        public EditorShotClientTest(string shots, GameTestReport report)
        {
            _shots = shots;
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        /// <remarks>Opens the editor on the block called <paramref name="name" />, which an earlier test pasted.</remarks>
        public IEnumerator<int> Run(string name)
        {
            var block = Named(name);
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyProgrammableBlock>(out controls);
            var open = controls.Find(control => control.Id == OpenButton) as IMyTerminalControlButton;
            if (block == null || open == null)
            {
                _report.Fail("editor open", block == null ? $"no block called {name}" : "the programmable block has no Automation button");
                yield break;
            }

            open.Action(block);
            yield return 2 * PluginUpdates.PerSecond;

            var picture = Path.Combine(_shots, "editor-home.png");
            if (File.Exists(picture))
                File.Delete(picture);

            MyRenderProxy.TakeScreenshot(Vector2.One, picture, false, false, false);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("editor open", File.Exists(picture), File.Exists(picture) ? picture : $"no picture at {picture}");
        }

        static IMyTerminalBlock Named(string name)
        {
            var found = new List<IMyTerminalBlock>();
            var grids = new HashSet<VRage.ModAPI.IMyEntity>();
            MyAPIGateway.Entities.GetEntities(grids, entity => entity is VRage.Game.ModAPI.IMyCubeGrid);
            foreach (VRage.Game.ModAPI.IMyCubeGrid grid in grids)
            {
                var blocks = new List<VRage.Game.ModAPI.IMySlimBlock>();
                grid.GetBlocks(blocks, slim => (slim.FatBlock as IMyTerminalBlock)?.CustomName == name);
                foreach (var slim in blocks)
                    found.Add((IMyTerminalBlock)slim.FatBlock);
            }

            return found.Count > 0 ? found[0] : null;
        }
    }
}
