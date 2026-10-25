using System.Collections.Generic;
using System.Text;

namespace Sprocket.Sim.Scenarios;

/// <summary>A scenario's checks as the text of its sign.</summary>
/// <remarks>A text panel never wraps a line, so the text is wrapped here.</remarks>
public static class Instructions
{
    /// <summary>The sign's font size.</summary>
    public const string FontSize = "1";

    /// <summary>The longest line, in letters, that fits the sign at <see cref="FontSize" />, as seen in the game.</summary>
    public const int Width = 40;

    /// <summary>What is done first with every pasted scenario, whose blueprint gives its blocks no owner.</summary>
    public const string First = "First, take ownership of every block.";

    /// <summary>The sign's text for <paramref name="checks" />.</summary>
    public static string Of(string name, IReadOnlyList<GameCheck> checks)
    {
        var text = new StringBuilder();
        Wrap(text, name.ToUpperInvariant(), "");
        Wrap(text, First, "");
        text.Append('\n');
        for (var i = 0; i < checks.Count; i++)
        {
            var check = checks[i];
            Wrap(text, $"{i + 1}. {check.Title}", "   ");
            Wrap(text, "Setup: " + check.Setup, "   ");
            foreach (var step in check.Steps)
                Wrap(text, "- " + step, "  ");

            Wrap(text, "Expect: " + check.Expect, "   ");
            text.Append('\n');
        }

        return text.ToString().TrimEnd('\n');
    }

    static void Wrap(StringBuilder text, string line, string indent)
    {
        var current = new StringBuilder();
        foreach (var word in line.Split(' '))
        {
            if (current.Length > 0 && current.Length + 1 + word.Length > Width)
            {
                text.Append(current).Append('\n');
                current.Clear().Append(indent);
            }
            else if (current.Length > 0 && current.ToString() != indent)
            {
                current.Append(' ');
            }

            current.Append(word);
        }

        text.Append(current).Append('\n');
    }
}
