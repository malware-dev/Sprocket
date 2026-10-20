using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>What a check can read is offered in both views, as what a step can do is; what starts a trigger has the one list.</summary>
public class CheckViewTests
{
    /// <summary>
    /// A check's organized view lists the block's states under headings, and its terminal view the values the block declares,
    /// under the terminal's own names; the organized view comes back as it was.
    /// </summary>
    [Fact]
    public void ACheckHasAnOrganizedAndATerminalView()
    {
        var rig = Airlock();
        rig.Editor.PressAdd(SentenceText.IfWord);
        rig.Tick();
        rig.Editor.PressBlock("Airlock Outer Door");
        rig.Tick();

        var what = What(rig);
        Assert.Contains(what.Entries(), entry => entry.Text == "closed");
        Assert.Contains(what.Entries(), entry => entry.IsHeading);

        what.PressView("Terminal");
        rig.Tick();

        what = What(rig);
        Assert.DoesNotContain(what.Entries(), entry => entry.Text == "closed");
        Assert.Contains(what.Entries(), entry => !entry.IsHeading);

        what.PressView("Organized");
        rig.Tick();

        Assert.Contains(What(rig).Entries(), entry => entry.Text == "closed");
    }

    /// <summary>A check lists the states of the block's status first, under its own heading, then the other values under theirs.</summary>
    [Fact]
    public void TheStatusComesFirstAsItsStates()
    {
        var rig = Airlock();
        Open(rig);

        var entries = What(rig).Entries();
        Assert.True(entries[0].IsHeading);
        Assert.Equal(rig.Texts.PickStatus, entries[0].Text);
        var states = entries.Skip(1).TakeWhile(entry => !entry.IsHeading).Select(entry => entry.Text).ToList();
        Assert.Contains("open", states);
        Assert.Contains("closed", states);
        Assert.Contains("working", states);
        Assert.DoesNotContain(entries, entry => entry.Text.StartsWith("is not", StringComparison.Ordinal));
    }

    /// <summary>A state of the status reads as the block itself, and is not as the comparison chosen beside it.</summary>
    [Fact]
    public void AStateReadsAsTheBlock()
    {
        var rig = Airlock();
        var sequence = rig.Editor.Program.Systems[0].Sequences[0];
        Open(rig);

        What(rig).Entries().First(entry => entry.Text == "open" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        rig.Tick();
        rig.Editor.Typed("is-not");
        rig.Tick();

        var opens = sequence.Steps.OfType<If>().Single();
        Assert.Equal("if Airlock Outer Door is not open", SequenceText.Step(rig.Editor.Runner.Bindings, sequence, opens, null));
    }

    /// <summary>A check on several blocks reads their state as any one of them has it, and in the plural where all of them must.</summary>
    [Fact]
    public void SeveralBlocksAreInAState()
    {
        var rig = Airlock();
        var sequence = rig.Editor.Program.Systems[0].Sequences[0];
        rig.Editor.PressAdd(SentenceText.IfWord);
        rig.Tick();
        rig.Editor.TickBlock("Airlock Inner Door");
        rig.Editor.TickBlock("Airlock Outer Door");
        rig.Tick();
        rig.Tick();

        What(rig).Entries().First(entry => entry.Text == "open" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        rig.Tick();
        rig.Editor.Typed("is");
        rig.Tick();

        var opens = sequence.Steps.OfType<If>().Single();
        var through = rig.Editor.StandIn(sequence.System);
        Assert.Equal("if any Airlock Inner Door or Airlock Outer Door is open", SequenceText.Step(rig.Editor.Runner.Bindings, sequence, opens, through));

        opens.Test.Quantifier = Quantifier.All;
        Assert.Equal("if all Airlock Inner Door and Airlock Outer Door are open", SequenceText.Step(rig.Editor.Runner.Bindings, sequence, opens, through));
    }

    /// <summary>Any other value is named in the check, and a number is offered more than sameness.</summary>
    [Fact]
    public void AValueIsNamedAndANumberMeasured()
    {
        var rig = Airlock();
        var sequence = rig.Editor.Program.Systems[0].Sequences[0];
        Open(rig);

        What(rig).Entries().First(entry => entry.Text == "power" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        rig.Tick();
        var relation = ((PickPage)rig.Editor.Current).Fields[0];
        Assert.Equal(new[] { "is", "is not" }, relation.Words);
        rig.Editor.Typed("is on");
        rig.Tick();

        var opens = sequence.Steps.OfType<If>().Single();
        Assert.Equal("if power on Airlock Outer Door is on", SequenceText.Step(rig.Editor.Runner.Bindings, sequence, opens, null));
    }

    /// <summary>What can start a trigger has the two views every list of members has: the organized one lists what Sprocket vouches for, and the terminal view leaves it out.</summary>
    [Fact]
    public void WhatHappensHasBothViews()
    {
        var rig = Airlock();
        rig.Editor.Push(new LogicSystemPage(rig.Editor, rig.Editor.Program.Systems[0]));
        rig.Tick();
        rig.Editor.PressStart(SentenceText.WhenWord);
        rig.Tick();
        rig.Editor.PressBlock("Airlock Outer Door");
        rig.Tick();

        var what = rig.Editor.Pick().Stages.Single(stage => stage.Views.Count > 0);
        Assert.Contains(what.Entries(), entry => entry.Text == "finishes opening");

        what.PressView("Terminal");
        rig.Tick();
        Assert.DoesNotContain(rig.Editor.Pick().Stages.SelectMany(stage => stage.Entries()), entry => entry.Text == "finishes opening");
        rig.Editor.Pick().Stages.Single(stage => stage.Views.Count > 0).PressView("Organized");
    }

    /// <summary>A toolbar nothing knows the slots of, such as a vent's, is one signal of the terminal view, never of the organized one.</summary>
    [Fact]
    public void AToolbarNothingKnowsIsInTheTerminalViewAlone()
    {
        var rig = Airlock();
        rig.Editor.Push(new LogicSystemPage(rig.Editor, rig.Editor.Program.Systems[0]));
        rig.Tick();
        rig.Editor.PressStart(SentenceText.WhenWord);
        rig.Tick();
        rig.Editor.PressBlock("Airlock Vent");
        rig.Tick();

        var what = rig.Editor.Pick().Stages.Single(stage => stage.Views.Count > 0);
        Assert.Contains(what.Entries(), entry => entry.Text == "finishes filling");
        Assert.DoesNotContain(what.Entries(), entry => entry.Text.Contains("toolbar"));

        what.PressView("Terminal");
        rig.Tick();
        var entries = rig.Editor.Pick().Stages.SelectMany(stage => stage.Entries()).Where(entry => !entry.IsHeading).ToList();
        Assert.Single(entries, entry => entry.Text == "runs a toolbar slot");
        Assert.DoesNotContain(entries, entry => entry.Text == "finishes filling");
        rig.Editor.Pick().Stages.Single(stage => stage.Views.Count > 0).PressView("Organized");
    }

    /// <summary>
    /// How several blocks are read is chosen from a list in the sentence's own words; a sum or an average lists numbers alone and
    /// says why, lets go of a value that is no number, and reads in the sentence as the list says it.
    /// </summary>
    [Fact]
    public void ATotalListsOnlyNumbers()
    {
        var rig = Airlock();
        var sequence = rig.Editor.Program.Systems[0].Sequences[0];
        Open(rig);
        rig.Editor.TickBlock("Airlock Inner Door");
        rig.Tick();
        What(rig).Entries().First(entry => entry.Text == "power" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        rig.Tick();

        var reads = rig.Editor.Pick().Stages[0].Options.OfType<ListField>().Single();
        Assert.Equal("any", reads.Text);
        ((IInputTarget)reads).Text('t');
        rig.Tick();
        Assert.Equal("the sum of", reads.Text);

        var listed = What(rig).Entries().Where(entry => !entry.IsHeading).Select(entry => entry.Text).ToList();
        Assert.DoesNotContain("power", listed);
        Assert.NotEmpty(listed);
        Assert.Equal(rig.Texts.PickSumNote, What(rig).Note);
        Assert.Empty(((PickPage)rig.Editor.Current).Fields);

        ((IInputTarget)reads).Text('t');
        rig.Tick();
        Assert.Equal("the average of", reads.Text);
        Assert.Equal(rig.Texts.PickAverageNote, What(rig).Note);

        var number = listed[0];
        What(rig).Entries().First(entry => entry.Text == number && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        rig.Tick();
        rig.Editor.Typed("above 50");
        rig.Tick();

        var opens = sequence.Steps.OfType<If>().Single();
        Assert.Equal(Quantifier.Average, opens.Test.Quantifier);
        Assert.StartsWith("if the average of " + number + " on ", SequenceText.Step(rig.Editor.Runner.Bindings, sequence, opens, rig.Editor.StandIn(sequence.System)));
    }

    /// <summary>Adds an if, and selects the airlock's outer door for it.</summary>
    static void Open(Simulation rig)
    {
        rig.Editor.PressAdd(SentenceText.IfWord);
        rig.Tick();
        rig.Editor.PressBlock("Airlock Outer Door");
        rig.Tick();
    }

    /// <summary>The column of the selector that offers what is read or done, the one with a link to another view.</summary>
    static StageColumn What(Simulation rig)
    {
        return rig.Editor.Pick().Stages.Single(stage => stage.Views.Count > 0);
    }

    /// <summary>An airlock logic system, switched off and open on its first sequence.</summary>
    static Simulation Airlock()
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Tick();

        var system = rig.Editor.Program.Systems[0];
        system.Enabled = false;
        rig.Editor.Program.Edited();
        rig.Editor.Push(new SequencePage(rig.Editor, system.Sequences[0]));
        rig.Tick();
        return rig;
    }
}
