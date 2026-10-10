using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A power producer being listened to for a change in its output, until disposed.</summary>
    /// <remarks>
    /// The game raises its output event from the main thread whenever the power the block gives, or the most it could
    /// give, moves.
    /// </remarks>
    public sealed class Producing : IDisposable
    {
        readonly IMyPowerProducer _producer;
        readonly Action _told;

        /// <summary>Listens to <paramref name="producer" />, telling <paramref name="told" /> each time its output changes.</summary>
        public Producing(IMyPowerProducer producer, Action told)
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
