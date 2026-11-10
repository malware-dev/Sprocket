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
    /// Testing row "Items move only over a real conveyor connection", run in the real game on the Sprocket Move Items blueprint: a
    /// press of To B Button moves every ingot from Move A to Move B, joined by a conveyor junction, and a press of To C Button moves
    /// none to Move C, which no conveyor reaches.
    /// </summary>
    public sealed class MoveItemsGameTest
    {
        // The server calls a plugin every second gameplay frame: 90 calls in 180 frames over 3 s, measured on the dedicated server.
        const int UpdatesPerSecond = 30;

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
            if (source == null || joined == null || apart == null || toJoined == null || toApart == null)
            {
                _report.Fail("move items blueprint", "a container or a button is missing after the paste");
                yield break;
            }

            // Time for the conveyor network to form, and for Sprocket to take up the pasted programmable block.
            yield return 5 * UpdatesPerSecond;

            _report.Note($"the game says Move A is connected to Move B: {source.GetInventory(0).IsConnectedTo(joined.GetInventory(0))}, to Move C: {source.GetInventory(0).IsConnectedTo(apart.GetInventory(0))}");

            var ingot = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ingot>("Iron");
            source.GetInventory(0).AddItems(Ingots, ingot);
            // ActivateButton runs the button's toolbar slot on the server. PressButton is the client's request, and on the dedicated
            // server it runs nothing: measured, no ingot moved after it.
            toJoined.ActivateButton(0);
            yield return 3 * UpdatesPerSecond;

            var arrived = Amount(joined);
            _report.Check("move items over a conveyor", arrived == Ingots && Amount(source) == 0,
                $"{arrived} of {Ingots} kg of iron ingots reached Move B, {Amount(source)} kg stayed in Move A");

            joined.GetInventory(0).RemoveItemsOfType(arrived, ingot);
            source.GetInventory(0).AddItems(Ingots - Amount(source), ingot);
            toApart.ActivateButton(0);
            yield return 3 * UpdatesPerSecond;
            _report.Check("move items with no conveyor", Amount(apart) == 0 && Amount(source) == Ingots,
                $"{Amount(apart)} kg of iron ingots reached Move C, {Amount(source)} kg stayed in Move A");
        }

        static MyFixedPoint Amount(IMyTerminalBlock block) => block.GetInventory(0).GetItemAmount(Iron);
    }
}
