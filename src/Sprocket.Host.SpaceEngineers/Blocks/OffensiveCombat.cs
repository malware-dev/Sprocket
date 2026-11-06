using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>An offensive combat block: how it picks a target and how each of its attack patterns flies.</summary>
    public sealed class OffensiveCombat : Capability
    {
        const string Circle = "OffensiveCombatCircleOrbit_";

        const string Range = "OffensiveCombatStayAtRange_";

        static readonly string[] Facings = { "Front", "Back", "Left", "Right", "Up", "Down" };

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyOffensiveCombatBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("AttackPattern", true, "attack pattern")
                .Renaming("TargetPriority", true, "target priority")
                .Renaming("UpdateInterval", true, "search interval")
                .Measuring("UpdateInterval", " s")
                .Hiding("SetTargetPriority_Closest", "SetTargetPriority_Largest", "SetTargetPriority_Smallest")
                .Renaming($"{Circle}CircleDistance", true, "circle distance")
                .Measuring($"{Circle}CircleDistance", " m")
                .Renaming($"{Circle}CircleInPGravity", true, "circling in planet gravity")
                .Renaming($"{Circle}CircleInPGravity", false, "toggle circling in planet gravity")
                .Renaming($"{Circle}FacingMode", true, "circle facing mode")
                .Renaming($"{Range}MinimalDistance", true, "minimal distance")
                .Measuring($"{Range}MinimalDistance", " m")
                .Renaming($"{Range}MaximalDistance", true, "maximal distance")
                .Measuring($"{Range}MaximalDistance", " m")
                .Renaming($"{Range}EvasiveManeuvers", true, "evasive maneuvers")
                .Renaming($"{Range}EvasiveManeuvers", false, "toggle evasive maneuvers")
                .Renaming($"{Range}FacingMode", true, "stay at range facing mode")
                .Renaming("OffensiveCombatHitAndRun_BreakOffDistance", true, "break-off distance")
                .Measuring("OffensiveCombatHitAndRun_BreakOffDistance", " m")
                .Renaming("OffensiveCombatHitAndRun_RetreatDistance", true, "retreat distance")
                .Measuring("OffensiveCombatHitAndRun_RetreatDistance", " m")
                .Renaming("OffensiveCombatHitAndRun_RetreatTimeout", true, "retreat timeout")
                .Measuring("OffensiveCombatHitAndRun_RetreatTimeout", " s")
                .Renaming("OffensiveCombatHitAndRun_RetreatAngle", true, "retreat angle")
                .Measuring("OffensiveCombatHitAndRun_RetreatAngle", "°")
                .Renaming("OffensiveCombatIntercept_GuidanceType", true, "guidance type")
                .Renaming("OffensiveCombatIntercept_OverrideCollisionAvoidance", true, "override collision avoidance")
                .Renaming("OffensiveCombatIntercept_OverrideCollisionAvoidance", false, "toggle override collision avoidance");

            foreach (var direction in Facings)
            {
                var facing = direction.ToLowerInvariant();
                kind.Renaming($"{Circle}Facing{direction}", true, $"circle facing {facing}")
                    .Renaming($"{Circle}Facing{direction}", false, $"toggle circle facing {facing}")
                    .Renaming($"{Range}Facing{direction}", true, $"stay at range facing {facing}")
                    .Renaming($"{Range}Facing{direction}", false, $"toggle stay at range facing {facing}");
            }
        }
    }
}
