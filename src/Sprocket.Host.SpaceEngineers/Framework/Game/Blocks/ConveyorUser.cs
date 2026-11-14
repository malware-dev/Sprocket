using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A block with the game's Use Conveyor System switch: assemblers, refineries, survival kits, collectors, drills, grinders, welders and stores.</summary>
    public sealed class ConveyorUser : Capability
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
