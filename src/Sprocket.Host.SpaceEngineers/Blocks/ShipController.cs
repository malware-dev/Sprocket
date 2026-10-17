using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// What every seat and remote control has: each slot of the pilot's hotbar, its target lock being gained and lost, and the
    /// ship's speed.
    /// </summary>
    /// <remarks>
    /// The game orders a ship controller's toolbars as the pilot's hotbar, the build toolbar and the target-lock toolbar. A
    /// hotbar key runs its slot on the page the pilot has open; the target lock runs the first slot of every page when it is
    /// gained and the second when it is lost. Nothing runs the build toolbar of itself, so it offers no signal.
    /// </remarks>
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
        /// The ship's speed is the same through any seat or remote control of it, in metres a second. Every ship controller
        /// carries every seat's flight settings, and the game's terminal shows each only where its own test holds: the flight
        /// settings where the block can control the ship, which a shower or a toilet cannot, the main cockpit on a cockpit
        /// alone, a horizon anywhere but a cryo chamber, and target locking on a cockpit whose definition allows it. Each test
        /// is transcribed from the game's ship controller.
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
                kind.WithPressed(1, "hotbar slot is pressed", hotbar.PageCount, hotbar.SlotCount,
                    at => $"hotbar page {at.Page}, slot {at.Slot} is pressed");

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
