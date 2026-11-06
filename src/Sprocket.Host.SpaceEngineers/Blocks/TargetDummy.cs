using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A target dummy's two signals, and how it stands up again.</summary>
    /// <remarks>
    /// The game runs the first slot of every page of its toolbar on every hit to one of its parts, and the second when it is
    /// knocked down, both on the server.
    /// </remarks>
    public sealed class TargetDummy : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyTargetDummyBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithSignal(new ToolbarSlot(1, 0, 1), "gets hit")
                .WithSignal(new ToolbarSlot(1, 0, 2), "gets knocked down")
                .Renaming("Enable Restoration", true, "regeneration")
                .Renaming("Delay", true, "regeneration delay")
                .Measuring("Delay", " s");
        }
    }
}
