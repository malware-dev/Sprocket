using Reactor.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What starts a sequence is changed on the row that says so.
/// </summary>
/// <remarks>
/// It used to be two chips on the sequence's first line, which could only ever show one trigger -
/// so a sequence three buttons started showed one of them and lied about the rest. The row is the
/// trigger itself, and pressing it asks the question it was made with.
/// </remarks>
public class ChangingTests
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
        screen.Editor.Back();
        return screen.Run(2);
    }

    static AutomationSystem System(Screen screen) => screen.Editor.Program.Systems[0];

    [Fact]
    public void PressingATriggerAsksAgainWhichBlockStartsIt()
    {
        var screen = WithOne();

        screen.Editor.PressWhen(0);
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Run(2);

        var trigger = Assert.IsType<WhenTrigger>(System(screen).Triggers[0]);
        Assert.Equal("closed", trigger.EventId);
        Assert.Equal("When Airlock Inner Door finishes closing", screen.Editor.Triggers().Entries[0].Summary);

        // The one it replaced is gone rather than left beside it.
        Assert.Single(System(screen).Triggers);
    }

    /// <summary>
    /// The sequence it runs comes through the change, and so does stopping that sequence.
    /// </summary>
    /// <remarks>
    /// The question was only ever about what starts it. A trigger that came back running the wrong
    /// sequence, or running one it used to stop, would be the machine doing the opposite of what it
    /// says on a screen somebody was not looking at.
    /// </remarks>
    [Fact]
    public void ATriggerThatStopsASequenceStillStopsIt()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Stop a sequence");
        screen.Editor.PressChoice("Sequence 1");
        screen.Run(2);

        screen.Editor.PressWhen(1);
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Trigger changed = System(screen).Triggers[1];
        Assert.True(changed.Stops);
        Assert.Equal(1, changed.Runs);
        Assert.Equal("stops Sequence 1", screen.Editor.Triggers().Entries[1].Status);
    }

    [Fact]
    public void PressingARepeatAsksHowOften()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Housekeeping");
        screen.Editor.PressStart("Every…");
        screen.Editor.PressChoice("steady - every 100 ticks");
        screen.Editor.PressChoice("Run a new sequence");
        screen.Editor.Back();
        screen.Run(2);

        screen.Editor.PressWhen(0);
        screen.Editor.PressChoice("slow - every 300 ticks");
        screen.Run(2);

        Assert.Equal(300, Assert.IsType<EveryTrigger>(System(screen).Triggers[0]).IntervalTicks);
        Assert.Equal("Every 300 ticks", screen.Editor.Triggers().Entries[0].Summary);
    }

    /// <summary>
    /// The other half of the row: what the trigger does, changed without remaking it.
    /// </summary>
    /// <remarks>
    /// The same list the wizard asks with, so there is one way to say what a trigger is for. The
    /// trigger itself is kept rather than replaced, because what starts it has not changed - and the
    /// toolbar slot the construct holds names that trigger.
    /// </remarks>
    [Fact]
    public void ATriggerCanBePointedAtSomethingElse()
    {
        var screen = WithOne();

        Trigger was = System(screen).Triggers[0];

        screen.Editor.PressDoes(0);
        screen.Editor.PressChoice("Do something");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Same(was, System(screen).Triggers[0]);
        Assert.Equal("close Airlock Outer Door", screen.Editor.Triggers().Entries[0].Status);

        // What it used to run stands on its own, so it stays behind rather than going with the
        // pointing.
        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
        Assert.Single(screen.Editor.Rows().Entries);
    }

    /// <summary>
    /// Pointing away from a sequence nobody named takes that sequence with it.
    /// </summary>
    /// <remarks>
    /// It existed only to be the one thing that trigger did, so nothing is left that could ever run
    /// it - and it has no row of its own to be deleted from. The way back is the arrows, like every
    /// other trigger act.
    /// </remarks>
    [Fact]
    public void PointingAwayFromAPlainThingTakesItWithIt()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Do something");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Program.Sequences.Count);

        screen.Editor.PressDoes(1);
        screen.Editor.PressChoice("Run an existing sequence");
        screen.Editor.PressChoice("Sequence 1");
        screen.Run(2);

        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Equal("runs Sequence 1", screen.Editor.Triggers().Entries[1].Status);

        // And it comes back with what it did, because nothing else was holding it.
        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
        Assert.Equal("close Airlock Outer Door", screen.Editor.Triggers().Entries[1].Status);
    }

    /// <summary>
    /// Every change to a trigger walks back, from the screen it was made on.
    /// </summary>
    /// <remarks>
    /// A system's way back rather than a sequence's, because a trigger belongs to the system. It is
    /// the same pair of arrows in the same order, so nobody has to learn a second way to take
    /// something back.
    /// </remarks>
    [Fact]
    public void ChangingATriggerWalksBack()
    {
        var screen = WithOne();

        // Nothing done to this system yet, so both arrows are quiet and holding their place.
        Assert.Equal(0, screen.Editor.Steps().UndoCommand);
        Assert.Equal(0, screen.Editor.Steps().RedoCommand);

        screen.Editor.PressWhen(0);
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("changing when it runs", screen.Editor.Steps().UndoText);

        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        var back = Assert.IsType<RequiredTrigger>(System(screen).Triggers[0]);
        Assert.Equal("pressed-1", back.EventId);
        Assert.Equal("When Airlock Button is pressed", screen.Editor.Triggers().Entries[0].Summary);

        // And forward again, into the same change rather than a rebuilt one that reads the same.
        Assert.Equal("changing when it runs", screen.Editor.Steps().RedoText);
        screen.Editor.Invoke(screen.Editor.Steps().RedoCommand);
        screen.Run(2);

        Assert.Equal("closed", Assert.IsType<WhenTrigger>(System(screen).Triggers[0]).EventId);
    }

    /// <summary>
    /// Deleting a trigger walks back, and brings the sequence it took with it.
    /// </summary>
    /// <remarks>
    /// Which is what makes deleting one safe to do without asking first. The steps of a plain
    /// trigger's sequence are nowhere else on any screen, so a bin with no way back would be the
    /// one control in the editor that can lose work silently.
    /// </remarks>
    [Fact]
    public void DeletingATriggerWalksBack()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Do something");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Program.Sequences.Count);

        screen.Editor.Invoke(screen.Editor.Triggers().Entries[1].DeleteCommand);
        screen.Run(2);

        Assert.Single(screen.Editor.Triggers().Entries);
        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Equal("deleting a trigger", screen.Editor.Steps().UndoText);

        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        // Back where it was, with what it did, and in the place it had among the others.
        Assert.Equal(2, screen.Editor.Triggers().Entries.Count);
        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
        Assert.Equal("close Airlock Outer Door", screen.Editor.Triggers().Entries[1].Status);
    }

    /// <summary>
    /// A change made outside the editor is not walked back over.
    /// </summary>
    /// <remarks>
    /// The host takes a block out of a trigger when somebody clears that button in the game, which
    /// is a decision rather than a mistake. Undo putting the old trigger back would both undo what
    /// they meant and claim a toolbar slot they had just taken away - so the way back stops there
    /// instead, quietly, and nothing fails.
    /// </remarks>
    [Fact]
    public void AChangeMadeOutsideIsNotWalkedBackOver()
    {
        var screen = WithOne();

        screen.Editor.PressWhen(0);
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.NotEqual(0, screen.Editor.Steps().UndoCommand);

        // What the host does when the button somebody named is cleared or pointed elsewhere.
        var trigger = (MomentTrigger)System(screen).Triggers[0];
        trigger.Sources.Clear();
        screen.Run(2);

        Assert.Equal(0, screen.Editor.Steps().UndoCommand);
        Assert.Equal("", screen.Editor.Steps().UndoText);

        // And the trigger is left as the game left it.
        Assert.Same(trigger, System(screen).Triggers[0]);
        Assert.Empty(trigger.Sources);
    }

    /// <summary>
    /// And the sequence's own screen says nothing about any of it.
    /// </summary>
    /// <remarks>
    /// Not even the one trigger it happens to have. A sequence may be started from several places,
    /// so a line on this screen would either show one of them or grow into the list its system
    /// already keeps.
    /// </remarks>
    [Fact]
    public void TheSequenceScreenSaysNothingAboutWhatStartsIt()
    {
        var screen = WithOne();

        screen.Editor.Invoke(screen.Editor.Rows().Entries[0].Command);
        screen.Run(2);

        Assert.NotNull(screen.Editor.Sentence());
        Assert.False(screen.Shows("Airlock Button"));
        Assert.False(screen.Shows("is pressed"));
        Assert.DoesNotContain("When", screen.Words);
    }
}
