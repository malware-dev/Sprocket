using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A grinder or a welder: whether a pilot is working it by hand from a seat.</summary>
    public sealed class ShipTool : Capability
    {
        const string Unheard = "Nothing says when a pilot takes up a tool's trigger.";

        static readonly OwnValue Piloted = OwnValue.Flag(Reading.Of<IMyShipToolBase>(tool => tool.IsActivated));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyShipGrinder || block is IMyShipWelder;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("run-by-a-pilot", "run by a pilot", Piloted).Looked(Unheard).In(Category.Production);
        }
    }
}
