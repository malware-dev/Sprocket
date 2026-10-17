using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class CryoChamber
    {
        /// <summary>A chamber being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyCryoChamber _chamber;
            readonly Action _told;

            public Listening(IMyCryoChamber chamber, Action told)
            {
                _chamber = chamber;
                _told = told;
                _chamber.IsOccupiedChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _chamber.IsOccupiedChanged -= Changed;
            }

            void Changed(IMyCockpit cockpit)
            {
                _told();
            }
        }
    }
}
