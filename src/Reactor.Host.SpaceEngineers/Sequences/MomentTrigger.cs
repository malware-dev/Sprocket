using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A trigger about a moment on some blocks: "when the inner door finishes closing".
    /// </summary>
    /// <remarks>
    /// The same sentence arrives two ways, and which one is the block's business rather than the
    /// author's. A door finishing closing is something Reactor has to watch for; a button being
    /// pressed is something the construct already announces through a toolbar slot, and watching for
    /// it would be Reactor inventing a signal the game does not have.
    /// <para>
    /// So the sources and the moment live here, shared, and the two below differ only in how the
    /// moment reaches Reactor - see <see cref="WhenTrigger" /> and <see cref="RequiredTrigger" />.
    /// Everything that resolves what a trigger names, or renames it, or asks what it reads as, works
    /// on this and never has to know which.
    /// </para>
    /// </remarks>
    public abstract class MomentTrigger : Trigger
    {
        readonly List<string> _sources = new List<string>();

        protected MomentTrigger(string eventId)
        {
            EventId = eventId;
        }

        /// <summary>
        /// The blocks this is about, as declarations.
        /// </summary>
        /// <remarks>
        /// Picked inline and belonging to this trigger alone - no named sets, no saved groups,
        /// nothing to maintain. Picking several is the ordinary case rather than an advanced one,
        /// which is what lets the whole concept of iterating over a collection stay out of the
        /// language.
        /// </remarks>
        public IList<string> Sources => _sources;

        /// <summary>The <see cref="World.EntityEvent" /> id this is about.</summary>
        public string EventId { get; set; }
    }
}
