using Reactor.World;

namespace Reactor.Host.System;

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
    readonly List<SimEntity> _entities = [];
    readonly Dictionary<long, SimEntity> _byId = [];

    public IReadOnlyList<IEntity> Entities => _entities;

    /// <summary>
    /// Space Engineers says block, so Reactor says block. Nobody playing it has ever called
    /// a door a device.
    /// </summary>
    public EntityNoun Noun { get; } = new EntityNoun("block", "blocks", "Blocks");

    public string Name { get; private set; } = "Test Rig";

    public bool CanRename => true;

    public void Rename(string name) => Name = name;

    public IReadOnlyList<SimEntity> SimEntities => _entities;

    public IEntity? FindEntity(long entityId) => _byId.GetValueOrDefault(entityId);

    public void Invoke(IEntity entity, string actionId) => ((SimEntity)entity).Invoke(actionId);

    public T Add<T>(T entity) where T : SimEntity
    {
        _entities.Add(entity);
        _byId[entity.Id] = entity;
        return entity;
    }

    /// <summary>
    /// Removes an entity, standing in for a block being destroyed or ground down.
    /// Sequences referring to it stay put and start reporting themselves as broken.
    /// </summary>
    public void Remove(SimEntity entity)
    {
        _entities.Remove(entity);
        _byId.Remove(entity.Id);
    }

    /// <summary>
    /// Advances the system. Called before the sequences are ticked, so sequences always
    /// sample signals that are current for this tick.
    /// </summary>
    public void Tick()
    {
        for (var i = 0; i < _entities.Count; i++)
            _entities[i].Tick();
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
