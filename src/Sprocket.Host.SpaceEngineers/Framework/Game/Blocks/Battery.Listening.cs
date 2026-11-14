using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class Battery
    {
        /// <summary>A battery's charge and output being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly ChargeListener _charge;
            readonly OutputListener _output;

            public Listening(ChargeListener charge, OutputListener output)
            {
                _charge = charge;
                _output = output;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _charge.Dispose();
                _output.Dispose();
            }
        }
    }
}
