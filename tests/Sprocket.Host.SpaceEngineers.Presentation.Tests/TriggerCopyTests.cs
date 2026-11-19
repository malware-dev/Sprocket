using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A trigger is copied from its row, whole, except a button trigger's copy, which starts from an empty slot of its own and says why.</summary>
public class TriggerCopyTests
{
    /// <summary>A button trigger's copy runs the same sequence from a new, empty signal slot expecting what the original's did, and says why.</summary>
    [Fact]
    public void AButtonTriggerIsCopiedWithASlotOfItsOwn()
    {
        var sim = Built(out var system);
        var original = (SignalTrigger)system.Triggers[0];

        Copy(sim, 0);

        var copy = Assert.IsType<SignalTrigger>(system.Triggers[1]);
        Assert.NotEqual(original.Part, copy.Part);
        Assert.Equal(original.Runs, copy.Runs);
        Assert.NotSame(original.Does, copy.Does);

        var slot = system.FindPart(copy.Part)!;
        Assert.True(slot.IsSignal);
        Assert.Equal("", slot.Expects);
        Assert.True(system.First!.FilledWith(slot.Id) is not { Count: > 0 }, "the copy's slot came filled");

        var line = sim.Editor.Triggers()[1];
        Assert.Equal(sim.Texts.TriggerCopiedButton, line.Problem);
        Assert.Equal(NoticeLevel.Info, line.Level);
    }

    /// <summary>A trigger on a block's signal is copied whole, reading the same blocks, and says nothing about it.</summary>
    [Fact]
    public void ATriggerOnBlocksIsCopiedWhole()
    {
        var sim = OnADoor(out var system);
        var original = (MomentTrigger)system.Triggers[0];
        var slots = system.Parts.Count;

        Copy(sim, 0);

        var copy = (MomentTrigger)system.Triggers[1];
        Assert.Equal(original.EventId, copy.EventId);
        Assert.Equal(original.Sources, copy.Sources);
        Assert.Equal(original.Runs, copy.Runs);
        Assert.Equal(slots, system.Parts.Count);
        Assert.False(sim.Editor.Starts.Copied(copy));
        Assert.NotEqual(NoticeLevel.Info, sim.Editor.Triggers()[1].Level);
    }

    /// <summary>A clock is copied whole, and its copy says nothing about it.</summary>
    [Fact]
    public void AClockIsCopiedWhole()
    {
        var sim = Built(out var system);
        system.AddTrigger(new EveryTrigger(600) { Does = new Run(system.Sequences[0].Number, false) });
        sim.Editor.Program.Rewire();
        sim.Tick();
        sim.Tick();
        var slots = system.Parts.Count;

        Copy(sim, 1);

        var copy = Assert.IsType<EveryTrigger>(system.Triggers[2]);
        Assert.Equal(600, copy.IntervalTicks);
        Assert.Equal(slots, system.Parts.Count);
        Assert.Equal(NoticeLevel.Problem, sim.Editor.Triggers()[2].Level);
    }

    /// <summary>One undo takes the copy back with the slot made for it, and redo makes both again.</summary>
    [Fact]
    public void UndoTakesTheCopyAndItsSlotBack()
    {
        var sim = Built(out var system);
        var slots = system.Parts.Count;

        Copy(sim, 0);
        var made = ((SignalTrigger)system.Triggers[1]).Part;

        sim.Editor.Screen.Undo!.Execute();
        sim.Tick();
        Assert.Single(system.Triggers);
        Assert.Equal(slots, system.Parts.Count);
        Assert.Null(system.FindPart(made));

        sim.Editor.Screen.Redo!.Execute();
        sim.Tick();
        Assert.Equal(2, system.Triggers.Count);
        Assert.NotNull(system.FindPart(made));
    }

    /// <summary>The copy's line stops saying why once its start is first picked.</summary>
    [Fact]
    public void TheNoteGoesWithTheFirstPick()
    {
        var sim = Built(out var system);
        Copy(sim, 0);
        Assert.Equal(NoticeLevel.Info, sim.Editor.Triggers()[1].Level);

        var copy = system.Triggers[1];
        sim.Editor.Starts.ChangeTrigger(system, copy, OpeningColumn.Blocks);
        sim.Tick();
        sim.Editor.PressBlock("Airlock Button");
        sim.Editor.PressChoice("button 1 is pressed");
        sim.Editor.PressPrimary();
        sim.Tick();

        Assert.False(sim.Editor.Starts.Copied(copy));
    }

    /// <summary>The copy and its empty slot are written with the program and read back as they were.</summary>
    [Fact]
    public void TheCopyIsSavedWithItsSlot()
    {
        var sim = Built(out var system);
        Copy(sim, 0);
        var made = ((SignalTrigger)system.Triggers[1]).Part;
        sim.Unit.Written(out var program, out _);

        var store = Store.Nowhere();
        store.SetValue(Framework.Storage.BlockStorage.ProgramKey, program);
        var again = Simulation.On(store);
        again.Tick();

        var read = again.Editor.Program.Systems[0];
        Assert.Equal(2, read.Triggers.Count);
        Assert.Equal(made, ((SignalTrigger)read.Triggers[1]).Part);
        Assert.True(read.FindPart(made)!.IsSignal);
    }

    /// <summary>The copy's empty slot is the copy's to say, and the original it was copied from, running the same sequence, stays clear.</summary>
    [Fact]
    public void TheOriginalSaysNothingOfTheCopysSlot()
    {
        var sim = Built(out _);

        Copy(sim, 0);
        for (var i = 0; i < 70; i++)
            sim.Tick();

        Assert.Equal("", sim.Editor.Triggers()[0].Problem);
    }

    /// <summary>Copying is an edit, so a trigger of a logic system switched on is not copied.</summary>
    [Fact]
    public void ATriggerOfARunningSystemIsNotCopied()
    {
        var sim = Built(out var system);
        sim.Editor.SwitchSystem(system, true);
        sim.Tick();

        Assert.Null(sim.Editor.Triggers()[0].Copies);
    }

    static void Copy(Simulation sim, int row)
    {
        sim.Editor.Triggers()[row].Copies!.Execute();
        sim.Tick();
        sim.Tick();
    }

    /// <summary>A logic system run when the inner door finishes closing, on its own screen.</summary>
    static Simulation OnADoor(out AutomationSystem system)
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
        return sim;
    }

    static Simulation Built(out AutomationSystem system)
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.Back();
        sim.Tick();
        system = sim.Editor.Program.Systems[0];
        return sim;
    }
}
