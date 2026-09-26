using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Sprocket.Localization.Generator.Tests;

/// <summary>The options a build gives the generator: its properties, and nothing set per file.</summary>
sealed class Build(Properties properties) : AnalyzerConfigOptionsProvider
{
    static readonly Properties Nothing = new(new Dictionary<string, string>());

    /// <inheritdoc />
    public override AnalyzerConfigOptions GlobalOptions { get; } = properties;

    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => Nothing;

    /// <inheritdoc />
    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => Nothing;
}
