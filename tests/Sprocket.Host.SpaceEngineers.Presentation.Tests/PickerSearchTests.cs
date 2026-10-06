using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What is typed into a picker's block search belongs to the screen the picker was opened from: every picker opened from
/// it starts with it, and it is gone once that screen is left. The kind and the other searches start empty every time.
/// </summary>
public class PickerSearchTests
{
    /// <summary>A step's picker opens with the block search the last one on the same sequence was left with.</summary>
    [Fact]
    public void ASequenceKeepsItsSearchFromPickerToPicker()
    {
        var sim = OnASequence();

        OpenTask(sim);
        sim.Editor.Pick().Stages[0].Search("airl");
        sim.Tick();
        sim.Editor.Back();
        sim.Tick();

        OpenTask(sim);
        Assert.Equal("airl", sim.Editor.Pick().Stages[0].NarrowText);
    }

    /// <summary>Once a logic system's screen is left, a signal slot's picker opened elsewhere starts with no search.</summary>
    [Fact]
    public void LeavingAScreenClearsItsSearch()
    {
        var sim = OnASequence();
        sim.Editor.Back();
        sim.Tick();

        sim.Editor.PressStart("when");
        sim.Tick();
        sim.Editor.Pick().Stages[0].Search("airl");
        sim.Tick();
        sim.Editor.Back();
        sim.Tick();

        sim.Editor.GoToRoot();
        sim.Tick();
        sim.Editor.AddInstallation(0);
        sim.Tick();
        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();

        Assert.True(string.IsNullOrEmpty(sim.Editor.Pick().Stages[0].NarrowText), "the search outlived its screen");
    }

    /// <summary>What was typed over the kinds is that picker's alone: the next one opens with every kind.</summary>
    [Fact]
    public void TheKindSearchStartsEmptyInEveryPicker()
    {
        var sim = OnASequence();

        OpenTask(sim);
        sim.Editor.Pick().Stages[0].SearchKinds("door");
        sim.Tick();
        sim.Editor.Back();
        sim.Tick();

        OpenTask(sim);
        Assert.True(string.IsNullOrEmpty(sim.Editor.Pick().Stages[0].KindText), "the kind search was kept");
    }

    /// <summary>A configuration panel keeps its block search from one slot's picker to the next.</summary>
    [Fact]
    public void AConfigurationPanelKeepsItsSearchFromSlotToSlot()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        sim.Tick();
        sim.Editor.AddInstallation(0);
        sim.Tick();

        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();
        sim.Editor.Pick().Stages[0].Search("airl");
        sim.Tick();
        sim.Editor.Back();
        sim.Tick();

        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();
        Assert.Equal("airl", sim.Editor.Pick().Stages[0].NarrowText);
    }

    /// <summary>A logic system with a trigger started by the airlock's button, on the sequence it runs.</summary>
    static Simulation OnASequence()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Tick();
        Assert.IsType<Features.Sequences.SequencePage>(sim.Editor.Current);
        return sim;
    }

    /// <summary>Opens the picker for a new step.</summary>
    static void OpenTask(Simulation sim)
    {
        sim.Editor.PressAdd("task");
        sim.Tick();
        Assert.IsType<Features.Picking.PickPage>(sim.Editor.Current);
    }
}
