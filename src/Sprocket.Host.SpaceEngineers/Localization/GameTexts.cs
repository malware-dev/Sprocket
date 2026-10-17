using VRage;
using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Localization
{
    /// <summary>A mod's texts, loaded into the game's own table of texts and read back out of it.</summary>
    /// <remarks>
    /// <c>MyTexts.LoadTexts</c> lays the culture's <c>MyTexts*.resx</c> over the ones with no culture, so a key a translation
    /// lacks keeps its English. The game does not reload a mod's texts itself, so a change of language needs another load.
    /// </remarks>
    public sealed class GameTexts : ITextSource
    {
        readonly string _folder;

        /// <summary>The texts in <paramref name="folder" />, a mod's own <c>Data\Localization</c>, until <see cref="Load" /> reads them.</summary>
        public GameTexts(string folder)
        {
            _folder = folder;
        }

        /// <summary>The language last loaded.</summary>
        public MyLanguagesEnum Language { get; private set; }

        /// <summary>Loads the texts for <paramref name="language" /> into the game's table.</summary>
        public void Load(MyLanguagesEnum language)
        {
            MyTexts.MyLanguageDescription description;
            if (!MyTexts.Languages.TryGetValue(language, out description) || description == null)
                MyTexts.LoadTexts(_folder);
            else
                MyTexts.LoadTexts(_folder, description.CultureName, description.SubcultureName);

            Language = language;
        }

        /// <inheritdoc />
        public string Find(string key)
        {
            var id = MyStringId.GetOrCompute(key);
            return MyTexts.Exists(id) ? MyTexts.GetString(id) : null;
        }
    }
}
