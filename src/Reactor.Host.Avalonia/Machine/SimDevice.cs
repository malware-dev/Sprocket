using Reactor.World;

namespace Reactor.Host.Machine;

/// <summary>
/// A device on the simulated ship.
/// </summary>
/// <remarks>
/// These behave like the real thing in the one way that matters for the UX
/// question: they take time. A door that snapped open instantly would hide every
/// interesting problem - waiting, re-triggering, and sequences overlapping each
/// other are exactly what people get wrong, and they only show up when things have
/// duration.
/// </remarks>
abstract class SimDevice(long id, string name, DeviceKind kind) : IDevice
{
    protected readonly Dictionary<string, bool> Signals = [];

    public long Id { get; } = id;
    public string Name { get; set; } = name;
    public DeviceKind Kind { get; } = kind;

    /// <summary>Live state for the device list, in the user's words.</summary>
    public abstract string Status { get; }

    /// <summary>Whether this device is doing something interesting right now.</summary>
    public virtual bool IsActive => false;

    public bool GetSignal(string signalId) => Signals.TryGetValue(signalId, out var value) && value;

    protected void Set(string signalId, bool value) => Signals[signalId] = value;

    public virtual void Invoke(string actionId) { }

    public virtual void Tick() { }
}

sealed class SimDoor(long id, string name) : SimDevice(id, name, Kinds.Door)
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

sealed class SimLight(long id, string name) : SimDevice(id, name, Kinds.Light)
{
    public override string Status => GetSignal("on") ? "on" : "off";

    public override bool IsActive => GetSignal("on");

    public override void Invoke(string actionId)
    {
        if (actionId == "on")
            Set("on", true);
        else if (actionId == "off")
            Set("on", false);
    }
}

/// <summary>A sensor the tester trips by hand, standing in for a ship flying up.</summary>
sealed class SimSensor(long id, string name) : SimDevice(id, name, Kinds.Sensor)
{
    public override string Status => GetSignal("detecting") ? "detecting" : "clear";

    public override bool IsActive => GetSignal("detecting");

    public void SetDetecting(bool detecting) => Set("detecting", detecting);
}

/// <summary>
/// A button. Its signal is true for a single tick, which is what makes "is pressed"
/// a moment rather than a state.
/// </summary>
sealed class SimButton(long id, string name) : SimDevice(id, name, Kinds.Button)
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

sealed class SimTank(long id, string name) : SimDevice(id, name, Kinds.Tank)
{
    const float LowThreshold = 0.25f;

    float _level = 1f;

    public bool Draining { get; set; }

    public override string Status => $"{(int)(_level * 100)}%" + (GetSignal("low") ? " - low" : "");

    public override bool IsActive => GetSignal("low");

    public override void Tick()
    {
        _level += Draining ? -0.002f : 0.0008f;
        _level = Math.Clamp(_level, 0f, 1f);
        Set("low", _level < LowThreshold);
    }
}

sealed class SimVent(long id, string name) : SimDevice(id, name, Kinds.Vent)
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
        if (actionId == "pump-in")
            _pressurizing = true;
        else if (actionId == "pump-out")
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
