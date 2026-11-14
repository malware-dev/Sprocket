using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A block a player can respawn at: the medical room and the survival kit, each named on the respawn screen.</summary>
    public sealed class RespawnPoint : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyMedicalRoom
                || (block is IMyAssembler && block.BlockDefinition.TypeId == typeof(MyObjectBuilder_SurvivalKit));
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("SpawnName", true, "spawn name");
        }
    }
}
