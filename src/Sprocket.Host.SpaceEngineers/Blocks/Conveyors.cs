using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A block that pulls what it needs and pushes what it makes along the conveyors by itself, unless it is told to leave
    /// that to a player: assemblers, refineries, survival kits, collectors, drills, grinders, welders and stores.
    /// </summary>
    /// <remarks>Words for the game's own switch, which the terminal calls Use Conveyor System.</remarks>
    public sealed class Conveyors : Capability
    {
        /// <summary>The game's id for the switch, and for the act that flips it.</summary>
        public const string Switch = "UseConveyor";

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return Carries(block);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming(Switch, true, "automatic push and pull").Renaming(Switch, false, "toggle automatic push and pull");
        }

        /// <summary>Whether <paramref name="block" /> is one of the blocks these words are for.</summary>
        public static bool Carries(IMyTerminalBlock block)
        {
            return block is IMyProductionBlock || block is IMyCollector || block is IMyShipToolBase || block is IMyStoreBlock;
        }
    }
}
