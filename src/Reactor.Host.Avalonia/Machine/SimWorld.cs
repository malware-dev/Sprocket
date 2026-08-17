using Reactor.World;

namespace Reactor.Host.Machine;

/// <summary>
/// The simulated construct.
/// </summary>
/// <remarks>
/// This stands in for one Space Engineers grid plus its mechanically connected
/// subgrids. There is deliberately no way to reach anything outside it: scope is a
/// property of the world the host hands over, not something sequences can opt out of.
/// </remarks>
sealed class SimWorld : IWorld
{
    readonly List<SimDevice> _devices = [];
    readonly Dictionary<long, SimDevice> _byId = [];

    public IReadOnlyList<IDevice> Devices => _devices;

    public IReadOnlyList<SimDevice> SimDevices => _devices;

    public IDevice? FindDevice(long deviceId) => _byId.GetValueOrDefault(deviceId);

    public void Invoke(IDevice device, string actionId) => ((SimDevice)device).Invoke(actionId);

    public T Add<T>(T device) where T : SimDevice
    {
        _devices.Add(device);
        _byId[device.Id] = device;
        return device;
    }

    /// <summary>
    /// Removes a device, standing in for a block being destroyed or ground down.
    /// Sequences referring to it stay put and start reporting themselves as broken.
    /// </summary>
    public void Remove(SimDevice device)
    {
        _devices.Remove(device);
        _byId.Remove(device.Id);
    }

    /// <summary>
    /// Advances the machine. Called before the sequences are ticked, so sequences always
    /// sample signals that are current for this tick.
    /// </summary>
    public void Tick()
    {
        for (var i = 0; i < _devices.Count; i++)
            _devices[i].Tick();
    }
}

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
