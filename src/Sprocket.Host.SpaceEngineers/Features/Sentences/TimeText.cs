using System.Globalization;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>
    /// How long, and how long ago, in the units a person would say them in.
    /// </summary>
    /// <remarks>
    /// Core counts in ticks and only in ticks, which is the one honest unit it has: the simulation
    /// sags under load, so anything promising seconds would drift exactly when the machine matters
    /// most. Saying that in seconds and minutes is this target choosing readability over precision,
    /// which is the right trade for "recently, or ages ago" and the wrong one for deciding when to
    /// act - and nothing decides when to act from these. A length in ticks is the program's own,
    /// so it stays in the program's words; how long ago is <see cref="Ago" />'s.
    /// </remarks>
    public static class TimeText
    {
        /// <summary>A length of time, in ticks, which is the only unit a program writes one in.</summary>
        public static string Duration(int ticks)
        {
            return ticks == 1 ? "1 tick" : ticks + " ticks";
        }

        /// <summary>One of the lengths offered for a wait, said aloud in <paramref name="texts" /> and then in ticks.</summary>
        /// <remarks>
        /// Where somebody meets a tick for the first time, so the length they know sits beside it.
        /// </remarks>
        public static string WaitChoice(Texts texts, int ticks)
        {
            return Template.Fill(Template.Fill(texts.TimeChoice, "ticks", Duration(ticks)), "spoken", Spoken(texts, ticks));
        }


        public static string Rate(int ticks)
        {
            return ticks == 1 ? "tick" : ticks + " ticks";
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
