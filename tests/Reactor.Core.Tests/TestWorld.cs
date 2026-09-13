using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Tests;

/// <summary>
/// A system that takes time to do things.
/// </summary>
/// <remarks>
/// Doors travel and vents cycle, because instant entities hide every problem worth
/// finding. Gates, overlapping sequences and interlocks only mean anything once
/// things have duration.
/// </remarks>
sealed class TestWorld : IWorld
{
    readonly List<TestEntity> _entities = [];

    public List<string> Log { get; } = [];

    /// <summary>The values this world deals in, and the lists behind the wide ones.</summary>
    public Values Values { get; } = new Values();

    public int Revision { get; private set; }

    /// <summary>How many times anything has been matched, so a test can assert it stopped.</summary>
    public int Matches { get; private set; }

    public IReadOnlyList<IEntity> Entities => _entities;

    public EntityNoun Noun { get; } = new EntityNoun("block", "blocks", "Blocks");

    public string Name { get; private set; } = "Hangar Bay Alpha";

    public bool CanRename => true;

    public void Rename(string name) => Name = name;

    public T Add<T>(T entity) where T : TestEntity
    {
        entity.Store = Values;
        Revision++;
        _entities.Add(entity);
        return entity;
    }

    public void Remove(long id)
    {
        Revision++;
        _entities.RemoveAll(d => d.Id == id);
    }

    public IEntity? FindEntity(long entityId) => _entities.FirstOrDefault(d => d.Id == entityId);

    /// <summary>
    /// Sequences nothing can start. Told, so a test can make one unreachable without a host.
    /// </summary>
    public readonly HashSet<int> Unstartable = [];

    public bool CanStart(Sequence sequence) => !Unstartable.Contains(sequence.Id);

    /// <summary>An id and a name, in that order, which is enough to tell bound from adrift.</summary>
    public void Identify(IEntity entity, List<EntityKey> keys)
    {
        keys.Clear();
        keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(entity.Id)));
        keys.Add(new EntityKey(ValueKind.Text, Values.Text(entity.Name)));
    }

    public IEntity? Match(IList<EntityKey> keys, out int matched)
    {
        Matches++;

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
        return name.Length == 0 ? null : _entities.FirstOrDefault(d => d.Name == name);
    }

    public void Invoke(IEntity entity, string actionId)
    {
        Log.Add($"{entity.Name}:{actionId}");
        ((TestEntity)entity).Invoke(actionId);
    }

    public void Tick()
    {
        foreach (var entity in _entities)
            entity.Tick();
    }
}

abstract class TestEntity(long id, string name, EntityKind kind) : IEntity
{
    protected readonly Dictionary<string, EntityValue> Values = [];

    public long Id { get; } = id;
    public string Name { get; } = name;
    public EntityKind Kind { get; } = kind;

    public abstract string Status { get; }
    public virtual bool IsActive => false;

    public EntityValue GetValue(string propertyId) =>
        Values.TryGetValue(propertyId, out var v) ? v : default;

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

sealed class TestDoor(long id, string name) : TestEntity(id, name, Kinds.Door)
{
    const int Travel = 10;

    int _progress;
    bool _opening;
    bool _locked;

    public override string Status =>
        _progress >= Travel ? "open" : _progress <= 0 ? "shut" : _opening ? "opening" : "closing";

    /// <summary>Forces the door open without going through an action, as a player would by hand.</summary>
    public void ForceOpen()
    {
        _progress = Travel;
        _opening = true;
        Publish();
    }

    public override void Invoke(string actionId)
    {
        switch (actionId)
        {
            // A locked door ignores being told to open, which is what makes locking
            // it a real safeguard rather than a note to self.
            case "open" when !_locked:
                _opening = true;
                break;
            case "close":
                _opening = false;
                break;
            case "lock":
                _locked = true;
                Set("locked", true);
                break;
            case "unlock":
                _locked = false;
                Set("locked", false);
                break;
        }
    }

    public override void Tick()
    {
        // Locking a door is disabling the block, and a disabled door stops where it
        // is. Lock one while it is open and it stays open - which is why closing it
        // has to actually finish before locking, and why doing the two at once is a
        // trap rather than a shortcut.
        if (_locked)
        {
            Publish();
            return;
        }

        if (_opening && _progress < Travel)
            _progress++;
        else if (!_opening && _progress > 0)
            _progress--;

        Publish();
    }

    void Publish()
    {
        Set("open", _progress >= Travel);
        Set("shut", _progress <= 0);
    }
}

sealed class TestVent(long id, string name) : TestEntity(id, name, Kinds.Vent)
{
    const int Cycle = 10;

    int _air = Cycle;
    bool _filling = true;

    public override string Status => _air >= Cycle ? "has air" : _air <= 0 ? "empty" : "cycling";

    public override void Invoke(string actionId)
    {
        if (actionId == "pressurize")
            _filling = true;
        else if (actionId == "depressurize")
            _filling = false;
    }

    public override void Tick()
    {
        if (_filling && _air < Cycle)
            _air++;
        else if (!_filling && _air > 0)
            _air--;

        Set("air", _air >= Cycle);
        Set("empty", _air <= 0);
    }
}

/// <summary>A lamp: its actions reach for no particular state, so they are instant.</summary>
sealed class TestLamp(long id, string name) : TestEntity(id, name, Kinds.Lamp)
{
    public override string Status => Flag("on") ? "on" : "off";

    public override void Invoke(string actionId)
    {
        if (actionId == "on")
            Set("on", true);
        else if (actionId == "off")
            Set("on", false);
    }
}

sealed class TestTank(long id, string name, Values values) : TestEntity(id, name, Kinds.Tank(values))
{
    public override string Status => $"{(int)(Store.AsNumber(ValueKind.Real, GetValue("fill")) * 100)}%";

    public void SetFill(double fill) => Set("fill", fill);

    public void SetLabel(string label) => Set("label", label);
}

sealed class TestButton(long id, string name) : TestEntity(id, name, Kinds.Button)
{
    bool _queued;

    public override string Status => "ready";

    public void Press() => _queued = true;

    public override void Tick()
    {
        Set("pressed", _queued);
        _queued = false;
    }
}

/// <summary>
/// The vocabulary a host declares. Note every kind states both its moments and its
/// situations, and every action says what state it is reaching for.
/// </summary>
static class Kinds
{
    public static readonly EntityKind Door = new EntityKind("door", "Door")
        .WithProperty("open", "open")
        .WithProperty("shut", "shut")
        .WithProperty("locked", "locked")
        .WithEvent("opened", "open", Edge.Rising, "finishes opening")
        .WithEvent("shut", "shut", Edge.Rising, "finishes closing")
        .WithState("is-open", "open", true, "is open")
        .WithState("is-shut", "shut", true, "is shut")
        .WithState("is-locked", "locked", true, "is locked")
        .WithState("is-unlocked", "locked", false, "is unlocked")
        .WithAction("open", "open", "is-open")
        .WithAction("close", "close", "is-shut")
        .WithAction("lock", "lock", "is-locked")
        .WithAction("unlock", "unlock", "is-unlocked");

    public static readonly EntityKind Vent = new EntityKind("vent", "Air Vent")
        .WithProperty("air", "has air")
        .WithProperty("empty", "is empty")
        .WithEvent("emptied", "empty", Edge.Rising, "finishes emptying")
        .WithEvent("filled", "air", Edge.Rising, "finishes filling")
        .WithState("has-air", "air", true, "has air")
        .WithState("is-empty", "empty", true, "is empty")
        .WithAction("depressurize", "depressurize", "is-empty")
        .WithAction("pressurize", "pressurize", "has-air");

    public static readonly EntityKind Lamp = new EntityKind("lamp", "Lamp")
        .WithProperty("on", "switched on")
        .WithState("is-on", "on", true, "is on")
        .WithAction("on", "turn on")
        .WithAction("off", "turn off");

    public static readonly EntityKind Button = new EntityKind("button", "Button")
        .WithProperty("pressed", "pressed")
        .WithEvent("pressed", "pressed", Edge.Rising, "is pressed");

    /// <summary>
    /// Reads a number, and says "low" by comparing against it. Also carries a text operand.
    /// </summary>
    /// <remarks>
    /// A method rather than a field, unlike its neighbours, and that is the design showing through: a
    /// text operand is a slot in a store, so a declaration carrying one has to be built against the
    /// store it will later be compared in. Only declarations whose operands fit in eight bytes can be
    /// static.
    /// </remarks>
    public static EntityKind Tank(Values values) => new EntityKind("tank", "Tank")
        .WithProperty("fill", ValueKind.Real, "fill level")
        .WithState("is-low", "fill", Relation.Below, EntityValue.Of(0.25), "is low")
        .WithState("is-fine", "fill", Relation.AtLeast, EntityValue.Of(0.25), "has plenty")
        .WithProperty("label", ValueKind.Text, "label")
        .WithState("is-spare", "label", Relation.Is, values.Text("spare"), "is the spare");
}
