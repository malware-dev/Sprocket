using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.Core.Ui
{
    /// <summary>
    /// The things the sentence is about.
    /// </summary>
    /// <remarks>
    /// Ids rather than names. A name is read off the target's own world, and what to say about one
    /// that is no longer there is the target's to phrase - so handing over names would be core
    /// answering both questions on everybody's behalf.
    /// </remarks>
    public sealed class EntitiesSlot : SentenceSlot
    {
        public EntitiesSlot(IList<long> entities, bool requiresAll, int command) : base(command)
        {
            Entities = new List<long>(entities);
            RequiresAll = requiresAll;
        }

        public List<long> Entities { get; private set; }

        /// <summary>
        /// Whether every one of them counts, or any one of them is enough.
        /// </summary>
        /// <remarks>
        /// Trigger sources fire on whichever happens first, so a target joining those with "and" would
        /// state the opposite of what the sequence does. In English it also settles the agreement -
        /// "the port door or the starboard door finishes" against "finish" - which is exactly the sort
        /// of thing that differs by language and so cannot be decided here.
        /// </remarks>
        public bool RequiresAll { get; private set; }

        public override bool IsEmpty => Entities.Count == 0;
    }
}
