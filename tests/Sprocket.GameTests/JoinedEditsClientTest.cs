using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing rows #19 and #36 in a client joined to the dedicated server, on the Sprocket Status Pages construct the server pastes
    /// and gives the player (ServerTestPlugin.Joined). #19: an edit, and then a switch, made in the editor are in this client's copy
    /// of the programmable block as the editor closes, the copy a blueprint is made from. #36: the editor opened again shows a
    /// logic system the server switched off, and then on, while it was closed, the server standing in for a second client.
    /// </summary>
    public sealed class JoinedEditsClientTest
    {
        // The construct's programmable block and one of its logic systems, switched off as pasted (StatusPages).
        const string Block = "Pages Automation";
        const string System = "Airlock";

        // The entry of a sequence's plus menu that adds an await, and the word that writes one (docs/Language.md).
        const string AddAwait = "wait for pending tasks";
        const string Await = "await";

        readonly GameTestReport _report;
        readonly EditorInput _input;
        readonly string _signals;

        /// <summary>The test, answering into <paramref name="report" />, closing the editor with <paramref name="input" />, and signalling the server through files in <paramref name="signals" />.</summary>
        public JoinedEditsClientTest(GameTestReport report, EditorInput input, string signals)
        {
            _report = report;
            _input = input;
            _signals = signals;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            // The server pastes the construct as the client joins; a minute is many times what that takes to arrive here.
            IMyTerminalBlock block = null;
            for (var waited = 0; waited < 60 && block == null; waited++)
            {
                block = EditorTour.Named(Block);
                if (block == null)
                    yield return PluginUpdates.PerSecond;
            }

            if (block == null)
            {
                _report.Fail("joined edits", $"no block called {Block} reached this client");
                yield break;
            }

            yield return 5 * PluginUpdates.PerSecond;
            var awaits = Count(Program(block), Await);

            // #19: a step added, the editor closed, and this client's copy of the block looked at every update after.
            var open = Open(block);
            while (open.MoveNext())
                yield return open.Current;
            var editor = Home();
            if (editor == null || !editor.Press(System) || !editor.Press("Sequences") || !editor.Press("Sequence 1") || !editor.Add(AddAwait))
            {
                _report.Fail("joined edit", editor == null ? "the editor did not open on the lent copy" : $"could not add an await; the page offers {editor.Buttons()}");
                _input.Close();
                yield break;
            }

            yield return PluginUpdates.PerSecond / 2;
            _input.Close();
            var seen = -1;
            for (var update = 0; update < 3 * PluginUpdates.PerSecond && seen < 0; update++)
            {
                if (Count(Program(block), Await) > awaits)
                    seen = update;
                else
                    yield return 1;
            }

            _report.Check("blueprint carries an edit at once", seen >= 0,
                seen >= 0 ? $"the added await is in this client's copy of {Block} {seen} updates after the editor closed" : $"the added await was not in this client's copy of {Block} three seconds after the editor closed");

            // #19 again, for a switch: the logic system switched on, the editor closed.
            var switches = Switches(block);
            open = Open(block);
            while (open.MoveNext())
                yield return open.Current;
            editor = Home();
            if (editor == null || !editor.Press(System) || !editor.Switch(true))
            {
                _report.Fail("joined switch", editor == null ? "the editor did not open on the lent copy" : $"could not switch {System} on; the page offers {editor.Buttons()}");
                _input.Close();
                yield break;
            }

            yield return PluginUpdates.PerSecond / 2;
            _input.Close();
            seen = -1;
            for (var update = 0; update < 3 * PluginUpdates.PerSecond && seen < 0; update++)
            {
                if (Switches(block) != switches)
                    seen = update;
                else
                    yield return 1;
            }

            _report.Check("blueprint carries a switch at once", seen >= 0,
                seen >= 0 ? $"the switch is in this client's copy of {Block} {seen} updates after the editor closed" : $"the switch was not in this client's copy of {Block} three seconds after the editor closed");

            // #36: the server switches the logic system off, then on, each while the editor is closed, and the editor opened again shows it.
            foreach (var on in new[] { false, true })
            {
                var word = on ? "on" : "off";
                var asked = Path.Combine(_signals, "switch-" + word);
                var done = Path.Combine(_signals, "switched-" + word);
                File.WriteAllText(asked, System);
                for (var waited = 0; waited < 60 && !File.Exists(done); waited++)
                    yield return PluginUpdates.PerSecond;

                if (!File.Exists(done))
                {
                    _report.Fail("reopened copy " + word, "the server did not answer within a minute");
                    yield break;
                }

                yield return 2 * PluginUpdates.PerSecond;
                open = Open(block);
                while (open.MoveNext())
                    yield return open.Current;
                editor = Home();
                var shown = editor?.IsOn(System);
                _report.Check("reopened copy shows switched " + word, shown == on,
                    $"after the server switched {System} {word}, the editor opened again shows it {(shown == null ? "nowhere" : shown.Value ? "on" : "off")}");
                _input.Close();
                yield return PluginUpdates.PerSecond;
            }
        }

        /// <summary>The lent copy's editor on the screen it opens on, or null where none is open.</summary>
        static InGameEditor? Home()
        {
            var editor = InGameEditor.Lent();
            editor?.Home();
            return editor;
        }

        /// <summary>Presses the Automation button of <paramref name="block" />, and waits for the server to lend the copy, twenty seconds at most.</summary>
        static IEnumerator<int> Open(IMyTerminalBlock block)
        {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyProgrammableBlock>(out controls);
            var button = controls.Find(control => control.Id == EditorTour.OpenButton) as IMyTerminalControlButton;
            button?.Action(block);
            for (var waited = 0; waited < 40 && InGameEditor.Lent() == null; waited++)
                yield return PluginUpdates.PerSecond / 2;

            yield return PluginUpdates.PerSecond;
        }

        /// <summary>The program this client's copy of <paramref name="block" /> carries in its storage, or "" where it carries none.</summary>
        static string Program(IMyTerminalBlock block)
        {
            return block.Storage?.Values.FirstOrDefault(value => value.StartsWith("sprocket ")) ?? "";
        }

        /// <summary>Everything else this client's copy of <paramref name="block" /> carries in its storage, the switches among it.</summary>
        static string Switches(IMyTerminalBlock block)
        {
            return string.Join("\n", block.Storage?.Values.Where(value => !value.StartsWith("sprocket ")) ?? Enumerable.Empty<string>());
        }

        /// <summary>How many lines of <paramref name="program" /> say <paramref name="word" /> alone.</summary>
        static int Count(string program, string word)
        {
            return program.Split('\n').Count(line => line.Trim() == word);
        }
    }
}
