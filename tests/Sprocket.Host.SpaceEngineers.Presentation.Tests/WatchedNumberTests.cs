using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Triggers;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A trigger that starts something as a number on its blocks comes to compare as it says.</summary>
public class WatchedNumberTests
{
    /// <summary>
    /// The numbers are offered after the moments, and one chosen is compared over what the trigger does, in the order the
    /// sentence says them, every column on the one page.
    /// </summary>
    [Fact]
    public void ANumberIsComparedOverWhatItDoes()
    {
        var rig = Picking("any");

        var happens = rig.Editor.Pick().Stages[1];
        var heading = happens.Entries().FindIndex(entry => entry.IsHeading && string.Equals(entry.Text, "values", StringComparison.OrdinalIgnoreCase));
        Assert.True(heading > 0, "the numbers come after the moments, under a heading of their own");

        happens.Entries().First(entry => entry.Text == "radius" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        rig.Tick();

        var strip = rig.Editor.Pick();
        Assert.NotNull(strip.Arguments);
        Assert.True(strip.Arguments!.Nested);
        Assert.Equal(3, strip.Stages.Count);
        Assert.Contains(strip.Arguments, strip.Stages[2].Within());
        Assert.DoesNotContain(strip.Arguments, strip.Children);
        Assert.Contains(strip.Stages[2], strip.Children);
    }

    /// <summary>
    /// Finished, the trigger reads as a check does, how its blocks are read and its comparison each a part of their own; and
    /// pressing the value opens the picker again on what it compares.
    /// </summary>
    [Fact]
    public void ATriggerOnANumberReadsAsACheck()
    {
        var rig = Made();
        var when = Assert.IsType<WhenTrigger>(rig.Editor.Program.Systems[0].Triggers[0]);
        Assert.Equal(Quantifier.Average, when.Quantifier);
        Assert.Equal(Relation.Below, when.Relation);
        Assert.Equal(5.0, when.Value.Number);

        var row = rig.Editor.Triggers()[0];
        Assert.Equal(new[] { "When", "the average of the", "radius", "on", "block slot 1", "is below", "5 m", "then run", "Sequence 1" },
            Said(row).Take(9));

        rig.Editor.Triggers()[0].Starts[1].Opens!.Execute();
        rig.Tick();
        var strip = rig.Editor.Pick();
        Assert.True(strip.ShowsArguments);
        Assert.StartsWith("below 5", strip.Arguments!.Form.Answer);
    }

    /// <summary>How it compares is chosen from a menu under its part, as one edit.</summary>
    [Fact]
    public void TheComparisonIsChosenFromAMenu()
    {
        var rig = Made();
        var system = rig.Editor.Program.Systems[0];

        rig.Editor.Triggers()[0].Starts[3].Opens!.Execute();
        rig.Tick();
        Assert.Contains("is above", rig.Editor.Screen.Answers.Select(answer => answer.Text));

        rig.Editor.PressAnswer("is above");
        rig.Tick();
        Assert.Equal(Relation.Above, ((WhenTrigger)system.Triggers[0]).Relation);
        Assert.Equal(5.0, ((WhenTrigger)system.Triggers[0]).Value.Number);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal(Relation.Below, ((WhenTrigger)system.Triggers[0]).Relation);
    }

    /// <summary>How several blocks are read is chosen from a menu in the words the picker offers, as one edit.</summary>
    [Fact]
    public void TheReadingIsChosenFromAMenu()
    {
        var rig = Made();
        var system = rig.Editor.Program.Systems[0];

        rig.Editor.Triggers()[0].Starts[0].Opens!.Execute();
        rig.Tick();
        Assert.Equal(new[] { "any", "all / each", "the sum of", "the average of" }, rig.Editor.Screen.Answers.Select(answer => answer.Text).ToArray());

        rig.Editor.PressAnswer("the sum of");
        rig.Tick();
        Assert.Equal(Quantifier.Sum, ((WhenTrigger)system.Triggers[0]).Quantifier);
        Assert.Equal("the sum of the", Said(rig.Editor.Triggers()[0])[1]);
    }

    /// <summary>
    /// A trigger on a value read with an option asks for the option before the comparison, and its row says it straight
    /// after the value; pressing it opens the picker again holding it.
    /// </summary>
    [Fact]
    public void ATriggerOnAValueReadWithAnOptionAsksForIt()
    {
        var rig = Simulation.Fresh();
        var by = Field.Called("by");
        by.Entry = FieldInput.Decimal;
        rig.Editor.World.Entities.First(entity => entity.Name == "Airlock Light").Kind.WithValue("brighter", "brighter",
            OwnValue.Number<Sandbox.ModAPI.IMyTerminalBlock, double>(arguments => arguments[0].Real, (_, more) => more).Taking(by))
            .Looked("A test value.");
        rig.Editor.NewSystem("Airlock");
        rig.Editor.PressStart(SentenceText.WhenWord);
        rig.Editor.PressBlock("Airlock Light");
        rig.Tick();

        rig.Editor.Pick().Stages[1].Entries().First(entry => entry.Text == "brighter" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        rig.Tick();
        Assert.Equal("by", ((PickPage)rig.Editor.Current).Fields[0].Name);

        rig.Editor.Typed("2 above 5");
        rig.Tick();
        if (rig.Editor.Current is not LogicSystemPage)
            rig.Editor.Back();

        rig.Tick();

        var when = Assert.IsType<WhenTrigger>(rig.Editor.Program.Systems[0].Triggers[0]);
        Assert.Equal(new[] { 2.0 }, when.Arguments.Select(argument => argument.Number).ToArray());
        Assert.Equal(Relation.Above, when.Relation);
        Assert.Equal(5.0, when.Value.Number);

        var row = rig.Editor.Triggers()[0];
        Assert.Equal(new[] { "When", "the", "brighter", "2", "on", "any in", "block slot 1", "is above", "5" }, Said(row).Take(9));

        row.Starts.OfType<ArgumentsSlot>().First().Opens!.Execute();
        rig.Tick();
        var giving = Assert.IsType<LogicSystemPage>(rig.Editor.Current).Giving!;
        Assert.Equal("2.0", giving.Form.Answer);
        giving.Form.Fields[0].Text = "3";
        giving.Done();
        rig.Tick();
        var now = Assert.IsType<WhenTrigger>(rig.Editor.Program.Systems[0].Triggers[0]);
        Assert.Equal(new[] { 3.0 }, now.Arguments.Select(argument => argument.Number).ToArray());
        Assert.Equal(5.0, now.Value.Number);
    }

    /// <summary>What a trigger on a number compares with is changed in a panel under it, as one change.</summary>
    [Fact]
    public void WhatATriggerComparesWithIsChangedInItsPanel()
    {
        var rig = Made();
        var page = Assert.IsType<LogicSystemPage>(rig.Editor.Current);
        rig.Editor.Triggers()[0].Starts.OfType<ArgumentsSlot>().Single(part => part.Holds == ArgumentRole.Checked).Opens!.Execute();
        rig.Tick();

        var giving = page.Giving;
        Assert.NotNull(giving);
        Assert.Equal("5.0", giving.Form.Fields[0].Text);
        giving.Form.Fields[0].Text = "7";
        giving.Done();
        rig.Tick();

        Assert.Null(page.Giving);
        var system = rig.Editor.Program.Systems[0];
        var when = Assert.IsType<WhenTrigger>(system.Triggers[0]);
        Assert.Equal(7.0, when.Value.Number);
        Assert.Equal(Relation.Below, when.Relation);
        Assert.Equal(Quantifier.Average, when.Quantifier);

        rig.Editor.Screen.Undo!.Execute();
        rig.Tick();
        Assert.Equal(5.0, Assert.IsType<WhenTrigger>(system.Triggers[0]).Value.Number);
    }

    /// <summary>A trigger missing what its value is read with says so, naming it, and so does one given what it can't take.</summary>
    [Fact]
    public void ATriggerSaysWhichOptionIsMissingOrWrong()
    {
        var rig = Simulation.Fresh();
        var by = Field.Called("by");
        by.Entry = FieldInput.Whole;
        rig.Editor.World.Entities.First(entity => entity.Name == "Airlock Light").Kind.WithValue("brighter", "brighter",
                OwnValue.Number<Sandbox.ModAPI.IMyTerminalBlock, double>(arguments => arguments[0].Real, (_, more) => more).Taking(by))
            .Looked("A test value.");
        rig.Editor.NewSystem("Airlock");
        rig.Editor.PressStart(SentenceText.WhenWord);
        rig.Editor.PressBlock("Airlock Light");
        rig.Tick();
        rig.Editor.Pick().Stages[1].Entries().First(entry => entry.Text == "brighter" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        rig.Tick();
        rig.Editor.Typed("2 above 5");
        rig.Tick();
        if (rig.Editor.Current is not LogicSystemPage)
            rig.Editor.Back();

        rig.Tick();

        var system = rig.Editor.Program.Systems[0];
        var sequence = system.Sequences[0];
        sequence.Steps.Add(new Wait(Duration.OfTicks(1)));
        var when = Assert.IsType<WhenTrigger>(system.Triggers[0]);

        when.Arguments.Clear();
        var missing = SequenceSummary.Checked(rig.Editor.World, rig.Editor.Runner.Bindings, sequence, system.First);
        Assert.Equal(SequenceProblem.Unfilled, missing.Problem);
        Assert.Equal(SequenceFault.Trigger, missing.Step);
        Assert.Equal("This trigger is missing its by.", SequenceText.Problem(rig.Editor.Texts, missing, sequence));

        when.Arguments.Add(Literal.Of("three"));
        var wrong = SequenceSummary.Checked(rig.Editor.World, rig.Editor.Runner.Bindings, sequence, system.First);
        Assert.Equal(SequenceProblem.Mistyped, wrong.Problem);
        Assert.Equal("This trigger's by doesn't fit.", SequenceText.Problem(rig.Editor.Texts, wrong, sequence));
    }

    /// <summary>A new logic system's trigger picker, with both of the airlock's lights picked and read as <paramref name="reading" /> says.</summary>
    static Simulation Picking(string reading)
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewSystem("Airlock");
        rig.Editor.PressStart(SentenceText.WhenWord);
        rig.Editor.PressBlock("Airlock Light");
        rig.Tick();
        rig.Editor.TickBlock("Cockpit Light");
        rig.Tick();

        var reads = rig.Editor.Pick().Stages[0].Options.OfType<ListField>().Single();
        while (reads.Text != reading)
        {
            ((IInputTarget)reads).Text('t');
            rig.Tick();
        }

        return rig;
    }

    /// <summary>A logic system started when the average radius of the airlock's lights is below 5, made in the picker, on its page.</summary>
    static Simulation Made()
    {
        var rig = Picking("the average of");
        rig.Editor.Pick().Stages[1].Entries().First(entry => entry.Text == "radius" && !entry.IsHeading).Pick(HeldModifier.Alone, 1);
        rig.Tick();
        rig.Editor.Typed("below 5");
        rig.Tick();
        if (rig.Editor.Current is not LogicSystemPage)
            rig.Editor.Back();

        rig.Tick();
        return rig;
    }

    /// <summary>The words of the row's sentence as they stand, the hidden ones left out.</summary>
    static string[] Said(TriggerRow row) =>
        row.Within().OfType<Flow>().Single().Children
            .Where(part => part.IsVisible)
            .Select(part => part is SlotChip chip ? chip.Text : ((Label)part).Text)
            .ToArray();
}
