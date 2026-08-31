using Reactor.World;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

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

    /// <summary>
    /// The world's value store, set when this joins one.
    /// </summary>
    /// <remarks>
    /// A value carries no kind and text lives in a list, so making or reading one needs the store the
    /// rest of the world uses. Set on joining rather than taken in the constructor, so a new sort of
    /// entity does not have to thread it through every subclass.
    /// </remarks>
    internal Values Store { get; set; } = new Values();

    protected void Set(string propertyId, bool value) => Values[propertyId] = EntityValue.Of(value);

    protected void Set(string propertyId, double value) => Values[propertyId] = EntityValue.Of(value);

    /// <summary>Written into the slot this reading already owns, so a per-tick read takes no new one.</summary>
    protected void Set(string propertyId, string value) =>
        Values[propertyId] = Store.Text(GetValue(propertyId), value);

    protected bool Flag(string propertyId) => Store.Flag(KindOf(propertyId), GetValue(propertyId));

    ValueKind KindOf(string propertyId)
    {
        var property = Kind.FindProperty(propertyId);
        return property == null ? ValueKind.Flag : property.Kind;
    }

    public virtual void Invoke(string actionId) { }

    public virtual void Tick() { }
}
