namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// A wait being looked at: when it began, and how long it was expected to take.
    /// </summary>
    /// <remarks>
    /// How often it is looked at follows from how much time it should have left: the slowest period
    /// that fits into that at least twice, so a wait tightens as it closes in and the last few ticks
    /// are looked at one by one.
    /// <para>
    /// Past its time it slows down again rather than staying on every tick. Being late means the
    /// estimate was wrong - a door is blocked, a rotor has stalled - and something that may never
    /// arrive must not be looked at sixty times a second for as long as it does not. The slowest
    /// period that fits twice into how late it is, and never faster than ten.
    /// </para>
    /// </remarks>
    struct Hold
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
