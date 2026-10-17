using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A block a player can respawn at: the medical room and the survival kit, each named on the respawn screen.</summary>
    /// <remarks>
    /// A survival kit is an assembler to the game, told apart by its type. The refill stations are medical rooms that respawn
    /// nobody, and have no name to give.
    /// </remarks>
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
            kind.Renaming("SpawnName", true, "respawn name");
        }
    }
}
