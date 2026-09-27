using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Changing which blocks a step acts on keeps the values it was already given.
/// </summary>
/// <remarks>
/// The picker is handed the step's values when it opens on one, so its column of values opens
/// holding them. Without that, a box left alone means "no value" rather than "the one that was
/// there", and the only way to keep a value is to type it again.
/// </remarks>
public class KeepingTests
{
    const string ARadiusStep = "60 133 type Airlock enter 60 164 200 384 560 340 1353 314 "
        + "70 200 200 485 800 653 1315 209 type 5 enter";

    const string AColourStep = "60 133 type Airlock enter 60 164 200 384 560 340 1353 314 "
        + "70 200 200 485 800 565 1049 374 type #FF8800 enter";

    const string Again = "270 182";

    const string Done = "1365 263";

    [Fact]
    public void ThePickerOpensOnTheValueTheStepAlreadyHas()
    {
        var screen = Screen.Fresh((ARadiusStep + " " + Again).Split(' '));

        Assert.True(screen.Shows("Radius (0.1 - 20)"), "the column asks for nothing");
        Assert.True(screen.Shows("5.0"), "the box opened empty");
    }

    /// <summary>
    /// Changing only which blocks a step acts on finishes without asking for anything, and keeps
    /// the value.
    /// </summary>
    [Fact]
    public void ChangingOnlyTheBlocksFinishesWithTheValue()
    {
        var screen = Screen.Fresh((ARadiusStep + " " + Again + " " + Done).Split(' '));

        Assert.Equal("Sequence 1", screen.Ended);
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(5L, Assert.Single(act.Values).Whole);
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

        Assert.True(screen.Shows("#FF8800"), "the picker opened on no colour");
    }
}
