using Sprocket.Host.SpaceEngineers.World;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>What the step does to the things it names.</summary>
    public sealed class ActionSlot : SentenceSlot
    {
        /// <summary>The part saying <paramref name="chosen" />, or a gap where nothing is chosen, opening the question it was answered with through <paramref name="opens" />.</summary>
        public ActionSlot(EntityAction chosen, ICommand opens) : base(opens)
        {
            Action = chosen;
        }

        /// <summary>The action itself, or null while nothing valid has been chosen.</summary>
        public EntityAction Action { get; private set; }

        /// <inheritdoc />
        public override bool IsEmpty => Action == null;
    }
}
