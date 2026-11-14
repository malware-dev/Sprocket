using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A separation charge: a block that detaches itself from its grid, splitting the ship where it stands.</summary>
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
