using Reactor.World;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

namespace Reactor.Sim.Machine;
/// <summary>
/// An entity on the simulated ship.
/// </summary>
/// <remarks>
/// These behave like the real thing in the one way that matters for the UX
/// question: they take time. A door that snapped open instantly would hide every
/// interesting problem - waiting, re-triggering, and sequences overlapping each
/// other are exactly what people get wrong, and they only show up when things have
/// duration.
/// </remarks>
abstract class SimEntity(long id, string name, EntityKind kind) : IEntity
{
    protected readonly Dictionary<string, EntityValue> Values = [];

    public long Id { get; } = id;
    public string Name { get; set; } = name;
    public EntityKind Kind { get; } = kind;

    /// <summary>Live state for the entity list, in the user's words.</summary>
    public abstract string Status { get; }

    /// <summary>Whether this entity is doing something interesting right now.</summary>
    public virtual bool IsActive => false;

    public EntityValue GetValue(string propertyId) =>
        Values.TryGetValue(propertyId, out var value) ? value : default;

    protected void Set(string propertyId, bool value) => Values[propertyId] = EntityValue.Of(value);

    protected void Set(string propertyId, double value) => Values[propertyId] = EntityValue.Of(value);

    protected void Set(string propertyId, string value) => Values[propertyId] = EntityValue.Of(value);

    protected bool Flag(string propertyId) => GetValue(propertyId).Flag;

    public virtual void Invoke(string actionId) { }

    public virtual void Tick() { }
}
