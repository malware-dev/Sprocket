using System;
using SpaceEngineers.Game.ModAPI;
using LandingGearMode = SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class LandingGear
    {
        /// <summary>A landing gear being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyLandingGear _gear;
            readonly Action _told;

            public Listening(IMyLandingGear gear, Action told)
            {
                _gear = gear;
                _told = told;
                _gear.LockModeChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _gear.LockModeChanged -= Changed;
            }

            void Changed(IMyLandingGear gear, LandingGearMode was)
            {
                _told();
            }
        }
    }
}
