using Sprocket.Host.SpaceEngineers.Ui.Rich;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Text with named slots: read into a document once, or filled as plain text.</summary>
public class TemplateTests
{
    /// <summary>A slot becomes a run of its own, set apart as the words around it would be.</summary>
    [Fact]
    public void ASlotIsARunOfItsOwn()
    {
        var runs = Template.Read("Made from **{name}** blueprint").Blocks[0].Runs;

        Assert.Equal("Made from ", runs[0].Text);
        Assert.Equal("name", runs[1].Slot);
        Assert.Equal(RunKind.Strong, runs[1].Kind);
        Assert.Equal(" blueprint", runs[2].Text);
    }

    /// <summary>Stars close around a slot with a letter hard against them, as they do around a word.</summary>
    [Fact]
    public void StarsCloseAroundASlotAsAroundAWord()
    {
        var runs = Template.Read("**{name}**の設計図").Blocks[0].Runs;

        Assert.Equal("name", runs[0].Slot);
        Assert.Equal(RunKind.Strong, runs[0].Kind);
        Assert.Equal("の設計図", runs[1].Text);
        Assert.Equal(RunKind.Plain, runs[1].Kind);
    }

    /// <summary>Doubled braces are a brace, and braces around anything but a name are only text.</summary>
    [Fact]
    public void BracesThatAreNoSlotAreText()
    {
        var runs = Template.Read("{{name}} and { spaced } and {1st}").Blocks[0].Runs;

        var run = Assert.Single(runs);
        Assert.Null(run.Slot);
        Assert.Equal("{name} and { spaced } and {1st}", run.Text);
    }

    /// <summary>Every slot of the name is filled, and a slot of another name is left alone.</summary>
    [Fact]
    public void ASlotIsFilledWhereverItIs()
    {
        Assert.Equal("Airlock and Airlock, {other}", Template.Fill("{name} and {name}, {other}", "name", "Airlock"));
    }
}
