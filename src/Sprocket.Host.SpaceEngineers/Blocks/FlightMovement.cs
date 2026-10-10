using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>The AI flight block, which flies the ship for the task and combat blocks: how it flies, and where it is heading.</summary>
    public sealed class FlightMovement : Capability
    {
        // Its waypoint-reached event comes only at the last point of a one-way route, and carries a type a mod may not name.
        const string Unheard = "The flight block raises no event a mod can hear as its current waypoint changes.";

        static readonly Being Waypoint = Being.Text<IMyFlightMovementBlock>(block =>
            block.CurrentWaypoint == null ? "" : block.CurrentWaypoint.Name);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyFlightMovementBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("current-waypoint", "current waypoint", Waypoint).Looked(Unheard);
            Flying(kind);
            kind.Renaming("MinHeightAboveTerrain", true, "minimum altitude")
                .Renaming("AlignToGravity", true, "aligning to gravity")
                .Renaming("AlignToGravity", false, "toggle aligning to gravity")
                .Renaming("MaxPitchAngle", true, "max pitch")
                .Renaming("MaxRollAngle", true, "max roll");
        }

        /// <summary>Gives the settings a flight block and a remote control share their words.</summary>
        public static void Flying(EntityKind kind)
        {
            kind.Renaming("SpeedLimit", true, "speed limit")
                .Renaming("CollisionAvoidance", true, "collision avoidance")
                .Renaming("CollisionAvoidance", false, "toggle collision avoidance")
                .Renaming("DockingMode", true, "precision mode")
                .Renaming("DockingMode", false, "toggle precision mode");
        }
    }
}
