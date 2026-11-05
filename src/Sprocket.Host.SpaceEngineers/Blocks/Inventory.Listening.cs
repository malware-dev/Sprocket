using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class Inventory
    {
        /// <summary>A block's inventories being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly List<IMyInventory> _inventories = new List<IMyInventory>();
            readonly Action _told;

            public Listening(IMyTerminalBlock block, Action told)
            {
                _told = told;
                for (var i = 0; i < block.InventoryCount; i++)
                {
                    var inventory = block.GetInventory(i);
                    inventory.OnVolumeChanged += Changed;
                    _inventories.Add(inventory);
                }
            }

            /// <inheritdoc />
            public void Dispose()
            {
                foreach (var inventory in _inventories)
                    inventory.OnVolumeChanged -= Changed;
            }

            void Changed(IMyInventory inventory, float before, float after)
            {
                _told();
            }
        }
    }
}
