using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>A sensor's two triggers: the game runs the first slot of every page when it starts detecting, and the second when it stops.</summary>
    public sealed class SensorTriggers : ITriggers
    {
        /// <inheritdoc />
        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMySensorBlock;
        }

        /// <inheritdoc />
        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithTrigger(new ToolbarSlot(1, 0, 1), "detects something")
                .WithTrigger(new ToolbarSlot(1, 0, 2), "stops detecting");
        }
    }
}
