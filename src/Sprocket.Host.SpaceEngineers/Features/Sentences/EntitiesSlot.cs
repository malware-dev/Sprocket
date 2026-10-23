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
        /// The part naming the slots <paramref name="slots" />, which the blocks <paramref name="entities" /> fill: by their names,
        /// each under its mark of <paramref name="marks" />, or by the blocks where
        /// <paramref name="marks" /> is null; all of them where <paramref name="requiresAll" /> and any one otherwise, opening the
        /// question it was answered with through <paramref name="opens" />.
        /// </summary>
        public EntitiesSlot(IList<long> entities, IList<string> slots, bool requiresAll, IList<string> marks, ICommand opens)
            : base(opens)
        {
            Entities = new List<long>(entities);
            Slots = new List<string>(slots);
            RequiresAll = requiresAll;
            Marks = marks == null ? null : new List<string>(marks);
        }

        /// <summary>The mark of each of the slots, in their order, where the part says them by name; null where it says the blocks.</summary>
        public List<string> Marks { get; private set; }

        /// <summary>Whether the part says the slots by their names, as a blueprint's lines do, rather than the blocks.</summary>
        public bool SaysSlots => Marks != null;

        /// <summary>The blocks the part names, by entity id.</summary>
        public List<long> Entities { get; private set; }

        /// <summary>The screen of each of <see cref="Entities" /> the slots hold, by name and in their order, where the slots hold screens; null where they hold blocks.</summary>
        public List<string> Screens { get; set; }

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
