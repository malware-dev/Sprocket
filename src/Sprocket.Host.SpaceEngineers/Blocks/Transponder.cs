using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A transponder: sending a signal on its channel, the channel, and whom it hears from, said in Sprocket's words.</summary>
    /// <remarks>Its three buttons that pick whom it hears from are the value they set, and so is the setter written beside its channel.</remarks>
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
            kind.Renaming("SendSignal", false, "send a signal")
                .Renaming("Channel", true, "channel")
                .Renaming("ReceiveFrom", true, "hears from")
                .Hiding("SetChannelValue", "SetFilter_Owner", "SetFilter_Faction", "SetFilter_Everyone");
        }
    }
}
