using System;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

// The charge mode lives in the Ingame namespace, which cannot be imported whole: IMyBatteryBlock is
// declared in both.
using ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A battery: recharged and waited on until it is full, and read for how much it holds and how hard it is worked.
    /// </summary>
    /// <remarks>
    /// The game moves its charge on once every hundred frames, says so, and lands it exactly on the battery's capacity
    /// when it gets there, so full is a comparison and needs no band.
    /// <para>
    /// Nothing to call off: a battery left recharging holds nobody in danger, and its mode is a step of its own.
    /// </para>
    /// </remarks>
    public sealed class Battery : Capability
    {
        /// <summary>Recharging, which is Sprocket's own and may wait until the battery is full.</summary>
        public const string Recharge = "recharge";

        const string Unheard = "A battery raises no event as its input or its mode changes.";

        static readonly Reading Full = Reading.Of<IMyBatteryBlock>(Charged);

        static readonly Doing Recharges = Doing.Of<IMyBatteryBlock, Recharging>(Recharging.Read,
            (battery, recharging, seen) => battery.ChargeMode = ChargeMode.Recharge, Full,
            (battery, recharging, seen) => !recharging.Waits,
            (battery, recharging, seen) => recharging.Waits ? Why(battery) : Trouble.None);

        static readonly Being Charge = Being.Number<IMyBatteryBlock>(battery =>
            battery.MaxStoredPower > 0f ? battery.CurrentStoredPower / battery.MaxStoredPower * 100.0 : 0.0);

        static readonly Being Stored = Being.Number<IMyBatteryBlock>(battery => battery.CurrentStoredPower);

        static readonly Being Capacity = Being.Number<IMyBatteryBlock>(battery => battery.MaxStoredPower);

        static readonly Being Charging = Being.Flag(Reading.Of<IMyBatteryBlock>(battery => battery.IsCharging));

        static readonly Being Input = Being.Number<IMyBatteryBlock>(battery => battery.CurrentInput);

        static readonly Being MostIn = Being.Number<IMyBatteryBlock>(battery => battery.MaxInput);

        static readonly Being Output = Being.Number<IMyBatteryBlock>(battery => battery.CurrentOutput);

        static readonly Being MostOut = Being.Number<IMyBatteryBlock>(battery => battery.MaxOutput);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyBatteryBlock;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Capacity is fixed by the block, so nothing need announce it. The most it takes in and gives out are not: the game
        /// sets both to nothing while the battery is full, empty or held to one direction.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Recharge, "recharge", Recharges, "Recharge_On").Lasting().Asking(Recharging.Asked())
                .In(Category.Power)
                .Renaming("Recharge", false, "toggle recharge")
                .Renaming("Discharge", false, "toggle discharge")
                .Renaming("ChargeMode", true, "charge mode")
                .WithValue("charge", "charge", Charge).Announced().In(Category.Power)
                .WithValue("stored-power", "stored power", Stored).Announced().In(Category.Power)
                .WithValue("capacity", "capacity", Capacity).Announced().In(Category.Power)
                .WithValue("charging", "charging", Charging).Looked(Unheard).In(Category.Power)
                .WithValue("input", "input", Input).Looked(Unheard).In(Category.Power)
                .WithValue("maximum-input", "maximum input", MostIn).Looked(Unheard).In(Category.Power)
                .WithValue("output", "output", Output).Announced().In(Category.Power)
                .WithValue("maximum-output", "maximum output", MostOut).Looked(Unheard).In(Category.Power);
        }

        /// <summary>Told as the battery's charge moves and as its output changes, both of which it announces.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var battery = block as IMyBatteryBlock;
            var store = block as IMyStoredPowerRatio;
            return battery == null || store == null ? null : new Listening(new Storing(store, told), new Producing(battery, told));
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
                Trouble stuck = Filling(battery);
                if (stuck.Exists)
                    return Status.Of(stuck);
            }

            if (battery.IsCharging)
                return Status.Of(Condition.Charging);

            return battery.HasCapacityRemaining ? Status.None : Status.Of(Condition.Empty);
        }

        /// <summary>Busy while it charges on a recharge, which is the one charging a step asks for.</summary>
        /// <remarks>
        /// A battery on auto charges whenever the grid has power to spare, so counting that would light
        /// every battery on a ship with a solar panel.
        /// </remarks>
        public override bool IsBusy(IMyTerminalBlock block)
        {
            var battery = block as IMyBatteryBlock;
            return battery != null && battery.ChargeMode == ChargeMode.Recharge && !Charged(battery)
                && battery.CurrentInput > 0f;
        }

        /// <summary>
        /// Why this battery will not fill, or null while it might.
        /// </summary>
        /// <remarks>
        /// Set to another mode is its own answer: whatever set it has overruled the step, and nothing
        /// is heading towards full any more.
        /// </remarks>
        static Trouble Why(IMyBatteryBlock battery)
        {
            if (Charged(battery))
                return Trouble.None;

            if (battery.ChargeMode == ChargeMode.Discharge)
                return Trouble.Fatal(Cause.BatteryToldToDischarge);

            if (battery.ChargeMode == ChargeMode.Auto)
                return Trouble.Fatal(Cause.BatteryToldToAuto);

            return Filling(battery);
        }

        /// <summary>Why a battery set to recharge is not charging, or null while it is.</summary>
        /// <remarks>
        /// Asked of the switch and the damage rather than of whether it is working, because the game
        /// calls a battery with nothing left in it not working, and an empty battery is the one most
        /// likely to be recharged.
        /// </remarks>
        static Trouble Filling(IMyBatteryBlock battery)
        {
            if (!battery.Enabled)
                return Trouble.Waiting(Cause.BatteryOff);

            if (!battery.IsFunctional)
                return Trouble.Waiting(Cause.BatteryDamaged);

            return battery.CurrentInput > 0f
                ? Trouble.None
                : Trouble.Waiting(Cause.BatteryNoPower);
        }

        static bool Charged(IMyBatteryBlock battery)
        {
            return battery.MaxStoredPower > 0f && battery.CurrentStoredPower >= battery.MaxStoredPower;
        }

        /// <summary>A battery's charge and output being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly Storing _charge;
            readonly Producing _output;

            public Listening(Storing charge, Producing output)
            {
                _charge = charge;
                _output = output;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _charge.Dispose();
                _output.Dispose();
            }
        }
    }
}
