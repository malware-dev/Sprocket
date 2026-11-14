using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
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
            kind.Renaming("EnableBroadCast", true, "broadcasting").Stating("EnableBroadCast")
                .Renaming("EnableBroadCast", false, "toggle broadcasting")
                .Renaming("Radius", true, "broadcast radius")
                .Measuring("Radius", " m")
                .Renaming("HudText", true, "HUD text")
                .Renaming("ShowShipName", true, "show ship name")
                .Renaming("ShowShipName", false, "toggle show ship name");
        }
    }
}
