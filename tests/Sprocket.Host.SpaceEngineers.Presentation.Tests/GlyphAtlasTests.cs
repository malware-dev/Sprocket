using System.IO;
using System.Text.RegularExpressions;
using Sprocket.Sim.Content;
using Xunit;
using Lumel.Sim;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A font page drawn in a color, as the game draws one: the page's own color multiplied by the text's.</summary>
public class GlyphAtlasTests
{
    /// <summary>
    /// In white text a white glyph stays white, and the chip behind the logic system's mark comes out at the darker shade
    /// the page gives it.
    /// </summary>
    [Fact]
    public void APagesOwnColorDarkensTheText()
    {
        // The PNG the DDS is encoded from: the DDS decoder does not start in the test runner.
        var atlas = GlyphAtlas.FromPng(Path.Combine(SprocketContent.FontFolder("MaltekSymbols"), "MaltekSymbols-0.png"));
        Assert.NotNull(atlas);

        var page = atlas.Tinted(255, 255, 255);
        var width = page.PixelWidth;
        var pixels = new byte[width * page.PixelHeight * 4];
        page.CopyPixels(pixels, width * 4, 0);

        Assert.All(Shades(pixels, width, Origin("q")), shade => Assert.Equal(255, shade));
        var logicSystem = Shades(pixels, width, Origin("y"));
        Assert.Contains(255, logicSystem);
        Assert.Contains(logicSystem, shade => shade >= 190 && shade <= 205);
    }

    /// <summary>The screen's mark has a glyph of its own on the page, with ink in it.</summary>
    [Fact]
    public void TheScreensMarkIsDrawn()
    {
        var atlas = GlyphAtlas.FromPng(Path.Combine(SprocketContent.FontFolder("MaltekSymbols"), "MaltekSymbols-0.png"));
        Assert.NotNull(atlas);

        var page = atlas.Tinted(255, 255, 255);
        var width = page.PixelWidth;
        var pixels = new byte[width * page.PixelHeight * 4];
        page.CopyPixels(pixels, width * 4, 0);

        Assert.NotEmpty(Shades(pixels, width, Origin(Framework.Ui.Look.Symbols.Screen)));
    }

    /// <summary>Where the glyph <paramref name="ch" /> sits on the page, from the font's own table.</summary>
    static (int X, int Y) Origin(string ch)
    {
        var table = File.ReadAllText(Path.Combine(SprocketContent.FontFolder("MaltekSymbols"), "MaltekSymbols.xml"));
        var match = Regex.Match(table, "<glyph ch=\"" + Regex.Escape(ch) + "\" code=\"[0-9A-F]+\" bm=\"0\" origin=\"(\\d+),(\\d+)\"");
        Assert.True(match.Success, "no glyph " + ch);
        return (int.Parse(match.Groups[1].Value), int.Parse(match.Groups[2].Value));
    }

    /// <summary>The red of every pixel of the cell at <paramref name="origin" /> that is fully covered.</summary>
    static List<int> Shades(byte[] pixels, int width, (int X, int Y) origin)
    {
        var shades = new List<int>();
        for (var y = origin.Y; y < origin.Y + 64; y++)
        for (var x = origin.X; x < origin.X + 64; x++)
        {
            var at = (y * width + x) * 4;
            if (pixels[at + 3] == 255)
                shades.Add(pixels[at + 2]);
        }

        return shades;
    }
}
