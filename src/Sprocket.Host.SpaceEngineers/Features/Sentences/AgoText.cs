using System.Globalization;
using Sprocket.Host.SpaceEngineers.Localization;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>How long ago something happened, in words.</summary>
    /// <remarks>
    /// Asked every tick for every visible sequence, so every answer is a string made once. Seconds count ticks at the
    /// nominal rate, so under heavy load they run short of wall-clock.
    /// </remarks>
    public sealed class AgoText
    {
        const int TicksPerSecond = 60;

        readonly Texts _texts;
        readonly string[] _minutes = new string[60];

        string _made;

        /// <summary>Times said in <paramref name="texts" />.</summary>
        public AgoText(Texts texts)
        {
            _texts = texts;
        }

        /// <summary>How long ago <paramref name="ticksAgo" /> ticks is, or empty for a time that has not come.</summary>
        public string Said(int ticksAgo)
        {
            if (ticksAgo < 0)
                return "";

            var seconds = ticksAgo / TicksPerSecond;
            if (seconds < 2)
                return _texts.TimeJustNow;

            // One answer for the whole first minute: a count of seconds would change every open row
            // once a second.
            if (seconds < 60)
                return _texts.TimeUnderMinute;
            if (seconds < 120)
                return _texts.TimeMinuteAgo;

            var minutes = seconds / 60;
            if (minutes < 60)
                return Minutes()[minutes];

            return _texts.TimeHourAgo;
        }

        /// <summary>The table of minutes in the language loaded, made again where the language has changed.</summary>
        string[] Minutes()
        {
            var template = _texts.TimeMinutesAgo;
            if (ReferenceEquals(template, _made))
                return _minutes;

            _made = template;
            for (var i = 2; i < 60; i++)
                _minutes[i] = Template.Fill(template, "count", i.ToString(CultureInfo.InvariantCulture));

            return _minutes;
        }
    }
}
