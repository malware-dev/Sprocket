using Sprocket.Host.SpaceEngineers.Framework.Text;
using System.IO;
using VRage;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A mod's texts loaded into the game's own table of texts, in the game's own assemblies, and read back out.</summary>
public class GameTextsTests
{
    static readonly string Mod = Path.Combine(AppContext.BaseDirectory, "Content", "Localization");

    /// <summary>A key says what the file of the language loaded says, English or Norwegian, and the codex is loaded beside the texts in each, so its pages read in the language spoken.</summary>
    [Fact]
    public void AKeySaysWhatTheLanguageLoadedSays()
    {
        var texts = new GameTexts(Mod);
        texts.Load(MyLanguagesEnum.English);

        Assert.Equal("logic system", texts.Find("Sprocket.Systems.New"));
        Assert.StartsWith("A ![](mark:logic-system)", texts.Find("Sprocket.Term.LogicSystem"));

        texts.Load(MyLanguagesEnum.Norwegian);

        Assert.Equal("logikksystem", texts.Find("Sprocket.Systems.New"));
        Assert.Equal(MyLanguagesEnum.Norwegian, texts.Language);
        Assert.StartsWith("Et ![](mark:logic-system)", texts.Find("Sprocket.Term.LogicSystem"));
    }

    /// <summary>A key the translation lacks keeps its English, and a key nobody wrote is not found at all.</summary>
    [Fact]
    public void AMissingTranslationKeepsTheEnglish()
    {
        var folder = Path.Combine(Path.GetTempPath(), "GameTextsTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            File.WriteAllText(Path.Combine(folder, "MyTexts.resx"),
                Resx(("Test.Greeting", "Hello"), ("Test.Farewell", "Goodbye")));
            File.WriteAllText(Path.Combine(folder, "MyTexts.no.resx"), Resx(("Test.Greeting", "Hei")));

            var texts = new GameTexts(folder);
            texts.Load(MyLanguagesEnum.Norwegian);

            Assert.Equal("Hei", texts.Find("Test.Greeting"));
            Assert.Equal("Goodbye", texts.Find("Test.Farewell"));
            Assert.Null(texts.Find("Test.Nobody.Wrote.This"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    /// <summary>A file of texts holding <paramref name="texts" />, in the shape the game reads.</summary>
    static string Resx(params (string Key, string Value)[] texts)
    {
        var data = string.Join("\n", texts.Select(text =>
            $"  <data name=\"{text.Key}\" xml:space=\"preserve\">\n    <value>{text.Value}</value>\n  </data>"));
        return "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root>\n" + data + "\n</root>\n";
    }
}
