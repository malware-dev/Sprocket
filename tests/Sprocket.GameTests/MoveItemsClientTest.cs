using System.Collections.Generic;
using System.IO;
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
        // and a cargo container that slot does not hold (MoveItems).
        const string Automation = "Move Automation";
        const string System = "Move";
        const string Target = "inventory slot 2";
        const string Other = "Move C";

        readonly string _blueprint;
        readonly GameTestReport _report;
        readonly EditorShotClientTest _editor;

        /// <summary>The test on the blueprints in <paramref name="blueprints" />, answering into <paramref name="report" />, pictured by <paramref name="editor" />.</summary>
        public MoveItemsClientTest(string blueprints, GameTestReport report, EditorShotClientTest editor)
        {
            _blueprint = Path.Combine(blueprints ?? "", "Sprocket Move Items", "bp.sbc");
            _report = report;
            _editor = editor;
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

            var visit = _editor.Visit(Automation, stops, "move ");
            while (visit.MoveNext())
                yield return visit.Current;
        }
    }
}
