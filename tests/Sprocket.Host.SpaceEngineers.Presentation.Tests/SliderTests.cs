using Lumel.Input;
using Lumel.Controls;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A number between two bounds is set with a slider beside its box, as the game's terminal sets it.</summary>
public class SliderTests
{
    /// <summary>A range far wider at the top than the bottom runs on a log scale, so its middle is a small number.</summary>
    [Fact]
    public void AWideRangeRunsOnALogScale()
    {
        var field = new SliderField(Field.Between("radius", "1", "50000"), null!);
        var slider = Assert.IsType<Slider>(field.Children[0]);

        field.Text = "200";

        Assert.Equal(Math.Log(200) / Math.Log(50000), slider.Value, 3);
    }

    /// <summary>A narrow range runs straight, so its middle is the number halfway between the bounds.</summary>
    [Fact]
    public void ANarrowRangeRunsStraight()
    {
        var field = new SliderField(Field.Between("radius", "0", "20"), null!);
        var slider = Assert.IsType<Slider>(field.Children[0]);

        field.Text = "5";

        Assert.Equal(0.25f, slider.Value, 3);
    }

    /// <summary>A bounded value gets a slider, its label is the value's name alone, and a new step opens on what the block holds.</summary>
    [Fact]
    public void ABoundedValueIsSlidAndOpensOnWhatTheBlockHolds()
    {
        var screen = Screen.Fresh("60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 press step press task 200 485 press radius".Split(' '));

        var form = screen.Editor.Pick().Arguments!.Form;
        var field = Assert.IsType<SliderField>(Assert.Single(form.Fields));
        Assert.True(screen.Shows("radius"), "the value is not named");
        Assert.False(screen.Shows("radius (0.1 - 20)"), "the label still says the bounds the slider shows");
        Assert.NotEqual("", field.Text);
        Assert.False(form.Complete, "what the block holds counts as given before anybody gave it");
    }

    /// <summary>A number box selects what it holds as it takes the keyboard, so what is typed replaces it.</summary>
    [Fact]
    public void ANumberBoxReplacesWhatItHeldWhenTypedInto()
    {
        var box = new NumberBox { Style = Styles.Number, Focusable = true, SelectsAll = true, Text = "4.5" };
        var input = new InputRoot(box);

        input.Focus(box);
        ((IInputTarget)box).Text('7');

        Assert.Equal("7", box.Text);
    }
}
