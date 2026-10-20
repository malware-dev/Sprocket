using System;
using System.Diagnostics;
using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>How long Sprocket's own work takes each tick on the machine that runs the world, said in the game's log every ten seconds.</summary>
    /// <remarks>
    /// Kept by the session only while the world's debug log is on, so a server owner can see what Sprocket costs on the
    /// server itself, with the game's own calls in it as the harness cannot have them.
    /// </remarks>
    public sealed class TickTiming
    {
        /// <summary>How many ticks one line covers: ten seconds.</summary>
        public const int Span = 600;

        readonly Stopwatch _clock = new Stopwatch();
        readonly Action<string> _write;
        long _total;
        long _worst;
        int _ticks;

        /// <summary>Timing that hands each line to <paramref name="write" />.</summary>
        public TickTiming(Action<string> write)
        {
            _write = write;
        }

        /// <summary>Starts timing a tick.</summary>
        public void Begin()
        {
            _clock.Restart();
        }

        /// <summary>Ends timing a tick on a world of <paramref name="constructs" /> constructs with automation, and says what the last <see cref="Span" /> cost once they are counted.</summary>
        public void End(int constructs)
        {
            _clock.Stop();
            var spent = _clock.ElapsedTicks;
            _total += spent;
            if (spent > _worst)
                _worst = spent;

            if (++_ticks < Span)
                return;

            _write(string.Format(CultureInfo.InvariantCulture, "last {0} ticks: {1:0.0} µs a tick on average, worst {2:0.0} µs, {3} constructs with automation",
                _ticks, Micros(_total) / _ticks, Micros(_worst), constructs));
            _total = 0;
            _worst = 0;
            _ticks = 0;
        }

        static double Micros(long stopwatchTicks)
        {
            return stopwatchTicks * 1000000.0 / Stopwatch.Frequency;
        }
    }
}
