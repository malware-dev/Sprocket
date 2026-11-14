using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;
using MyJumpDriveStatus = Sandbox.ModAPI.Ingame.MyJumpDriveStatus;

namespace Sprocket.Sim.Machine;

/// <summary>A jump drive on the rig, recharging, which holds its jump distance within its reach as the game's drive does.</summary>
/// <remarks>
/// It never jumps and never fills: the rig has nowhere to jump to and no power to give it. Its reach is fixed at what the
/// game gives a light ship once a drive is charged, though its own charge stays at nothing.
/// </remarks>
public sealed class JumpDrive : Fitting
{
    /// <summary>The large jump drive's power needed for a jump, in megawatt hours, from its definition.</summary>
    const float Capacity = 3f;

    /// <summary>The large jump drive's shortest jump, in meters, from its definition.</summary>
    const float Shortest = 5000f;

    /// <summary>The longest jump the rig's drive reaches, in meters, which in the game follows the ship's mass.</summary>
    const float Longest = 2000000f;

    float _share = 1f;
    bool _recharge = true;

    /// <summary>A jump drive on the rig, empty, recharging, and set to its longest jump.</summary>
    public JumpDrive(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyJumpDrive>()
            .Also<IMyStoredPowerRatio>()
            .Gives("JumpDistanceMeters", () => Distance)
            .Takes("JumpDistanceMeters", value => Reach((float)value))
            .Gives("JumpDistanceRatio", () => _share)
            .Gives("MinJumpDistanceMeters", () => Shortest)
            .Gives("MaxJumpDistanceMeters", () => Longest)
            .Gives("CurrentStoredPower", () => 0f)
            .Gives("MaxStoredPower", () => Capacity)
            .Gives("StoredPowerRatio", () => 0f)
            .Gives("Status", () => MyJumpDriveStatus.Charging)
            .Gives("Recharge", () => _recharge)
            .Takes("Recharge", value => Recharges((bool)value))
            .Does("add_StoredPowerRatioChanged", () => null)
            .Does("remove_StoredPowerRatioChanged", () => null));
    }

    /// <summary>How far it is set to jump, in meters: its share of the way from its shortest jump to its longest.</summary>
    public float Distance => Shortest + (Longest - Shortest) * _share;

    /// <summary>Whether it recharges.</summary>
    public bool Recharging => _recharge;

    /// <summary>Sets how far it jumps, held to between its shortest and longest jump as the game's drive holds it.</summary>
    void Reach(float meters)
    {
        var held = Math.Max(Shortest, Math.Min(Longest, meters));
        _share = (held - Shortest) / (Longest - Shortest);
        Told("jump distance " + Math.Round(held / 1000f));
    }

    /// <summary>The game's actions for recharging and jumping, and the steps and the setter of its jump distance.</summary>
    public override bool Offers(string actionId)
    {
        return actionId == "Recharge" || actionId == "Recharge_On" || actionId == "Recharge_Off" || actionId == "Jump"
            || actionId == "IncreaseJumpDistance" || actionId == "DecreaseJumpDistance"
            || actionId == "SetGravityAcceleration" || base.Offers(actionId);
    }

    /// <inheritdoc />
    public override void Offered(List<Offer> into)
    {
        into.Add(new Offer("Recharge", "Recharge On/Off"));
        into.Add(new Offer("Recharge_On", "Recharge On"));
        into.Add(new Offer("Recharge_Off", "Recharge Off"));
        into.Add(new Offer("Jump", "Jump"));
        into.Add(new Offer("IncreaseJumpDistance", "Increase Jump distance"));
        into.Add(new Offer("DecreaseJumpDistance", "Decrease Jump distance"));
        into.Add(new Offer("SetGravityAcceleration", "Set jump distance", 1));
        base.Offered(into);
    }

    /// <summary>Whether it recharges, and its jump distance as a share of its reach in percent, as the game's terminal carries them.</summary>
    public override void Carried(List<Offer> into)
    {
        into.Add(new Offer("Recharge", "Recharge", ValueKind.Flag));
        into.Add(new Offer("JumpDistance", "Jump distance", ValueKind.Real, "0", "100"));
        base.Carried(into);
    }

    /// <inheritdoc />
    public override bool Set(string propertyId, Literal value)
    {
        if (propertyId != "Recharge")
            return base.Set(propertyId, value);

        Recharges(value.Flag);
        return true;
    }

    /// <inheritdoc />
    public override Literal Read(string propertyId)
    {
        return propertyId == "Recharge" ? Literal.Of(_recharge) : base.Read(propertyId);
    }

    /// <inheritdoc />
    public override void Perform(string actionId, IList<Literal> arguments)
    {
        if (actionId == "Recharge_On")
            Recharges(true);
        else if (actionId == "Recharge_Off")
            Recharges(false);
        else if (actionId == "Recharge")
            Recharges(!_recharge);
        else
            base.Perform(actionId, arguments);
    }

    void Recharges(bool on)
    {
        _recharge = on;
        Told(on ? "recharge" : "recharge off");
    }
}
