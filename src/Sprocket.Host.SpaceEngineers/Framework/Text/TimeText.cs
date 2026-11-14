using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Globalization;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Text
{
    /// <summary>
    /// How long, and how long ago, in the units a person would say them in.
    /// </summary>
    public static class TimeText
    {
        /// <summary>How many seconds are shown of a length, at most.</summary>
        const string SecondsFormat = "0.##";

        /// <summary><paramref name="length" /> in the unit it is not written in, as <paramref name="texts" /> word it: "= 80 ticks" for a length written in seconds.</summary>
        public static string Other(Texts texts, Duration length)
        {
            if (length.InSeconds)
                return length.Ticks == 1 ? texts.WaitAsTick : Template.Fill(texts.WaitAsTicks, "count", length.Ticks.ToString(CultureInfo.InvariantCulture));

            var seconds = (double)length.Ticks / Model.Duration.PerSecond;
            return seconds == 1 ? texts.WaitAsSecond : Template.Fill(texts.WaitAsSeconds, "count", seconds.ToString(SecondsFormat, CultureInfo.InvariantCulture));
        }

        /// <summary>A length of time in ticks.</summary>
        public static string Duration(int ticks)
        {
            return ticks == 1 ? "1 tick" : $"{ticks} ticks";
        }

        /// <summary>A length of time in the unit the program writes it in: seconds, or ticks.</summary>
        public static string Duration(Duration length)
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
        public static string Rate(Duration interval)
        {
            if (!interval.InSeconds)
                return Rate(interval.Ticks);

            var seconds = interval.Seconds;
            return seconds == 1 ? "second" : $"{seconds.ToString("R", CultureInfo.InvariantCulture)} seconds";
        }
    }
}
