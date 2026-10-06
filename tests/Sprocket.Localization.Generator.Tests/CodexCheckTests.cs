using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Sprocket.Localization.Generator.Tests;

/// <summary>The codex check: every page titled and beside its tooltip, every link leading to a page, every translation whole.</summary>
public class CodexCheckTests
{
    const string English = "Content/Data/Codex/en/";
    const string Norwegian = "Content/Data/Codex/no/";

    /// <summary>A codex whose every term is whole, in both languages, is reported for nothing and writes nothing.</summary>
    [Fact]
    public void AWholeCodexIsQuiet()
    {
        var run = Run(Whole(English).Concat(Whole(Norwegian)).ToArray());

        Assert.Empty(run.Diagnostics);
        Assert.Empty(run.GeneratedTrees);
    }

    /// <summary>A page that does not open with its title is reported.</summary>
    [Fact]
    public void APageWithoutItsTitleIsReported()
    {
        var run = Run(Whole(English).Append((English + "c.md", "No title here.")).Append((English + "c.tldr.md", "C.")).ToArray());

        Assert.Equal("LOC007", Assert.Single(run.Diagnostics).Id);
    }

    /// <summary>A page without its tooltip, and a tooltip without its page, are each reported.</summary>
    [Fact]
    public void HalfATermIsReported()
    {
        var run = Run(Whole(English).Append((English + "c.md", "# C\n")).Append((English + "d.tldr.md", "D.")).ToArray());

        Assert.Equal(new[] { "LOC008", "LOC008" }, run.Diagnostics.Select(d => d.Id));
    }

    /// <summary>A link to a term with no page is reported on the line it is on.</summary>
    [Fact]
    public void ALinkToNoPageIsReported()
    {
        var run = Run(Whole(English).Append((English + "c.md", "# C\n\nSee [nothing](#nothing).")).Append((English + "c.tldr.md", "C.")).ToArray());

        var dead = Assert.Single(run.Diagnostics);
        Assert.Equal("LOC009", dead.Id);
        Assert.Equal(2, dead.Location.GetLineSpan().StartLinePosition.Line);
    }

    /// <summary>A box never closed is reported on the line that opens it; one that is closed, inside another, is not.</summary>
    [Fact]
    public void ABoxNeverClosedIsReported()
    {
        var run = Run(Whole(English).Append((English + "c.md", "# C\n\n::: warning\n::: center\nx\n:::\nstill open")).Append((English + "c.tldr.md", "C.")).ToArray());

        var unclosed = Assert.Single(run.Diagnostics);
        Assert.Equal("LOC010", unclosed.Id);
        Assert.Equal(2, unclosed.Location.GetLineSpan().StartLinePosition.Line);
    }

    /// <summary>A link in a tooltip is reported on the line it is on, since nothing in a tooltip can be pressed; an image is not a link.</summary>
    [Fact]
    public void ALinkInATooltipIsReported()
    {
        var run = Run(Whole(English).Append((English + "c.md", "# C\n")).Append((English + "c.tldr.md", "A **C** is shown ![mark](logo).\nSee [a](#a).")).ToArray());

        var pressless = Assert.Single(run.Diagnostics);
        Assert.Equal("LOC011", pressless.Id);
        Assert.Equal(1, pressless.Location.GetLineSpan().StartLinePosition.Line);
    }

    /// <summary>A translation missing a file of the original, or holding one the original lacks, is reported.</summary>
    [Fact]
    public void ATranslationThatIsNotWholeIsReported()
    {
        var run = Run(Whole(English).Concat(Whole(Norwegian).Where(f => !f.Path.EndsWith("b.md")))
            .Append((Norwegian + "x.md", "# X\n")).Append((Norwegian + "x.tldr.md", "X.")).ToArray());

        Assert.Contains(run.Diagnostics, d => d.Id == "LOC006" && d.GetMessage().StartsWith("b.md"));
        Assert.Contains(run.Diagnostics, d => d.Id == "LOC005" && d.GetMessage().StartsWith("x.md"));
    }

    /// <summary>A language the texts are translated into and the codex has no folder for lacks every file of it.</summary>
    [Fact]
    public void ALanguageWithNoCodexLacksEveryFile()
    {
        var run = Run(Whole(English).Append(("Content/Data/Localization/MyTexts.no.resx", "<root />")).ToArray());

        Assert.Equal(4, run.Diagnostics.Count(d => d.Id == "LOC006"));
    }

    /// <summary>Markdown outside a codex folder is no business of the check.</summary>
    [Fact]
    public void OtherMarkdownIsLeftAlone()
    {
        var run = Run(("docs/Notes.md", "No title, and [a link](#nowhere)."));

        Assert.Empty(run.Diagnostics);
    }

    /// <summary>Two terms, the first linking to the second, each with its page and tooltip, in the language folder <paramref name="folder" />.</summary>
    static IEnumerable<(string Path, string Text)> Whole(string folder) => new[]
    {
        (folder + "a.md", "# A\n\nSee [b](#b)."),
        (folder + "a.tldr.md", "An **a** is a thing."),
        (folder + "b.md", "# B\n\nBack to [a](#a)."),
        (folder + "b.tldr.md", "A **b** is another.")
    };

    static GeneratorDriverRunResult Run(params (string Path, string Text)[] files)
    {
        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new CodexCheck().AsSourceGenerator() },
            files.Select(file => (AdditionalText)new Written(file.Path, file.Text)).ToImmutableArray(),
            optionsProvider: new Build(new Properties(new Dictionary<string, string>())));

        return driver.RunGenerators(CSharpCompilation.Create("Mod")).GetRunResult();
    }
}
