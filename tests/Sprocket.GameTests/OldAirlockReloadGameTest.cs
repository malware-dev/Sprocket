using System.Collections.Generic;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.GameTests
{
    /// <summary>
    /// The second half of testing row "An old program comes through the upgrade", run on the world the first run saved: the Old
    /// Airlock OldAirlockGameTest upgraded is found again, its program read out of its block, and Cycle Button pressed once more.
    /// </summary>
    public sealed class OldAirlockReloadGameTest
    {
        readonly GameTestReport _report;
        int _thirds;

        /// <summary>The test, answering into <paramref name="report" />.</summary>
        public OldAirlockReloadGameTest(GameTestReport report)
        {
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var blocks = Named();
            var door = Block<IMyDoor>(blocks, "Old Door");
            var piston = Block<IMyPistonBase>(blocks, "Old Piston");
            var button = Block<MyButtonPanel>(blocks, "Cycle Button");
            var cycled = Block<IMyFunctionalBlock>(blocks, "Cycled");
            var opened = Block<IMyFunctionalBlock>(blocks, "Opened");
            var automation = Block<IMyTerminalBlock>(blocks, "Old Automation");
            if (door == null || piston == null || button == null || cycled == null || opened == null || automation == null)
            {
                _report.Fail("reloaded airlock", "a block of the old airlock is missing from the saved world");
                yield break;
            }

            string program;
            var kept = automation.Storage != null && automation.Storage.TryGetValue(OldAirlockGameTest.ProgramKey, out program) ? program ?? "" : "";
            _report.Check("an upgraded program read back from a saved world", kept.StartsWith("sprocket 1") && kept.Contains("ext.move_to("),
                $"the block holds a program of {kept.Length} characters, starting \"{(kept.Length > 20 ? kept.Substring(0, 20) : kept)}\"");

            // The piston last stood at 2 m, so the cycle closes the door, moves nothing and opens it.
            cycled.Enabled = false;
            opened.Enabled = false;
            button.ActivateButton(0);
            _thirds = 0;
            while (!cycled.Enabled && _thirds < 45)
            {
                _thirds++;
                yield return PluginUpdates.PerSecond / 3;
            }

            yield return PluginUpdates.PerSecond;
            _report.Check("an upgraded airlock runs after a save and load", cycled.Enabled && opened.Enabled && door.Status == DoorStatus.Open,
                $"after {_thirds / 3.0:0.#} s Old Door is {door.Status}, Old Piston at {piston.CurrentPosition:0.##} m, Cycled is {Said(cycled)} and Opened {Said(opened)}");
        }

        /// <summary>Every terminal block in the world, by its name.</summary>
        static Dictionary<string, IMyTerminalBlock> Named()
        {
            var named = new Dictionary<string, IMyTerminalBlock>();
            var grids = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(grids, entity => entity is IMyCubeGrid);
            var blocks = new List<IMySlimBlock>();
            foreach (IMyCubeGrid grid in grids)
            {
                blocks.Clear();
                grid.GetBlocks(blocks, slim => slim.FatBlock is IMyTerminalBlock);
                foreach (var slim in blocks)
                {
                    var block = (IMyTerminalBlock)slim.FatBlock;
                    named[block.CustomName] = block;
                }
            }

            return named;
        }

        static T Block<T>(Dictionary<string, IMyTerminalBlock> named, string name) where T : class
        {
            IMyTerminalBlock block;
            return named.TryGetValue(name, out block) ? block as T : null;
        }

        static string Said(IMyFunctionalBlock light) => light.Enabled ? "on" : "off";
    }
}
