using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A grinder or a welder: whether it is at work.</summary>
    public sealed class ShipTool : Capability
    {
        const string Unheard = "Nothing says when a tool starts or stops working.";

        static readonly OwnValue AtWork = OwnValue.Flag(Reading.Of<IMyShipToolBase>(tool => tool.IsActivated));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyShipGrinder || block is IMyShipWelder;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("activated", "at work", AtWork).Looked(Unheard).In(Category.Production);
        }
    }
}
