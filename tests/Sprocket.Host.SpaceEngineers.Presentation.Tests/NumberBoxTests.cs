using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Localization;
using VRage.Input;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A box that holds a number: against its right-hand end, as wide as the widest number it expects, taking only what can become a number, and stepped by up and down.</summary>
public class NumberBoxTests
{
    const float Wide = 400f;
    const float High = 200f;

    /// <summary>The number is drawn ending at the right-hand end of the box.</summary>
    [Fact]
    public void ANumberSitsAgainstTheRightHandEnd()
    {
        var (box, easel, _) = Laid("42");

        var run = Assert.Single(easel.Written);
        var end = run.Position.X + easel.WidthOf("42");
        var right = (box.ContentBounds.X + box.ContentBounds.Width - 2f) * easel.Pixels;

        Assert.Equal(right, end, 0);
    }

    /// <summary>A press in a number set against the right finds the boundary under it.</summary>
    [Fact]
    public void APressFindsTheBoundaryInANumberSetRight()
    {
        var (box, easel, input) = Laid("4250");

        var start = box.ContentBounds.X + box.ContentBounds.Width - 2f - easel.WidthOf("4250") / easel.Pixels;
        var at = new Vector2(start + easel.WidthOf("42") / easel.Pixels, box.Bounds.Y + box.Bounds.Height / 2f);
        input.PointerDown(at, 1, false, false);
        input.PointerUp(at);

        Assert.Equal(2, box.Caret);
    }

    /// <summary>Only what can still become a number is taken: digits, one point unless it is whole, and a leading minus where it may be below nought.</summary>
    [Fact]
    public void OnlyWhatCanBecomeANumberIsTaken()
    {
        var (box, _, _) = Laid("");

        box.Insert("1x");
        box.Insert("-1");
        Assert.Equal("", box.Text);

        box.Insert("1.5");
        box.Insert(".");
        Assert.Equal("1.5", box.Text);

        var (negative, _, _) = Laid("", b => b.Negative = true);
        negative.Insert("-2.5");
        Assert.Equal("-2.5", negative.Text);

        var (whole, _, _) = Laid("", b => b.Whole = true);
        whole.Insert("3.");
        Assert.Equal("", whole.Text);
    }

    /// <summary>Up and down move the number a step on from the nearest step, within its bounds, and say so as an edit.</summary>
    [Fact]
    public void UpAndDownStepWithinTheBounds()
    {
        var (box, _, input) = Laid("12", b =>
        {
            b.Step = 5;
            b.Most = 20;
        });
        var edits = 0;
        box.Edited += () => edits++;
        input.Focus(box);

        input.Key(MyKeys.Up, false, false);
        Assert.Equal("15", box.Text);

        input.Key(MyKeys.Up, false, false);
        input.Key(MyKeys.Up, false, false);
        Assert.Equal("20", box.Text);

        box.Text = "3";
        input.Key(MyKeys.Down, false, false);
        Assert.Equal("0", box.Text);

        Assert.Equal(4, edits);
    }

    /// <summary>The box is as wide as the widest number it expects, whatever it holds.</summary>
    [Fact]
    public void TheWidthIsTheWidestExpected()
    {
        var narrow = new NumberBox { Widest = "00", Align = Align.Left };
        var wide = new NumberBox { Widest = "000000", Text = "1", Align = Align.Left };
        var column = new VStack { Children = { narrow, wide } };
        var easel = new Easel(column, Wide, High);

        Assert.True(wide.Bounds.Width > narrow.Bounds.Width);

        var before = wide.Bounds.Width;
        wide.Text = "123";
        easel.Draw(Wide, High);
        Assert.Equal(before, wide.Bounds.Width, 3);
    }

    /// <summary>A value that is a number is given in a number box, and one that is words in a box for words.</summary>
    [Fact]
    public void AFormGivesANumberANumberBox()
    {
        var form = new FieldSet();
        form.Show(new[]
        {
            Field.Between("speed", "0", "5"),
            new Field { Name = "count", Entry = Entry.Whole },
            Field.Called("label")
        }, new[] { "", "", "" }, new Hsv[0], new Texts());

        Assert.IsType<NumberBox>(form.Fields[0].Keyboard);
        Assert.IsType<NumberBox>(form.Fields[1].Keyboard);
        Assert.IsNotType<NumberBox>(form.Fields[2].Keyboard);
    }

    static (NumberBox, Easel, InputRoot) Laid(string text, Action<NumberBox>? shape = null)
    {
        var box = new NumberBox { Placement = Fixed.Wide(300f) };
        shape?.Invoke(box);
        box.Text = text;
        var root = new VStack { Children = { box } };
        var easel = new Easel(root, Wide, High);
        return (box, easel, new InputRoot(root));
    }
}
