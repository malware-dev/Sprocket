using Reactor.World;

namespace Reactor.Sim.Machine;

/// <summary>
/// The entity vocabulary for the simulated ship.
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
    public static readonly EntityKind Door = new EntityKind("door", "Door")
        .WithIcon("door")
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

    public static readonly EntityKind Light = new EntityKind("light", "Light")
        .WithIcon("light")
        .WithProperty("on", "switched on")
        .WithEvent("switched-on", "on", Edge.Rising, "comes on")
        .WithEvent("switched-off", "on", Edge.Falling, "goes out")
        .WithState("is-on", "on", true, "is on")
        .WithState("is-off", "on", false, "is off")
        .WithAction("on", "turn on", "is-on")
        .WithAction("off", "turn off", "is-off");

    public static readonly EntityKind Sensor = new EntityKind("sensor", "Sensor")
        .WithIcon("sensor")
        .WithProperty("detecting", "detecting something")
        .WithEvent("detects", "detecting", Edge.Rising, "detects something")
        .WithEvent("clears", "detecting", Edge.Falling, "stops detecting")
        .WithState("is-detecting", "detecting", true, "is detecting something")
        .WithState("is-clear", "detecting", false, "is clear");

    // No state over "pressed" on purpose. It is true for a single tick, so "only if the
    // button is pressed" would be a condition that is essentially never met - a sentence
    // that reads perfectly and never works.
    public static readonly EntityKind Button = new EntityKind("button", "Button")
        .WithIcon("button")
        .WithProperty("pressed", "pressed")
        .WithEvent("pressed", "pressed", Edge.Rising, "is pressed");

    /// <summary>
    /// The kind that shows why properties are worth having: it reads a number, and "low" is
    /// a relation against it rather than something the tank decided in advance.
    /// </summary>
    public static readonly EntityKind Tank = new EntityKind("tank", "Hydrogen Tank")
        .WithIcon("tank")
        .WithProperty("fill", ValueKind.Real, "fill level")
        .WithState("is-low", "fill", Relation.Below, EntityValue.Of(0.25), "is low")
        .WithState("is-fine", "fill", Relation.AtLeast, EntityValue.Of(0.25), "has plenty");

    public static readonly EntityKind Vent = new EntityKind("vent", "Air Vent")
        .WithIcon("vent")
        .WithProperty("air", "has air")
        .WithProperty("empty", "is empty")
        .WithEvent("filled", "air", Edge.Rising, "finishes filling")
        .WithEvent("emptied", "empty", Edge.Rising, "finishes emptying")
        .WithState("has-air", "air", true, "has air")
        .WithState("is-empty", "empty", true, "is empty")
        .WithAction("pressurize", "pressurize", "has-air")
        .WithAction("depressurize", "depressurize", "is-empty");
}
