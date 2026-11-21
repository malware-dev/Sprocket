using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRageMath;
using LandingGearMode = SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode;
using MyShipConnectorStatus = Sandbox.ModAPI.Ingame.MyShipConnectorStatus;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "A connector and a magnetic plate that unlock", run in the real game on the Sprocket Dock Unlocks blueprint: a
    /// free grid docks to Dock Connector and a block is held to Dock Plate, both lock with their lights left on, and each light goes
    /// off as its own block unlocks.
    /// </summary>
    public sealed class DockUnlocksGameTest
    {
        /// <summary>The length of a large grid's cell, in meters.</summary>
        const double Cell = 2.5;

        readonly string _blueprint;
        readonly GameTestReport _report;

        /// <summary>The test on the scenario's blueprint in <paramref name="blueprints" />, answering into <paramref name="report" />.</summary>
        public DockUnlocksGameTest(string blueprints, GameTestReport report)
        {
            _blueprint = Path.Combine(blueprints, "Sprocket Dock Unlocks", "bp.sbc");
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var pasted = new PastedBlueprint(_blueprint, new Vector3D(1500, 0, 0));
            var connector = pasted.Find("Dock Connector") as IMyShipConnector;
            var plate = pasted.Find("Dock Plate") as IMyLandingGear;
            var connectorLight = pasted.Find("Connector Light") as IMyFunctionalBlock;
            var plateLight = pasted.Find("Plate Light") as IMyFunctionalBlock;
            if (connector == null || plate == null || connectorLight == null || plateLight == null)
            {
                _report.Fail("dock unlocks blueprint", "a block of the dock is missing after the paste");
                yield break;
            }

            yield return 3 * PluginUpdates.PerSecond;

            // A ship's connector one cell out from Dock Connector's face, turned to face it.
            var facing = connector.WorldMatrix;
            new BuiltGrid(facing.Translation + facing.Forward * Cell, -(Vector3)facing.Forward, (Vector3)facing.Up, false,
                BuiltGrid.Block<MyObjectBuilder_ShipConnector>("Connector", Vector3I.Zero));

            // Dock Plate locks only when told, so its locking is the test's own act.
            plate.AutoLock = false;

            // A block on each open face of Dock Plate in turn, until the plate is ready to lock to one.
            var held = Held(plate);
            while (held.MoveNext())
                yield return held.Current;

            yield return 2 * PluginUpdates.PerSecond;
            if (connector.Status != MyShipConnectorStatus.Connectable || plate.LockMode != LandingGearMode.ReadyToLock)
            {
                _report.Fail("dock unlocks setup", $"Dock Connector is {connector.Status} and Dock Plate {plate.LockMode} with the ship and the block beside them");
                yield break;
            }

            connector.Connect();
            plate.Lock();
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("a connector and a plate that lock", connector.Status == MyShipConnectorStatus.Connected
                    && plate.LockMode == LandingGearMode.Locked && connectorLight.Enabled && plateLight.Enabled,
                $"Dock Connector is {connector.Status}, Dock Plate {plate.LockMode}, Connector Light {Said(connectorLight)} and Plate Light {Said(plateLight)}");

            connector.Disconnect();
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("a connector that unlocks", !connectorLight.Enabled && plateLight.Enabled,
                $"Dock Connector is {connector.Status}, Connector Light {Said(connectorLight)} and Plate Light {Said(plateLight)}");

            plate.Unlock();
            yield return 2 * PluginUpdates.PerSecond;
            _report.Check("a magnetic plate that unlocks", !plateLight.Enabled,
                $"Dock Plate is {plate.LockMode} and Plate Light {Said(plateLight)}");
        }

        /// <summary>
        /// Puts a free block of armor against each side of <paramref name="plate" />'s model with no block of its own grid beyond it,
        /// until the plate is ready to lock to one.
        /// </summary>
        IEnumerator<int> Held(IMyLandingGear plate)
        {
            var matrix = plate.WorldMatrix;
            var model = plate.LocalAABB;
            foreach (var side in new[] { Vector3.Forward, Vector3.Backward, Vector3.Up, Vector3.Down, Vector3.Left, Vector3.Right })
            {
                var outward = Vector3D.TransformNormal(side, matrix);
                if (plate.CubeGrid.CubeExists(plate.CubeGrid.WorldToGridInteger(matrix.Translation + outward * Cell)))
                    continue;

                // The plate's face on this side, from its model, with a block's half and a hair of air beyond it.
                var face = Math.Max(Vector3.Dot(model.Min, side), Vector3.Dot(model.Max, side));
                var block = new BuiltGrid(matrix.Translation + outward * (face + Cell / 2 + 0.05), false, BuiltGrid.Armor(Vector3I.Zero));
                yield return PluginUpdates.PerSecond;
                _report.Note($"a block {face:0.##} m out from Dock Plate's {Base6Directions.GetDirection(side)} side: the plate is {plate.LockMode}");
                if (plate.LockMode == LandingGearMode.ReadyToLock)
                    yield break;

                block.Grid.Close();
                yield return 2;
            }
        }

        static string Said(IMyFunctionalBlock light) => light.Enabled ? "on" : "off";
    }
}
