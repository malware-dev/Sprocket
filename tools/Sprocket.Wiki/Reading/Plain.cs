using System.Text;
using Lumel.Text;

namespace Sprocket.Wiki.Reading;

/// <summary>Markdown-lite as the plain words it shows, for a place that holds no more than words.</summary>
public static class Plain
{
    /// <summary>The words <paramref name="source" /> shows, its pictures left out and every run of spaces one space.</summary>
    public static string Of(string source)
    {
        return Of(Markdown.Parse(source).Blocks);
    }

    /// <summary>The words <paramref name="runs" /> show, its pictures left out and every run of spaces one space.</summary>
    public static string Of(IList<Run> runs)
    {
        var words = new StringBuilder();
        Add(runs, words);
        return Squeezed(words);
    }

    /// <summary>The first heading of the first level in <paramref name="blocks" />, inside boxes and quotes too, or null where there is none.</summary>
    public static Block Title(IList<Block> blocks)
    {
        foreach (var block in blocks)
        {
            if (block.Kind == BlockKind.Heading && block.Level == 1)
                return block;

            var inner = Title(block.Blocks);
            if (inner != null)
                return inner;
        }

        return null;
    }

    static string Of(IList<Block> blocks)
    {
        var words = new StringBuilder();
        Add(blocks, words);
        return Squeezed(words);
    }

    static void Add(IList<Block> blocks, StringBuilder words)
    {
        foreach (var block in blocks)
        {
            Add(block.Runs, words);
            words.Append(' ');
            Add(block.Blocks, words);
        }
    }

    static void Add(IList<Run> runs, StringBuilder words)
    {
        foreach (var run in runs)
        {
            if (run is ImageRun)
                continue;

            words.Append(run.IsBreak ? " " : run.Text);
        }
    }

    static string Squeezed(StringBuilder words)
    {
        var squeezed = new StringBuilder(words.Length);
        foreach (var c in words.ToString())
        {
            var space = char.IsWhiteSpace(c);
            if (space && (squeezed.Length == 0 || squeezed[^1] == ' '))
                continue;

            squeezed.Append(space ? ' ' : c);
        }

        return squeezed.ToString().TrimEnd();
    }
}
