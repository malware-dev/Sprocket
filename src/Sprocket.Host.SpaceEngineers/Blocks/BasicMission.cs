using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>The AI task block: which task a drone has, and how it does it.</summary>
    /// <remarks>Follow me and set player position act on the person who pressed them, and a sequence has none.</remarks>
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
            kind.Renaming("SelectMission", true, "task")
                .Renaming("ID_MODE_SELECTOR_COMBOBOX", true, "route mode")
                .Renaming("SetCurrentGridPositionHome", false, "make this home")
                .Renaming("FollowHomeMinRange", true, "min range from home")
                .Renaming("FollowHomeMaxRange", true, "max range from home")
                .Renaming("Wander", true, "wandering")
                .Renaming("Wander", false, "toggle wandering")
                .Renaming("StopFollowing", false, "stop following")
                .Renaming("FollowDistance", true, "follow distance")
                .Hiding("FollowMe", "SetCurrentPositionHome");
        }
    }
}
