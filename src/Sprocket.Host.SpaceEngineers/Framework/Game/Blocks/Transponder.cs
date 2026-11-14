using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A transponder: sending a signal on its channel, the channel, and whom it receives from, said in Sprocket's words.</summary>
    public sealed class Transponder : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyTransponder;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("SendSignal", false, "send signal")
                .Renaming("Channel", true, "channel")
                .Renaming("ReceiveFrom", true, "receives from")
                .Hiding("SetChannelValue", "SetFilter_Owner", "SetFilter_Faction", "SetFilter_Everyone");
        }
    }
}
