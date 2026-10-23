using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Trading the blocks a trigger watches, where nothing else uses its slot, changes the slot, as a step's trade does: it keeps
/// its name, its description and what the other installations filled it with, and its type follows the blocks.
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

    /// <summary>A block of another kind changes the slot's type to follow it once asked, and the slot keeps its name.</summary>
    [Fact]
    public void ABlockOfAnotherKindChangesTheSlotsType()
    {
        var sim = OnTheInnerDoor(out var system, out var slot);
        var was = slot.Expects;

        Repick(sim, system, "Hangar Door");
        Assert.True(sim.Editor.Screen.HasQuestion, "a type that takes less was not asked about");
        sim.Editor.PressAnswer(sim.Editor.Texts.SlotNarrowYes);
        sim.Tick();
        sim.Tick();

        Assert.Equal(slot.Id, Assert.Single(((MomentTrigger)system.Triggers[0]).Sources));
        Assert.NotEqual(was, slot.Expects);
        Assert.Equal(new[] { "Hangar Door" }, sim.Editor.Seen(system, new[] { slot.Id }).Select(Shown(sim)));
    }

    /// <summary>
    /// A button trigger whose signal slot another trigger uses too asks before it changes the slot; given a slot of its own,
    /// it is answered by the button picked, and the other trigger keeps the slot and its button.
    /// </summary>
    [Fact]
    public void ASharedSignalSlotAsks()
    {
        var sim = Simulation.Fresh();
        sim.Machine.Panel(30, "Port Button");
        sim.Tick();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.Back();
        sim.Tick();
        var system = sim.Editor.Program.Systems[0];
        var part = ((SignalTrigger)system.Triggers[0]).Part;
        system.AddTrigger(Automation.Copied(system.Triggers[0], part));
        sim.Editor.Program.Edited();
        sim.Tick();

        sim.Editor.Starts.ChangeTrigger(system, system.Triggers[0], OpeningColumn.Blocks, false);
        sim.Tick();
        sim.Tick();
        sim.Editor.PressBlock("Port Button");
        sim.Editor.PressChoice("button 1 is pressed");
        sim.Editor.PressPrimary();
        sim.Tick();
        Assert.True(sim.Editor.Screen.HasQuestion, "nothing asked about a signal slot another trigger uses");
        sim.Editor.PressAnswer(sim.Editor.Texts.SlotSharedOwn);
        sim.Tick();
        sim.Tick();

        var own = ((SignalTrigger)system.Triggers[0]).Part;
        Assert.NotEqual(part, own);
        Assert.Equal(new[] { "Port Button" }, sim.Editor.Seen(system, new[] { own }).Select(Shown(sim)));
        Assert.Equal(part, ((SignalTrigger)system.Triggers[1]).Part);
        Assert.Equal(new[] { "Airlock Button" }, sim.Editor.Seen(system, new[] { part }).Select(Shown(sim)));
    }

    /// <summary>A logic system run when the inner door finishes closing, with that trigger's slot.</summary>
    static Simulation OnTheInnerDoor(out AutomationSystem system, out Part slot)
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewSystem("Airlock");
        sim.Editor.PressStart(SentenceText.WhenWord);
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
        sim.Editor.Starts.ChangeTrigger(system, system.Triggers[0], OpeningColumn.Blocks, false);
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
