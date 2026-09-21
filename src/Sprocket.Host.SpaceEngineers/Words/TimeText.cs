
namespace Sprocket.Host.SpaceEngineers.Words
{
    /// <summary>
    /// How long, and how long ago, in the units a person would say them in.
    /// </summary>
    /// <remarks>
    /// Core counts in ticks and only in ticks, which is the one honest unit it has: the simulation
    /// sags under load, so anything promising seconds would drift exactly when the machine matters
    /// most. Saying that in seconds and minutes is this target choosing readability over precision,
    /// which is the right trade for "recently, or ages ago" and the wrong one for deciding when to
    /// act - and nothing decides when to act from these.
    /// </remarks>
    public static class TimeText
    {
        const int TicksPerSecond = 60;

        /// <summary>What the box for a length nobody offered says while it is empty.</summary>
        public const string OtherWait = "Another length, in ticks";

        /// <summary>The button beside that box, which takes the length typed in it.</summary>
        public const string UseLength = "Use";

        static readonly string[] Minutes = BuildMinutes();


        /// <summary>A length of time, in ticks, which is the only unit a program writes one in.</summary>
        public static string Duration(int ticks)
        {
            return ticks == 1 ? "1 tick" : ticks + " ticks";
        }

        /// <summary>
        /// How long ago, in words.
        /// </summary>
        /// <remarks>
        /// Every answer comes from a table built once, so this can be called on every tick for every
        /// visible sequence without allocating.
        /// <para>
        /// The seconds are derived from ticks at the nominal rate, so under heavy load they run
        /// slightly short of wall-clock. Fine here, and it would not be elsewhere: this only ever has
        /// to answer "recently, or ages ago", and nothing decides when to act from it.
        /// </para>
        /// </remarks>
        public static string Ago(int ticksAgo)
        {
            if (ticksAgo < 0)
                return "";

            var seconds = ticksAgo / TicksPerSecond;
            if (seconds < 2)
                return "just now";

            // One answer for the whole first minute, rather than a new one every second. Counting
            // seconds changes what every row says once a second for as long as a screen is open,
            // and wakes everything downstream of it to be told a number nobody is reading - a
            // construct with fifty sequences pays that fifty times a second, for ever.
            //
            // Nothing is lost with it. What second-level words were good for is saying that
            // something has just happened, and the light on the row says that faster and without
            // being read.
            if (seconds < 60)
                return "less than a minute ago";
            if (seconds < 120)
                return "a minute ago";

            var minutes = seconds / 60;
            if (minutes < 60)
                return Minutes[minutes];

            return "over an hour ago";
        }


        /// <summary>One of the lengths offered for a wait, said aloud and then in ticks.</summary>
        /// <remarks>
        /// Where somebody meets a tick for the first time, so the length they know sits beside it.
        /// </remarks>
        public static string WaitChoice(int ticks)
        {
            return Spoken(ticks) + " - " + Duration(ticks);
        }


        public static string Rate(int ticks)
        {
            return ticks == 1 ? "tick" : ticks + " ticks";
        }

        /// <summary>A length of time as it would be said aloud, for a choice that also gives its ticks.</summary>
        static string Spoken(int ticks)
        {
            if (ticks < 60)
                return Duration(ticks);

            var seconds = ticks / 60;
            if (seconds < 60)
                return seconds == 1 ? "1 second" : seconds + " seconds";

            var minutes = seconds / 60;
            return minutes == 1 ? "a minute" : minutes + " minutes";
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
