using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox;
using Sandbox.Graphics.GUI;
using VRage;
using System.IO;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Input;
using VRageMath;
using VRageRender;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Sprocket's editor in the game client, opened by the Automation button of a pasted programmable block and toured stop by stop,
    /// each stop what is pressed from the editor's home screen and the picture taken there.
    /// </summary>
    /// <remarks>
    /// A stop is its picture's name and its steps, each one of: <c>hover:</c> rests the pointer on what follows, <c>page:</c> opens
    /// the codex page it names in the tip that is open, <c>type:</c> types what follows, <c>add:</c> takes the entry it names from a plus menu, <c>click</c> clicks where the pointer rests, <c>down</c> and <c>up</c> press and let go of its button, so a <c>hover:</c> between them drags, <c>right:</c> moves the pointer that many units right,
    /// <c>open:</c> presses the first shown element of the type it names, such as a plus menu or a list field, <c>tab</c>,
    /// <c>escape</c>, <c>arrowdown</c>, <c>arrowup</c> and <c>enter</c> press their keys, <c>on</c> and <c>off</c> flip the logic system's switch, <c>alt</c> holds Alt,
    /// <c>answer</c> answers a question where one is asked, <c>wait</c> gives a page two seconds, <c>stay</c> first starts where the last stop left the editor, and anything else is pressed by
    /// its words. A stop ending <c>frame:</c> and a count is pictured that many plugin updates after its last press.
    /// </remarks>
    public sealed class EditorTour
    {
        /// <summary>The id of Sprocket's button in a programmable block's control panel (SprocketControls).</summary>
        public const string OpenButton = "Sprocket_Open";

        readonly string _shots;
        readonly GameTestReport _report;
        readonly EditorInput _input;

        /// <summary>A tour writing pictures to <paramref name="shots" />, answering into <paramref name="report" />, pointing with <paramref name="input" />.</summary>
        public EditorTour(string shots, GameTestReport report, EditorInput input)
        {
            _shots = shots;
            _report = report;
            _input = input;
        }

        /// <summary>
        /// Opens the editor on the programmable block called <paramref name="name" />, which an earlier test pasted, goes to each of
        /// <paramref name="stops" /> and takes its picture, named with <paramref name="prefix" /> before it, and closes the editor.
        /// </summary>
        public IEnumerator<int> Visit(string name, string[][] stops, string prefix)
        {
            var editor = Open(Named(name), name);
            if (editor == null)
                yield break;

            yield return 2 * PluginUpdates.PerSecond;
            var tour = Tour(editor, stops, prefix);
            while (tour.MoveNext())
                yield return tour.Current;

            Close();
            yield return PluginUpdates.PerSecond;
        }

        /// <summary>Opens the editor on <paramref name="block" />, called <paramref name="name" />, by its Automation button, and returns it, or null with the reason reported.</summary>
        public InGameEditor Open(IMyTerminalBlock block, string name)
        {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyProgrammableBlock>(out controls);
            var open = controls.Find(control => control.Id == OpenButton) as IMyTerminalControlButton;
            if (block == null || open == null)
            {
                _report.Fail("editor open", block == null ? $"no block called {name}" : "the programmable block has no Automation button");
                return null;
            }

            open.Action(block);
            var editor = InGameEditor.Of(block);
            if (editor == null)
                _report.Fail("editor open", "Sprocket keeps no editor for the block it opened on");

            return editor;
        }

        /// <summary>Closes the editor that is open.</summary>
        public void Close()
        {
            _input.Close();
        }

        /// <summary>
        /// Goes to each of <paramref name="stops" /> from the editor's home screen and takes its picture, named with
        /// <paramref name="prefix" /> before it.
        /// </summary>
        public IEnumerator<int> Tour(InGameEditor editor, string[][] stops, string prefix)
        {
            foreach (var stop in stops)
            {
                // A drag a stop left held is let go before the next.
                _input.Up();

                // A stop opening with stay starts where the last one left the editor, such as the resume screen it opens on.
                if (stop.Length < 2 || stop[1] != "stay")
                    editor.Home();
                yield return PluginUpdates.PerSecond / 2;
                var name = prefix + stop[0];
                var pressed = true;
                var hovering = false;
                var last = stop.Length - 1;
                var frame = stop[last].StartsWith("frame:") ? int.Parse(stop[last].Substring(6)) : 0;
                for (var i = 1; i < (frame > 0 ? last : stop.Length) && pressed; i++)
                {
                    if (stop[i] == "stay")
                        continue;

                    if (stop[i] == "wait")
                    {
                        yield return 2 * PluginUpdates.PerSecond;
                        continue;
                    }

                    // A page opened in the tip keeps the pointer where it rests, so the tip stays open.
                    hovering = stop[i].StartsWith("hover:") || hovering && stop[i].StartsWith("page:");
                    pressed = Step(editor, stop[i]);
                    if (!pressed)
                        _report.Fail($"editor {name}", $"no button {stop[i]}; the page offers {editor.Buttons()}");

                    // The press that begins a pictured motion is followed by its picture alone.
                    if (frame == 0 || i < last - 1)
                        yield return PluginUpdates.PerSecond / 2;
                }

                if (!pressed)
                    continue;

                var shot = frame > 0 ? Moment("editor-" + name.Replace(" ", "-"), frame) : Shoot("editor-" + name.Replace(" ", "-"), !hovering);
                while (shot.MoveNext())
                    yield return shot.Current;
            }
        }

        /// <summary>Puts the game in <paramref name="language" /> as its options screen does, by the setting and a screen closing after it.</summary>
        /// <remarks>Sprocket looks for a new language as any screen closes. The setting is the test user data's own (Run-ClientTests.ps1 -AppData).</remarks>
        public static IEnumerator<int> Speak(MyLanguagesEnum language)
        {
            MySandboxGame.Config.Language = language;
            var box = MyGuiSandbox.CreateMessageBox(MyMessageBoxStyleEnum.Info, messageText: new StringBuilder(language.ToString()));
            MyGuiSandbox.AddScreen(box);
            yield return PluginUpdates.PerSecond / 2;

            box.CloseScreen();
            yield return PluginUpdates.PerSecond;
        }

        /// <summary>The programmable block, or other terminal block, called <paramref name="name" />, or null where none is.</summary>
        public static IMyTerminalBlock Named(string name)
        {
            var found = new List<IMyTerminalBlock>();
            var grids = new HashSet<VRage.ModAPI.IMyEntity>();
            MyAPIGateway.Entities.GetEntities(grids, entity => entity is VRage.Game.ModAPI.IMyCubeGrid);
            foreach (var grid in grids.OfType<VRage.Game.ModAPI.IMyCubeGrid>())
            {
                var blocks = new List<VRage.Game.ModAPI.IMySlimBlock>();
                grid.GetBlocks(blocks, slim => (slim.FatBlock as IMyTerminalBlock)?.CustomName == name);
                foreach (var slim in blocks)
                    found.Add((IMyTerminalBlock)slim.FatBlock);
            }

            return found.Count > 0 ? found[0] : null;
        }

        /// <summary>Takes one step of a stop, as the remarks on this type list them, and says whether there was what it names.</summary>
        bool Step(InGameEditor editor, string step)
        {
            if (step.StartsWith("hover:"))
                return editor.Hover(step.Substring(6), _input);
            if (step.StartsWith("page:"))
                return editor.Page(step.Substring(5), _input);
            if (step.StartsWith("type:"))
                return _input.Type(step.Substring(5));
            if (step.StartsWith("right:"))
                return _input.Nudge(float.Parse(step.Substring(6), System.Globalization.CultureInfo.InvariantCulture));
            if (step.StartsWith("add:"))
                return editor.Add(step.Substring(4));
            if (step.StartsWith("open:"))
                return editor.Open(step.Substring(5));

            switch (step)
            {
                case "click":
                    return _input.Click();
                case "down":
                    return _input.Down();
                case "up":
                    return _input.Up();
                case "tab":
                    return _input.Press(MyKeys.Tab);
                case "arrowdown":
                    return _input.Press(MyKeys.Down);
                case "arrowup":
                    return _input.Press(MyKeys.Up);
                case "enter":
                    return _input.Press(MyKeys.Enter);
                case "escape":
                    return _input.Press(MyKeys.Escape);
                case "on":
                    return editor.Switch(true);
                case "off":
                    return editor.Switch(false);
                case "alt":
                    return editor.HoldAlt(true);
                case "answer":
                    // Some presses ask and some go ahead, so a stop answers where a question was asked.
                    editor.Answer();
                    return true;
                default:
                    return editor.Press(step);
            }
        }

        /// <summary>Takes a picture called <paramref name="name" />, with the pointer outside the editor so no tip is open in it where <paramref name="park" /> holds, and otherwise once the tip of what it rests on has had time to open.</summary>
        IEnumerator<int> Shoot(string name, bool park)
        {
            if (park)
                _input.Park();

            yield return 2 * PluginUpdates.PerSecond;

            var picture = Path.Combine(_shots, name + ".png");
            if (File.Exists(picture))
                File.Delete(picture);

            MyRenderProxy.TakeScreenshot(Vector2.One, picture, false, false, false);
            yield return 3 * PluginUpdates.PerSecond;
            if (!park)
                _report.Note($"{name}: {_input.Describe()}");

            _report.Check(name, File.Exists(picture), File.Exists(picture) ? picture : $"no picture at {picture}");
        }

        /// <summary>Takes a picture called <paramref name="name" /> <paramref name="updates" /> plugin updates into a motion that has just begun, two game frames each.</summary>
        IEnumerator<int> Moment(string name, int updates)
        {
            _input.Park();
            yield return updates;

            var picture = Path.Combine(_shots, name + ".png");
            if (File.Exists(picture))
                File.Delete(picture);

            MyRenderProxy.TakeScreenshot(Vector2.One, picture, false, false, false);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check(name, File.Exists(picture), File.Exists(picture) ? picture : $"no picture at {picture}");
        }
    }
}
