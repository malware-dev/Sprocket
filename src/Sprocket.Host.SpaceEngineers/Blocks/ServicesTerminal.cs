using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A services terminal: repair and salvage offered to visitors, which anyone may be allowed to use.</summary>
    public sealed class ServicesTerminal : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            IMyRepairServiceComponent repair;
            return block is IMyFunctionalBlock && block.Components.TryGet(out repair);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Vetting(true, "AnyoneCanUse").Vetting(false, "AnyoneCanUse");
        }
    }
}
