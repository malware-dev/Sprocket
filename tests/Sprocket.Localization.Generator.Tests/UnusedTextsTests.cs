using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Sprocket.Localization.Generator.Tests;

/// <summary>A text no code reads and no definition names is reported; one either reaches is not.</summary>
public class UnusedTextsTests
{
    const string Texts = """
        <?xml version="1.0" encoding="utf-8"?>
        <root>
          <data name="Mod.Read" xml:space="preserve">
            <value>Read</value>
            <comment>Shown on the test screen.</comment>
          </data>
          <data name="Mod.Logo" xml:space="preserve">
            <value>Logo</value>
            <comment>The name of an image in an LCD's image list.</comment>
          </data>
          <data name="Mod.Never" xml:space="preserve">
            <value>Never</value>
            <comment>Shown on no screen.</comment>
          </data>
        </root>
        """;

    const string Definition = """
        <Definitions><LCDTextureDefinition><LocalizationId>Mod.Logo</LocalizationId></LCDTextureDefinition></Definitions>
        """;

    const string Code = """
        namespace Mod
        {
            public static class Screen
            {
                public static string Heading(Mod.Texts texts) => texts.Read;
            }
        }
        """;

    /// <summary>Only the text nothing reaches is reported, at its line in the file of texts.</summary>
    [Fact]
    public void OnlyATextNothingReachesIsReported()
    {
        var reported = Reported();

        var problem = Assert.Single(reported);
        Assert.Equal("LOC020", problem.Id);
        Assert.Contains("Mod.Never", problem.GetMessage());
        Assert.Equal(10, problem.Location.GetLineSpan().StartLinePosition.Line);
    }

    /// <summary>A text a link names counts as read, and a link naming no text is an error at the text it is in; a picture's locator is no link.</summary>
    [Fact]
    public void ALinkReadsItsTextAndOneToNothingFails()
    {
        const string codex = """
            <?xml version="1.0" encoding="utf-8"?>
            <root>
              <data name="Mod.Term.Slot" xml:space="preserve">
                <value>A slot.</value>
                <comment>A page.</comment>
              </data>
              <data name="Mod.Term.Page" xml:space="preserve">
                <value>![](mark:slot) See [slot](Term.Slot) and [gone](Term.Gone).</value>
                <comment>A page.</comment>
              </data>
            </root>
            """;

        var reported = Reported(new Written("Codex.resx", codex));

        Assert.DoesNotContain(reported, problem => problem.GetMessage().Contains("Mod.Term.Slot "));
        var nowhere = Assert.Single(reported, problem => problem.Id == "LOC021");
        Assert.Equal(DiagnosticSeverity.Error, nowhere.Severity);
        Assert.Contains("Term.Gone", nowhere.GetMessage());
        Assert.DoesNotContain(reported, problem => problem.GetMessage().Contains("mark:slot"));
    }

    /// <summary>The diagnostics the analyzer reports on <see cref="Code" />, built with the classes the generator writes from <see cref="Texts" /> and <paramref name="more" />.</summary>
    static ImmutableArray<Diagnostic> Reported(params AdditionalText[] more)
    {
        var files = ImmutableArray.Create<AdditionalText>(new Written("MyTexts.resx", Texts), new Written("LCDTextures.sbc", Definition)).AddRange(more);
        var properties = new Properties(new Dictionary<string, string>
        {
            ["build_property.RootNamespace"] = "Mod",
            ["build_property.LocalizationPrefix"] = "Mod."
        });

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
            .Select(assembly => (MetadataReference)MetadataReference.CreateFromFile(assembly.Location));
        var compilation = CSharpCompilation.Create("Mod", [CSharpSyntaxTree.ParseText(Code)], references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create([new TextsGenerator().AsSourceGenerator()], files,
            optionsProvider: new Build(properties));
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var built, out _);

        return built.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new UnusedTexts()),
                new AnalyzerOptions(files, new Build(properties)))
            .GetAnalyzerDiagnosticsAsync().Result;
    }
}
