using System;
using System.IO;
using System.Text;
using BenchmarkDotNet.Attributes;
using Lumel.Text;

namespace Sprocket.Benchmarks;

/// <summary>Reading markdown-lite into a document: the shapes the profile reads, and one codex page as written.</summary>
[MemoryDiagnoser]
[InProcess]
public class ParseBenchmarks
{
    string _imagesUnclosed = "";
    string _bracketsUnclosed = "";
    string _linksUnclosed = "";
    string _bracketsOnly = "";
    string _unclosedClosers = "";
    string _page = "";

    /// <summary>Builds the texts once, so only reading them is timed.</summary>
    [GlobalSetup]
    public void Setup()
    {
        _imagesUnclosed = Repeat("![a](b ", 30000);
        _bracketsUnclosed = Repeat("![a ", 30000);
        _linksUnclosed = Repeat("[a](b ", 30000);
        _bracketsOnly = Repeat("[a] ", 30000);
        _unclosedClosers = Repeat("a* ", 30000);
        _page = File.ReadAllText(Path.Combine(Root(), "src", "Sprocket.Host.SpaceEngineers", "Content", "Data", "Codex", "en", "sequence.md"));
    }

    /// <summary>Thirty thousand images opened and never closed.</summary>
    [Benchmark]
    public Document ImagesUnclosed() => Markdown.Parse(_imagesUnclosed);

    /// <summary>Thirty thousand image brackets with no closing bracket.</summary>
    [Benchmark]
    public Document BracketsUnclosed() => Markdown.Parse(_bracketsUnclosed);

    /// <summary>Thirty thousand links opened and never closed.</summary>
    [Benchmark]
    public Document LinksUnclosed() => Markdown.Parse(_linksUnclosed);

    /// <summary>Thirty thousand bracketed words that are no links.</summary>
    [Benchmark]
    public Document BracketsOnly() => Markdown.Parse(_bracketsOnly);

    /// <summary>Thirty thousand emphasis closers with nothing to close.</summary>
    [Benchmark]
    public Document UnclosedClosers() => Markdown.Parse(_unclosedClosers);

    /// <summary>The codex's sequence page, as written.</summary>
    [Benchmark]
    public Document CodexPage() => Markdown.Parse(_page);

    static string Repeat(string piece, int times)
    {
        var text = new StringBuilder(piece.Length * times);
        for (var i = 0; i < times; i++)
            text.Append(piece);

        return text.ToString();
    }

    /// <summary>The repository this runs from, found by walking up to its solution.</summary>
    static string Root()
    {
        var at = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        while (at != null && !File.Exists(Path.Combine(at.FullName, "Sprocket.slnx")))
            at = at.Parent;

        return at?.FullName ?? throw new DirectoryNotFoundException("no Sprocket.slnx above " + AppDomain.CurrentDomain.BaseDirectory);
    }
}
