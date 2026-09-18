using Reactor.Host.SpaceEngineers.Lists;
using Reactor.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What a trigger is for, asked once what starts it is known.
/// </summary>
/// <remarks>
/// A trigger does one thing, and the list of what that can be opens with the way to have several
/// things happen - so nobody has to be told the rule. The other rows appear only once there is a
/// sequence to point at, which is also the first moment either of them means anything.
/// </remarks>
public class TargetTests
{
    /// <summary>A system with one named sequence, made the ordinary way.</summary>
    static Screen WithOne()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Run a new sequence");
        screen.Editor.Name("Cycle");
        screen.Editor.Back();
        return screen.Run(2);
    }

    /// <summary>Starts a second trigger, up to the question of what it does.</summary>
    static void Another(Screen screen)
    {
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
    }

    [Fact]
    public void NothingToPointAtOffersOnlyTheTwoThatMakeSense()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.True(screen.Shows("Run a new sequence"));
        Assert.True(screen.Shows("Do something"));
        Assert.False(screen.Shows("Run an existing sequence"));
        Assert.False(screen.Shows("Stop a sequence"));
    }

    [Fact]
    public void ASecondTriggerCanRunTheSameSequence()
    {
        var screen = WithOne();
        Another(screen);

        screen.Editor.PressChoice("Run an existing sequence");
        screen.Editor.PressChoice("Cycle");
        screen.Run(2);

        // One sequence, two ways in, and the list of triggers says so.
        Assert.Single(screen.Editor.Program.Sequences);

        Assert.Equal(2, screen.Editor.Triggers().Count);
        Assert.Equal("runs Cycle", screen.Editor.Doing(0));
        Assert.Equal("runs Cycle", screen.Editor.Doing(1));
    }

    [Fact]
    public void ATriggerCanStopASequence()
    {
        var screen = WithOne();
        Another(screen);

        screen.Editor.PressChoice("Stop a sequence");
        screen.Editor.PressChoice("Cycle");
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Triggers().Count);
        Assert.Equal("stops Cycle", screen.Editor.Doing(1));
    }

    /// <summary>
    /// One plain thing makes a sequence nobody named, which the list does not show as a thing.
    /// </summary>
    /// <remarks>
    /// It is not a sequence somebody made; it is what their trigger does. So the trigger's own row
    /// says it - "open Airlock Inner Door" - and the list of sequences is left for the things that
    /// stand on their own.
    /// </remarks>
    [Fact]
    public void OnePlainThingIsSaidOnTheTriggersOwnRow()
    {
        var screen = WithOne();
        Another(screen);

        screen.Editor.PressChoice("Do something");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Triggers().Count);
        Assert.Equal("close Airlock Outer Door", screen.Editor.Doing(1));

        // Two sequences exist, but only the named one is a row in the list of them.
        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
        Assert.Single(screen.Editor.Rows().Entries);
    }

    /// <summary>A trigger is thrown away from its row, and takes its own sequence with it.</summary>
    [Fact]
    public void DeletingATriggerTakesTheOneThingItDidWithIt()
    {
        var screen = WithOne();
        Another(screen);

        screen.Editor.PressChoice("Do something");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("close Airlock Outer Door", screen.Editor.Doing(1));

        screen.Editor.Invoke(screen.Editor.Triggers()[1].DeleteCommand);
        screen.Run(2);

        Assert.Single(screen.Editor.Triggers());
        Assert.Single(screen.Editor.Program.Sequences);
    }

    /// <summary>Deleting a trigger leaves a sequence that stands on its own alone.</summary>
    [Fact]
    public void DeletingATriggerLeavesTheSequenceItRan()
    {
        var screen = WithOne();

        screen.Editor.Invoke(screen.Editor.Triggers()[0].DeleteCommand);
        screen.Run(2);

        Assert.Empty(screen.Editor.Triggers());
        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Single(screen.Editor.Rows().Entries);
    }
}
