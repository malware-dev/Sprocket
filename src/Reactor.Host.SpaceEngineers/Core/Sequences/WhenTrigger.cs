using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.Core.Sequences
{
    /// <summary>
    /// "When any of these entities ___".
    /// </summary>
    /// <remarks>
    /// Sources are picked inline and belong to this sequence alone - no named sets,
    /// no saved groups, nothing to maintain. Selecting several entities is the
    /// ordinary case rather than an advanced one, which is what lets the whole
    /// concept of iterating over a collection stay out of the language.
    ///
    /// Several sources means "any of them", which is how the sentence reads out loud.
    /// </remarks>
    public sealed class WhenTrigger : Trigger
    {
        readonly List<string> _sources = new List<string>();

        public WhenTrigger(string eventId)
        {
            EventId = eventId;
        }

        public IList<string> Sources => _sources;

        /// <summary>
        /// Whether every source has to be in the event's state, rather than any one
        /// of them reaching it.
        /// </summary>
        /// <remarks>
        /// "Any" is the default because it is what people mean most of the time - any
        /// of these doors opening should turn the lights on. "All" is a genuinely
        /// different sentence: "when tank A <em>and</em> tank B are empty" fires at
        /// the moment the last of them empties, not when the first one does.
        ///
        /// So this is an edge on the whole condition rather than on any one signal,
        /// which is why the runtime tracks it separately instead of just requiring
        /// several edges at once - they would almost never land on the same tick.
        /// </remarks>
        public bool RequiresAll { get; set; }

        /// <summary>The <see cref="World.EntityEvent"/> id being listened for.</summary>
        public string EventId { get; set; }
    }
}
