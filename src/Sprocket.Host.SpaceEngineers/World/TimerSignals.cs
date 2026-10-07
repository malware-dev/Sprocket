using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>A timer block's one signal: it goes off, and the game runs every slot of its toolbar.</summary>
    public sealed class TimerSignals : ISignals
    {
        /// <inheritdoc />
        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyTimerBlock;
        }

        /// <inheritdoc />
        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithSignal(new ToolbarSlot(1, 0, 0), "goes off");
        }
    }
}
