using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>A timer block's one trigger: it goes off, and the game runs every slot of its toolbar.</summary>
    sealed class TimerTriggers : ITriggers
    {
        /// <inheritdoc />
        public bool Claims(IMyTerminalBlock block)
        {
            return block is IMyTimerBlock;
        }

        /// <inheritdoc />
        public void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithTrigger(new ToolbarSlot(1, 0, 0), "goes off");
        }
    }
}
