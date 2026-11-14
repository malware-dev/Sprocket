using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class Sensor
    {
        /// <summary>A sensor being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMySensorBlock _sensor;
            readonly Action _told;

            public Listening(IMySensorBlock sensor, Action told)
            {
                _sensor = sensor;
                _told = told;
                _sensor.StateChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _sensor.StateChanged -= Changed;
            }

            void Changed(bool detecting)
            {
                _told();
            }
        }
    }
}
