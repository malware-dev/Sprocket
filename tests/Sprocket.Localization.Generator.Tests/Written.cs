using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Sprocket.Localization.Generator.Tests;

/// <summary>A file handed to the generator as the compiler hands it an additional file, written here rather than read from disk.</summary>
sealed class Written(string path, string text) : AdditionalText
{
    /// <inheritdoc />
    public override string Path { get; } = path;

    /// <inheritdoc />
    public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
}
