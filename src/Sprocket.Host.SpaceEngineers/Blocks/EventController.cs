using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// An event controller's two signals, and the act that flips whether one watched block meets its condition or every
    /// one must.
    /// </summary>
    /// <remarks>The game runs the first slot of every page when its condition is met, and the second when it no longer is.</remarks>
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
            kind.WithSignal(new ToolbarSlot(1, 0, 1), "meets its condition")
                .WithSignal(new ToolbarSlot(1, 0, 2), "stops meeting its condition")
                .Renaming("ANDCheckbox", false, "toggle met by");
        }
    }
}
