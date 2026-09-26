using Sprocket.Host.SpaceEngineers.Picking;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What a trigger is for, chosen beside what starts it.
/// </summary>
/// <remarks>
/// A trigger does one thing, and the list of what that can be opens with the way to have several
/// things happen - so nobody has to be told the rule. Running or stopping a sequence the system has
/// appears only once there is one to point at, which is also the first moment either means anything.
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
        screen.Editor.Name("Cycle");
        screen.Editor.Back();
        return screen.Run(2);
    }

    /// <summary>Starts a second trigger, up to choosing what it does.</summary>
    static void Another(Screen screen)
    {
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
    }

    [Fact]
    public void NothingToPointAtOffersOnlyTheTwoThatMakeSense()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Run(2);

        var does = screen.Editor.Component<PickComponent>().Stages[2];
        Assert.Equal(new[] { "A single action…", "A new sequence" }, does.Entries.Select(e => e.Text));
        Assert.Empty(does.Options);
    }

    [Fact]
    public void ASecondTriggerCanRunTheSameSequence()
    {
        var screen = WithOne();
        Another(screen);

        screen.Editor.PressChoice("Cycle");
        screen.Editor.PressPrimary();
        screen.Run(2);

        // One sequence, two ways in, and the list of triggers says so.
        Assert.Single(screen.Editor.Program.Sequences);

        Assert.Equal(2, screen.Editor.Triggers().Count);
        Assert.Equal("then run Cycle", screen.Editor.Doing(0));
        Assert.Equal("then run Cycle", screen.Editor.Doing(1));
    }

    [Fact]
    public void ATriggerCanStopASequence()
    {
        var screen = WithOne();
        Another(screen);

        screen.Editor.PressChoice("stop");
        screen.Editor.PressChoice("Cycle");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Triggers().Count);
        Assert.Equal("then stop Cycle", screen.Editor.Doing(1));
    }

    /// <summary>What it does comes once what happens is chosen, and not before.</summary>
    [Fact]
    public void WhatItDoesWaitsForWhatHappens()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");

        Assert.Equal(2, screen.Editor.Component<PickComponent>().Stages.Count);

        screen.Editor.PressChoice("is pressed");
        Assert.Equal(3, screen.Editor.Component<PickComponent>().Stages.Count);
    }

    /// <summary>Stopping lists only what there is to stop, and nothing is finished until one is chosen.</summary>
    [Fact]
    public void StoppingListsOnlyTheSequences()
    {
        var screen = WithOne();
        Another(screen);

        screen.Editor.PressChoice("stop");
        screen.Run(2);

        var does = screen.Editor.Component<PickComponent>().Stages[2];
        Assert.Equal(new[] { "Cycle" }, does.Entries.Select(e => e.Text));
        Assert.False(screen.Editor.Screen.HasPrimary, "stopping finished without saying what");

        // And back to running offers the two that make something again, a new sequence already chosen,
        // with the sequences under their own heading after them.
        screen.Editor.PressChoice("run");
        Assert.Equal(new[] { "A single action…", "A new sequence", "Sequences", "Cycle" },
            does.Entries.Select(e => e.Text));
        Assert.Contains(does.Entries, e => e.Text == "A new sequence" && e.IsChosen);
        Assert.True(does.Entries[2].IsHeading, "the sequences are not set apart");
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

        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Triggers().Count);
        Assert.Equal("then close Airlock Outer Door", screen.Editor.Doing(1));

        // Two sequences exist, but only the named one is a row in the list of them.
        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
        Assert.Single(screen.Editor.Rows());
    }

    /// <summary>A trigger is thrown away from its row, and takes its own sequence with it.</summary>
    [Fact]
    public void DeletingATriggerTakesTheOneThingItDidWithIt()
    {
        var screen = WithOne();
        Another(screen);

        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("then close Airlock Outer Door", screen.Editor.Doing(1));

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
        Assert.Single(screen.Editor.Rows());
    }
}
