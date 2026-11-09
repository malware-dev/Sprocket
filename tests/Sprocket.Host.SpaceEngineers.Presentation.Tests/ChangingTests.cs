using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
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
        screen.Editor.NewButtonSystem("Airlock");
        screen.Editor.BackKeeping();
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

        var trigger = Assert.IsType<SignalTrigger>(System(screen).Triggers[0]);
        Assert.Equal("finishes_closing", trigger.EventId);
        Assert.Equal("When block slot 1 finishes closing", screen.Editor.Says(0));

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

        screen.Editor.PressStart("when");
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
        screen.Editor.PressStart("every");
        screen.Editor.TimerRate().Give("100", true);
        screen.Editor.PressPrimary();
        screen.Editor.BackKeeping();
        screen.Run(2);

        Trigger was = System(screen).Triggers[0];
        Assert.Equal(100, Assert.IsType<EveryTrigger>(was).IntervalTicks);

        screen.Editor.PressWhen(0);
        screen.Run(2);
        var rate = screen.Editor.RateLengths();
        rate.Give("300", true);
        rate.Box.Submit!.Execute();
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
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Same(was, System(screen).Triggers[0]);
        Assert.Equal("then close block slot 1", screen.Editor.Doing(0));
        Assert.NotNull(was.Acts);

        // What it used to run stands on its own, so it stays behind; the act is the trigger's own and
        // makes no sequence.
        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Single(screen.Editor.Rows());
    }

    /// <summary>
    /// Pointing a trigger that acts itself at a sequence instead walks back to the act it had.
    /// </summary>
    /// <remarks>
    /// The act has no row of its own anywhere else, so the way back is the only place it can be had again.
    /// </remarks>
    [Fact]
    public void PointingAwayFromAnActWalksBack()
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

        Assert.Single(screen.Editor.Program.Sequences);

        screen.Editor.PressWhen(1);
        screen.Editor.PressChoice("Sequence 1");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Equal("then run Sequence 1", screen.Editor.Doing(1));

        // One step back puts the act back as what the trigger does.
        screen.Editor.Steps().Undo!.Execute();
        screen.Run(2);

        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Equal("then close block slot 2", screen.Editor.Doing(1));
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

        screen.Editor.PressStart("sequence");
        screen.Editor.Name("Seal up");
        screen.Editor.BackKeeping();
        screen.Run(2);

        Trigger was = System(screen).Triggers[0];
        screen.Editor.PressDoes(0);

        Assert.Equal("signal slot 1", screen.Editor.Screen.Title());
        Assert.Equal("", screen.Editor.Screen.Question);

        var does = screen.Editor.Pick().Stages[2];
        Assert.Contains(does.Entries(), e => e.Text == "Sequence 1" && e.IsChosen);

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
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressChoice("A task on blocks…");
        screen.Run(2);

        Assert.Equal("block slot 2", screen.Editor.Screen.Title());
        Assert.Equal(3, screen.Editor.Pick().Page);

        screen.Editor.BackKeeping();
        screen.Run(2);

        Assert.Equal("block slot 1", screen.Editor.Screen.Title());
        var pick = screen.Editor.Pick();
        Assert.Equal(0, pick.Page);
        Assert.Equal(3, pick.Stages.Count);
        Assert.Contains(pick.Stages[2].Entries(), e => e.Text == "A task on blocks…" && e.IsChosen);
        Assert.Same(was, System(screen).Triggers[0]);

        // Nothing to finish here while a single action is chosen: choosing it again goes on.
        Assert.False(screen.Editor.Screen.HasPrimary);
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressChoice("close");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("When block slot 1 finishes closing", screen.Editor.Says(0));
        Assert.Equal("then close block slot 2", screen.Editor.Doing(0));

        screen.Editor.Steps().Undo!.Execute();
        screen.Run(2);

        Assert.Equal("When signal slot 1 button 1 is pressed", screen.Editor.Says(0));
        Assert.Equal("then run Sequence 1", screen.Editor.Doing(0));
    }

    /// <summary>A trigger being made is not made where its single action is never chosen.</summary>
    [Fact]
    public void NothingIsMadeUntilTheActionIsChosen()
    {
        var screen = WithOne();

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressChoice("A task on blocks…");
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

        var sequences = screen.Editor.Program.Sequences.Count;

        screen.Editor.PressWhen(1);
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressChoice("A task on blocks…");
        screen.Run(2);

        var pick = screen.Editor.Pick();
        Assert.Contains(pick.Stages[pick.Page].Entries(), e => e.Text == "Airlock Outer Door" && e.IsChosen);

        screen.Editor.PressChoice("open");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("then open block slot 2", screen.Editor.Doing(1));
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

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        Assert.Null(screen.Editor.Pick().Onward);

        screen.Editor.DeveloperMode = true;

        screen.Run(1);

        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.BackKeeping();
        screen.Run(2);

        var pick = screen.Editor.Pick();
        Assert.Equal("block slot 1", screen.Editor.Screen.Title());
        Assert.NotNull(pick.Onward);

        pick.Onward!.Execute();
        screen.Run(2);

        pick = screen.Editor.Pick();
        Assert.Equal("block slot 2", screen.Editor.Screen.Title());
        Assert.Contains(pick.Stages[pick.Page].Entries(), e => e.Text == "Airlock Outer Door" && e.IsChosen);
    }

    /// <summary>Choosing something else for the trigger to do lets the action's page go.</summary>
    [Fact]
    public void ChoosingSomethingElseLetsTheActionsPageGo()
    {
        var screen = WithOne();

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.DeveloperMode = true;
        screen.Run(1);
        screen.Editor.PressChoice("A task on blocks…");
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.BackKeeping();
        screen.Editor.PressChoice("A new sequence");
        screen.Run(2);

        Assert.Null(screen.Editor.Pick().Onward);

        screen.Editor.DeveloperMode = true;

        screen.Run(1);

        screen.Editor.PressChoice("A task on blocks…");
        screen.Run(2);

        var pick = screen.Editor.Pick();
        Assert.DoesNotContain(pick.Stages[pick.Page].Entries(), e => e.IsChosen);
    }

    /// <summary>
    /// A change to what starts a trigger and to what it does, made on one Done, is one step back.
    /// </summary>
    [Fact]
    public void ChangingBothWalksBackAsOne()
    {
        var screen = WithOne();

        screen.Editor.PressStart("sequence");
        screen.Editor.Name("Seal up");
        screen.Editor.BackKeeping();
        screen.Run(2);

        screen.Editor.PressWhen(0);
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressChoice("Seal up");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("When block slot 1 finishes closing", screen.Editor.Says(0));
        Assert.Equal("then run Seal up", screen.Editor.Doing(0));

        screen.Editor.Steps().Undo!.Execute();
        screen.Run(2);

        Assert.Equal("When signal slot 1 button 1 is pressed", screen.Editor.Says(0));
        Assert.Equal("then run Sequence 1", screen.Editor.Doing(0));

        screen.Editor.Steps().Redo!.Execute();
        screen.Run(2);

        Assert.Equal("When block slot 1 finishes closing", screen.Editor.Says(0));
        Assert.Equal("then run Seal up", screen.Editor.Doing(0));
    }

    /// <summary>
    /// And pressing the one thing a trigger does opens that, in the picker it was chosen in.
    /// </summary>
    /// <remarks>
    /// The act is the trigger's own, with no screen of its own - so this is the only way to change it,
    /// and the way back is the system's.
    /// </remarks>
    [Fact]
    public void PressingWhatItDoesOpensThatAct()
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

        screen.Editor.PressDoes(1);
        screen.Editor.PressBlock("Airlock Outer Door");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("then close block slot 1", screen.Editor.Doing(1));

        // And it walks back from here, because the act it changed has nowhere else to offer it.
        Assert.Equal(Filled.With(screen.Editor.Texts.ChangeTarget, "thing", SentenceText.Short(screen.Editor.Says(1))), screen.Editor.Steps().UndoText);

        screen.Editor.Steps().Undo!.Execute();
        screen.Run(2);

        Assert.Equal("then close block slot 2", screen.Editor.Doing(1));
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

        // Only the trigger it was made with so far, and nothing to walk forward.
        Assert.Equal(Filled.With(screen.Editor.Texts.ChangeAddTrigger, "thing", SentenceText.Short(screen.Editor.Says(0))), screen.Editor.Steps().UndoText);
        Assert.Null(screen.Editor.Steps().Redo);

        screen.Editor.PressWhen(0);
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Run(2);

        var changed = Filled.With(screen.Editor.Texts.ChangeWhen, "thing", SentenceText.Short(screen.Editor.Says(0)));
        Assert.Equal(changed, screen.Editor.Steps().UndoText);

        screen.Editor.Steps().Undo!.Execute();
        screen.Run(2);

        var back = Assert.IsType<SignalTrigger>(System(screen).Triggers[0]);
        Assert.Equal(1, Assert.Single(Pressed.Where(screen.Editor.Program, System(screen), back.Part)!).Slot);
        Assert.Equal("When signal slot 1 button 1 is pressed", screen.Editor.Says(0));

        // And forward again, into the same change rather than a rebuilt one that reads the same.
        Assert.Equal(changed, screen.Editor.Steps().RedoText);
        screen.Editor.Steps().Redo!.Execute();
        screen.Run(2);

        Assert.Equal("finishes_closing", Assert.IsType<SignalTrigger>(System(screen).Triggers[0]).EventId);
    }

    /// <summary>
    /// Deleting a trigger walks back, with the act it did.
    /// </summary>
    /// <remarks>
    /// Which is what makes deleting one safe to do without asking first. A trigger's own act is
    /// nowhere else on any screen, so a bin with no way back would be the one control in the editor
    /// that can lose work silently.
    /// </remarks>
    [Fact]
    public void DeletingATriggerWalksBack()
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

        var deleted = Filled.With(screen.Editor.Texts.ChangeForgetTrigger, "thing", SentenceText.Short(screen.Editor.Says(1)));
        screen.Editor.Triggers()[1].Deletes!.Execute();
        screen.Run(2);

        Assert.Single(screen.Editor.Triggers());
        Assert.Equal(deleted, screen.Editor.Steps().UndoText);

        screen.Editor.Steps().Undo!.Execute();
        screen.Run(2);

        // Back where it was, with what it did, and in the place it had among the others.
        Assert.Equal(2, screen.Editor.Triggers().Count);
        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Equal("then close block slot 2", screen.Editor.Doing(1));
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

        Assert.NotNull(screen.Editor.Steps().Undo);

        // What the host does when the button somebody named is cleared or pointed elsewhere.
        var trigger = (MomentTrigger)System(screen).Triggers[0];
        trigger.Sources.Clear();
        screen.Run(2);

        // The way back goes on from the step before it.
        Assert.Equal(Filled.With(screen.Editor.Texts.ChangeRenameSystem, "was", "Logic system 1", "now", "Airlock"), screen.Editor.Steps().UndoText);

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
        var parts = System(screen).Parts.Count;

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("button 1 is pressed");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("Airlock Button already starts Sequence 1 on Installation 1 of Airlock.", screen.Editor.Question());

        // And leaving it alone leaves everything as it was: no second trigger, and no sequence made
        // for one.
        screen.Editor.PressChoice("Leave it alone");
        screen.Run(2);

        Assert.Single(System(screen).Triggers);
        Assert.Single(screen.Editor.Program.Sequences);
        Assert.Equal(parts, System(screen).Parts.Count);
    }

    /// <summary>
    /// Saying so anyway goes through, which is what a warning is rather than a refusal; in the same installation the trigger
    /// that had the button goes, and the one taking it takes its slot.
    /// </summary>
    [Fact]
    public void TakingItAnywayIsAllowed()
    {
        var screen = WithOne();

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("button 1 is pressed");
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Take it over");
        screen.Run(2);

        Assert.Equal(2, screen.Editor.Program.Sequences.Count);
        var took = Assert.IsType<SignalTrigger>(Assert.Single(System(screen).Triggers));
        Assert.True(took.IsAbout(screen.Editor.Program.Sequences[1]));
        Assert.Equal("signal slot 1", took.Part);
        Assert.Single(System(screen).Parts, part => part.IsSignal);
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

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Editor.BackKeeping();
        screen.Run(2);

        screen.Editor.PressWhen(1);
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("button 1 is pressed");
        screen.Editor.PressPrimary();
        screen.Run(2);

        Assert.Equal("Airlock Button already starts Sequence 1 on Installation 1 of Airlock.", screen.Editor.Question());

        screen.Editor.PressChoice("Leave it alone");
        screen.Run(2);

        // Untouched: it still waits for the door it was given, and no slot was made for the button.
        Assert.Equal("When block slot 1 finishes closing", screen.Editor.Says(1));
        Assert.DoesNotContain(System(screen).Parts, part => part.IsSignal && part.Id != "signal slot 1");
    }

    /// <summary>Changing a trigger onto a button another trigger has is one step back, which brings the other back with it.</summary>
    [Fact]
    public void ChangingOntoAButtonIsOneStepBack()
    {
        var screen = WithOne();

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Editor.BackKeeping();
        screen.Run(2);

        screen.Editor.PressWhen(1);
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("button 1 is pressed");
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Take it over");
        screen.Run(2);

        Assert.IsType<LogicSystemPage>(screen.Editor.Current);
        Assert.Single(System(screen).Triggers);

        screen.Editor.Screen.Undo!.Execute();
        screen.Run(2);

        Assert.Equal(2, System(screen).Triggers.Count);
        Assert.Equal("When signal slot 1 button 1 is pressed", screen.Editor.Says(0));
        Assert.Equal("When block slot 1 finishes closing", screen.Editor.Says(1));
    }

    /// <summary>
    /// Taking the button over takes out the trigger in the same installation that had it, and walks back.
    /// </summary>
    /// <remarks>
    /// The answer decides which trigger keeps the button, because the construct cannot hold both and
    /// arranges neither when asked to: the slot ends up empty and neither sequence starts. A logic
    /// system's trigger is a variable holding its button, so the one that lost it goes, and one press
    /// of the way back puts both of them where they were, since it was one answer to one question.
    /// </remarks>
    [Fact]
    public void TakingAButtonOverTakesOutTheTriggerThatHadIt()
    {
        var screen = WithOne();

        screen.Editor.PressStart("when");
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressChoice("finishes closing");
        screen.Editor.PressPrimary();
        screen.Editor.BackKeeping();
        screen.Run(2);

        screen.Editor.PressWhen(1);
        screen.Editor.PressBlock("Airlock Inner Door");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("button 1 is pressed");
        screen.Editor.PressPrimary();
        screen.Editor.PressChoice("Take it over");
        screen.Run(2);

        Assert.Single(System(screen).Triggers);
        Assert.Equal("When signal slot 1 button 1 is pressed", screen.Editor.Says(0));
        Assert.Equal(Filled.With(screen.Editor.Texts.ChangeTakeOver, "thing", SentenceText.Short(screen.Editor.Says(0)), "place", "Airlock"),
            screen.Editor.Steps().UndoText);


        screen.Editor.Steps().Undo!.Execute();
        screen.Run(2);

        Assert.Equal("When signal slot 1 button 1 is pressed", screen.Editor.Says(0));
        Assert.Equal("When block slot 1 finishes closing", screen.Editor.Says(1));
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
        Assert.Equal("When signal slot 1 button 1 is pressed", screen.Editor.Says(0));
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

        Assert.Equal("When block slot 1 finishes closing", screen.Editor.Says(0));
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

        screen.Editor.Rows()[0].Opens!.Execute();
        screen.Run(2);

        Assert.NotNull(screen.Editor.Sentence());
        Assert.False(screen.Shows("Airlock Button"));
        Assert.False(screen.Shows("button 1 is pressed"));
        Assert.DoesNotContain("When", screen.Words);
    }
}
