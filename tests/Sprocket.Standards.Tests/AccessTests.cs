using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Sprocket.Standards.Tests;

public class AccessTests
{
    /// <summary>The names <see cref="Access" /> reports in <paramref name="code" />, in the order they are written.</summary>
    static string[] Reported(string code)
    {
        var compilation = CSharpCompilation.Create("Checked", [CSharpSyntaxTree.ParseText(code)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new Access()))
            .GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.Where(d => d.Id == Rules.Internal.Id)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .Select(d => d.GetMessage().Split(' ')[0])
            .ToArray();
    }

    [Fact]
    public void ATopLevelTypeWithNoModifierIsInternal()
    {
        Assert.Equal(["Hidden"], Reported("class Hidden { }"));
    }

    [Fact]
    public void TheKeywordIsReportedOnEveryKindOfMember()
    {
        const string code = """
            public class Open
            {
                internal int Field;
                internal int Property { get; set; }
                internal event System.Action Happened;
                internal void Method() { }
                internal Open() { }
                internal class Nested { }
            }
            """;

        Assert.Equal(["Open.Field", "Open.Property", "Open.Happened", "Open.Method()", "Open.Open()", "Open.Nested"], Reported(code));
    }

    [Fact]
    public void ProtectedInternalAndPrivateProtectedAreInternalToo()
    {
        const string code = """
            public class Open
            {
                protected internal void Wide() { }
                private protected void Narrow() { }
            }
            """;

        Assert.Equal(["Open.Wide()", "Open.Narrow()"], Reported(code));
    }

    [Fact]
    public void AnAccessorIsReportedOnlyWhereItNarrowsItsProperty()
    {
        const string code = """
            public class Open
            {
                public int Set { get; internal set; }
                internal int Whole { get; set; }
            }
            """;

        Assert.Equal(["Open.Set.set", "Open.Whole"], Reported(code));
    }

    [Fact]
    public void PublicAndPrivateAreLeftAlone()
    {
        const string code = """
            public class Open
            {
                int _field;
                class Nested { }
                public void Method() { }
                protected void Inherited() { }
                private void Own() { }
            }
            """;

        Assert.Empty(Reported(code));
    }
}
