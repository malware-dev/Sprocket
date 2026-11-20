using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A ship drill, which mines while it is switched on: whether it is drilling, and whether it mines or only clears terrain.</summary>
    public sealed class Drill : Capability
    {
        const string Unheard = "A drill raises nothing as a pilot takes it up or its mode changes.";

        static readonly OwnValue Drilling = OwnValue.Flag(Reading.Of<IMyShipDrill>(drill => drill.IsActivated));

        static readonly OwnValue Mode = OwnValue.OneOf<IMyShipDrill>(ChoiceTypes.DrillMode, drill => drill.TerrainClearingMode ? 1 : 0)
            .Writing<IMyShipDrill>((drill, mode) => drill.TerrainClearingMode = mode.Integer == 1);

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
