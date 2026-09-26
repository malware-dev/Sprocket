using VRage;
using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Localization
{
    /// <summary>A mod's texts, loaded into the game's own table of texts and read back out of it.</summary>
    /// <remarks>
    ///     <c>MyTexts.LoadTexts</c> reads every <c>MyTexts*.resx</c> under the folder with no culture in its
    ///     name, then the ones named for the language's culture over them, then the ones named for its
    ///     culture and subculture, each replacing what it repeats. A key a translation lacks therefore keeps
    ///     its English, and the game draws a key from the table wherever it is given one, a terminal control's
    ///     title among them. Whoever changes the language has to load again: the game does not reload a
    ///     mod's texts itself.
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
