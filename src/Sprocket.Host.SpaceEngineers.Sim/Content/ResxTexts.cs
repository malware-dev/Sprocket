using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Sim.Content
{
    /// <summary>The mod's texts read straight out of its .resx files, layered the way the game's <c>MyTexts.LoadTexts</c> layers them.</summary>
    /// <remarks>
    /// <c>MyTexts.resx</c> first, then <c>MyTexts.&lt;culture&gt;.resx</c> over it, then
    /// <c>MyTexts.&lt;culture&gt;-&lt;subculture&gt;.resx</c>, each replacing what it repeats. The files are the ones
    /// the mod ships, copied beside the harness.
    /// </remarks>
    public sealed class ResxTexts : ITextSource
    {
        readonly Dictionary<string, string> _texts = new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>The texts in <paramref name="culture" />, such as <c>no</c> or <c>pt-BR</c>, read from <paramref name="folder" />.</summary>
        public ResxTexts(string folder, string culture)
        {
            culture = culture ?? "";
            Read(Path.Combine(folder, "MyTexts.resx"));

            if (culture.Length == 0)
                return;

            var dash = culture.IndexOf('-');
            if (dash > 0)
                Read(Path.Combine(folder, "MyTexts." + culture.Substring(0, dash) + ".resx"));

            Read(Path.Combine(folder, "MyTexts." + culture + ".resx"));
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
