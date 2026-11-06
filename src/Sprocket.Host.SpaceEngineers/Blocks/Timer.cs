using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A timer block, with its one signal and read for whether it is counting down.</summary>
    /// <remarks>The game runs every slot of its toolbar as it goes off; it raises no event, so whether it is counting down is looked at.</remarks>
    public sealed class Timer : Capability
    {
        static readonly OwnValue Counting = OwnValue.Flag(Reading.Of<IMyTimerBlock>(timer => timer.IsCountingDown));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyTimerBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithSignal(new ToolbarSlot(1, 0, 0), "goes off")
                .WithValue("counting_down", "counting down", Counting)
                .Looked("A timer raises no event of its own.").In(Category.Automation)
                .Renaming("Start", false, "start")
                .Renaming("Stop", false, "stop")
                .Renaming("TriggerNow", false, "trigger now")
                .Renaming("TriggerDelay", true, "delay")
                .Measuring("TriggerDelay", " s")
                .Renaming("Silent", true, "silent")
                .Renaming("Silent", false, "toggle silent");
        }
    }
}
