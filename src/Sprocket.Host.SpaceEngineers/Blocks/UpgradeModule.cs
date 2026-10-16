using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An upgrade module, which boosts the blocks it is fitted to while it works: how many it boosts, and with how many kinds of boost.</summary>
    public sealed class UpgradeModule : Capability
    {
        const string Unheard = "A module raises nothing as blocks are built or taken away beside it.";

        static readonly Being Connections = Being.Whole<IMyUpgradeModule>(module => module.Connections);

        static readonly Being Upgrades = Being.Whole<IMyUpgradeModule>(module => module.UpgradeCount);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyUpgradeModule;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            // How many kinds of boost a module gives is fixed by its definition, so it never changes and there is nothing to announce.
            kind.WithValue("connections", "connections", Connections).Looked(Unheard).In(Category.State)
                .WithValue("upgrade-count", "upgrade count", Upgrades).Announced().In(Category.State);
        }
    }
}
