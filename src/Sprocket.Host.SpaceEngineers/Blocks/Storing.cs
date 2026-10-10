using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block that stores power, a battery or a jump drive, being listened to for a change in its charge, until disposed.</summary>
    /// <remarks>
    /// The game raises its charge event on the server each time the charge moves: every hundred frames on a battery that is
    /// switched on, and as a jump drive fills or empties.
    /// </remarks>
    public sealed class Storing : IDisposable
    {
        readonly IMyStoredPowerRatio _store;
        readonly Action _told;

        /// <summary>Listens to <paramref name="store" />, telling <paramref name="told" /> each time its charge changes.</summary>
        public Storing(IMyStoredPowerRatio store, Action told)
        {
            _store = store;
            _told = told;
            _store.StoredPowerRatioChanged += Changed;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _store.StoredPowerRatioChanged -= Changed;
        }

        void Changed(IMyStoredPowerRatio store, float before, float after)
        {
            _told();
        }
    }
}
