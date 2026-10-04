using Sprocket.Host.SpaceEngineers.World;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>What happens to the things the trigger watches.</summary>
    public sealed class EventSlot : SentenceSlot
    {
        public EventSlot(EntityEvent chosen, ICommand opens) : base(opens)
        {
            Event = chosen;
        }

        /// <summary>The event itself, or null while nothing valid has been chosen.</summary>
        public EntityEvent Event { get; private set; }

        /// <inheritdoc />
        public override bool IsEmpty => Event == null;
    }
}
