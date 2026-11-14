using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>The state an if's test reads its blocks for: "is closed", or "is not closed".</summary>
    public sealed class StateSlot : SentenceSlot
    {
        /// <summary>The state <paramref name="moment" /> reads, being in it where <paramref name="holds" /> says so and not otherwise, opening <paramref name="opens" /> when pressed.</summary>
        public StateSlot(EntityEvent moment, bool holds, ICommand opens) : base(opens)
        {
            Moment = moment;
            Holds = holds;
        }

        /// <summary>The moment whose state is read, or null while the blocks have none by that name.</summary>
        public EntityEvent Moment { get; private set; }

        /// <summary>Whether the test is for the blocks being in it rather than not.</summary>
        public bool Holds { get; private set; }

        /// <inheritdoc />
        public override bool IsEmpty => Moment == null;
    }
}
