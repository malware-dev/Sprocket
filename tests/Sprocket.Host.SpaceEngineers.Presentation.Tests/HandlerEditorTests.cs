using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// "if it fails" and "finally" are added from the sequence's last "+ step", each once, and stand at its bottom, the catch
/// first, each ending at its own "+ step"; a sequence running its finally while switched off is locked until it ends.
/// </summary>
public class HandlerEditorTests
{
    /// <summary>The last "+ step" offers both after the kinds of step; each drops out once added, and the catch goes above a finally.</summary>
    [Fact]
    public void EachIsAddedOnceAndTheCatchGoesFirst()
    {
        var screen = Sequence();
        Assert.Equal(new[] { SentenceText.CatchWords, SentenceText.FinallyWord }, Last(screen).Choices.Skip(Last(screen).Choices.Count - 2).Select(choice => choice.Text));

        screen.Editor.PressAdd(SentenceText.FinallyWord);
        screen.Run(3);
        Assert.DoesNotContain(Last(screen).Choices, choice => choice.Text == SentenceText.FinallyWord);

        screen.Editor.PressAdd(SentenceText.CatchWords);
        screen.Run(3);

        var steps = screen.Editor.Program.Sequences[0].Steps;
        Assert.IsType<Catch>(steps[steps.Count - 2]);
        Assert.IsType<Finally>(steps[steps.Count - 1]);
        Assert.False(Last(screen).IsVisible);
        Assert.Contains(Page(screen).Rows.OfType<StepRow>(), row => row.Kind == StepKind.Catch);
        Assert.Contains(Page(screen).Rows.OfType<StepRow>(), row => row.Kind == StepKind.Finally);
    }

    /// <summary>A step added from the end of the catch goes into it, and one added above the catch stays among the sequence's own.</summary>
    [Fact]
    public void EachPartTakesItsOwnSteps()
    {
        var screen = Sequence();
        screen.Editor.PressAdd(SentenceText.CatchWords);
        screen.Run(3);
        var sequence = screen.Editor.Program.Sequences[0];
        var own = sequence.HandlersAt;

        // The first "+ step" on the screen ends the sequence's own steps; the last ends the catch.
        var ends = Page(screen).Rows.OfType<StepRow>().Where(row => row.Kind == StepKind.Adds && row.IsVisible).ToList();
        Assert.Equal(2, ends.Count);
        ends[1].Adds[0].Choices!.Single(choice => choice.Text == SentenceText.AbortWord).Adds!.Execute();
        screen.Run(3);
        Assert.IsType<Abort>(sequence.Steps[sequence.Steps.Count - 1]);

        ends = Page(screen).Rows.OfType<StepRow>().Where(row => row.Kind == StepKind.Adds && row.IsVisible).ToList();
        ends[0].Adds[0].Choices!.Single(choice => choice.Text == SentenceText.AbortWord).Adds!.Execute();
        screen.Run(3);
        Assert.IsType<Abort>(sequence.Steps[own]);
        Assert.IsType<Catch>(sequence.Steps[own + 1]);
    }

    /// <summary>Deleting a part that holds steps asks first; keeping them has them join the steps above.</summary>
    [Fact]
    public void DeletingAPartWithStepsAsks()
    {
        var screen = Sequence();
        screen.Editor.PressAdd(SentenceText.CatchWords);
        screen.Run(3);
        var sequence = screen.Editor.Program.Sequences[0];
        var own = sequence.Steps.Count - 1;
        sequence.Steps.Add(new Abort());
        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
        screen.Run(3);

        Page(screen).Rows.OfType<StepRow>().Single(row => row.Kind == StepKind.Catch).Deletes!.Execute();
        screen.Run(2);
        Assert.Equal(screen.Editor.Texts.SequenceRemoveHandler, screen.Editor.Question());
        screen.Editor.PressChoice(screen.Editor.Texts.SequenceKeepSteps);
        screen.Run(3);

        Assert.Equal(-1, sequence.CatchAt);
        Assert.IsType<Abort>(sequence.Steps[own]);
    }

    /// <summary>A run in its finally, its logic system switched off, locks the sequence and says why, until the finally ends.</summary>
    [Fact]
    public void ARunInItsFinallyLocksTheSequence()
    {
        var screen = Sequence();
        var sequence = screen.Editor.Program.Sequences[0];
        sequence.Steps.Add(new Wait(Duration.OfTicks(300)));
        sequence.Steps.Add(new Finally());
        sequence.Steps.Add(new Wait(Duration.OfTicks(120)));
        screen.Editor.Program.Edited();
        screen.Editor.Invalidate();
        screen.Run(3);

        screen.Editor.SwitchOn();
        screen.Run(3);
        screen.Editor.RunNow(sequence);
        screen.Run(10);
        sequence.System.Enabled = false;
        screen.Editor.Program.Edited();
        screen.Run(10);

        Assert.True(screen.Editor.Runner.Get(sequence)!.Cleaning);
        Assert.False(Page(screen).Editable);
        Assert.Equal(screen.Editor.Texts.SequenceCleaning, Page(screen).LockedText);

        screen.Run(200);
        Assert.False(screen.Editor.Runner.Get(sequence)!.IsRunning);
        Assert.True(Page(screen).Editable);
    }

    /// <summary>The Airlock's first sequence, its logic system switched off, on its screen.</summary>
    static Screen Sequence()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        return screen;
    }

    static SequencePage Page(Screen screen) => Assert.IsType<SequencePage>(screen.Editor.Current);

    /// <summary>The sequence's last "+ step", under every row.</summary>
    static PlusMenu Last(Screen screen) => Page(screen).View!.Within().OfType<PlusMenu>().Last(menu => !(menu.Parent is StepRow) && menu.Parent?.Parent is not StepRow);
}
