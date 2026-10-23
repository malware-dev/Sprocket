using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Sim.Content
{
    /// <summary>The mod's texts read straight out of its .resx files, layered the way the game's <c>MyTexts.LoadTexts</c> layers them.</summary>
    /// <remarks>
    /// Every file with no culture first, such as <c>MyTexts.resx</c> and <c>Codex.resx</c>, then each one's
    /// <c>&lt;culture&gt;</c> file over it, then its <c>&lt;culture&gt;-&lt;subculture&gt;</c> file, each replacing what it
    /// repeats. The files are the ones the mod ships, copied beside the harness.
    /// </remarks>
    public sealed class ResxTexts : ITextSource
    {
        readonly Dictionary<string, string> _texts = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>The texts in <paramref name="culture" />, such as <c>no</c> or <c>pt-BR</c>, read from <paramref name="folder" />.</summary>
        public ResxTexts(string folder, string culture)
        {
            culture = culture ?? "";
            var stems = new List<string>();
            foreach (var path in Directory.GetFiles(folder, "*.resx"))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                if (name.IndexOf('.') < 0)
                    stems.Add(name);
            }

            stems.Sort(StringComparer.Ordinal);
            foreach (var stem in stems)
                Read(Path.Combine(folder, stem + ".resx"));

            if (culture.Length == 0)
                return;

            var dash = culture.IndexOf('-');
            foreach (var stem in stems)
            {
                if (dash > 0)
                    Read(Path.Combine(folder, stem + "." + culture.Substring(0, dash) + ".resx"));

                Read(Path.Combine(folder, stem + "." + culture + ".resx"));
            }
        }

        /// <summary>The folder the mod's texts are copied to beside the harness.</summary>
        public static string Beside => Path.Combine(AppContext.BaseDirectory, "Content", "Localization");

        /// <inheritdoc />
        public string? Find(string key)
        {
            return _texts.TryGetValue(key, out var text) ? text : null;
        }

        void Read(string path)
        {
            if (!File.Exists(path))
                return;

            var root = XDocument.Load(path).Root;
            if (root == null)
                return;

            foreach (var data in root.Elements("data"))
            {
                var name = data.Attribute("name");
                var value = data.Element("value");
                if (name != null && value != null)
                    _texts[name.Value] = value.Value;
            }
        }
    }
}
