using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "A railgun's time until charged with no power", run in the real game on a railgun and a battery built by the
    /// test: what its capacitor says of the time remaining while it charges, and once the grid has no power.
    /// </summary>
    public sealed class RailgunGameTest
    {
        readonly GameTestReport _report;

        /// <summary>The test, answering into <paramref name="report" />.</summary>
        public RailgunGameTest(GameTestReport report)
        {
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var grid = new BuiltGrid(new Vector3D(0, 0, 380), BuiltGrid.Battery(Vector3I.Zero),
                BuiltGrid.Block<MyObjectBuilder_SmallMissileLauncherReload>("LargeRailgun", new Vector3I(1, 0, 0)));
            var railgun = grid.Find<IMySmallMissileLauncherReload>();
            var battery = grid.Find<IMyBatteryBlock>();
            var capacitor = railgun == null ? null : Capacitor(railgun);
            if (capacitor == null || battery == null)
            {
                _report.Fail("railgun", "the railgun, its capacitor or its battery was not built");
                yield break;
            }

            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("a railgun charging on power", !capacitor.IsCharged && capacitor.TimeRemaining > 0 && !float.IsPositiveInfinity(capacitor.TimeRemaining),
                Said(capacitor));

            // Sprocket reads endless time to go as not charging.
            battery.Enabled = false;
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("a railgun with no power on the grid", float.IsPositiveInfinity(capacitor.TimeRemaining) && !capacitor.IsCharged,
                $"{Said(capacitor)}, working {railgun.IsWorking}");
            battery.Enabled = true;
            yield return PluginUpdates.PerSecond;

            railgun.Enabled = false;
            yield return 3 * PluginUpdates.PerSecond;
            _report.Check("a railgun switched off", capacitor.TimeRemaining == 0 && !capacitor.IsCharged, $"with power on the grid: {Said(capacitor)}");
            railgun.Enabled = true;
        }

        static string Said(IMyEntityCapacitorComponent capacitor)
        {
            return $"time remaining {capacitor.TimeRemaining} (infinite {float.IsPositiveInfinity(capacitor.TimeRemaining)}), stored {capacitor.StoredPower} of {capacitor.Capacity}, charged {capacitor.IsCharged}";
        }

        static IMyEntityCapacitorComponent Capacitor(IMyTerminalBlock block)
        {
            foreach (var component in block.Components)
            {
                var capacitor = component as IMyEntityCapacitorComponent;
                if (capacitor != null)
                    return capacitor;
            }

            return null;
        }
    }
}
