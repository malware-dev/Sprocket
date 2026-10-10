using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A custom turret controller: who aims it, the sun, how fast it turns, and its two signals.</summary>
    /// <remarks>The game runs the first slot of every page of its toolbar when it comes to bear on its target, and the second when it no longer does.</remarks>
    public sealed class TurretController : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyTurretControlBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithSignal(new ToolbarSlot(1, 0, 1), "gets on target")
                .WithSignal(new ToolbarSlot(1, 0, 2), "loses its aim")
                .Renaming("AI", true, "aiming automatically")
                .Renaming("AI", false, "toggle aiming automatically")
                .Renaming("SunTracking", true, "tracking the sun")
                .Renaming("SunTracking", false, "toggle tracking the sun")
                .Renaming("MultiplierAz", true, "azimuth speed")
                .Renaming("MultiplierEl", true, "elevation speed")
                .Renaming("AngleDeviation", true, "angle deviation");
        }
    }
}
