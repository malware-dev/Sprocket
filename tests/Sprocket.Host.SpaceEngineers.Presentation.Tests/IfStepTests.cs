using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>An if is added, given branches, filled, moved and taken away on the sequence's own screen.</summary>
public class IfStepTests
{
    /// <summary>The if button asks what to check and adds an if checking it, closed and empty, after the last step.</summary>
    [Fact]
    public void AnIfIsAddedFromItsButton()
    {
        var rig = Airlock(out var sequence);
        var before = sequence.Steps.Count;

        AddIf(rig, "Airlock Outer Door", "is closed");

        var opens = Assert.IsType<If>(sequence.Steps[before]);
        Assert.IsType<EndIf>(sequence.Steps[before + 1]);
        Assert.Equal("closed", opens.Test.MemberId);
        Assert.True(opens.Test.IsMoment);
        Assert.Equal(Literal.Of(true), opens.Test.Value);
    }

    /// <summary>A state's other side is a choice of its own, and makes a test for the blocks not being in it.</summary>
    [Fact]
    public void NotBeingInAStateIsAChoiceOfItsOwn()
    {
        var rig = Airlock(out var sequence);

        AddIf(rig, "Airlock Outer Door", "is not closed");

        var opens = Assert.IsType<If>(sequence.Steps[sequence.Steps.Count - 2]);
        Assert.Equal("closed", opens.Test.MemberId);
        Assert.Equal(Literal.Of(false), opens.Test.Value);
    }

    /// <summary>The row closing an if adds an otherwise once, and an otherwise if above it; the otherwise is made an otherwise if where it stands.</summary>
    [Fact]
    public void TheClosingRowAddsTheBranches()
    {
        var rig = Airlock(out var sequence);
        AddIf(rig, "Airlock Outer Door", "is closed");

        var end = rig.Editor.StepRows().Single(row => row.Kind == StepKind.End);
        Assert.Equal(new[] { rig.Texts.SequenceOtherwiseIf, rig.Texts.SequenceOtherwise }, end.Adds.Select(adder => adder.Text));
        end.Adds[1].Adds!.Execute();
        rig.Tick();

        Assert.Null(Assert.IsType<Else>(sequence.Steps[sequence.Steps.Count - 2]).Test);
        Assert.Equal(rig.Texts.SequenceOtherwiseIf, Assert.Single(rig.Editor.StepRows().Single(row => row.Kind == StepKind.End).Adds).Text);

        var otherwise = rig.Editor.StepRows().Single(row => row.Kind == StepKind.Otherwise);
        Assert.Equal(rig.Texts.SequenceIf, Assert.Single(otherwise.Adds).Text);
        otherwise.Adds[0].Adds!.Execute();
        rig.Tick();
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Tick();
        What(rig, "is open");
        rig.Editor.PressPrimary();
        rig.Tick();

        var tested = Assert.IsType<Else>(sequence.Steps[sequence.Steps.Count - 2]);
        Assert.Equal("opened", tested.Test.MemberId);
        Assert.Equal(2, rig.Editor.StepRows().Single(row => row.Kind == StepKind.End).Adds.Count);
    }

    /// <summary>An otherwise if added to an if that has an otherwise goes in above it, since the otherwise comes last.</summary>
    [Fact]
    public void AnOtherwiseIfGoesAboveTheOtherwise()
    {
        var rig = Airlock(out var sequence);
        sequence.Steps.Clear();
        sequence.Steps.Add(new If(Closed(rig)));
        sequence.Steps.Add(new Else(null!));
        sequence.Steps.Add(new EndIf());
        rig.Editor.Program.Edited();
        rig.Tick();

        rig.Editor.StepRows().Single(row => row.Kind == StepKind.End).Adds[0].Adds!.Execute();
        rig.Tick();
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Tick();
        What(rig, "is open");
        rig.Editor.PressPrimary();
        rig.Tick();

        Assert.IsType<If>(sequence.Steps[0]);
        Assert.Equal("opened", Assert.IsType<Else>(sequence.Steps[1]).Test.MemberId);
        Assert.Null(Assert.IsType<Else>(sequence.Steps[2]).Test);
        Assert.IsType<EndIf>(sequence.Steps[3]);
    }

    /// <summary>The end of a branch adds a step into that branch, where the sequence's own adders add after everything.</summary>
    [Fact]
    public void TheEndOfABranchAddsIntoIt()
    {
        var rig = Airlock(out var sequence);
        sequence.Steps.Clear();
        sequence.Steps.Add(new If(Closed(rig)));
        sequence.Steps.Add(new EndIf());
        rig.Editor.Program.Edited();
        rig.Tick();

        var end = rig.Editor.StepRows().Single(row => row.Kind == StepKind.Adds);
        end.Adds.Single(adder => adder.Text == rig.Texts.SequenceWait).Adds!.Execute();
        rig.Tick();
        rig.Editor.PressChoice(rig.Editor.Choices().First().Text);
        rig.Tick();

        Assert.IsType<If>(sequence.Steps[0]);
        Assert.IsType<Wait>(sequence.Steps[1]);
        Assert.IsType<EndIf>(sequence.Steps[2]);
    }

    /// <summary>The rows of an if are numbered as any others but the closing one, and what is in it stands further in.</summary>
    [Fact]
    public void TheStepsInAnIfStandFurtherIn()
    {
        var rig = Airlock(out var sequence);
        sequence.Steps.Clear();
        sequence.Steps.Add(new If(Closed(rig)));
        sequence.Steps.Add(new Wait(60));
        sequence.Steps.Add(new Else(null!));
        sequence.Steps.Add(new Wait(120));
        sequence.Steps.Add(new EndIf());
        sequence.Steps.Add(new Wait(30));
        rig.Editor.Program.Edited();
        rig.Tick();

        var rows = rig.Editor.StepRows();
        Assert.Equal(new[]
            {
                StepKind.If, StepKind.Wait, StepKind.Adds, StepKind.Otherwise, StepKind.Wait, StepKind.Adds, StepKind.End,
                StepKind.Wait
            },
            rows.Select(row => row.Kind));
        Assert.Equal(new[] { 0, 1, 1, 0, 1, 1, 0, 0 }, rows.Select(row => row.Depth));
        Assert.Equal(new[] { 1, 2, 0, 3, 4, 0, 0, 5 }, rows.Select(row => row.Number));
        Assert.Equal(6, rows[0].Carries);
    }

    /// <summary>Deleting an if asks first, and can keep the steps in it where they stand.</summary>
    [Fact]
    public void AnIfTakenAwayCanKeepItsSteps()
    {
        var rig = Airlock(out var sequence);
        sequence.Steps.Clear();
        sequence.Steps.Add(new If(Closed(rig)));
        sequence.Steps.Add(new Wait(60));
        sequence.Steps.Add(new Else(null!));
        sequence.Steps.Add(new Wait(120));
        sequence.Steps.Add(new EndIf());
        rig.Editor.Program.Edited();
        rig.Tick();

        rig.Editor.StepRows()[0].Deletes!.Execute();
        rig.Tick();
        rig.Editor.PressAnswer(rig.Texts.SequenceKeepSteps);
        rig.Tick();

        Assert.Equal(2, sequence.Steps.Count);
        Assert.All(sequence.Steps, step => Assert.IsType<Wait>(step));

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal(5, sequence.Steps.Count);
        Assert.IsType<If>(sequence.Steps[0]);
    }

    /// <summary>Deleting an if can take the steps in it with it.</summary>
    [Fact]
    public void AnIfDeletedTakesItsSteps()
    {
        var rig = Airlock(out var sequence);
        sequence.Steps.Clear();
        sequence.Steps.Add(new If(Closed(rig)));
        sequence.Steps.Add(new Wait(60));
        sequence.Steps.Add(new EndIf());
        sequence.Steps.Add(new Wait(30));
        rig.Editor.Program.Edited();
        rig.Tick();

        rig.Editor.StepRows()[0].Deletes!.Execute();
        rig.Tick();
        rig.Editor.PressAnswer(rig.Texts.SequenceDeleteSteps);
        rig.Tick();

        Assert.Equal(30, Assert.IsType<Wait>(Assert.Single(sequence.Steps)).Ticks);
    }

    /// <summary>An if dragged moves whole, and a step dragged across its lines moves into it.</summary>
    [Fact]
    public void AnIfMovesWholeAndAStepMovesIntoIt()
    {
        var rig = Airlock(out var sequence);
        sequence.Steps.Clear();
        sequence.Steps.Add(new Wait(30));
        sequence.Steps.Add(new If(Closed(rig)));
        sequence.Steps.Add(new Wait(60));
        sequence.Steps.Add(new EndIf());
        rig.Editor.Program.Edited();
        rig.Tick();

        var list = (StepList)rig.Editor.StepRows()[0].Parent!;

        // The if's row to the top: the wait it was under is now after the whole of it.
        list.Moves!(1, 0);
        rig.Tick();
        Assert.IsType<If>(sequence.Steps[0]);
        Assert.Equal(60, Assert.IsType<Wait>(sequence.Steps[1]).Ticks);
        Assert.IsType<EndIf>(sequence.Steps[2]);
        Assert.Equal(30, Assert.IsType<Wait>(sequence.Steps[3]).Ticks);

        // That wait above the end of the branch: into the if, after the step already there.
        list.Moves!(4, 2);
        rig.Tick();
        Assert.IsType<If>(sequence.Steps[0]);
        Assert.Equal(60, Assert.IsType<Wait>(sequence.Steps[1]).Ticks);
        Assert.Equal(30, Assert.IsType<Wait>(sequence.Steps[2]).Ticks);
        Assert.IsType<EndIf>(sequence.Steps[3]);
    }

    /// <summary>An otherwise if dragged above its if becomes the if, with its steps, and the if becomes an otherwise if.</summary>
    [Fact]
    public void ABranchDraggedAboveTheIfBecomesIt()
    {
        var rig = Chain(out var sequence, out var first, out var second);
        var list = (StepList)rig.Editor.StepRows()[0].Parent!;

        // Rows: if, wait 60, end, otherwise if, wait 120, end, otherwise, wait 30, end, closing.
        list.Moves!(3, 0);
        rig.Tick();

        Assert.Same(second, ((If)sequence.Steps[0]).Test);
        Assert.Equal(120, Assert.IsType<Wait>(sequence.Steps[1]).Ticks);
        Assert.Same(first, Assert.IsType<Else>(sequence.Steps[2]).Test);
        Assert.Equal(60, Assert.IsType<Wait>(sequence.Steps[3]).Ticks);
        Assert.Null(Assert.IsType<Else>(sequence.Steps[4]).Test);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Same(first, ((If)sequence.Steps[0]).Test);
        Assert.Equal(60, Assert.IsType<Wait>(sequence.Steps[1]).Ticks);
    }

    /// <summary>A branch dropped outside its if, or below its otherwise, stays where it was.</summary>
    [Theory]
    [InlineData(7)]
    [InlineData(6)]
    public void ABranchStaysInsideItsIfAndAboveItsOtherwise(int to)
    {
        var rig = Chain(out var sequence, out var first, out var second);
        var before = sequence.Steps.ToList();
        var list = (StepList)rig.Editor.StepRows()[0].Parent!;

        list.Moves!(3, to);
        rig.Tick();

        Assert.Equal(before, sequence.Steps);
        Assert.Same(second, ((Else)sequence.Steps[2]).Test);
    }

    /// <summary>A branch carried may land only right above a line opening a branch of its if, the if's own included, and never below the otherwise.</summary>
    [Fact]
    public void ABranchLandsOnlyBetweenBranches()
    {
        var rig = Chain(out _, out _, out _);
        var list = (StepList)rig.Editor.StepRows()[0].Parent!;

        // Above the if, or where it stands now, right above the otherwise.
        Assert.Equal(new[] { 0, 3 }, list.Landings!(3));

        // A step lands anywhere.
        Assert.Null(list.Landings!(1));
    }

    /// <summary>if A { wait 60 } otherwise if B { wait 120 } otherwise { wait 30 }, on the screen.</summary>
    static Simulation Chain(out Sequence sequence, out Comparison first, out Comparison second)
    {
        var rig = Airlock(out sequence);
        first = Closed(rig);
        second = Closed(rig);
        second.Value = Literal.Of(false);
        sequence.Steps.Clear();
        sequence.Steps.Add(new If(first));
        sequence.Steps.Add(new Wait(60));
        sequence.Steps.Add(new Else(second));
        sequence.Steps.Add(new Wait(120));
        sequence.Steps.Add(new Else(null!));
        sequence.Steps.Add(new Wait(30));
        sequence.Steps.Add(new EndIf());
        rig.Editor.Program.Edited();
        rig.Tick();
        return rig;
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

    static Comparison Closed(Simulation rig)
    {
        var door = rig.World.Entities.First(entity => entity.Name == "Airlock Outer Door");
        var test = new Comparison { MemberId = "closed", Provider = "ext", Value = Literal.Of(true) };
        test.Targets.Add(rig.Editor.Program.Declare(rig.World, door).Name);
        return test;
    }

    static void AddIf(Simulation rig, string block, string state)
    {
        rig.Editor.PressAdd(rig.Texts.SequenceIf);
        rig.Tick();
        rig.Editor.PressBlock(block);
        rig.Tick();
        What(rig, state);
        rig.Editor.PressPrimary();
        rig.Tick();
    }

    static void What(Simulation rig, string text)
    {
        rig.Editor.Pick().Stages[1].Entries().First(entry => entry.Text == text).Pick(Held.Alone, 1);
        rig.Tick();
    }

}
