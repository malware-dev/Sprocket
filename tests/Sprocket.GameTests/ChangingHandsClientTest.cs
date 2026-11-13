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
    /// The joined client's side of Testing row #2, a construct changing hands on a dedicated server (ChangingHandsServer): refused
    /// the automation of a construct it owns none of, then given the construct and shown the paused screen with both names, left
    /// paused, resumed from the banner, and finally running the sequence by hand once its door is somebody else's.
    /// </summary>
    public sealed class ChangingHandsClientTest
    {
        // The construct's programmable block and logic system (ForcedDoor).
        const string Block = "Forced Automation";
        const string System = "Forced";

        readonly string _shots;
        readonly GameTestReport _report;
        readonly EditorInput _input;
        readonly EditorTour _tour;
        readonly string _signals;

        /// <summary>The client's side, writing pictures to <paramref name="shots" />, answering into <paramref name="report" />, pressing with <paramref name="input" /> and touring with <paramref name="tour" />, signalling the server through <paramref name="signals" />.</summary>
        public ChangingHandsClientTest(string shots, GameTestReport report, EditorInput input, EditorTour tour, string signals)
        {
            _shots = shots;
            _report = report;
            _input = input;
            _tour = tour;
            _signals = signals;
        }

        /// <summary>The steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            Signal("hands-1");
            var wait = Waiting("hands-2");
            while (wait.MoveNext())
                yield return wait.Current;

            var block = EditorTour.Named(Block);
            if (block == null)
            {
                _report.Fail("hands", $"no block called {Block} reached this client");
                yield break;
            }

            // Step 2: the construct the faction mate's, this player outside the faction, refused.
            Press(block);
            yield return 2 * PluginUpdates.PerSecond;
            var refused = _input.IsOpen && InGameEditor.Lent() == null;
            _report.Check("hands refused to an outsider", refused, refused ? "the Automation button opened a notice and no editor" : InGameEditor.Lent() != null ? "the editor opened for a player who owns none of the construct" : "the Automation button opened nothing");
            var shot = Picture("hands-refused");
            while (shot.MoveNext())
                yield return shot.Current;
            _input.Press(MyKeys.Enter);
            yield return PluginUpdates.PerSecond;

            // Step 3 and 4: given to this player, opened on the paused screen, left paused, then resumed from the banner.
            Signal("hands-3");
            wait = Waiting("hands-4");
            while (wait.MoveNext())
                yield return wait.Current;

            Press(block);
            for (var waited = 0; waited < 40 && InGameEditor.Lent() == null; waited++)
                yield return PluginUpdates.PerSecond / 2;
            var editor = InGameEditor.Lent();
            if (editor == null)
            {
                _report.Fail("hands paused", "the editor did not open for the construct's new owner");
                yield break;
            }

            var stops = new[]
            {
                new[] { "paused", "stay" },
                new[] { "left paused", "stay", "Leave paused" },
                new[] { "resumed", "Review", "Resume" }
            };
            var tour = _tour.Tour(editor, stops, "hands ");
            while (tour.MoveNext())
                yield return tour.Current;
            _input.Close();
            yield return PluginUpdates.PerSecond;

            // Step 5: the door the owner's again, and the sequence run by hand.
            Signal("hands-5");
            wait = Waiting("hands-6");
            while (wait.MoveNext())
                yield return wait.Current;

            Press(block);
            for (var waited = 0; waited < 40 && InGameEditor.Lent() == null; waited++)
                yield return PluginUpdates.PerSecond / 2;
            editor = InGameEditor.Lent();
            editor?.Home();
            var ran = editor != null && editor.Press(System) && editor.Press("Sequences") && editor.Press("Sequence 1") && editor.Run();
            _report.Check("hands run by hand", ran, ran ? "the sequence was run by hand" : $"could not run the sequence by hand; the page offers {editor?.Buttons() ?? "nothing"}");
            yield return 4 * PluginUpdates.PerSecond;
            _input.Park();
            shot = Picture("hands-door-gone");
            while (shot.MoveNext())
                yield return shot.Current;
            _input.Close();
            yield return PluginUpdates.PerSecond;

            Signal("hands-7");
            wait = Waiting("hands-done");
            while (wait.MoveNext())
                yield return wait.Current;
        }

        /// <summary>Presses the Automation button of <paramref name="block" />.</summary>
        static void Press(IMyTerminalBlock block)
        {
            List<IMyTerminalControl> controls;
            MyAPIGateway.TerminalControls.GetControls<IMyProgrammableBlock>(out controls);
            (controls.Find(control => control.Id == EditorTour.OpenButton) as IMyTerminalControlButton)?.Action(block);
        }

        /// <summary>Takes a picture called <paramref name="name" /> of the screen as it is.</summary>
        IEnumerator<int> Picture(string name)
        {
            var picture = Path.Combine(_shots, name + ".png");
            if (File.Exists(picture))
                File.Delete(picture);

            MyRenderProxy.TakeScreenshot(Vector2.One, picture, false, false, false);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check(name, File.Exists(picture), File.Exists(picture) ? picture : $"no picture at {picture}");
        }

        /// <summary>Writes the file <paramref name="name" /> for the server to read.</summary>
        void Signal(string name)
        {
            File.WriteAllText(Path.Combine(_signals, name), name);
        }

        /// <summary>Waits for the server's file <paramref name="name" />, five minutes at most, and reports where it never came.</summary>
        IEnumerator<int> Waiting(string name)
        {
            var waited = 0;
            for (; waited < 300 && !File.Exists(Path.Combine(_signals, name)); waited++)
                yield return PluginUpdates.PerSecond;

            if (waited == 300)
                _report.Fail("hands " + name, "the server did not answer within five minutes");
        }
    }
}
