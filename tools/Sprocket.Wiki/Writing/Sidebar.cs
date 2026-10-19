using System.Text;
using Sprocket.Wiki.Reading;

namespace Sprocket.Wiki.Writing;

/// <summary>The wiki's sidebar, which GitHub shows beside every page: the codex's contents, each section's terms listed under its head, then the wiki's own pages.</summary>
public static class Sidebar
{
    /// <summary>The file GitHub shows as the sidebar.</summary>
    public const string File = "_Sidebar.md";

    /// <summary>The sidebar listing the terms of <paramref name="codex" /> in its order.</summary>
    public static string Of(Codex codex)
    {
        return Of(codex, Array.Empty<string>());
    }

    /// <summary>The sidebar listing the terms of <paramref name="codex" /> in its order, then each page of <paramref name="own" />, named as its file is, on its own.</summary>
    public static string Of(Codex codex, IReadOnlyList<string> own)
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

        foreach (var page in own)
            sidebar.Append('\n').Append("**[").Append(Inline.Escape(page.Replace('-', ' '))).Append("](").Append(page).Append(")**\n");

        return sidebar.ToString();
    }
}
