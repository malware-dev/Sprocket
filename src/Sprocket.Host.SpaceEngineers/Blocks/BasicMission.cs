using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>The AI task block: which task a drone has, and how it does it.</summary>
    public sealed class BasicMission : Capability
    {
        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyBasicMissionBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming("SelectMission", true, "objective")
                .Renaming("ID_MODE_SELECTOR_COMBOBOX", true, "route mode")
                .Renaming("SetCurrentGridPositionHome", false, "set home position")
                .Renaming("FollowHomeMinRange", true, "minimum range from home")
                .Measuring("FollowHomeMinRange", " m")
                .Renaming("FollowHomeMaxRange", true, "maximum range from home")
                .Measuring("FollowHomeMaxRange", " m")
                .Renaming("Wander", true, "wandering").Stating("Wander")
                .Renaming("Wander", false, "toggle wandering")
                .Renaming("StopFollowing", false, "stop following")
                .Renaming("FollowDistance", true, "follow distance")
                .Measuring("FollowDistance", " m")
                .Hiding("FollowMe", "SetCurrentPositionHome");
        }
    }
}
