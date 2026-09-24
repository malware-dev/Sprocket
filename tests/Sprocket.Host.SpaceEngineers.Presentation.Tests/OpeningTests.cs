using Sprocket.Host.SpaceEngineers.Picking;
using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Pressing a part of a sentence opens the picker that made it, on that part's column, with the caret
/// in the first thing there is to type into.
/// </summary>
/// <remarks>
/// What made something is what changes it, so a single action a trigger does opens the trigger's own
/// picker, on the page the action was chosen on.
/// </remarks>
public class OpeningTests
{
    /// <summary>A step that sets a light's radius to 5, on a trigger's new sequence.</summary>
    const string ARadiusStep = "60 130 type Airlock enter 60 161 200 375 560 357 1353 311 "
        + "70 193 200 476 800 676 1315 206 type 5 enter";

    /// <summary>A stage of the page showing, counted from its first.</summary>
    static PickStage Stage(Screen screen, int at)
    {
        var pick = screen.Editor.Component<PickComponent>();
        return pick.Stages[pick.Page + at];
    }

    /// <summary>A system whose one trigger, the button, runs a new sequence, shown on the system's page.</summary>
    static Screen WithOne()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Editor.Back();
        return screen.Run(2);
    }

    static void PressStep(Screen screen, int part)
    {
        screen.Editor.Invoke(screen.Editor.Component<SequenceComponent>().Steps[0].Slots[part].Command);
        screen.Run(2);
    }

    [Fact]
    public void AStepsActionOpensOnWhatToDo()
    {
        var screen = Screen.Fresh(ARadiusStep.Split(' '));

        PressStep(screen, 0);
        screen.Do("type", "Rad");

        Assert.Equal("Rad", Stage(screen, 1).NarrowText);
        Assert.True(string.IsNullOrEmpty(Stage(screen, 0).NarrowText), "the blocks took what was typed");
    }

    [Fact]
    public void AStepsBlocksOpenOnTheBlocks()
    {
        var screen = Screen.Fresh(ARadiusStep.Split(' '));

        PressStep(screen, 1);
        screen.Do("type", "Air");

        Assert.Equal("Air", Stage(screen, 0).NarrowText);
    }

    [Fact]
    public void AStepsValuesOpenOnTheValues()
    {
        var screen = Screen.Fresh(ARadiusStep.Split(' '));

        PressStep(screen, 2);

        Assert.True(screen.Scene.IsFilling, "the values did not take the caret");
    }

    [Fact]
    public void ATriggersMomentOpensOnWhatHappens()
    {
        var screen = WithOne();

        screen.Editor.Invoke(screen.Editor.Triggers()[0].Slots[1].Command);
        screen.Run(2);
        screen.Do("type", "pre");

        Assert.Equal("When something happens", screen.Editor.Screen.Title());
        Assert.Equal("pre", Stage(screen, 1).NarrowText);
    }

    /// <summary>
    /// A single action a trigger does opens the trigger's picker on the action's page, at the column
    /// of the part pressed.
    /// </summary>
    [Fact]
    public void ATriggersSingleActionOpensOnItsPage()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        screen.Editor.Invoke(screen.Editor.Triggers()[1].Does[0].Command);
        screen.Run(2);

        var pick = screen.Editor.Component<PickComponent>();
        Assert.Equal("Do something", screen.Editor.Screen.Title());
        Assert.True(pick.Page > 0, "the picker opened on the trigger's own page");
        Assert.Contains(Stage(screen, 0).Entries, e => e.Text == "Airlock Outer Door" && e.IsChosen);

        screen.Do("type", "Op");
        Assert.Equal("Op", Stage(screen, 1).NarrowText);

        // Back retraces the way here, which was straight from the system's page.
        screen.Editor.Back();
        screen.Run(2);
        Assert.Equal("Airlock", screen.Editor.Screen.Title());
    }

    /// <summary>
    /// The way back beside the columns pages to the trigger's page all the same, and from then on the
    /// action's page was paged to, so back from it pages back too.
    /// </summary>
    [Fact]
    public void PagedBackToOnceBackPagesThrough()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        screen.Editor.Invoke(screen.Editor.Triggers()[1].Does[0].Command);
        screen.Run(2);

        screen.Editor.Invoke(screen.Editor.Component<PickComponent>().Returns);
        screen.Run(2);
        Assert.Equal("When something happens", screen.Editor.Screen.Title());

        screen.Editor.Invoke(screen.Editor.Component<PickComponent>().Onward);
        screen.Run(2);
        Assert.Equal("Do something", screen.Editor.Screen.Title());

        screen.Editor.Back();
        screen.Run(2);
        Assert.Equal("When something happens", screen.Editor.Screen.Title());
    }

    /// <summary>And a repeat's single action opens the repeat's own picker, on the action's page.</summary>
    [Fact]
    public void ARepeatsSingleActionOpensOnItsPage()
    {
        var screen = WithOne();

        screen.Editor.PressStart("Every…");
        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("close Airlock Outer Door", screen.Editor.Doing(1));

        screen.Editor.Invoke(screen.Editor.Triggers()[1].Does[1].Command);
        screen.Run(2);
        screen.Do("type", "Air");

        Assert.Equal("Do something", screen.Editor.Screen.Title());
        Assert.Equal(2, screen.Editor.Component<PickComponent>().Page);
        Assert.Equal("Air", Stage(screen, 0).NarrowText);

        screen.Editor.Back();
        screen.Run(2);
        Assert.Equal("Airlock", screen.Editor.Screen.Title());
    }
}
