using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

// The status lives in the Ingame namespace, which cannot be imported whole: IMyJumpDrive is declared in both.
using MyJumpDriveStatus = Sandbox.ModAPI.Ingame.MyJumpDriveStatus;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A jump drive: recharged and waited on until it can jump, its jump distance set, and read for its charge, its status and how far it reaches.</summary>
    /// <remarks>The game moves its charge on the server and lands it exactly on what a jump needs.</remarks>
    public sealed class JumpDrive : Capability
    {
        /// <summary>Recharging, which is Sprocket's own and may wait until the drive can jump.</summary>
        public const string Recharge = "recharge";

        const string Unjumped = "A jump drive raises nothing as a jump starts or ends.";

        const string Unweighed = "Nothing announces a change in the ship's mass, which the drive's reach follows.";


        static readonly string[] Statuses = { "charging", "ready", "jumping" };

        static readonly Reading Full = Reading.Of<IMyJumpDrive>(Charged);

        static readonly OwnAction Recharges = OwnAction.Of<IMyJumpDrive, RechargeOptions>(RechargeOptions.Read,
            (drive, recharging, seen) => drive.Recharge = true, Full,
            (drive, recharging, seen) => !recharging.Waits,
            (drive, recharging, seen) => recharging.Waits ? Why(drive) : BlockTrouble.None);

        static readonly OwnValue Status = OwnValue.OneOf<IMyJumpDrive>(Statuses, drive => Position(drive.Status));

        static readonly OwnValue Charge = OwnValue.Number<IMyJumpDrive>(drive =>
            drive.MaxStoredPower > 0f ? drive.CurrentStoredPower / drive.MaxStoredPower * 100.0 : 0.0);

        static readonly OwnValue Stored = OwnValue.Number<IMyJumpDrive>(drive => drive.CurrentStoredPower);

        static readonly OwnValue Capacity = OwnValue.Number<IMyJumpDrive>(drive => drive.MaxStoredPower);


        static readonly OwnValue Shortest = OwnValue.Number<IMyJumpDrive>(drive => drive.MinJumpDistanceMeters / 1000.0);

        static readonly OwnValue Longest = OwnValue.Number<IMyJumpDrive>(drive => drive.MaxJumpDistanceMeters / 1000.0);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyJumpDrive;
        }

        /// <inheritdoc />
        /// <remarks>Jump distance is in kilometers; only a distance below 0 is refused.</remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Recharge, "recharge", Recharges, (string)null).Lasting().Asking(RechargeOptions.Asked())
                .In(Category.Power)
                .Renaming("Recharge", true, "recharging").Stating("Recharge")
                .Renaming("Recharge", false, "toggle recharging")
                .WithValue("status", "status", Status).Status().Looked(Unjumped).In(Category.Flight)
                .WithValue("charge", "charge", Charge).Percent().Announced().In(Category.Power)
                .WithValue("stored-power", "stored power", Stored).Measured(" MWh").Announced().In(Category.Power)
                .WithValue("capacity", "capacity", Capacity).Measured(" MWh").Announced().In(Category.Power)
                .WithValue("minimum-jump-distance", "minimum jump distance", Shortest).Measured(" km").Announced().In(Category.Flight)
                .WithValue("maximum-jump-distance", "maximum jump distance", Longest).Measured(" km").Looked(Unweighed).In(Category.Flight)
                .Hiding("Jump", "SearchField", "SelectedTarget", "BeaconList", "JumpDistance", "SetGravityAcceleration",
                    "IncreaseJumpDistance", "DecreaseJumpDistance");
        }

        /// <summary>Told as the drive's charge moves.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var store = block as IMyStoredPowerRatio;
            return block is IMyJumpDrive && store != null ? new ChargeListener(store, told) : null;
        }

        /// <summary>Why this drive will not charge, or none while it might.</summary>
        static BlockTrouble Why(IMyJumpDrive drive)
        {
            if (Charged(drive) || drive.Recharge)
                return BlockTrouble.None;

            return BlockTrouble.Fatal(Cause.JumpDriveRechargeOff);
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
