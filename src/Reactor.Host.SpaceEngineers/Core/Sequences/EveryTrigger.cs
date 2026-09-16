
namespace Reactor.Host.SpaceEngineers.Core.Sequences
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
        public EveryTrigger(int intervalTicks)
        {
            IntervalTicks = intervalTicks;
        }

        public int IntervalTicks { get; set; }
    }
}
