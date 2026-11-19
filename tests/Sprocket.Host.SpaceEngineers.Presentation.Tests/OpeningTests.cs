using Sprocket.Host.SpaceEngineers.Features.Selector;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Pressing a part of a sentence opens the picker that made it, on that part's column, with the caret
/// in the first thing there is to type into.
/// </summary>
public class OpeningTests
{
    const string ARadiusStep = "60 196 type Airlock enter press trigger press when 200 384 560 340 1394 72 "
        + "press step press task 200 485 press radius 1348 276 type 5 enter";

    /// <summary>A stage of the pick showing, counted from its first.</summary>
    static StageColumn Stage(Screen screen, int at)
    {
        return screen.Editor.Pick().Stages[at];
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

    /// <summary>Pressing a step's action opens its picker on the column of what to do, and pressing its blocks opens it on the blocks, so what is typed goes to the column pressed.</summary>
    [Theory]
    [InlineData(0, "Rad", 1)]
    [InlineData(1, "Air", 0)]
    public void AStepsPartOpensOnItsColumn(int part, string typed, int column)
    {
        var screen = Screen.Fresh(ARadiusStep.Split(' '));

        PressStep(screen, part);
        screen.Do("type", typed);

        Assert.Equal(typed, Stage(screen, column).NarrowText);
        Assert.True(string.IsNullOrEmpty(Stage(screen, 1 - column).NarrowText), "the other column took what was typed");
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

        Assert.Equal("signal slot 1", screen.Editor.Screen.Title());
        Assert.Equal("pre", Stage(screen, 1).NarrowText);
    }

    /// <summary>
    /// A double press on a different block, where one block may be several, takes that block alone and
    /// finishes, the way it does where only one can be chosen.
    /// </summary>
    [Fact]
    public void ADoublePressOnAnotherBlockFinishes()
    {
        const string anOpenStep = "60 196 type Airlock enter press trigger press when 200 359 560 341 1394 72 "
            + "press step press task 400 404 press open 1394 72";

        // The step's blocks, then the outer door pressed twice.
        var screen = Screen.Fresh((anOpenStep + " 270 187 tick 2 double 138 553 tick 2").Split(' '));

        Assert.Equal("Sequence 1", screen.Editor.Screen.Title());
        var system = screen.Editor.Program.Systems[0];
        var step = screen.Editor.Program.Sequences[0].Steps.OfType<Framework.Model.Act>().First();
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
