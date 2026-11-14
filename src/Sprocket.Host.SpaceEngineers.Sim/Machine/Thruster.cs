using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>A thruster on the rig, which pushes exactly as hard as it is overridden to and announces its override.</summary>
/// <remarks>It flies nothing: the rig has no flight to give it, so its thrust is its override and the air leaves all of it.</remarks>
public sealed class Thruster : Fitting
{
    /// <summary>The large ion thruster's force, in newtons, from its definition's ForceMagnitude.</summary>
    const float Force = 4320000f;

    float _override;
    Action<IMyThrust, float>? _overridden;

    /// <summary>A thruster on the rig, not overridden.</summary>
    public Thruster(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyThrust>()
            .Gives("ThrustOverridePercentage", () => _override)
            .Takes("ThrustOverridePercentage", value => Overridden((float)value))
            .Gives("ThrustOverride", () => _override * Force)
            .Gives("MaxThrust", () => Force)
            .Gives("MaxEffectiveThrust", () => Force)
            .Gives("CurrentThrust", () => _override * Force)
            .Gives("CurrentThrustPercentage", () => _override * 100f)
            .Does("add_ThrustOverrideChanged", handler => { _overridden += (Action<IMyThrust, float>)handler[0]; })
            .Does("remove_ThrustOverrideChanged", handler => { _overridden -= (Action<IMyThrust, float>)handler[0]; }));
    }

    /// <summary>How many are listening to its override.</summary>
    public int Listeners => _overridden == null ? 0 : _overridden.GetInvocationList().Length;

    /// <summary>What it is overridden to, from nought, which is none, to one.</summary>
    public float Override
    {
        get { return _override; }
        set { Overridden(value); }
    }

    /// <summary>Holds its override to between nought and one, and says so where that is a change, as the game's thruster does.</summary>
    void Overridden(float ratio)
    {
        ratio = Math.Max(0f, Math.Min(1f, ratio));
        if (ratio == _override)
            return;

        _override = ratio;
        Told("override " + Math.Round(ratio * 100f));
        if (_overridden != null)
            _overridden((IMyThrust)Block, _override * Force);
    }

    /// <summary>The game's steps of its override, and its setter in percent.</summary>
    public override bool Offers(string actionId)
    {
        return actionId == "IncreaseOverride" || actionId == "DecreaseOverride" || actionId == "SetOverride"
            || base.Offers(actionId);
    }

    /// <inheritdoc />
    public override void Offered(List<Offer> into)
    {
        into.Add(new Offer("IncreaseOverride", "Increase Thrust override"));
        into.Add(new Offer("DecreaseOverride", "Decrease Thrust override"));
        into.Add(new Offer("SetOverride", "Set override", 1));
        base.Offered(into);
    }

    /// <summary>Its override in newtons, which is how the game's terminal carries it.</summary>
    public override void Carried(List<Offer> into)
    {
        into.Add(new Offer("Override", "Thrust override", ValueKind.Real, "0", Force.ToString(CultureInfo.InvariantCulture)));
        base.Carried(into);
    }

    /// <inheritdoc />
    public override bool Set(string propertyId, Literal value)
    {
        if (propertyId != "Override")
            return base.Set(propertyId, value);

        Overridden((float)(value.Number / Force));
        return true;
    }

    /// <inheritdoc />
    public override Literal Read(string propertyId)
    {
        return propertyId == "Override" ? Literal.Of(_override * Force) : base.Read(propertyId);
    }
}
