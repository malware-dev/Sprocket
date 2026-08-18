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

        static readonly string[] _seconds = BuildSeconds();
        static readonly string[] _minutes = BuildMinutes();

        /// <summary>
        /// How long ago, in words.
        /// </summary>
        /// <remarks>
        /// Every answer comes from a table built once, so this can be called on every
        /// tick for every visible sequence without allocating. That is why nothing
        /// caches the result any more - there is nothing left to cache.
        /// </remarks>
        public static string Ago(int ticksAgo)
        {
            if (ticksAgo < 0)
                return "";

            var seconds = ticksAgo / TicksPerSecond;
            if (seconds < 2)
                return "just now";
            if (seconds < 60)
                return _seconds[seconds];
            if (seconds < 120)
                return "a minute ago";

            var minutes = seconds / 60;
            if (minutes < 60)
                return _minutes[minutes];

            return "over an hour ago";
        }

        static string[] BuildSeconds()
        {
            var words = new string[60];
            for (var i = 2; i < 60; i++)
                words[i] = i + " seconds ago";
            return words;
        }

        static string[] BuildMinutes()
        {
            var words = new string[60];
            for (var i = 2; i < 60; i++)
                words[i] = i + " minutes ago";
            return words;
        }
    }
}
