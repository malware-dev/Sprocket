using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace Sprocket.Sim.Content
{
    /// <summary>Where each sprite a text panel can draw keeps its picture, read from the game's own LCD texture definitions and the mod's.</summary>
    /// <remarks>
    ///     The definitions are <c>Data/LCDTextures*.sbc</c> under the game's content, in two shapes of id: an
    ///     <c>Id</c> element holding a <c>SubtypeId</c>, or one carrying it as a <c>Subtype</c> attribute. Each names
    ///     its picture as a <c>SpritePath</c>, or only a <c>TexturePath</c>. The mod's own are read the same way from
    ///     its content, copied beside the harness, together with the transparent materials it declares, which the
    ///     editor draws by name in the same way. Read once, the first time a sprite is asked for; on a machine
    ///     without the game only the mod's names are known.
    /// </remarks>
    public sealed class LcdSprites
    {
        Dictionary<string, string> _paths;
        Dictionary<string, string> _own;

        /// <summary>The folder the mod's own content is copied to beside the harness.</summary>
        public static string Mod => Path.Combine(AppContext.BaseDirectory, "Content", "Mod");

        /// <summary>Where the picture of the sprite <paramref name="name" /> lives under the game's content, or null for a name the game does not define.</summary>
        public string PathFor(string name)
        {
            if (_paths == null)
                _paths = Read(GameContent.ContentFolder);

            string path;
            return name != null && _paths.TryGetValue(name, out path) ? path : null;
        }

        /// <summary>A picture file for the sprite <paramref name="name" />, decoded where it has to be, or null where there is none.</summary>
        public string Resolve(string name)
        {
            if (_own == null)
            {
                _own = Read(Mod);
                Materials(Mod, _own);
            }

            string own;
            if (name != null && _own.TryGetValue(name, out own))
                return GameImages.ResolveFile(Path.Combine(Mod, own), Path.Combine("Mod", own));

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

        /// <summary>Adds every transparent material the definitions under the content folder <paramref name="content" /> declare to <paramref name="paths" />, by where its picture is under that folder.</summary>
        static void Materials(string content, Dictionary<string, string> paths)
        {
            var data = Path.Combine(content, "Data");
            if (!Directory.Exists(data))
                return;

            foreach (var file in Directory.GetFiles(data, "TransparentMaterials*.sbc"))
            {
                foreach (var definition in XDocument.Load(file).Descendants("TransparentMaterial"))
                {
                    var subtype = (string)definition.Element("Id")?.Element("SubtypeId");
                    var picture = (string)definition.Element("Texture");
                    if (!string.IsNullOrEmpty(subtype) && !string.IsNullOrEmpty(picture) && File.Exists(Path.Combine(content, picture)))
                        paths[subtype] = picture.Replace('\\', '/');
                }
            }
        }

        /// <summary>Every sprite the definitions under the content folder <paramref name="content" /> name, by where its picture is under that folder.</summary>
        static Dictionary<string, string> Read(string content)
        {
            var paths = new Dictionary<string, string>(StringComparer.Ordinal);
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
