using Sprocket.Host.SpaceEngineers.Features.Picking;
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
    const string ARadiusStep = "60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 "
        + "press step press task 200 485 press radius 1348 232 type 5 enter";

    /// <summary>A stage of the page showing, counted from its first.</summary>
    static StageColumn Stage(Screen screen, int at)
    {
        var pick = screen.Editor.Pick();
        return pick.Stages[pick.Page + at];
    }

    /// <summary>A system whose one trigger, the button, runs a new sequence, shown on the system's page.</summary>
    static Screen WithOne()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Editor.Back();
        return screen.Run(2);
    }

    static void PressStep(Screen screen, int part)
    {
        screen.Editor.StepRows()[0].Slots[part].Opens!.Execute();
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

        screen.Editor.Triggers()[0].Starts[1].Opens!.Execute();
        screen.Run(2);
        screen.Do("type", "pre");

        Assert.Equal("slot 1", screen.Editor.Screen.Title());
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

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        screen.Editor.Triggers()[1].Does[0].Opens!.Execute();
        screen.Run(2);

        var pick = screen.Editor.Pick();
        Assert.Equal("slot 3", screen.Editor.Screen.Title());
        Assert.True(pick.Page > 0, "the picker opened on the trigger's own page");
        Assert.Contains(Stage(screen, 0).Entries(), e => e.Text == "Airlock Outer Door" && e.IsChosen);

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

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        screen.Editor.Triggers()[1].Does[0].Opens!.Execute();
        screen.Run(2);

        screen.Editor.Pick().Returns!.Execute();
        screen.Run(2);
        Assert.Equal("slot 2", screen.Editor.Screen.Title());

        screen.Editor.Pick().Onward!.Execute();
        screen.Run(2);
        Assert.Equal("slot 3", screen.Editor.Screen.Title());

        screen.Editor.Back();
        screen.Run(2);
        Assert.Equal("slot 2", screen.Editor.Screen.Title());
    }

    /// <summary>And a repeat's single action opens the repeat's own picker, on the action's page.</summary>
    [Fact]
    public void ARepeatsSingleActionOpensOnItsPage()
    {
        var screen = WithOne();

        screen.Editor.PressStart("every");
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("close slot 2", screen.Editor.Doing(1));

        screen.Editor.Triggers()[1].Does[1].Opens!.Execute();
        screen.Run(2);
        screen.Do("type", "Air");

        Assert.Equal("slot 2", screen.Editor.Screen.Title());
        Assert.Equal(2, screen.Editor.Pick().Page);
        Assert.Equal("Air", Stage(screen, 0).NarrowText);

        screen.Editor.Back();
        screen.Run(2);
        Assert.Equal("Airlock", screen.Editor.Screen.Title());
    }

    /// <summary>
    /// A double press on a different block, where one block may be several, takes that block alone and
    /// finishes, the way it does where only one can be chosen.
    /// </summary>
    [Fact]
    public void ADoublePressOnAnotherBlockFinishes()
    {
        const string anOpenStep = "60 196 type Airlock enter press trigger press when 200 351 560 333 1394 72 "
            + "press step press task 400 396 press open 1394 72";

        // The step's blocks, then the outer door pressed twice.
        var screen = Screen.Fresh((anOpenStep + " 270 187 tick 2 double 138 545 tick 2").Split(' '));

        Assert.Equal("Sequence 1", screen.Editor.Screen.Title());
        var system = screen.Editor.Program.Systems[0];
        var step = screen.Editor.Program.Sequences[0].Steps.OfType<Sequences.Act>().First();
        var filled = screen.Editor.Seen(system, step.Targets).Select(name => screen.Editor.Runner.Bindings.Entity(name)!.Name);
        Assert.Equal(new[] { "Airlock Outer Door" }, filled);
    }

    /// <summary>A picker opens narrowed the way the last one of its sort was left, searches included.</summary>
    [Fact]
    public void APickerOpensSearchedTheWayTheLastWasLeft()
    {
        var screen = WithOne();

        screen.Editor.PressStart("when");
        screen.Run(2);
        Stage(screen, 0).Search("Air");
        screen.Editor.Back();
        screen.Run(2);

        screen.Editor.PressStart("when");
        screen.Run(2);

        Assert.Equal("Air", Stage(screen, 0).NarrowText);
    }
}
