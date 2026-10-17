

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// "Every ___" - a recurring trigger.
    /// </summary>
    public sealed class EveryTrigger : Trigger
    {
        /// <summary>A trigger every <paramref name="intervalTicks" /> ticks.</summary>
        public EveryTrigger(int intervalTicks) : this(Duration.OfTicks(intervalTicks))
        {
        }

        /// <summary>A trigger every <paramref name="interval" />.</summary>
        public EveryTrigger(Duration interval)
        {
            Interval = interval;
        }

        /// <summary>How long between one start and the next, as the script writes it.</summary>
        public Duration Interval { get; set; }

        /// <summary>How long between one start and the next, in ticks.</summary>
        public int IntervalTicks => Interval.Ticks;
    }
}
