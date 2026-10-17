using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class Thruster
    {
        /// <summary>A thruster being listened to for a change in its override, until disposed.</summary>
        /// <remarks>The game raises it on every machine as the override's synced value changes.</remarks>
        sealed class Overriding : IDisposable
        {
            readonly IMyThrust _thruster;
            readonly Action _told;

            public Overriding(IMyThrust thruster, Action told)
            {
                _thruster = thruster;
                _told = told;
                _thruster.ThrustOverrideChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _thruster.ThrustOverrideChanged -= Changed;
            }

            void Changed(IMyThrust thruster, float newtons)
            {
                _told();
            }
        }
    }
}
