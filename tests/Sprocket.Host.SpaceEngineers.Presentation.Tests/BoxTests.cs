using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Boxes in markdown-lite: blocks between a fence naming attributes and a bare fence, set where <c>left</c>, <c>center</c>
/// and <c>right</c> say, and looking as the host's styles for their other names say.
/// </summary>
public class BoxTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly Color Body = new Color(162, 195, 205);
    static readonly Color Rule = new Color(91, 115, 123);
    static readonly Color Stressed = new Color(220, 245, 253);

    /// <summary>A fence with names opens a box of everything up to the bare fence, and reading goes on after it.</summary>
    [Fact]
    public void AFenceOpensABoxAndABareOneClosesIt()
    {
        var blocks = Markdown.Parse("::: warning center\n# Title\n\nWords\n:::\nafter").Blocks;

        var box = blocks[0];
        Assert.Equal(BlockKind.Box, box.Kind);
        Assert.Equal(new[] { "warning", "center" }, box.Names);
        Assert.Equal(new[] { BlockKind.Heading, BlockKind.Paragraph }, box.Blocks.Select(b => b.Kind));
        Assert.Equal("after", Words(blocks[1]));
    }

    /// <summary>A box inside a box is closed first, by the first bare fence after it.</summary>
    [Fact]
    public void BoxesNest()
    {
        var box = Markdown.Parse("::: warning\n::: center\ninner\n:::\nouter\n:::").Blocks[0];

        var inner = box.Blocks[0];
        Assert.Equal(new[] { "center" }, inner.Names);
        Assert.Equal("inner", Words(Assert.Single(inner.Blocks)));
        Assert.Equal("outer", Words(box.Blocks[1]));
    }

    /// <summary>More colons, or none of the space before the names, open a box all the same.</summary>
    [Theory]
    [InlineData(":::: note\nx\n::::")]
    [InlineData(":::note\nx\n:::")]
    public void FencesComeInMoreThanOneLength(string source)
    {
        var box = Assert.Single(Markdown.Parse(source).Blocks);

        Assert.Equal(new[] { "note" }, box.Names);
        Assert.Equal("x", Words(Assert.Single(box.Blocks)));
    }

    /// <summary>A box never closed runs to the end.</summary>
    [Fact]
    public void ABoxNeverClosedRunsToTheEnd()
    {
        var box = Assert.Single(Markdown.Parse("::: note\none\n\ntwo").Blocks);

        Assert.Equal(2, box.Blocks.Count);
    }

    /// <summary>A bare fence with no box open, and a fence naming something no attribute can be, are words.</summary>
    [Theory]
    [InlineData(":::")]
    [InlineData("::: not!a name")]
    public void NotAFenceIsWords(string source)
    {
        Assert.Equal(BlockKind.Paragraph, Assert.Single(Markdown.Parse(source).Blocks).Kind);
    }

    /// <summary>An escaped fence is the words it shows, so a page can write one out.</summary>
    [Fact]
    public void AnEscapedFenceIsWords()
    {
        var block = Assert.Single(Markdown.Parse("\\::: center").Blocks);

        Assert.Equal(BlockKind.Paragraph, block.Kind);
        Assert.Equal("::: center", Words(block));
    }

    /// <summary>A name the host has a style for gives the box its look, whatever the name's case: a bar, a fill behind, and plain words in its color, strong ones keeping theirs.</summary>
    [Fact]
    public void ANamedStyleGivesTheBoxItsLook()
    {
        var (text, easel) = Laid("::: Warning\nMind the **door**\n:::");
        text.Boxes = new BoxStyles(new BoxStyle("warning", bar: Color.Orange, fill: Color.Purple, color: Color.Yellow));
        easel.Draw(Wide, High);

        Assert.Single(easel.Sprites, s => s.Color == Color.Orange);
        var fill = Assert.Single(easel.Sprites, s => s.Color == Color.Purple).Destination;
        Assert.True(fill.Width > Wide * 0.9f * easel.Pixels, "the fill does not reach across the room");
        Assert.Equal(Color.Yellow, Word(easel, "Mind the").Color);
        Assert.Equal(Stressed, Word(easel, "door").Color);
    }

    /// <summary>A name the host gives nothing changes nothing: no bar, no fill, and the words where any others would be.</summary>
    [Fact]
    public void AnUnknownNameChangesNothing()
    {
        var (text, easel) = Laid("Before\n\n::: other\nWords\n:::");
        text.Boxes = new BoxStyles(new BoxStyle("warning", fill: Color.Purple));
        easel.Draw(Wide, High);

        Assert.Empty(easel.Sprites);
        Assert.Equal(Word(easel, "Before").Position.X, Word(easel, "Words").Position.X);
        Assert.Equal(Body, Word(easel, "Words").Color);
    }

    /// <summary>center and right set where a box's lines sit, with no style from the host, and left sets them back inside a box that moved them.</summary>
    [Fact]
    public void LeftCenterAndRightAreMarkdownLitesOwn()
    {
        var (_, easel) = Laid("Plain\n\n::: center\nMiddle\n\n::: left\nBack\n:::\n:::\n\n::: right\nEnd\n:::");

        var plain = Word(easel, "Plain").Position.X;
        Assert.True(Word(easel, "Middle").Position.X > plain + 40f, "center did not move the line in");
        Assert.Equal(plain, Word(easel, "Back").Position.X);
        Assert.True(Word(easel, "End").Position.X > Word(easel, "Middle").Position.X, "right is not further across than center");
    }

    /// <summary>A box inside a box comes to the same as one box naming both.</summary>
    [Fact]
    public void ABoxInABoxIsOneBoxNamingBoth()
    {
        var styles = new BoxStyles(new BoxStyle("warning", bar: Color.Orange, fill: Color.Purple, color: Color.Yellow));

        var (one, apart) = Laid("::: warning\n::: center\nWords\n:::\n:::");
        one.Boxes = styles;
        apart.Draw(Wide, High);

        var (both, together) = Laid("::: warning center\nWords\n:::");
        both.Boxes = styles;
        together.Draw(Wide, High);

        Assert.Equal(Word(together, "Words").Position, Word(apart, "Words").Position);
        Assert.Equal(Word(together, "Words").Color, Word(apart, "Words").Color);
        Assert.Equal(together.Sprites.Select(s => s.Destination), apart.Sprites.Select(s => s.Destination));
    }

    /// <summary>A style gives a text its box looks and its link colors like everything else it sets.</summary>
    [Fact]
    public void AStyleSetsBoxesAndLinks()
    {
        var boxes = new BoxStyles(new BoxStyle("note", bar: Color.Orange));
        var style = new RichTextStyle { Base = { Boxes = boxes, LinkColor = Color.Red, LinkFill = Color.Blue } };
        var text = new RichText { Document = Markdown.Parse("x"), Style = style };

        Assert.Same(boxes, text.Boxes);
        Assert.Equal(Color.Red, text.LinkColor);
        Assert.Equal(Color.Blue, text.LinkFill);
    }

    /// <summary>Two styles under one name are refused when the styles are made.</summary>
    [Fact]
    public void TwoStylesForOneNameAreRefused()
    {
        Assert.Throws<ArgumentException>(() => new BoxStyles(new BoxStyle("note"), new BoxStyle("NOTE")));
    }

    /// <summary>Boxes nest no deeper than markdown-lite allows, however many fences open them: a deeper fence is words.</summary>
    [Fact]
    public void BoxesNestNoDeeperThanTheCap()
    {
        var blocks = Markdown.Parse(string.Concat(Enumerable.Repeat("::: a\n", 10000))).Blocks;

        var depth = 0;
        while (blocks.Count > 0 && blocks[0].Kind == BlockKind.Box)
        {
            depth++;
            blocks = blocks[0].Blocks;
        }

        Assert.Equal(Markdown.Deepest, depth);
        Assert.Equal(BlockKind.Paragraph, Assert.Single(blocks).Kind);
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
