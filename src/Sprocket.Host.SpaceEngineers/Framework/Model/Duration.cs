using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A length of time as the script writes it: in ticks, or in seconds of sixty ticks each.</summary>
    /// <remarks>The unit is kept as written: a wait written <c>2s</c> is written back <c>2s</c>.</remarks>
    public struct Duration : IEquatable<Duration>
    {
        /// <summary>How many ticks make a second, at the game's own rate.</summary>
        public const int PerSecond = 60;

        Duration(int ticks, double seconds, bool inSeconds)
        {
            Ticks = ticks;
            Seconds = seconds;
            InSeconds = inSeconds;
        }

        /// <summary>How long it lasts, in ticks.</summary>
        public int Ticks { get; }

        /// <summary>How long it lasts in seconds: as written where it is in seconds, and its ticks as seconds otherwise.</summary>
        public double Seconds { get; }

        /// <summary>Whether it is written in seconds rather than ticks.</summary>
        public bool InSeconds { get; }

        /// <summary>A length of <paramref name="ticks" /> ticks.</summary>
        public static Duration OfTicks(int ticks)
        {
            return new Duration(ticks, (double)ticks / PerSecond, false);
        }

        /// <summary>A length of <paramref name="seconds" /> seconds, lasting the nearest whole tick.</summary>
        public static Duration OfSeconds(double seconds)
        {
            return new Duration(TicksIn(seconds), seconds, true);
        }

        /// <summary>The nearest whole tick to <paramref name="seconds" />, a half rounded away from nought.</summary>
        public static int TicksIn(double seconds)
        {
            return (int)Math.Round(seconds * PerSecond, MidpointRounding.AwayFromZero);
        }

        /// <inheritdoc />
        public bool Equals(Duration other)
        {
            return Ticks == other.Ticks && InSeconds == other.InSeconds && Seconds.Equals(other.Seconds);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is Duration && Equals((Duration)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Ticks * 2 + (InSeconds ? 1 : 0);
        }

        /// <summary>Whether two lengths are written the same.</summary>
        public static bool operator ==(Duration left, Duration right)
        {
            return left.Equals(right);
        }

        /// <summary>Whether two lengths are written differently.</summary>
        public static bool operator !=(Duration left, Duration right)
        {
            return !left.Equals(right);
        }
    }
}
