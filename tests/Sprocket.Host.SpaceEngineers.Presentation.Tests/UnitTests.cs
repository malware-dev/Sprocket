using Lumel.Controls;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A number says what it is in after its box, and shows the scale a person reads while the script keeps the game's.</summary>
public class UnitTests
{
    /// <summary>A strength the game holds as 0 to 1 is typed as a percent, and kept as the game holds it.</summary>
    [Fact]
    public void AFractionIsTypedAsAPercent()
    {
        var field = Field.Between("strength", "0", "1");
        field.Unit = " %";
        field.Scale = 100;
        var slider = new SliderField(field, null!);

        slider.Text = "0.25";

        Assert.Equal("25", ((NumberBox)slider.Keyboard).Text);
        Assert.Equal("0.25", slider.Text);
        Assert.Equal(0.25f, Assert.IsType<Slider>(slider.Children[0]).Value, 3);
    }

    /// <summary>The unit stands in the box, after the number.</summary>
    [Fact]
    public void TheUnitStandsInTheBox()
    {
        var field = Field.Between("charge", "0", "100");
        field.Unit = " %";
        var number = new NumberField(field, null!);

        var box = Assert.IsType<NumberBox>(Assert.Single(number.Children));
        Assert.Equal("0.##' %'", box.FormatString);
    }

    /// <summary>A unit that follows a choice follows what the choice stands for, as the words on screen are not what is stored.</summary>
    [Fact]
    public void AUnitFollowsWhatAChoiceStandsFor()
    {
        var item = Field.Among("item", new[] { "Iron Ore", "Steel Plate" }, new[] { "Ore/Iron", "Component/SteelPlate" });
        var amount = Field.Called("amount");
        amount.Entry = FieldInput.Decimal;
        amount.UnitFollows = "item";
        amount.UnitOf = (said, _) => said == "Ore/Iron" ? " kg" : " pieces";
        Assert.Equal("0.##' kg'", Written(item, amount, "Iron Ore"));
        Assert.Equal("0.##' pieces'", Written(item, amount, "Steel Plate"));
    }

    /// <summary>How a form asking for <paramref name="item" /> and <paramref name="amount" /> writes the amount with <paramref name="chosen" /> chosen.</summary>
    static string Written(Field item, Field amount, string chosen)
    {
        var form = new FieldSet();
        form.Show(new[] { item, amount }, new[] { chosen, "" }, null, Screen.Fresh().Editor.Texts);
        return Assert.IsType<NumberBox>(Assert.IsType<NumberField>(form.Fields[1]).Children[0]).FormatString;
    }

    /// <summary>A value that moves in steps goes to the nearest one, shown and kept.</summary>
    [Fact]
    public void AStepGoesToTheNearest()
    {
        var field = Field.Between("pitch", "-2", "2");
        field.Unit = "°";
        field.Scale = 90;
        field.Step = 90;

        Assert.Equal("180", field.Scaled("2"));
        Assert.Equal("1", field.Unscaled("100"));
        Assert.Equal("-1", field.Unscaled("-80"));
    }

    /// <summary>A percent of Sprocket's own is a slider from 0 to 100 in a check, its unit after the box and in the sentence.</summary>
    [Fact]
    public void APercentIsSlidAndSaysItsUnit()
    {
        var sim = Simulation.Fresh();
        var door = sim.Editor.World.Entities.First(entity => entity.Name == "Airlock Inner Door").Kind;
        var open = door.Actions.First(action => action.Id == "how-far-open");

        var fields = ComparisonQuestion.Fields(open, door.Events);
        var compared = fields[1];
        Assert.Equal(" %", compared.Unit);
        Assert.True(SliderField.Takes(compared));
        Assert.Equal("0", compared.Least);
        Assert.Equal("100", compared.Most);
    }
}
