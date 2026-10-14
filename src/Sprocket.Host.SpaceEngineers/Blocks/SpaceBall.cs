using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A space ball, a mass whose weight can be set while it runs: its mass and whether it broadcasts, in Sprocket's words.</summary>
    /// <remarks>
    /// Its friction and restitution are left out: the game applies them only under a physics setting it leaves off.
    /// </remarks>
    public sealed class SpaceBall : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMySpaceBall;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("VirtualMass", true, "virtual mass")
                .Renaming("EnableBroadCast", true, "broadcasting").Stating("EnableBroadCast")
                .Renaming("EnableBroadCast", false, "toggle broadcasting")
                .Hiding("Friction", "Restitution", "IncreaseFriction", "DecreaseFriction", "IncreaseRestitution",
                    "DecreaseRestitution");
        }
    }
}
