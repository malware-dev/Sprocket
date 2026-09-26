using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A symbol: sized and tinted by its style, and lit by it.</summary>
public class GlyphTests
{
    const float Wide = 400f;
    const float High = 200f;

    static readonly Color Faint = new Color(133, 161, 169);
    static readonly Color Ink = new Color(220, 245, 253);
    static readonly Color Dim = new Color(95, 114, 120);
    static readonly Color PanelHot = new Color(60, 76, 82);

    static readonly GlyphStyle Tool = new GlyphStyle
    {
        Base = { FontSize = 32f, Tint = Faint },
        Hot = { Background = PanelHot, Tint = Ink },
        Disabled = { Tint = Dim }
    };

    /// <summary>Its style sizes and tints the symbol.</summary>
    [Fact]
    public void ItsStyleSizesAndTintsIt()
    {
        var glyph = new Glyph { Symbol = "d", Style = Tool };
        var easel = new Easel(new VStack { Children = { glyph } }, Wide, High);

        Assert.Equal(32f, glyph.DesiredSize.Y, 2);
        Assert.Equal(Faint, Assert.Single(easel.Written).Color);
    }

    /// <summary>Hot, it takes its hot fill and tint; left, it goes back.</summary>
    [Fact]
    public void HotItTakesItsHotFillAndTint()
    {
        var glyph = new Glyph { Symbol = "d", Style = Tool };

        glyph.IsHot = true;
        Assert.Equal(PanelHot, glyph.Background);
        Assert.Equal(Ink, glyph.Tint);

        glyph.IsHot = false;
        Assert.Equal(Color.Transparent, glyph.Background);
        Assert.Equal(Faint, glyph.Tint);
    }

    /// <summary>Disabled, it looks disabled whether or not it is hot.</summary>
    [Fact]
    public void DisabledItLooksDisabledEvenWhenHot()
    {
        var glyph = new Glyph { Symbol = "d", Style = Tool };

        glyph.IsHot = true;
        glyph.IsEnabled = false;

        Assert.Equal(Dim, glyph.Tint);
    }

    /// <summary>Going hot and back measures no text.</summary>
    [Fact]
    public void GoingHotMeasuresNoText()
    {
        var glyph = new Glyph { Symbol = "d", Style = Tool };
        var easel = new Easel(new VStack { Children = { glyph } }, Wide, High);
        var settled = easel.Measured;

        glyph.IsHot = true;
        easel.Draw(Wide, High);
        glyph.IsHot = false;
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }
}
