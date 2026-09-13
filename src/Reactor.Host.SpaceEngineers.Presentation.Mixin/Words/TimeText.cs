using System.Collections.Generic;
using Reactor.Editing;

namespace Reactor.Host.SpaceEngineers.Presentation.Words
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
    public sealed class TimeText : ITimeText
    {
        const int TicksPerSecond = 60;


        /// <summary>
        /// A length of time in the units a person would say it in.
        /// </summary>
        /// <remarks>
        /// The tick count is still shown alongside when the duration is being chosen, which is where
        /// the two vocabularies need to meet.
        /// </remarks>
        public static string Duration(int ticks)
        {
            if (ticks < 60)
                return ticks == 1 ? "1 tick" : ticks + " ticks";

            var seconds = ticks / 60;
            if (seconds < 60)
                return seconds == 1 ? "1 second" : seconds + " seconds";

            var minutes = seconds / 60;
            return minutes == 1 ? "a minute" : minutes + " minutes";
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
        public string Ago(int ticksAgo)
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


        public string WaitChoice(int ticks)
        {
            return Duration(ticks) + " - " + ticks + " ticks";
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


        static readonly string[] _seconds = BuildSeconds();

        static readonly string[] _minutes = BuildMinutes();


        public static string Rate(int ticks)
        {
            return ticks == 1 ? "tick" : ticks + " ticks";
        }
    }
}
