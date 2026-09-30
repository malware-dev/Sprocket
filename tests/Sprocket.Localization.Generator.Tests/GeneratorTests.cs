using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Sprocket.Localization.Generator.Tests;

/// <summary>The generator: a class from the texts a mod writes, and a report of every text that cannot be said.</summary>
public class GeneratorTests
{
    const string Prefix = "Mod.";

    /// <summary>A file of texts is a class named for it without the game's "My", with a text for each key, said by its name less the prefix.</summary>
    [Fact]
    public void EachTextIsAMember()
    {
        var run = Run(("MyTexts.resx", Resx(("Mod.Greeting", "Hello"), ("Mod.Rows.Empty", "Nothing here yet."))));

        Assert.Empty(run.Diagnostics);
        var source = Assert.Single(run.GeneratedTrees).ToString();
        Assert.Contains("public sealed class Texts", source);
        Assert.Contains("public string Greeting =>", source);
        Assert.Contains("public string RowsEmpty =>", source);
        Assert.Contains("\"Hello\"", source);
    }

    /// <summary>A translation beside the texts makes no class of its own.</summary>
    [Fact]
    public void ATranslationMakesNoClass()
    {
        var run = Run(
            ("MyTexts.resx", Resx(("Mod.Greeting", "Hello"))),
            ("MyTexts.no.resx", Resx(("Mod.Greeting", "Hei"))));

        Assert.Empty(run.Diagnostics);
        Assert.Single(run.GeneratedTrees);
    }

    /// <summary>A file that is not XML is reported, and makes nothing.</summary>
    [Fact]
    public void UnreadableTextsAreReported()
    {
        var run = Run(("MyTexts.resx", "<root><data"));

        Assert.Equal("LOC001", Assert.Single(run.Diagnostics).Id);
        Assert.Empty(run.GeneratedTrees);
    }

    /// <summary>A key without the prefix is reported and left out, so the texts cannot collide with the game's own.</summary>
    [Fact]
    public void AKeyWithoutThePrefixIsReported()
    {
        var run = Run(("MyTexts.resx", Resx(("Greeting", "Hello"), ("Mod.Farewell", "Goodbye"))));

        var problem = Assert.Single(run.Diagnostics);
        Assert.Equal("LOC002", problem.Id);
        Assert.Equal(DiagnosticSeverity.Error, problem.Severity);
        Assert.DoesNotContain("Greeting", Assert.Single(run.GeneratedTrees).ToString());
    }

    /// <summary>Two keys that would be said by one name are reported, and the second is left out.</summary>
    [Fact]
    public void TwoKeysMakingOneMemberAreReported()
    {
        var run = Run(("MyTexts.resx", Resx(("Mod.Rows.Empty", "Nothing here yet."), ("Mod.RowsEmpty", "Empty"))));

        Assert.Equal("LOC003", Assert.Single(run.Diagnostics).Id);
        Assert.DoesNotContain("\"Empty\"", Assert.Single(run.GeneratedTrees).ToString());
    }

    /// <summary>A key that makes no name C# takes is reported and left out.</summary>
    [Fact]
    public void AKeyMakingNoNameIsReported()
    {
        var run = Run(("MyTexts.resx", Resx(("Mod.1st", "First"))));

        Assert.Equal("LOC004", Assert.Single(run.Diagnostics).Id);
    }

    /// <summary>A text in a translation that the original lacks is a warning, since the game would simply never ask for it.</summary>
    [Fact]
    public void AStrayTranslationIsAWarning()
    {
        var run = Run(
            ("MyTexts.resx", Resx(("Mod.Greeting", "Hello"))),
            ("MyTexts.no.resx", Resx(("Mod.Greeting", "Hei"), ("Mod.Farewell", "Ha det"))));

        var problem = Assert.Single(run.Diagnostics);
        Assert.Equal("LOC005", problem.Id);
        Assert.Equal(DiagnosticSeverity.Warning, problem.Severity);
    }

    /// <summary>A text of the original that a translation lacks is a warning, since the game would show it untranslated.</summary>
    [Fact]
    public void AMissingTranslationIsAWarning()
    {
        var run = Run(
            ("MyTexts.resx", Resx(("Mod.Greeting", "Hello"), ("Mod.Farewell", "Goodbye"))),
            ("MyTexts.no.resx", Resx(("Mod.Greeting", "Hei"))));

        var problem = Assert.Single(run.Diagnostics);
        Assert.Equal("LOC006", problem.Id);
        Assert.Equal(DiagnosticSeverity.Warning, problem.Severity);
        Assert.Contains("Mod.Farewell", problem.GetMessage());
    }

    /// <summary>The generator run over <paramref name="files" />, with the prefix and namespaces a mod would set.</summary>
    static GeneratorDriverRunResult Run(params (string Path, string Text)[] files)
    {
        var properties = new Properties(new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "Mod",
            ["build_property.LocalizationPrefix"] = Prefix
        });

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            new[] { new TextsGenerator().AsSourceGenerator() },
            files.Select(file => (AdditionalText)new Written(file.Path, file.Text)).ToImmutableArray(),
            optionsProvider: new Build(properties));

        driver = driver.RunGenerators(CSharpCompilation.Create("Mod"));
        return driver.GetRunResult();
    }

    /// <summary>A file of texts holding <paramref name="texts" />, in the shape the game reads.</summary>
    static string Resx(params (string Key, string Value)[] texts)
    {
        var data = string.Join("\n", texts.Select(text =>
            $"  <data name=\"{text.Key}\" xml:space=\"preserve\">\n    <value>{text.Value}</value>\n  </data>"));
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>\n" + data + "\n</root>\n";
    }
}
