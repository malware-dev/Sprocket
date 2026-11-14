using System;
using Sandbox.Game.EntityComponents;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class RemoteControl
    {
        /// <summary>An autopilot being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly MyAutopilotComponent _autopilot;
            readonly Action _told;

            public Listening(MyAutopilotComponent autopilot, Action told)
            {
                _autopilot = autopilot;
                _told = told;
                _autopilot.OnCurrentWaypointIndexChanged += told;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _autopilot.OnCurrentWaypointIndexChanged -= _told;
            }
        }
    }
}
