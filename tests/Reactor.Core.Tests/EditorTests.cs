using Reactor.Editing;
using Reactor.Runtime;
using Reactor.Sequences;
using Reactor.Ui;
using Xunit;

namespace Reactor.Tests;

/// <summary>
/// The authoring flow, driven through the component surface a target renders.
/// </summary>
public class EditorTests
{
    sealed class Ship
    {
        public TestWorld World = new();
        public Automation Automation = new();
        public AutomationRunner Runner = null!;
        public ReactorEditor Editor = null!;

        int _frame;

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                World.Tick();
                Editor.Tick(++_frame);
            }
        }
    }

    static Ship Build()
    {
        var ship = new Ship();
        ship.World.Add(new TestDoor(1, "Inner Door"));
        ship.World.Add(new TestDoor(2, "Outer Door"));
        ship.World.Add(new TestButton(3, "Airlock Button"));
        ship.World.Add(new TestLamp(4, "Airlock Light"));

        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new ReactorEditor(ship.World, ship.Automation, ship.Runner);
        return ship;
    }

    static void StartOnTheButton(ReactorEditor editor)
    {
        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
    }

    /// <summary>
    /// A system's switch and a sequence's switch are separate, and both have to hold.
    /// </summary>
    /// <remarks>
    /// Parking a whole airlock to weld on it, and then starting it again, must not quietly
    /// arm the one sequence somebody had deliberately left off. Those are two different
    /// intentions and neither may overwrite the other.
    /// </remarks>
    [Fact]
    public void ParkingAMachineDoesNotForgetWhichSequencesWereAlreadyParked()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.SwitchOn();
        ship.Tick();

        var sequence = ship.Automation.Sequences[0];
        Assert.True(sequence.CanRun);

        // Deliberately park this one sequence.
        editor.SetToggle(editor.Sentence().ToggleCommand, false);
        ship.Tick();
        Assert.False(sequence.CanRun);

        editor.SwitchOn();
        ship.Tick();
        Assert.True(sequence.CanRun);

        // Now park the whole system from its own screen.
        editor.Back();
        ship.Tick();
        Assert.True(editor.Screen.HasSwitch);

        editor.SetToggle(editor.Screen.SwitchCommand, false);
        ship.Tick();
        Assert.False(sequence.CanRun);
        Assert.True(sequence.Enabled);

        editor.SetToggle(editor.Screen.SwitchCommand, true);
        ship.Tick();
        Assert.True(sequence.CanRun);
    }

    /// <summary>
    /// Nothing that changes what a system does is offered while it can run.
    /// </summary>
    [Fact]
    public void ARunningSequenceCannotBeRewired()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        ship.Tick();

        // Parked while it is being built, so the sentence is open for work.
        var sentence = editor.Sentence();
        Assert.True(sentence.Editable);
        Assert.NotEqual(0, sentence.AddOptions[0].Command);

        editor.SwitchOn();
        ship.Tick();

        // Live: the rows are all still there and the screen has not changed shape, but
        // nothing on it will do anything, and it says why.
        sentence = editor.Sentence();
        Assert.False(sentence.Editable);
        Assert.NotEmpty(sentence.LockedText);
        Assert.Equal(2, sentence.AddOptions.Count);
        Assert.Equal(0, sentence.AddOptions[0].Command);
        Assert.Equal(0, sentence.Trigger.Slots[0].Command);
        Assert.Equal(0, sentence.DeleteCommand);

        // Renaming is exempt: it changes nothing about what the system does.
        Assert.True(editor.Screen.CanRename);
    }

    /// <summary>
    /// Undo and redo walk the same list in both directions, and a new edit ends the branch.
    /// </summary>
    /// <remarks>
    /// Redo replays the edit's own <c>Apply</c> rather than a third code path, so anything
    /// that survives the round-trip tests survives this too.
    /// </remarks>
    [Fact]
    public void UndoAndRedoWalkTheSameList()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("turn on");
        editor.PressPrimary();
        ship.Tick();

        var sequence = ship.Automation.Sequences[0];
        Assert.Single(sequence.Steps);

        // Nothing has been walked back, so there is nothing to put back.
        Assert.NotEqual(0, editor.Sentence().UndoCommand);
        Assert.Equal(0, editor.Sentence().RedoCommand);
        Assert.Equal("adding a step", editor.Sentence().UndoText);

        editor.Invoke(editor.Sentence().UndoCommand);
        ship.Tick();
        Assert.Empty(sequence.Steps);
        Assert.Equal("adding a step", editor.Sentence().RedoText);

        editor.Invoke(editor.Sentence().RedoCommand);
        ship.Tick();
        Assert.Single(sequence.Steps);
        Assert.Equal("Airlock Light", ship.World.FindEntity(sequence.Steps[0].Targets[0])!.Name);

        // Walk back, then do something different: the road not taken is gone, because putting
        // a step back at an index that now means something else is worse than not offering.
        editor.Invoke(editor.Sentence().UndoCommand);
        ship.Tick();
        editor.Name("Let me out");
        ship.Tick();

        Assert.Equal(0, editor.Sentence().RedoCommand);
        Assert.Equal("renaming", editor.Sentence().UndoText);
    }

    /// <summary>
    /// Both are edits, so both wait for the switch.
    /// </summary>
    [Fact]
    public void NeitherUndoNorRedoIsOfferedWhileItRuns()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("turn on");
        editor.PressPrimary();
        editor.Invoke(editor.Sentence().UndoCommand);
        ship.Tick();

        Assert.NotEqual(0, editor.Sentence().RedoCommand);

        editor.SwitchOn();
        ship.Tick();

        Assert.Equal(0, editor.Sentence().UndoCommand);
        Assert.Equal(0, editor.Sentence().RedoCommand);
    }

    /// <summary>
    /// A command id says what it is, so the same control keeps it across a rebuild.
    /// </summary>
    /// <remarks>
    /// It used to be a counter reset on every rebuild - id 7 meant "the seventh thing this page
    /// registered this time round". Stable only while nothing about the page's shape changed,
    /// with nothing enforcing that and nothing noticing when it stopped being true. Anything
    /// holding an id went quietly dead, because Invoke ignores what it does not recognise.
    ///
    /// The case that matters is the one that used to break it: the shape changing underneath.
    /// </remarks>
    [Fact]
    public void ACommandKeepsItsIdentityAcrossARebuild()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Inner Door");
        editor.PressChoice("close");
        editor.PressPrimary();
        ship.Tick();

        var before = editor.Sentence();
        var toggle = before.ToggleCommand;
        var firstStepMove = before.Steps[0].MoveToCommand;
        var firstStepSlot = before.Steps[0].Slots[0].Command;

        editor.PressAdd("do something");
        editor.PressBlock("Outer Door");
        editor.PressChoice("open");
        editor.PressPrimary();
        ship.Tick();

        var after = editor.Sentence();
        Assert.Equal(2, after.Steps.Count);
        Assert.Equal(toggle, after.ToggleCommand);
        Assert.Equal(firstStepMove, after.Steps[0].MoveToCommand);
        Assert.Equal(firstStepSlot, after.Steps[0].Slots[0].Command);

        // And an id held from before the rebuild still does what it did.
        Assert.True(ship.Automation.Sequences[0].Enabled == false);
        editor.SetToggle(toggle, true);
        ship.Tick();
        Assert.True(ship.Automation.Sequences[0].Enabled);
    }

    /// <summary>
    /// Narrowing a list does not renumber the commands after it.
    /// </summary>
    /// <remarks>
    /// This is the case that actually used to break, and the reason the search box went dead
    /// weeks ago. Under a counter, an id was a position in this rebuild's registration order,
    /// so filtering the entity list to fewer rows shifted every command registered after it -
    /// including everything in the next stage. A target holding one was then pressing
    /// somebody else's button, or nothing at all.
    /// </remarks>
    [Fact]
    public void NarrowingAListDoesNotRenumberWhatComesAfterIt()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Inner Door");
        ship.Tick();

        var stages = editor.Component<PickComponent>().Stages;
        var narrow = stages[0].NarrowCommand;
        var kinds = stages[0].Filters[0].Command;
        var firstChoice = stages[1].Entries[0].Command;
        var rows = stages[0].Entries.Count;

        // Type something that leaves fewer rows in the first stage.
        editor.SetText(narrow, "inner");
        ship.Tick();

        stages = editor.Component<PickComponent>().Stages;
        Assert.True(stages[0].Entries.Count < rows, "the search did not narrow anything");

        Assert.Equal(narrow, stages[0].NarrowCommand);
        Assert.Equal(kinds, stages[0].Filters[0].Command);
        Assert.Equal(firstChoice, stages[1].Entries[0].Command);
    }

    /// <summary>
    /// Two controls may not share one identity, and saying so is the point.
    /// </summary>
    /// <remarks>
    /// Under the old counter this mistake could not be detected at all - two controls that
    /// ought to be distinguishable simply were not, and the symptom was a button doing another
    /// button's job.
    /// </remarks>
    [Fact]
    public void TwoCommandsCannotClaimTheSameIdentity()
    {
        var ship = Build();
        ship.Editor.Register(Cmd.Undo, delegate { });

        Assert.Throws<InvalidOperationException>(
            () => ship.Editor.Register(Cmd.Undo, delegate { }));
    }

    /// <summary>
    /// A system can be stopped from the list it appears in, and renamed from its own heading.
    /// </summary>
    /// <remarks>
    /// Somebody welding on a hangar wants that hangar left alone. Making them open it to say
    /// so is a step that buys nothing, and the list is where they will look to check. Renaming
    /// used to be possible only at the moment of creation, so a typo was permanent.
    /// </remarks>
    [Fact]
    public void ASystemCanBeStoppedFromTheShipAndRenamedFromItsOwnScreen()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.SwitchOn();
        ship.Tick();

        var system = ship.Automation.Systems[0];
        var sequence = ship.Automation.Sequences[0];
        Assert.True(sequence.CanRun);

        editor.Back();
        ship.Tick();

        // On its own screen: renameable, the same way a sequence is.
        Assert.True(editor.Screen.CanRename);
        Assert.Equal("Airlock", editor.Screen.CustomName);
        editor.SetText(editor.Screen.RenameCommand, "Front Airlock");
        ship.Tick();
        Assert.Equal("Front Airlock", system.Name);

        editor.Back();
        ship.Tick();

        // On the ship's list: the system's own row carries its switch.
        var heading = editor.Component<StatusListComponent>().Entries
            .First(e => e.Depth == 0 && !e.IsAction);

        Assert.True(heading.CanSwitch);
        Assert.True(heading.SwitchIsOn);

        editor.SetToggle(heading.SwitchCommand, false);
        ship.Tick();

        Assert.False(system.Enabled);
        Assert.False(sequence.CanRun);
        Assert.True(sequence.Enabled);
        Assert.False(editor.Component<StatusListComponent>().Entries
            .First(e => e.Depth == 0 && !e.IsAction).SwitchIsOn);
    }

    /// <summary>
    /// A thing can be stopped from wherever it is listed, at every level.
    /// </summary>
    /// <remarks>
    /// A system on the ship's list, a sequence on its system's list, and a sequence on its own
    /// screen. Having it at two of those three and not the middle one was an accident of the
    /// order they were built in.
    /// </remarks>
    [Fact]
    public void ASequenceCanBeStoppedFromItsSystemsScreen()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.SwitchOn();
        ship.Tick();

        var sequence = ship.Automation.Sequences[0];
        Assert.True(sequence.Enabled);

        editor.Back();
        ship.Tick();

        var row = editor.Component<StatusListComponent>().Entries[0];
        Assert.True(row.CanSwitch);
        Assert.True(row.SwitchIsOn);

        editor.SetToggle(row.SwitchCommand, false);
        ship.Tick();

        Assert.False(sequence.Enabled);
        Assert.False(editor.Component<StatusListComponent>().Entries[0].SwitchIsOn);
    }

    /// <summary>
    /// The one irreversible act in the editor asks before it happens.
    /// </summary>
    /// <remarks>
    /// Everything else here is live and undone by doing the opposite. This throws away
    /// everything somebody built and there is no undo, so it used to be the single press that
    /// could not be walked back.
    /// </remarks>
    [Fact]
    public void DeletingASequenceAsksFirst()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);
        ship.Tick();

        var sentence = ship.Editor.Sentence();
        ship.Editor.Invoke(sentence.DeleteCommand);
        ship.Tick();

        // Nothing has gone yet, and the question names what it is about.
        Assert.Single(ship.Automation.Sequences);
        Assert.Equal("Delete this sequence?", ship.Editor.Screen.Title);
        Assert.StartsWith("When Airlock Button is pressed", ship.Editor.Screen.Subtitle);

        // The safe answer comes first, so a reflex press lands on it.
        var options = ship.Editor.Component<ChoiceListComponent>().Entries;
        Assert.Equal("Keep it, Delete it", string.Join(", ", options.Select(e => e.Text)));

        ship.Editor.PressChoice("Keep it");
        ship.Tick();
        Assert.Single(ship.Automation.Sequences);
        Assert.StartsWith("When Airlock Button is pressed", ship.Editor.Screen.Title);

        ship.Editor.Invoke(ship.Editor.Sentence().DeleteCommand);
        ship.Tick();
        ship.Editor.PressChoice("Delete it");
        ship.Tick();

        // Gone, and standing on the system rather than on a question about nothing.
        Assert.Empty(ship.Automation.Sequences);
        Assert.Equal("Airlock", ship.Editor.Screen.Title);
    }

    /// <summary>
    /// Naming lives on the heading, and clearing it hands the heading back.
    /// </summary>
    /// <remarks>
    /// There used to be a naming box inside the sequence, underneath a heading that already
    /// showed the name: two places for one thing, one announcing it and one setting it. The
    /// name belongs on the heading because the heading is what a name is for.
    /// </remarks>
    [Fact]
    public void TheHeadingIsWhereNamingHappens()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);
        ship.Tick();

        // Unnamed: the heading says what the thing does, and there is no name behind it for a
        // target to prefill a box with.
        Assert.True(ship.Editor.Screen.CanRename);
        Assert.StartsWith("When Airlock Button is pressed", ship.Editor.Screen.Title);
        Assert.Equal("", ship.Editor.Screen.CustomName);

        ship.Editor.Name("Let me out");
        ship.Tick();
        Assert.Equal("Let me out", ship.Editor.Screen.Title);
        Assert.Equal("Let me out", ship.Editor.Screen.CustomName);

        // Clearing is as ordinary an act as naming, and the sentence comes back.
        ship.Editor.Name("");
        ship.Tick();
        Assert.StartsWith("When Airlock Button is pressed", ship.Editor.Screen.Title);
        Assert.Equal("", ship.Editor.Screen.CustomName);
    }

    /// <summary>
    /// Backing out of a new system's first sequence lands on the system.
    /// </summary>
    /// <remarks>
    /// Making a system goes straight into its first sequence, because there is nothing on an
    /// empty system's screen worth stopping for. That shortcut used to leave the system's
    /// own screen off the stack entirely, so the only way back out of that first sequence was
    /// all the way to the ship - past the very thing you had just made.
    /// </remarks>
    [Fact]
    public void BackingOutOfTheFirstSequenceLandsOnTheMachine()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);

        ship.Editor.Back();

        Assert.Equal("Airlock", ship.Editor.Screen.Title);
        Assert.True(ship.Editor.Screen.CanGoBack);

        ship.Editor.Back();
        Assert.Equal("Your ship", ship.Editor.Screen.Title);
    }

    /// <summary>
    /// Ticking a block must not take the rest of the ship off the screen.
    /// </summary>
    /// <remarks>
    /// The picker used to offer only blocks that could still be grouped with the ones already
    /// ticked, so picking a button - the one thing on this ship with an event nothing else
    /// shares - emptied the list down to itself. Reading that as "my blocks are gone" is the
    /// only reasonable reading. The stages run left to right: what is picked decides what can
    /// be said about it, never which blocks exist.
    /// </remarks>
    [Fact]
    public void PickingABlockLeavesEveryOtherBlockOnOffer()
    {
        var ship = Build();
        ship.Editor.NewSystem("Airlock");
        ship.Editor.PressStart("When...");

        var before = Listed(ship.Editor);
        ship.Editor.PressBlock("Airlock Button");

        Assert.Equal(before, Listed(ship.Editor));

        // The lamp is absent throughout, and that is the question talking rather than the
        // selection: nothing can happen to it, so there is nothing to ask about it here.
        Assert.Equal("Inner Door, Outer Door, Airlock Button", before);
    }

    /// <summary>
    /// An impossible pair is allowed, and answered where the answer belongs.
    /// </summary>
    [Fact]
    public void BlocksWithNothingInCommonSaySo_RatherThanBeingUnpickable()
    {
        var ship = Build();
        ship.Editor.NewSystem("Airlock");
        ship.Editor.PressStart("When...");
        ship.Editor.PressBlock("Airlock Button");
        ship.Editor.PressBlock("Inner Door");

        var what = ship.Editor.Component<PickComponent>().Stages[1];
        Assert.Empty(what.Entries);
        Assert.NotEqual("", what.EmptyText);
    }

    static string Listed(ReactorEditor editor) =>
        string.Join(", ", editor.Component<PickComponent>().Stages[0].Entries.Select(e => e.Text));

    [Fact]
    public void ItOpensOnTheMachines_NotOnAListOfBlocks()
    {
        var ship = Build();

        // The same screen an established ship gets, with nothing in the list yet - not a
        // different screen that rearranges itself the moment anything is built.
        Assert.Equal("Your ship", ship.Editor.Screen.Title);
        Assert.Empty(ship.Editor.Component<StatusListComponent>().Entries);

        // Both ways in are offered. How a target asks for the name is its own business, so
        // there is nothing here about panels or dialogs.
        var start = ship.Editor.Component<StartComponent>();
        Assert.Equal("New system, Load system", string.Join(", ", start.Options.Select(o => o.Label)));
        Assert.Equal("What is it?", start.Options[0].Question);
    }

    [Fact]
    public void TheQuestionsComeInReadingOrder()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");

        // One pane to begin with; the second appears once it has something to be about.
        Assert.Single(editor.Component<PickComponent>().Stages);
        Assert.False(editor.Screen.HasPrimary);

        editor.PressBlock("Airlock Button");
        Assert.Equal(2, editor.Component<PickComponent>().Stages.Count);

        // Still nothing to move on to - the path is not finished.
        Assert.False(editor.Screen.HasPrimary);

        editor.PressChoice("is pressed");
        Assert.True(editor.Screen.HasPrimary);

        editor.PressPrimary();
        Assert.NotNull(editor.Sentence());
    }

    [Fact]
    public void SeveralBlocksReadAsEitherOnATrigger_AndBothOnAStep()
    {
        // A trigger fires on whichever happens first, a step acts on all of them.
        // Saying "and" in both places would state the wrong thing in one of them.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Inner Door");
        editor.PressBlock("Outer Door");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();
        Assert.Equal("Inner Door or Outer Door", editor.Sentence().Trigger.Slots[0].Text);

        editor.PressAdd("do something");
        editor.PressBlock("Inner Door");
        editor.PressBlock("Outer Door");
        editor.PressChoice("close");
        editor.PressPrimary();

        Assert.Equal("Inner Door and Outer Door", editor.Sentence().Steps[0].Slots[1].Text);
    }

    static TestDoor Door(Ship ship, string name) =>
        ship.World.Entities.OfType<TestDoor>().First(d => d.Name == name);

    static TestButton Button(Ship ship, string name) =>
        ship.World.Entities.OfType<TestButton>().First(b => b.Name == name);

    [Fact]
    public void AllOfThemTogether_ReadsAsAndAndFiresWhenTheLastArrives()
    {
        // "When tank A and tank B are empty" is a different sentence from "either of
        // them", and fires at a different moment - when the last one gets there.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Inner Door");
        editor.PressBlock("Outer Door");
        editor.PressChoice("all of them");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();
        Assert.Equal("Inner Door and Outer Door", editor.Sentence().Trigger.Slots[0].Text);

        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("turn on");
        editor.PressPrimary();
        editor.SwitchOn();

        ship.Tick(3);
        Door(ship, "Inner Door").ForceOpen();
        ship.Tick(3);
        Assert.Empty(ship.World.Log);

        Door(ship, "Outer Door").ForceOpen();
        ship.Tick(3);
        Assert.Contains("Airlock Light:on", ship.World.Log);
    }

    [Fact]
    public void TheAnyOrAllChoiceIsThereBeforeItMatters()
    {
        // It used to arrive on the second block, which put it on screen underneath the pointer at
        // the moment somebody was reaching for a third. Holding room for it was no better - a hole
        // sitting open whenever it was not needed. So it is simply always there on a stage that can
        // ask, which also means it can be set before it matters rather than only after.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");

        Assert.True(editor.HasChoice("any one"));
        Assert.True(editor.HasChoice("all of them"));

        editor.PressBlock("Inner Door");
        Assert.True(editor.HasChoice("all of them"));

        editor.PressBlock("Outer Door");
        Assert.True(editor.HasChoice("all of them"));
    }

    [Fact]
    public void BackingOutOfTheQuestionsLeavesNothingBehind()
    {
        // The sequence is not created until the questions are answered, so an
        // abandoned run does not litter the list with an empty one.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.Back();

        Assert.Empty(ship.Automation.Sequences);

        // And one more back off the system itself.
        editor.Back();
        Assert.Equal("Your ship", editor.Screen.Title);
    }

    [Fact]
    public void BackRetreatsOneQuestionAtATime()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        Assert.Equal("When something happens", editor.Screen.Title);

        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");

        // Both stages are one screen now, and the starters are no longer a screen at all, so
        // one step back reaches the system and a second reaches the ship.
        editor.Back();
        Assert.Equal("Airlock", editor.Screen.Title);

        editor.Back();
        Assert.Equal("Your ship", editor.Screen.Title);
    }

    [Fact]
    public void NamingASequence_ReplacesTheSentenceEverywhere()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();

        ship.Tick();
        Assert.StartsWith("When Airlock Button is pressed", editor.Screen.Title);

        editor.Name("Let me out");
        ship.Tick();
        Assert.Equal("Let me out", editor.Screen.Title);

        // And in the list, which is the reason naming exists at all. Backing out of the
        // sequence lands on the system it belongs to, which is where its sequences are listed.
        editor.Back();
        ship.Tick();

        Assert.Equal("Airlock", editor.Screen.Title);
        Assert.Equal("Let me out", editor.Component<StatusListComponent>().Entries[0].Summary);
    }

    [Fact]
    public void NamingDoesNotRebuildTheScreen()
    {
        // Rebuilding would recreate the text field mid-word and drop the caret.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("Every...");
        editor.PressChoice("steady - every 100 ticks");

        // The component instances must survive, or a target rebuilds its controls
        // and the field loses focus and the caret mid-word.
        var before = editor.Sentence();
        editor.Name("Housekeeping");

        Assert.Same(before, editor.Sentence());
    }

    [Fact]
    public void AddingAStep_AsksWhatThenAsksHow()
    {
        var ship = Build();
        var editor = ship.Editor;
        StartOnTheButton(editor);

        editor.PressAdd("do something");
        Assert.Equal("Do something", editor.Screen.Title);
        Assert.Single(editor.Component<PickComponent>().Stages);

        editor.PressBlock("Inner Door");
        Assert.Equal(2, editor.Component<PickComponent>().Stages.Count);

        editor.PressChoice("close");
        editor.PressPrimary();

        Assert.Single(editor.Sentence().Steps);
        Assert.Equal("close", editor.Sentence().Steps[0].Slots[0].Text);
        Assert.Equal("Inner Door", editor.Sentence().Steps[0].Slots[1].Text);
    }

    [Fact]
    public void BackingOutOfAddingAStep_LeavesNoHalfWrittenLine()
    {
        var ship = Build();
        var editor = ship.Editor;
        StartOnTheButton(editor);

        editor.PressAdd("do something");
        editor.PressBlock("Inner Door");
        editor.Back();

        Assert.Empty(editor.Sentence().Steps);
    }

    [Fact]
    public void WaitingIsItsOwnKindOfStep_NotAnActionOnABlock()
    {
        // A timed wait has no block to act on, so forcing it through the same
        // question would mean answering one that does not apply.
        var ship = Build();
        var editor = ship.Editor;
        StartOnTheButton(editor);

        editor.PressAdd("wait");
        Assert.Equal("How long should it wait?", editor.Screen.Title);

        editor.PressChoice("5 seconds - 300 ticks");

        var step = Assert.Single(editor.Sentence().Steps);
        Assert.Equal("wait", step.Lead);
        Assert.Equal("5 seconds", step.Slots[0].Text);
    }

    [Fact]
    public void LastRunIsWordedForPeople_AndNotRebuiltEveryTick()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();

        editor.SwitchOn();
        ship.Tick();
        Assert.Equal("", editor.Sentence().LastRun);

        Button(ship, "Airlock Button").Press();
        ship.Tick(2);
        Assert.Equal("just now", editor.Sentence().LastRun);

        ship.Tick(180);
        var words = editor.Sentence().LastRun;
        Assert.Equal("3 seconds ago", words);

        // Half a second later it still says the same thing, and says it with the very
        // same string - the words are not rebuilt sixty times a second.
        ship.Tick(30);
        Assert.Same(words, editor.Sentence().LastRun);

        ship.Tick(60);
        Assert.Equal("4 seconds ago", editor.Sentence().LastRun);

        ship.Tick(60 * 60);
        Assert.Equal("a minute ago", editor.Sentence().LastRun);
    }

}
