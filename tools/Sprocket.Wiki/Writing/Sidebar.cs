using System.Text;
using Sprocket.Wiki.Reading;

namespace Sprocket.Wiki.Writing;

/// <summary>The wiki's sidebar, which GitHub shows beside every page: the codex's contents, each section's terms listed under its head.</summary>
public static class Sidebar
{
    /// <summary>The file GitHub shows as the sidebar.</summary>
    public const string File = "_Sidebar.md";

    /// <summary>The sidebar listing the terms of <paramref name="codex" /> in its order.</summary>
    public static string Of(Codex codex)
    {
        var sidebar = new StringBuilder();
        foreach (var term in codex.Terms)
        {
            var link = "[" + Inline.Escape(term.Title) + "](" + term.Id + ")";
            if (term.Opens)
                sidebar.Append('\n').Append("**").Append(link).Append("**\n\n");
            else
                sidebar.Append("- ").Append(link).Append('\n');
        }

        return sidebar.ToString();
    }
}
