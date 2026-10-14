using Sprocket.Host.SpaceEngineers.World;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>What happens to the things the trigger watches.</summary>
    public sealed class EventSlot : SentenceSlot
    {
        /// <summary>The event <paramref name="chosen" />, pressed to <paramref name="opens" />, said for several blocks together where <paramref name="plural" /> says so.</summary>
        public EventSlot(EntityEvent chosen, ICommand opens, bool plural = false) : base(opens)
        {
            Event = chosen;
            Plural = plural;
        }

        /// <summary>The event itself, or null while nothing valid has been chosen.</summary>
        public EntityEvent Event { get; private set; }

        /// <summary>Whether it is said of several blocks joined with "and", which takes the plural: "finish closing".</summary>
        public bool Plural { get; private set; }

        /// <inheritdoc />
        public override bool IsEmpty => Event == null;
    }
}
