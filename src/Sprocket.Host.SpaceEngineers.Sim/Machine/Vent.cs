using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using SpaceEngineers.Game.ModAPI;
using VRage.ObjectBuilders;
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;

namespace Sprocket.Sim.Machine;

/// <summary>An air vent on the rig, which takes time to fill and to empty.</summary>
public sealed class Vent : Fitting
{
    const int CycleTicks = 90;

    /// <summary>The oxygen a working vent moves either way, measured in game.</summary>
    const float Rate = 300f;

    int _progress = CycleTicks;
    bool _pressurizing = true;

    /// <summary>A vent, <paramref name="id" /> and <paramref name="name" />, of the game's type and subtype, called <paramref name="display" />.</summary>
    public Vent(long id, string name, MyObjectBuilderType type, string subtype, string display)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyAirVent>()
            .Gives("Status", () => Status)
            .Gives("CanPressurize", () => !Leaking)
            .Gives("PressurizationEnabled", () => true)
            .Gives("GasInputPerSecond", () => In)
            .Gives("GasOutputPerSecond", () => Out)
            .Gives("Depressurize", () => !_pressurizing)
            .Takes("Depressurize", value =>
            {
                _pressurizing = !(bool)value;
                Told(_pressurizing ? "pressurize" : "depressurize");
            })
            // Nothing, rather than what the room holds, when the vent is not working - which is what
            // the game answers, and the reason Sprocket cannot read a level without asking whether the
            // vent is working first. A rig that answered anyway would hide exactly that.
            .Does("GetOxygenLevel", () => Enabled ? (float)_progress / CycleTicks : 0f));
    }

    /// <summary>Whether the construct has nowhere to put the room's air, as with its tanks full, so an emptying stops short.</summary>
    public bool Nowhere { get; set; }

    /// <summary>Whether the construct has no oxygen to give, as with its tanks empty, so a filling stops short.</summary>
    public bool Dry { get; set; }

    /// <summary>Whether the room is not airtight, so it never fills.</summary>
    public bool Leaking { get; set; }

    /// <summary>The oxygen it is being given, which it fills the room with; none where there is none to give.</summary>
    public float In => Enabled && _pressurizing && _progress < CycleTicks && !Dry && !Leaking ? Rate : 0f;

    /// <summary>The oxygen it is passing on, which it empties the room into; none where there is nowhere to put it.</summary>
    public float Out => Enabled && !_pressurizing && _progress > 0 && !Nowhere ? Rate : 0f;

    /// <summary>
    /// What the game would report for a vent in a sealed room.
    /// </summary>
    /// <remarks>
    /// <b>Depressurized is not one of them.</b> The game works its status out from the room, and an
    /// airtight one only ever reads Pressurized, Pressurizing or Depressurizing - a sealed airlock
    /// emptied to nothing still says Depressurizing. Only an unsealed room, or none at all, reads
    /// Depressurized, which is a vent hanging in space rather than one doing its job.
    /// <para>
    /// This said Depressurized at empty, and Sprocket waited on that state - so an airlock cycle that
    /// could never finish in the game finished here, in every test.
    /// </para>
    /// </remarks>
    VentStatus Status
    {
        get
        {
            if (_progress >= CycleTicks)
                return VentStatus.Pressurized;

            return _pressurizing ? VentStatus.Pressurizing : VentStatus.Depressurizing;
        }
    }

    /// <summary>The game's own pair for a vent's switch, which is called Depressurize.</summary>
    public override bool Offers(string actionId)
    {
        return actionId == "Depressurize" || actionId == "Depressurize_On"
            || actionId == "Depressurize_Off" || base.Offers(actionId);
    }

    /// <inheritdoc />
    public override void Offered(List<Offer> into)
    {
        into.Add(new Offer("Depressurize", "Depressurize On/Off"));
        into.Add(new Offer("Depressurize_On", "Depressurize On"));
        into.Add(new Offer("Depressurize_Off", "Depressurize Off"));
        base.Offered(into);
    }

    /// <summary>The switch itself, which the game carries beside the pair that turns it.</summary>
    public override void Carried(List<Offer> into)
    {
        into.Add(new Offer("Depressurize", "Depressurize", ValueKind.Flag, "", ""));
        base.Carried(into);
    }

    /// <inheritdoc />
    public override void Perform(string actionId, IList<Literal> arguments)
    {
        if (actionId == "Depressurize_On")
        {
            _pressurizing = false;
            Told("depressurize");
            return;
        }

        if (actionId == "Depressurize_Off")
        {
            _pressurizing = true;
            Told("pressurize");
            return;
        }

        base.Perform(actionId, arguments);
    }

    /// <inheritdoc />
    public override void Tick()
    {
        if (!Enabled)
            return;

        if (_pressurizing && _progress < CycleTicks && !Dry && !Leaking)
            _progress++;
        else if (!_pressurizing && _progress > 0 && !Nowhere)
            _progress--;
    }
}
