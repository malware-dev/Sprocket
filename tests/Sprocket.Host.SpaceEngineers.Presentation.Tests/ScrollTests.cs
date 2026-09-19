using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A list taller than the column it sits in has to be reachable by scrolling.
/// </summary>
/// <remarks>
/// The column stacked its parts, and a stack measures every child against infinite height. The rows
/// scroller therefore grew to fit whatever was in it and had nothing left to scroll, so a long block
/// list simply ran off the bottom of the screen with no way down.
/// </remarks>
public class ScrollTests
{
    /// <summary>From an empty construct to the block picker: name a system, then ask what starts it.</summary>
    const string OpenThePicker = "60 130 type Airlock enter 60 161";

    [Fact]
    public void TheBlocksListIsTallerThanTheColumnItSitsIn()
    {
        var picker = Screen.Fresh(OpenThePicker.Split(' '));

        var first = picker.Text("Airlock Button");
        var last = picker.Text("Warning Beacon");

        Assert.True(last.At.Y > first.Clip.Y + first.Clip.Height,
            $"the list ends at {last.At.Y:0} and its column at {first.Clip.Y + first.Clip.Height:0}, "
            + "so nothing is out of reach and this test is no longer testing anything");
    }

    [Fact]
    public void ScrollingBringsTheEndOfTheListIntoReach()
    {
        var picker = Screen.Fresh(OpenThePicker.Split(' '));

        Assert.False(picker.Shows("Warning Beacon"), "the last block was reachable without scrolling");

        picker.Do("scroll", "250", "600", "300");

        Assert.True(picker.Shows("Warning Beacon"), "scrolling did not bring the last block into view");
    }
}
