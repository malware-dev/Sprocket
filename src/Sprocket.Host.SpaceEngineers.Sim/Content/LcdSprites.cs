using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Sprocket.Sim.Content
{
    /// <summary>Where each sprite a text panel can draw keeps its picture, read from the game's own LCD texture definitions.</summary>
    /// <remarks>
    ///     The definitions are <c>Data/LCDTextures*.sbc</c> under the game's content, in two shapes of id: an
    ///     <c>Id</c> element holding a <c>SubtypeId</c>, or one carrying it as a <c>Subtype</c> attribute. Each names
    ///     its picture as a <c>SpritePath</c>, or only a <c>TexturePath</c>. Read once, the first time a sprite is
    ///     asked for; on a machine without the game every name is unknown.
    /// </remarks>
    public sealed class LcdSprites
    {
        Dictionary<string, string> _paths;

        /// <summary>Where the picture of the sprite <paramref name="name" /> lives under the game's content, or null for a name the game does not define.</summary>
        public string PathFor(string name)
        {
            if (_paths == null)
                _paths = Read();

            string path;
            return name != null && _paths.TryGetValue(name, out path) ? path : null;
        }

        /// <summary>A picture file for the sprite <paramref name="name" />, decoded where it has to be, or null where there is none.</summary>
        public string Resolve(string name)
        {
            var relative = PathFor(name);
            if (relative == null)
                return null;

            if (!relative.EndsWith(".dds", StringComparison.OrdinalIgnoreCase))
            {
                var content = GameContent.ContentFolder;
                var direct = content == null ? null : Path.Combine(content, relative);
                return direct != null && File.Exists(direct) ? direct : null;
            }

            return GameImages.TryResolve(relative);
        }

        static Dictionary<string, string> Read()
        {
            var paths = new Dictionary<string, string>(StringComparer.Ordinal);
            var content = GameContent.ContentFolder;
            var data = content == null ? null : Path.Combine(content, "Data");
            if (data == null || !Directory.Exists(data))
                return paths;

            foreach (var file in Directory.GetFiles(data, "LCDTextures*.sbc"))
            {
                foreach (var definition in XDocument.Load(file).Descendants("LCDTextureDefinition"))
                {
                    var id = definition.Element("Id");
                    var subtype = (string)id?.Attribute("Subtype") ?? (string)id?.Element("SubtypeId");
                    var picture = (string)definition.Element("SpritePath") ?? (string)definition.Element("TexturePath");
                    if (!string.IsNullOrEmpty(subtype) && !string.IsNullOrEmpty(picture))
                        paths[subtype] = picture.Replace('\\', '/');
                }
            }

            return paths;
        }
    }
}
