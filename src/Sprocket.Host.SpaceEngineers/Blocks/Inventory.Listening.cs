using System;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class Inventory
    {
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
