using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Quotes in markdown-lite: blocks set apart behind a bar, and a tag that lets the host give some of them a look of their own.</summary>
public class QuoteTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly Color Body = new Color(162, 195, 205);
    static readonly Color Rule = new Color(91, 115, 123);
    static readonly Color Stressed = new Color(220, 245, 253);

    /// <summary>Lines starting with &gt; are one quote, read like any other text.</summary>
    [Fact]
    public void QuotedLinesAreOneQuote()
    {
        var blocks = Markdown.Parse("> one\n> two\n>\n> - item\nafter").Blocks;

        var quote = blocks[0];
        Assert.Equal(BlockKind.Quote, quote.Kind);
        Assert.Null(quote.Tag);
        Assert.Equal(new[] { BlockKind.Paragraph, BlockKind.Bullet }, quote.Blocks.Select(b => b.Kind));
        Assert.Equal("one\ntwo", Words(quote.Blocks[0]));
        Assert.Equal("after", Words(blocks[1]));
    }

    /// <summary>A first line that is only [!name] is the quote's tag, and not its words.</summary>
    [Fact]
    public void AQuotesFirstLineCanTagIt()
    {
        var quote = Assert.Single(Markdown.Parse("> [!NOTE]\n> Careful now").Blocks);

        Assert.Equal("NOTE", quote.Tag);
        Assert.Equal("Careful now", Words(Assert.Single(quote.Blocks)));
    }

    /// <summary>Anything else on a quote's first line is words, and a tag outside a quote is words too.</summary>
    [Theory]
    [InlineData("> [!not a tag]", "[!not a tag]")]
    [InlineData("> [!NOTE] and more", "[!NOTE] and more")]
    [InlineData("> > nested", "> nested")]
    public void NotATagIsWords(string source, string words)
    {
        var quote = Assert.Single(Markdown.Parse(source).Blocks);

        Assert.Null(quote.Tag);
        Assert.Equal(words, Words(Assert.Single(quote.Blocks)));
        Assert.Equal(BlockKind.Paragraph, Assert.Single(Markdown.Parse("[!NOTE]").Blocks).Kind);
    }

    /// <summary>A slot in a quote takes its words from the arguments, as anywhere else.</summary>
    [Fact]
    public void ASlotInAQuoteIsASlot()
    {
        var quote = Assert.Single(Template.Read("> [!tip]\n> Open {name}").Blocks);

        Assert.Equal("name", Assert.Single(quote.Blocks).Runs[1].Slot);
    }

    /// <summary>A quote is set in from a bar down its edge, in the rule color.</summary>
    [Fact]
    public void AQuoteStandsBesideABar()
    {
        var (_, easel) = Laid("Before\n\n> Quoted");

        var bar = Assert.Single(easel.Sprites, s => s.Color == Rule).Destination;
        Assert.True(Word(easel, "Quoted").Position.X > Word(easel, "Before").Position.X, "the quote is not set in");
        Assert.True(bar.X < Word(easel, "Quoted").Position.X, "the bar is not before the words");
    }

    /// <summary>A tag the host has given a look takes it, whatever its case: a bar, a fill behind, and plain words in its color, strong ones keeping theirs.</summary>
    [Fact]
    public void ATaggedQuoteTakesItsLook()
    {
        var (text, easel) = Laid("> [!Warning]\n> Mind the **door**");
        text.Quotes = new QuoteStyles(new QuoteStyle("warning", bar: Color.Orange, fill: Color.Purple, color: Color.Yellow));
        easel.Draw(Wide, High);

        Assert.Single(easel.Sprites, s => s.Color == Color.Orange);
        var fill = Assert.Single(easel.Sprites, s => s.Color == Color.Purple).Destination;
        Assert.True(fill.Width > Wide * 0.9f * easel.Pixels, "the fill does not reach across the room");
        Assert.Equal(Color.Yellow, Word(easel, "Mind the").Color);
        Assert.Equal(Stressed, Word(easel, "door").Color);
    }

    /// <summary>A tag with no look, or a quote with no tag, looks like any quote.</summary>
    [Fact]
    public void AnUnknownTagLooksLikeAnyQuote()
    {
        var (text, easel) = Laid("> [!other]\n> Words");
        text.Quotes = new QuoteStyles(new QuoteStyle("warning", fill: Color.Purple));
        easel.Draw(Wide, High);

        Assert.Single(easel.Sprites, s => s.Color == Rule);
        Assert.DoesNotContain(easel.Sprites, s => s.Color == Color.Purple);
        Assert.Equal(Body, Word(easel, "Words").Color);
    }

    /// <summary>A style gives a text its quote looks and its link colors like everything else it sets.</summary>
    [Fact]
    public void AStyleSetsQuotesAndLinks()
    {
        var quotes = new QuoteStyles(new QuoteStyle("note", bar: Color.Orange));
        var style = new RichTextStyle { Base = { Quotes = quotes, LinkColor = Color.Red, LinkFill = Color.Blue } };
        var text = new RichText { Document = Markdown.Parse("x"), Style = style };

        Assert.Same(quotes, text.Quotes);
        Assert.Equal(Color.Red, text.LinkColor);
        Assert.Equal(Color.Blue, text.LinkFill);
    }

    /// <summary>Two looks for one tag are refused when the looks are made.</summary>
    [Fact]
    public void TwoLooksForOneTagAreRefused()
    {
        Assert.Throws<ArgumentException>(() => new QuoteStyles(new QuoteStyle("note"), new QuoteStyle("NOTE")));
    }

    static (RichText, Easel) Laid(string markdown)
    {
        var text = new RichText
        {
            Document = Markdown.Parse(markdown),
            FontSize = 20f,
            Color = Body,
            StrongColor = Stressed,
            RuleColor = Rule,
            BlockSpacing = 8f,
            Indent = 20f,
            Placement = Fixed.Wide(Wide)
        };

        var easel = new Easel(new VStack { Children = { text } }, Wide, High);
        return (text, easel);
    }

    static string Words(Block block) => string.Concat(block.Runs.Select(r => r.IsBreak ? "\n" : r.Text));

    static Sim.Simulation.DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text.Trim() == text);
}
