using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

// The charge mode lives in the Ingame namespace, which cannot be imported whole: IMyBatteryBlock is
// declared in both.
using ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A battery, which can be recharged and waited on until it is full.
    /// </summary>
    /// <remarks>
    /// Its charge is sampled. The game moves it on once every hundred frames and lands it exactly on
    /// the battery's capacity when it gets there, so full is a comparison and needs no band.
    /// <para>
    /// Nothing to call off: a battery left recharging holds nobody in danger, and its mode is a step of its own.
    /// </para>
    /// </remarks>
    public sealed class Battery : Capability
    {
        /// <summary>Recharging until full, which is Sprocket's own and waits until the battery is full.</summary>
        public const string Recharge = "recharge";

        static readonly Reading Full = Reading.Of<IMyBatteryBlock>(Charged);

        static readonly Doing Recharges = Doing.Of<IMyBatteryBlock>(battery => battery.ChargeMode = ChargeMode.Recharge,
            Full, Why);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyBatteryBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Recharge, "recharge", Recharges, "Recharge_On").Lasting().In(Category.Power);
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
                Trouble stuck = Charging(battery);
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

            return Charging(battery);
        }

        /// <summary>Why a battery set to recharge is not charging, or null while it is.</summary>
        /// <remarks>
        /// Asked of the switch and the damage rather than of whether it is working, because the game
        /// calls a battery with nothing left in it not working, and an empty battery is the one most
        /// likely to be recharged.
        /// </remarks>
        static Trouble Charging(IMyBatteryBlock battery)
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
    }
}
