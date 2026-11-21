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

    /// <summary>
    /// A cockpit on the rig, empty, holding <paramref name="air" /> of oxygen where it is enclosed, a cryo chamber where
    /// <paramref name="cryo" /> says so, and able to fly the ship unless <paramref name="flies" /> says otherwise.
    /// </summary>
    /// <remarks>
    /// A cryo chamber is a cockpit to the game, which tells it apart by the interface it adds. A seat that cannot fly the
    /// ship, a couch or a toilet, is a cockpit whose definition says so. A seat has the screens <paramref name="faces" />
    /// holds, and none where it holds none.
    /// </remarks>
    public Seat(long id, string name, MyObjectBuilderType type, string subtype, string display, float air, bool cryo = false,
        bool flies = true, Face[]? faces = null)
        : base(id, name, type, subtype, display)
    {
        // The hotbar, nine pages of nine, as every seat in the game has.
        Toolbar = new Toolbar(9, 9);
        Faces = faces ?? [];

        var stand = Pretend.To<IMyCockpit>();
        if (cryo)
            stand.Also<Sandbox.ModAPI.IMyCryoChamber>();
        if (Faces.Length > 0)
        {
            stand.Also<IMyTextSurfaceProvider>()
                .Gives("UseGenericLcd", () => true)
                .Gives("SurfaceCount", () => Faces.Length)
                .Does("GetSurface", arguments => Faces[(int)arguments[0]].Surface);
        }

        Fit(stand
            .Gives("IsOccupied", () => _pilot != null)
            .Gives("Pilot", () => _pilot!)
            .Gives("IsUnderControl", () => _pilot != null)
            .Gives("CanControlShip", () => flies)
            .Gives("OxygenCapacity", () => air)
            .Gives("OxygenFilledRatio", () => Air)
            .Does("GetShipSpeed", () => Speed)
            .Does("GetNaturalGravity", () => Natural)
            .Does("GetArtificialGravity", () => Artificial)
            .Does("CalculateShipMass", () => new MyShipMass(Mass, Mass, Mass))
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

    /// <summary>Its screens, in the order the game numbers them.</summary>
    public Face[] Faces { get; }

    /// <summary>How full of oxygen it is, from 0 to 1.</summary>
    public float Air { get; set; }

    /// <summary>How fast the ship moves, in metres a second.</summary>
    public double Speed { get; set; }

    /// <summary>The pull of a planet on the ship, in metres a second squared.</summary>
    public Vector3D Natural { get; set; }

    /// <summary>The pull of gravity generators on the ship, in metres a second squared.</summary>
    public Vector3D Artificial { get; set; }

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

    /// <summary>Uses slot <paramref name="slot" /> of the seat's toolbar, counted from zero, as a seated player pressing its key does.</summary>
    public void Press(int slot)
    {
        Run(slot);
    }
}
