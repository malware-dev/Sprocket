using Lumel;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The world's trial of how a sequence's waits are shown: an underhang under each step that takes time, switching its wait; or each
/// wait a row of its own. Either way, a wait under the pointer lights the steps it covers, and a step the wait covering it.
/// </summary>
public class WaitingTrialTests
{
    /// <summary>A step that takes time has an underhang, waiting where a wait follows it; a step that takes none has none.</summary>
    [Fact]
    public void AStepThatTakesTimeHasAClock()
    {
        var rig = Airlock(out var sequence, Waiting.Switch);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), new Await(), new Wait(60));

        var rows = rig.Editor.StepRows();
        Assert.Equal(2, rows.Count);
        Assert.True(rows[0].Waits);
        Assert.Null(rows[1].Waits);
    }

    /// <summary>Pressing the underhang lets the sequence go straight on from the step, and pressing it again waits there, each one edit.</summary>
    [Fact]
    public void TheClockSwitchesTheStepsWait()
    {
        var rig = Airlock(out var sequence, Waiting.Switch);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), new Await(), new Wait(60));

        rig.Editor.StepRows()[0].Toggles!.Execute();
        rig.Tick();
        Assert.Equal(2, sequence.Steps.Count);
        Assert.False(rig.Editor.StepRows()[0].Waits);

        rig.Editor.StepRows()[0].Toggles!.Execute();
        rig.Tick();
        Assert.IsType<Await>(sequence.Steps[1]);
        Assert.True(rig.Editor.StepRows()[0].Waits);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal(2, sequence.Steps.Count);
    }

    /// <summary>The underhang says whether the sequence waits for its step.</summary>
    [Fact]
    public void TheUnderhangSaysWhetherTheStepIsWaitedFor()
    {
        var rig = Airlock(out var sequence, Waiting.Switch);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await());

        var rows = rig.Editor.StepRows();
        Assert.NotNull(Find(rows[0], element => (element as Label)?.Text == rig.Texts.SequenceDoNotWait));
        Assert.NotNull(Find(rows[1], element => (element as Label)?.Text == rig.Texts.SequenceWaitForCompletion));
    }

    /// <summary>
    /// The pointer on a step lights its own clock and the underhang of the wait covering it, and on that underhang lights its own
    /// clock and the clocks of the steps it covers, leaving their underhangs as they are.
    /// </summary>
    [Fact]
    public void TheStepUnderThePointerLightsWhatItIsTiedTo()
    {
        var rig = Airlock(out var sequence, Waiting.Switch);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await(),
            Open(rig, "Airlock Outer Door"));

        var rows = rig.Editor.StepRows();
        var chip = rows[0].ChipOf(rows[0].Slots[0])!;
        chip.IsHot = true;
        rig.Tick();
        Assert.Equal(new[] { false, true, false }, rows.Select(row => row.ClockLit).ToArray());
        Assert.Equal(new[] { true, false, false }, rows.Select(row => row.MarksLit).ToArray());

        chip.IsHot = false;
        Underhang(rows[1]).IsHot = true;
        rig.Tick();
        Assert.Equal(new[] { true, true, false }, rows.Select(row => row.MarksLit).ToArray());
        Assert.Equal(new[] { false, true, false }, rows.Select(row => row.ClockLit).ToArray());

        // Nothing waits for the last step, so it lights nothing.
        Underhang(rows[1]).IsHot = false;
        rows[2].ChipOf(rows[2].Slots[0])!.IsHot = true;
        rig.Tick();
        Assert.All(rows, row => Assert.False(row.ClockLit || row.MarksLit));
    }

    /// <summary>With waits as rows, the pointer on a wait row lights its own clock and those of the steps it covers, and on a step its own and the wait row's.</summary>
    [Fact]
    public void AWaitRowLightsWhatItIsTiedTo()
    {
        var rig = Airlock(out var sequence, Waiting.Step);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await(),
            Open(rig, "Airlock Outer Door"), new Await());

        var rows = rig.Editor.StepRows();
        rows[2].IsHot = true;
        rig.Tick();
        Assert.Equal(new[] { true, true, false, false, false }, rows.Select(row => row.MarksLit).ToArray());
        Assert.Equal(new[] { false, false, true, false, false }, rows.Select(row => row.ClockLit).ToArray());

        rows[2].IsHot = false;
        var chip = rows[3].ChipOf(rows[3].Slots[0])!;
        chip.IsHot = true;
        rig.Tick();
        Assert.Equal(new[] { false, false, false, false, true }, rows.Select(row => row.ClockLit).ToArray());
        Assert.Equal(new[] { false, false, false, true, false }, rows.Select(row => row.MarksLit).ToArray());
    }

    /// <summary>A step that goes straight on still goes straight on once changed to another action that takes time.</summary>
    [Fact]
    public void ChangingAStepKeepsItGoingStraightOn()
    {
        var rig = Airlock(out var sequence, Waiting.Switch);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), Close(rig, "Airlock Outer Door"), new Await());

        rig.Editor.StepRows()[0].Slots[0].Opens!.Execute();
        rig.Tick();
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();
        rig.Tick();

        Assert.Equal("open", ((Act)sequence.Steps[0]).ActionId);
        Assert.IsType<Act>(sequence.Steps[1]);
        Assert.False(rig.Editor.StepRows()[0].Waits);
    }

    /// <summary>A step changed to an action that takes time, from one that takes none, waits where it stands, as a new one would.</summary>
    [Fact]
    public void AStepComingToTakeTimeWaits()
    {
        var rig = Airlock(out var sequence, Waiting.Switch);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), new Await(), Doing(rig, "on", "Airlock Inner Door"));

        rig.Editor.StepRows()[1].Slots[0].Opens!.Execute();
        rig.Tick();
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();
        rig.Tick();

        Assert.IsType<Await>(sequence.Steps[3]);
        Assert.True(rig.Editor.StepRows()[1].Waits);
    }

    /// <summary>A spine joins the lit lines from the first to the last, through every row between, unless the world switches it off.</summary>
    [Fact]
    public void ASpineJoinsTheLitLinesUnlessTheWorldSwitchesItOff()
    {
        var rig = Airlock(out var sequence, Waiting.Switch);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), new Wait(60), Close(rig, "Airlock Outer Door"), new Await(),
            Open(rig, "Airlock Outer Door"));

        var rows = rig.Editor.StepRows();
        rows[0].ChipOf(rows[0].Slots[0])!.IsHot = true;
        rig.Tick();
        Assert.Equal(new[] { true, true, true, false }, rows.Select(row => row.Joins).ToArray());
        Assert.True(rows[0].JoinFirst);
        Assert.True(rows[2].JoinLast);

        rig.Settle(rig.Settings.WithWorld(rig.Settings.World.WithWaitSpine(false)));
        rig.Tick();
        rows = rig.Editor.StepRows();
        rows[0].ChipOf(rows[0].Slots[0])!.IsHot = true;
        rig.Tick();
        Assert.All(rows, row => Assert.False(row.Joins));
    }

    /// <summary>An await with nothing above it that takes time is warned about under its own line; one after a step that takes time is not.</summary>
    [Fact]
    public void AnAwaitWithNothingToWaitForIsWarnedAbout()
    {
        var rig = Airlock(out var sequence, Waiting.Step);
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
        var rig = Airlock(out var sequence, Waiting.Step);
        Steps(rig, sequence, new If(Closed(rig)), Close(rig, "Airlock Inner Door"), new Else(null!), new Wait(60), new EndIf(),
            new Await());

        Assert.All(rig.Editor.StepRows(), row => Assert.False(row.HasProblem));
    }

    /// <summary>With waits as rows of their own, each wait is a numbered line, and the steps have no underhangs.</summary>
    [Fact]
    public void EachWaitIsARowOfItsOwn()
    {
        var rig = Airlock(out var sequence, Waiting.Step);
        Steps(rig, sequence, Close(rig, "Airlock Inner Door"), new Await(), new Wait(60));

        var rows = rig.Editor.StepRows();
        Assert.Equal(3, rows.Count);
        Assert.Equal(StepKind.Await, rows[1].Kind);
        Assert.Equal(new[] { 1, 2, 3 }, rows.Select(row => row.Number).ToArray());
        Assert.All(rows, row => Assert.Null(row.Waits));
    }

    /// <summary>A wait row is deleted on its own, and a step deleted leaves the wait row after it.</summary>
    [Fact]
    public void AWaitRowIsDeletedOnItsOwn()
    {
        var rig = Airlock(out var sequence, Waiting.Step);
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

    /// <summary>The menu of steps offers an await where waits are rows of their own, and adds one with nothing else.</summary>
    [Fact]
    public void TheMenuOffersAWaitForAllWhereWaitsAreRows()
    {
        var rig = Airlock(out var sequence, Waiting.Switch);
        Steps(rig, sequence, new If(Closed(rig)), new EndIf());
        Assert.DoesNotContain(rig.Texts.SequenceAwait, Offered(rig));

        rig.Settle(rig.Settings.WithWorld(rig.Settings.World.WithWaiting(Waiting.Step)));
        rig.Tick();
        Assert.Contains(rig.Texts.SequenceAwait, Offered(rig));

        Menu(rig).Single(choice => choice.Text == rig.Texts.SequenceAwait).Adds!.Execute();
        rig.Tick();
        Assert.IsType<Await>(sequence.Steps[1]);
        Assert.Equal(3, sequence.Steps.Count);
    }

    /// <summary>The menu of steps at the end of the if's branch.</summary>
    static IList<Adder> Menu(Simulation rig) =>
        Assert.Single(rig.Editor.StepRows().Single(row => row.Kind == StepKind.Adds).Adds).Choices!;

    /// <summary>The words the menu of steps offers.</summary>
    static List<string> Offered(Simulation rig) => Menu(rig).Select(choice => choice.Text).ToList();

    static Comparison Closed(Simulation rig)
    {
        var door = rig.World.Entities.First(entity => entity.Name == "Airlock Outer Door");
        var test = new Comparison { MemberId = "status", Provider = "ext", Value = Literal.Name("closed") };
        test.Targets.Add(rig.Editor.Program.Declare(rig.World, door).Name);
        return test;
    }

    /// <summary>The underhang of <paramref name="row" />, found as what its press switches the wait with.</summary>
    static Element Underhang(StepRow row) => Find(row, element => row.Toggles != null && element.Command == row.Toggles)!;

    static Element? Find(Element element, Func<Element, bool> wanted)
    {
        if (wanted(element))
            return element;

        for (var i = 0; i < element.VisualChildCount; i++)
        {
            var found = Find(element.GetVisualChild(i), wanted);
            if (found != null)
                return found;
        }

        return null;
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

    static Simulation Airlock(out Sequence sequence, Waiting waiting)
    {
        var rig = Simulation.Fresh();
        rig.Settle(new Settings(Layer.Empty, new Layer(null, null, waiting)));
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
