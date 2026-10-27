using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Sim.Machine;
using Xunit;
using Lumel.Images;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sentences;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A trigger on its logic system's page, drawn as the sentence it reads as.</summary>
public class TriggerRowTests
{
    /// <summary>The row says what starts it and then what it does, in order, the parts somebody chose sitting among the words.</summary>
    [Fact]
    public void ATriggerReadsAsItsSentence()
    {
        var sim = Built();

        Assert.Equal(new[] { "When", "slot 1", "button 1 is pressed", "then run", "Sequence 1" }, Said(sim.Editor.Triggers()[0]));
    }

    /// <summary>Each part is pressed by its own command, the one that opens the question it was answered with.</summary>
    [Fact]
    public void EachPartIsPressedByItsOwnCommand()
    {
        var sim = Built();
        var row = sim.Editor.Triggers()[0];
        var chips = row.Within().OfType<SlotChip>().ToList();

        Assert.Equal(3, chips.Count);
        Assert.Same(row.Starts[0].Opens, chips[0].Command);
        Assert.Same(row.Starts[1].Opens, chips[1].Command);
        Assert.Same(row.Does[0].Opens, chips[2].Command);
    }

    /// <summary>A trigger with something wrong is washed and says what, on its own line under the sentence.</summary>
    [Fact]
    public void ABrokenTriggerIsWashedAndSaysWhy()
    {
        var sim = Built();
        var row = sim.Editor.Triggers()[0];

        row.Problem = "Nothing can start this.";
        row.Show(sim.World, sim.Texts);
        row.Restyle();

        Assert.Equal(Palette.ProblemWash, row.Background);
        Assert.Contains(row.Within().OfType<Label>(), label => label.IsVisible && label.Text == "Nothing can start this.");
    }

    /// <summary>The badge saying what it watches for is true shows only while it is.</summary>
    [Fact]
    public void TheBadgeShowsOnlyWhileTheSignalIsTrue()
    {
        var sim = Built();
        var row = sim.Editor.Triggers()[0];

        row.IsLiveNow = true;
        row.Show(sim.World, sim.Texts);
        Assert.Contains(sim.Texts.TriggerLive, Said(row));

        row.IsLiveNow = false;
        row.Show(sim.World, sim.Texts);
        Assert.DoesNotContain(sim.Texts.TriggerLive, Said(row));
    }

    /// <summary>A sentence that loses a part keeps the chips it still has, and moves no words about.</summary>
    [Fact]
    public void ASentenceThatLosesAPartKeepsTheChipsItHas()
    {
        var sim = Built();
        var row = sim.Editor.Triggers()[0];
        var first = row.Within().OfType<SlotChip>().First();

        row.Starts.RemoveAt(1);
        row.Show(sim.World, sim.Texts);

        Assert.Same(first, row.Within().OfType<SlotChip>().First());
        Assert.Equal(new[] { "When", "slot 1", "then run", "Sequence 1" }, Said(row));
    }

    /// <summary>A trigger nobody may delete from here has no bin.</summary>
    [Fact]
    public void ATriggerThatCannotBeDeletedHasNoBin()
    {
        var sim = Built();
        var row = sim.Editor.Triggers()[0];
        var bin = row.Within().OfType<Glyph>().Single(glyph => glyph.Symbol == Symbols.Delete);
        Assert.True(bin.IsVisible);

        row.Deletes = null;
        row.Show(sim.World, sim.Texts);

        Assert.False(bin.IsVisible);
    }

    /// <summary>A logic system with a trigger started by the airlock's button, on its own page.</summary>
    static Simulation Built()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.Back();
        sim.Tick();
        return sim;
    }

    /// <summary>The words of the row's sentence as they stand, the hidden ones left out.</summary>
    static string[] Said(TriggerRow row) =>
        row.Within().OfType<Flow>().Single().Children
            .Where(part => part.IsVisible)
            .Select(part => part is SlotChip chip ? chip.Text : ((Label)part).Text)
            .ToArray();
}
