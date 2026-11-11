using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "Moved items arrive in the real amounts", run in the real game on the Sprocket Move Amounts blueprint: Ingots Button
    /// moves 100 kg of iron ingots exactly, Plates Button moves the 2 whole steel plates in 2.5, All Button empties Amounts A, and
    /// Empty Button, with nothing to move, moves on and switches Moved On on.
    /// </summary>
    public sealed class MoveAmountsGameTest
    {
        const int Ingots = 500;
        const int Plates = 10;
        const int Stone = 300;

        static readonly MyDefinitionId Iron = new MyDefinitionId(typeof(MyObjectBuilder_Ingot), "Iron");
        static readonly MyDefinitionId SteelPlate = new MyDefinitionId(typeof(MyObjectBuilder_Component), "SteelPlate");
        static readonly MyDefinitionId Rock = new MyDefinitionId(typeof(MyObjectBuilder_Ore), "Stone");

        readonly string _blueprint;
        readonly GameTestReport _report;

        /// <summary>The test on the scenario's blueprint in <paramref name="blueprints" />, answering into <paramref name="report" />.</summary>
        public MoveAmountsGameTest(string blueprints, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints, "Sprocket Move Amounts", "bp.sbc");
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, new Vector3D(1350, 0, 0));
            var source = pasted.Find("Amounts A");
            var target = pasted.Find("Amounts B");
            var ingots = pasted.Find("Ingots Button") as MyButtonPanel;
            var plates = pasted.Find("Plates Button") as MyButtonPanel;
            var all = pasted.Find("All Button") as MyButtonPanel;
            var empty = pasted.Find("Empty Button") as MyButtonPanel;
            var movedOn = pasted.Find("Moved On") as IMyFunctionalBlock;
            if (source == null || target == null || ingots == null || plates == null || all == null || empty == null || movedOn == null)
            {
                _report.Fail("move amounts blueprint", "a container, a button or Moved On is missing after the paste");
                yield break;
            }

            movedOn.Enabled = false;

            // Time for the conveyor network to form, and for Sprocket to take up the pasted programmable block.
            yield return 5 * PluginUpdates.PerSecond;

            var from = source.GetInventory(0);
            var to = target.GetInventory(0);
            from.AddItems(Ingots, MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ingot>("Iron"));
            from.AddItems(Plates, MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Component>("SteelPlate"));
            from.AddItems(Stone, MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Stone"));

            // ActivateButton runs the button's toolbar slot on the server (MoveItemsGameTest).
            ingots.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("move 100 kg of ingots", to.GetItemAmount(Iron) == 100 && from.GetItemAmount(Iron) == Ingots - 100,
                $"{to.GetItemAmount(Iron)} kg of iron ingots reached Amounts B, {from.GetItemAmount(Iron)} kg stayed in Amounts A");

            plates.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("move 2.5 steel plates", to.GetItemAmount(SteelPlate) == 2 && from.GetItemAmount(SteelPlate) == Plates - 2,
                $"{to.GetItemAmount(SteelPlate)} steel plates reached Amounts B, {from.GetItemAmount(SteelPlate)} stayed in Amounts A");

            all.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("move all items", from.ItemCount == 0
                && to.GetItemAmount(Iron) == Ingots && to.GetItemAmount(SteelPlate) == Plates && to.GetItemAmount(Rock) == Stone,
                $"Amounts A holds {from.ItemCount} stacks; Amounts B holds {to.GetItemAmount(Iron)} kg of iron ingots, {to.GetItemAmount(SteelPlate)} steel plates and {to.GetItemAmount(Rock)} kg of stone");

            var held = (MyFixedPoint)to.CurrentMass;
            empty.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("move on with nothing to move", movedOn.Enabled && (MyFixedPoint)to.CurrentMass == held,
                $"Moved On is {(movedOn.Enabled ? "on" : "off")}, Amounts B went from {held} to {to.CurrentMass} kg");
        }
    }
}
