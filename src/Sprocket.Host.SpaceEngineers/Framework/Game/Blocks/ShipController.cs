using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>What every seat and remote control has: each slot of the pilot's hotbar, its target lock gained and lost, and the ship's speed.</summary>
    /// <remarks>The game runs the first slot of every page of the target-lock toolbar as a lock is gained and the second as it is lost.</remarks>
    public sealed class ShipController : Capability
    {
        const string Unheard = "The game raises no event when a ship's speed changes.";

        static readonly OwnValue Speed = OwnValue.Number<IMyShipController>(controller => controller.GetShipSpeed());

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyShipController;
        }

        /// <inheritdoc />
        /// <remarks>
        /// The ship's speed is in meters per second. Each flight setting is offered where the game's terminal shows it, by tests
        /// transcribed from the game's ship controller.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("speed", "speed", Speed).Measured(" m/s").Looked(Unheard).In(Category.Flight);

            var flies = ((IMyShipController)block).CanControlShip;
            if (!flies)
                kind.Lacking("HandBrake", "Park", "DampenersOverride", "ControlThrusters", "ControlWheels", "ControlGyros");

            if (!flies || !(block is IMyCockpit))
                kind.Lacking("MainCockpit");

            if (!flies || block is IMyCryoChamber)
                kind.Lacking("HorizonIndicator");

            if (!(block is IMyCockpit) || !Locks(block))
                kind.Lacking("TargetLocking");

            IMyToolbar hotbar = Toolbars.Of(block, 1);
            if (hotbar != null && hotbar.PageCount > 0 && hotbar.SlotCount > 0)
                kind.WithPressed(1, "toolbar slot is pressed", hotbar.PageCount, hotbar.SlotCount,
                    at => $"toolbar page {at.Page}, slot {at.Slot} is pressed");

            if (Toolbars.Of(block, 3) == null)
                return;

            kind.WithSignal(new ToolbarSlot(3, 0, 1), "gets a target lock")
                .WithSignal(new ToolbarSlot(3, 0, 2), "loses its target lock");
        }

        /// <summary>Whether the definition of <paramref name="block" /> allows target locking, or nothing has been loaded to say.</summary>
        static bool Locks(IMyTerminalBlock block)
        {
            var definitions = MyDefinitionManager.Static;
            var definition = definitions?.GetCubeBlockDefinition(block.BlockDefinition) as MyShipControllerDefinition;

            return definition == null || definition.TargetLockingEnabled;
        }
    }
}
