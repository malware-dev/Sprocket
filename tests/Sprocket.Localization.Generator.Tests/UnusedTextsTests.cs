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

    /// <summary>The diagnostics the analyzer reports on <see cref="Code" />, built with the class the generator writes from <see cref="Texts" />.</summary>
    static ImmutableArray<Diagnostic> Reported()
    {
        var files = ImmutableArray.Create<AdditionalText>(new Written("MyTexts.resx", Texts), new Written("LCDTextures.sbc", Definition));
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
