using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox;
using Sandbox.Graphics.GUI;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage;
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

        // The English codex's words for what the tips stops rest on (Codex.resx): the term home's heading holds, the tips switch's
        // tip while tips are on and while they are off, a trigger's bin and the undo arrow.
        const string Term = "Term.LogicSystem";
        const string TipsOn = "Show fewer tips";
        const string TipsOff = "Show every tip";
        const string TriggerBin = "Delete this trigger";
        const string Undo = "[![](mark:undo)";

        // The logic system the editor opens on and its blueprint (KeptCount), the logic system of the construct Destroy takes the
        // programmable block of (PistonStatus), and the library button's Norwegian word (MyTexts.no.resx, Sprocket.Systems.Load).
        const string Kept = "Kept Here";
        const string Backup = "Piston";
        const string NorwegianLibrary = "Bibliotek";

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

            // Each stop is its picture's name and what is pressed from the home screen to reach it. Tips switched off stay off until
            // switched on again, so the stops from "tips switch off" on are seen with tips off.
            var stops = new[]
            {
                new[] { "home" },
                new[] { "library", "Library" },
                new[] { "logic system", "Kept Here" },
                new[] { "installation panel", "Kept Here", "Installation panel" },
                new[] { "slots switched off", "Kept Here", "off", "Installation panel" },
                new[] { "optimize slots asks", "Kept Here", "Installation panel", "Optimize slots" },
                new[] { "heading renaming", "Kept Here", "Kept Here" },
                new[] { "blueprint made", "Make blueprint" },
                new[] { "term tip", "hover:has:" + Term },
                new[] { "tips switch on", "hover:tip:" + TipsOn },
                new[] { "tips switch off", "tip:" + TipsOn, "hover:tip:" + TipsOff },
                new[] { "tips off home" },
                new[] { "tips off term", "hover:has:" + Term },
                new[] { "tips off logic system", "Kept Here" },
                new[] { "tips off sequences", "Kept Here", "Sequences" },
                new[] { "tips off bin", "Kept Here", "Triggers", "hover:tip:" + TriggerBin },
                new[] { "tips off strip to library tip", "Library", "hover:Moves.ToLibrary" },
                new[] { "tips off undo", "Kept Here", "Triggers", "tip:" + TriggerBin, "answer", "hover:tip:" + Undo },
                new[] { "tips on again term", "tip:" + TipsOff, "hover:has:" + Term },
                new[] { "tips on again sequences", "Kept Here", "Sequences" },
                new[] { "term tip pinned", "alt", "hover:has:" + Term }
            };

            var tour = Tour(editor, stops, "");
            while (tour.MoveNext())
                yield return tour.Current;

            editor.HoldAlt(false);

            // A backup is kept of a construct's logic systems as its last programmable block is destroyed.
            var lost = Destroy(block);
            _report.Check("backup made", lost > 0, $"{lost} programmable blocks destroyed on other constructs");
            yield return 2 * PluginUpdates.PerSecond;

            var english = Tour(editor, Strip("Library"), "");
            while (english.MoveNext())
                yield return english.Current;

            var norwegian = Speak(MyLanguagesEnum.Norwegian);
            while (norwegian.MoveNext())
                yield return norwegian.Current;

            var strip = Tour(editor, Strip(NorwegianLibrary), "no ");
            while (strip.MoveNext())
                yield return strip.Current;

            var back = Speak(MyLanguagesEnum.English);
            while (back.MoveNext())
                yield return back.Current;
        }

        /// <summary>
        /// The library's strip, its tips rested on with a logic system on this construct chosen, the blueprint of it, and the backup
        /// <see cref="Destroy" /> made, reached by the library button that says <paramref name="library" />.
        /// </summary>
        static string[][] Strip(string library)
        {
            return new[]
            {
                new[] { "strip copy tip", library, "hover:Moves.Copies" },
                new[] { "strip to library tip", library, "hover:Moves.ToLibrary" },
                new[] { "strip to construct tip", library, "hover:Moves.ToConstruct" },
                new[] { "strip blueprint to construct tip", library, "last:" + Kept, "hover:Moves.ToConstruct" },
                new[] { "strip blueprint to library tip", library, "last:" + Kept, "hover:Moves.ToLibrary" },
                new[] { "strip backup to library tip", library, Backup, "hover:Moves.ToLibrary" },
                new[] { "strip backup to construct tip", library, Backup, "hover:Moves.ToConstruct" }
            };
        }

        /// <summary>
        /// Goes to each of <paramref name="stops" /> from the editor's home screen and takes its picture, named with
        /// <paramref name="prefix" /> before it.
        /// </summary>
        IEnumerator<int> Tour(InGameEditor editor, string[][] stops, string prefix)
        {
            foreach (var stop in stops)
            {
                editor.Home();
                yield return PluginUpdates.PerSecond / 2;
                var name = prefix + stop[0];
                var pressed = true;
                var hovering = false;
                for (var i = 1; i < stop.Length && pressed; i++)
                {
                    hovering = stop[i].StartsWith("hover:");
                    // "answer" answers a question where one is asked, since some presses ask and some go ahead.
                    pressed = hovering ? editor.Hover(stop[i].Substring(6)) : stop[i] == "off" ? editor.Switch(false) : stop[i] == "alt" ? editor.HoldAlt(true) : stop[i] == "answer" ? editor.Answer() || true : editor.Press(stop[i]);
                    if (!pressed)
                        _report.Fail($"editor {name}", $"no button {stop[i]}; the page offers {editor.Buttons()}");

                    yield return PluginUpdates.PerSecond / 2;
                }

                if (!pressed)
                    continue;

                var shot = Shoot("editor-" + name.Replace(" ", "-"), !hovering);
                while (shot.MoveNext())
                    yield return shot.Current;
            }
        }

        /// <summary>Puts the game in <paramref name="language" /> as its options screen does, by the setting and a screen closing after it.</summary>
        /// <remarks>Sprocket looks for a new language as any screen closes. The setting is the test user data's own (Run-ClientTests.ps1 -AppData).</remarks>
        static IEnumerator<int> Speak(MyLanguagesEnum language)
        {
            MySandboxGame.Config.Language = language;
            var box = MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Info, messageText: new StringBuilder(language.ToString()));
            MyGuiSandbox.AddScreen(box);
            yield return PluginUpdates.PerSecond / 2;

            box.CloseScreen();
            yield return PluginUpdates.PerSecond;
        }

        /// <summary>
        /// Destroys every programmable block on the constructs other than <paramref name="block" />'s, as grinding them away would,
        /// and says how many there were.
        /// </summary>
        static int Destroy(IMyTerminalBlock block)
        {
            var mine = new List<VRage.Game.ModAPI.IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(block.CubeGrid, GridLinkTypeEnum.Mechanical, mine);
            var grids = new HashSet<VRage.ModAPI.IMyEntity>();
            MyAPIGateway.Entities.GetEntities(grids, entity => entity is VRage.Game.ModAPI.IMyCubeGrid && !mine.Contains((VRage.Game.ModAPI.IMyCubeGrid)entity));
            var destroyed = 0;
            foreach (VRage.Game.ModAPI.IMyCubeGrid grid in grids)
            {
                var blocks = new List<VRage.Game.ModAPI.IMySlimBlock>();
                grid.GetBlocks(blocks, slim => slim.FatBlock is IMyProgrammableBlock);
                foreach (var slim in blocks)
                {
                    grid.RazeBlock(slim.Position);
                    destroyed++;
                }
            }

            return destroyed;
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
