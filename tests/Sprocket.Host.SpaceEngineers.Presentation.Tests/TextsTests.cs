using System.IO;
using System.Xml.Linq;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Sim.Content;
using Xunit;

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

    /// <summary>Each translation shipped beside the texts translates only texts there are.</summary>
    [Theory]
    [InlineData("no")]
    public void EachTranslationTranslatesTextsThereAre(string culture)
    {
        var written = Keys("MyTexts.resx");
        foreach (var key in Keys("MyTexts." + culture + ".resx"))
            Assert.Contains(key, written);
    }

    /// <summary>The Norwegian example is said in Norwegian.</summary>
    [Fact]
    public void NorwegianIsSaid()
    {
        var texts = new Texts();
        texts.Book.Load(new ResxTexts(ResxTexts.Beside, "no"));

        Assert.Equal("Automasjon for {name}", texts.SystemsTitle);
        Assert.Equal("Laget fra **{name}**-plantegningen", texts.SystemsMadeFrom);
    }

    static HashSet<string> Keys(string file)
    {
        var root = XDocument.Load(Path.Combine(ResxTexts.Beside, file)).Root!;
        return new HashSet<string>(root.Elements("data").Select(data => (string)data.Attribute("name")!));
    }
}
