using System.Text.RegularExpressions;
using Xunit;

namespace Sprocket.Wiki.Tests;

public class CatalogTests
{
    /// <summary>Every mark is the glyph the game draws for it, read out of the mod's own source, and the wiki has every mark the game has.</summary>
    [Fact]
    public void EveryMarkIsTheGlyphTheGameDraws()
    {
        var look = Path.Combine(Given.Repository, "src", "Sprocket.Host.SpaceEngineers", "Features", "Look");
        var symbols = Regex.Matches(File.ReadAllText(Path.Combine(look, "Symbols.cs")), "public const string (\\w+) = \"(.)\";")
            .ToDictionary(match => match.Groups[1].Value, match => match.Groups[2].Value[0]);
        var game = Regex.Matches(File.ReadAllText(Path.Combine(look, "MarkImages.cs")), "Add\\(\"([a-z-]+)\", Symbols\\.(\\w+)\\);")
            .ToDictionary(match => match.Groups[1].Value, match => symbols[match.Groups[2].Value]);

        Assert.NotEmpty(game);
        Assert.Equal(game.OrderBy(mark => mark.Key), Given.Catalog().Marks.OrderBy(mark => mark.Key));
    }
}
