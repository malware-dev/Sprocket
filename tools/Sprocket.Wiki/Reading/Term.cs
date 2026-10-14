namespace Sprocket.Wiki.Reading;

/// <summary>One term of the codex, in the language it is written in.</summary>
public sealed class Term
{
    /// <summary>The term <paramref name="id" />, named <paramref name="title" />, explained by <paramref name="page" /> and <paramref name="tooltip" />.</summary>
    public Term(string id, string title, string page, string tooltip, bool opens)
    {
        Id = id;
        Title = title;
        Page = page;
        Tooltip = tooltip;
        Opens = opens;
    }

    /// <summary>What a link names the term by, as in <c>[installations](installation)</c>.</summary>
    public string Id { get; }

    /// <summary>The term's name, as its page's first heading of the first level says it.</summary>
    public string Title { get; }

    /// <summary>The term's page, in markdown-lite.</summary>
    public string Page { get; }

    /// <summary>The sentence or two shown where a link to the term is rested on, in markdown-lite.</summary>
    public string Tooltip { get; }

    /// <summary>Whether the term begins a section of the contents: its name heads the section, and the terms after it are listed under it.</summary>
    public bool Opens { get; }
}
