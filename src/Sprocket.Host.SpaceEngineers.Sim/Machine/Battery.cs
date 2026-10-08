using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode;

namespace Sprocket.Sim.Machine;

/// <summary>A battery on the rig, which fills on a recharge while power is coming in.</summary>
public sealed class Battery : Fitting
{
    const float Capacity = 3f;

    const int ChargeTicks = 120;

    ChargeMode _mode = ChargeMode.Auto;
    float _stored;

    /// <summary>A battery on the rig, empty and on auto, as the game's large battery.</summary>
    public Battery(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyBatteryBlock>()
            .Gives("ChargeMode", () => _mode)
            .Takes("ChargeMode", value => Mode((ChargeMode)value))
            .Gives("CurrentStoredPower", () => _stored)
            .Gives("MaxStoredPower", () => Capacity)
            .Gives("CurrentInput", () => Input)
            .Gives("HasCapacityRemaining", () => _stored > 0f)
            .Gives("IsCharging", () => Input > 0f));
    }

    /// <summary>Whether the grid has power to give it, which is the rig's stand-in for a ship's reactors.</summary>
    public bool Powered { get; set; } = true;

    /// <summary>How full it is, from zero to one.</summary>
    public float Charge
    {
        get { return _stored / Capacity; }
        set { _stored = value * Capacity; }
    }

    /// <summary>What it has been set to do with its charge.</summary>
    public ChargeMode ChargeMode => _mode;

    /// <summary>
    /// What it is being given, in megawatts: the large battery's own rate, and nothing once it is full,
    /// discharging, off or without power.
    /// </summary>
    /// <remarks>The large battery holds three megawatt hours, which is <c>Capacity</c>.</remarks>
    float Input => Enabled && Powered && _mode != ChargeMode.Discharge && _stored < Capacity ? 12f : 0f;

    void Mode(ChargeMode mode)
    {
        _mode = mode;
        Told(mode.ToString().ToLowerInvariant());
    }

    /// <summary>The game's actions for the charge mode, a pair each for two of them and one for auto.</summary>
    public override bool Offers(string actionId)
    {
        return actionId == "Recharge" || actionId == "Recharge_On" || actionId == "Recharge_Off"
            || actionId == "Discharge" || actionId == "Discharge_On" || actionId == "Discharge_Off"
            || actionId == "Auto" || base.Offers(actionId);
    }

    /// <inheritdoc />
    public override void Offered(List<Offer> into)
    {
        into.Add(new Offer("Recharge", "Recharge On/Off"));
        into.Add(new Offer("Recharge_On", "Recharge On"));
        into.Add(new Offer("Recharge_Off", "Recharge Off"));
        into.Add(new Offer("Discharge", "Discharge On/Off"));
        into.Add(new Offer("Discharge_On", "Discharge On"));
        into.Add(new Offer("Discharge_Off", "Discharge Off"));
        into.Add(new Offer("Auto", "Enable Auto"));
        base.Offered(into);
    }

    /// <summary>The charge mode, which the game reports as a number and names on its control.</summary>
    public override void Carried(List<Offer> into)
    {
        into.Add(new Offer("ChargeMode", "Charge Mode", ValueKind.Integer).Between(new List<Choice>
        {
            new Choice("Auto", "0"),
            new Choice("Recharge", "1"),
            new Choice("Discharge", "2")
        }));
        base.Carried(into);
    }

    /// <inheritdoc />
    public override bool Set(string propertyId, Literal value)
    {
        if (propertyId != "ChargeMode")
            return base.Set(propertyId, value);

        Mode((ChargeMode)(int)value.Number);
        return true;
    }

    /// <inheritdoc />
    public override bool Holds(string propertyId, Literal value)
    {
        if (propertyId != "ChargeMode")
            return base.Holds(propertyId, value);

        return value.Kind == ValueKind.Integer && value.Whole == (long)_mode;
    }

    /// <inheritdoc />
    public override void Perform(string actionId, IList<Literal> arguments)
    {
        if (actionId == "Recharge_On")
            Mode(ChargeMode.Recharge);
        else if (actionId == "Discharge_On")
            Mode(ChargeMode.Discharge);
        else if (actionId == "Auto" || actionId == "Recharge_Off" || actionId == "Discharge_Off")
            Mode(ChargeMode.Auto);
        else
            base.Perform(actionId, arguments);
    }

    /// <summary>Charges on a recharge, and lands exactly on full as the game's battery does.</summary>
    public override void Tick()
    {
        if (_mode == ChargeMode.Recharge && Input > 0f)
            _stored = Math.Min(Capacity, _stored + Capacity / ChargeTicks);
    }
}
