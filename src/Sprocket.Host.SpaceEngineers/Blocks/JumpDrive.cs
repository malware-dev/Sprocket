using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

// The status lives in the Ingame namespace, which cannot be imported whole: IMyJumpDrive is declared in both.
using MyJumpDriveStatus = Sandbox.ModAPI.Ingame.MyJumpDriveStatus;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A jump drive: recharged and waited on until it can jump, its jump distance set, and read for its charge, its status and how far it reaches.</summary>
    /// <remarks>
    /// The game moves its charge on the server as it fills and says so, and lands it exactly on what a jump needs, so
    /// charged is a comparison and needs no band.
    /// <para>
    /// Nothing to call off: a drive left recharging holds nobody in danger, and recharging is a value of its own.
    /// </para>
    /// </remarks>
    public sealed class JumpDrive : Capability
    {
        /// <summary>Recharging, which is Sprocket's own and may wait until the drive can jump.</summary>
        public const string Recharge = "recharge";

        const string Unjumped = "A jump drive raises nothing as a jump starts or ends.";

        const string Unweighed = "Nothing announces a change in the ship's mass, which the drive's reach follows.";


        static readonly string[] Statuses = { "charging", "ready", "jumping" };

        static readonly Reading Full = Reading.Of<IMyJumpDrive>(Charged);

        static readonly Doing Recharges = Doing.Of<IMyJumpDrive, Recharging>(Recharging.Read,
            (drive, recharging, seen) => drive.Recharge = true, Full,
            (drive, recharging, seen) => !recharging.Waits,
            (drive, recharging, seen) => recharging.Waits ? Why(drive) : Trouble.None);

        static readonly Being Status = Being.OneOf<IMyJumpDrive>(Statuses, drive => Position(drive.Status));

        static readonly Being Charge = Being.Number<IMyJumpDrive>(drive =>
            drive.MaxStoredPower > 0f ? drive.CurrentStoredPower / drive.MaxStoredPower * 100.0 : 0.0);

        static readonly Being Stored = Being.Number<IMyJumpDrive>(drive => drive.CurrentStoredPower);

        static readonly Being Capacity = Being.Number<IMyJumpDrive>(drive => drive.MaxStoredPower);


        static readonly Being Shortest = Being.Number<IMyJumpDrive>(drive => drive.MinJumpDistanceMeters / 1000.0);

        static readonly Being Longest = Being.Number<IMyJumpDrive>(drive => drive.MaxJumpDistanceMeters / 1000.0);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyJumpDrive;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Recharge names no game action: an entry of Sprocket's that names one half of a switch's pair hides the switch
        /// itself, and recharging is listed. Capacity and the shortest jump are fixed by the block, so nothing need announce
        /// them. The game's own jump asks a pilot to confirm it, which nobody does for a sequence, so it is hidden.
        /// <para>
        /// Jump distance is in kilometers, held by the drive between the shortest and the longest jump the ship can make,
        /// which move with its mass and with how many drives it has, so only a distance below nothing is refused. The
        /// game's own distance as a share of that reach, its setter, and the steps made out of it are hidden behind it.
        /// </para>
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Recharge, "recharge", Recharges, (string)null).Lasting().Asking(Recharging.Asked())
                .In(Category.Power)
                .Renaming("Recharge", true, "recharging").Stating("Recharge")
                .Renaming("Recharge", false, "toggle recharging")
                .WithValue("status", "status", Status).Status().Looked(Unjumped).In(Category.Flight)
                .WithValue("charge", "charge", Charge).Announced().In(Category.Power)
                .WithValue("stored-power", "stored power", Stored).Announced().In(Category.Power)
                .WithValue("capacity", "capacity", Capacity).Announced().In(Category.Power)
                .WithValue("minimum-jump-distance", "minimum jump distance", Shortest).Announced().In(Category.Flight)
                .WithValue("maximum-jump-distance", "maximum jump distance", Longest).Looked(Unweighed).In(Category.Flight)
                .Hiding("Jump", "SearchField", "SelectedTarget", "BeaconList", "JumpDistance", "SetGravityAcceleration",
                    "IncreaseJumpDistance", "DecreaseJumpDistance");
        }

        /// <summary>Told as the drive's charge moves, which it announces.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var store = block as IMyStoredPowerRatio;
            return block is IMyJumpDrive && store != null ? new Storing(store, told) : null;
        }

        /// <summary>Why this drive will not charge, or none while it might.</summary>
        /// <remarks>
        /// Switched off, unpowered or damaged is said by the step being stalled. Recharging switched off is its own answer:
        /// whatever switched it has overruled the step, and nothing is heading towards charged any more.
        /// </remarks>
        static Trouble Why(IMyJumpDrive drive)
        {
            if (Charged(drive) || drive.Recharge)
                return Trouble.None;

            return Trouble.Fatal(Cause.JumpDriveRechargeOff);
        }

        static bool Charged(IMyJumpDrive drive)
        {
            return drive.MaxStoredPower > 0f && drive.CurrentStoredPower >= drive.MaxStoredPower;
        }

        /// <summary>Where <paramref name="status" /> stands among the drive's statuses.</summary>
        static int Position(MyJumpDriveStatus status)
        {
            switch (status)
            {
                case MyJumpDriveStatus.Ready:
                    return 1;
                case MyJumpDriveStatus.Jumping:
                    return 2;
            }

            return 0;
        }
    }
}
