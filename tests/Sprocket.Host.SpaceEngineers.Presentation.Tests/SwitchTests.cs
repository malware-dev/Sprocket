using Lumel;
using Lumel.Controls;
using Lumel.Layout;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A switch: the word for its state beside a thumb that moves, sized for the longer word, and its look from its style.</summary>
public class SwitchTests
{
    const float Wide = 400f;
    const float High = 200f;

    static readonly Color Ink = new Color(220, 245, 253);
    static readonly Color Body = new Color(91, 115, 123);
    static readonly Color Well = new Color(32, 39, 45);
    static readonly Color Dim = new Color(95, 114, 120);
    static readonly Color Muted = new Color(162, 195, 205);

    static readonly SwitchStyle Plain = new SwitchStyle
    {
        Base =
        {
            FontSize = 16f,
            Color = Ink,
            Background = Body,
            OffTrack = Well,
            OnThumb = Ink,
            OffThumb = Ink,
            WordPadding = new Thickness(8f, 4f),
            Padding = new Thickness(4f),
            Edge = Dim
        },
        Hot = { Edge = Muted }
    };

    /// <summary>Only the word for the state it is in is written.</summary>
    [Fact]
    public void ItSaysOnlyTheStateItIsIn()
    {
        var control = new Switch { Style = Plain, IsOn = true };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);

        Assert.Single(easel.Written, command => command.Text == "ON");
        Assert.DoesNotContain(easel.Written, command => command.Text == "OFF");

        control.IsOn = false;
        easel.Draw(Wide, High);

        Assert.Single(easel.Written, command => command.Text == "OFF");
        Assert.DoesNotContain(easel.Written, command => command.Text == "ON");
    }

    /// <summary>The thumb is at the right while on and at the left while off.</summary>
    [Fact]
    public void TheThumbIsAtTheRightWhenOn()
    {
        var control = new Switch { Style = Plain, IsOn = true };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);
        var onAt = Thumb(easel).X;

        control.IsOn = false;
        easel.Draw(Wide, High);
        var offAt = Thumb(easel).X;

        Assert.True(onAt > offAt);
    }

    /// <summary>The thumb takes the color its style gives the state the switch is in.</summary>
    [Fact]
    public void TheThumbTakesTheColorOfItsState()
    {
        var amber = new Color(255, 170, 40);
        var style = new SwitchStyle(Plain) { Base = { OffThumb = amber } };
        var control = new Switch { Style = style, IsOn = false };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);
        Assert.Single(easel.Sprites, sprite => sprite.Color == amber);

        control.IsOn = true;
        easel.Draw(Wide, High);
        Assert.DoesNotContain(easel.Sprites, sprite => sprite.Color == amber);
        Assert.Equal(Thumb(easel).Width, Thumb(easel).Height);
    }

    /// <summary>The room inside is filled with the off track only while off.</summary>
    [Fact]
    public void TheTrackIsFilledOnlyWhileOff()
    {
        var control = new Switch { Style = Plain, IsOn = true };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);

        Assert.DoesNotContain(easel.Sprites, sprite => sprite.Color == Well);

        control.IsOn = false;
        easel.Draw(Wide, High);

        Assert.Contains(easel.Sprites, sprite => sprite.Color == Well);
    }

    /// <summary>It is as wide as the longer word needs whichever it is showing, so it keeps its size when switched.</summary>
    [Fact]
    public void ItIsSizedForTheLongerWord()
    {
        var control = new Switch { Style = Plain, IsOn = true };
        control.Says("On", "Isključeno");
        _ = new Easel(new VStack { Children = { control } }, Wide, High);
        var on = control.DesiredSize;

        var other = new Switch { Style = Plain, IsOn = false };
        other.Says("On", "Isključeno");
        _ = new Easel(new VStack { Children = { other } }, Wide, High);

        var shorter = new Switch { Style = Plain, IsOn = true };
        shorter.Says("On", "Off");
        _ = new Easel(new VStack { Children = { shorter } }, Wide, High);

        Assert.Equal(on, other.DesiredSize);
        Assert.True(on.X > shorter.DesiredSize.X);
    }

    /// <summary>Hot, it takes its hot edge; left, it goes back.</summary>
    [Fact]
    public void HotItTakesItsHotEdge()
    {
        var control = new Switch { Style = Plain };

        control.IsHot = true;
        Assert.Equal(Muted, control.Edge);

        control.IsHot = false;
        Assert.Equal(Dim, control.Edge);
    }

    /// <summary>Its height is its words' size, the room round them and the room round the inside, from its style.</summary>
    [Fact]
    public void ItIsSizedByItsStyle()
    {
        var control = new Switch { Style = Plain };
        _ = new Easel(new VStack { Children = { control } }, Wide, High);

        Assert.Equal(16f + 4f * 2f + 4f * 2f, control.DesiredSize.Y, 3);
    }

    /// <summary>Switching measures no text, since only what is drawn changes.</summary>
    [Fact]
    public void SwitchingMeasuresNoText()
    {
        var control = new Switch { Style = Plain };
        var easel = new Easel(new VStack { Children = { control } }, Wide, High);
        var settled = easel.Measured;

        control.IsOn = true;
        easel.Draw(Wide, High);
        control.IsOn = false;
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>The thumb: the one fill in the thumb's color.</summary>
    static Rectangle Thumb(Easel easel)
    {
        return Assert.Single(easel.Sprites, sprite => sprite.Color == Ink).Destination;
    }
}
