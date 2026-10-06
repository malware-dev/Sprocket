using Lumel.Layout;
using Lumel.Text;
using Sprocket.Sim.Simulation;
using VRage.Game;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>How each kind of run is set apart by its shape: strong and emphasized words by their faces, a link by the line under it, code by the box behind it.</summary>
public class RunLookTests
{
    const float Wide = 400f;
    const float ScreenWide = 1920f;
    const float ScreenHigh = 1080f;

    static readonly Color Body = new Color(162, 195, 205);
    static readonly Color Blue = new Color(79, 195, 247);

    /// <summary>Strong, emphasized and both are each set in their own face, and code stays in the code face whatever else it is.</summary>
    [Fact]
    public void EachKindOfWordIsSetInItsOwnFace()
    {
        var easel = Laid("plain **bold** *slanted* ***both*** **`code`**", text =>
        {
            text.StrongFont = "Bold";
            text.EmphasisFont = "Italic";
            text.StrongEmphasisFont = "BoldItalic";
            text.CodeFont = "Mono";
        });

        Assert.Equal(MyFontEnum.White, Word(easel, "plain").Font);
        Assert.Equal("Bold", Word(easel, "bold").Font);
        Assert.Equal("Italic", Word(easel, "slanted").Font);
        Assert.Equal("BoldItalic", Word(easel, "both").Font);
        Assert.Equal("Mono", Word(easel, "code").Font);
    }

    /// <summary>A face that is not set falls back to the nearest one that is: both strong and emphasized to the strong face, and anything to the text's own.</summary>
    [Fact]
    public void AFaceNotSetFallsBackToTheNearest()
    {
        var easel = Laid("plain **bold** *slanted* ***both***", text => text.StrongFont = "Bold");

        Assert.Equal("Bold", Word(easel, "both").Font);
        Assert.Equal(MyFontEnum.White, Word(easel, "slanted").Font);
    }

    /// <summary>A link is underlined in its own color, under its own words: the line runs across the space inside it and stops at its last word.</summary>
    [Fact]
    public void ALinkIsUnderlinedAcrossItsWordsOnly()
    {
        var easel = Laid("Pick a [logic system](logic-system) now", text => text.LinkColor = Blue);

        var link = Word(easel, "logic system");
        var line = Assert.Single(easel.Sprites, sprite => sprite.Color == Blue);

        Assert.Equal("logic system", link.Text);
        Assert.Equal(link.Position.X, line.Destination.X, 1f);
        Assert.Equal(Word(easel, "now").Position.X, line.Destination.Right, 1f);
        Assert.True(line.Destination.Y > link.Position.Y, "the line is not under the words");
        Assert.True(line.Destination.Height <= easel.Pixels * 2f, "the line is thicker than a hairline");
    }

    /// <summary>Code stands on a box reaching a little past its words, and the word after it starts where the box ends.</summary>
    [Fact]
    public void CodeStandsOnABoxClearOfItsNeighbours()
    {
        var easel = Laid("Press `Open door` now", _ => { });

        var code = Word(easel, "Open door");
        var box = Assert.Single(easel.Sprites, sprite => sprite.Color.A == RichText.CodeWash.A && sprite.Color.R == RichText.CodeWash.R);

        Assert.Equal(RichText.CodeRoom * easel.Pixels, code.Position.X - box.Destination.X, 1f);
        Assert.Equal(Word(easel, "now").Position.X, box.Destination.Right, 1f);
    }

    static Easel Laid(string markdown, Action<RichText> styled)
    {
        var text = new RichText
        {
            Document = Markdown.Parse(markdown),
            FontSize = 20f,
            Color = Body,
            StrongColor = Body,
            EmphasisColor = Body,
            Placement = Fixed.Wide(Wide)
        };
        styled(text);

        return new Easel(new VStack { Children = { text } }, ScreenWide, ScreenHigh);
    }

    static DrawCommand Word(Easel easel, string text) =>
        Assert.Single(easel.Written, command => command.Text.Trim() == text);
}
