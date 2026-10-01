using Xunit;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Markdown-lite read into a document: CommonMark's meaning for what it supports, and plain text for the rest.</summary>
public class MarkdownTests
{
    /// <summary>Headings at every level keep their level; a hash with no space after it is a word.</summary>
    [Fact]
    public void HeadingsKeepTheirLevel()
    {
        var blocks = Markdown.Parse("# One\n### Three ###\n####### Seven\n#hashtag").Blocks;

        Assert.Equal(BlockKind.Heading, blocks[0].Kind);
        Assert.Equal(1, blocks[0].Level);
        Assert.Equal("One", Words(blocks[0]));
        Assert.Equal(3, blocks[1].Level);
        Assert.Equal("Three", Words(blocks[1]));
        Assert.Equal(BlockKind.Paragraph, blocks[2].Kind);
        Assert.Equal("####### Seven\n#hashtag", Words(blocks[2]));
    }

    /// <summary>Three or more of one mark, spaced or not, is a rule, and wins over a list item.</summary>
    [Theory]
    [InlineData("---")]
    [InlineData("***")]
    [InlineData("___")]
    [InlineData("- - -")]
    [InlineData(" *  *  * ")]
    public void ARuleIsThreeOfOneMark(string line)
    {
        var blocks = Markdown.Parse(line).Blocks;

        Assert.Single(blocks);
        Assert.Equal(BlockKind.Rule, blocks[0].Kind);
    }

    /// <summary>Dashes, stars and pluses start bullets; numbers with a dot or a bracket start counted items, keeping their number.</summary>
    [Fact]
    public void ListItemsAreBulletsOrNumbered()
    {
        var blocks = Markdown.Parse("- one\n* two\n+ three\n4. four\n5) five").Blocks;

        Assert.Equal(BlockKind.Bullet, blocks[0].Kind);
        Assert.Equal(BlockKind.Bullet, blocks[1].Kind);
        Assert.Equal(BlockKind.Bullet, blocks[2].Kind);
        Assert.Equal(BlockKind.Numbered, blocks[3].Kind);
        Assert.Equal(4, blocks[3].Level);
        Assert.Equal(5, blocks[4].Level);
        Assert.Equal("five", Words(blocks[4]));
    }

    /// <summary>A single newline is a line break inside a block, and a blank line starts the next block.</summary>
    [Fact]
    public void ANewlineBreaksAndABlankLineEnds()
    {
        var blocks = Markdown.Parse("one\ntwo\n\nthree").Blocks;

        Assert.Equal(2, blocks.Count);
        Assert.Equal("one\ntwo", Words(blocks[0]));
        Assert.True(blocks[0].Runs[1].IsBreak);
        Assert.Equal("three", Words(blocks[1]));
    }

    /// <summary>A plain line after a list item carries on that item, on a line of its own.</summary>
    [Fact]
    public void ALineAfterAnItemCarriesItOn()
    {
        var blocks = Markdown.Parse("- one\ncarried on").Blocks;

        Assert.Single(blocks);
        Assert.Equal("one\ncarried on", Words(blocks[0]));
    }

    /// <summary>Two marks make strong, one makes emphasis, and three make both.</summary>
    [Theory]
    [InlineData("**strong**", "strong", RunKind.Strong)]
    [InlineData("__strong__", "strong", RunKind.Strong)]
    [InlineData("*emphasis*", "emphasis", RunKind.Emphasis)]
    [InlineData("_emphasis_", "emphasis", RunKind.Emphasis)]
    [InlineData("***both***", "both", RunKind.Strong | RunKind.Emphasis)]
    public void MarksSetWordsApart(string source, string text, RunKind kind)
    {
        var run = Markdown.Parse(source).Blocks[0].Runs[0];

        Assert.Equal(text, run.Text);
        Assert.Equal(kind, run.Kind);
    }

    /// <summary>Emphasis nests inside strong, and the words around both stay plain.</summary>
    [Fact]
    public void EmphasisNestsInsideStrong()
    {
        var runs = Markdown.Parse("a **b *c* d** e").Blocks[0].Runs;

        Assert.Equal(new[] { "a ", "b ", "c", " d", " e" }, Texts(runs));
        Assert.Equal(RunKind.Plain, runs[0].Kind);
        Assert.Equal(RunKind.Strong, runs[1].Kind);
        Assert.Equal(RunKind.Strong | RunKind.Emphasis, runs[2].Kind);
        Assert.Equal(RunKind.Strong, runs[3].Kind);
        Assert.Equal(RunKind.Plain, runs[4].Kind);
    }

    /// <summary>An underscore inside a word is a character, so names written with them survive.</summary>
    [Fact]
    public void UnderscoresInsideWordsAreCharacters()
    {
        var runs = Markdown.Parse("snake_case_name").Blocks[0].Runs;

        Assert.Single(runs);
        Assert.Equal("snake_case_name", runs[0].Text);
        Assert.Equal(RunKind.Plain, runs[0].Kind);
    }

    /// <summary>A star with space on both sides opens and closes nothing.</summary>
    [Fact]
    public void ALoneStarIsACharacter()
    {
        var runs = Markdown.Parse("2 * 3 = 6").Blocks[0].Runs;

        Assert.Single(runs);
        Assert.Equal("2 * 3 = 6", runs[0].Text);
    }

    /// <summary>A mark nobody closes stays as it was written.</summary>
    [Fact]
    public void AnUnclosedMarkStaysAsWritten()
    {
        var runs = Markdown.Parse("**never closed").Blocks[0].Runs;

        Assert.Equal("**never closed", string.Concat(Texts(runs)));
        Assert.All(runs, r => Assert.Equal(RunKind.Plain, r.Kind));
    }

    /// <summary>A code span keeps what is inside it as it is, marks included.</summary>
    [Fact]
    public void CodeKeepsItsInsideAsItIs()
    {
        var runs = Markdown.Parse("run `when *pressed*` now").Blocks[0].Runs;

        Assert.Equal("when *pressed*", runs[1].Text);
        Assert.Equal(RunKind.Code, runs[1].Kind);
    }

    /// <summary>Anything escaped reads as exactly what it says, block marks included.</summary>
    [Theory]
    [InlineData("**Airlock**")]
    [InlineData("# not a heading")]
    [InlineData("1. not counted")]
    [InlineData("- not a bullet")]
    [InlineData("---")]
    [InlineData("back\\slash and `tick`")]
    public void AnEscapedNameReadsAsItIs(string name)
    {
        var blocks = Markdown.Parse(Markdown.Escape(name)).Blocks;

        Assert.Single(blocks);
        Assert.Equal(BlockKind.Paragraph, blocks[0].Kind);
        Assert.Single(blocks[0].Runs);
        Assert.Equal(name, blocks[0].Runs[0].Text);
        Assert.Equal(RunKind.Plain, blocks[0].Runs[0].Kind);
    }

    /// <summary>Plain text is one paragraph, broken where it has newlines, and never read for marks.</summary>
    [Fact]
    public void PlainTextIsReadForNothing()
    {
        var blocks = Document.Plain("**one**\n# two").Blocks;

        Assert.Single(blocks);
        Assert.Equal("**one**\n# two", Words(blocks[0]));
    }

    /// <summary>The words of a block, with its line breaks as newlines.</summary>
    static string Words(Block block) =>
        string.Concat(block.Runs.Select(r => r.IsBreak ? "\n" : r.Text));

    static string[] Texts(IList<Run> runs) => runs.Select(r => r.Text).ToArray();
}
