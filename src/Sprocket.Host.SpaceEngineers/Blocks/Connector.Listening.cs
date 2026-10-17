using System;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class Connector
    {
        /// <summary>A connector being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyShipConnector _connector;
            readonly Action _told;

            public Listening(IMyShipConnector connector, Action told)
            {
                _connector = connector;
                _told = told;
                _connector.IsConnectedChanged += Changed;
                _connector.AttachFinished += Changed;
                _connector.DetachFinished += Changed;
                _connector.IsWorkingChanged += Working;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _connector.IsConnectedChanged -= Changed;
                _connector.AttachFinished -= Changed;
                _connector.DetachFinished -= Changed;
                _connector.IsWorkingChanged -= Working;
            }

            void Changed(IMyShipConnector connector)
            {
                _told();
            }

            void Working(IMyCubeBlock block)
            {
                _told();
            }
        }
    }
}
