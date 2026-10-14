using Xunit;

namespace Sprocket.Wiki.Tests;

public class PageTests
{
    /// <summary>A page's opening heading is left out, since the wiki shows the title above it; a heading after anything else stays.</summary>
    [Fact]
    public void TheOpeningHeadingIsLeftOut()
    {
        Assert.Equal("Words.\n", Given.Page("# A\n\nWords."));
        Assert.Equal("Words.\n\n# A\n", Given.Page("Words.\n\n# A"));
    }

    /// <summary>A line inside a paragraph is a line break in markdown-lite, which GitHub reads only with a backslash before it.</summary>
    [Fact]
    public void ALineBreakIsKept()
    {
        Assert.Equal("one\\\ntwo\n", Given.Page("one\ntwo"));
    }

    /// <summary>Strong words and emphasis are tags, emphasis inside strong where words are both; stars markdown-lite reads as stars stay stars.</summary>
    [Fact]
    public void StrongWordsAndEmphasisAreTags()
    {
        Assert.Equal("<strong>Note:</strong> then <em>this</em> and <strong><em>both</em></strong>\n", Given.Page("**Note:** then *this* and ***both***"));
        Assert.Equal("\\*\\*Note:\\*\\*then\n", Given.Page("**Note:**then"));
    }

    /// <summary>A link leads to its term's page and carries the plain words of the term's tooltip, which GitHub shows where it is rested on.</summary>
    [Fact]
    public void ALinkCarriesItsTermsTooltip()
    {
        Assert.Equal("See [the a](a \"An a is one thing.\") and [b](b \"A \\\"b\\\" is another.\").\n", Given.Page("See [the a](a) and [b](b)."));
    }

    /// <summary>A link to anything but a term of the codex stops the wiki being written.</summary>
    [Fact]
    public void ALinkToNoTermStops()
    {
        var stopped = Assert.Throws<InvalidDataException>(() => Given.Page("See [c](c)."));
        Assert.Contains("c", stopped.Message);
    }

    /// <summary>Code is written as it is, so a link or a picture shown as code is neither followed nor looked for.</summary>
    [Fact]
    public void CodeIsWrittenAsItIs()
    {
        Assert.Equal("`[c](c)` and ``![](mark:nothing) `x` ``\n", Given.Page("`[c](c)` and ``![](mark:nothing) `x` ``"));
    }

    /// <summary>A mark is one of Sprocket's own pictures, drawn as tall as the words beside it, in the ink of the theme it is shown in.</summary>
    [Fact]
    public void AMarkIsAPictureAsTallAsTheWords()
    {
        Assert.Equal("<picture><source media=\"(prefers-color-scheme: dark)\" srcset=\"images/marks/dark/signal.png\"><img src=\"images/marks/light/signal.png\" alt=\"\" height=\"18\"></picture>&nbsp;[a](a \"An a is one thing.\")\n",
            Given.Page("![](mark:signal)&nbsp;[a](a)"));
    }

    /// <summary>A picture given a size is drawn at that size, and the logo is Sprocket's own picture.</summary>
    [Fact]
    public void APictureGivenASizeIsDrawnAtIt()
    {
        Assert.Equal("<img src=\"images/sprocket-mark.png\" alt=\"The &quot;mark&quot;\" width=\"256\">\n", Given.Page("![The \"mark\"](Sprocket_Mark =256)"));
    }

    /// <summary>A picture that is none of Sprocket's own stops the wiki being written, so nothing of the game's is ever published.</summary>
    [Fact]
    public void APictureNotSprocketsOwnStops()
    {
        Assert.Throws<InvalidDataException>(() => Given.Page("![](Some_Game_Texture)"));
        Assert.Throws<InvalidDataException>(() => Given.Page("![](mark:nothing)"));
    }

    /// <summary>A note about the player's own computer is a note, as GitHub draws one.</summary>
    [Fact]
    public void APlayerBoxIsANote()
    {
        Assert.Equal("> [!NOTE]\n> Copy the file.\n>\n> Then the index.\n", Given.Page("::: player\nCopy the file.\n\nThen the index.\n:::"));
    }

    /// <summary>A centered box is a centered division, with blank lines so GitHub reads what is in it.</summary>
    [Fact]
    public void ACenteredBoxIsACenteredDivision()
    {
        Assert.Equal("<div align=\"center\">\n\n# Hangar\n\n</div>\n", Given.Page("::: center\n# Hangar\n:::"));
    }

    /// <summary>A box asking for something the wiki has no form for stops the wiki being written.</summary>
    [Fact]
    public void ABoxTheWikiHasNoFormForStops()
    {
        Assert.Throws<InvalidDataException>(() => Given.Page("::: sparkle\nWords.\n:::"));
    }

    /// <summary>A table loses its columns' shares, which GitHub has no way to give, and gains a heading row of empty cells where it has none.</summary>
    [Fact]
    public void ATableIsAsGitHubHasThem()
    {
        Assert.Equal("|  |  |\n|---|--:|\n| Airlock | 1 |\n", Given.Page("| | |\n|:--*|--:|\n| Airlock | 1 |"));
    }

    /// <summary>The items of one list stay together, and a list stands apart from what follows it.</summary>
    [Fact]
    public void TheItemsOfAListStayTogether()
    {
        Assert.Equal("- one\n- two\n\n1. three\n\nWords.\n", Given.Page("- one\n- two\n1. three\n\nWords."));
    }

    /// <summary>Words that GitHub would read as markup are escaped, where they start a line as well as anywhere else.</summary>
    [Fact]
    public void WordsAreShownAsTheyAre()
    {
        Assert.Equal("&lt;Inner door&gt; &amp; \\*not strong\\*\\\n1\\. not a list\n", Given.Page("&lt;Inner door&gt; &amp; \\*not strong\\*\n1\\. not a list"));
    }
}
