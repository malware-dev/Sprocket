using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A timer block: its one signal, whether it is counting down, and the members of its terminal said in Sprocket's words.
    /// </summary>
    /// <remarks>
    /// It goes off and the game runs every slot of its toolbar. It raises no event of its own, so whether it is counting
    /// down is looked for.
    /// </remarks>
    public sealed class Timer : Capability
    {
        static readonly Being Counting = Being.Flag(Reading.Of<IMyTimerBlock>(timer => timer.IsCountingDown));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyTimerBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithSignal(new ToolbarSlot(1, 0, 0), "goes off")
                .WithValue("counting-down", "counting down", Counting)
                .Looked("A timer raises no event of its own.")
                .Renaming("Start", false, "start")
                .Renaming("Stop", false, "stop")
                .Renaming("TriggerNow", false, "trigger now")
                .Renaming("TriggerDelay", true, "delay")
                .Renaming("Silent", true, "silent").Stating("Silent")
                .Renaming("Silent", false, "toggle silent");
        }
    }
}
