using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Utils;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "A script over its instruction limit", run in the real game on the Sprocket Script Limit blueprint: a run of a
    /// script that loops without end stops the sequence at the run and is written in the log, and the game keeps running; a run
    /// of a script that returns at once goes on to the next step.
    /// </summary>
    public sealed class ScriptLimitGameTest
    {
        const string Looping = "public void Main(string argument) { while (true) { } }";
        const string Plain = "public void Main(string argument) { }";

        readonly string _blueprint;
        readonly GameTestReport _report;

        /// <summary>The test on the scenario's blueprint in <paramref name="blueprints" />, answering into <paramref name="report" />.</summary>
        public ScriptLimitGameTest(string blueprints, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints, "Sprocket Script Limit", "bp.sbc");
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, new Vector3D(750, 0, 0));
            var looping = pasted.Find("Looping Script") as IMyProgrammableBlock;
            var plain = pasted.Find("Plain Script") as IMyProgrammableBlock;
            var loopingButton = pasted.Find("Looping Button") as MyButtonPanel;
            var plainButton = pasted.Find("Plain Button") as MyButtonPanel;
            var loopingRan = pasted.Find("Looping Ran") as IMyFunctionalBlock;
            var plainRan = pasted.Find("Plain Ran") as IMyFunctionalBlock;
            if (looping == null || plain == null || loopingButton == null || plainButton == null || loopingRan == null || plainRan == null)
            {
                _report.Fail("script limit blueprint", "a programmable block, a button or a light is missing after the paste");
                yield break;
            }

            looping.ProgramData = Looping;
            plain.ProgramData = Plain;
            loopingRan.Enabled = false;
            plainRan.Enabled = false;
            yield return 5 * PluginUpdates.PerSecond;
            _report.Note($"the scripts compiled: Looping Script {(looping.HasCompileErrors ? "has errors" : "clean")}, Plain Script {(plain.HasCompileErrors ? "has errors" : "clean")}");

            plainButton.ActivateButton(0);
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("a run of a script that returns", plainRan.Enabled, $"Plain Ran is {(plainRan.Enabled ? "on" : "off")}");

            loopingButton.ActivateButton(0);
            yield return 2 * PluginUpdates.PerSecond;
            MyLog.Default.Flush();
            var said = Logged("would not run");
            _report.Check("a run of a script over its instruction limit", !loopingRan.Enabled && said.Any(line => line.Contains("Looping Script")),
                $"Looping Ran is {(loopingRan.Enabled ? "on" : "off")}, the game is still running, and the log says: {string.Join(" / ", said)}");

            // The run that met the error switched the logic system off, so Plain Button starts nothing; the game itself still runs
            // the plain script.
            plainRan.Enabled = false;
            plainButton.ActivateButton(0);
            yield return 2 * PluginUpdates.PerSecond;
            var ran = plain.TryRun("");
            _report.Check("a run after the overrun", !plainRan.Enabled && ran,
                $"after the logic system was switched off Plain Ran is {(plainRan.Enabled ? "on" : "off")}, and the game runs Plain Script: {ran}");
        }

        /// <summary>The lines of the game's log holding <paramref name="text" />, read while the game still writes to it.</summary>
        static List<string> Logged(string text)
        {
            using (var stream = new FileStream(MyLog.Default.GetFilePath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                var found = new List<string>();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Contains(text))
                        found.Add(line.Trim());
                }

                return found;
            }
        }
    }
}
