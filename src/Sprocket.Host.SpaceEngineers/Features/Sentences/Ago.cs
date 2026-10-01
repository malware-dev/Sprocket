using System.Globalization;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>How long ago something happened, in words.</summary>
    /// <remarks>
    /// Asked on every tick for every visible sequence, so every answer is a string made once: the minutes are a table,
    /// made again only when the language changes.
    /// <para>
    /// The seconds are derived from ticks at the nominal rate, so under heavy load they run slightly short of
    /// wall-clock. Fine here, and it would not be elsewhere: this only ever has to answer "recently, or ages ago", and
    /// nothing decides when to act from it.
    /// </para>
    /// </remarks>
    public sealed class Ago
    {
        const int TicksPerSecond = 60;

        readonly Texts _texts;
        readonly string[] _minutes = new string[60];

        string _made;

        /// <summary>Times said in <paramref name="texts" />.</summary>
        public Ago(Texts texts)
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

            // One answer for the whole first minute, rather than a new one every second. Counting
            // seconds changes what every row says once a second for as long as a screen is open,
            // and wakes everything downstream of it to be told a number nobody is reading - a
            // construct with fifty sequences pays that fifty times a second, for ever.
            //
            // Nothing is lost with it. What second-level words were good for is saying that
            // something has just happened, and the light on the row says that faster and without
            // being read.
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
