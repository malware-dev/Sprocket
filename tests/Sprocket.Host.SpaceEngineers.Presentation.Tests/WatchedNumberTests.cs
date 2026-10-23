using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
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
        Assert.Equal(new[] { "When", "the average of", "radius", "on", "slot 1", "is below", "5 m", "then run", "Sequence 1" },
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

    /// <summary>How several blocks are read is chosen from a menu in the sentence's own words, as one edit.</summary>
    [Fact]
    public void TheReadingIsChosenFromAMenu()
    {
        var rig = Made();
        var system = rig.Editor.Program.Systems[0];

        rig.Editor.Triggers()[0].Starts[0].Opens!.Execute();
        rig.Tick();
        Assert.Equal(new[] { "any in", "all in", "the sum of", "the average of" }, rig.Editor.Screen.Answers.Select(answer => answer.Text).ToArray());

        rig.Editor.PressAnswer("the sum of");
        rig.Tick();
        Assert.Equal(Quantifier.Sum, ((WhenTrigger)system.Triggers[0]).Quantifier);
        Assert.Equal("the sum of", Said(rig.Editor.Triggers()[0])[1]);
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
    /// <summary>Every part of what starts a trigger on a number says what pressing it does: how its blocks are read, the value, the blocks, the comparison and the number.</summary>
    [Fact]
    public void EveryPartOfANumberTriggerHasATooltip()
    {
        var rig = Made();
        rig.Tick();

        var row = rig.Editor.Triggers()[0];
        var texts = rig.Editor.Texts;
        var tips = row.Starts.Select(part => row.ChipOf(part)!.Tip?.Text).ToArray();
        Assert.All(tips, Assert.NotNull);
        Assert.Equal(texts.SequenceReadAbout, tips[0]);
        Assert.Equal(texts.PartWatchedAbout, tips[1]);
        Assert.Equal(texts.SequenceComparedAbout, tips[3]);
        Assert.Equal(texts.SequenceCheckedAbout, tips[4]);
    }

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
