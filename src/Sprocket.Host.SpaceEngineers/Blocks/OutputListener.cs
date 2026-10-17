using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A power producer being listened to for a change in its output, until disposed.</summary>
    /// <remarks>The game raises the output event on the main thread whenever the power given, or the most it could give, moves.</remarks>
    public sealed class OutputListener : IDisposable
    {
        readonly IMyPowerProducer _producer;
        readonly Action _told;

        /// <summary>Listens to <paramref name="producer" />, telling <paramref name="told" /> each time its output changes.</summary>
        public OutputListener(IMyPowerProducer producer, Action told)
        {
            _producer = producer;
            _told = told;
            _producer.CurrentOutputRatioChanged += Changed;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _producer.CurrentOutputRatioChanged -= Changed;
        }

        void Changed(IMyPowerProducer producer, float before, float after)
        {
            _told();
        }
    }
}
