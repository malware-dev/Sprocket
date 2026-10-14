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
        rig.Editor.PressAdd(rig.Texts.SequenceIf);
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

        What(rig).Entries().First(entry => entry.Text == "open" && !entry.IsHeading).Pick(Held.Alone, 1);
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
        rig.Editor.PressAdd(rig.Texts.SequenceIf);
        rig.Tick();
        rig.Editor.TickBlock("Airlock Inner Door");
        rig.Editor.TickBlock("Airlock Outer Door");
        rig.Tick();
        rig.Tick();

        What(rig).Entries().First(entry => entry.Text == "open" && !entry.IsHeading).Pick(Held.Alone, 1);
        rig.Tick();
        rig.Editor.Typed("is");
        rig.Tick();

        var opens = sequence.Steps.OfType<If>().Single();
        var through = rig.Editor.StandIn(sequence.System);
        Assert.Equal("if Airlock Inner Door or Airlock Outer Door is open", SequenceText.Step(rig.Editor.Runner.Bindings, sequence, opens, through));

        opens.Test.Quantifier = Quantifier.All;
        Assert.Equal("if Airlock Inner Door and Airlock Outer Door are open", SequenceText.Step(rig.Editor.Runner.Bindings, sequence, opens, through));
    }

    /// <summary>Any other value is named in the check, and a number is offered more than sameness.</summary>
    [Fact]
    public void AValueIsNamedAndANumberMeasured()
    {
        var rig = Airlock();
        var sequence = rig.Editor.Program.Systems[0].Sequences[0];
        Open(rig);

        What(rig).Entries().First(entry => entry.Text == "power" && !entry.IsHeading).Pick(Held.Alone, 1);
        rig.Tick();
        var relation = ((PickPage)rig.Editor.Current).Fields[0];
        Assert.Equal(new[] { "is", "is not" }, relation.Words);
        rig.Editor.Typed("is on");
        rig.Tick();

        var opens = sequence.Steps.OfType<If>().Single();
        Assert.Equal("if Airlock Outer Door power is on", SequenceText.Step(rig.Editor.Runner.Bindings, sequence, opens, null));
    }

    /// <summary>The signals that start a trigger are one list, with no link to another view.</summary>
    [Fact]
    public void ASignalHasOneList()
    {
        var rig = Airlock();
        rig.Editor.Push(new LogicSystemPage(rig.Editor, rig.Editor.Program.Systems[0]));
        rig.Tick();
        rig.Editor.PressStart(rig.Editor.Texts.SystemWhen);
        rig.Tick();
        rig.Editor.PressBlock("Airlock Outer Door");
        rig.Tick();

        Assert.All(rig.Editor.Pick().Stages, stage => Assert.Empty(stage.Views));
    }

    /// <summary>Adds an if, and selects the airlock's outer door for it.</summary>
    static void Open(Simulation rig)
    {
        rig.Editor.PressAdd(rig.Texts.SequenceIf);
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
