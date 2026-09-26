using Microsoft.CodeAnalysis.Diagnostics;

namespace Sprocket.Localization.Generator.Tests;

/// <summary>The build properties a project would pass the generator, set here by name.</summary>
sealed class Properties(IReadOnlyDictionary<string, string> set) : AnalyzerConfigOptions
{
    /// <inheritdoc />
    public override bool TryGetValue(string key, out string value) => set.TryGetValue(key, out value!);
}
