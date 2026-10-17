using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

// The charge mode lives in the Ingame namespace, which cannot be imported whole: IMyBatteryBlock is
// declared in both.
using ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A battery, recharged and waited on until it is full, and read for how much it holds and how hard it is worked.</summary>
    /// <remarks>The game moves its charge every hundred frames and lands it exactly on its capacity when full.</remarks>
    public sealed partial class Battery : Capability
    {
        /// <summary>Recharging, which is Sprocket's own and may wait until the battery is full.</summary>
        public const string Recharge = "recharge";

        const string Unheard = "A battery raises no event as its input or its mode changes.";

        static readonly Reading Full = Reading.Of<IMyBatteryBlock>(Charged);

        static readonly OwnAction Recharges = OwnAction.Of<IMyBatteryBlock, RechargeOptions>(RechargeOptions.Read,
            (battery, recharging, seen) => battery.ChargeMode = ChargeMode.Recharge, Full,
            (battery, recharging, seen) => !recharging.Waits,
            (battery, recharging, seen) => recharging.Waits ? Why(battery) : BlockTrouble.None);

        static readonly OwnValue Charge = OwnValue.Number<IMyBatteryBlock>(battery =>
            battery.MaxStoredPower > 0f ? battery.CurrentStoredPower / battery.MaxStoredPower * 100.0 : 0.0);

        static readonly OwnValue Stored = OwnValue.Number<IMyBatteryBlock>(battery => battery.CurrentStoredPower);

        static readonly OwnValue Capacity = OwnValue.Number<IMyBatteryBlock>(battery => battery.MaxStoredPower);

        static readonly OwnValue Charging = OwnValue.Flag(Reading.Of<IMyBatteryBlock>(battery => battery.IsCharging));

        static readonly OwnValue Topped = OwnValue.Flag(Full);

        static readonly OwnValue Input = OwnValue.Number<IMyBatteryBlock>(battery => battery.CurrentInput);

        static readonly OwnValue MostIn = OwnValue.Number<IMyBatteryBlock>(battery => battery.MaxInput);

        static readonly OwnValue Output = OwnValue.Number<IMyBatteryBlock>(battery => battery.CurrentOutput);

        static readonly OwnValue MostOut = OwnValue.Number<IMyBatteryBlock>(battery => battery.MaxOutput);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyBatteryBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Recharge, "recharge", Recharges, "Recharge_On").Lasting().Asking(RechargeOptions.Asked())
                .In(Category.Power)
                .Renaming("Recharge", false, "toggle recharge")
                .Renaming("Discharge", false, "toggle discharge")
                .Renaming("ChargeMode", true, "charge mode")
                .WithValue("charge", "charge", Charge).Percent().Announced().In(Category.Power)
                .WithValue("stored-power", "stored power", Stored).Measured(" MWh").Announced().In(Category.Power)
                .WithValue("capacity", "capacity", Capacity).Measured(" MWh").Announced().In(Category.Power)
                .WithValue("charging", "charging", Charging).Status().Looked(Unheard).In(Category.Power)
                .WithValue("charged", "charged", Topped).Status().Announced().In(Category.Power)
                .WithValue("input", "input", Input).Measured(" MW").Looked(Unheard).In(Category.Power)
                .WithValue("maximum-input", "maximum input", MostIn).Measured(" MW").Looked(Unheard).In(Category.Power)
                .WithValue("output", "output", Output).Measured(" MW").Announced().In(Category.Power)
                .WithValue("maximum-output", "maximum output", MostOut).Measured(" MW").Looked(Unheard).In(Category.Power);
        }

        /// <summary>Told as the battery's charge moves and as its output changes, both of which it announces.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var battery = block as IMyBatteryBlock;
            var store = block as IMyStoredPowerRatio;
            return battery == null || store == null ? null : new Listening(new ChargeListener(store, told), new OutputListener(battery, told));
        }

        /// <inheritdoc />
        public override Status StatusOf(IMyTerminalBlock block)
        {
            var battery = block as IMyBatteryBlock;
            if (battery == null)
                return Status.None;

            if (Charged(battery))
                return Status.Of(Condition.Full);

            if (battery.ChargeMode == ChargeMode.Recharge)
            {
                BlockTrouble stuck = Filling(battery);
                if (stuck.Exists)
                    return Status.Of(stuck);
            }

            if (battery.IsCharging)
                return Status.Of(Condition.Charging);

            return battery.HasCapacityRemaining ? Status.None : Status.Of(Condition.Empty);
        }

        /// <summary>Busy while it charges on a recharge, which is the one charging a step asks for.</summary>
        public override bool IsBusy(IMyTerminalBlock block)
        {
            var battery = block as IMyBatteryBlock;
            return battery != null && battery.ChargeMode == ChargeMode.Recharge && !Charged(battery)
                && battery.CurrentInput > 0f;
        }

        /// <summary>Why this battery will not fill, or null while it might.</summary>
        static BlockTrouble Why(IMyBatteryBlock battery)
        {
            if (Charged(battery))
                return BlockTrouble.None;

            if (battery.ChargeMode == ChargeMode.Discharge)
                return BlockTrouble.Fatal(Cause.BatteryToldToDischarge);

            if (battery.ChargeMode == ChargeMode.Auto)
                return BlockTrouble.Fatal(Cause.BatteryToldToAuto);

            return Filling(battery);
        }

        /// <summary>Why a battery set to recharge is not charging, or null while it is.</summary>
        static BlockTrouble Filling(IMyBatteryBlock battery)
        {
            // The switch and the damage, since the game calls an empty battery not working.
            if (!battery.Enabled)
                return BlockTrouble.Waiting(Cause.BatteryOff);

            if (!battery.IsFunctional)
                return BlockTrouble.Waiting(Cause.BatteryDamaged);

            return battery.CurrentInput > 0f
                ? BlockTrouble.None
                : BlockTrouble.Waiting(Cause.BatteryNoPower);
        }

        static bool Charged(IMyBatteryBlock battery)
        {
            return battery.MaxStoredPower > 0f && battery.CurrentStoredPower >= battery.MaxStoredPower;
        }
    }
}
