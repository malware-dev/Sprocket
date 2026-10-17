using System.Collections.Generic;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>
    /// The things the sentence is about.
    /// </summary>
    public sealed class EntitiesSlot : SentenceSlot
    {
        /// <summary>The part naming the blocks <paramref name="entities" />, all of them where <paramref name="requiresAll" /> and any one otherwise, opening the question it was answered with through <paramref name="opens" />.</summary>
        public EntitiesSlot(IList<long> entities, bool requiresAll, ICommand opens) : base(opens)
        {
            Entities = new List<long>(entities);
            RequiresAll = requiresAll;
        }

        /// <summary>The blocks the part names, by entity id.</summary>
        public List<long> Entities { get; private set; }

        /// <summary>
        /// Whether every one of them counts, or any one of them is enough.
        /// </summary>
        public bool RequiresAll { get; private set; }

        /// <inheritdoc />
        public override bool IsEmpty => Entities.Count == 0;
    }
}
