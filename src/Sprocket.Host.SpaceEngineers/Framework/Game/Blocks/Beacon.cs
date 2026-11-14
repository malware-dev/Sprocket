using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
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
            kind.Renaming("HudText", true, "HUD text")
                .Renaming("Radius", true, "broadcast radius")
                .Measuring("Radius", " m");
        }
    }
}
