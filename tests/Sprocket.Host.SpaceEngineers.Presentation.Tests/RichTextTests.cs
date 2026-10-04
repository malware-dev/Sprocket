using Lumel.Layout;
using VRageMath;
using Xunit;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A document drawn: wrapped to its room, each kind of run in its own color, and measured only when something about it changes.</summary>
public class RichTextTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly Color Body = new Color(162, 195, 205);
    static readonly Color Bright = new Color(220, 245, 253);

    /// <summary>Strong words take the strong color and the words around them the body color.</summary>
    [Fact]
    public void StrongWordsTakeTheStrongColor()
    {
        var (_, easel) = Laid("Made from **Airlock** blueprint", Wide);

        Assert.Equal(Body, Word(easel, "Made from").Color);
        Assert.Equal(Bright, Word(easel, "Airlock").Color);
        Assert.Equal(Body, Word(easel, "blueprint").Color);
    }

    /// <summary>Words that do not fit the room carry on on the next line, and none is cut.</summary>
    [Fact]
    public void WordsThatDoNotFitWrap()
    {
        const string said = "one two three four five six seven eight nine ten";
        var (text, easel) = Laid(said, 120f);

        var lines = easel.Written.Select(c => c.Position.Y).Distinct().Count();

        Assert.True(lines > 1, "nothing wrapped");
        Assert.True(text.DesiredSize.X <= 120f, "a line is wider than the room");
        Assert.Equal(said.Replace(" ", ""), string.Concat(easel.Written.Select(c => c.Text)).Replace(" ", ""));
    }

    /// <summary>A single newline starts a new line, and a blank line starts a new block below it.</summary>
    [Fact]
    public void ANewlineStartsALine()
    {
        var (_, easel) = Laid("one\ntwo", Wide);

        Assert.True(Word(easel, "two").Position.Y > Word(easel, "one").Position.Y);
    }

    /// <summary>A bullet's words stand in, and a numbered item draws its number.</summary>
    [Fact]
    public void ListItemsStandIn()
    {
        var (_, easel) = Laid("plain\n\n- item\n\n3. counted", Wide);

        Assert.True(Word(easel, "item").Position.X > Word(easel, "plain").Position.X);
        Assert.Contains(easel.Written, c => c.Text == "3.");
    }

    /// <summary>Drawing again measures nothing, and nor does changing a color.</summary>
    [Fact]
    public void DrawingAgainMeasuresNothing()
    {
        var (text, easel) = Laid("# Title\nSome **strong** words, and *some* not.\n\n- one\n- two", Wide);

        easel.Draw(Wide, High);
        var settled = easel.Measured;

        text.StrongColor = Color.Red;
        easel.Draw(Wide, High);
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>A slot says its argument, set apart as the slot is.</summary>
    [Fact]
    public void ASlotSaysItsArgument()
    {
        var named = new Arguments();
        named.Set("name", "Airlock");
        var (_, easel) = Laid(Template.Read("Made from **{name}** blueprint"), Wide, named);

        Assert.Equal(Bright, Word(easel, "Airlock").Color);
        Assert.Equal(Body, Word(easel, "blueprint").Color);
    }

    /// <summary>A new argument lays the words out again from the same document, which is never read again.</summary>
    [Fact]
    public void ANewArgumentLaysOutAgainFromTheSameDocument()
    {
        var named = new Arguments();
        named.Set("name", "Airlock");
        var (text, easel) = Laid(Template.Read("Made from **{name}** blueprint"), Wide, named);
        var document = text.Document;

        named.Set("name", "Hangar Door");
        easel.Draw(Wide, High);

        Assert.Same(document, text.Document);
        Assert.Contains(easel.Written, c => c.Text.Trim() == "Hangar Door");
        Assert.DoesNotContain(easel.Written, c => c.Text.Trim() == "Airlock");
    }

    /// <summary>Setting an argument to what it already is lays nothing out.</summary>
    [Fact]
    public void TheSameArgumentLaysNothingOut()
    {
        var named = new Arguments();
        named.Set("name", "Airlock");
        var (_, easel) = Laid(Template.Read("Made from **{name}** blueprint"), Wide, named);

        easel.Draw(Wide, High);
        var settled = easel.Measured;

        named.Set("name", "Airlock");
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>An argument is words, never markdown.</summary>
    [Fact]
    public void AnArgumentIsNeverMarkdown()
    {
        var named = new Arguments();
        named.Set("name", "*Test* rig");
        var (_, easel) = Laid(Template.Read("Made from {name}"), Wide, named);

        Assert.Contains(easel.Written, c => c.Text.Contains("*Test* rig"));
    }

    /// <summary>A table with a starred column fills its room, the right-aligned column against the right edge.</summary>
    [Fact]
    public void AStarredColumnTakesTheSpareWidth()
    {
        var (text, easel) = Laid("| | |\n|:-*|--:|\n| Power cells | 450 |", Wide);

        Assert.Equal(Wide, text.DesiredSize.X);
        Assert.True(Word(easel, "450").Position.X > Wide * 0.6f * easel.Pixels, "the price is not near the right edge");
    }

    /// <summary>A table with no star is only as wide as its cells and the gaps between them.</summary>
    [Fact]
    public void ATableWithNoStarHugsItsCells()
    {
        var (text, _) = Laid("| | |\n|:--|--:|\n| Ice | 2 |", Wide);

        Assert.True(text.DesiredSize.X < Wide / 2f, "the table spread across its room");
    }

    /// <summary>A heading row's cells take the heading color.</summary>
    [Fact]
    public void AHeadingRowTakesTheHeadingColor()
    {
        var (_, easel) = Laid("| Item | Price |\n|--|--|\n| Ice | 2 |", Wide);

        Assert.Equal(Bright, Word(easel, "Item").Color);
        Assert.Equal(Body, Word(easel, "Ice").Color);
    }

    /// <summary>A table too wide for its room narrows its widest column, whose cell wraps, and the row grows to hold it.</summary>
    [Fact]
    public void ATooWideTableWrapsItsWidestColumn()
    {
        var (_, easel) = Laid("| | |\n|--|--|\n| one two three four five six seven eight nine ten eleven twelve | Next |\n| Below | x |", Wide);

        var lines = easel.Written.Where(c => c.Position.Y < Word(easel, "Below").Position.Y).Select(c => c.Position.Y).Distinct().Count();
        Assert.True(lines > 1, "the long cell did not wrap");
        Assert.Equal(Word(easel, "Next").Position.Y, easel.Written.Min(c => c.Position.Y));
    }

    /// <summary>A slot in a cell takes its words from the arguments.</summary>
    [Fact]
    public void ASlotInACellIsFilled()
    {
        var named = new Arguments();
        named.Set("price", "12 SC");
        var (_, easel) = Laid(Template.Read("| | |\n|--|--:|\n| Whiskey | {price} |"), Wide, named);

        Assert.Equal(Body, Word(easel, "12 SC").Color);
    }

    static (RichText, Easel) Laid(string markdown, float wide) => Laid(Markdown.Parse(markdown), wide, null);

    static (RichText, Easel) Laid(Document document, float wide, Arguments? arguments)
    {
        var text = new RichText
        {
            Document = document,
            Arguments = arguments,
            FontSize = 20f,
            Color = Body,
            StrongColor = Bright,
            EmphasisColor = Bright,
            HeadingColor = Bright,
            BlockSpacing = 8f,
            Indent = 20f,
            Placement = Fixed.Wide(wide)
        };

        var easel = new Easel(new VStack { Children = { text } }, Wide, High);
        return (text, easel);
    }

    static Sim.Simulation.DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text.Trim() == text);
}
