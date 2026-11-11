using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Input;
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
            var editor = InGameEditor.Of(block);
            if (editor == null)
            {
                _report.Fail("editor open", "Sprocket keeps no editor for the block it opened on");
                yield break;
            }

            foreach (var stop in new[] { new[] { "home" }, new[] { "library", "Library" }, new[] { "logic system", "Kept Here" }, new[] { "installation panel", "Kept Here", "Installation panel" }, new[] { "slots switched off", "Kept Here", "off", "Installation panel" }, new[] { "optimize slots asks", "Kept Here", "Installation panel", "Optimize slots" }, new[] { "heading renaming", "Kept Here", "Kept Here" }, new[] { "blueprint made", "Make blueprint" }, new[] { "strip copy tip", "Library", "hover:Moves.Copies" }, new[] { "strip to library tip", "Library", "hover:Moves.ToLibrary" }, new[] { "strip to construct tip", "Library", "hover:Moves.ToConstruct" } })
            {
                editor.Home();
                yield return PluginUpdates.PerSecond / 2;
                var pressed = true;
                var hovering = false;
                for (var i = 1; i < stop.Length && pressed; i++)
                {
                    hovering = stop[i].StartsWith("hover:");
                    pressed = hovering ? editor.Hover(stop[i].Substring(6)) : stop[i] == "off" ? editor.Switch(false) : stop[i] == "answer" ? editor.Answer() : editor.Press(stop[i]);
                    if (!pressed)
                        _report.Fail($"editor {stop[0]}", $"no button {stop[i]}; the page offers {editor.Buttons()}");

                    yield return PluginUpdates.PerSecond / 2;
                }

                if (!pressed)
                    continue;

                var shot = Shoot("editor-" + stop[0].Replace(" ", "-"), !hovering);
                while (shot.MoveNext())
                    yield return shot.Current;
            }
        }

        /// <summary>Takes a picture called <paramref name="name" />, with the mouse in the screen's corner so no tip is open in it where <paramref name="park" /> holds, and otherwise once the tip of what it rests on has had time to open.</summary>
        IEnumerator<int> Shoot(string name, bool park)
        {
            if (park)
                MyInput.Static.SetMousePosition(0, 0);

            yield return 2 * PluginUpdates.PerSecond;

            var picture = Path.Combine(_shots, name + ".png");
            if (File.Exists(picture))
                File.Delete(picture);

            MyRenderProxy.TakeScreenshot(Vector2.One, picture, false, false, false);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check(name, File.Exists(picture), File.Exists(picture) ? picture : $"no picture at {picture}");
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
