using System.Collections.Generic;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>
    /// The things the sentence is about.
    /// </summary>
    public sealed class EntitiesSlot : SentenceSlot
    {
        /// <summary>
        /// The part naming the blocks <paramref name="entities" /> that fill the slots <paramref name="slots" />, all of them where
        /// <paramref name="requiresAll" /> and any one otherwise, opening the question it was answered with through
        /// <paramref name="opens" />.
        /// </summary>
        public EntitiesSlot(IList<long> entities, IList<string> slots, bool requiresAll, ICommand opens) : base(opens)
        {
            Entities = new List<long>(entities);
            Slots = new List<string>(slots);
            RequiresAll = requiresAll;
        }

        /// <summary>The blocks the part names, by entity id.</summary>
        public List<long> Entities { get; private set; }

        /// <summary>The slots of the logic system the blocks fill, by name.</summary>
        public List<string> Slots { get; private set; }

        /// <summary>
        /// Whether every one of them counts, or any one of them is enough.
        /// </summary>
        public bool RequiresAll { get; private set; }

        /// <inheritdoc />
        public override bool IsEmpty => Entities.Count == 0;
    }
}
