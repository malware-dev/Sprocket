using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Sprocket.Standards.Tests;

/// <summary>Everything the mod declares and never names is reported; what the game reaches without a name is not.</summary>
public class DeadTests
{
    /// <summary>The names <see cref="Dead" /> reports in <paramref name="code" />, with <paramref name="generated" /> as a generated file beside it.</summary>
    static string[] Reported(string code, string? generated = null)
    {
        var trees = new List<SyntaxTree> { CSharpSyntaxTree.ParseText(code, path: "Mod.cs") };
        if (generated != null)
            trees.Add(CSharpSyntaxTree.ParseText(generated, path: "Texts.g.cs"));

        var compilation = CSharpCompilation.Create("Checked", trees,
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var diagnostics = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(new Dead()))
            .GetAnalyzerDiagnosticsAsync().Result;

        return diagnostics.Where(d => d.Id == Rules.Dead.Id)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .Select(d => d.GetMessage().Split(' ')[0])
            .ToArray();
    }

    /// <summary>A member nothing names is reported; one something names is not.</summary>
    [Fact]
    public void AMemberNothingNamesIsReported()
    {
        const string code = """
            public class Door
            {
                public static void Use() { Open(); }
                public static void Open() { }
                public static void Forgotten() { }
                public static int Count;
            }
            public class Panel { public void Press() { Door.Use(); } }
            """;

        Assert.Equal(["Door.Forgotten()", "Door.Count", "Panel"], Reported(code));
    }

    /// <summary>A type nothing names is reported once, and its members with it.</summary>
    [Fact]
    public void ATypeNothingNamesIsReportedOnce()
    {
        Assert.Equal(["Lonely"], Reported("public class Lonely { public void Alone() { } public int Value { get; set; } }"));
    }

    /// <summary>What overrides, implements an interface or carries an attribute is reached by the game without a name.</summary>
    [Fact]
    public void WhatTheGameReachesIsLeftAlone()
    {
        const string code = """
            public interface IScreen { void Draw(); }
            public abstract class Component { public abstract void Update(); public virtual void Load() { } }
            [System.Serializable]
            public class Found { public void ByTheGame() { } }
            public class Session : Component, IScreen
            {
                public override void Update() { }
                public void Draw() { }
            }
            public static class Entry { public static void Run() { new Session().Update(); IScreen s = null; Component c = null; } }
            """;

        Assert.Equal(["Entry"], Reported(code));
    }

    /// <summary>A property read through an object initializer, an Add a collection initializer calls and an extension method all count as named.</summary>
    [Fact]
    public void WhatIsReachedWithoutItsOwnNameCounts()
    {
        const string code = """
            public class Bag : System.Collections.IEnumerable
            {
                public int Size { get; set; }
                public void Add(int x) { }
                public System.Collections.IEnumerator GetEnumerator() => null;
            }
            public static class Ways { public static int Twice(this int x) => x * 2; }
            public static class Entry { public static object Run() => new Bag { 1, 2 }.ToString() + new Bag() { Size = 3 } + 2.Twice(); }
            """;

        Assert.Equal(["Entry"], Reported(code));
    }

    /// <summary>A use in generated code counts, and nothing generated is reported.</summary>
    [Fact]
    public void GeneratedCodeCountsAsAUser()
    {
        const string code = "public sealed class Phrasebook { public string Say(string key) => key; }";
        const string generated = "public sealed class Texts { public string Hello => new Phrasebook().Say(\"hello\"); public string Unused => \"x\"; }";

        Assert.Empty(Reported(code, generated));
    }
}
