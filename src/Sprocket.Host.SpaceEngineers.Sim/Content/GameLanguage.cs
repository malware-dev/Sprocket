using System.IO;
using Lumel.Sim;

namespace Sprocket.Sim.Content;

/// <summary>The game's own texts, from its content: what its items and terminal controls are called in one of its languages.</summary>
public static class GameLanguage
{
    const string Folder = "Data/Localization";

    /// <summary>
    /// The game's texts in <paramref name="culture" />, such as <c>no</c>, or in English where it is empty; null where neither the
    /// game nor the harness's cache of its content has them.
    /// </summary>
    /// <remarks>A file read from the game is copied into the cache, so a machine without the game reads it too.</remarks>
    public static ResxTexts? In(string culture)
    {
        var english = GameContent.Resolve(Path.Combine(Folder, "MyTexts.resx"));
        if (english == null)
            return null;

        culture ??= "";
        var dash = culture.IndexOf('-');
        if (dash > 0)
            GameContent.Resolve(Path.Combine(Folder, "MyTexts." + culture.Substring(0, dash) + ".resx"));

        if (culture.Length > 0)
            GameContent.Resolve(Path.Combine(Folder, "MyTexts." + culture + ".resx"));

        return new ResxTexts(Path.GetDirectoryName(english)!, culture);
    }
}
