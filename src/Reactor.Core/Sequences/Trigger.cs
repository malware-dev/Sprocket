using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// What starts a sequence. Always a moment, never a situation.
    /// </summary>
    public abstract class Trigger
    {
    }

    /// <summary>
    /// "When any of these devices ___".
    /// </summary>
    /// <remarks>
    /// Sources are picked inline and belong to this sequence alone - no named sets,
    /// no saved groups, nothing to maintain. Selecting several devices is the
    /// ordinary case rather than an advanced one, which is what lets the whole
    /// concept of iterating over a collection stay out of the language.
    ///
    /// Several sources means "any of them", which is how the sentence reads out loud.
    /// </remarks>
    public sealed class WhenTrigger : Trigger
    {
        readonly List<long> _sources = new List<long>();

        public WhenTrigger(string eventId)
        {
            EventId = eventId;
        }

        public IList<long> Sources { get { return _sources; } }

        /// <summary>The <see cref="World.DeviceEvent"/> id being listened for.</summary>
        public string EventId { get; set; }
    }

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
        public EveryTrigger(int intervalBeats)
        {
            IntervalBeats = intervalBeats;
        }

        public int IntervalBeats { get; set; }
    }

    /// <summary>
    /// Started by name from elsewhere, rather than by anything happening.
    /// </summary>
    /// <remarks>
    /// What lets two buttons run the same sequence, and what lets a sequence be a
    /// thing the ship does rather than a thing a particular block causes.
    /// </remarks>
    public sealed class CalledTrigger : Trigger
    {
    }
}
