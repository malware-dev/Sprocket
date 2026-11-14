using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A turret or a turret controller, read for its target, who it shoots at and whether a person is at its controls.</summary>
    public sealed partial class TargetingBlock : Capability
    {
        const string Unaimed = "The game raises no event as a turret finds a target, loses one or comes to bear on it.";

        static readonly OwnValue TurretTargeting = OwnValue.Flag(Reading.Of<IMyLargeTurretBase>(turret => turret.HasTarget));

        static readonly OwnValue TurretOnTarget = OwnValue.Flag(Reading.Of<IMyLargeTurretBase>(turret => turret.IsAimed));

        static readonly OwnValue TurretControlled = OwnValue.Flag(Reading.Of<IMyLargeTurretBase>(turret => turret.IsUnderControl));

        static readonly OwnValue ControllerTargeting = OwnValue.Flag(Reading.Of<IMyTurretControlBlock>(controller => controller.HasTarget));

        static readonly OwnValue ControllerOnTarget = OwnValue.Flag(Reading.Of<IMyTurretControlBlock>(controller => controller.IsAimed));

        static readonly OwnValue ControllerControlled =
            OwnValue.Flag(Reading.Of<IMyTurretControlBlock>(controller => controller.IsUnderControl));

        static readonly string[] Switches =
        {
            "Neutrals", "Enemies", "Friends", "Characters", "SmallShips", "LargeShips", "Stations", "Meteors", "Missiles"
        };

        static readonly string[] Targeted =
        {
            "neutrals", "enemies", "friends", "characters", "small ships", "large ships", "stations", "meteors", "rockets"
        };

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyLargeTurretBase || block is IMyTurretControlBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            var turret = block is IMyLargeTurretBase;
            kind.WithValue("targeting", "targeting", turret ? TurretTargeting : ControllerTargeting).Status().Looked(Unaimed)
                .WithValue("on_target", "on target", turret ? TurretOnTarget : ControllerOnTarget).Looked(Unaimed)
                .WithValue("manually_controlled", "controlled by a person", turret ? TurretControlled : ControllerControlled)
                .Announced().In(Category.Usage);

            kind.Renaming("Range", true, "AI aiming radius")
                .Measuring("Range", " m")
                .Renaming("EnableIdleMovement", true, "idle movement")
                .Renaming("EnableIdleMovement", false, "toggle idle movement")
                .Renaming("EnableTargetLocking", true, "target locking")
                .Renaming("EnableTargetLocking", false, "toggle target locking")
                .Hiding("FocusLockedTarget", "TargetingGroup_CycleSubsystems");

            // A weapon says this for every gun it is; a turret controller is not one.
            if (!turret)
                kind.Renaming("TargetingGroup_Selector", true, "aims at");

            for (var i = 0; i < Switches.Length; i++)
            {
                kind.Renaming($"Target{Switches[i]}", true, $"target {Targeted[i]}")
                    .Renaming($"Target{Switches[i]}", false, $"toggle target {Targeted[i]}");
            }
        }

        /// <summary>Told as a person takes or gives up its controls.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var controlled = block as IMyControllableEntity;
            if (controlled == null || controlled.ControllerInfo == null)
                return null;

            return new Listening(controlled.ControllerInfo, told);
        }
    }
}
