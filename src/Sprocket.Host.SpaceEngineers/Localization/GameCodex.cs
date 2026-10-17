using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;

namespace Sprocket.Host.SpaceEngineers.Localization
{
    /// <summary>The codex's pages and tooltips, read out of a mod's <c>Data\Codex</c> in the language loaded.</summary>
    /// <remarks>Each page is read from the mod's folder on every call; a page a translation lacks is read in English.</remarks>
    public sealed class GameCodex : ITextSource
    {
        const string Folder = "Data/Codex/";
        const string Original = "en";

        readonly MyObjectBuilder_Checkpoint.ModItem _mod;
        readonly List<string> _folders = new List<string>();

        /// <summary>The codex of the mod <paramref name="mod" />, in English until <see cref="Load" /> is given a language.</summary>
        public GameCodex(MyObjectBuilder_Checkpoint.ModItem mod)
        {
            _mod = mod;
            _folders.Add($"{Folder}{Original}/");
            _folders.Add(Folder);
        }

        /// <summary>Reads in <paramref name="language" /> from here on.</summary>
        public void Load(MyLanguagesEnum language)
        {
            _folders.Clear();

            MyTexts.MyLanguageDescription description;
            if (MyTexts.Languages.TryGetValue(language, out description) && description != null
                && !string.IsNullOrEmpty(description.CultureName) && description.CultureName != Original)
            {
                if (!string.IsNullOrEmpty(description.SubcultureName))
                    _folders.Add($"{Folder}{description.CultureName}-{description.SubcultureName}/");

                _folders.Add($"{Folder}{description.CultureName}/");
            }

            _folders.Add($"{Folder}{Original}/");
            _folders.Add(Folder);
        }

        /// <inheritdoc />
        public string Find(string key)
        {
            string text = null;
            for (var i = 0; i < _folders.Count && text == null; i++)
            {
                var path = _folders[i] + key;
                if (!MyAPIGateway.Utilities.FileExistsInModLocation(path, _mod))
                    continue;

                using (var reader = MyAPIGateway.Utilities.ReadFileInModLocation(path, _mod))
                    text = reader.ReadToEnd();
            }

            return text;
        }
    }
}
