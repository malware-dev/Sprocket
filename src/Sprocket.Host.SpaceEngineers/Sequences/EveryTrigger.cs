

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// "Every ___" - a recurring trigger.
    /// </summary>
    /// <remarks>
    /// This deliberately replaces the loop. There is no counter, no termination
    /// condition and no iteration variable, so three of the concepts novices most
    /// reliably form broken mental models around never come up.
    /// </remarks>
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
