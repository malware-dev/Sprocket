using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An antenna: whether it broadcasts, how far, and what its mark on the HUD says, said in Sprocket's words.</summary>
    public sealed class RadioAntenna : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyRadioAntenna;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("EnableBroadCast", true, "broadcasting")
                .Renaming("EnableBroadCast", false, "toggle broadcasting")
                .Renaming("Radius", true, "radius")
                .Renaming("HudText", true, "name on HUD")
                .Renaming("ShowShipName", true, "showing ship name")
                .Renaming("ShowShipName", false, "toggle showing ship name");
        }
    }
}
