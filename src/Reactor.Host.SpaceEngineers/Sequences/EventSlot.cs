using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>What happens to the things the trigger watches.</summary>
    public sealed class EventSlot : SentenceSlot
    {
        public EventSlot(EntityEvent chosen, int command) : base(command)
        {
            Event = chosen;
        }

        /// <summary>The event itself, or null while nothing valid has been chosen.</summary>
        public EntityEvent Event { get; private set; }

        public override bool IsEmpty => Event == null;
    }
}
