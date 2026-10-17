using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>
    /// How long, and how long ago, in the units a person would say them in.
    /// </summary>
    /// <remarks>
    /// The runtime counts in ticks, and a second is sixty of them whatever the simulation's speed. A length the program
    /// writes is the program's own, so it is said in the unit the program writes it in; how long ago is
    /// <see cref="AgoText" />'s, in seconds and minutes, which is readability over precision and right for "recently, or ages
    /// ago".
    /// </remarks>
    public static class TimeText
    {
        /// <summary>A length of time in ticks.</summary>
        public static string Duration(int ticks)
        {
            return ticks == 1 ? "1 tick" : $"{ticks} ticks";
        }

        /// <summary>A length of time in the unit the program writes it in: seconds, or ticks.</summary>
        public static string Duration(SpaceEngineers.Sequences.Duration length)
        {
            if (!length.InSeconds)
                return Duration(length.Ticks);

            var seconds = length.Seconds;
            return seconds == 1 ? "1 second" : $"{seconds.ToString("R", CultureInfo.InvariantCulture)} seconds";
        }

        /// <summary>How often, after "every", in ticks.</summary>
        public static string Rate(int ticks)
        {
            return ticks == 1 ? "tick" : $"{ticks} ticks";
        }

        /// <summary>How often, after "every", in the unit the program writes it in: seconds, or ticks.</summary>
        public static string Rate(SpaceEngineers.Sequences.Duration interval)
        {
            if (!interval.InSeconds)
                return Rate(interval.Ticks);

            var seconds = interval.Seconds;
            return seconds == 1 ? "second" : $"{seconds.ToString("R", CultureInfo.InvariantCulture)} seconds";
        }
    }
}
