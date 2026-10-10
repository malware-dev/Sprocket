using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An ore detector: its reach, and whether it shares what it finds through the antennas, said in Sprocket's words.</summary>
    /// <remarks>The ore it finds stays on players' HUDs; the game gives a mod no reading of it.</remarks>
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
            kind.Renaming("BroadcastUsingAntennas", true, "sharing through antennas")
                .Renaming("BroadcastUsingAntennas", false, "toggle sharing through antennas")
                .Renaming("Range", true, "range");
        }
    }
}
