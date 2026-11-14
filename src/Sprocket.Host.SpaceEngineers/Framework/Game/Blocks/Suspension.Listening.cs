using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class Suspension
    {
        /// <summary>A suspension being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyMotorSuspension _suspension;
            readonly Action _told;

            public Listening(IMyMotorSuspension suspension, Action told)
            {
                _suspension = suspension;
                _told = told;
                _suspension.OnAttachedChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _suspension.OnAttachedChanged -= Changed;
            }

            void Changed(IMyMechanicalConnectionBlock block)
            {
                _told();
            }
        }
    }
}
