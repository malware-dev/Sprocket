using Reactor.World;

namespace Reactor.Tests;

/// <summary>
/// A machine that takes time to do things.
/// </summary>
/// <remarks>
/// Doors travel and vents cycle, because instant devices hide every problem worth
/// finding. Gates, overlapping sequences and interlocks only mean anything once
/// things have duration.
/// </remarks>
sealed class TestWorld : IWorld
{
    readonly List<TestDevice> _devices = [];

    public List<string> Log { get; } = [];

    public IReadOnlyList<IDevice> Devices => _devices;

    public T Add<T>(T device) where T : TestDevice
    {
        _devices.Add(device);
        return device;
    }

    public void Remove(long id) => _devices.RemoveAll(d => d.Id == id);

    public IDevice? FindDevice(long deviceId) => _devices.FirstOrDefault(d => d.Id == deviceId);

    public void Invoke(IDevice device, string actionId)
    {
        Log.Add($"{device.Name}:{actionId}");
        ((TestDevice)device).Invoke(actionId);
    }

    public void Tick()
    {
        foreach (var device in _devices)
            device.Tick();
    }
}

abstract class TestDevice(long id, string name, DeviceKind kind) : IDevice
{
    protected readonly Dictionary<string, bool> Signals = [];

    public long Id { get; } = id;
    public string Name { get; } = name;
    public DeviceKind Kind { get; } = kind;

    public abstract string Status { get; }
    public virtual bool IsActive => false;

    public bool GetSignal(string signalId) => Signals.TryGetValue(signalId, out var v) && v;

    protected void Set(string signalId, bool value) => Signals[signalId] = value;

    public virtual void Invoke(string actionId) { }

    public virtual void Tick() { }
}

sealed class TestDoor(long id, string name) : TestDevice(id, name, Kinds.Door)
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

sealed class TestVent(long id, string name) : TestDevice(id, name, Kinds.Vent)
{
    const int Cycle = 10;

    int _air = Cycle;
    bool _filling = true;

    public override string Status => _air >= Cycle ? "has air" : _air <= 0 ? "empty" : "cycling";

    public override void Invoke(string actionId)
    {
        if (actionId == "pump-in")
            _filling = true;
        else if (actionId == "pump-out")
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
sealed class TestLamp(long id, string name) : TestDevice(id, name, Kinds.Lamp)
{
    public override string Status => GetSignal("on") ? "on" : "off";

    public override void Invoke(string actionId)
    {
        if (actionId == "on")
            Set("on", true);
        else if (actionId == "off")
            Set("on", false);
    }
}

sealed class TestButton(long id, string name) : TestDevice(id, name, Kinds.Button)
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
    public static readonly DeviceKind Door = new DeviceKind("door", "Door")
        .WithEvent("opened", "open", SignalEdge.Rising, "finishes opening")
        .WithEvent("shut", "shut", SignalEdge.Rising, "finishes closing")
        .WithState("is-open", "open", true, "is open")
        .WithState("is-shut", "shut", true, "is shut")
        .WithState("is-locked", "locked", true, "is locked")
        .WithState("is-unlocked", "locked", false, "is unlocked")
        .WithAction("open", "open", "is-open")
        .WithAction("close", "close", "is-shut")
        .WithAction("lock", "lock", "is-locked")
        .WithAction("unlock", "unlock", "is-unlocked");

    public static readonly DeviceKind Vent = new DeviceKind("vent", "Air Vent")
        .WithEvent("emptied", "empty", SignalEdge.Rising, "finishes emptying")
        .WithEvent("filled", "air", SignalEdge.Rising, "finishes filling")
        .WithState("has-air", "air", true, "has air")
        .WithState("is-empty", "empty", true, "is empty")
        .WithAction("pump-out", "pump the air out", "is-empty")
        .WithAction("pump-in", "pump the air back in", "has-air");

    public static readonly DeviceKind Lamp = new DeviceKind("lamp", "Lamp")
        .WithState("is-on", "on", true, "is on")
        .WithAction("on", "turn on")
        .WithAction("off", "turn off");

    public static readonly DeviceKind Button = new DeviceKind("button", "Button")
        .WithEvent("pressed", "pressed", SignalEdge.Rising, "is pressed");
}
