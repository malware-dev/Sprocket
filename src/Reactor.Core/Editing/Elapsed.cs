namespace Reactor.Editing
{
    /// <summary>
    /// How long ago something happened, in words a person uses.
    /// </summary>
    /// <remarks>
    /// Ticks are the right unit for the machine and the wrong one for a human reading
    /// a diagnostic: "last ran 2417 ticks ago" asks the reader to do arithmetic before
    /// they can tell whether that is a problem. Seconds and minutes do not.
    ///
    /// The seconds are derived from ticks at the nominal rate, so under heavy load
    /// they run slightly short of wall-clock. That is fine here and would not be
    /// elsewhere: this only ever has to answer "recently, or ages ago", and it is
    /// never used to decide when something should happen.
    /// </remarks>
    static class Elapsed
    {
        const int TicksPerSecond = 60;

        /// <summary>
        /// A key that changes exactly when <see cref="Ago"/> would produce different
        /// words.
        /// </summary>
        /// <remarks>
        /// The point of this is to avoid building a string on every tick. Comparing
        /// the key costs nothing; the words are rebuilt roughly once a second at
        /// first, then once a minute, then not at all.
        /// </remarks>
        public static int Bucket(int ticksAgo)
        {
            if (ticksAgo < 0)
                return -1;

            var seconds = ticksAgo / TicksPerSecond;
            if (seconds < 2)
                return 0;
            if (seconds < 60)
                return seconds;
            if (seconds < 120)
                return 100;
            if (seconds < 3600)
                return 200 + seconds / 60;
            return 999;
        }

        public static string Ago(int ticksAgo)
        {
            if (ticksAgo < 0)
                return "";

            var seconds = ticksAgo / TicksPerSecond;
            if (seconds < 2)
                return "just now";
            if (seconds < 60)
                return seconds + " seconds ago";
            if (seconds < 120)
                return "a minute ago";
            if (seconds < 3600)
                return seconds / 60 + " minutes ago";
            return "over an hour ago";
        }
    }
}
