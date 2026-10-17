using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block with an inventory, read for how full it is and how heavy its load.</summary>
    /// <remarks>How full and how heavy are read only on a block with one inventory.</remarks>
    public sealed partial class Inventory : Capability
    {
        static readonly OwnValue Fill = OwnValue.Number<IMyTerminalBlock>(block => block.GetInventory(0).VolumeFillFactor * 100d);

        static readonly OwnValue Mass = OwnValue.Number<IMyTerminalBlock>(block => (double)block.GetInventory(0).CurrentMass);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block.InventoryCount > 0;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Planning(PlannedMember.MovingItems);
            if (block.InventoryCount != 1)
                return;

            kind.WithValue("inventory-fill", "inventory fill", Fill).Percent().In(Category.Stock).Announced()
                .WithValue("inventory-mass", "inventory mass", Mass).Measured(" kg").In(Category.Stock).Announced();
        }

        /// <summary>Told as anything goes into or out of the inventory.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var inventory = block.InventoryCount == 1 ? block.GetInventory(0) : null;
            return inventory == null ? null : new Listening(inventory, told);
        }
    }
}
