using System.Collections.Generic;
using System.IO;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// A cargo container's inventory mass and fill, read in the real game on the Sprocket Inventory Readings blueprint: 1000 kg of
    /// iron ingots pass a trigger at 990 kg and not one at 1010, and stone to half the volume passes 49% and not 51%.
    /// </summary>
    public sealed class InventoryReadingsGameTest
    {
        readonly string _blueprint;
        readonly GameTestReport _report;

        /// <summary>The test on the scenario's blueprint in <paramref name="blueprints" />, answering into <paramref name="report" />.</summary>
        public InventoryReadingsGameTest(string blueprints, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints, "Sprocket Inventory Readings", "bp.sbc");
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, new Vector3D(300, 0, 0));
            var heavy = pasted.Find("Mass Cargo");
            var full = pasted.Find("Fill Cargo");
            var lights = new[] { "Mass Low", "Mass High", "Fill Low", "Fill High" };
            var lit = new IMyFunctionalBlock[lights.Length];
            for (var i = 0; i < lights.Length; i++)
                lit[i] = pasted.Find(lights[i]) as IMyFunctionalBlock;
            if (heavy == null || full == null || System.Array.IndexOf(lit, null) >= 0)
            {
                _report.Fail("inventory readings blueprint", "a container or a light is missing after the paste");
                yield break;
            }

            foreach (var light in lit)
                light.Enabled = false;

            // Time for Sprocket to take up the pasted programmable block and switch its logic system on.
            yield return 5 * PluginUpdates.PerSecond;

            heavy.GetInventory(0).AddItems(1000, MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ingot>("Iron"));

            var stone = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Stone");
            var stoneVolume = MyDefinitionManager.Static.GetPhysicalItemDefinition(stone.GetId()).Volume;
            var inventory = full.GetInventory(0);
            inventory.AddItems((MyFixedPoint)(int)((double)inventory.MaxVolume / 2 / stoneVolume), stone);
            yield return 3 * PluginUpdates.PerSecond;

            var held = $"Mass Cargo holds {(double)heavy.GetInventory(0).CurrentMass:0.##} kg, Fill Cargo is {100 * (double)inventory.CurrentVolume / (double)inventory.MaxVolume:0.##}% full";
            _report.Check("inventory mass", lit[0].Enabled && !lit[1].Enabled,
                $"{held}; Mass Low is {(lit[0].Enabled ? "on" : "off")}, Mass High is {(lit[1].Enabled ? "on" : "off")}");
            _report.Check("inventory fill", lit[2].Enabled && !lit[3].Enabled,
                $"{held}; Fill Low is {(lit[2].Enabled ? "on" : "off")}, Fill High is {(lit[3].Enabled ? "on" : "off")}");
        }
    }
}
