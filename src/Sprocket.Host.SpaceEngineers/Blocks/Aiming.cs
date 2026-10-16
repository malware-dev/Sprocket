using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// What aims itself at a target, a turret or a turret controller: whether it has a target and is on it, who it shoots at,
    /// and whether a person is at its controls.
    /// </summary>
    /// <remarks>The turret and the controller say the same things through two interfaces of the game's that share nothing.</remarks>
    public sealed class Aiming : Capability
    {
        const string Unaimed = "The game raises no event as a turret finds a target, loses one or comes to bear on it.";

        static readonly Being TurretTargeting = Being.Flag(Reading.Of<IMyLargeTurretBase>(turret => turret.HasTarget));

        static readonly Being TurretOnTarget = Being.Flag(Reading.Of<IMyLargeTurretBase>(turret => turret.IsAimed));

        static readonly Being TurretControlled = Being.Flag(Reading.Of<IMyLargeTurretBase>(turret => turret.IsUnderControl));

        static readonly Being ControllerTargeting = Being.Flag(Reading.Of<IMyTurretControlBlock>(controller => controller.HasTarget));

        static readonly Being ControllerOnTarget = Being.Flag(Reading.Of<IMyTurretControlBlock>(controller => controller.IsAimed));

        static readonly Being ControllerControlled =
            Being.Flag(Reading.Of<IMyTurretControlBlock>(controller => controller.IsUnderControl));

        static readonly string[] Switches =
        {
            "Neutrals", "Enemies", "Friends", "Characters", "SmallShips", "LargeShips", "Stations", "Meteors", "Missiles"
        };

        static readonly string[] Targeted =
        {
            "neutrals", "enemies", "friends", "characters", "small ships", "large ships", "stations", "meteors", "missiles"
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
                .WithValue("on-target", "on target", turret ? TurretOnTarget : ControllerOnTarget).Looked(Unaimed)
                .WithValue("controlled-by-a-person", "controlled by a person", turret ? TurretControlled : ControllerControlled)
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

        /// <summary>Told as a person takes or gives up its controls, which the game says of anything a person can control.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var controlled = block as IMyControllableEntity;
            if (controlled == null || controlled.ControllerInfo == null)
                return null;

            return new Listening(controlled.ControllerInfo, told);
        }

        /// <summary>A block's controls being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyControllerInfo _controls;
            readonly Action _told;

            public Listening(IMyControllerInfo controls, Action told)
            {
                _controls = controls;
                _told = told;
                _controls.ControlAcquired += Changed;
                _controls.ControlReleased += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _controls.ControlAcquired -= Changed;
                _controls.ControlReleased -= Changed;
            }

            void Changed(IMyEntityController controller)
            {
                _told();
            }
        }
    }
}
