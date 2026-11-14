using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class LaserAntenna
    {
        /// <summary>A laser antenna being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyLaserAntenna _antenna;
            readonly Action _told;

            public Listening(IMyLaserAntenna antenna, Action told)
            {
                _antenna = antenna;
                _told = told;
                _antenna.PropertiesChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _antenna.PropertiesChanged -= Changed;
            }

            void Changed(IMyTerminalBlock block)
            {
                _told();
            }
        }
    }
}
