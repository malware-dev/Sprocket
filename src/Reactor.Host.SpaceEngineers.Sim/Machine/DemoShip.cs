using Reactor.World;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

namespace Reactor.Sim.Machine;
/// <summary>
/// The ship testers are given.
/// </summary>
/// <remarks>
/// Chosen to support the three tasks the prototype is meant to answer: open the
/// hangar when a ship approaches, build a working airlock, and flash the lights when
/// hydrogen runs low. The third quietly probes the event-versus-state confusion
/// without ever asking the tester about it.
/// </remarks>
static class DemoShip
{
    public static SimWorld Build()
    {
        var world = new SimWorld();

        world.Add(new SimDoor(1, "Hangar Door"));
        world.Add(new SimDoor(2, "Airlock Inner Door"));
        world.Add(new SimDoor(3, "Airlock Outer Door"));
        world.Add(new SimVent(4, "Airlock Vent"));

        world.Add(new SimLight(10, "Landing Lights"));
        world.Add(new SimLight(11, "Airlock Light"));
        world.Add(new SimLight(12, "Cockpit Light"));
        world.Add(new SimLight(13, "Warning Beacon"));

        world.Add(new SimSensor(20, "Approach Sensor"));
        world.Add(new SimButton(21, "Airlock Button"));
        world.Add(new SimTank(22, "Hydrogen Tank"));

        return world;
    }
}
