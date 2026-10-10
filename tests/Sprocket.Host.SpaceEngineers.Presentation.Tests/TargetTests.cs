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
        screen.Editor.NewButtonSystem("Airlock");
        screen.Editor.Name("Cycle");
        screen.Editor.Back();
        return screen.Run(2);
    }

    /// <summary>Starts a second trigger, up to choosing what it does.</summary>
    static void Another(Screen screen)
    {
        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
    }

    /// <summary>With no sequence to point at, a new trigger is offered a new sequence and nothing else.</summary>
    [Fact]
    public void NothingToPointAtOffersOnlyANewSequence()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("button 1 is pressed");
        screen.Run(2);

        var does = screen.Editor.Pick().Stages[2];
        Assert.Equal(new[] { "A new sequence" }, does.Entries().Select(e => e.Text));
        Assert.Empty(does.Options);
    }

    /// <summary>A single action is offered to a new trigger in developer mode.</summary>
    [Fact]
    public void DeveloperModeOffersASingleAction()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("button 1 is pressed");
        screen.Run(2);

        var does = screen.Editor.Pick().Stages[2];
        Assert.Equal(new[] { "A task on blocks…", "A new sequence" }, does.Entries().Select(e => e.Text));
    }

    /// <summary>A trigger that does a single action already offers it when opened again, out of developer mode.</summary>
    [Fact]
    public void ATriggersOwnSingleActionIsStillOffered()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("button 1 is pressed");
        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        screen.Editor.DeveloperMode = false;
        screen.Run(1);
        screen.Editor.PressWhen(0);
        screen.Run(2);

        Assert.Contains(screen.Editor.Pick().Stages[2].Entries(), e => e.Text == "A task on blocks…" && e.IsChosen);
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
        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Button");

        Assert.Equal(2, screen.Editor.Pick().Stages.Count);

        screen.Editor.PressChoice("button 1 is pressed");
        Assert.Equal(3, screen.Editor.Pick().Stages.Count);
    }

    /// <summary>Stopping lists only what there is to stop, and nothing is finished until one is chosen.</summary>
    [Fact]
    public void StoppingListsOnlyTheSequences()
    {
        var screen = WithOne();
        Another(screen);

        screen.Editor.PressChoice("stop");
        screen.Run(2);

        var does = screen.Editor.Pick().Stages[2];
        Assert.Equal(new[] { "Cycle" }, does.Entries().Select(e => e.Text));
        Assert.Null(screen.Editor.Screen.Primary);

        // And back to running offers the two that make something again, a new sequence already chosen,
        // with the sequences under their own heading after them.
        screen.Editor.PressChoice("run");
        Assert.Equal(new[] { "A new sequence", "Sequences", "Cycle" }, does.Entries().Select(e => e.Text));
        Assert.Contains(does.Entries(), e => e.Text == "A new sequence" && e.IsChosen);
        Assert.True(does.Entries()[1].IsHeading, "the sequences are not set apart");
    }

    /// <summary>
    /// One plain thing is the trigger's own act, said on the trigger's own row, and makes no sequence.
    /// </summary>
    [Fact]
    public void OnePlainThingIsSaidOnTheTriggersOwnRow()
    {
        var screen = WithOne();
        Another(screen);

        screen.Editor.DeveloperMode = true;

        screen.Run(1);

        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Triggers().Count);
        Assert.Equal("then close Airlock Outer Door", screen.Editor.Doing(1));
        Assert.NotNull(screen.Editor.Program.Systems[0].Triggers[1].Acts);

        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Single(screen.Editor.Rows());
    }

    /// <summary>A trigger is thrown away from its row, and takes the act it did with it.</summary>
    [Fact]
    public void DeletingATriggerTakesTheOneThingItDidWithIt()
    {
        var screen = WithOne();
        Another(screen);

        screen.Editor.DeveloperMode = true;

        screen.Run(1);

        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("then close Airlock Outer Door", screen.Editor.Doing(1));

        screen.Editor.Triggers()[1].Deletes!.Execute();
        screen.Run(2);

        Assert.Single(screen.Editor.Triggers());
        Assert.Single(screen.Editor.Program.Sequences);
    }

    /// <summary>Deleting a trigger leaves a sequence that stands on its own alone.</summary>
    [Fact]
    public void DeletingATriggerLeavesTheSequenceItRan()
    {
        var screen = WithOne();

        screen.Editor.Triggers()[0].Deletes!.Execute();
        screen.Run(2);

        Assert.Empty(screen.Editor.Triggers());
        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Single(screen.Editor.Rows());
    }
}
