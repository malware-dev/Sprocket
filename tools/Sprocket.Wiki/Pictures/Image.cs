namespace Sprocket.Wiki.Pictures;

/// <summary>One of Sprocket's own pictures, as the wiki keeps it.</summary>
public sealed class Image
{
    /// <summary>The picture kept at <paramref name="path" /> in the wiki, and at <paramref name="dark" /> for GitHub's dark theme where it has a copy of its own for it.</summary>
    public Image(string path, string dark)
    {
        Path = path;
        Dark = dark;
    }

    /// <summary>Where the picture is kept, relative to the wiki's pages, as in <c>images/marks/light/signal.png</c>.</summary>
    public string Path { get; }

    /// <summary>Where the copy for GitHub's dark theme is kept, or null where one picture serves both themes.</summary>
    public string Dark { get; }

    /// <summary>Whether the picture is a mark, drawn in a line of words in the ink of the theme it is shown in.</summary>
    public bool Mark => Dark != null;
}
