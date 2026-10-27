using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Memory;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The color picker gives a color on a square of saturation and value beside a strip of hues, or typed by hue, saturation and
/// value, by red, green and blue, or as a code; the field takes it on Done, and the picker offers the colors chosen in it
/// before.
/// </summary>
public class ColorChoiceTests
{
    static readonly Hsv[] Paint = { Hsv.Of(new Color(255, 0, 0)), Hsv.Of(new Color(0, 255, 0)) };

    /// <summary>A color field offering <paramref name="offer" />, on a screen its picker can hang open over.</summary>
    static ColorField Field(ColorOffer offer)
    {
        var field = new ColorField(offer, new Texts());
        new WindowLayer().Covers(field);
        return field;
    }

    /// <summary>A picker laid out alone, with the pointer to press it with.</summary>
    static (ColorPicker Picker, InputRoot Input) Laid(ColorOffer offer)
    {
        var picker = Field(offer).Picker;
        var root = new VStack { Children = { picker } };
        _ = new Easel(root, 600f, 400f);
        return (picker, new InputRoot(root));
    }

    /// <summary>Pressing the square gives the hue already chosen at the saturation across and the value up where it is pressed.</summary>
    [Fact]
    public void TheSquareGivesSaturationAndValue()
    {
        var (picker, input) = Laid(new ColorOffer(new Hsv[0], null, null));
        picker.Text = "#FF0000";
        var plane = picker.Within().OfType<ColorPlane>().Single();

        Press(input, plane, new Vector2(plane.ContentBounds.Right, plane.ContentBounds.Y));
        Assert.Equal("#FF0000", picker.Text);

        Press(input, plane, new Vector2(plane.ContentBounds.X, plane.ContentBounds.Y));
        Assert.Equal("#FFFFFF", picker.Text);

        Press(input, plane, new Vector2(plane.ContentBounds.Right, plane.ContentBounds.Bottom));
        Assert.Equal("#000000", picker.Text);
    }

    /// <summary>Pressing the strip gives the hue there at the saturation and value already chosen, and moves the square to that hue.</summary>
    [Fact]
    public void TheStripGivesAHue()
    {
        var (picker, input) = Laid(new ColorOffer(new Hsv[0], null, null));
        picker.Text = "#FF0000";
        var strip = picker.Within().OfType<HueStrip>().Single();

        Press(input, strip, new Vector2(strip.ContentBounds.Center.X, strip.ContentBounds.Center.Y));

        Assert.Equal("#00FFFF", picker.Text);
        Assert.Equal(0.5f, picker.Within().OfType<ColorPlane>().Single().Hue, 2);
    }

    /// <summary>A channel typed in its box gives the color and stays as typed while the other boxes follow.</summary>
    [Fact]
    public void AChannelTypedInItsBoxGivesTheColor()
    {
        var (picker, input) = Laid(new ColorOffer(new Hsv[0], null, null) { Tab = 1 });
        picker.Text = "#000000";

        var boxes = picker.Within().OfType<Lumel.Text.NumberBox>().ToArray();
        Assert.Equal(new[] { "0", "0", "0" }, boxes.Select(box => box.Text).ToArray());

        input.Focus(boxes[0]);
        foreach (var typed in "255")
            ((IInputTarget)boxes[0]).Text(typed);

        Assert.Equal("#FF0000", picker.Text);
        Assert.Equal("255", boxes[0].Text);
    }

    /// <summary>The picker opens on the tab last used, and a tab chosen is kept with the player's settings in the world, and said.</summary>
    [Fact]
    public void TheTabUsedIsKept()
    {
        var player = new PlayerMemory();
        var told = 0;
        var picker = Field(new ColorOffer(new Hsv[0], player, () => told++)).Picker;
        var modes = picker.Within().OfType<TabView>().Single();
        Assert.Equal(0, modes.Current);

        modes.Tabs[1].Command!.Execute();

        Assert.Equal(1, player.ColorTab);
        Assert.Equal(1, told);
        var again = Field(new ColorOffer(new Hsv[0], player, null)).Picker;
        Assert.Equal(1, again.Within().OfType<TabView>().Single().Current);
    }

    /// <summary>A color given in the picker reaches the field only on Done, and Done puts the picker away.</summary>
    [Fact]
    public void TheFieldTakesTheColorOnDone()
    {
        var field = Field(new ColorOffer(Paint, new PlayerMemory(), null));
        field.Text = "#123456";
        var edited = 0;
        field.Edited += () => edited++;

        field.Command!.Execute();
        PaintSwatches(field.Picker)[1].Command!.Execute();
        Assert.Equal("#123456", field.Text);
        Assert.Equal(0, edited);

        Done(field).Command!.Execute();

        Assert.Equal("#00FF00", field.Text);
        Assert.Equal(1, edited);
        Assert.False(field.IsOpen);
    }

    /// <summary>Enter in the picker is Done.</summary>
    [Fact]
    public void EnterIsDone()
    {
        var field = Field(new ColorOffer(Paint, new PlayerMemory(), null));
        field.Command!.Execute();
        PaintSwatches(field.Picker)[0].Command!.Execute();

        ((Lumel.Text.TextBox)field.Picker.Keyboard).Submit!.Execute();

        Assert.Equal("#FF0000", field.Text);
        Assert.False(field.IsOpen);
    }

    /// <summary>The picker put away without Done gives the field nothing and chooses nothing, and opens again on the field's color.</summary>
    [Fact]
    public void PuttingThePickerAwayGivesNothing()
    {
        var player = new PlayerMemory();
        var field = Field(new ColorOffer(Paint, player, null));
        field.Text = "#123456";

        field.Command!.Execute();
        PaintSwatches(field.Picker)[0].Command!.Execute();
        field.Command!.Execute();

        Assert.Equal("#123456", field.Text);
        Assert.Empty(player.RecentColors);

        field.Command!.Execute();
        Assert.Equal("#123456", field.Picker.Text);
    }

    /// <summary>A color taken with Done goes first among the colors chosen before, once, and a picker opened afterwards shows them newest first, an edge alone that does nothing where none is yet.</summary>
    [Fact]
    public void AColorChosenComesFirstOnce()
    {
        var player = new PlayerMemory();
        var field = Field(new ColorOffer(Paint, player, null));
        Choose(field, 0);
        Choose(field, 1);
        Choose(field, 0);

        Assert.Equal(new[] { "#FF0000", "#00FF00" }, player.RecentColors.ToArray());

        var other = Field(new ColorOffer(Paint, player, null));
        other.Command!.Execute();
        var recent = Recent(other.Picker);
        Assert.Equal(ColorPicker.RecentCount, recent.Length);
        Assert.Equal(new Color(255, 0, 0), recent[0].Color);
        Assert.Equal(new Color(0, 255, 0), recent[1].Color);
        Assert.True(recent[1].IsEnabled);
        Assert.False(recent[2].IsEnabled);
    }

    /// <summary>Done on the color the field already holds chooses nothing.</summary>
    [Fact]
    public void DoneUnchangedChoosesNothing()
    {
        var player = new PlayerMemory();
        var field = Field(new ColorOffer(Paint, player, null));
        field.Text = "#123456";

        field.Command!.Execute();
        Done(field).Command!.Execute();

        Assert.Empty(player.RecentColors);
    }

    /// <summary>Pressing a color chosen before gives it.</summary>
    [Fact]
    public void PressingAColorChosenBeforeGivesIt()
    {
        var player = new PlayerMemory();
        player.RecentColors.Add("#20C0FF");
        var field = Field(new ColorOffer(Paint, player, null));
        field.Command!.Execute();

        Recent(field.Picker)[0].Command!.Execute();
        Done(field).Command!.Execute();

        Assert.Equal("#20C0FF", field.Text);
    }

    /// <summary>Presses <paramref name="element" /> at <paramref name="point" /> and lets go there.</summary>
    static void Press(InputRoot input, Element element, Vector2 point)
    {
        input.PointerDown(element, point, PointerButton.Left, 1, false, false);
        input.PointerUp(point, PointerButton.Left);
    }

    /// <summary>Opens the field's picker, presses the paint color at <paramref name="paint" />, and presses Done.</summary>
    static void Choose(ColorField field, int paint)
    {
        field.Command!.Execute();
        PaintSwatches(field.Picker)[paint].Command!.Execute();
        Done(field).Command!.Execute();
    }

    /// <summary>The Done button under the field's picker.</summary>
    static Button Done(ColorField field) => field.Picker.Parent!.Parent!.Within().OfType<Button>().Single();

    /// <summary>The swatches of the paint colors: after the color being made.</summary>
    static Swatch[] PaintSwatches(ColorPicker picker) => picker.Within().OfType<Swatch>().Skip(1).Take(Paint.Length).ToArray();

    /// <summary>The swatches of the colors chosen before: the row after the paint colors.</summary>
    static Swatch[] Recent(ColorPicker picker) => picker.Within().OfType<Swatch>().Skip(1 + Paint.Length).Take(ColorPicker.RecentCount).ToArray();
}
