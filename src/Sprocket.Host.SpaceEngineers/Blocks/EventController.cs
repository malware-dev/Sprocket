using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An event controller's two signals: the game runs the first slot of every page when its condition is met, and the second when it no longer is.</summary>
    public sealed class EventController : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyEventControllerBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithSignal(new ToolbarSlot(1, 0, 1), "has its condition met")
                .WithSignal(new ToolbarSlot(1, 0, 2), "no longer has its condition met");
        }
    }
}
