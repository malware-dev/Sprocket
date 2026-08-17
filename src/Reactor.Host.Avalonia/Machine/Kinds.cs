using Reactor.World;

namespace Reactor.Host.Machine;

/// <summary>
/// The device vocabulary for the simulated ship.
/// </summary>
/// <remarks>
/// Core knows nothing about doors or lights. In Space Engineers this catalogue is
/// built from real block types; here it is written by hand, which is the whole point
/// of keeping it on the host side.
///
/// Every kind declares three things, and the split matters. <b>Events</b> are
/// moments and only ever start a sequence. <b>States</b> are situations and only
/// ever gate or guard. <b>Actions</b> name the state they are reaching for, which is
/// what tells the runtime when a step has finished - so "close the inner door"
/// finishes when the door is actually shut, without anybody writing that down.
///
/// Wording is written to be read out loud as part of a sentence: "finishes opening",
/// "is shut", "close" - never "OnDoorClosed".
/// </remarks>
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

    public static readonly DeviceKind Light = new DeviceKind("light", "Light")
        .WithEvent("switched-on", "on", SignalEdge.Rising, "comes on")
        .WithEvent("switched-off", "on", SignalEdge.Falling, "goes out")
        .WithState("is-on", "on", true, "is on")
        .WithState("is-off", "on", false, "is off")
        .WithAction("on", "turn on", "is-on")
        .WithAction("off", "turn off", "is-off");

    public static readonly DeviceKind Sensor = new DeviceKind("sensor", "Sensor")
        .WithEvent("detects", "detecting", SignalEdge.Rising, "detects something")
        .WithEvent("clears", "detecting", SignalEdge.Falling, "stops detecting")
        .WithState("is-detecting", "detecting", true, "is detecting something")
        .WithState("is-clear", "detecting", false, "is clear");

    public static readonly DeviceKind Button = new DeviceKind("button", "Button")
        .WithEvent("pressed", "pressed", SignalEdge.Rising, "is pressed");

    public static readonly DeviceKind Tank = new DeviceKind("tank", "Hydrogen Tank")
        .WithEvent("runs-low", "low", SignalEdge.Rising, "runs low")
        .WithEvent("recovers", "low", SignalEdge.Falling, "is topped up")
        .WithState("is-low", "low", true, "is low")
        .WithState("is-fine", "low", false, "has plenty");

    public static readonly DeviceKind Vent = new DeviceKind("vent", "Air Vent")
        .WithEvent("filled", "air", SignalEdge.Rising, "finishes filling")
        .WithEvent("emptied", "empty", SignalEdge.Rising, "finishes emptying")
        .WithState("has-air", "air", true, "has air")
        .WithState("is-empty", "empty", true, "is empty")
        .WithAction("pump-in", "pump the air back in", "has-air")
        .WithAction("pump-out", "pump the air out", "is-empty");
}
