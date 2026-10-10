using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;

// The mode lives in the Ingame namespace, which cannot be imported whole: IMyLandingGear is declared in both, and the
// mod-side one is the one with the event on it.
using LandingGearMode = SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>Landing gear and magnetic plates: whether they hold the ship, and the members of their terminal said in Sprocket's words.</summary>
    /// <remarks>Lock and unlock are the game's own and take effect at once, so neither is waited for.</remarks>
    public sealed class LandingGear : Capability
    {
        static readonly string[] Statuses = { "unlocked", "ready-to-lock", "locked" };

        static readonly Being State = Being.OneOf<IMyLandingGear>(Statuses, gear => Position(gear.LockMode));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyLandingGear;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("status", "status", State).Announced().In(Category.Locking)
                .WithChange("becomes-ready-to-lock", "becomes ready to lock", "status", Literal.Name("ready-to-lock"))
                .Announced().Reading("is ready to lock", "is not ready to lock")
                .WithChange("locks", "locks", "status", Literal.Name("locked")).Announced()
                .Reading("is locked", "is not locked")
                .Renaming("Lock", false, "lock")
                .Renaming("Unlock", false, "unlock")
                .Renaming("SwitchLock", false, "toggle lock")
                .Renaming("Autolock", true, "locking automatically")
                .Renaming("Autolock", false, "toggle locking automatically")
                .Renaming("EnableParking", true, "used for parking")
                .Renaming("EnableParking", false, "toggle used for parking");
        }

        /// <summary>Told as the gear locks, unlocks, or comes into or out of reach of something to lock to, which it raises itself.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var gear = block as IMyLandingGear;
            return gear == null ? null : new Listening(gear, told);
        }

        /// <summary>Where <paramref name="mode" /> stands among the gear's statuses.</summary>
        static int Position(LandingGearMode mode)
        {
            switch (mode)
            {
                case LandingGearMode.ReadyToLock:
                    return 1;
                case LandingGearMode.Locked:
                    return 2;
            }

            return 0;
        }

        /// <summary>A landing gear being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyLandingGear _gear;
            readonly Action _told;

            public Listening(IMyLandingGear gear, Action told)
            {
                _gear = gear;
                _told = told;
                _gear.LockModeChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _gear.LockModeChanged -= Changed;
            }

            void Changed(IMyLandingGear gear, LandingGearMode was)
            {
                _told();
            }
        }
    }
}
