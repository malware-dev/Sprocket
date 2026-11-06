using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An upgrade module, which boosts the blocks it is fitted to while it works: how many it boosts, and with how many kinds of boost.</summary>
    public sealed class UpgradeModule : Capability
    {
        const string Unheard = "A module raises nothing as blocks are built or taken away beside it.";

        static readonly OwnValue Connections = OwnValue.Whole<IMyUpgradeModule>(module => module.Connections);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyUpgradeModule;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("connections", "connections", Connections).Looked(Unheard).In(Category.State);
        }
    }
}
