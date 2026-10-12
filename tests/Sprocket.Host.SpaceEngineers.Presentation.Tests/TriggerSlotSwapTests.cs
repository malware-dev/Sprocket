using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Trading the one block a trigger watches for another of the same kind keeps the slot, as a step's trade does: its name,
/// its description and what the other installations filled it with.
/// </summary>
public class TriggerSlotSwapTests
{
    /// <summary>The inner door traded for the outer one leaves the trigger's slot as it was named and described, holding the outer door.</summary>
    [Fact]
    public void ADoorTradedForADoorKeepsItsSlot()
    {
        var sim = OnTheInnerDoor(out var system, out var slot);
        slot.Annotation = "The door on the ship's side.";
        var other = system.AddInstance();
        other.Fill(slot.Id, new[] { "elsewhere" });

        Repick(sim, system, "Airlock Outer Door");

        var trigger = (MomentTrigger)system.Triggers[0];
        Assert.Equal(slot.Id, Assert.Single(trigger.Sources));
        Assert.Same(slot, system.FindPart(slot.Id));
        Assert.Equal("The door on the ship's side.", slot.Annotation);
        Assert.Equal(new[] { "elsewhere" }, other.FilledWith(slot.Id));
        Assert.Equal(new[] { "Airlock Outer Door" }, sim.Editor.Seen(system, trigger.Sources).Select(Shown(sim)));
    }

    /// <summary>Taking the trade back gives the slot its old block back.</summary>
    [Fact]
    public void UndoingTheTradeGivesTheSlotItsBlockBack()
    {
        var sim = OnTheInnerDoor(out var system, out var slot);
        Repick(sim, system, "Airlock Outer Door");

        sim.Editor.Screen.Undo!.Execute();
        sim.Tick();

        var trigger = (MomentTrigger)system.Triggers[0];
        Assert.Equal(slot.Id, Assert.Single(trigger.Sources));
        Assert.Equal(new[] { "Airlock Inner Door" }, sim.Editor.Seen(system, trigger.Sources).Select(Shown(sim)));
    }

    /// <summary>A block of another kind than the slot expects is a slot of its own.</summary>
    [Fact]
    public void ABlockOfAnotherKindMakesASlotOfItsOwn()
    {
        var sim = OnTheInnerDoor(out var system, out var slot);

        Repick(sim, system, "Hangar Door");

        Assert.NotEqual(slot.Id, Assert.Single(((MomentTrigger)system.Triggers[0]).Sources));
    }

    /// <summary>A logic system run when the inner door finishes closing, with that trigger's slot.</summary>
    static Simulation OnTheInnerDoor(out AutomationSystem system, out Part slot)
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewSystem("Airlock");
        sim.Editor.PressStart(sim.Texts.SystemWhen);
        sim.Editor.PressBlock("Airlock Inner Door");
        sim.Editor.PressChoice("finishes closing");
        sim.Editor.PressPrimary();
        sim.Editor.Back();
        sim.Tick();
        system = sim.Editor.Program.Systems[0];
        slot = system.FindPart(((MomentTrigger)system.Triggers[0]).Sources[0])!;
        return sim;
    }

    /// <summary>Opens the trigger's blocks and picks <paramref name="door" /> in place of what it watches.</summary>
    static void Repick(Simulation sim, AutomationSystem system, string door)
    {
        sim.Editor.ChangeTrigger(system, system.Triggers[0], Opening.Blocks, false);
        sim.Tick();
        sim.Tick();
        sim.Editor.PressBlock(door);
        sim.Tick();
        sim.Editor.PressChoice("finishes closing");
        sim.Editor.PressPrimary();
        sim.Tick();
        sim.Tick();
    }

    /// <summary>What a declared block is called on the construct.</summary>
    static Func<string, string> Shown(Simulation sim) =>
        declared => sim.Editor.World.Entities.First(e => sim.Editor.Program.Declare(sim.Editor.World, e).Name == declared).Name;
}
