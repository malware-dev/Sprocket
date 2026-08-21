using Reactor.World;

namespace Reactor.Host.Machine;

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

sealed class SimDoor(long id, string name) : SimEntity(id, name, Kinds.Door)
{
    const int TravelTicks = 45;

    int _progress;
    bool _opening;
    bool _locked;

    public override string Status => _locked
        ? _progress >= TravelTicks ? "locked open" : "locked"
        : Moving
            ? _opening ? "opening…" : "closing…"
            : _progress >= TravelTicks ? "open" : "shut";

    public override bool IsActive => Moving;

    bool Moving => _progress > 0 && _progress < TravelTicks;

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
        // Locking is disabling, and a disabled door stops where it is. Lock one
        // mid-travel and it stays there, which is why closing has to actually finish
        // before locking.
        if (!_locked)
        {
            if (_opening && _progress < TravelTicks)
                _progress++;
            else if (!_opening && _progress > 0)
                _progress--;
        }

        Set("open", _progress >= TravelTicks);
        Set("shut", _progress <= 0);
        Set("moving", Moving);
    }
}

sealed class SimLight(long id, string name) : SimEntity(id, name, Kinds.Light)
{
    public override string Status => Flag("on") ? "on" : "off";

    public override bool IsActive => Flag("on");

    public override void Invoke(string actionId)
    {
        if (actionId == "on")
            Set("on", true);
        else if (actionId == "off")
            Set("on", false);
    }
}

/// <summary>A sensor the tester trips by hand, standing in for a ship flying up.</summary>
sealed class SimSensor(long id, string name) : SimEntity(id, name, Kinds.Sensor)
{
    public override string Status => Flag("detecting") ? "detecting" : "clear";

    public override bool IsActive => Flag("detecting");

    public void SetDetecting(bool detecting) => Set("detecting", detecting);
}

/// <summary>
/// A button. Its signal is true for a single tick, which is what makes "is pressed"
/// a moment rather than a state.
/// </summary>
sealed class SimButton(long id, string name) : SimEntity(id, name, Kinds.Button)
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

sealed class SimTank(long id, string name) : SimEntity(id, name, Kinds.Tank)
{
    float _level = 1f;

    public bool Draining { get; set; }

    public override string Status => $"{(int)(_level * 100)}%" + (Flag("low") ? " - low" : "");

    public override bool IsActive => Flag("low");

    public override void Tick()
    {
        _level += Draining ? -0.002f : 0.0008f;
        _level = Math.Clamp(_level, 0f, 1f);

        // The reading core gets is the level itself. "Low" is a relation the kind declares
        // against it, not something decided here and flattened to a bool on the way out -
        // which is what used to happen, and it threw the number away before anything could
        // ask a different question of it.
        Set("fill", _level);
    }
}

sealed class SimVent(long id, string name) : SimEntity(id, name, Kinds.Vent)
{
    const int CycleTicks = 90;

    int _progress = CycleTicks;
    bool _pressurizing = true;

    public override string Status => Cycling
        ? _pressurizing ? "filling…" : "emptying…"
        : _progress >= CycleTicks ? "has air" : "empty";

    public override bool IsActive => Cycling;

    bool Cycling => _progress > 0 && _progress < CycleTicks;

    public override void Invoke(string actionId)
    {
        if (actionId == "pressurize")
            _pressurizing = true;
        else if (actionId == "depressurize")
            _pressurizing = false;
    }

    public override void Tick()
    {
        if (_pressurizing && _progress < CycleTicks)
            _progress++;
        else if (!_pressurizing && _progress > 0)
            _progress--;

        Set("air", _progress >= CycleTicks);
        Set("empty", _progress <= 0);
    }
}
