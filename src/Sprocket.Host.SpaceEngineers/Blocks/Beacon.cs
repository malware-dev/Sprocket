using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A beacon: what its mark on the HUD says and how far it reaches, said in Sprocket's words.</summary>
    public sealed class Beacon : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyBeacon;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("HudText", true, "name on HUD")
                .Renaming("Radius", true, "radius");
        }
    }
}
