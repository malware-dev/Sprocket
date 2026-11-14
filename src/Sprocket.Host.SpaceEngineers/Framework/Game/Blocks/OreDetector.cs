using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>An ore detector: its reach, and whether it shares what it finds through the antennas, said in Sprocket's words.</summary>
    public sealed class OreDetector : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyOreDetector;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("BroadcastUsingAntennas", true, "broadcast using antennas")
                .Renaming("BroadcastUsingAntennas", false, "toggle broadcast using antennas")
                .Renaming("Range", true, "range")
                .Measuring("Range", " m");
        }
    }
}
