using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>
/// A gas tank on the rig, which fills while it stockpiles and has gas to take in.
/// </summary>
/// <remarks>
/// It carries no resource components, as no block on the rig does, so the gas it is given cannot be
/// read and a tank with none coming in says nothing about it here.
/// </remarks>
public sealed class Tank : Fitting
{
    const int FillTicks = 120;

    bool _stockpile;
    double _filled;

    /// <summary>A gas tank on the rig, empty and not stockpiling.</summary>
    public Tank(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyGasTank>()
            .Gives("Stockpile", () => _stockpile)
            .Takes("Stockpile", value => Stockpiling((bool)value))
            .Gives("FilledRatio", () => _filled));
    }

    /// <summary>Whether the grid has gas to give it, which is the rig's stand-in for a ship's generators.</summary>
    public bool Supplied { get; set; } = true;

    /// <summary>How full it is, from zero to one.</summary>
    public double Filled
    {
        get { return _filled; }
        set { _filled = value; }
    }

    /// <summary>Whether it is stockpiling.</summary>
    public bool Stockpile => _stockpile;

    void Stockpiling(bool on)
    {
        _stockpile = on;
        Told(on ? "stockpile" : "stockpile off");
    }

    /// <summary>The game's actions for stockpiling and for the bottles it refills.</summary>
    public override bool Offers(string actionId)
    {
        return actionId == "Stockpile" || actionId == "Stockpile_On" || actionId == "Stockpile_Off"
            || actionId == "Refill" || actionId == "Auto-Refill" || base.Offers(actionId);
    }

    /// <inheritdoc />
    public override void Offered(List<Offer> into)
    {
        into.Add(new Offer("Stockpile", "Stockpile On/Off"));
        into.Add(new Offer("Stockpile_On", "Stockpile On"));
        into.Add(new Offer("Stockpile_Off", "Stockpile Off"));
        into.Add(new Offer("Refill", "Refill Bottles"));
        into.Add(new Offer("Auto-Refill", "Auto-Refill On/Off"));
        base.Offered(into);
    }

    /// <inheritdoc />
    public override void Carried(List<Offer> into)
    {
        into.Add(new Offer("Stockpile", "Stockpile", ValueKind.Flag, "", ""));
        into.Add(new Offer("Auto-Refill", "Auto-Refill", ValueKind.Flag, "", ""));
        base.Carried(into);
    }

    /// <inheritdoc />
    public override bool Set(string propertyId, Literal value)
    {
        if (propertyId != "Stockpile")
            return base.Set(propertyId, value);

        Stockpiling(value.Flag);
        return true;
    }

    /// <inheritdoc />
    public override bool Holds(string propertyId, Literal value)
    {
        if (propertyId != "Stockpile")
            return base.Holds(propertyId, value);

        return value.Kind == ValueKind.Flag && value.Flag == _stockpile;
    }

    /// <inheritdoc />
    public override void Perform(string actionId, IList<Literal> values)
    {
        if (actionId == "Stockpile_On")
            Stockpiling(true);
        else if (actionId == "Stockpile_Off")
            Stockpiling(false);
        else if (actionId == "Stockpile")
            Stockpiling(!_stockpile);
        else
            base.Perform(actionId, values);
    }

    /// <summary>Fills while it stockpiles, and lands exactly on full as the game's tank does.</summary>
    public override void Tick()
    {
        if (Enabled && Supplied && _stockpile)
            _filled = Math.Min(1.0, _filled + 1.0 / FillTicks);
    }
}
