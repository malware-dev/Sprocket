using Sprocket.Wiki.Pictures;
using Sprocket.Wiki.Reading;

namespace Sprocket.Wiki;

/// <summary>Writes the project's GitHub wiki from the codex and docs/wiki: <c>dotnet run --project tools/Sprocket.Wiki -- &lt;wiki folder&gt;</c>.</summary>
public static class Program
{
    /// <summary>Writes the wiki into the folder named by the one argument, a clone of the wiki's repository or an empty folder.</summary>
    public static int Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.Error.WriteLine("usage: Sprocket.Wiki <wiki folder>");
            return 2;
        }

        var repository = Repository(Directory.GetCurrentDirectory()) ?? Repository(AppContext.BaseDirectory);
        if (repository == null)
        {
            Console.Error.WriteLine("no Sprocket.slnx above " + Directory.GetCurrentDirectory() + " or " + AppContext.BaseDirectory);
            return 2;
        }

        try
        {
            var codex = Codex.Read(Path.Combine(repository, "src", "Sprocket.Host.SpaceEngineers", "Content", "Data", "Codex"));
            var fonts = Path.Combine(repository, "src", "Maltek.Symbols", "Content", "Fonts", "Maltek");
            var glyphs = new Glyphs(Path.Combine(fonts, "MaltekSymbols.xml"), Path.Combine(fonts, "MaltekSymbols-0.png"));
            var catalog = new Catalog(glyphs, Path.Combine(repository, "art", "thumbnail", "sprocket-mark.png"));

            var folder = Path.GetFullPath(args[0]);
            new Site(codex, catalog, Path.Combine(repository, "docs", "wiki")).Write(folder);
            Console.WriteLine("wrote the codex's " + codex.Terms.Count + " pages and the wiki's own into " + folder);
            return 0;
        }
        catch (InvalidDataException e)
        {
            Console.Error.WriteLine(e.Message);
            return 1;
        }
    }

    /// <summary>The repository holding <paramref name="from" />, found by its solution, or null where none does.</summary>
    static string Repository(string from)
    {
        for (var folder = new DirectoryInfo(from); folder != null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "Sprocket.slnx")))
                return folder.FullName;
        }

        return null;
    }
}
