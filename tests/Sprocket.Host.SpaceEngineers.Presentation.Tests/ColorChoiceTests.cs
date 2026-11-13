using Lumel.Controls;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Memory;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A color field holds a color as the script writes one, takes a color from its panel only on Done, and keeps the colors
/// chosen in it, and the tab used, with the player's settings in the world.
/// </summary>
public class ColorChoiceTests
{
    static readonly Hsv[] Paint = { Hsv.Of(new Color(255, 0, 0)), Hsv.Of(new Color(0, 255, 0)) };

    /// <summary>A color field offering <paramref name="offer" />, on a screen its panel can hang open over.</summary>
    static ColorField Field(ColorOffer offer)
    {
        var field = new ColorField(offer, new Texts());
        new WindowLayer().Covers(field);
        return field;
    }

    /// <summary>A color written as the script writes one is the field's color, its alpha included, and is given back as written.</summary>
    [Theory]
    [InlineData("#FF8800", 255)]
    [InlineData("#ff8800", 255)]
    [InlineData("#FF880080", 128)]
    public void AWrittenColorIsTheFieldsColor(string written, int alpha)
    {
        var field = Field(new ColorOffer(new Hsv[0], null, null));
        field.Text = written;

        Assert.Equal(written, field.Text);
        Assert.Equal(new Color(255, 136, 0, alpha), field.Color);
    }

    /// <summary>What does not read as a color is kept as written, with no color shown.</summary>
    [Theory]
    [InlineData("FF88")]
    [InlineData("orange")]
    [InlineData("")]
    public void WhatIsNoColorIsKeptAsWritten(string written)
    {
        var field = Field(new ColorOffer(new Hsv[0], null, null));
        field.Text = written;

        Assert.Equal(written, field.Text);
        Assert.Null(field.Color);
    }

    /// <summary>A color made in the panel reaches the field only on Done, written as the script writes one.</summary>
    [Fact]
    public void TheFieldTakesTheColorOnDone()
    {
        var field = Field(new ColorOffer(Paint, new PlayerMemory(), null));
        field.Text = "#123456";
        var edited = 0;
        field.Edited += () => edited++;

        field.Command!.Execute();
        field.Panel.Color = new Color(0, 255, 0);
        Assert.Equal("#123456", field.Text);
        Assert.Equal(0, edited);

        field.Panel.Submit!.Execute();

        Assert.Equal("#00FF00", field.Text);
        Assert.Equal(1, edited);
        Assert.False(field.IsOpen);
    }

    /// <summary>The panel put away without Done gives the field nothing and keeps nothing.</summary>
    [Fact]
    public void PuttingThePanelAwayGivesNothing()
    {
        var player = new PlayerMemory();
        var field = Field(new ColorOffer(Paint, player, null));
        field.Text = "#123456";

        field.Command!.Execute();
        field.Panel.Color = new Color(255, 0, 0);
        field.Command!.Execute();

        Assert.Equal("#123456", field.Text);
        Assert.Empty(player.RecentColors);
    }

    /// <summary>A color taken with Done goes first among the colors kept with the player, once, and a field made afterwards offers them newest first.</summary>
    [Fact]
    public void AColorChosenIsKeptWithThePlayer()
    {
        var player = new PlayerMemory();
        var told = 0;
        var field = Field(new ColorOffer(Paint, player, () => told++));
        Choose(field, new Color(255, 0, 0));
        Choose(field, new Color(0, 255, 0));
        Choose(field, new Color(255, 0, 0));

        Assert.Equal(new[] { "#FF0000", "#00FF00" }, player.RecentColors.ToArray());
        Assert.Equal(3, told);

        var other = Field(new ColorOffer(Paint, player, null));
        Assert.Equal(new[] { new Color(255, 0, 0), new Color(0, 255, 0) }, other.Recent.Colors.ToArray());
    }

    /// <summary>Colors kept with the player that do not read, or are there twice, are left out of what is offered.</summary>
    [Fact]
    public void KeptColorsAreReadOnce()
    {
        var player = new PlayerMemory();
        player.RecentColors.AddRange(new[] { "#20C0FF", "orange", "#20c0ff", "#20C0FF80" });

        var field = Field(new ColorOffer(Paint, player, null));

        Assert.Equal(new[] { new Color(32, 192, 255), new Color(32, 192, 255, 128) }, field.Recent.Colors.ToArray());
    }

    /// <summary>The panel opens on the tab last used, and a tab chosen is kept with the player's settings in the world, and said.</summary>
    [Fact]
    public void TheTabUsedIsKept()
    {
        var player = new PlayerMemory();
        var told = 0;
        var field = Field(new ColorOffer(new Hsv[0], player, () => told++));
        Assert.Equal(ColorEntry.Hsv, field.Entry);

        var modes = field.Panel.Within().OfType<TabView>().Single();
        modes.Tabs[1].Command!.Execute();

        Assert.Equal(1, player.ColorTab);
        Assert.Equal(1, told);
        Assert.Equal(ColorEntry.Rgb, Field(new ColorOffer(new Hsv[0], player, null)).Entry);
    }

    /// <summary>Opens the field's panel, makes <paramref name="color" /> in it, and gives it with Done.</summary>
    static void Choose(ColorField field, Color color)
    {
        field.Command!.Execute();
        field.Panel.Color = color;
        field.Panel.Submit!.Execute();
    }
}
