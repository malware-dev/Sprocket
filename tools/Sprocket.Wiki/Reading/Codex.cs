using Lumel.Text;

namespace Sprocket.Wiki.Reading;

/// <summary>Every term the codex explains, in the order its contents list them.</summary>
public sealed class Codex
{
    /// <summary>The term that is the codex's front page.</summary>
    public const string Home = "home";

    /// <summary>The language the codex is written in, and the only one the wiki shows.</summary>
    const string Original = "en";

    readonly List<Term> _terms;

    /// <summary>A codex of <paramref name="terms" />, in their order.</summary>
    public Codex(IEnumerable<Term> terms)
    {
        _terms = new List<Term>(terms);
    }

    /// <summary>Every term, in the order the contents list them.</summary>
    public IReadOnlyList<Term> Terms => _terms;

    /// <summary>The codex in <paramref name="folder" />: the terms its <c>index</c> lists, each read from the original language's folder.</summary>
    /// <remarks>The index is read as the game reads it: one id a line, <c>#</c> starting a comment, and <c>&gt;</c> beginning a section.</remarks>
    public static Codex Read(string folder)
    {
        var terms = new List<Term>();
        foreach (var raw in File.ReadAllLines(Path.Combine(folder, "index")))
        {
            var id = raw.Trim();
            var opens = id.Length > 0 && id[0] == '>';
            if (opens)
                id = id.Substring(1).Trim();

            if (id.Length == 0 || id[0] == '#')
                continue;

            var page = Text(folder, id + ".md");
            var title = Plain.Title(Markdown.Parse(page).Blocks);
            if (title == null)
                throw new InvalidDataException(id + ".md has no heading to name it, a line starting \"# \"");

            terms.Add(new Term(id, Plain.Of(title.Runs), page, Text(folder, id + ".tldr.md"), opens));
        }

        return new Codex(terms);
    }

    /// <summary>The term called <paramref name="id" />, or null where the codex has none.</summary>
    public Term Find(string id)
    {
        foreach (var term in _terms)
        {
            if (term.Id == id)
                return term;
        }

        return null;
    }

    static string Text(string folder, string name)
    {
        var path = Path.Combine(folder, Original, name);
        if (!File.Exists(path))
            throw new InvalidDataException("the index lists " + name + ", which " + Original + " has no file for");

        return File.ReadAllText(path);
    }
}
