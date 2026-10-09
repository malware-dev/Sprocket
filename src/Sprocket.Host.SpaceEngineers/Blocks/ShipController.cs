using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A cockpit's signals: each slot of the pilot's hotbar, and its target lock being gained and lost.</summary>
    /// <remarks>
    /// The game orders a ship controller's toolbars as the pilot's hotbar, the build toolbar and the target-lock toolbar. A
    /// hotbar key runs its slot on the page the pilot has open; the target lock runs the first slot of every page when it is
    /// gained and the second when it is lost. Nothing runs the build toolbar of itself, so it offers no signal.
    /// </remarks>
    public sealed class ShipController : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyShipController;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            IMyToolbar hotbar = Toolbars.Of(block, 1);
            for (var page = 1; hotbar != null && page <= hotbar.PageCount; page++)
            {
                for (var slot = 1; slot <= hotbar.SlotCount; slot++)
                    kind.WithSignal(new ToolbarSlot(1, page, slot), "hotbar page " + page + ", slot " + slot + " is pressed");
            }

            if (Toolbars.Of(block, 3) == null)
                return;

            kind.WithSignal(new ToolbarSlot(3, 0, 1), "gets a target lock")
                .WithSignal(new ToolbarSlot(3, 0, 2), "loses its target lock");
        }
    }
}
