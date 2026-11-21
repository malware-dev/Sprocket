using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;

// The mode lives in the Ingame namespace, which cannot be imported whole: IMyLandingGear is declared in both, and the
// mod-side one is the one with the event on it.
using LandingGearMode = SpaceEngineers.Game.ModAPI.Ingame.LandingGearMode;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>Landing gear and magnetic plates, read for whether they hold the ship.</summary>
    public sealed partial class LandingGear : Capability
    {
        static readonly OwnValue State = OwnValue.OneOf<IMyLandingGear>(ChoiceTypes.LockStatus, gear => Position(gear.LockMode));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyLandingGear;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("status", "status", State).Status().Announced().In(Category.Locking)
                .WithChange("becomes_ready_to_lock", "becomes ready to lock", "status", Literal.Name("ready-to-lock"))
                .Announced().Reading("is ready to lock", "is not ready to lock")
                .WithChange("locks", "locks", "status", Literal.Name("locked")).Announced()
                .Reading("is locked", "is not locked")
                .WithEvent("unlocks", State.Is(Literal.Name("locked")), SignalEdge.Falling, "unlocks").Announced()
                .Renaming("Lock", false, "lock")
                .Renaming("Unlock", false, "unlock")
                .Renaming("SwitchLock", false, "toggle lock")
                .Renaming("Autolock", true, "lock automatically")
                .Renaming("Autolock", false, "toggle lock automatically")
                .Renaming("EnableParking", true, "used for parking")
                .Renaming("EnableParking", false, "toggle used for parking");
        }

        /// <summary>Told as the gear locks, unlocks, or comes into or out of reach of something to lock to.</summary>
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
    }
}
