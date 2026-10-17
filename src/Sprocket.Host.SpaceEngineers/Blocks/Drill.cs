using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A ship drill, which mines while it is switched on: whether it is drilling, and whether it mines or only clears terrain.</summary>
    public sealed class Drill : Capability
    {
        const string Unheard = "A drill raises nothing as a pilot takes it up or its mode changes.";

        static readonly string[] Modes = { "mining", "clearing-terrain" };

        static readonly OwnValue Drilling = OwnValue.Flag(Reading.Of<IMyShipDrill>(drill => drill.IsActivated));

        static readonly OwnValue Mode = OwnValue.OneOf<IMyShipDrill>(Modes, drill => drill.TerrainClearingMode ? 1 : 0)
            .Writing<IMyShipDrill>((drill, mode) => drill.TerrainClearingMode = mode.Whole == 1);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyShipDrill;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("drilling", "drilling", Drilling).Looked(Unheard).In(Category.Production)
                .WithValue("mode", "mode", Mode).Looked(Unheard).In(Category.Production)
                .Renaming("TerrainClearingMode", false, "toggle mode");
        }
    }
}
