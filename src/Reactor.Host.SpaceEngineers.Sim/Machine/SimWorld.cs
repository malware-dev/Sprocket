using Reactor.World;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Sim.Machine;
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

    /// <summary>The values this world deals in, and the lists behind the wide ones.</summary>
    public Values Values { get; } = new Values();

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
        entity.Store = Values;
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
