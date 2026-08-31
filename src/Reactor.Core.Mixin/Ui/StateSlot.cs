using Reactor.World;

namespace Reactor.Ui
{
    /// <summary>The situation a gate waits on.</summary>
    public sealed class StateSlot : SentenceSlot
    {
        public StateSlot(EntityState chosen, int command) : base(command)
        {
            State = chosen;
        }

        /// <summary>The state itself, or null while nothing valid has been chosen.</summary>
        public EntityState State { get; private set; }

        public override bool IsEmpty => State == null;
    }
}
