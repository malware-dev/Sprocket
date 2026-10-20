using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Localization;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The color picker takes a code written with or without its hash, and always shows it as the script writes one.</summary>
public class ColorPickerTests
{
    static ColorPicker Field() => new ColorPicker(new ColorOffer(new List<Hsv>(), null, null), new Relay(() => { }), new Texts());

    /// <summary>A code written without the hash is taken, and given back with it.</summary>
    [Theory]
    [InlineData("FF8800", "#FF8800")]
    [InlineData("ff8800", "#ff8800")]
    [InlineData("FF880080", "#FF880080")]
    [InlineData("  FF8800 ", "#FF8800")]
    [InlineData("#FF8800", "#FF8800")]
    public void ACodeWithOrWithoutItsHashIsTaken(string written, string given)
    {
        var field = Field();
        field.Text = written;

        Assert.Equal(given, field.Text);
    }

    /// <summary>What does not read as a color is given back as it was written, hash or not.</summary>
    [Theory]
    [InlineData("FF88")]
    [InlineData("orange")]
    [InlineData("")]
    public void WhatIsNoColorIsLeftAsWritten(string written)
    {
        var field = Field();
        field.Text = written;

        Assert.Equal(written, field.Text);
    }
}
