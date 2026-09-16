using Reactor.Host.SpaceEngineers.Core.World;

namespace Reactor.Host.SpaceEngineers.Core.Ui
{
    /// <summary>What the step does to the things it names.</summary>
    public sealed class ActionSlot : SentenceSlot
    {
        public ActionSlot(EntityAction chosen, int command) : base(command)
        {
            Action = chosen;
        }

        /// <summary>The action itself, or null while nothing valid has been chosen.</summary>
        public EntityAction Action { get; private set; }

        public override bool IsEmpty => Action == null;
    }
}
