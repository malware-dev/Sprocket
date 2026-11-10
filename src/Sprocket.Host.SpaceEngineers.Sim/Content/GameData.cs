using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Lumel.Sim;

namespace Sprocket.Sim.Content;

/// <summary>The game's definition files, from the game or the harness's cache of its content.</summary>
public static class GameData
{
    /// <summary>
    /// Each <c>.sbc</c> file directly in <paramref name="folder" /> under the content, such as <c>Data</c>, whose definitions
    /// hold a list named in <paramref name="lists" />, read; none where neither the game nor the cache has any.
    /// </summary>
    /// <remarks>A file read from the game is copied into the cache, so a machine without the game reads it too.</remarks>
    public static List<XDocument> Holding(string folder, params string[] lists)
    {
        var content = GameContent.ContentFolder;
        var root = Path.Combine(content ?? GameContent.CacheDirectory, folder);
        var found = new List<XDocument>();
        if (!Directory.Exists(root))
            return found;

        var files = Directory.GetFiles(root, "*.sbc");
        Array.Sort(files, StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            if (!lists.Any(list => text.Contains("<" + list + ">")))
                continue;

            var document = XDocument.Parse(text);
            if (document.Root == null || !lists.Any(list => document.Root.Element(list) != null))
                continue;

            found.Add(document);
            if (content != null)
                GameContent.Resolve(Path.Combine(folder, Path.GetFileName(file)));
        }

        return found;
    }
}
