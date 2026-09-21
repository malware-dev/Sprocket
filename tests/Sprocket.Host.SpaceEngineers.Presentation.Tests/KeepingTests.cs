using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Changing which blocks a step acts on keeps the values it was already given.
/// </summary>
/// <remarks>
/// The picker is handed the step's values when it opens on one, so the question that finishes it
/// opens holding them. Without that, a box left alone means "no value" rather than "the one that
/// was there", and the only way to keep a value is to type it again.
/// </remarks>
public class KeepingTests
{
    /// <summary>A step that sets a light's radius to 5.</summary>
    const string ARadiusStep = "60 130 type Airlock enter 60 161 200 375 560 272 1380 1000 "
        + "1300 918 70 193 200 500 800 610 1380 1000 type 5 enter";

    /// <summary>The same, with a colour: three values the form shows as one.</summary>
    const string AColourStep = "60 130 type Airlock enter 60 161 200 375 560 272 1380 1000 "
        + "1300 918 70 193 200 500 800 692 1380 1000 type #FF8800 enter";

    /// <summary>Pressing the step's blocks, then finishing.</summary>
    const string Again = "270 175 1380 1000";

    [Fact]
    public void ThePickerOpensOnTheValueTheStepAlreadyHas()
    {
        var screen = Screen.Fresh((ARadiusStep + " " + Again).Split(' '));

        Assert.True(screen.Shows("This needs one value."), "finishing asked nothing");
        Assert.True(screen.Shows("5"), "the box opened empty");
    }

    /// <summary>
    /// And on a colour, which is three values the question shows as one.
    /// </summary>
    /// <remarks>
    /// The harder case, because what opens in the field is not what the program holds: the picker
    /// has to collapse three numbers the way the step's own chip does, or the two would disagree
    /// about one value.
    /// </remarks>
    [Fact]
    public void AndOnAColourItOpensOnTheColour()
    {
        var screen = Screen.Fresh((AColourStep + " " + Again).Split(' '));

        Assert.True(screen.Shows("This needs a colour."), "finishing asked for something else");
        Assert.True(screen.Shows("#FF8800"), "the picker opened on no colour");
    }
}
