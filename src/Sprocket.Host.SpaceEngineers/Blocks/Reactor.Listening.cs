using System;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class Reactor
    {
        /// <summary>A reactor's inventory and output being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyInventory _inventory;
            readonly Action _told;
            readonly OutputListener _output;

            public Listening(IMyInventory inventory, IMyPowerProducer reactor, Action told)
            {
                _inventory = inventory;
                _told = told;
                _inventory.OnVolumeChanged += Changed;
                _output = new OutputListener(reactor, told);
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _inventory.OnVolumeChanged -= Changed;
                _output.Dispose();
            }

            void Changed(IMyInventory inventory, float before, float after)
            {
                _told();
            }
        }
    }
}
