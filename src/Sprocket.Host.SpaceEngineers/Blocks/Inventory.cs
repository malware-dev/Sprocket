using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block with an inventory: how full it is and how heavy its load, and moving items, which is still to come.</summary>
    /// <remarks>
    /// How full and how heavy are read on a block with one inventory only. On a block with several, which one a reading
    /// means is an option, and a value takes none.
    /// </remarks>
    public sealed class Inventory : Capability
    {
        static readonly Being Fill = Being.Number<IMyTerminalBlock>(block => block.GetInventory(0).VolumeFillFactor * 100d);

        static readonly Being Mass = Being.Number<IMyTerminalBlock>(block => (double)block.GetInventory(0).CurrentMass);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block.InventoryCount > 0;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Planning(Plan.MovingItems);
            if (block.InventoryCount != 1)
                return;

            kind.WithValue("inventory-fill", "inventory fill", Fill).In(Category.Stock).Announced()
                .WithValue("inventory-mass", "inventory mass", Mass).In(Category.Stock).Announced();
        }

        /// <summary>Told as anything goes into or out of the inventory, which every inventory announces.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var inventory = block.InventoryCount == 1 ? block.GetInventory(0) : null;
            return inventory == null ? null : new Listening(inventory, told);
        }

        /// <summary>An inventory being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyInventory _inventory;
            readonly Action _told;

            public Listening(IMyInventory inventory, Action told)
            {
                _inventory = inventory;
                _told = told;
                _inventory.OnVolumeChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _inventory.OnVolumeChanged -= Changed;
            }

            void Changed(IMyInventory inventory, float before, float after)
            {
                _told();
            }
        }
    }
}
