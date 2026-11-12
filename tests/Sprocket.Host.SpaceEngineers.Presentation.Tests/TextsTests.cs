using System.IO;
using System.Xml.Linq;
using Sprocket.Sim.Content;
using Xunit;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The mod's own texts, as written and as the translations it ships say them.</summary>
public class TextsTests
{
    /// <summary>Each text is its English until a language is loaded.</summary>
    [Fact]
    public void EveryTextIsItsEnglish()
    {
        var texts = new Texts();

        Assert.Equal("Automation for {name}", texts.SystemsTitle);
    }

    /// <summary>Each translation shipped beside the texts and the codex translates only texts there are.</summary>
    [Theory]
    [InlineData("MyTexts", "no")]
    [InlineData("MyTextsCodex", "no")]
    public void EachTranslationTranslatesTextsThereAre(string stem, string culture)
    {
        var written = Keys(stem + ".resx");
        foreach (var key in Keys(stem + "." + culture + ".resx"))
            Assert.Contains(key, written);
    }

    /// <summary>The Norwegian example is said in Norwegian.</summary>
    [Fact]
    public void NorwegianIsSaid()
    {
        var texts = new Texts();
        texts.Load(new ResxTexts(ResxTexts.Beside, "no"));

        Assert.Equal("Automasjon for {name}", texts.SystemsTitle);
        Assert.Contains("på et annet sett med blokker.", texts.Codex.SystemsAddAnotherAbout);
    }

    static HashSet<string> Keys(string file)
    {
        var root = XDocument.Load(Path.Combine(ResxTexts.Beside, file)).Root!;
        return new HashSet<string>(root.Elements("data").Select(data => (string)data.Attribute("name")!));
    }
}
