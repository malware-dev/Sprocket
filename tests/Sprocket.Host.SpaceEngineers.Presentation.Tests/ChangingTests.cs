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
        Assert.Equal("then stop Sequence 1", screen.Editor.Doing(1));
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
        Assert.Equal("Every 300 ticks", screen.Editor.Says(0));
    }

    /// <summary>
    /// The other half of the row: what the trigger does, changed without remaking it.
    /// </summary>
    /// <remarks>
    /// The same list the wizard asks with, so there is one way to say what a trigger is for. The
    /// trigger itself is kept rather than replaced, because what starts it has not changed - and the
    /// toolbar slot the construct holds names that trigger.
    /// <para>
    /// Asked for by the cross, which is what takes back the answer that is there. Pressing the words
    /// themselves changes what is in them, which is the commoner thing to want.
    /// </para>
    /// </remarks>
    [Fact]
    public void ATriggerCanBePointedAtSomethingElse()
    {
        var screen = WithOne();

        Trigger was = System(screen).Triggers[0];

        // The cross empties it, and an empty slot is what asks the whole question.
        screen.Editor.PressClear(0);
        screen.Editor.PressDoes(0);
        screen.Editor.PressChoice("Do something");
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
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Do something");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Program.Sequences.Count);

        screen.Editor.PressClear(1);
        screen.Editor.PressDoes(1);
        screen.Editor.PressChoice("Run an existing sequence");
        screen.Editor.PressChoice("Sequence 1");
        screen.Run(2);

        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Equal("then run Sequence 1", screen.Editor.Doing(1));

        // Two acts, so two steps back: the pointing, and then the emptying that took the plain
        // sequence with it.
        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        Assert.Equal("…", screen.Editor.Doing(1));

        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
        Assert.Equal("then close Airlock Outer Door", screen.Editor.Doing(1));
    }

    /// <summary>
    /// Pressing what a trigger runs asks which one, and nothing else.
    /// </summary>
    /// <remarks>
    /// Changing which sequence a button runs is what somebody pressing those words almost always
    /// means. Being asked what a trigger is for first - run a new one, run an existing one, stop
    /// one, do something - is three answers where one would do, every time.
    /// </remarks>
    [Fact]
    public void PressingWhatItRunsAsksWhichOne()
    {
        var screen = WithOne();

        screen.Editor.PressStart("New sequence");
        screen.Editor.Name("Seal up");
        screen.Editor.Back();
        screen.Run(2);

        screen.Editor.PressDoes(0);

        // Straight to the sequences, with no question about what the trigger is for.
        Assert.Equal("Run which sequence?", screen.Editor.Screen.Title());
        Assert.Equal("", screen.Editor.Screen.Question);

        screen.Editor.PressChoice("Seal up");
        screen.Run(2);

        Assert.Equal("then run Seal up", screen.Editor.Doing(0));
        Assert.Single(System(screen).Triggers);
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
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Do something");
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
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Do something");
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
    /// The cross empties what a trigger does, and says so by being empty.
    /// </summary>
    /// <remarks>
    /// What it does rather than what it asks: a mark that opened a question instead would be saying
    /// one thing and doing another. An empty slot is the editor's way of saying nobody has answered
    /// yet, everywhere else in a sentence, and pressing one is how it gets answered.
    /// </remarks>
    [Fact]
    public void TheCrossEmptiesWhatItDoes()
    {
        var screen = WithOne();

        screen.Editor.PressClear(0);
        screen.Run(2);

        Assert.Equal(0, System(screen).Triggers[0].Runs);
        Assert.True(screen.Editor.Triggers()[0].Does[0].IsEmpty);

        // The same gap an unanswered part of any sentence reads as.
        Assert.Equal("…", screen.Editor.Doing(0));

        // Still a trigger, still saying when it happens, and still on the list.
        Assert.Equal("When Airlock Button is pressed", screen.Editor.Says(0));
        Assert.Single(System(screen).Triggers);

        // And the empty part is what asks for an answer.
        screen.Editor.PressDoes(0);
        Assert.Equal("What should it do?", screen.Editor.Screen.Question);
    }

    /// <summary>
    /// Emptying it takes the one thing it did with it, and walks back.
    /// </summary>
    /// <remarks>
    /// The same rule as pointing it somewhere else: a sequence nobody named existed only to be the
    /// thing this trigger did, so nothing is left that could ever run it. The arrows put both back.
    /// </remarks>
    [Fact]
    public void EmptyingItTakesThePlainThingWithIt()
    {
        var screen = WithOne();

        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Do something");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("Close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Program.Sequences.Count);

        screen.Editor.PressClear(1);
        screen.Run(2);

        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Equal("…", screen.Editor.Doing(1));

        screen.Editor.Invoke(screen.Editor.Steps().UndoCommand);
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
        Assert.Equal("then close Airlock Outer Door", screen.Editor.Doing(1));
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
        screen.Editor.PressChoice("Run a new sequence");
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
        screen.Editor.PressChoice("Run a new sequence");
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
        screen.Editor.PressChoice("Run a new sequence");
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
