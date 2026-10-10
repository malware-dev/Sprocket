using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A separation charge: a block that detaches itself from its grid, splitting the ship where it stands.</summary>
    /// <remarks>
    /// The game gives it no interface of its own: to a mod it is a functional block, and its detach comes from the destroy
    /// component its definition gives it, which the game finds by that component's mod-side interface.
    /// </remarks>
    public sealed class SeparationCharge : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            IMyDestroyBlockComponent charge;
            return block is IMyFunctionalBlock && block.Components.TryGet(out charge);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Vetting(false, "Detach");
        }
    }
}
