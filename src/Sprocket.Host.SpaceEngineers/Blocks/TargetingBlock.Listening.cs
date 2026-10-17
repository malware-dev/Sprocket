using System;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class TargetingBlock
    {
        /// <summary>A block's controls being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyControllerInfo _controls;
            readonly Action _told;

            public Listening(IMyControllerInfo controls, Action told)
            {
                _controls = controls;
                _told = told;
                _controls.ControlAcquired += Changed;
                _controls.ControlReleased += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _controls.ControlAcquired -= Changed;
                _controls.ControlReleased -= Changed;
            }

            void Changed(IMyEntityController controller)
            {
                _told();
            }
        }
    }
}
