using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A row of a list keeps its words in the same places whatever it has to say.</summary>
public class StatusRowTests
{
    /// <summary>A row marked as wrong puts its name where an unmarked row does, since the name's line is as tall as the mark.</summary>
    [Fact]
    public void TheMarkMovesNoName()
    {
        var (marked, plain) = Laid();

        Assert.Equal(Word(plain, "Sequence 2").Bounds.Y - plain.Bounds.Y, Word(marked, "Sequence 1").Bounds.Y - marked.Bounds.Y);
    }

    /// <summary>What a row is doing starts where what is wrong with another does, under the name.</summary>
    [Fact]
    public void TheStatusStandsWhereAProblemDoes()
    {
        var (marked, plain) = Laid();

        Assert.Equal(Word(marked, "Nothing starts this yet.").Bounds.X, Word(plain, "switched off").Bounds.X);
    }

    /// <summary>A row stands the same height whatever it has to say, and a way to add something stands lower, having nothing under its name.</summary>
    [Fact]
    public void EveryRowOfAKindIsOneHeight()
    {
        var texts = Simulation.Fresh().Texts;
        var quiet = new StatusRow { Summary = "Sequence 3" };
        var adds = new StatusRow { Summary = "add another", IsAction = true };
        quiet.Show(texts);
        adds.Show(texts);

        var (marked, plain) = Laid();
        _ = new Easel(new VStack { Children = { quiet, adds } }, 1200f, 400f);

        Assert.Equal(68f, marked.Bounds.Height);
        Assert.Equal(68f, plain.Bounds.Height);
        Assert.Equal(68f, quiet.Bounds.Height);
        Assert.Equal(48f, adds.Bounds.Height);
    }

    /// <summary>A row with nothing to say under its name has the name on its middle.</summary>
    [Fact]
    public void ANameAloneStandsOnTheMiddle()
    {
        var texts = Simulation.Fresh().Texts;
        var quiet = new StatusRow { Summary = "test" };
        quiet.Show(texts);
        _ = new Easel(new VStack { Children = { quiet } }, 1200f, 400f);

        var name = Word(quiet, "test");
        Assert.Equal(quiet.Bounds.Y + quiet.Bounds.Height / 2f, name.Bounds.Y + name.Bounds.Height / 2f, 1);
    }

    static (StatusRow Marked, StatusRow Plain) Laid()
    {
        var texts = Simulation.Fresh().Texts;
        var marked = new StatusRow { Summary = "Sequence 1", Problem = "Nothing starts this yet.", ProblemIsWarning = true, Status = "ready" };
        var plain = new StatusRow { Summary = "Sequence 2", Status = "switched off" };
        marked.Show(texts);
        plain.Show(texts);

        _ = new Easel(new VStack { Children = { marked, plain } }, 1200f, 400f);
        return (marked, plain);
    }

    static Label Word(Element row, string text) =>
        row.Within().OfType<Label>().Single(label => label.IsVisible && label.Text == text);
}
