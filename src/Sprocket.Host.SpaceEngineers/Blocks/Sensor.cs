using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A sensor's two signals: the game runs the first slot of every page when it starts detecting, and the second when it stops.</summary>
    public sealed class Sensor : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySensorBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithSignal(new ToolbarSlot(1, 0, 1), "detects something")
                .WithSignal(new ToolbarSlot(1, 0, 2), "stops detecting");
        }
    }
}
