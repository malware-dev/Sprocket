using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A step that runs or stops another sequence of the system is added from the sequence's own screen, each with a button of its own.</summary>
public class RunStepTests
{
    /// <summary>Run offers, in a menu where it was pressed, a new sequence, a line, then every other sequence, and never the sequence itself.</summary>
    [Fact]
    public void TheOtherSequencesAreOfferedToRun()
    {
        var rig = Two(out var first, out _);

        rig.Editor.PressAdd(SentenceText.RunWord);
        rig.Tick();

        Assert.IsType<SequencePage>(rig.Editor.Current);
        Assert.True(rig.Editor.Screen.HasQuestion);
        var answers = rig.Editor.Screen.Answers;
        Assert.Equal(rig.Texts.PickNewSequence, answers[0].Text);
        Assert.True(answers[1].IsRule, "no line between a new sequence and the others");
        var offered = answers.Skip(2).Select(answer => answer.Text).ToList();
        Assert.Equal(new[] { "Cycle Out" }, offered);
        Assert.DoesNotContain(offered, text => text.Contains(first.Name));
    }

    /// <summary>Running a new sequence makes it, adds the step running it with a wait after it, and opens it; one step back takes all of it.</summary>
    [Fact]
    public void RunningANewSequenceMakesItAndOpensIt()
    {
        var rig = Two(out var first, out _);
        var system = first.System!;
        var count = first.Steps.Count;

        rig.Editor.PressAdd(SentenceText.RunWord);
        rig.Tick();
        rig.Editor.PressAnswer(rig.Texts.PickNewSequence);
        rig.Tick();

        Assert.Equal(3, system.Sequences.Count);
        var made = system.Sequences[2];
        var run = Assert.IsType<Run>(first.Steps[count]);
        Assert.Equal(made.Number, run.Number);
        Assert.IsType<Await>(first.Steps[count + 1]);
        Assert.Equal(made.Id, Assert.IsType<SequencePage>(rig.Editor.Current).SequenceId);

        rig.Editor.Back();
        rig.Tick();
        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal(2, system.Sequences.Count);
        Assert.Equal(count, first.Steps.Count);
    }

    /// <summary>Stop chosen in the step menu hangs the sequences on the screen where that menu hung, and a press there adds the step.</summary>
    [Fact]
    public void StopsMenuHangsWhereTheStepMenuHung()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        var system = editor.Program.Systems[0];
        var first = system.Sequences[0];
        editor.Program.AddSequence(system).Name = "Cycle Out";
        system.Enabled = false;
        editor.Program.Edited();
        editor.GoToRoot();
        editor.Push(new SequencePage(editor, first));
        screen.Run(5);

        screen.Do("press", "step", "press", "stop");
        screen.Run(2);
        Assert.True(screen.Scene.IsQuestioning, "the sequences did not hang on the screen");
        Assert.True(screen.Shows("Cycle Out"), "the other sequence is not offered");

        screen.Do("press", "Cycle Out");
        screen.Run(2);
        var stop = Assert.IsType<Run>(first.Steps[first.Steps.Count - 1]);
        Assert.True(stop.Stops);
    }

    /// <summary>Stop with no other sequence says so in its menu, with nothing to choose.</summary>
    [Fact]
    public void StopWithNoOtherSequenceSaysSo()
    {
        var rig = Two(out _, out var second);
        rig.Editor.Program.RemoveSequence(second);
        rig.Editor.Program.Edited();
        rig.Tick();

        rig.Editor.PressAdd(SentenceText.StopWord);
        rig.Tick();

        Assert.IsType<SequencePage>(rig.Editor.Current);
        Assert.Equal(rig.Texts.SequenceStopNone, rig.Editor.Screen.Question);
        Assert.Empty(rig.Editor.Screen.Answers);
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

    /// <summary>Pressing the sequence a run step names offers the other sequences in a menu under it, and choosing another is one step back.</summary>
    [Fact]
    public void TheSequenceARunStepNamesIsChangedFromAMenuUnderIt()
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
        Assert.True(rig.Editor.Screen.HasQuestion);
        Assert.Equal(new[] { rig.Texts.PickNewSequence, "Cycle Out", "Purge" },
            rig.Editor.Screen.Answers.Where(answer => !answer.IsRule).Select(answer => answer.Text).ToArray());
        Assert.Equal("Cycle Out", rig.Editor.Screen.Default!.Text);
        rig.Editor.PressAnswer("Purge");
        rig.Tick();

        Assert.Equal(third.Number, run.Number);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal(second.Number, run.Number);
    }

    /// <summary>A run step in a system with no other sequence left still offers a new one, with no line under it.</summary>
    [Fact]
    public void ARunStepWithNoOtherSequenceOffersANewOne()
    {
        var rig = Two(out _, out var second);
        rig.Editor.PressAdd(SentenceText.RunWord);
        rig.Tick();
        rig.Editor.PressChoice("Cycle Out");
        rig.Tick();
        rig.Editor.Program.RemoveSequence(second);
        rig.Editor.Program.Edited();
        rig.Tick();

        rig.Editor.PressStepSlot(0, 0);
        rig.Tick();
        Assert.IsType<SequencePage>(rig.Editor.Current);
        Assert.Equal(new[] { rig.Texts.PickNewSequence }, rig.Editor.Screen.Answers.Select(answer => answer.Text).ToArray());
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
