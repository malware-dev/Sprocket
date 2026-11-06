using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A turret of the game's own, interior, gatling, missile or artillery: what it has that a turret controller does not.</summary>
    public sealed class Turret : Capability
    {
        // Restores the target switches and idle movement from the block's definition, read from the game's code.
        static readonly OwnAction Resets = OwnAction.Of<IMyLargeTurretBase>(turret => turret.ResetTargetingToDefault());

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyLargeTurretBase;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction("reset_targeting", "reset targeting", Resets).In(Category.Weapons)
                .Renaming("ViewRotation", true, "view rotation");
        }
    }
}
