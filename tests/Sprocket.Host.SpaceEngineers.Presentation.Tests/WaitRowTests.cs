using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel.Input;
using Lumel.Images;
using Sprocket.Sim.Machine;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// How a sequence shows its waits: each wait for what is running is a row of its own. A wait under the pointer lights the steps
/// it covers, and a step the wait covering it.
/// </summary>
public class WaitRowTests
{
    /// <summary>The pointer on a wait row lights its own clock and those of the steps it covers, and on a step its own and the wait row's.</summary>
    [Fact]
    public void AWaitRowLightsWhatItIsTiedTo()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await(),
            Open(rig, "Airlock Outer Door"), new Await());

        var rows = rig.Editor.StepRows();
        ((IInputTarget)rows[2]).IsPointerOver = true;
        rig.Tick();
        Assert.Equal(new[] { true, true, false, false, false }, rows.Select(row => row.MarksLit).ToArray());
        Assert.Equal(new[] { false, false, true, false, false }, rows.Select(row => row.ClockLit).ToArray());

        ((IInputTarget)rows[2]).IsPointerOver = false;
        var chip = rows[3].ChipOf(rows[3].Slots[0])!;
        ((IInputTarget)chip).IsPointerOver = true;
        rig.Tick();
        Assert.Equal(new[] { false, false, false, false, true }, rows.Select(row => row.ClockLit).ToArray());
        Assert.Equal(new[] { false, false, false, true, false }, rows.Select(row => row.MarksLit).ToArray());
    }

    /// <summary>The pointer itself, on a step's number, its words or the room after them, lights the wait covering it; and on the wait's words, the step.</summary>
    [Theory]
    [InlineData("number")]
    [InlineData("words")]
    [InlineData("after")]
    public void ThePointerOnAStepLightsItsWait(string where)
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        editor.PressAdd("task");
        editor.PressBlock("Airlock Inner Door");
        editor.PressChoice("open");
        editor.PressPrimary();
        screen.Run(5);

        var rows = editor.StepRows();
        var step = rows.First(row => row.Kind == StepKind.Do);
        var wait = rows.First(row => row.Kind == StepKind.Await);
        var number = step.Within().OfType<Lumel.Text.Label>().First(label => label.Text == step.Number.ToString());
        var at = where == "number" ? number.ToScreen(number.Bounds.Center)
            : where == "words" ? step.ChipOf(step.Slots[0])!.ToScreen(step.ChipOf(step.Slots[0])!.Bounds.Center)
            : step.ToScreen(new Vector2(step.Bounds.X + step.Bounds.Width * 0.6f, step.Bounds.Center.Y));

        screen.Scene.MoveTo(at);
        screen.Run(2);
        Assert.True(wait.ClockLit, "the wait's clock is not lit with the pointer on the step's " + where);

        var words = wait.Within().OfType<Lumel.Text.RichText>().First();
        screen.Scene.MoveTo(words.ToScreen(words.Bounds.Center));
        screen.Run(2);
        Assert.True(step.MarksLit, "the step's mark is not lit with the pointer on the wait's words");
    }

    /// <summary>A run, which is waited for as an action that takes time is, leads with that mark, and a wait with its clock.</summary>
    [Fact]
    public void ARunLeadsWithTheMarkOfWhatIsWaitedFor()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, new Run(1), new Await());

        var rows = rig.Editor.StepRows();
        Assert.Equal(Symbols.Lasting, Lead(rows[0]));
        Assert.Equal(Symbols.Awaited, Lead(rows[1]));
    }

    static string Lead(StepRow row) => row.Within().OfType<Glyph>().First(glyph => glyph.IsVisible).Symbol;

    /// <summary>The pointer on a wait row's words lights the steps it covers, as the pointer on the row around them does.</summary>
    [Fact]
    public void AWaitRowLightsWhatItIsTiedToFromItsWords()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await());

        var rows = rig.Editor.StepRows();
        ((IInputTarget)rows[2].Within().OfType<Lumel.Text.RichText>().First()).IsPointerOver = true;
        rig.Tick();
        Assert.Equal(new[] { true, true, false }, rows.Select(row => row.MarksLit).ToArray());
        Assert.Equal(new[] { false, false, true }, rows.Select(row => row.ClockLit).ToArray());
    }

    /// <summary>The pointer on a step lights its own mark and the wait row covering it, and nothing waits for a step with no wait below it.</summary>
    [Fact]
    public void AStepLightsTheWaitCoveringIt()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await(),
            Open(rig, "Airlock Outer Door"));

        var rows = rig.Editor.StepRows();
        var chip = rows[0].ChipOf(rows[0].Slots[0])!;
        ((IInputTarget)chip).IsPointerOver = true;
        rig.Tick();
        Assert.Equal(new[] { false, false, true, false }, rows.Select(row => row.ClockLit).ToArray());
        Assert.Equal(new[] { true, false, false, false }, rows.Select(row => row.MarksLit).ToArray());

        // Nothing waits for the last step, so it lights nothing.
        ((IInputTarget)chip).IsPointerOver = false;
        ((IInputTarget)rows[3].ChipOf(rows[3].Slots[0])!).IsPointerOver = true;
        rig.Tick();
        Assert.All(rows, row => Assert.False(row.ClockLit || row.MarksLit));
    }

    /// <summary>The pointer on a step's plain words or the room round them, where the row itself takes it, lights the wait covering it as its parts do.</summary>
    [Fact]
    public void AStepLightsItsWaitFromAnywhereOnIt()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await());

        var rows = rig.Editor.StepRows();
        ((IInputTarget)rows[1]).IsPointerOver = true;
        rig.Tick();
        Assert.Equal(new[] { false, false, true }, rows.Select(row => row.ClockLit).ToArray());
        Assert.Equal(new[] { false, true, false }, rows.Select(row => row.MarksLit).ToArray());

        ((IInputTarget)rows[1]).IsPointerOver = false;
        ((IInputTarget)rows[0].Within().OfType<Lumel.Text.RichText>().First()).IsPointerOver = true;
        rig.Tick();
        Assert.Equal(new[] { false, false, true }, rows.Select(row => row.ClockLit).ToArray());
        Assert.Equal(new[] { true, false, false }, rows.Select(row => row.MarksLit).ToArray());
    }

    /// <summary>A paused wait is read as though it were not there: its steps go to the next wait, and it lights nothing itself.</summary>
    [Fact]
    public void APausedWaitLeavesItsStepsToTheNext()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await(),
            Open(rig, "Airlock Outer Door"), new Await());
        sequence.Steps[2].IsOff = true;
        rig.Editor.Program.Edited();
        rig.Tick();

        var rows = rig.Editor.StepRows();
        ((IInputTarget)rows[4]).IsPointerOver = true;
        rig.Tick();
        Assert.Equal(new[] { true, true, false, true, false }, rows.Select(row => row.MarksLit).ToArray());

        ((IInputTarget)rows[4]).IsPointerOver = false;
        ((IInputTarget)rows[2]).IsPointerOver = true;
        rig.Tick();
        Assert.All(rows, row => Assert.False(row.MarksLit || row.ClockLit));
    }

    /// <summary>A paused step that takes time is not waited for, so the wait below it lights only the others.</summary>
    [Fact]
    public void APausedStepIsNotWaitedFor()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await());
        sequence.Steps[1].IsOff = true;
        rig.Editor.Program.Edited();
        rig.Tick();

        var rows = rig.Editor.StepRows();
        ((IInputTarget)rows[2]).IsPointerOver = true;
        rig.Tick();
        Assert.Equal(new[] { true, false, false }, rows.Select(row => row.MarksLit).ToArray());
    }

    /// <summary>Nothing is lit while a step is carried, whatever the pointer passes over.</summary>
    [Fact]
    public void NothingLightsWhileAStepIsCarried()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await());

        var rows = rig.Editor.StepRows();
        ((IInputTarget)rows[0].DragHandle).PointerDown(Vector2.Zero, PointerButton.Left, 1, false, false);
        ((IInputTarget)rows[2]).IsPointerOver = true;
        rig.Tick();
        Assert.All(rows, row => Assert.False(row.ClockLit || row.MarksLit));
    }

    /// <summary>A step that goes straight on still goes straight on once changed to another action that takes time.</summary>
    [Fact]
    public void ChangingAStepKeepsItGoingStraightOn()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await());

        rig.Editor.StepRows()[0].Slots[0].Opens!.Execute();
        rig.Tick();
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();
        rig.Tick();

        Assert.Equal("open", ((Act)sequence.Steps[0]).ActionId);
        Assert.IsType<Act>(sequence.Steps[1]);
        Assert.Equal(3, sequence.Steps.Count);
    }

    /// <summary>A step changed to an action that takes time, from one that takes none, is followed by a wait, as a new one would be.</summary>
    [Fact]
    public void AStepComingToTakeTimeWaits()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), new Await(), Doing(rig, "on", "Airlock Inner Door"));

        rig.Editor.StepRows()[2].Slots[0].Opens!.Execute();
        rig.Tick();
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();
        rig.Tick();

        Assert.IsType<Await>(sequence.Steps[3]);
    }

    /// <summary>A spine joins the lit lines from the first to the last, through every row between.</summary>
    [Fact]
    public void ASpineJoinsTheLitLines()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), new Wait(60), Close(rig, "Airlock Outer Door"), new Await(),
            Open(rig, "Airlock Outer Door"));

        var rows = rig.Editor.StepRows();
        ((IInputTarget)rows[0].ChipOf(rows[0].Slots[0])!).IsPointerOver = true;
        rig.Tick();
        Assert.Equal(new[] { true, true, true, true, false }, rows.Select(row => row.Joins).ToArray());
        Assert.True(rows[0].JoinFirst);
        Assert.True(rows[3].JoinLast);
    }

    /// <summary>An await with nothing above it that takes time is warned about under its own line; one after a step that takes time is not.</summary>
    [Fact]
    public void AnAwaitWithNothingToWaitForIsWarnedAbout()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), new Await(), new Wait(60), new Await());

        var rows = rig.Editor.StepRows();
        Assert.False(rows[1].HasProblem);
        Assert.True(rows[3].HasProblem);
        Assert.Equal(NoticeLevel.Warning, rows[3].Level);
        Assert.Equal(rig.Texts.ProblemNothingToAwait, rows[3].Problem);
        Assert.Empty(rig.Editor.Sentence().Bar.Problem);
    }

    /// <summary>An await after an if has something to wait for where any way through the if starts something that takes time.</summary>
    [Fact]
    public void AnAwaitAfterAnIfWaitsForWhatAnyBranchStarts()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, new If(Closed(rig)), Close(rig, "Airlock Inner Door"), new Else(null!), new Wait(60), new EndIf(),
            new Await());

        Assert.All(rig.Editor.StepRows(), row => Assert.False(row.HasProblem));
    }

    /// <summary>A wait row is deleted on its own, and a step deleted leaves the wait row after it.</summary>
    [Fact]
    public void AWaitRowIsDeletedOnItsOwn()
    {
        var rig = Airlock(out var sequence);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), new Await(), new Wait(60));

        rig.Editor.StepRows()[1].Deletes!.Execute();
        rig.Tick();
        Assert.IsType<Act>(sequence.Steps[0]);
        Assert.IsType<Wait>(sequence.Steps[1]);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        rig.Editor.StepRows()[0].Deletes!.Execute();
        rig.Tick();
        Assert.IsType<Await>(sequence.Steps[0]);
        Assert.IsType<Wait>(sequence.Steps[1]);
    }

    static Comparison Closed(Simulation rig)
    {
        var door = rig.World.Entities.First(entity => entity.Name == "Airlock Outer Door");
        var test = new Comparison { MemberId = "status", Provider = "ext", Value = Literal.Name("closed") };
        test.Targets.Add(rig.Editor.Program.Declare(rig.World, door).Name);
        return test;
    }

    static Act Close(Simulation rig, string door) => Doing(rig, "close", door);

    static Act Open(Simulation rig, string door) => Doing(rig, "open", door);

    static Act Doing(Simulation rig, string action, string door)
    {
        var step = new Act(action);
        step.Targets.Add(rig.Editor.Program.Declare(rig.World, rig.World.Entities.First(entity => entity.Name == door)).Name);
        return step;
    }

    static void Steps(Simulation rig, Sequence sequence, params Step[] steps)
    {
        sequence.Steps.Clear();
        foreach (var step in steps)
            sequence.Steps.Add(step);
        rig.Editor.Program.Edited();
        rig.Tick();
    }

    static Simulation Airlock(out Sequence sequence)
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Tick();

        var system = rig.Editor.Program.Systems[0];
        sequence = system.Sequences[0];
        system.Enabled = false;
        rig.Editor.Program.Edited();

        rig.Editor.GoToRoot();
        rig.Tick();
        rig.Editor.Push(new SequencePage(rig.Editor, sequence));
        rig.Tick();
        return rig;
    }
}
