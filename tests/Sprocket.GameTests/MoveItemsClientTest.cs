using System.Collections.Generic;
using System.IO;
using VRage;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "The look of the move line, and the new icon", in the game client on the Sprocket Move Items blueprint: its move
    /// line, the selector its target opens, the target changed with Done and left with back, each pictured.
    /// </summary>
    public sealed class MoveItemsClientTest
    {
        static readonly Vector3D At = new Vector3D(0, 0, -330);

        // The scenario's programmable block, its logic system, the slot its first step moves to, as its part on the line says it,
        // a cargo container that slot does not hold, and a remote control (MoveItems).
        const string Automation = "Move Automation";
        const string System = "Move";
        const string Target = "inventory slot 2";
        const string Other = "Move C";
        const string Remote = "Move Remote";

        // The remote control's flight settings by Sprocket's own names, and the Norwegian words of the Sequences tab and of the task a
        // step adds (MyTexts.no.resx, Sprocket.System.Sequences and Sprocket.Sequence.DoSomething).
        static readonly string[] Flight = { "handbrake", "dampeners", "control thrusters" };
        const string NorwegianSequences = "Sekvenser";
        const string NorwegianTask = "oppgave";

        readonly string _blueprint;
        readonly GameTestReport _report;
        readonly EditorTour _tour;

        /// <summary>The test on the blueprints in <paramref name="blueprints" />, answering into <paramref name="report" />, pictured by <paramref name="tour" />.</summary>
        public MoveItemsClientTest(string blueprints, GameTestReport report, EditorTour tour)
        {
            _blueprint = Path.Combine(blueprints ?? "", "Sprocket Move Items", "bp.sbc");
            _report = report;
            _tour = tour;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, At);
            if (pasted.Find(Automation) == null)
            {
                _report.Fail("move items", $"{Automation} is missing after the paste");
                yield break;
            }

            // Five seconds for Sprocket to take up the pasted programmable block and its logic system.
            yield return 5 * PluginUpdates.PerSecond;

            var stops = new[]
            {
                new[] { "line", System, "Sequences", "Sequence 1" },
                new[] { "action", System, "off", "Sequences", "Sequence 1", "move" },
                new[] { "target", System, "off", "Sequences", "Sequence 1", Target },
                new[] { "target left", System, "off", "Sequences", "Sequence 1", Target, "escape" },
                new[] { "target changed", System, "off", "Sequences", "Sequence 1", Target, "hover:" + Other, "click", "Done" }
            };

            var visit = _tour.Visit(Automation, stops, "move ");
            while (visit.MoveNext())
                yield return visit.Current;

            // Testing row "Remote control rows in a Norwegian game": the remote control's flight settings, each chosen in a step's
            // selector with the game in Norwegian, where Sprocket names them its own way and describes them in Norwegian.
            var norwegian = EditorTour.Speak(MyLanguagesEnum.Norwegian);
            while (norwegian.MoveNext())
                yield return norwegian.Current;

            // Each in a visit of its own, since a stop leaves its selector open.
            foreach (var setting in Flight)
            {
                var stop = new[] { "remote " + setting, System, "off", NorwegianSequences, "Sequence 1", "add:" + NorwegianTask, "wait", "hover:" + Remote, "click", "hover:" + setting, "click" };
                var row = _tour.Visit(Automation, new[] { stop }, "move no ");
                while (row.MoveNext())
                    yield return row.Current;
            }

            var english = EditorTour.Speak(MyLanguagesEnum.English);
            while (english.MoveNext())
                yield return english.Current;
        }
    }
}
