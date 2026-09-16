using System;
using System.Collections.Generic;
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

    public Rig()
    {
        Reactor = _grid.Weld(new Programmable(100, "Automation",
            Type<MyObjectBuilder_MyProgrammableBlock>(), "LargeProgrammableBlock",
            "Programmable Block"));

        _grid.Weld(new Door(1, "Hangar Door", Type<MyObjectBuilder_AirtightHangarDoor>(),
            "AirtightHangarDoor", "Hangar Door"));
        _grid.Weld(new Door(2, "Airlock Inner Door", Type<MyObjectBuilder_Door>(),
            "LargeBlockDoor", "Door"));
        _grid.Weld(new Door(3, "Airlock Outer Door", Type<MyObjectBuilder_Door>(),
            "LargeBlockDoor", "Door"));
        _grid.Weld(new Vent(4, "Airlock Vent", Type<MyObjectBuilder_AirVent>(),
            "SmallAirVent", "Air Vent"));

        _grid.Weld(new Plain(10, "Landing Lights", Type<MyObjectBuilder_InteriorLight>(),
            "LargeBlockLight_1corner", "Interior Light"));
        _grid.Weld(new Plain(11, "Airlock Light", Type<MyObjectBuilder_InteriorLight>(),
            "LargeBlockLight_1corner", "Interior Light"));
        _grid.Weld(new Plain(12, "Cockpit Light", Type<MyObjectBuilder_InteriorLight>(),
            "LargeBlockLight_1corner", "Interior Light"));
        _grid.Weld(new Plain(13, "Warning Beacon", Type<MyObjectBuilder_Beacon>(),
            "LargeBlockBeacon", "Beacon"));

        _grid.Weld(new Plain(20, "Approach Sensor", Type<MyObjectBuilder_SensorBlock>(),
            "LargeBlockSensor", "Sensor"));
        _grid.Weld(new Panel(21, "Airlock Button", Type<MyObjectBuilder_ButtonPanel>(),
            "ButtonPanelLarge", "Button Panel", 1));
        _grid.Weld(new Plain(22, "Hydrogen Tank", Type<MyObjectBuilder_OxygenTank>(),
            "LargeHydrogenTank", "Hydrogen Tank"));
    }

    /// <summary>The block Reactor runs in, which is what the mod is handed.</summary>
    public Programmable Reactor { get; private set; }

    public IReadOnlyList<Fitting> Fitted => _grid.Fitted;

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
