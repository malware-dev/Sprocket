using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Styling;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A switch: its words coloured by which half is in force, and its look from its style.</summary>
public class SwitchTests
{
    const float Wide = 400f;
    const float High = 200f;

    static readonly Color Ink = new Color(220, 245, 253);
    static readonly Color Faint = new Color(133, 161, 169);
    static readonly Color Dim = new Color(95, 114, 120);
    static readonly Color Muted = new Color(162, 195, 205);

    static readonly SwitchStyle Plain = new SwitchStyle
    {
        Base = { FontSize = 16f, Color = Ink, QuietColor = Faint, Edge = Dim },
        Hot = { Edge = Muted }
    }.Frozen();

    /// <summary>The word in force takes the style's colour and the other its quiet colour.</summary>
    [Fact]
    public void TheWordInForceTakesTheColour()
    {
        var control = new Switch { Style = Plain, IsSet = true, IsOn = true };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);

        Assert.Equal(Ink, Word(easel, "ON").Color);
        Assert.Equal(Faint, Word(easel, "OFF").Color);

        control.IsOn = false;
        easel.Draw(Wide, High);

        Assert.Equal(Faint, Word(easel, "ON").Color);
        Assert.Equal(Ink, Word(easel, "OFF").Color);
    }

    /// <summary>With neither half in force, both words are quiet.</summary>
    [Fact]
    public void UnsetBothWordsAreQuiet()
    {
        var control = new Switch { Style = Plain, IsOn = true };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);

        Assert.Equal(Faint, Word(easel, "ON").Color);
        Assert.Equal(Faint, Word(easel, "OFF").Color);
    }

    /// <summary>Hot, it takes its hot edge; left, it goes back.</summary>
    [Fact]
    public void HotItTakesItsHotEdge()
    {
        var control = new Switch { Style = Plain, IsSet = true };

        control.IsHot = true;
        Assert.Equal(Muted, control.Edge);

        control.IsHot = false;
        Assert.Equal(Dim, control.Edge);
    }

    /// <summary>Its words are sized by its style.</summary>
    [Fact]
    public void ItsWordsAreSizedByItsStyle()
    {
        var control = new Switch { Style = Plain, IsSet = true };
        _ = new Easel(new VStack { Children = { control } }, Wide, High);

        Assert.Equal(16f, control.DesiredSize.Y, 3);
    }

    /// <summary>Switching measures no text, since only colours change.</summary>
    [Fact]
    public void SwitchingMeasuresNoText()
    {
        var control = new Switch { Style = Plain, IsSet = true };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);
        var settled = easel.Measured;

        control.IsOn = true;
        easel.Draw(Wide, High);
        control.IsOn = false;
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    static Sim.Simulation.DrawCommand Word(Easel easel, string text)
    {
        return Assert.Single(easel.Written, command => command.Text == text);
    }
}
