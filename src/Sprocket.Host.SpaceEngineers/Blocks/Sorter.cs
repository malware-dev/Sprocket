using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A conveyor sorter: whether it lets its list through or holds it back, and whether it pulls items.</summary>
    /// <remarks>Setting the list itself is still to come.</remarks>
    public sealed class Sorter : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyConveyorSorter;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Planning(PlannedMember.SorterList)
                .Renaming("blacklistWhitelist", true, "filter mode")
                .Renaming("DrainAll", true, "draining all").Stating("DrainAll")
                .Renaming("DrainAll", false, "toggle draining all");
        }
    }
}
