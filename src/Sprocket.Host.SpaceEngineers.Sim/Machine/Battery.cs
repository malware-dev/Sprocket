using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
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

    const float Most = 12f;

    ChargeMode _mode = ChargeMode.Auto;
    float _stored;
    Action<IMyStoredPowerRatio, float, float>? _charged;
    Action<IMyPowerProducer, float, float>? _output;

    /// <summary>A battery on the rig, empty and on auto, as the game's large battery.</summary>
    /// <remarks>It gives nothing out: the rig draws no power from it.</remarks>
    public Battery(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyBatteryBlock>()
            .Also<IMyStoredPowerRatio>()
            .Gives("ChargeMode", () => _mode)
            .Takes("ChargeMode", value => Mode((ChargeMode)value))
            .Gives("CurrentStoredPower", () => _stored)
            .Gives("MaxStoredPower", () => Capacity)
            .Gives("StoredPowerRatio", () => _stored / Capacity)
            .Gives("CurrentInput", () => Input)
            .Gives("MaxInput", () => _stored < Capacity ? Most : 0f)
            .Gives("CurrentOutput", () => 0f)
            .Gives("MaxOutput", () => _mode != ChargeMode.Recharge && _stored > 0f ? Most : 0f)
            .Gives("HasCapacityRemaining", () => _stored > 0f)
            .Gives("IsCharging", () => Input > 0f)
            .Does("add_StoredPowerRatioChanged", handler => { _charged += (Action<IMyStoredPowerRatio, float, float>)handler[0]; })
            .Does("remove_StoredPowerRatioChanged", handler => { _charged -= (Action<IMyStoredPowerRatio, float, float>)handler[0]; })
            .Does("add_CurrentOutputRatioChanged", handler => { _output += (Action<IMyPowerProducer, float, float>)handler[0]; })
            .Does("remove_CurrentOutputRatioChanged", handler => { _output -= (Action<IMyPowerProducer, float, float>)handler[0]; }));
    }

    /// <summary>How many are listening to the battery's charge and output, together.</summary>
    public int Listeners => (_charged == null ? 0 : _charged.GetInvocationList().Length)
        + (_output == null ? 0 : _output.GetInvocationList().Length);

    /// <summary>Whether the grid has power to give it, which is the rig's stand-in for a ship's reactors.</summary>
    public bool Powered { get; set; } = true;

    /// <summary>How full it is, from zero to one.</summary>
    public float Charge
    {
        get { return _stored / Capacity; }
        set { Store(value * Capacity); }
    }

    /// <summary>What it has been set to do with its charge.</summary>
    public ChargeMode ChargeMode => _mode;

    /// <summary>
    /// What it is being given, in megawatts: the large battery's own rate, and nothing once it is full,
    /// discharging, off or without power.
    /// </summary>
    /// <remarks>The large battery holds three megawatt hours, which is <c>Capacity</c>.</remarks>
    float Input => Enabled && Powered && _mode != ChargeMode.Discharge && _stored < Capacity ? Most : 0f;

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
    public override Literal Read(string propertyId)
    {
        return propertyId == "ChargeMode" ? Literal.Of((long)_mode) : base.Read(propertyId);
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
            Store(Math.Min(Capacity, _stored + Capacity / ChargeTicks));
    }

    /// <summary>Holds <paramref name="stored" /> megawatt hours, and says so where that is a change, as the game's battery does.</summary>
    void Store(float stored)
    {
        if (stored == _stored)
            return;

        var before = _stored / Capacity;
        _stored = stored;
        if (_charged != null)
            _charged((IMyStoredPowerRatio)Block, before, _stored / Capacity);
    }
}
