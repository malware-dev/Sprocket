using System;
using System.Collections.Generic;
using System.IO;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Sim.Content
{
    /// <summary>The codex's pages and tooltips read straight out of the folders the mod ships, the way <c>GameCodex</c> reads them.</summary>
    /// <remarks>
    /// Under <c>&lt;culture&gt;-&lt;subculture&gt;</c>, then <c>&lt;culture&gt;</c>, then <c>en</c>, the first that has the file. The files
    /// are the ones the mod ships, copied beside the harness.
    /// </remarks>
    public sealed class CodexFiles : ITextSource
    {
        readonly List<string> _folders = new List<string>();

        /// <summary>The codex in <paramref name="culture" />, such as <c>no</c> or <c>pt-BR</c>, read from <paramref name="folder" />.</summary>
        public CodexFiles(string folder, string culture)
        {
            culture = culture ?? "";
            if (culture.Length > 0)
            {
                _folders.Add(Path.Combine(folder, culture));
                var dash = culture.IndexOf('-');
                if (dash > 0)
                    _folders.Add(Path.Combine(folder, culture.Substring(0, dash)));
            }

            _folders.Add(Path.Combine(folder, "en"));
        }

        /// <summary>The folder the mod's codex is copied to beside the harness.</summary>
        public static string Beside => Path.Combine(AppContext.BaseDirectory, "Content", "Codex");

        /// <inheritdoc />
        public string? Find(string key)
        {
            foreach (var folder in _folders)
            {
                var path = Path.Combine(folder, key);
                if (File.Exists(path))
                    return File.ReadAllText(path);
            }

            return null;
        }
    }
}
