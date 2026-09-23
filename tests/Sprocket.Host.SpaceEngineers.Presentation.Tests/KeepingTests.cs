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
    /// <summary>A step that sets a light's radius to 5.</summary>
    const string ARadiusStep = "60 130 type Airlock enter 60 161 200 375 560 357 1353 311 "
        + "70 193 200 476 800 676 1315 206 type 5 enter";

    /// <summary>The same, with a colour: three values the form shows as one.</summary>
    const string AColourStep = "60 130 type Airlock enter 60 161 200 375 560 357 1353 311 "
        + "70 193 200 476 800 584 1049 371 type #FF8800 enter";

    /// <summary>Pressing the step's blocks, which opens the picker on them.</summary>
    const string Again = "270 175";

    /// <summary>Where the values column's Done is, under a radius field.</summary>
    const string Done = "1365 260";

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
