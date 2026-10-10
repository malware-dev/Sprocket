using System;
using Sandbox.ModAPI.Ingame;
using Sprocket.Sim.Fakes;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using IMyCockpit = Sandbox.ModAPI.IMyCockpit;

namespace Sprocket.Sim.Machine;

/// <summary>A cockpit on the rig, which somebody sits down in and gets up from, on a ship that moves as a test says.</summary>
/// <remarks>
/// The game tells a mod a seat has been sat in or left through <c>IsOccupiedChanged</c>, so the rig raises it on each,
/// and a seat that changed quietly would leave the listening untested.
/// </remarks>
public sealed class Seat : Fitting
{
    Action<IMyCockpit>? _changed;
    IMyCharacter? _pilot;

    /// <summary>A cockpit on the rig, empty, holding <paramref name="air" /> of oxygen where it is enclosed.</summary>
    public Seat(long id, string name, MyObjectBuilderType type, string subtype, string display, float air)
        : base(id, name, type, subtype, display)
    {
        Fit(Pretend.To<IMyCockpit>()
            .Gives("IsOccupied", () => _pilot != null)
            .Gives("Pilot", () => _pilot!)
            .Gives("IsUnderControl", () => _pilot != null)
            .Gives("OxygenCapacity", () => air)
            .Gives("OxygenFilledRatio", () => Air)
            .Does("GetShipSpeed", () => Speed)
            .Does("GetNaturalGravity", () => Gravity)
            .Does("CalculateShipMass", () => new MyShipMass(Mass, Mass, Mass))
            .Does("RemovePilot", _ => Rise())
            .Answers("add_IsOccupiedChanged", invocation =>
            {
                _changed = (Action<IMyCockpit>?)Delegate.Combine(_changed, (Delegate)invocation.Arguments[0]);
                return null!;
            })
            .Answers("remove_IsOccupiedChanged", invocation =>
            {
                _changed = (Action<IMyCockpit>?)Delegate.Remove(_changed, (Delegate)invocation.Arguments[0]);
                return null!;
            }));
    }

    /// <summary>How full of oxygen it is, from 0 to 1.</summary>
    public float Air { get; set; }

    /// <summary>How fast the ship moves, in metres a second.</summary>
    public double Speed { get; set; }

    /// <summary>The pull of a planet on the ship, in metres a second squared.</summary>
    public Vector3D Gravity { get; set; }

    /// <summary>What the ship weighs, cargo included, in kilograms.</summary>
    public float Mass { get; set; }

    /// <summary>Somebody called <paramref name="who" /> sitting down.</summary>
    public void Sit(string who)
    {
        _pilot = Pretend.To<IMyCharacter>().Gives("DisplayName", () => who).Done();
        Told("sat " + who);
        _changed?.Invoke((IMyCockpit)Block);
    }

    /// <summary>Whoever sits in it getting up, as the game puts them out.</summary>
    public void Rise()
    {
        _pilot = null;
        Told("empty");
        _changed?.Invoke((IMyCockpit)Block);
    }
}
