using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Ui.Input;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>What the step does to the things it names.</summary>
    public sealed class ActionSlot : SentenceSlot
    {
        public ActionSlot(EntityAction chosen, ICommand opens) : base(opens)
        {
            Action = chosen;
        }

        /// <summary>The action itself, or null while nothing valid has been chosen.</summary>
        public EntityAction Action { get; private set; }

        public override bool IsEmpty => Action == null;
    }
}
