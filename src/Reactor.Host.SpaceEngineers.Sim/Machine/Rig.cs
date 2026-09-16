using System;
using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.World;
using Reactor.Sim.Fakes;
using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Reactor.Sim.Machine;

/// <summary>
/// The ship testers are given, as blocks the game would recognise.
/// </summary>
/// <remarks>
/// Chosen to support the three tasks the prototype is meant to answer: open the hangar when a ship
/// approaches, build a working airlock, and flash the lights when hydrogen runs low. The third
/// quietly probes the event-versus-state confusion without ever asking the tester about it.
/// <para>
/// Every block here is a real block type with a real definition id, because that is what the mod
/// builds its vocabulary from: a door offers "open" because it is an <c>IMyDoor</c>, and a light
/// offers only a switch because that is all the mod knows about lights.
/// </para>
/// </remarks>
sealed class Rig
{
    readonly Grid _grid = new Grid("Test Rig");

    /// <summary>
    /// A construct with nothing on it but the block Reactor runs in.
    /// </summary>
    /// <remarks>
    /// What a test wants: exactly the blocks it is about, welded on by name, so what it asserts is
    /// not mixed in with a demonstration ship.
    /// </remarks>
    public Rig()
    {
        Reactor = _grid.Weld(new Programmable(100, "Automation",
            Type<MyObjectBuilder_MyProgrammableBlock>(), "LargeProgrammableBlock",
            "Programmable Block"));
    }

    /// <summary>
    /// The ship testers are given.
    /// </summary>
    /// <remarks>
    /// Chosen to support the three tasks the prototype is meant to answer: open the hangar when a
    /// ship approaches, build a working airlock, and flash the lights when hydrogen runs low.
    /// </remarks>
    public static Rig Demo()
    {
        var rig = new Rig();

        rig.Weld(new Door(1, "Hangar Door", Type<MyObjectBuilder_AirtightHangarDoor>(),
            "AirtightHangarDoor", "Hangar Door"));
        rig.Weld(new Door(2, "Airlock Inner Door", Type<MyObjectBuilder_Door>(),
            "LargeBlockDoor", "Door"));
        rig.Weld(new Door(3, "Airlock Outer Door", Type<MyObjectBuilder_Door>(),
            "LargeBlockDoor", "Door"));
        rig.Weld(new Vent(4, "Airlock Vent", Type<MyObjectBuilder_AirVent>(),
            "SmallAirVent", "Air Vent"));

        rig.Weld(new Plain(10, "Landing Lights", Type<MyObjectBuilder_InteriorLight>(),
            "LargeBlockLight_1corner", "Interior Light"));
        rig.Weld(new Plain(11, "Airlock Light", Type<MyObjectBuilder_InteriorLight>(),
            "LargeBlockLight_1corner", "Interior Light"));
        rig.Weld(new Plain(12, "Cockpit Light", Type<MyObjectBuilder_InteriorLight>(),
            "LargeBlockLight_1corner", "Interior Light"));
        rig.Weld(new Plain(13, "Warning Beacon", Type<MyObjectBuilder_Beacon>(),
            "LargeBlockBeacon", "Beacon"));

        rig.Weld(new Plain(20, "Approach Sensor", Type<MyObjectBuilder_SensorBlock>(),
            "LargeBlockSensor", "Sensor"));
        rig.Weld(new Panel(21, "Airlock Button", Type<MyObjectBuilder_ButtonPanel>(),
            "ButtonPanelLarge", "Button Panel", 1));
        rig.Weld(new Plain(22, "Hydrogen Tank", Type<MyObjectBuilder_OxygenTank>(),
            "LargeHydrogenTank", "Hydrogen Tank"));

        return rig;
    }

    /// <summary>Welds a block on, as building one does.</summary>
    public T Weld<T>(T fitting) where T : Fitting
    {
        return _grid.Weld(fitting);
    }

    /// <summary>Grinds one off, which leaves whatever named it pointing at nothing.</summary>
    public void Grind(Fitting fitting)
    {
        _grid.Grind(fitting);
    }

    /// <summary>A door, named, with the game's own type behind it.</summary>
    public Door Door(long id, string name)
    {
        return Weld(new Door(id, name, Type<MyObjectBuilder_Door>(), "LargeBlockDoor", "Door"));
    }

    public Vent Vent(long id, string name)
    {
        return Weld(new Vent(id, name, Type<MyObjectBuilder_AirVent>(), "SmallAirVent", "Air Vent"));
    }

    /// <summary>A button panel, named, with as many buttons as asked for.</summary>
    public Panel Panel(long id, string name, int buttons = 1)
    {
        return Weld(new Panel(id, name, Type<MyObjectBuilder_ButtonPanel>(), "ButtonPanelLarge",
            "Button Panel", buttons));
    }

    /// <summary>A block with nothing but a switch, which is most of a ship.</summary>
    public Plain Light(long id, string name)
    {
        return Weld(new Plain(id, name, Type<MyObjectBuilder_InteriorLight>(),
            "LargeBlockLight_1corner", "Interior Light"));
    }

    /// <summary>The block Reactor runs in, which is what the mod is handed.</summary>
    public Programmable Reactor { get; private set; }

    public IReadOnlyList<Fitting> Fitted => _grid.Fitted;

    /// <summary>Everything the construct has been told to do, oldest first.</summary>
    public List<string> Log => _grid.Journal;

    /// <summary>Forgets what was done, so a test can watch one moment.</summary>
    public void Forget()
    {
        _grid.Journal.Clear();
    }

    /// <summary>
    /// Puts the harness behind the statics the mod reaches the world through.
    /// </summary>
    /// <remarks>
    /// Only what the world layer reads: which grids are mechanically one construct, and whose game
    /// this is. Everything else on the gateway stays null, which is exactly what the mod tolerates
    /// on a machine that is not a server.
    /// </remarks>
    public void Install()
    {
        // Every toolbar read and write the mod makes, answered over this rig's own slots.
        Toolbars.Use(new RigSlots(this));

        IMyCubeGrid grid = _grid.Block;

        MyAPIGateway.GridGroups = Pretend.To<IMyGridGroups>()
            .Does("GetGroup", arguments =>
            {
                var into = arguments[2] as List<IMyCubeGrid>;
                if (into != null)
                    into.Add(grid);

                return null;
            })
            .Done();

        MyAPIGateway.Multiplayer = Pretend.To<IMyMultiplayer>()
            .Gives("MyName", () => Environment.UserName)
            .Gives("IsServer", () => true)
            .Done();
    }

    public void Tick()
    {
        _grid.Tick();
    }

    static MyObjectBuilderType Type<T>()
    {
        return new MyObjectBuilderType(typeof(T));
    }
}
