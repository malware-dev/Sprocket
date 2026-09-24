using Sprocket.Host.SpaceEngineers.Picking;
using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

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
        Assert.Equal("When Airlock Inner Door finishes closing", screen.Editor.Says(0));

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
        screen.Editor.PressChoice("stop");
        screen.Editor.PressChoice("Sequence 1");
        screen.Editor.PressPrimary();
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
        Assert.Equal("then stop Sequence 1", screen.Editor.Doing(1));
    }

    [Fact]
    public void PressingARepeatAsksHowOften()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Housekeeping");
        screen.Editor.PressStart("Every…");
        screen.Editor.PressChoice("steady - every 100 ticks");
        screen.Editor.PressPrimary();
        screen.Editor.Back();
        screen.Run(2);

        Trigger was = System(screen).Triggers[0];

        screen.Editor.PressWhen(0);
        screen.Editor.PressChoice("slow - every 300 ticks");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Same(was, System(screen).Triggers[0]);
        Assert.Equal(1, was.Runs);

        Assert.Equal(300, Assert.IsType<EveryTrigger>(System(screen).Triggers[0]).IntervalTicks);
        Assert.Equal("Every 300 ticks", screen.Editor.Says(0));
    }

    /// <summary>
    /// The other half of the row: what the trigger does, changed without remaking it.
    /// </summary>
    /// <remarks>
    /// The same picker the trigger was made in, so there is one way to say what a trigger is for.
    /// The trigger itself is kept rather than replaced, because what starts it has not changed - and
    /// the toolbar slot the construct holds names that trigger.
    /// </remarks>
    [Fact]
    public void ATriggerCanBePointedAtSomethingElse()
    {
        var screen = WithOne();

        Trigger was = System(screen).Triggers[0];

        screen.Editor.PressDoes(0);
        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Same(was, System(screen).Triggers[0]);
        Assert.Equal("then close Airlock Outer Door", screen.Editor.Doing(0));

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
        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Program.Sequences.Count);

        screen.Editor.PressWhen(1);
        screen.Editor.PressChoice("Sequence 1");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Equal("then run Sequence 1", screen.Editor.Doing(1));

        // One step back puts the plain sequence back with what the trigger did.
        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
        Assert.Equal("then close Airlock Outer Door", screen.Editor.Doing(1));
    }

    /// <summary>
    /// Pressing what a trigger runs opens the picker it was made in, on the sequence it runs.
    /// </summary>
    /// <remarks>
    /// Changing which sequence a button runs is what somebody pressing those words almost always
    /// means, and the sequences are in the column beside what starts it, with no question first.
    /// </remarks>
    [Fact]
    public void PressingWhatItRunsOpensThePickerOnIt()
    {
        var screen = WithOne();

        screen.Editor.PressStart("New sequence");
        screen.Editor.Name("Seal up");
        screen.Editor.Back();
        screen.Run(2);

        Trigger was = System(screen).Triggers[0];
        screen.Editor.PressDoes(0);

        Assert.Equal("When something happens", screen.Editor.Screen.Title());
        Assert.Equal("", screen.Editor.Screen.Question);

        var does = screen.Editor.Component<PickComponent>().Stages[2];
        Assert.Contains(does.Entries, e => e.Text == "Sequence 1" && e.IsChosen);

        screen.Editor.PressChoice("Seal up");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("then run Seal up", screen.Editor.Doing(0));
        Assert.Same(was, System(screen).Triggers[0]);
    }

    /// <summary>
    /// A single action is chosen on the page after the trigger's, in the same pick, and going back
    /// finds the trigger's page as it was left.
    /// </summary>
    /// <remarks>
    /// Nothing is kept until the action is done, and then what starts it and what it does are one
    /// step back.
    /// </remarks>
    [Fact]
    public void ASingleActionIsThePageAfter()
    {
        var screen = WithOne();
        Trigger was = System(screen).Triggers[0];

        screen.Editor.PressWhen(0);
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressChoice("A single action…");
        screen.Run(2);

        Assert.Equal("Do something", screen.Editor.Screen.Title());
        Assert.Equal(3, screen.Editor.Component<PickComponent>().Page);

        screen.Editor.Back();
        screen.Run(2);

        Assert.Equal("When something happens", screen.Editor.Screen.Title());
        var pick = screen.Editor.Component<PickComponent>();
        Assert.Equal(0, pick.Page);
        Assert.Equal(3, pick.Stages.Count);
        Assert.Contains(pick.Stages[2].Entries, e => e.Text == "A single action…" && e.IsChosen);
        Assert.Same(was, System(screen).Triggers[0]);

        // Nothing to finish here while a single action is chosen: choosing it again goes on.
        Assert.False(screen.Editor.Screen.HasPrimary);
        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("When Airlock Inner Door finishes closing", screen.Editor.Says(0));
        Assert.Equal("then close Airlock Outer Door", screen.Editor.Doing(0));

        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        Assert.Equal("When Airlock Button is pressed", screen.Editor.Says(0));
        Assert.Equal("then run Sequence 1", screen.Editor.Doing(0));
    }

    /// <summary>A trigger being made is not made where its single action is never chosen.</summary>
    [Fact]
    public void NothingIsMadeUntilTheActionIsChosen()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressChoice("A single action…");
        screen.Editor.Back();
        screen.Editor.Back();
        screen.Editor.PressChoice("Discard them");
        screen.Run(2);

        Assert.Single(System(screen).Triggers);
    }

    /// <summary>
    /// Going on from a trigger that already does one thing opens that thing, and finishing changes
    /// it where it is.
    /// </summary>
    [Fact]
    public void GoingOnOpensTheActionItDoes()
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

        var sequences = screen.Editor.Program.Sequences.Count;

        screen.Editor.PressWhen(1);
        screen.Editor.PressChoice("A single action…");
        screen.Run(2);

        var pick = screen.Editor.Component<PickComponent>();
        Assert.Contains(pick.Stages[pick.Page].Entries, e => e.Text == "Airlock Outer Door" && e.IsChosen);

        screen.Editor.PressChoice("Open");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("then open Airlock Outer Door", screen.Editor.Doing(1));
        Assert.Equal(sequences, screen.Editor.Program.Sequences.Count);
    }

    /// <summary>
    /// Going back to the trigger's page and on again finds the action's page as it was left, and the
    /// way on is there only while something chosen has a page after it.
    /// </summary>
    [Fact]
    public void PagingBackAndOnKeepsTheActionsPage()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        Assert.Equal(0, screen.Editor.Component<PickComponent>().Onward);

        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.Back();
        screen.Run(2);

        var pick = screen.Editor.Component<PickComponent>();
        Assert.Equal("When something happens", screen.Editor.Screen.Title());
        Assert.NotEqual(0, pick.Onward);

        screen.Editor.Invoke(pick.Onward);
        screen.Run(2);

        pick = screen.Editor.Component<PickComponent>();
        Assert.Equal("Do something", screen.Editor.Screen.Title());
        Assert.Contains(pick.Stages[pick.Page].Entries, e => e.Text == "Airlock Outer Door" && e.IsChosen);
    }

    /// <summary>Choosing something else for the trigger to do lets the action's page go.</summary>
    [Fact]
    public void ChoosingSomethingElseLetsTheActionsPageGo()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.Back();
        screen.Editor.PressChoice("A new sequence");
        screen.Run(2);

        Assert.Equal(0, screen.Editor.Component<PickComponent>().Onward);

        screen.Editor.PressChoice("A single action…");
        screen.Run(2);

        var pick = screen.Editor.Component<PickComponent>();
        Assert.DoesNotContain(pick.Stages[pick.Page].Entries, e => e.IsChosen);
    }

    /// <summary>
    /// A change to what starts a trigger and to what it does, made on one Done, is one step back.
    /// </summary>
    [Fact]
    public void ChangingBothWalksBackAsOne()
    {
        var screen = WithOne();

        screen.Editor.PressStart("New sequence");
        screen.Editor.Name("Seal up");
        screen.Editor.Back();
        screen.Run(2);

        screen.Editor.PressWhen(0);
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressChoice("Seal up");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("When Airlock Inner Door finishes closing", screen.Editor.Says(0));
        Assert.Equal("then run Seal up", screen.Editor.Doing(0));

        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        Assert.Equal("When Airlock Button is pressed", screen.Editor.Says(0));
        Assert.Equal("then run Sequence 1", screen.Editor.Doing(0));

        screen.Editor.Invoke(screen.Editor.Steps().RedoCommand);
        screen.Run(2);

        Assert.Equal("When Airlock Inner Door finishes closing", screen.Editor.Says(0));
        Assert.Equal("then run Seal up", screen.Editor.Doing(0));
    }

    /// <summary>
    /// And pressing the one thing a trigger does opens that, in the picker it was chosen in.
    /// </summary>
    /// <remarks>
    /// The act belongs to a sequence nobody named, which has no screen of its own - so this is the
    /// only way to change it, and the way back is the system's.
    /// </remarks>
    [Fact]
    public void PressingWhatItDoesOpensThatAct()
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

        screen.Editor.PressDoes(1);
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("then close Airlock Inner Door", screen.Editor.Doing(1));

        // And it walks back from here, because the sequence it changed has nowhere else to offer it.
        Assert.Equal("changing a step", screen.Editor.Steps().UndoText);

        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        Assert.Equal("then close Airlock Outer Door", screen.Editor.Doing(1));
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
        Assert.Equal("pressed_1", back.EventId);
        Assert.Equal("When Airlock Button is pressed", screen.Editor.Says(0));

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
        screen.Editor.PressChoice("A single action…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Program.Sequences.Count);

        screen.Editor.Invoke(screen.Editor.Triggers()[1].DeleteCommand);
        screen.Run(2);

        Assert.Single(screen.Editor.Triggers());
        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Equal("deleting a trigger", screen.Editor.Steps().UndoText);

        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        // Back where it was, with what it did, and in the place it had among the others.
        Assert.Equal(2, screen.Editor.Triggers().Count);
        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
        Assert.Equal("then close Airlock Outer Door", screen.Editor.Doing(1));
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
    /// A button that already starts something is not quietly taken over.
    /// </summary>
    /// <remarks>
    /// A toolbar slot holds one thing, so two triggers on the same press are two claims on it and
    /// one of them stops working. Told before it happens rather than found afterwards on a row that
    /// has gone quiet.
    /// </remarks>
    [Fact]
    public void TakingAButtonThatIsAlreadyUsedAsksFirst()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("Airlock Button is already in use", screen.Editor.Screen.Question);

        // And leaving it alone leaves everything as it was: no second trigger, and no sequence made
        // for one.
        screen.Editor.PressChoice("Leave it alone");
        screen.Run(2);

        Assert.Single(System(screen).Triggers);
        Assert.Single(screen.Editor.Program.Sequences);
    }

    /// <summary>Saying so anyway goes through, which is what a warning is rather than a refusal.</summary>
    [Fact]
    public void TakingItAnywayIsAllowed()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Take it over");
        screen.Run(2);

        Assert.Equal(2, System(screen).Triggers.Count);
    }

    /// <summary>
    /// Changing a trigger onto a button that is already used asks the same thing.
    /// </summary>
    /// <remarks>
    /// The two ways to arrive at the same clash, and one of them was the way it was found: a trigger
    /// pointed at a button somebody else's trigger already held, taken without a word.
    /// </remarks>
    [Fact]
    public void ChangingOntoAUsedButtonAsksFirst()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Editor.Back();
        screen.Run(2);

        screen.Editor.PressWhen(1);
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("Airlock Button is already in use", screen.Editor.Screen.Question);

        screen.Editor.PressChoice("Leave it alone");
        screen.Run(2);

        // Untouched: it still waits for the door it was given.
        Assert.Equal("When Airlock Inner Door finishes closing", screen.Editor.Says(1));
    }

    /// <summary>
    /// Taking the button over leaves the trigger that had it without one, and walks back.
    /// </summary>
    /// <remarks>
    /// The answer decides which trigger keeps the button, because the construct cannot hold both and
    /// arranges neither when asked to: the slot ends up empty and neither sequence starts. So the
    /// one that lost it is left in the state anybody can mend, and one press of the way back puts
    /// both of them where they were, since it was one answer to one question.
    /// </remarks>
    [Fact]
    public void TakingAButtonOverEmptiesTheTriggerThatHadIt()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Editor.Back();
        screen.Run(2);

        screen.Editor.PressWhen(1);
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Take it over");
        screen.Run(2);

        var had = (MomentTrigger)System(screen).Triggers[0];

        Assert.Empty(had.Sources);
        Assert.Equal("When Airlock Button is pressed", screen.Editor.Says(1));
        Assert.Equal("taking a button over", screen.Editor.Steps().UndoText);

        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        Assert.Equal("When Airlock Button is pressed", screen.Editor.Says(0));
        Assert.Equal("When Airlock Inner Door finishes closing", screen.Editor.Says(1));
    }

    /// <summary>
    /// Settling on what a trigger already had is not a clash with itself.
    /// </summary>
    /// <remarks>
    /// Going to the picker and coming back without changing your mind must leave nothing behind and
    /// ask nothing, or every second visit to a trigger is a warning about it.
    /// </remarks>
    [Fact]
    public void KeepingWhatItAlreadyHadAsksNothing()
    {
        var screen = WithOne();

        screen.Editor.PressWhen(0);
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("", screen.Editor.Screen.Question);
        Assert.Equal("When Airlock Button is pressed", screen.Editor.Says(0));
    }

    /// <summary>
    /// A trigger the host has emptied can still be told what should start it.
    /// </summary>
    /// <remarks>
    /// Somebody clears that button in the game and the host takes the block out of the trigger,
    /// which leaves a row saying nothing can start this. Its parts are still there and still open
    /// the picker, because a row that reports a fault and offers no way to mend it is a dead end.
    /// </remarks>
    [Fact]
    public void ATriggerWithNothingLeftCanBePointedSomewhere()
    {
        var screen = WithOne();

        var emptied = (MomentTrigger)System(screen).Triggers[0];
        emptied.Sources.Clear();
        screen.Run(120);

        screen.Editor.PressWhen(0);
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("When Airlock Inner Door finishes closing", screen.Editor.Says(0));
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
