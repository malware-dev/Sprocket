using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class Cockpit
    {
        /// <summary>A seat being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyCockpit _cockpit;
            readonly Action _told;

            public Listening(IMyCockpit cockpit, Action told)
            {
                _cockpit = cockpit;
                _told = told;
                _cockpit.IsOccupiedChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _cockpit.IsOccupiedChanged -= Changed;
            }

            void Changed(IMyCockpit cockpit)
            {
                _told();
            }
        }
    }
}
