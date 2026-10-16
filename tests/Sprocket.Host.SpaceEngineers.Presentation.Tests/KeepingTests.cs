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
    const string ARadiusStep = "60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 "
        + "press step press task 200 485 press radius 1348 232 type 5 enter";

    const string Again = "208 182";

    const string Done = "1394 72";

    [Fact]
    public void ThePickerOpensOnTheValueTheStepAlreadyHas()
    {
        var screen = Screen.Fresh((ARadiusStep + " " + Again).Split(' '));

        Assert.True(screen.Shows("radius"), "the column asks for nothing");
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
        Assert.Equal(5L, Assert.Single(act.Arguments).Whole);
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
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.PressAdd("task");
        screen.Editor.PressBlock("Cockpit Light");
        screen.Run(2);
        screen.Editor.PressChoice("color");
        screen.Run(2);
        screen.Editor.Pick().Arguments!.Form.Fields[0].Text = "#FF8800";
        screen.Editor.PressPrimary();
        screen.Run(2);

        screen.Editor.PressStepSlot(0, 1);
        screen.Run(2);

        Assert.True(screen.Shows("#FF8800"), "the picker opened on no colour");
    }

    /// <summary>A double press on other blocks that offer the step's action finishes at once, keeping the value.</summary>
    [Fact]
    public void ADoublePressOnOtherBlocksFinishesWithTheValue()
    {
        var screen = OnTheBlocks();

        screen.Editor.DoublePressBlock("Cockpit Light");
        screen.Run(2);

        Assert.Equal("Sequence 1", screen.Editor.Screen.Title());
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(new[] { "Cockpit Light" }, screen.Editor.Seen(screen.Editor.Program.Systems[0], act.Targets));
        Assert.Equal(5.0, Assert.Single(act.Arguments).Number);
    }

    /// <summary>Blocks that do not offer the step's action leave nothing chosen, and nothing to finish with.</summary>
    [Fact]
    public void BlocksWithoutTheActionClearIt()
    {
        var screen = OnTheBlocks();

        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Run(2);

        Assert.Equal("\u2026 Airlock Inner Door", screen.Editor.Screen.Title());
        Assert.False(screen.Editor.Current.CanPrimary, "the picker would finish with nothing chosen");
        Assert.False(screen.Shows("radius"), "the values column still asks for the radius");
    }

    /// <summary>Back on blocks that offer the step's action, the action is chosen again and holds its value.</summary>
    [Fact]
    public void ReturningToBlocksThatOfferTheActionBringsItBack()
    {
        var screen = OnTheBlocks();

        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Run(2);
        screen.Editor.PressBlock("Cockpit Light");
        screen.Run(2);

        Assert.Equal("Radius on Cockpit Light", screen.Editor.Screen.Title());
        Assert.True(screen.Shows("5.0"), "the value did not come back with the action");

        screen.Editor.DoublePressBlock("Cockpit Light");
        screen.Run(2);

        Assert.Equal("Sequence 1", screen.Editor.Screen.Title());
        var act = Assert.IsType<Act>(screen.Editor.Program.Sequences[0].Steps[0]);
        Assert.Equal(5.0, Assert.Single(act.Arguments).Number);
    }

    /// <summary>An action chosen in place of the step's asks for its own values: a double press on it goes to them rather than finishing.</summary>
    [Fact]
    public void AnotherActionAsksForItsValues()
    {
        var screen = OnTheBlocks();

        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Run(2);
        screen.Editor.PressBlock("Cockpit Light");
        screen.Run(2);
        screen.Editor.DoublePressChoice("falloff");
        screen.Run(2);

        Assert.Equal("Falloff on Cockpit Light", screen.Editor.Screen.Title());
        Assert.Equal(1, screen.Editor.Pick().Arguments!.Form.Count);
        Assert.False(screen.Shows("5.0"), "the radius's value was given to the falloff");
    }

    /// <summary>The radius step, with the picker opened on its blocks.</summary>
    static Screen OnTheBlocks()
    {
        var screen = Screen.Fresh(ARadiusStep.Split(' '));
        screen.Editor.StepRows()[0].Slots[1].Opens!.Execute();
        screen.Run(2);
        return screen;
    }
}
