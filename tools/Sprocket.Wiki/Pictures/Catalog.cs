using SkiaSharp;

namespace Sprocket.Wiki.Pictures;

/// <summary>The pictures the wiki may show, each one Sprocket's own, found by the locator a codex page names it by.</summary>
/// <remarks>
/// A locator outside the catalog stops the wiki being written, so a picture of the game's can never be published by being named
/// on a page. Each picture is written into the wiki once a page has asked for it.
/// </remarks>
public sealed class Catalog
{
    const string MarkPrefix = "mark:";

    /// <summary>The ink a mark is drawn in on GitHub's light theme: the theme's own text, Primer's <c>fgColor-default</c>.</summary>
    static readonly SKColor Light = new(0x1f, 0x23, 0x28);

    /// <summary>The ink a mark is drawn in on GitHub's dark theme: the theme's own text, Primer's <c>fgColor-default</c>.</summary>
    static readonly SKColor Dark = new(0xf0, 0xf6, 0xfc);

    readonly Glyphs _glyphs;
    readonly Dictionary<string, char> _marks = new();
    readonly Dictionary<string, string> _files = new();
    readonly HashSet<string> _asked = new();

    /// <summary>The marks <paramref name="glyphs" /> draws, and the logo drawn as <paramref name="logo" />.</summary>
    /// <remarks>Each mark is the glyph <c>MarkImages</c> draws for the same name in the game.</remarks>
    public Catalog(Glyphs glyphs, string logo)
    {
        _glyphs = glyphs;
        _marks.Add("sequence", 'q');
        _marks.Add("signal", 'n');
        _marks.Add("trigger", 'T');
        _marks.Add("action", 'i');
        _marks.Add("awaited", 'A');
        _marks.Add("value", 'l');
        _marks.Add("block", 'B');
        _marks.Add("blueprint", 'b');
        _marks.Add("logic-system", 'y');
        _marks.Add("installation", 'I');
        _marks.Add("play", 'g');
        _marks.Add("stop", 't');
        _marks.Add("pause", 'e');
        _marks.Add("delete", 'd');
        _marks.Add("duplicate", '2');
        _marks.Add("rename", 'p');
        _marks.Add("undo", 'u');
        _marks.Add("redo", 'r');
        _files.Add("Sprocket_Mark", logo);
    }

    /// <summary>Every mark, by the name a locator gives it after <c>mark:</c>, and the glyph that draws it.</summary>
    public IReadOnlyDictionary<string, char> Marks => _marks;

    /// <summary>The picture <paramref name="locator" /> names, kept to be written into the wiki.</summary>
    /// <exception cref="InvalidDataException">The locator names none of Sprocket's own pictures.</exception>
    public Image Find(string locator)
    {
        var image = Placed(locator) ?? throw new InvalidDataException("the picture " + locator + " is none of Sprocket's own, so the wiki cannot show it");
        _asked.Add(locator);
        return image;
    }

    /// <summary>Writes every picture asked for into the wiki at <paramref name="folder" />.</summary>
    public void Write(string folder)
    {
        foreach (var locator in _asked)
        {
            var image = Placed(locator);
            if (_marks.TryGetValue(Name(locator), out var symbol))
            {
                File.WriteAllBytes(Prepared(folder, image.Path), _glyphs.Cut(symbol, Light));
                File.WriteAllBytes(Prepared(folder, image.Dark), _glyphs.Cut(symbol, Dark));
            }
            else
                File.Copy(_files[locator], Prepared(folder, image.Path), true);
        }
    }

    /// <summary>Where <paramref name="path" /> is in <paramref name="folder" />, its folder made.</summary>
    static string Prepared(string folder, string path)
    {
        var full = Path.Combine(folder, path);
        Directory.CreateDirectory(Path.GetDirectoryName(full));
        return full;
    }

    Image Placed(string locator)
    {
        if (locator.StartsWith(MarkPrefix, StringComparison.Ordinal))
            return _marks.ContainsKey(Name(locator))
                ? new Image("images/marks/light/" + Name(locator) + ".png", "images/marks/dark/" + Name(locator) + ".png")
                : null;

        return _files.TryGetValue(locator, out var file) ? new Image("images/" + Path.GetFileName(file), null) : null;
    }

    static string Name(string locator)
    {
        return locator.StartsWith(MarkPrefix, StringComparison.Ordinal) ? locator.Substring(MarkPrefix.Length) : locator;
    }
}
