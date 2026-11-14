using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class OxygenFarm
    {
        /// <summary>A farm being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyTerminalBlock _farm;
            readonly Action _told;

            public Listening(IMyTerminalBlock farm, Action told)
            {
                _farm = farm;
                _told = told;
                _farm.PropertiesChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _farm.PropertiesChanged -= Changed;
            }

            void Changed(IMyTerminalBlock block)
            {
                _told();
            }
        }
    }
}
