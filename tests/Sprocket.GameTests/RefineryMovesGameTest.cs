using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "A refinery and moved items", run in the real game on the Sprocket Refinery Moves blueprint: ore moved out of a
    /// working refinery's input, watched for a minute to see whether the refinery pulls it back; ingots moved from its output into
    /// its own input, which takes none; and ingots moved from its output to the cargo container.
    /// </summary>
    public sealed class RefineryMovesGameTest
    {
        static readonly MyDefinitionId IronOre = new MyDefinitionId(typeof(MyObjectBuilder_Ore), "Iron");
        static readonly MyDefinitionId IronIngot = new MyDefinitionId(typeof(MyObjectBuilder_Ingot), "Iron");

        readonly string _blueprint;
        readonly GameTestReport _report;

        /// <summary>The test on the scenario's blueprint in <paramref name="blueprints" />, answering into <paramref name="report" />.</summary>
        public RefineryMovesGameTest(string blueprints, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints, "Sprocket Refinery Moves", "bp.sbc");
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, new Vector3D(450, 0, 0));
            var refinery = pasted.Find("Move Refinery") as IMyFunctionalBlock;
            var cargo = pasted.Find("Refinery Cargo");
            var outward = pasted.Find("Out Button") as MyButtonPanel;
            var store = pasted.Find("Store Button") as MyButtonPanel;
            var back = pasted.Find("Back Button") as MyButtonPanel;
            if (refinery == null || cargo == null || outward == null || store == null || back == null)
            {
                _report.Fail("refinery blueprint", "the refinery, the container or a button is missing after the paste");
                yield break;
            }

            var input = refinery.GetInventory(0);
            var output = refinery.GetInventory(1);
            var stored = cargo.GetInventory(0);
            yield return 5 * PluginUpdates.PerSecond;
            _report.Note($"the game says the refinery's input is connected to Refinery Cargo: {input.IsConnectedTo(stored)}, its output: {output.IsConnectedTo(stored)}");

            // Out of a working refinery's input: what the refinery does with the ore afterwards is the game's, and is reported.
            refinery.Enabled = true;
            input.AddItems(1000, MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Iron"));
            outward.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
            var moved = (double)stored.GetItemAmount(IronOre);
            _report.Check("move ore out of a refinery's input", moved > 0, $"{moved:0.##} kg of iron ore reached Refinery Cargo, {(double)input.GetItemAmount(IronOre):0.##} kg stayed in the input");
            for (var half = 1; half <= 2; half++)
            {
                yield return 30 * PluginUpdates.PerSecond;
                _report.Note($"{30 * half} s after the move: {(double)stored.GetItemAmount(IronOre):0.##} kg of iron ore in Refinery Cargo, {(double)input.GetItemAmount(IronOre):0.##} kg in the refinery's input, {(double)output.GetItemAmount(IronIngot):0.##} kg of iron ingots in its output");
            }

            // The refinery stays on with its conveyor use off, so it neither pulls nor pushes: only the buttons move items. A task on a
            // switched-off block is refused and switches the logic system off, which would stop every press after it.
            refinery.SetValueBool("UseConveyor", false);
            stored.Clear();
            input.Clear();
            output.Clear();
            var ingot = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ingot>("Iron");
            output.AddItems(500, ingot);
            yield return PluginUpdates.PerSecond;
            store.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("move ingots out of a refinery's output", stored.GetItemAmount(IronIngot) == 500 && output.GetItemAmount(IronIngot) == 0,
                $"{(double)stored.GetItemAmount(IronIngot):0.##} kg of iron ingots reached Refinery Cargo, {(double)output.GetItemAmount(IronIngot):0.##} kg stayed in the output");

            stored.Clear();
            output.Clear();
            output.AddItems(500, ingot);
            back.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("move ingots into a refinery's own input", output.GetItemAmount(IronIngot) == 500 && input.GetItemAmount(IronIngot) == 0,
                $"{(double)output.GetItemAmount(IronIngot):0.##} kg of iron ingots stayed in the output, {(double)input.GetItemAmount(IronIngot):0.##} kg reached the input");

            // The conveyors carry items through a block switched off, so a move goes ahead from a refinery that is off.
            refinery.Enabled = false;
            stored.Clear();
            output.Clear();
            output.AddItems(500, ingot);
            yield return PluginUpdates.PerSecond;
            store.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("move ingots out of a switched off refinery's output", stored.GetItemAmount(IronIngot) == 500 && output.GetItemAmount(IronIngot) == 0,
                $"{(double)stored.GetItemAmount(IronIngot):0.##} kg of iron ingots reached Refinery Cargo, {(double)output.GetItemAmount(IronIngot):0.##} kg stayed in the output");

            // With no power on the grid, the programmable block Sprocket runs on is unpowered too, so a press runs nothing and is not
            // kept: what reaches the cargo is nothing, before the power comes back or after.
            var batteries = new[] { pasted.Find("Refine Battery") as IMyFunctionalBlock, pasted.Find("Refine Battery 2") as IMyFunctionalBlock };
            stored.Clear();
            output.Clear();
            output.AddItems(500, ingot);
            foreach (var battery in batteries)
                battery.Enabled = false;
            yield return PluginUpdates.PerSecond;
            store.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
            foreach (var battery in batteries)
                battery.Enabled = true;
            yield return 5 * PluginUpdates.PerSecond;
            _report.Check("a press with no power on the grid runs nothing", stored.GetItemAmount(IronIngot) == 0 && output.GetItemAmount(IronIngot) == 500,
                $"{(double)stored.GetItemAmount(IronIngot):0.##} kg of iron ingots in Refinery Cargo and {(double)output.GetItemAmount(IronIngot):0.##} kg in the output, 5 s after the power came back");
        }
    }
}
