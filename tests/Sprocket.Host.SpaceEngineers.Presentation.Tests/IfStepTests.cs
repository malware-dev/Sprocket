using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
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
        Assert.Equal("status", opens.Test.MemberId);
        Assert.Equal("ext", opens.Test.Provider);
        Assert.Equal(Relation.Is, opens.Test.Relation);
        Assert.Equal("closed", opens.Test.Value.Text);
    }

    /// <summary>A state's other side is a choice of its own, and makes a test for the blocks not being in it.</summary>
    [Fact]
    public void NotBeingInAStateIsAChoiceOfItsOwn()
    {
        var rig = Airlock(out var sequence);

        AddIf(rig, "Airlock Outer Door", "is not closed");

        var opens = Assert.IsType<If>(sequence.Steps[sequence.Steps.Count - 2]);
        Assert.Equal("status", opens.Test.MemberId);
        Assert.Equal(Relation.IsNot, opens.Test.Relation);
        Assert.Equal("closed", opens.Test.Value.Text);
    }

    /// <summary>The row closing an if adds an otherwise once, and an otherwise if above it; the otherwise is made an otherwise if where it stands.</summary>
    [Fact]
    public void TheClosingRowAddsTheBranches()
    {
        var rig = Airlock(out var sequence);
        AddIf(rig, "Airlock Outer Door", "is closed");

        var end = rig.Editor.StepRows().Single(row => row.Kind == StepKind.End);
        var branches = Assert.Single(end.Adds).Choices!;
        Assert.Equal(new[] { SentenceText.OtherwiseIfWords, SentenceText.OtherwiseWord }, Offered(branches));
        branches[1].Adds!.Execute();
        rig.Tick();

        Assert.Null(Assert.IsType<Else>(sequence.Steps[sequence.Steps.Count - 2]).Test);
        Assert.Equal(new[] { SentenceText.OtherwiseIfWords },
            Offered(Assert.Single(rig.Editor.StepRows().Single(row => row.Kind == StepKind.End).Adds).Choices!));

        var otherwise = rig.Editor.StepRows().Single(row => row.Kind == StepKind.Otherwise);
        var test = Assert.Single(Assert.Single(otherwise.Adds).Choices!);
        Assert.Equal(SentenceText.IfWord, test.Text);
        test.Adds!.Execute();
        rig.Tick();
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Tick();
        Check(rig, "is open");

        var tested = Assert.IsType<Else>(sequence.Steps[sequence.Steps.Count - 2]);
        Assert.Equal("status", tested.Test.MemberId);
        Assert.Equal(Literal.Name("open"), tested.Test.Value);
        Assert.Equal(2, Offered(Assert.Single(rig.Editor.StepRows().Single(row => row.Kind == StepKind.End).Adds).Choices!).Count);
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

        rig.Editor.StepRows().Single(row => row.Kind == StepKind.End).Adds[0].Choices![0].Adds!.Execute();
        rig.Tick();
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Tick();
        Check(rig, "is open");

        Assert.IsType<If>(sequence.Steps[0]);
        var tested = Assert.IsType<Else>(sequence.Steps[1]).Test;
        Assert.Equal("status", tested.MemberId);
        Assert.Equal(Literal.Name("open"), tested.Value);
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
        end.Adds.Single().Choices!.Single(adder => adder.Text == SentenceText.WaitWord).Adds!.Execute();
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

    /// <summary>How an if compares is a part of its own, chosen from a menu under it of the comparisons its value takes, as one edit.</summary>
    [Fact]
    public void TheComparisonIsChosenFromAMenuUnderIt()
    {
        var rig = Airlock(out var sequence);
        sequence.Steps.Clear();
        sequence.Steps.Add(new If(Closed(rig)));
        sequence.Steps.Add(new EndIf());
        rig.Editor.Program.Edited();
        rig.Tick();

        var relation = rig.Editor.StepRows()[0].Slots.OfType<ArgumentsSlot>().First();
        Assert.Equal(new[] { "is" }, relation.Filled);
        relation.Opens!.Execute();
        rig.Tick();
        Assert.Equal(new[] { "is", "is not" }, rig.Editor.Screen.Answers.Select(answer => answer.Text).ToArray());

        rig.Editor.PressAnswer("is not");
        rig.Tick();
        Assert.Equal(Relation.IsNot, ((If)sequence.Steps[0]).Test.Relation);
        Assert.Equal("closed", ((If)sequence.Steps[0]).Test.Value.Text);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal(Relation.Is, ((If)sequence.Steps[0]).Test.Relation);
    }

    /// <summary>Pressing the comparison hangs its menu under it and keeps it there, though the menu asks nothing in words.</summary>
    [Fact]
    public void PressingTheComparisonHangsItsMenu()
    {
        var screen = Screen.Holding("sprocket 1 alpha 17\n\n"
            + "let door1 = block 'Door/LargeBlockDoor' [ 2 ]\n\n"
            + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock\n{\n"
            + "    sequence 1 {\n"
            + "        if all ext.status on [ door1 ] == closed then {\n"
            + "        }\n"
            + "    }\n"
            + "}\n");
        var editor = screen.Editor;
        var system = editor.Program.Systems[0];
        system.Enabled = false;
        editor.Program.Edited();
        editor.Push(new SequencePage(editor, system.Sequences[0]));
        screen.Run(5);

        var at = screen.Text("is").At;
        var point = Screen.Ui(at.X + 4f, at.Y + 8f);
        screen.Scene.MoveTo(point);
        screen.Scene.Press(point, 1);
        screen.Run(1);

        Assert.True(editor.Screen.HasQuestion, "the menu put itself away");
        Assert.Equal(new[] { "is", "is not" }, editor.Screen.Answers.Select(answer => answer.Text).ToArray());
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

    /// <summary>
    /// A step carried through an if is offered every gap but the one just under the end of a branch, which is the same place
    /// as the gap above that end, and so never opens a space it cannot be dropped in.
    /// </summary>
    [Fact]
    public void AStepIsNotOfferedTheGapUnderTheEndOfABranch()
    {
        var rig = Chain(out _, out _, out _);
        var list = (StepList)rig.Editor.StepRows()[0].Parent!;

        // Rows: if, wait 60, end, otherwise if, wait 120, end, otherwise, wait 30, end, closing. Carrying wait 120, the
        // others are counted without it: the gaps under the three ends are 3, 5 and 8.
        Assert.Equal(new[] { 0, 1, 2, 4, 6, 7, 9 }, list.Landings!(4));
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
        var rig = Chain(out var sequence, out _, out var second);
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

    /// <summary>The words of those of <paramref name="choices" /> that may be added now.</summary>
    static List<string> Offered(IList<AddOption> choices) => choices.Where(choice => choice.Adds != null).Select(choice => choice.Text).ToList();

    /// <summary>Changing the blocks of a test written in the script keeps how it compares and that it is a sum.</summary>
    [Fact]
    public void ChangingATestsBlocksKeepsHowItCompares()
    {
        var rig = Airlock(out var sequence);
        var beacon = rig.World.Entities.First(entity => entity.Name == "Warning Beacon");
        var test = new Comparison
        {
            MemberId = "Radius", Provider = "", Quantifier = Quantifier.Sum, Relation = Relation.AtLeast, Value = Literal.Of(7.0)
        };

        test.Targets.Add(rig.Editor.Program.Declare(rig.World, beacon).Name);
        sequence.Steps.Clear();
        sequence.Steps.Add(new If(test));
        sequence.Steps.Add(new EndIf());
        rig.Editor.Program.Edited();
        rig.Tick();

        rig.Editor.StepRows()[0].Slots[1].Opens!.Execute();
        rig.Tick();
        rig.Editor.PressPrimary();
        rig.Tick();

        var now = Assert.IsType<If>(sequence.Steps[0]).Test;
        Assert.Equal("Radius", now.MemberId);
        Assert.Equal(Quantifier.Sum, now.Quantifier);
        Assert.Equal(Relation.AtLeast, now.Relation);
        Assert.Equal(Literal.Of(7.0), now.Value);
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
        var test = new Comparison { MemberId = "status", Provider = "ext", Value = Literal.Name("closed") };
        test.Targets.Add(rig.Editor.Program.Declare(rig.World, door).Name);
        return test;
    }

    static void AddIf(Simulation rig, string block, string state)
    {
        rig.Editor.PressAdd(SentenceText.IfWord);
        rig.Tick();
        rig.Editor.PressBlock(block);
        rig.Tick();
        Check(rig, state);
    }

    /// <summary>Checks the door for the state <paramref name="state" /> says, "is open" or "is not open", and finishes the check.</summary>
    static void Check(Simulation rig, string state)
    {
        var not = state.StartsWith("is not ", StringComparison.Ordinal);
        var word = state.Substring(not ? 7 : 3);
        What(rig, word);
        rig.Editor.Typed(not ? "is-not" : "is");
        rig.Tick();
    }

    static void What(Simulation rig, string text)
    {
        rig.Editor.Pick().Stages[1].Entries().First(entry => entry.Text == text && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        rig.Tick();
    }

}
