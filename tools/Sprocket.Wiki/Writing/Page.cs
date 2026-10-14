using Lumel.Text;

namespace Sprocket.Wiki.Writing;

/// <summary>Writes a codex page as a page of GitHub's wiki: the blocks markdown-lite reads, each written as GitHub reads the same.</summary>
public sealed class Page
{
    /// <summary>A line break inside a paragraph or an item, which GitHub reads as a break only with a backslash before it.</summary>
    const string HardBreak = "\\\n";

    readonly Inline _inline;

    /// <summary>Pages whose words are written by <paramref name="inline" />.</summary>
    public Page(Inline inline)
    {
        _inline = inline;
    }

    /// <summary>The GitHub markdown for the markdown-lite <paramref name="source" />, its opening heading of the first level left out.</summary>
    /// <remarks>The wiki shows a page's title above it, as the codex shows a page's opening heading in its title bar.</remarks>
    /// <exception cref="InvalidDataException">The page asks for something the wiki cannot show.</exception>
    public string Write(string source)
    {
        var blocks = Markdown.Parse(source).Blocks;
        var titled = blocks.Count > 0 && blocks[0].Kind == BlockKind.Heading && blocks[0].Level == 1;

        var lines = new List<string>();
        Write(blocks, titled ? 1 : 0, lines);
        return string.Join("\n", lines) + "\n";
    }

    /// <summary>Writes <paramref name="blocks" /> from <paramref name="from" /> into <paramref name="lines" />, a blank line between each, apart from the items of one list.</summary>
    void Write(IList<Block> blocks, int from, List<string> lines)
    {
        var last = (BlockKind?)null;
        for (var i = from; i < blocks.Count; i++)
        {
            var block = blocks[i];
            var listed = (block.Kind == BlockKind.Bullet || block.Kind == BlockKind.Numbered) && block.Kind == last;
            if (lines.Count > 0 && !listed)
                lines.Add("");

            Write(block, lines);
            last = block.Kind;
        }
    }

    void Write(Block block, List<string> lines)
    {
        switch (block.Kind)
        {
            case BlockKind.Paragraph:
                lines.AddRange(_inline.Write(block.Runs, HardBreak, false).Split('\n'));
                break;
            case BlockKind.Heading:
                lines.Add(new string('#', block.Level) + " " + _inline.Write(block.Runs, " ", false));
                break;
            case BlockKind.Bullet:
                Item(block, "- ", lines);
                break;
            case BlockKind.Numbered:
                Item(block, block.Level + ". ", lines);
                break;
            case BlockKind.Rule:
                lines.Add("---");
                break;
            case BlockKind.Table:
                Table(block, lines);
                break;
            case BlockKind.Quote:
                lines.AddRange(Quoted(Inner(block)));
                break;
            case BlockKind.Box:
                Box(block, lines);
                break;
        }
    }

    /// <summary>Writes an item of a list after <paramref name="marker" />, the lines after its first indented to stay in it.</summary>
    void Item(Block block, string marker, List<string> lines)
    {
        var written = _inline.Write(block.Runs, HardBreak, false).Split('\n');
        lines.Add(marker + written[0]);
        for (var i = 1; i < written.Length; i++)
            lines.Add(new string(' ', marker.Length) + written[i]);
    }

    /// <summary>Writes a table, with a heading row of empty cells where it has none, as GitHub's tables always have one.</summary>
    /// <remarks>A column's share of the spare width is left out: GitHub sizes a table to its cells.</remarks>
    void Table(Block block, List<string> lines)
    {
        var rows = block.Blocks;
        var headed = rows.Count > 0 && rows[0].Level == 1;

        lines.Add(headed ? Row(rows[0]) : "|" + string.Concat(Enumerable.Repeat("  |", block.Columns.Count)));
        lines.Add("|" + string.Concat(block.Columns.Select(column => column.Align switch
        {
            Alignment.Center => ":-:|",
            Alignment.Right => "--:|",
            _ => "---|"
        })));

        for (var i = headed ? 1 : 0; i < rows.Count; i++)
            lines.Add(Row(rows[i]));
    }

    string Row(Block row)
    {
        return "| " + string.Join(" | ", row.Blocks.Select(cell => _inline.Write(cell.Runs, "<br>", true))) + " |";
    }

    /// <summary>Writes a box as GitHub can show it: <c>player</c> as a note, <c>warning</c> as a warning, and <c>center</c> or <c>right</c> as a division aligned so.</summary>
    /// <exception cref="InvalidDataException">The box asks for something the wiki has no form for.</exception>
    void Box(Block block, List<string> lines)
    {
        string align = null;
        string alert = null;
        foreach (var name in block.Names)
        {
            switch (name.ToLowerInvariant())
            {
                case "left":
                    align = null;
                    break;
                case "center":
                case "right":
                    align = name.ToLowerInvariant();
                    break;
                case "player":
                    alert = "NOTE";
                    break;
                case "warning":
                    alert = "WARNING";
                    break;
                default:
                    throw new InvalidDataException("a box asks for " + name + ", which the wiki has no form for");
            }
        }

        var inner = Inner(block);
        if (align != null)
            inner = new[] { "<div align=\"" + align + "\">", "" }.Concat(inner).Concat(new[] { "", "</div>" }).ToList();

        if (alert != null)
            inner = Quoted(new[] { "[!" + alert + "]" }.Concat(inner).ToList());

        lines.AddRange(inner);
    }

    List<string> Inner(Block block)
    {
        var inner = new List<string>();
        Write(block.Blocks, 0, inner);
        return inner;
    }

    static List<string> Quoted(List<string> lines)
    {
        return lines.Select(line => line.Length == 0 ? ">" : "> " + line).ToList();
    }
}
