using Lumel;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Sim.Machine;
using Lumel.Layout;
using Lumel.Text;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>How a Form lays out its rows: side by side where label and field fit, the field under its label where they do not.</summary>
public class FormTests
{
    const float Wide = 400f;
    const float High = 400f;
    const float Box = 120f;

    /// <summary>A label and a field that fit share a line, the field against the right-hand edge.</summary>
    [Fact]
    public void ALabelAndAFieldThatFitShareALine()
    {
        Label label, field;
        var form = new Form { Rows = { { new Label { Text = "radius" }.As(out label), new Label { Text = "4", Placement = FixedSize.Wide(Box) }.As(out field) } } };

        _ = new Easel(form, Wide, High);

        Assert.Equal(0f, label.Bounds.X, 3);
        Assert.Equal(Wide, field.Bounds.X + field.Bounds.Width, 3);
        Assert.True(field.Bounds.Y < label.Bounds.Y + label.Bounds.Height, "the field is not on the label's line");
        Assert.False(form.Rows[0].Stacked);
    }

    /// <summary>Where they do not fit, the field moves under its label, from the left, and nothing passes the right-hand edge.</summary>
    [Fact]
    public void AFieldThatDoesNotFitGoesUnderItsLabel()
    {
        Label label, field;
        var form = new Form
        {
            Rows = { { new Label { Text = "the longest label a value was ever given" }.As(out label), new Label { Text = "4", Placement = FixedSize.Wide(Box) }.As(out field) } }
        };

        _ = new Easel(form, 200f, High);

        Assert.True(form.Rows[0].Stacked);
        Assert.Equal(0f, field.Bounds.X, 3);
        Assert.True(field.Bounds.Y >= label.Bounds.Y + label.Bounds.Height, "the field is not under its label");
        Assert.True(field.Bounds.X + field.Bounds.Width <= 200f + 0.01f, "the field passes the right-hand edge");
    }

    /// <summary>The decision is each row's own: a short label keeps its field beside it while a long one in the same form does not.</summary>
    [Fact]
    public void EachRowDecidesForItself()
    {
        var form = new Form
        {
            Rows =
            {
                { new Label { Text = "radius" }, new Label { Text = "4", Placement = FixedSize.Wide(Box) } },
                { new Label { Text = "the longest label a value was ever given" }, new Label { Text = "5", Placement = FixedSize.Wide(Box) } }
            }
        };

        _ = new Easel(form, 260f, High);

        Assert.False(form.Rows[0].Stacked);
        Assert.True(form.Rows[1].Stacked);
    }

    /// <summary>A row asked for two lines puts its field under the label however wide the form, across its whole width.</summary>
    [Fact]
    public void TwoLinesPutsTheFieldUnderTheLabelAcrossTheWholeWidth()
    {
        Label label, field;
        var form = new Form { Rows = { { new Label { Text = "text" }.As(out label), new Label { Text = "Hello", Align = Align.Wide }.As(out field), FormRowLines.Two } } };

        _ = new Easel(form, Wide, High);

        Assert.True(form.Rows[0].Stacked);
        Assert.True(field.Bounds.Y >= label.Bounds.Y + label.Bounds.Height, "the field is not under its label");
        Assert.Equal(0f, field.Bounds.X, 3);
        Assert.Equal(Wide, field.Bounds.Width, 3);
    }

    /// <summary>A row of one element alone takes the whole width, with no label.</summary>
    [Fact]
    public void ARowOfOneElementTakesTheWholeWidth()
    {
        Label swatches;
        var form = new Form { Rows = { new Label { Text = "swatches" }.As(out swatches) } };

        _ = new Easel(form, Wide, High);

        Assert.Equal(0f, swatches.Bounds.X, 3);
        Assert.Equal(Wide, swatches.Bounds.Width, 3);
    }

    /// <summary>Clearing the rows takes their elements away, so a form can be asked something else.</summary>
    [Fact]
    public void ClearingTakesTheRowsAway()
    {
        var form = new Form { Rows = { { new Label { Text = "radius" }, new Label { Text = "4" } } } };

        form.Rows.Clear();
        _ = new Easel(form, Wide, High);

        Assert.Equal(0, form.Rows.Count);
        Assert.Equal(0f, form.DesiredSize.Y, 3);
    }

    /// <summary>Show text is written in a box of several lines under its label, where Enter starts a new line.</summary>
    [Fact]
    public void ShowTextIsWrittenOverSeveralLines()
    {
        var rig = Rig.Demo();
        rig.Weld(new Billboard(40, "Status Board"));
        var screen = Screen.On(rig);
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Status Board");
        screen.Run(2);
        screen.Editor.PressChoice("show text");
        screen.Run(2);

        var form = screen.Editor.Pick().Arguments!.Form;
        var at = form.Fields.ToList().FindIndex(field => field is LinesField);
        Assert.True(at >= 0, "show text has no box of several lines");
        Assert.True(form.Rows[at].Stacked, "the text is not under its label");

        form.Fields[at].Text = "Cycling\nStand clear";
        Assert.EndsWith("\"Cycling&br;Stand clear\"", form.Answer);
    }
}
