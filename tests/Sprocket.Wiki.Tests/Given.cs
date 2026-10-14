using Sprocket.Wiki.Pictures;
using Sprocket.Wiki.Reading;
using Sprocket.Wiki.Writing;

namespace Sprocket.Wiki.Tests;

/// <summary>What the tests write from: the repository's own pictures, and codices made up or read from the repository.</summary>
static class Given
{
    /// <summary>The repository the tests were built in.</summary>
    public static string Repository
    {
        get
        {
            for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
            {
                if (File.Exists(Path.Combine(folder.FullName, "Sprocket.slnx")))
                    return folder.FullName;
            }

            throw new InvalidOperationException("no Sprocket.slnx above " + AppContext.BaseDirectory);
        }
    }

    /// <summary>The codex the mod ships.</summary>
    public static Codex Codex => Codex.Read(Path.Combine(Repository, "src", "Sprocket.Host.SpaceEngineers", "Content", "Data", "Codex"));

    /// <summary>Sprocket's own pictures, as the repository holds them.</summary>
    public static Catalog Catalog()
    {
        var fonts = Path.Combine(Repository, "src", "Maltek.Symbols", "Content", "Fonts", "Maltek");
        return new Catalog(new Glyphs(Path.Combine(fonts, "MaltekSymbols.xml"), Path.Combine(fonts, "MaltekSymbols-0.png")),
            Path.Combine(Repository, "art", "thumbnail", "sprocket-mark.png"));
    }

    /// <summary>Two terms, <c>a</c> and <c>b</c>, each with a tooltip naming it.</summary>
    public static Codex Terms => new(new[]
    {
        new Term("a", "A", "# A\n", "An **a** is ![](mark:signal)&nbsp;one thing.", false),
        new Term("b", "B", "# B\n", "A \"b\" is another.", false)
    });

    /// <summary>The wiki page for the markdown-lite <paramref name="source" />, its links leading into <see cref="Terms" />.</summary>
    public static string Page(string source)
    {
        return new Page(new Inline(Terms, Catalog())).Write(source);
    }

    /// <summary>A new, empty folder under the system's temporary folder, for the test to delete when it is done.</summary>
    public static string Folder()
    {
        var folder = Path.Combine(Path.GetTempPath(), "sprocket-wiki-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }
}
