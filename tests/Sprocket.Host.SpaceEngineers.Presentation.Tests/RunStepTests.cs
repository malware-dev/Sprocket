using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A step that runs or stops another sequence of the system is added from the sequence's own screen, each with a button of its own.</summary>
public class RunStepTests
{
    /// <summary>Every other sequence is offered to run, and never the sequence itself.</summary>
    [Fact]
    public void TheOtherSequencesAreOfferedToRun()
    {
        var rig = Two(out var first, out _);

        rig.Editor.PressAdd(SentenceText.RunWord);
        rig.Tick();

        Assert.IsType<ChoicePage>(rig.Editor.Current);
        var offered = rig.Editor.Choices().Select(row => row.Text).ToList();
        Assert.Equal(new[] { "Cycle Out" }, offered);
        Assert.DoesNotContain(offered, text => text.Contains(first.Name));
    }

    /// <summary>Running another sequence adds the step and a wait for it after it, as one step back.</summary>
    [Fact]
    public void RunningOneAddsTheStepAndAWaitForIt()
    {
        var rig = Two(out var first, out var second);

        rig.Editor.PressAdd(SentenceText.RunWord);
        rig.Tick();
        rig.Editor.PressChoice("Cycle Out");
        rig.Tick();

        Assert.IsType<SequencePage>(rig.Editor.Current);
        var run = Assert.IsType<Run>(first.Steps[first.Steps.Count - 2]);
        Assert.Equal(second.Number, run.Number);
        Assert.False(run.Stops);
        Assert.IsType<Await>(first.Steps[first.Steps.Count - 1]);

        var count = first.Steps.Count;
        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal(count - 2, first.Steps.Count);
    }

    /// <summary>Stopping another sequence adds the step alone.</summary>
    [Fact]
    public void StoppingOneAddsTheStepAlone()
    {
        var rig = Two(out var first, out var second);
        var before = first.Steps.Count;

        rig.Editor.PressAdd(SentenceText.StopWord);
        rig.Tick();
        rig.Editor.PressChoice("Cycle Out");
        rig.Tick();

        Assert.Equal(before + 1, first.Steps.Count);
        var run = Assert.IsType<Run>(first.Steps[before]);
        Assert.Equal(second.Number, run.Number);
        Assert.True(run.Stops);
    }

    /// <summary>A step that runs another sequence carries the mark of something a step waits for; one that stops it does not.</summary>
    [Fact]
    public void RunningCarriesTheWaitsMarkAndStoppingDoesNot()
    {
        var rig = Two(out _, out _);

        rig.Editor.PressAdd(SentenceText.RunWord);
        rig.Tick();
        rig.Editor.PressChoice("Cycle Out");
        rig.Tick();
        rig.Editor.PressAdd(SentenceText.StopWord);
        rig.Tick();
        rig.Editor.PressChoice("Cycle Out");
        rig.Tick();

        var rows = rig.Editor.StepRows();
        Assert.True(Waits(rows[0]), "running another sequence has no waits mark");
        Assert.False(Waits(rows[2]), "stopping another sequence has the waits mark");
    }

    /// <summary>Pressing the sequence a run step names opens the list that made it, and choosing another is one step back.</summary>
    [Fact]
    public void TheSequenceARunStepNamesIsChangedFromItsList()
    {
        var rig = Two(out var first, out var second);
        var third = rig.Editor.Program.AddSequence(first.System!);
        third.Name = "Purge";

        rig.Editor.PressAdd(SentenceText.RunWord);
        rig.Tick();
        rig.Editor.PressChoice("Cycle Out");
        rig.Tick();
        var run = Assert.IsType<Run>(first.Steps[0]);

        rig.Editor.PressStepSlot(0, 0);
        rig.Tick();
        Assert.IsType<ChoicePage>(rig.Editor.Current);
        rig.Editor.PressChoice("Purge");
        rig.Tick();

        Assert.Equal(third.Number, run.Number);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal(second.Number, run.Number);
    }

    /// <summary>Whether a visible mark of something a step waits for stands anywhere in <paramref name="element" />.</summary>
    static bool Waits(Lumel.Element element) =>
        element.Within().OfType<Lumel.Images.Glyph>()
            .Any(glyph => Shown(glyph, element) && glyph.Symbol == Symbols.Lasting);

    /// <summary>Whether <paramref name="element" /> is on the screen: it and everything it stands in, up to <paramref name="top" />, visible.</summary>
    static bool Shown(Lumel.Element element, Lumel.Element top)
    {
        for (var at = element; at != null; at = at.Parent)
        {
            if (!at.IsVisible)
                return false;

            if (at == top)
                return true;
        }

        return true;
    }

    /// <summary>A system with two sequences, switched off so they can be edited, with the first one's screen open.</summary>
    /// <summary>The sequence a run step names says that pressing it chooses the sequence the step runs, and a stop step's the one it stops.</summary>
    [Fact]
    public void TheSequenceNamedSaysWhatPressingItDoes()
    {
        var rig = Two(out _, out _);
        rig.Editor.PressAdd(SentenceText.RunWord);
        rig.Tick();
        rig.Editor.PressChoice("Cycle Out");
        rig.Tick();
        rig.Editor.PressAdd(SentenceText.StopWord);
        rig.Tick();
        rig.Editor.PressChoice("Cycle Out");
        rig.Tick();

        var tips = rig.Editor.StepRows()
            .SelectMany(row => row.Slots.OfType<SequenceSlot>().Select(part => row.ChipOf(part)!.Tip?.Text))
            .ToArray();
        Assert.Contains(rig.Editor.Texts.Codex.PartRunsAbout, tips);
        Assert.Contains(rig.Editor.Texts.Codex.PartStopsAbout, tips);
    }

    static Simulation Two(out Sequence first, out Sequence second)
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Tick();

        var system = rig.Editor.Program.Systems[0];
        first = system.Sequences[0];
        first.Name = "Cycle In";
        second = rig.Editor.Program.AddSequence(system);
        second.Name = "Cycle Out";
        system.Enabled = false;
        rig.Editor.Program.Edited();

        rig.Editor.GoToRoot();
        rig.Tick();
        rig.Editor.Push(new SequencePage(rig.Editor, first));
        rig.Tick();
        return rig;
    }
}
