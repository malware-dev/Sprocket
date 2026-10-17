
namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// A wait being looked at: when it began, and how long it was expected to take.
    /// </summary>
    /// <remarks>
    /// Looked at more often as its time closes in, down to every tick; past its time it slows again, never faster than
    /// every ten ticks.
    /// </remarks>
    public struct Hold
    {
        readonly int _since;
        readonly int _expected;

        /// <summary>A wait begun on a tick, expected to take so many ticks, or negative where nothing can say.</summary>
        public Hold(int since, int expected)
        {
            _since = since;
            _expected = expected;
        }

        /// <summary>How often the wait is looked at, as things stand on this tick.</summary>
        public int Period(int tick)
        {
            if (_expected < 0)
                return Rota.Ten;

            var left = _expected - (tick - _since);
            if (left > 0)
                return Slowest(left, Rota.Each);

            return Slowest(-left, Rota.Ten);
        }

        /// <summary>The slowest period that fits into this many ticks at least twice, and none faster than the least.</summary>
        static int Slowest(int ticks, int least)
        {
            if (ticks >= 2 * Rota.Hundred)
                return Rota.Hundred;

            if (ticks >= 2 * Rota.Ten && least <= Rota.Ten)
                return Rota.Ten;

            return least;
        }
    }
}
