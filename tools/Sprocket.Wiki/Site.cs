using Sprocket.Wiki.Pictures;
using Sprocket.Wiki.Reading;
using Sprocket.Wiki.Writing;

namespace Sprocket.Wiki;

/// <summary>The project's GitHub wiki, written whole from the codex and the pages written for the wiki alone.</summary>
public sealed class Site
{
    /// <summary>The page GitHub opens a wiki at.</summary>
    const string Home = "Home";

    readonly Codex _codex;
    readonly Catalog _catalog;
    readonly Page _page;
    readonly string _own;

    /// <summary>The wiki for <paramref name="codex" />, its pictures found in <paramref name="catalog" />.</summary>
    public Site(Codex codex, Catalog catalog) : this(codex, catalog, null)
    {
    }

    /// <summary>
    /// The wiki for <paramref name="codex" />, its pictures found in <paramref name="catalog" />, with the pages in
    /// <paramref name="own" />, or none where it is null: pages for the wiki alone, which the game never shows.
    /// </summary>
    /// <remarks>Each of those is GitHub markdown, copied as it is, and named for its file.</remarks>
    public Site(Codex codex, Catalog catalog, string own)
    {
        _codex = codex;
        _catalog = catalog;
        _page = new Page(new Inline(codex, catalog));
        _own = own;
    }

    /// <summary>The name of <paramref name="term" />'s page in the wiki, which GitHub shows as its title with each hyphen a space.</summary>
    public static string Name(Term term)
    {
        return term.Id == Codex.Home ? Home : term.Title.Replace(' ', '-');
    }

    /// <summary>Writes the wiki into <paramref name="folder" />, a clone of the wiki's repository or a folder of its own, in place of the pages and pictures there.</summary>
    /// <exception cref="InvalidDataException">The codex holds something the wiki cannot show, or the folder is not a wiki's.</exception>
    public void Write(string folder)
    {
        foreach (var term in _codex.Terms)
        {
            if (term.Id != Codex.Home && term.Id != term.Title.ToLowerInvariant().Replace(' ', '-'))
                throw new InvalidDataException(term.Id + " is titled " + term.Title + ": an id is its page's English title, in lowercase with a hyphen for each space, so a link to it finds its page");
        }

        var pages = new Dictionary<string, string>();
        foreach (var term in _codex.Terms)
        {
            try
            {
                pages.Add(Name(term) + ".md", _page.Write(term.Page));
            }
            catch (InvalidDataException e)
            {
                throw new InvalidDataException(term.Id + ".md: " + e.Message, e);
            }
        }

        var own = new List<string>();
        if (_own != null && Directory.Exists(_own))
        {
            foreach (var file in Directory.GetFiles(_own, "*.md").OrderBy(name => name, StringComparer.Ordinal))
            {
                var name = Path.GetFileName(file);
                if (pages.Keys.Any(page => string.Equals(page, name, StringComparison.OrdinalIgnoreCase)))
                    throw new InvalidDataException(name + " is a page of the codex as well as one of the wiki's own");

                pages.Add(name, File.ReadAllText(file));
                own.Add(Path.GetFileNameWithoutExtension(name));
            }
        }

        Clear(folder);
        foreach (var page in pages)
            File.WriteAllText(Path.Combine(folder, page.Key), page.Value);

        File.WriteAllText(Path.Combine(folder, Sidebar.File), Sidebar.Of(_codex, own));
        _catalog.Write(folder);
    }

    /// <summary>Takes the pages and pictures out of <paramref name="folder" />, so the wiki holds exactly what the codex writes into it.</summary>
    /// <remarks>Refuses a folder holding anything but a wiki, so a mistyped folder is never emptied.</remarks>
    static void Clear(string folder)
    {
        Directory.CreateDirectory(folder);

        var git = Path.Combine(folder, ".git", "config");
        var wiki = File.Exists(git) && File.ReadAllText(git).Contains(".wiki.git", StringComparison.OrdinalIgnoreCase);
        if (!wiki && Directory.EnumerateFileSystemEntries(folder).Any())
            throw new InvalidDataException(folder + " is neither empty nor a clone of a wiki, so it is left as it is");

        foreach (var page in Directory.GetFiles(folder, "*.md"))
            File.Delete(page);

        var images = Path.Combine(folder, "images");
        if (Directory.Exists(images))
            Directory.Delete(images, true);
    }
}
