using Reactor.Sequences;
using Reactor.World;

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

    /// <summary>Bumped where the set changes, which here is adding and removing.</summary>
    public int Revision { get; private set; }

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

    /// <summary>Nothing here has to be arranged outside the program, so anything can start.</summary>
    public bool CanStart(Sequence sequence) => true;

    /// <summary>
    /// An id and a name, in that order.
    /// </summary>
    /// <remarks>
    /// The id is exact and dies with a blueprint; the name survives one and is the thing a player
    /// edits casually. Grid position belongs between them, because a pasted copy keeps every
    /// position and changes every id - it is not here yet because there is no value kind wide
    /// enough to write one down. See "Still to prove" in <c>docs/Script.md</c>.
    /// </remarks>
    public void Identify(IEntity entity, List<EntityKey> keys)
    {
        keys.Clear();
        keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(entity.Id)));
        keys.Add(new EntityKey(ValueKind.Text, Values.Text(entity.Name)));
    }

    public IEntity? Match(IList<EntityKey> keys, out int matched)
    {
        for (var i = 0; i < keys.Count; i++)
        {
            var found = Matching(keys[i]);
            if (found == null)
                continue;

            matched = i;
            return found;
        }

        matched = -1;
        return null;
    }

    IEntity? Matching(EntityKey key)
    {
        if (key.Kind == ValueKind.Integer)
        {
            Values.TryInteger(key.Kind, key.Value, out var id);
            return FindEntity(id);
        }

        if (key.Kind != ValueKind.Text)
            return null;

        Values.TryText(key.Kind, key.Value, out var name);
        return name.Length == 0 ? null : _entities.FirstOrDefault(e => e.Name == name);
    }

    public void Invoke(IEntity entity, string actionId) => ((SimEntity)entity).Invoke(actionId);

    public T Add<T>(T entity) where T : SimEntity
    {
        entity.Store = Values;
        Revision++;
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
        Revision++;
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
