using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A separation charge: a block that detaches itself from its grid, splitting the ship where it stands.</summary>
    /// <remarks>
    /// The game's own Detach action comes from the charge's component, and the Mod API lists and finds only the actions of a
    /// block's type, never those of its components: a server never has it. Detaching is Sprocket's own, through the component.
    /// </remarks>
    public sealed class SeparationCharge : Capability
    {
        /// <summary>Detaching, which is Sprocket's own.</summary>
        public const string Detach = "detach";

        static readonly OwnAction Detaches = OwnAction.Of<IMyFunctionalBlock>(Detached);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            IMyDestroyBlockComponent charge;
            return block is IMyFunctionalBlock && block.Components.TryGet(out charge);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Detach, "detach", Detaches, "Detach").In(Category.Locking);
        }

        static void Detached(IMyFunctionalBlock block)
        {
            IMyDestroyBlockComponent charge;
            if (block.Components.TryGet(out charge))
                charge.Detach();
        }
    }
}
