using System.Collections.Generic;
using System.Reflection;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// Testing row "A turret controller's loses its aim", run in the real game on a custom turret controller built by the test:
    /// a rotor with a camera on its head looking straight up, and an enemy's grid 200 m up its line, so it is on target at once.
    /// The controller's first toolbar slot switches one light on as it comes to bear, its second another as it loses its aim; the
    /// target is then taken away. Sprocket's two signals are those two slots.
    /// </summary>
    public sealed class TurretControllerGameTest
    {
        static readonly Vector3D At = new Vector3D(0, 0, 560);

        readonly GameTestReport _report;

        /// <summary>The test, answering into <paramref name="report" />.</summary>
        public TurretControllerGameTest(GameTestReport report)
        {
            _report = report;
        }

        /// <summary>The test's steps, each yielding the plugin updates to wait before the next.</summary>
        public IEnumerator<int> Run()
        {
            var players = MySession.Static.Players;
            var us = players.CreateNewIdentity("Sprocket Gunner", "", null).IdentityId;
            var them = players.CreateNewIdentity("Sprocket Target", "", null).IdentityId;

            var grid = new BuiltGrid(At, BuiltGrid.Battery(Vector3I.Zero),
                BuiltGrid.Block<MyObjectBuilder_TurretControlBlock>("LargeTurretControlBlock", new Vector3I(1, 0, 0)),
                BuiltGrid.Block<MyObjectBuilder_MotorStator>("LargeStator", new Vector3I(3, 0, 0)),
                BuiltGrid.Block<MyObjectBuilder_InteriorLight>("SmallLight", new Vector3I(0, 0, 2)),
                BuiltGrid.Block<MyObjectBuilder_InteriorLight>("SmallLight", new Vector3I(1, 0, 2)));
            var controller = grid.Find<IMyTurretControlBlock>();
            var rotor = grid.Find<IMyMotorStator>();
            var onTarget = grid.At<IMyLightingBlock>(new Vector3I(0, 0, 2));
            var lostAim = grid.At<IMyLightingBlock>(new Vector3I(1, 0, 2));
            if (controller == null || rotor == null || onTarget == null || lostAim == null)
            {
                _report.Fail("turret controller", "the controller, its rotor or its two lights were not built");
                yield break;
            }

            controller.CubeGrid.ChangeGridOwnership(us, MyOwnershipShareModeEnum.None);

            // The large head of a large rotor, by the game's own action, once the grid has settled (PistonStatusGameTest).
            yield return 3 * PluginUpdates.PerSecond;
            rotor.GetActionWithName("AddRotorTopPart").Apply(rotor);
            yield return 3 * PluginUpdates.PerSecond;
            var head = rotor.Top;
            if (head == null)
            {
                _report.Fail("turret head", "AddRotorTopPart gave the rotor no head");
                yield break;
            }

            // A camera on top of the head, looking along the rotor's axis.
            var up = head.Orientation.Up;
            var camera = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_CameraBlock>("LargeCameraBlock");
            camera.Min = head.Position + Base6Directions.GetIntVector(up);
            camera.BlockOrientation = new SerializableBlockOrientation(up, head.Orientation.Forward);
            camera.Enabled = true;
            var eye = head.CubeGrid.AddBlock(camera, false)?.FatBlock as IMyCameraBlock;
            if (eye == null)
            {
                _report.Fail("turret camera", "the game would not put a camera on the rotor's head");
                yield break;
            }

            head.CubeGrid.ChangeGridOwnership(us, MyOwnershipShareModeEnum.None);
            onTarget.Enabled = false;
            lostAim.Enabled = false;
            Slot(controller, 0, onTarget);
            Slot(controller, 1, lostAim);

            controller.AzimuthRotor = rotor;
            controller.Camera = eye;
            controller.Range = 800;
            controller.TargetLargeGrids = true;
            controller.TargetStations = true;
            controller.TargetNeutrals = false;
            controller.AIEnabled = true;
            yield return PluginUpdates.PerSecond;

            var target = new BuiltGrid(eye.GetPosition() + eye.WorldMatrix.Forward * 200, BuiltGrid.Battery(Vector3I.Zero),
                BuiltGrid.Block<MyObjectBuilder_InteriorLight>("SmallLight", new Vector3I(1, 0, 0)));
            var aim = target.Find<IMyBatteryBlock>();
            aim.CubeGrid.ChangeGridOwnership(them, MyOwnershipShareModeEnum.None);

            var thirds = 0;
            while (!onTarget.Enabled && thirds < 30)
            {
                thirds++;
                yield return PluginUpdates.PerSecond / 3;
            }

            _report.Note($"the controller aims at {Named(controller)}, the target grid is {aim.CubeGrid.EntityId}");
            _report.Check("a turret controller comes to bear", controller.HasTarget && controller.IsAimed && onTarget.Enabled && !lostAim.Enabled,
                $"after {thirds / 3.0:0.#} s it has a target: {controller.HasTarget}, is aimed: {controller.IsAimed}, the first slot's light is {Said(onTarget)}, the second's {Said(lostAim)}");

            aim.CubeGrid.Close();
            thirds = 0;
            while (!lostAim.Enabled && thirds < 30)
            {
                thirds++;
                yield return PluginUpdates.PerSecond / 3;
            }

            _report.Check("a turret controller whose target is taken away", !controller.HasTarget && lostAim.Enabled,
                $"{thirds / 3.0:0.#} s after the target went it has a target: {controller.HasTarget}, is aimed: {controller.IsAimed}, the second slot's light is {Said(lostAim)}");

            controller.AIEnabled = false;
        }

        /// <summary>Puts switching <paramref name="light" /> on in slot <paramref name="index" /> of the first page of <paramref name="controller" />'s toolbar.</summary>
        /// <remarks>The toolbar is the block's own field: the mod interface reaches no toolbar of a turret controller.</remarks>
        static void Slot(IMyTurretControlBlock controller, int index, IMyTerminalBlock light)
        {
            var field = controller.GetType().GetField("m_toolbar", BindingFlags.Instance | BindingFlags.NonPublic);
            var toolbar = (MyToolbar)field.GetValue(controller);
            var item = new MyObjectBuilder_ToolbarItemTerminalBlock { BlockEntityId = light.EntityId, _Action = "OnOff_On" };
            toolbar.SetItemAtIndex(index, MyToolbarItemFactory.CreateToolbarItem(item));
        }

        /// <summary>What <paramref name="controller" /> is aimed at: the entity and how far away it is, or nothing.</summary>
        static string Named(IMyTurretControlBlock controller)
        {
            var aimed = controller.GetTargetedEntity();
            return aimed.EntityId == 0 ? "nothing" : $"{aimed.Name} {aimed.EntityId} of {aimed.Type}, {Vector3D.Distance(aimed.Position, controller.GetPosition()):0} m away";
        }

        static string Said(IMyFunctionalBlock light) => light.Enabled ? "on" : "off";
    }
}
