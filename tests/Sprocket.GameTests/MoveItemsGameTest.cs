using System.Collections.Generic;
using System.IO;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using VRage;
using VRage.Game;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "Items move only over a real conveyor connection", run in the real game on the Sprocket Move Items blueprint: a
    /// press of To B Button moves every ingot from Move A to Move B, joined by a conveyor junction, and a press of To C Button moves
    /// none to Move C, which no conveyor reaches.
    /// </summary>
    public sealed class MoveItemsGameTest
    {
        const int Ingots = 1000;

        static readonly MyDefinitionId Iron = new MyDefinitionId(typeof(MyObjectBuilder_Ingot), "Iron");

        readonly string _blueprint;
        readonly GameTestReport _report;

        /// <summary>The test on the scenario's blueprint in <paramref name="blueprints" />, answering into <paramref name="report" />.</summary>
        public MoveItemsGameTest(string blueprints, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints, "Sprocket Move Items", "bp.sbc");
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, new Vector3D(150, 0, 0));
            var source = pasted.Find("Move A");
            var joined = pasted.Find("Move B");
            var apart = pasted.Find("Move C");
            var toJoined = pasted.Find("To B Button") as MyButtonPanel;
            var toApart = pasted.Find("To C Button") as MyButtonPanel;
            var done = pasted.Find("B Done") as IMyFunctionalBlock;
            if (source == null || joined == null || apart == null || toJoined == null || toApart == null || done == null)
            {
                _report.Fail("move items blueprint", "a container, a button or B Done is missing after the paste");
                yield break;
            }

            done.Enabled = false;

            // Time for the conveyor network to form, and for Sprocket to take up the pasted programmable block.
            yield return 5 * PluginUpdates.PerSecond;

            _report.Note($"the game says Move A is connected to Move B: {source.GetInventory(0).IsConnectedTo(joined.GetInventory(0))}, to Move C: {source.GetInventory(0).IsConnectedTo(apart.GetInventory(0))}");

            var ingot = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ingot>("Iron");
            source.GetInventory(0).AddItems(Ingots, ingot);
            // ActivateButton runs the button's toolbar slot on the server. PressButton is the client's request, and on the dedicated
            // server it runs nothing: measured, no ingot moved after it.
            toJoined.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;

            var arrived = Amount(joined);
            _report.Check("move items over a conveyor", arrived == Ingots && Amount(source) == 0 && done.Enabled,
                $"{arrived} of {Ingots} kg of iron ingots reached Move B, {Amount(source)} kg stayed in Move A, B Done is {(done.Enabled ? "on" : "off")}");

            joined.GetInventory(0).RemoveItemsOfType(arrived, ingot);
            source.GetInventory(0).AddItems(Ingots - Amount(source), ingot);
            toApart.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("move items with no conveyor", Amount(apart) == 0 && Amount(source) == Ingots,
                $"{Amount(apart)} kg of iron ingots reached Move C, {Amount(source)} kg stayed in Move A");

            // Move B filled with stone until only half the ingots fit: the step moves what fits and waits, and finishes once there is room.
            done.Enabled = false;
            var stone = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Stone");
            var stoneVolume = MyDefinitionManager.Static.GetPhysicalItemDefinition(stone.GetId()).Volume;
            var ingotVolume = MyDefinitionManager.Static.GetPhysicalItemDefinition(Iron).Volume;
            var target = joined.GetInventory(0);
            var filler = (MyFixedPoint)(int)(((double)target.MaxVolume - Ingots / 2.0 * ingotVolume) / stoneVolume);
            target.AddItems(filler, stone);
            _report.Note($"Move B holds {filler} kg of stone, leaving {(double)(target.MaxVolume - target.CurrentVolume) / ingotVolume:0} kg of room for iron ingots");

            toJoined.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
            var fitted = Amount(joined);
            _report.Check("move items into a full container", fitted > 0 && fitted < Ingots && Amount(source) == Ingots - fitted && !done.Enabled,
                $"{fitted} kg reached Move B, {Amount(source)} kg waited in Move A, B Done is {(done.Enabled ? "on" : "off")}");

            target.RemoveItemsOfType(filler, stone);
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("move items once there is room", Amount(joined) == Ingots && Amount(source) == 0 && done.Enabled,
                $"{Amount(joined)} kg in Move B, {Amount(source)} kg left in Move A, B Done is {(done.Enabled ? "on" : "off")}");
        }

        static MyFixedPoint Amount(IMyTerminalBlock block) => block.GetInventory(0).GetItemAmount(Iron);
    }
}
