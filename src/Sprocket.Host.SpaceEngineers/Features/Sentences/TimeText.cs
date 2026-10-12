using System.Globalization;
using Sprocket.Host.SpaceEngineers.Localization;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>
    /// How long, and how long ago, in the units a person would say them in.
    /// </summary>
    /// <remarks>
    /// The runtime counts in ticks, and a second is sixty of them whatever the simulation's speed. A length the program
    /// writes is the program's own, so it is said in the unit the program writes it in; how long ago is
    /// <see cref="Ago" />'s, in seconds and minutes, which is readability over precision and right for "recently, or ages
    /// ago".
    /// </remarks>
    public static class TimeText
    {
        /// <summary>A length of time in ticks.</summary>
        public static string Duration(int ticks)
        {
            return ticks == 1 ? "1 tick" : ticks + " ticks";
        }

        /// <summary>A length of time in the unit the program writes it in: seconds, or ticks.</summary>
        public static string Duration(SpaceEngineers.Sequences.Duration length)
        {
            if (!length.InSeconds)
                return Duration(length.Ticks);

            var seconds = length.Seconds;
            return seconds == 1 ? "1 second" : seconds.ToString("R", CultureInfo.InvariantCulture) + " seconds";
        }

        /// <summary>One of the lengths offered for a wait, said aloud in <paramref name="texts" /> and then in ticks.</summary>
        /// <remarks>
        /// Where somebody meets a tick for the first time, so the length they know sits beside it.
        /// </remarks>
        public static string WaitChoice(Texts texts, int ticks)
        {
            return Template.Fill(Template.Fill(texts.TimeChoice, "ticks", Duration(ticks)), "spoken", Spoken(texts, ticks));
        }


        /// <summary>How often, after "every", in ticks.</summary>
        public static string Rate(int ticks)
        {
            return ticks == 1 ? "tick" : ticks + " ticks";
        }

        /// <summary>How often, after "every", in the unit the program writes it in: seconds, or ticks.</summary>
        public static string Rate(SpaceEngineers.Sequences.Duration interval)
        {
            if (!interval.InSeconds)
                return Rate(interval.Ticks);

            var seconds = interval.Seconds;
            return seconds == 1 ? "second" : seconds.ToString("R", CultureInfo.InvariantCulture) + " seconds";
        }

        /// <summary>A length of time as it would be said aloud, for a choice that also gives its ticks.</summary>
        static string Spoken(Texts texts, int ticks)
        {
            if (ticks < 60)
                return Duration(ticks);

            var seconds = ticks / 60;
            if (seconds < 60)
                return seconds == 1 ? texts.TimeSecond : Template.Fill(texts.TimeSeconds, "count", seconds.ToString(CultureInfo.InvariantCulture));

            var minutes = seconds / 60;
            return minutes == 1 ? texts.TimeMinute : Template.Fill(texts.TimeMinutes, "count", minutes.ToString(CultureInfo.InvariantCulture));
        }
    }
}
