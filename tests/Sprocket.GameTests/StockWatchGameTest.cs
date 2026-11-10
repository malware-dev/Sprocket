using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing rows "Stock in a cargo container fires its trigger" and "Stock in a refinery's input fires its trigger", run in the
    /// real game on the Sprocket Stock Watch blueprint: items put into a real inventory pass the level the program watches, and its
    /// light comes on for the 3 seconds the program gives it.
    /// </summary>
    public sealed class StockWatchGameTest
    {
        readonly string _blueprint;
        readonly GameTestReport _report;

        /// <summary>The test on the scenario's blueprint in <paramref name="blueprints" />, answering into <paramref name="report" />.</summary>
        public StockWatchGameTest(string blueprints, GameTestReport report)
        {
            _blueprint = System.IO.Path.Combine(blueprints, "Sprocket Stock Watch", "bp.sbc");
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, VRageMath.Vector3D.Zero);
            var cargo = pasted.Find("Stock Cargo");
            var refinery = (IMyFunctionalBlock)pasted.Find("Stock Refinery");
            var light = (IMyFunctionalBlock)pasted.Find("Stock Light");
            if (cargo == null || refinery == null || light == null)
            {
                _report.Fail("blueprint", "Stock Cargo, Stock Refinery or Stock Light is missing after the paste");
                yield break;
            }

            refinery.Enabled = false;
            light.Enabled = false;

            // Time for Sprocket to take up the pasted programmable block and switch its logic system on.
            yield return 5 * PluginUpdates.PerSecond;

            var ingot = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ingot>("Iron");
            var ore = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Ore>("Iron");

            for (var round = 1; round <= 2; round++)
            {
                var flash = new LightFlash(light);
                cargo.GetInventory(0).AddItems(200, ingot);
                foreach (var wait in flash.Watch())
                    yield return wait;
                flash.Report(_report, $"cargo, round {round}");
                cargo.GetInventory(0).RemoveItemsOfType(200, ingot);
                yield return PluginUpdates.PerSecond;
            }

            for (var round = 1; round <= 2; round++)
            {
                var flash = new LightFlash(light);
                refinery.GetInventory(0).AddItems(200, ore);
                foreach (var wait in flash.Watch())
                    yield return wait;
                flash.Report(_report, $"refinery input, round {round}");
                refinery.GetInventory(0).RemoveItemsOfType(200, ore);
                yield return PluginUpdates.PerSecond;
            }
        }
    }
}
