using System;
using VRage.Game.ModAPI.Interfaces;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class Railgun
    {
        /// <summary>A capacitor being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyEntityCapacitorComponent _capacitor;
            readonly Action _told;

            public Listening(IMyEntityCapacitorComponent capacitor, Action told)
            {
                _capacitor = capacitor;
                _told = told;
                _capacitor.OnCharged += Changed;
                _capacitor.OnNotCharged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _capacitor.OnCharged -= Changed;
                _capacitor.OnNotCharged -= Changed;
            }

            void Changed(IMyEntityCapacitorComponent capacitor)
            {
                _told();
            }
        }
    }
}
