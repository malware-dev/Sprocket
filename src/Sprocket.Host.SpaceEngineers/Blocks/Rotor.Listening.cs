using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class Rotor
    {
        /// <summary>A rotor being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyMotorStator _rotor;
            readonly Action _told;

            public Listening(IMyMotorStator rotor, Action told)
            {
                _rotor = rotor;
                _told = told;
                _rotor.OnAttachedChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _rotor.OnAttachedChanged -= Changed;
            }

            void Changed(IMyMechanicalConnectionBlock block)
            {
                _told();
            }
        }
    }
}
