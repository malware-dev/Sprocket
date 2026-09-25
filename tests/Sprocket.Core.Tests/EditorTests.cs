using Sprocket.Host.SpaceEngineers.Blueprints;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Picking;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// The authoring flow, driven through the component surface a target renders.
/// </summary>
public class EditorTests
{
    sealed class Ship
    {
        public Rigged Rig = new();
        public Automation Automation = new();
        public AutomationRunner Runner = null!;
        public SprocketEditor Editor = null!;

        public GridWorld World => Rig.World;

        /// <summary>What the construct has been told to do, oldest first.</summary>
        public List<string> Log => Rig.Rig.Log;

        int _frame;

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++_frame);
                Editor.Tick(_frame);
            }
        }

        /// <summary>
        /// The host starting the sequence being edited, which is what pressing its button does.
        /// </summary>
        /// <remarks>
        /// A button raises nothing a rule can sample: its slot carries which sequence to run and the
        /// unit resolves that. Which slot carries which key is the harness's business - see RigTests.
        /// </remarks>
        public void Fire()
        {
            Runner.Call(Automation.Sequences[0]);
        }
    }

    static Ship Build()
    {
        var ship = new Ship();
        ship.Rig.Door(1, "Inner Door");
        ship.Rig.Door(2, "Outer Door");
        ship.Rig.Panel(3, "Airlock Button");
        ship.Rig.Light(4, "Airlock Light");

        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner);
        return ship;
    }

    /// <summary>
    /// A member that cannot be made into a step is still listed, and says why when it is chosen.
    /// </summary>
    /// <remarks>
    /// Left out, it would read as a block that does not have it. Offered as a number box, it would be
    /// a number nobody can know the meaning of, declined in silence when the step runs.
    /// </remarks>
    [Fact]
    public void AMemberThatCannotBeAStepSaysWhy()
    {
        var ship = new Ship();
        ship.Rig.Panel(3, "Airlock Button");
        ship.Rig.Light(4, "Airlock Light").CarryingUnread("Pattern", "Pattern");
        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner);
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");

        PickStage What() => editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages)
            .Single(s => s.Entries.Any(e => e.Text == "Set Pattern"));

        var pattern = What().Entries.Single(e => e.Text == "Set Pattern");
        Assert.True(pattern.IsWithheld);
        Assert.Equal("pending", pattern.Status);
        Assert.False(What().Entries.Single(e => e.Text == "Switch on").IsWithheld);

        // It says why where the pointer rests, and gives a press nothing to do.
        Assert.StartsWith("Set this in the terminal", pattern.Tip);
        Assert.Equal(0, pattern.Command);
        Assert.Null(What().Entries.Single(e => e.Text == "Switch on").Tip);
    }

    /// <summary>
    /// The list of what a block offers says above it how far that kind of block has been checked.
    /// </summary>
    /// <remarks>
    /// What a block offers is read from the game and tidied by rules that know nothing of it, so a
    /// block nobody has gone through can offer something doubled or oddly named. Said of the least
    /// checked of the picked blocks, since that is the one whose list may be wrong.
    /// </remarks>
    [Fact]
    public void TheListSaysHowFarItsBlockHasBeenChecked()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("do something");

        PickStage What() => editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages)
            .Single(s => s.Entries.Any(e => e.Text == "Switch on"));

        editor.PressBlock("Airlock Light");
        Assert.StartsWith("Sprocket has not checked this block yet", What().Note);

        editor.PressBlock("Inner Door");
        Assert.StartsWith("Sprocket has only partly checked this block", What().Note);

        editor.TickBlock("Airlock Light");
        Assert.StartsWith("Sprocket has not checked this block yet", What().Note);
    }

    /// <summary>
    /// A switch's value and the act that flips it share an id, and choosing one is not choosing both.
    /// </summary>
    /// <remarks>
    /// The game gives the act that flips a switch the switch's own id. Chosen by id alone, both rows
    /// would light and the step would be whichever came first in the list - a flip made into a set,
    /// asking for a value the flip does not take.
    /// </remarks>
    [Fact]
    public void AValueAndTheActThatFlipsItAreChosenApart()
    {
        var ship = new Ship();
        ship.Rig.Panel(3, "Airlock Button");
        ship.Rig.Light(4, "Airlock Light")
            .Carrying("AIBehavior", "AI Behavior", ValueKind.Flag)
            .Offering("AIBehavior", "AI Behavior On/Off");
        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner);
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");

        IEnumerable<PickOption> Offered() =>
            editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages).SelectMany(s => s.Entries);

        editor.PressChoice("AI Behavior On/Off");
        Assert.True(Offered().Single(e => e.Text == "AI Behavior On/Off").IsChosen);
        Assert.False(Offered().Single(e => e.Text == "Set AI Behavior").IsChosen);
        Assert.Equal("", editor.Screen.PrimaryQuestion);

        editor.PressPrimary();
        var step = ship.Automation.Sequences[0].Steps.OfType<Act>().Last();
        Assert.Equal("AIBehavior", step.ActionId);
        Assert.False(step.Sets);
    }

    /// <summary>
    /// The colon the game ends some of its names with is left to the terminal view.
    /// </summary>
    /// <remarks>
    /// "Mode:" is how the terminal labels the box beside it. In a list of things to choose it reads as a
    /// label with nothing after it, so the organized view says "Set Mode", and the terminal view shows the
    /// block's own words as they are.
    /// </remarks>
    [Fact]
    public void TheGamesLabelColonIsLeftToTheTerminalView()
    {
        var ship = new Ship();
        ship.Rig.Panel(3, "Airlock Button");
        ship.Rig.Light(4, "Airlock Light").Carrying("Mode", "Mode:", ValueKind.Flag);
        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner);
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");

        IEnumerable<PickStage> Stages() => editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages);

        Assert.Contains(Stages().SelectMany(s => s.Entries), e => e.Text == "Set Mode");
        Assert.DoesNotContain(Stages().SelectMany(s => s.Entries), e => e.Text == "Set Mode:");

        editor.Invoke(Stages().SelectMany(s => s.Views).Single(v => v.Text == "Terminal").Command);
        Assert.Contains(Stages().SelectMany(s => s.Entries), e => e.Text == "Mode:");
    }

    /// <summary>
    /// A member known to be out of a mod's reach is listed only where the block is shown as it is.
    /// </summary>
    /// <remarks>
    /// The organized view is what Sprocket offers, and this is nothing it can offer. The terminal view
    /// is what the block declares, and there it is, saying why nothing can be done with it.
    /// </remarks>
    [Fact]
    public void AMemberKnownToBeOutOfReachIsListedOnlyInTheTerminalView()
    {
        var ship = new Ship();
        ship.Rig.Panel(3, "Airlock Button");
        var light = ship.Rig.Light(4, "Airlock Light").Carrying("Pattern", "Pattern", ValueKind.Flag);
        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner);
        var editor = ship.Editor;

        // What a block's own knowledge says about it, the way the event controller's says it.
        foreach (var one in ship.Rig.Entity(light.Id).Kind.Actions)
        {
            if (one.GamePropertyId == "Pattern")
                one.Withhold(Readiness.Unsupported, "Set this in the terminal: the game keeps it from mods");
        }

        StartOnTheButton(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");

        IEnumerable<PickOption> Offered() =>
            editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages).SelectMany(s => s.Entries);

        Assert.DoesNotContain(Offered(), e => e.Text == "Pattern" || e.Text == "Set Pattern");

        editor.Invoke(editor.Screen.Components.OfType<PickComponent>().SelectMany(p => p.Stages)
            .SelectMany(s => s.Views).Single(v => v.Text == "Terminal").Command);
        var pattern = Offered().Single(e => e.Text == "Pattern");
        Assert.True(pattern.IsWithheld);
        Assert.Equal("not supported", pattern.Status);
        Assert.Equal(0, pattern.Command);
    }

    /// <summary>
    /// What somebody asks for and what the program says are not the same thing.
    /// </summary>
    /// <remarks>
    /// One sentence at the screen - "when the airlock button is pressed" - and two arrangements
    /// underneath. The game already starts things from a button, through a toolbar slot, so Sprocket
    /// puts its action in the slot and declares that the construct starts this. A door finishing
    /// closing is announced to nobody, so that one Sprocket has to watch for itself.
    /// <para>
    /// Nobody is asked which. The block knows, so the editor reads it off the block - which is why
    /// the same question can be asked about anything on the ship.
    /// </para>
    /// </remarks>
    [Fact]
    public void AMomentTheShipAnnouncesIsWiredRatherThanWatchedFor()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);

        var pressed = Assert.IsType<RequiredTrigger>(ship.Automation.Sequences[0].Trigger);
        Assert.Equal("pressed_1", pressed.EventId);
        Assert.Single(pressed.Sources);

        // And the same question about a door, which the game announces nothing about.
        ship.Editor.GoToRoot();
        ship.Tick();
        ship.Editor.PressRow(0);
        ship.Editor.PressStart("When…");
        ship.Editor.PressBlock("Inner Door");
        ship.Editor.PressChoice("finishes closing");
        ship.Editor.PressPrimary();

        var closed = Assert.IsType<WhenTrigger>(ship.Automation.Sequences[1].Trigger);
        Assert.Equal("closed", closed.EventId);
    }

    /// <summary>
    /// Changing the trigger can change how it arrives, and the sentence never mentions it.
    /// </summary>
    [Fact]
    public void PointingATriggerAtAButtonInsteadOfADoorChangesHowItArrives()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Inner Door");
        editor.PressChoice("finishes closing");
        editor.PressPrimary();
        ship.Tick();

        Assert.IsType<WhenTrigger>(ship.Automation.Sequences[0].Trigger);

        // Back to the system, where what starts a sequence is listed. Its row opens the same picker
        // the wizard used.
        editor.Back();
        ship.Tick();

        editor.PressWhen(0);
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
        ship.Tick();

        var pressed = Assert.IsType<RequiredTrigger>(ship.Automation.Sequences[0].Trigger);
        Assert.Equal("pressed_1", pressed.EventId);
        Assert.Equal("When Airlock Button is pressed", editor.Says(0));

        // One trigger for the sequence, not the old one left beside the new.
        Assert.Single(ship.Automation.Systems[0].Triggers);
    }

    /// <summary>
    /// A block picked for a sentence becomes a part of the system, which the first of it fills with
    /// that block, so another one of the system can fill it with its own.
    /// </summary>
    [Fact]
    public void APickedBlockIsAPartTheFirstOneFills()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("Switch on");
        editor.PressPrimary();
        ship.Tick();

        var system = ship.Automation.Systems[0];
        var step = Assert.IsType<Act>(ship.Automation.Sequences[0].Steps[0]);
        var trigger = Assert.IsAssignableFrom<MomentTrigger>(Assert.Single(system.Triggers));

        foreach (var (part, block) in new[] { (step.Targets[0], "Airlock Light"), (trigger.Sources[0], "Airlock Button") })
        {
            Assert.NotNull(system.FindPart(part));
            var filled = Assert.Single(system.First!.FilledWith(part));
            Assert.Equal(block, ship.Runner.Bindings.Entity(filled)!.Name);
        }
    }

    /// <summary>The same block picked again is the same part, not a second one filled by the same block.</summary>
    [Fact]
    public void PickingTheSameBlockAgainIsTheSamePart()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        for (var i = 0; i < 2; i++)
        {
            editor.PressAdd("do something");
            editor.PressBlock("Airlock Light");
            editor.PressChoice("Switch on");
            editor.PressPrimary();
            ship.Tick();
        }

        var steps = ship.Automation.Sequences[0].Steps.OfType<Act>().ToList();
        Assert.Equal(2, steps.Count);
        Assert.Equal(steps[0].Targets[0], steps[1].Targets[0]);
        Assert.Equal(2, ship.Automation.Systems[0].Parts.Count);
    }

    /// <summary>
    /// Making a blueprint asks nothing: the blueprint keeps the logic system's name, and so does the first
    /// made from it. It opens, and on the construct's own list that logic system then stands under it.
    /// </summary>
    [Fact]
    public void MakingABlueprintKeepsTheSystemsName()
    {
        var ship = Build();
        var editor = ship.Editor;
        var row = OneLogicSystem(ship);

        Assert.Equal("Make blueprint", row.ButtonText);
        Assert.Empty(row.ButtonQuestion);

        editor.Invoke(row.ButtonCommand);
        ship.Tick();

        var system = ship.Automation.Systems[0];
        Assert.True(system.IsBlueprint);
        Assert.Equal("Airlock", system.Name);
        Assert.Equal("Airlock", system.First!.Name);
        Assert.NotNull(editor.Component<BlueprintComponent>());

        editor.GoToRoot();
        ship.Tick();

        var heading = editor.Screen.Components.OfType<Sprocket.Host.SpaceEngineers.Screen.HeadingComponent>().Single();
        Assert.Equal("Airlock", heading.Name);
        Assert.Equal("Airlock", editor.Rows()[0].Summary);
        Assert.True(editor.Rows()[1].IsAction);
    }


    /// <summary>Another logic system made from a blueprint is numbered from two, the first number free.</summary>
    [Fact]
    public void AnotherFromABlueprintIsNumbered()
    {
        var ship = Build();
        var editor = ship.Editor;
        editor.Invoke(OneLogicSystem(ship).ButtonCommand);
        editor.GoToRoot();
        ship.Tick();

        var add = editor.Rows().Last();
        Assert.True(add.IsAction);
        Assert.False(add.Question.Length > 0);

        editor.Invoke(add.Command);
        ship.Tick();

        // Opened on the blueprint through the new one, which has blocks to be given.
        Assert.True(editor.Component<BlueprintComponent>().Members[1].Chosen);

        var system = ship.Automation.Systems[0];
        Assert.Equal(2, system.Instances.Count);
        Assert.Equal("Airlock 2", system.Instances[1].Name);

        editor.GoToRoot();
        ship.Tick();
        Assert.Equal("Airlock 2", editor.Rows()[1].Summary);

        editor.Invoke(editor.Rows().Last().Command);
        Assert.Equal("Airlock 3", system.Instances[2].Name);
    }

    /// <summary>
    /// A blueprint is edited through one of the logic systems made from it, and its sentences show that
    /// one's blocks: another shows nothing until its parts are filled, and then shows its own.
    /// </summary>
    [Fact]
    public void ABlueprintIsEditedThroughTheBlocksOfOneMadeFromIt()
    {
        var ship = Build();
        var editor = ship.Editor;
        editor.Invoke(OneLogicSystem(ship).ButtonCommand);
        ship.Tick();

        // Straight onto the blueprint, through the one it was made of.
        var blueprint = editor.Component<BlueprintComponent>();
        Assert.True(blueprint.Members[0].Chosen);
        Assert.Single(Assert.Single(blueprint.Parts).Blocks);

        editor.Invoke(blueprint.Members.Last().Command);
        ship.Tick();

        blueprint = editor.Component<BlueprintComponent>();
        Assert.True(blueprint.Members[1].Chosen);
        Assert.Empty(blueprint.Parts[0].Blocks);

        // Through the new one, the trigger names nothing yet.
        editor.Invoke(blueprint.Chosen!.ButtonCommand);
        ship.Tick();
        Assert.Equal("Airlock 2", editor.Component<UsingComponent>().Name);
        Assert.Empty(Assert.IsType<EntitiesSlot>(editor.Triggers()[0].Slots[0]).Entities);

        // Filled with a button of its own, it names that one.
        editor.Back();
        ship.Tick();
        editor.Invoke(editor.Component<BlueprintComponent>().Parts[0].Command);
        ship.Tick();
        editor.PressChoice("Airlock Button");
        ship.Tick();

        Assert.Single(editor.Component<BlueprintComponent>().Parts[0].Blocks);
        var cargo = ship.Automation.Systems[0].FindInstance("Airlock 2")!;
        Assert.Single(cargo.Filled);
    }

    /// <summary>A part is renamed on the blueprint, and the name is its annotation's label.</summary>
    [Fact]
    public void APartIsRenamedOnTheBlueprint()
    {
        var ship = Build();
        var editor = ship.Editor;
        editor.Invoke(OneLogicSystem(ship).ButtonCommand);
        ship.Tick();

        editor.SetText(editor.Component<BlueprintComponent>().Parts[0].RenameCommand, "Call button");
        ship.Tick();

        Assert.Equal("Call button", editor.Component<BlueprintComponent>().Parts[0].Name);
        Assert.Equal("Call button", Assert.Single(ship.Automation.Systems[0].Parts).Label);
    }

    /// <summary>A logic system with a trigger, and its row on the construct's own list.</summary>
    static StatusEntry OneLogicSystem(Ship ship)
    {
        StartOnTheButton(ship.Editor);
        ship.Editor.GoToRoot();
        ship.Tick();
        return ship.Editor.Rows()[0];
    }

    static void StartOnTheButton(SprocketEditor editor)
    {
        editor.NewSystem("Airlock");
        editor.PressStart("When…");
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
        Assert.True(sequence.CanRun(sequence.System.First));

        // Deliberately park this one sequence.
        editor.SetToggle(editor.Sentence().ToggleCommand, false);
        ship.Tick();
        Assert.False(sequence.CanRun(sequence.System.First));

        editor.SwitchOn();
        ship.Tick();
        Assert.True(sequence.CanRun(sequence.System.First));

        // Now park the whole system from its own screen.
        editor.Back();
        ship.Tick();
        Assert.True(editor.Screen.HasSwitch);

        editor.SetToggle(editor.Screen.SwitchCommand, false);
        ship.Tick();
        Assert.False(sequence.CanRun(sequence.System.First));
        Assert.True(sequence.Enabled);

        editor.SetToggle(editor.Screen.SwitchCommand, true);
        ship.Tick();
        Assert.True(sequence.CanRun(sequence.System.First));
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

        // The bin is the exception, and deliberately: throwing a sequence away is not editing it,
        // and wanting rid of one while it runs is exactly when somebody wants rid of it.
        Assert.NotEqual(0, sentence.DeleteCommand);

        // Renaming is exempt: it changes nothing about what the system does.
        Assert.True(editor.Screen.CanRename);
    }

    /// <summary>
    /// A system keeps its name whatever anybody types, and a sequence is free to lose one.
    /// </summary>
    /// <remarks>
    /// The two are deliberately opposite and the difference is not cosmetic. A sequence with no
    /// name reads as the sentence it makes, so clearing one is an ordinary act and naming stays
    /// genuinely optional. A system is a name and the ones of it and nothing else, so an empty
    /// one leaves a row on the front door that cannot be told apart from any other.
    ///
    /// Enforced in the page rather than only where the name is typed. A target that forgot to
    /// ask would otherwise erase the only thing a system can be called by, and the rule would
    /// end up living once per target instead of once.
    /// </remarks>
    [Fact]
    public void ASystemCannotBeLeftWithoutAName()
    {
        var ship = Build();
        var editor = ship.Editor;

        // One step back from the sequence is the system it belongs to, which is the only screen
        // in the editor where the name is the whole of what the thing is called.
        StartOnTheButton(editor);
        editor.Back();

        Assert.True(editor.Screen.NameRequired());
        Assert.Equal("Airlock", editor.Screen.CustomName());

        // Ticked after each one because that is when the screen is brought up to date, the same
        // as it is in a target. Reading CustomName without it reads the answer to the question
        // before last.
        editor.Name("");
        ship.Tick();
        Assert.Equal("Airlock", editor.Screen.CustomName());

        editor.Name("   ");
        ship.Tick();
        Assert.Equal("Airlock", editor.Screen.CustomName());

        editor.Name("Outer lock");
        ship.Tick();
        Assert.Equal("Outer lock", editor.Screen.CustomName());
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
        editor.PressChoice("Switch on");
        editor.PressPrimary();
        ship.Tick();

        var sequence = ship.Automation.Sequences[0];

        // The step and the await after it, added as one edit.
        Assert.Equal(2, sequence.Steps.Count);

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
        Assert.Equal(2, sequence.Steps.Count);
        var acted = Assert.IsType<Act>(sequence.Steps[0]);
        var block = SystemInstance.Through(sequence.System.First, acted.Targets)[0];
        Assert.Equal("Airlock Light", ship.Runner.Bindings.Entity(block)!.Name);

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
        editor.PressChoice("Switch on");
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
        editor.PressChoice("Close");
        editor.PressPrimary();
        ship.Tick();

        var before = editor.Sentence();
        var toggle = before.ToggleCommand;
        var firstStepMove = before.Steps[0].MoveToCommand;
        var firstStepSlot = before.Steps[0].Slots[0].Command;

        editor.PressAdd("do something");
        editor.PressBlock("Outer Door");
        editor.PressChoice("Open");
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
        editor.PressStart("When…");
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
    /// A picker opens on a ship whose blocks carry the game's own ids.
    /// </summary>
    /// <remarks>
    /// Block ids belong to the game, and Space Engineers hands them out in the quintillions. A command's
    /// number is twelve bits, so packing a raw entity id into one clamped every block to the same number
    /// and the second row registered threw - the editor could not open a picker on any real ship at all.
    ///
    /// Every other fake in this suite numbers its blocks from one, which is exactly why nothing here
    /// caught it and why the first real ship did, immediately.
    /// </remarks>
    [Fact]
    public void APickerOpensOnGameSizedEntityIds()
    {
        var ship = new Ship();
        // The low word is what matters: a command number is twelve bits, so any id whose bottom
        // thirty-two bits exceed 4095 is clamped to the same slot as every other. Real ids land there
        // essentially always; picking round numbers here would test nothing.
        ship.Rig.Door(1152921504606946976, "Inner Door");
        ship.Rig.Door(1152921504607046976, "Outer Door");
        ship.Rig.Panel(1152921504607146976, "Airlock Button");

        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner);

        ship.Editor.NewSystem("Airlock");

        // Throws before returning if two blocks claim one command.
        ship.Editor.PressStart("When…");

        // Four rows for three blocks: the construct's own programmable block is a block on the
        // grid like any other, and it has a switch, so it is offered like any other.
        var rows = ship.Editor.Component<PickComponent>().Stages[0].Entries;
        Assert.Equal(4, rows.Count);

        var seen = new HashSet<int>();
        for (var i = 0; i < rows.Count; i++)
            Assert.True(seen.Add(rows[i].Command), $"row {i} shares a command id with an earlier row");
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
        Assert.True(sequence.CanRun(sequence.System.First));

        editor.Back();
        ship.Tick();

        // On its own screen: renameable, the same way a sequence is.
        Assert.True(editor.Screen.CanRename);
        Assert.Equal("Airlock", editor.Screen.CustomName());
        editor.SetText(editor.Screen.RenameCommand(), "Front Airlock");
        ship.Tick();
        Assert.Equal("Front Airlock", system.Name);

        editor.Back();
        ship.Tick();

        // On the ship's list: the system's own row carries its switch.
        var heading = editor.Rows()
            .First(e => e.Depth == 0 && !e.IsAction);

        Assert.True(heading.CanSwitch);
        Assert.True(heading.SwitchIsOn);

        editor.SetToggle(heading.SwitchCommand, false);
        ship.Tick();

        Assert.False(system.First!.Enabled);
        Assert.False(sequence.CanRun(sequence.System.First));
        Assert.True(sequence.Enabled);
        Assert.False(editor.Rows()
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

        var row = editor.Rows()[0];
        Assert.True(row.CanSwitch);
        Assert.True(row.SwitchIsOn);

        editor.SetToggle(row.SwitchCommand, false);
        ship.Tick();

        Assert.False(sequence.Enabled);
        Assert.False(editor.Rows()[0].SwitchIsOn);
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
        Assert.Equal("Delete this sequence?", ship.Editor.Screen.Title());
        Assert.Equal("Sequence 1", ship.Editor.Screen.Subtitle);

        // The safe answer comes first, so a reflex press lands on it.
        var options = ship.Editor.Component<ChoiceListComponent>().Entries;
        Assert.Equal("Keep it, Delete it", string.Join(", ", options.Select(e => e.Text)));

        ship.Editor.PressChoice("Keep it");
        ship.Tick();
        Assert.Single(ship.Automation.Sequences);
        Assert.Equal("Sequence 1", ship.Editor.Screen.Title());

        ship.Editor.Invoke(ship.Editor.Sentence().DeleteCommand);
        ship.Tick();
        ship.Editor.PressChoice("Delete it");
        ship.Tick();

        // Gone, and standing on the system rather than on a question about nothing.
        Assert.Empty(ship.Automation.Sequences);
        Assert.Equal("Airlock", ship.Editor.Screen.Title());
    }

    /// <summary>
    /// Naming lives on the heading, and clearing it hands the heading back.
    /// </summary>
    /// <remarks>
    /// There used to be a naming box inside the sequence, underneath a heading that already
    /// showed the name: two places for one thing, one announcing it and one setting it. The
    /// name belongs on the heading because the heading is what a name is for.
    ///
    /// A sequence is named from birth, so this is about overtyping a default rather than filling
    /// in a blank, and clearing it is refused.
    /// </remarks>
    [Fact]
    public void TheHeadingIsWhereNamingHappens()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);
        ship.Tick();

        var heading = ship.Editor.Screen.Heading;

        // Already named, the way a new block is. So a target opening a box to rename it has
        // something to prefill, and this is always overtyping rather than filling in a blank.
        Assert.Single(heading);
        Assert.Equal("Sequence 1", heading[0].Name);
        Assert.True(heading[0].CanRename);

        ship.Editor.Name("Let me out");
        ship.Tick();

        Assert.Equal("Let me out", heading[0].Text);
        Assert.Equal("Let me out", heading[0].Name);

        // Clearing is refused now that the name is what identifies the row. Nothing is lost by
        // refusing: there is always a name to fall back to, because there was one from birth.
        ship.Editor.Name("");
        ship.Tick();
        Assert.Equal("Let me out", heading[0].Name);
        Assert.True(ship.Editor.Screen.NameRequired());
    }

    /// <summary>
    /// A new sequence takes the first free number in its system, not a running count.
    /// </summary>
    /// <remarks>
    /// So deleting the second of three and adding another gives "Sequence 2" back, rather than a
    /// fourth number for a third sequence. The numbering is per system because that is the list it
    /// has to be told apart in.
    /// </remarks>
    [Fact]
    public void ANewSequenceTakesTheFirstFreeNumber()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);
        ship.Tick();
        Assert.Equal("Sequence 1", ship.Editor.Screen.CustomName());

        AnotherOnTheButton(ship);
        Assert.Equal("Sequence 2", ship.Editor.Screen.CustomName());

        // The first one goes, so its number is free again.
        var system = ship.Automation.Systems[0];
        ship.Automation.RemoveSequence(system.Sequences[0]);

        AnotherOnTheButton(ship);
        Assert.Equal("Sequence 1", ship.Editor.Screen.CustomName());
    }

    /// <summary>
    /// Another sequence on the system that is already there, rather than a second system.
    /// </summary>
    /// <remarks>
    /// Through the system's own screen, which is where the ways to start one live. Numbering is per
    /// system, so a second system would start counting again and prove nothing.
    /// </remarks>
    static void AnotherOnTheButton(Ship ship)
    {
        ship.Editor.GoToRoot();
        ship.Tick();

        // Row 0 is the logic system, which opens.
        ship.Editor.PressRow(0);
        ship.Editor.PressStart("When…");
        ship.Editor.PressBlock("Airlock Button");
        ship.Editor.PressChoice("is pressed");
        ship.Editor.PressPrimary();

        // That button is already spoken for, which is asked about before anything else - and here
        // two on one button is the point of the walk.
        ship.Editor.PressChoice("Take it over");
        ship.Tick();
    }

    /// <summary>
    /// Leaving a sequence somebody built and left switched off offers to switch it on.
    /// </summary>
    /// <remarks>
    /// A sequence is parked while it is being built, so the way this fails is silent: somebody
    /// finishes, walks away, and concludes the whole thing does not work. Leaving is the last moment
    /// they are still thinking about it and the first at which one press puts it right.
    /// </remarks>
    [Fact]
    public void LeavingASequenceThatIsOffOffersToSwitchItOn()
    {
        var ship = Built();

        ship.Editor.Back();
        ship.Tick();

        Assert.Equal("Switch this sequence on?", ship.Editor.Question());
        Assert.True(ship.Editor.HasAnswer("Switch it on"));
        Assert.True(ship.Editor.HasAnswer("Leave it off"));

        ship.Editor.PressAnswer("Switch it on");
        ship.Tick();

        Assert.True(ship.Automation.Sequences[0].Enabled);
    }

    /// <summary>
    /// The answer stands until something changes again.
    /// </summary>
    /// <remarks>
    /// Being asked a question you have already answered is how a helpful prompt becomes one people
    /// learn to dismiss without reading. Editing it again is a different moment and earns one more.
    /// </remarks>
    [Fact]
    public void TheOfferIsNotRepeatedUntilSomethingChanges()
    {
        var ship = Built();

        ship.Editor.Back();
        ship.Tick();
        ship.Editor.PressAnswer("Leave it off");
        ship.Tick();

        Assert.False(ship.Automation.Sequences[0].Enabled);

        // Straight back in and out again, having touched nothing. Leaving landed on the system's
        // own screen, where the sequences are listed, so going back in is one press.
        ship.Editor.PressRow(0);
        ship.Tick();
        ship.Editor.Back();
        ship.Tick();

        Assert.False(ship.Editor.HasAnswer("Switch it on"));

        // Now change something, and it is worth asking again.
        ship.Editor.PressRow(0);
        ship.Tick();
        ship.Editor.Name("Let me out");
        ship.Tick();
        ship.Editor.Back();
        ship.Tick();

        Assert.True(ship.Editor.HasAnswer("Switch it on"));
    }

    /// <summary>
    /// Dismissing the question is not answering it, so pressing back again asks again.
    /// </summary>
    /// <remarks>
    /// Backing out of "switch this on?" says "I did not mean to leave", which is neither answer - so
    /// the sequence stays open, stays off, and the question is not spent.
    /// </remarks>
    [Fact]
    public void DismissingTheQuestionIsNotAnswering()
    {
        var ship = Built();

        ship.Editor.Back();
        ship.Tick();
        Assert.True(ship.Editor.HasAnswer("Switch it on"));

        ship.Editor.Unask();
        ship.Tick();

        // Still here, still off, and still worth asking about.
        Assert.Equal("", ship.Editor.Question());
        Assert.False(ship.Automation.Sequences[0].Enabled);

        ship.Editor.Back();
        ship.Tick();
        Assert.True(ship.Editor.HasAnswer("Switch it on"));
    }

    /// <summary>
    /// Swapping a step's verb changes the sentence, without the screen being rebuilt.
    /// </summary>
    /// <remarks>
    /// A slot carries what it refers to, resolved when it was made, and an edit that leaves the
    /// screen the same shape does not rebuild it - so the sentence went on saying what used to be
    /// true while the model said otherwise.
    /// </remarks>
    [Fact]
    public void SwappingAVerbChangesTheSentence()
    {
        var ship = Built();

        var before = (ActionSlot)ship.Editor.Sentence().Steps[0].Slots[0];
        Assert.Equal("close", before.Action.Text);

        ship.Editor.PressStepSlot(0, 0);
        ship.Tick();
        ship.Editor.PressChoice("Open");
        ship.Editor.PressPrimary();
        ship.Tick();

        var after = (ActionSlot)ship.Editor.Sentence().Steps[0].Slots[0];
        Assert.Equal("open", after.Action.Text);

        // A new object, because a target words a slot once and skips it while it is the same one.
        Assert.NotSame(before, after);
    }

    /// <summary>Nothing that cannot run is offered, since that is why it is off.</summary>
    [Fact]
    public void ABrokenSequenceIsNotOfferedToSwitchOn()
    {
        var ship = Built();
        ship.Rig.CannotStart(ship.Automation.Sequences[0]);

        ship.Editor.Back();
        ship.Tick();

        Assert.False(ship.Editor.HasAnswer("Switch it on"));
    }

    /// <summary>Nothing built yet is not something to be nagged about.</summary>
    [Fact]
    public void ASequenceWithNoStepsIsNotOffered()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);
        ship.Tick();

        ship.Editor.Back();
        ship.Tick();

        Assert.False(ship.Editor.HasAnswer("Switch it on"));
    }

    /// <summary>A sequence somebody built and left on has nothing to be asked about.</summary>
    static Ship Built()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);
        ship.Tick();

        ship.Editor.PressAdd("do something");
        ship.Editor.PressBlock("Inner Door");
        ship.Editor.PressChoice("Close");
        ship.Editor.PressPrimary();
        ship.Tick();

        return ship;
    }

    /// <summary>A name is cut to length by the model, not only by whatever asked for it.</summary>
    /// <remarks>
    /// A target should refuse it earlier and more kindly, while somebody is still typing. This is
    /// the backstop, because a target is the part that can be written wrong.
    /// </remarks>
    [Fact]
    public void ANameIsCutToLength()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);
        ship.Tick();

        ship.Editor.Name(new string('x', Naming.MaxLength + 20));
        ship.Tick();

        Assert.Equal(Naming.MaxLength, ship.Editor.Screen.CustomName().Length);
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

        Assert.Equal("Airlock", ship.Editor.Screen.Title());
        Assert.True(ship.Editor.Screen.CanGoBack);

        ship.Editor.Back();
        Assert.Equal("Automation for Test Rig", ship.Editor.Screen.Title());
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
        ship.Editor.PressStart("When…");

        var before = Listed(ship.Editor);
        ship.Editor.PressBlock("Airlock Button");

        Assert.Equal(before, Listed(ship.Editor));

        // Everything with a switch is here, the light and the programmable block included, because
        // every one of them can be switched on and off and so has something to be said about it.
        // Listed by name rather than in the order the ship handed them over, which on a real
        // construct is whatever walking the grid produced.
        Assert.Equal("Airlock Button, Airlock Light, Automation, Inner Door, Outer Door", before);
    }

    /// <summary>
    /// An impossible pair is allowed, and answered where the answer belongs.
    /// </summary>
    [Fact]
    public void BlocksWithNothingInCommonSaySo_RatherThanBeingUnpickable()
    {
        var ship = Build();
        ship.Editor.NewSystem("Airlock");
        ship.Editor.PressStart("When…");
        ship.Editor.PressBlock("Airlock Button");
        ship.Editor.TickBlock("Inner Door");

        var what = ship.Editor.Component<PickComponent>().Stages[1];
        Assert.Empty(what.Entries);
        Assert.NotEqual("", what.EmptyText);
    }

    static string Listed(SprocketEditor editor) =>
        string.Join(", ", editor.Component<PickComponent>().Stages[0].Entries.Select(e => e.Text));

    [Fact]
    public void ItOpensOnTheMachines_NotOnAListOfBlocks()
    {
        var ship = Build();

        // The same screen an established ship gets, with nothing in the list yet - not a
        // different screen that rearranges itself the moment anything is built.
        Assert.Equal("Automation for Test Rig", ship.Editor.Screen.Title());
        Assert.Empty(ship.Editor.Rows());

        // Both ways in are offered, and neither asks anything first.
        var start = ship.Editor.Component<StartComponent>();
        Assert.Equal("New logic system, Load blueprint", string.Join(", ", start.Options.Select(o => o.Label)));
        Assert.False(start.Options[0].Asks);
    }

    [Fact]
    public void TheQuestionsComeInReadingOrder()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");

        // One pane to begin with; the second appears once it has something to be about.
        Assert.Single(editor.Component<PickComponent>().Stages);
        Assert.False(editor.Screen.HasPrimary);

        editor.PressBlock("Airlock Button");
        Assert.Equal(2, editor.Component<PickComponent>().Stages.Count);

        // Still nothing to move on to - the path is not finished.
        Assert.False(editor.Screen.HasPrimary);

        // What happens, and then beside it what the trigger does.
        editor.PressChoice("is pressed");
        var stages = editor.Component<PickComponent>().Stages;
        Assert.Equal(3, stages.Count);
        Assert.Contains(stages[2].Entries, e => e.Text == "A new sequence" && e.IsChosen);
        Assert.True(editor.Screen.HasPrimary);

        // A new sequence is what a trigger usually does, so finishing lands on its sentence.
        editor.PressPrimary();
        Assert.Equal("", editor.Question());
        Assert.NotNull(editor.Sentence());
    }

    [Fact]
    public void SeveralBlocksAreEitherOnATrigger_AndBothOnAStep()
    {
        // A trigger fires on whichever happens first, a step acts on all of them. The two are a
        // different claim about the same pair of blocks, and a target that joined both with "and"
        // would state the wrong one - so the sentence says which it is rather than which word to use.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Inner Door");
        editor.TickBlock("Outer Door");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();

        var starts = Assert.IsType<WhenTrigger>(ship.Automation.Sequences[0].Trigger);
        Assert.Equal(2, starts.Sources.Count);
        Assert.False(starts.RequiresAll);

        editor.PressAdd("do something");
        editor.PressBlock("Inner Door");
        editor.TickBlock("Outer Door");
        editor.PressChoice("Close");
        editor.PressPrimary();

        var targets = Assert.IsType<EntitiesSlot>(editor.Sentence().Steps[0].Slots[1]);
        Assert.Equal(2, targets.Entities.Count);
        Assert.True(targets.RequiresAll);
    }

    static Door Door(Ship ship, string name) =>
        ship.Rig.Rig.Fitted.OfType<Door>().First(d => d.Name == name);

    /// <summary>
    /// And from its row on the system, which is the list somebody is looking at.
    /// </summary>
    /// <remarks>
    /// The same act in both places and the same words for it, because what somebody wants when they
    /// want to see a sequence work is not "open it first".
    /// </remarks>
    [Fact]
    public void ASequenceCanBeRunFromItsRow()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("Switch on");
        editor.PressPrimary();
        editor.SwitchOn();

        // Back to the system, where the sequence has a row of its own.
        editor.Back();
        ship.Tick();

        var row = editor.Rows()[0];
        Assert.NotEqual(0, row.RunCommand);

        editor.Invoke(row.RunCommand);
        ship.Tick(3);

        Assert.Contains("Airlock Light:on", ship.Log);
    }

    /// <summary>
    /// A sequence can be run by hand, from its own screen, switched off.
    /// </summary>
    /// <remarks>
    /// What starts a sequence is usually a door or a button somewhere else on the ship, so watching
    /// one work meant going and pressing it. This is the same call a trigger makes.
    /// <para>
    /// Parked is the state somebody working on one is in: the switch is what keeps its triggers off
    /// it while they poke at it, so refusing to run it then would take away the one way left to see
    /// it work. The switch holds against everything else, including another sequence calling it.
    /// </para>
    /// </remarks>
    [Fact]
    public void AParkedSequenceCanBeRunByHand()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("Switch on");
        editor.PressPrimary();
        ship.Tick();

        Assert.False(ship.Automation.Sequences[0].Enabled);

        var run = editor.Sentence().RunCommand;
        Assert.NotEqual(0, run);

        editor.Invoke(run);
        ship.Tick(3);

        Assert.Contains("Airlock Light:on", ship.Log);

        // And it says what is happening rather than what is allowed to: the switch is still off.
        Assert.False(ship.Automation.Sequences[0].Enabled);
    }

    /// <summary>
    /// A run by hand is not stopped by the switch it was started under.
    /// </summary>
    /// <remarks>
    /// The tick that follows would otherwise throw it away - a parked sequence is stopped, which is
    /// what makes the switch mean anything - so a parked run would light its first step and vanish.
    /// </remarks>
    [Fact]
    public void AParkedRunCarriesOnToItsEnd()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);

        editor.PressAdd("wait");
        editor.PressChoice("2 seconds - 120 ticks");

        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("Switch on");
        editor.PressPrimary();
        ship.Tick();

        editor.Invoke(editor.Sentence().RunCommand);
        ship.Tick(3);

        // Still going, on the wait, with the switch off throughout.
        var running = ship.Runner.Get(ship.Automation.Sequences[0]);
        Assert.True(running.IsRunning);
        Assert.DoesNotContain("Airlock Light:on", ship.Log);

        ship.Tick(400);

        Assert.Contains("Airlock Light:on", ship.Log);
    }

    /// <summary>
    /// Each trigger says what its own signal reads, and only its own.
    /// </summary>
    /// <remarks>
    /// A sequence that is idle while its signal reads true looks like a contradiction, and that
    /// apparent contradiction is the difference between a moment and a situation - which is the one
    /// thing about triggers somebody has to end up understanding. Two triggers on one system each
    /// keep their own reading, so one being true never lights the other.
    /// </remarks>
    [Fact]
    public void EachTriggerSaysWhatItIsReading()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");

        editor.PressStart("When…");
        editor.PressBlock("Inner Door");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();
        editor.Back();

        editor.PressStart("When…");
        editor.PressBlock("Outer Door");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();
        editor.Back();

        // Sampled only while they may run, which is what the switch on each row is for.
        foreach (var sequence in ship.Automation.Sequences)
            sequence.Enabled = true;

        Door(ship, "Inner Door").ForceOpen();
        ship.Tick(Rota.Ten);

        Assert.True(editor.Triggers()[0].IsLiveNow);
        Assert.False(editor.Triggers()[1].IsLiveNow);
    }

    /// <summary>
    /// "When tank A and tank B are empty" is a different sentence from "either of them", and fires
    /// at a different moment - when the last one gets there.
    /// </summary>
    /// <remarks>
    /// Said to the model rather than pressed, because the control that says it is hidden for now -
    /// see <c>PickPage.AsksAll</c>. What it claims is still true of every trigger that says it,
    /// including the ones already written down in somebody's program, which is why this stays.
    /// </remarks>
    [Fact]
    public void AllOfThemTogether_IsItsOwnClaimAndFiresWhenTheLastArrives()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Inner Door");
        editor.TickBlock("Outer Door");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();

        var starts = Assert.IsType<WhenTrigger>(ship.Automation.Sequences[0].Trigger);
        Assert.Equal(2, starts.Sources.Count);

        starts.RequiresAll = true;

        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("Switch on");
        editor.PressPrimary();
        editor.SwitchOn();

        ship.Tick(Rota.Ten);
        Door(ship, "Inner Door").ForceOpen();
        ship.Tick(Rota.Ten);
        Assert.Empty(ship.Log);

        Door(ship, "Outer Door").ForceOpen();
        ship.Tick(Rota.Ten);
        Assert.Contains("Airlock Light:on", ship.Log);
    }

    /// <summary>
    /// The choice between any one of the blocks and all of them is not on screen.
    /// </summary>
    /// <remarks>
    /// Hidden rather than removed - see <c>PickPage.AsksAll</c> for what is behind it and what it is
    /// waiting for. Here so that turning it back on is a decision somebody makes rather than
    /// something that happens quietly: this test is what fails then, and it names the reason.
    /// </remarks>
    [Fact]
    public void TheAnyOrAllChoiceIsNotOfferedYet()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");

        Assert.False(editor.HasChoice("any one"));
        Assert.False(editor.HasChoice("all of them"));

        editor.PressBlock("Inner Door");
        editor.TickBlock("Outer Door");

        Assert.False(editor.HasChoice("all of them"));
    }

    [Fact]
    public void BackingOutOfTheQuestionsLeavesNothingBehind()
    {
        // The sequence is not created until the questions are answered, so an
        // abandoned run does not litter the list with an empty one.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Airlock Button");
        editor.Back();

        Assert.Empty(ship.Automation.Sequences);

        // And one more back off the system itself.
        editor.Back();
        Assert.Equal("Automation for Test Rig", editor.Screen.Title());
    }

    [Fact]
    public void BackRetreatsOneQuestionAtATime()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        Assert.Equal("When something happens", editor.Screen.Title());

        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");

        // Both stages are one screen now, and the starters are no longer a screen at all, so
        // one step back reaches the system and a second reaches the ship. Leaving a picker that
        // has been filled in asks what to do with it first, and discarding is what back used to
        // mean on its own.
        editor.Back();
        editor.PressChoice("Discard them");
        Assert.Equal("Airlock", editor.Screen.Title());

        editor.Back();
        Assert.Equal("Automation for Test Rig", editor.Screen.Title());
    }

    /// <summary>
    /// A name replaces what a sequence does, and never what starts it - on its own screen and in
    /// the list alike.
    /// </summary>
    /// <remarks>
    /// It used to replace the whole line, which was defensible while a heading was one thing. Once
    /// the heading became two, the list kept the old rule and the two quietly disagreed: name a
    /// sequence and its heading still said when it happens while its row had stopped saying so.
    ///
    /// A row says one thing, and which thing depends on the list it is in. A sequence's row is what
    /// it is called, because what starts it is a row of its own directly above - so a list of twenty
    /// sequences still has the column worth comparing, on the triggers rather than on them.
    /// </remarks>
    [Fact]
    public void NamingASequenceReplacesWhatItDoesAndNotWhatStartsIt()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();

        ship.Tick();
        Assert.Equal("Sequence 1", editor.Screen.Title());

        editor.Name("Let me out");
        ship.Tick();

        // The sequence's own screen is what it is called and nothing about why it runs.
        Assert.Single(editor.Screen.Heading);
        Assert.Equal("Let me out", editor.Screen.Heading[0].Text);

        // What starts it is a row of its own, naming the sequence back, and the name it was just
        // given did not touch it.
        editor.Back();
        ship.Tick();

        Assert.Equal("Airlock", editor.Screen.Title());
        Assert.Equal("Let me out", editor.Rows()[0].Summary);
        Assert.Equal("When Airlock Button is pressed", editor.Says(0));
        Assert.Equal("then run Let me out", editor.Doing(0));
    }

    [Fact]
    public void NamingDoesNotRebuildTheScreen()
    {
        // Rebuilding would recreate the text field mid-word and drop the caret.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("Every…");
        editor.PressChoice("steady - every 100 ticks");
        editor.PressPrimary();

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
        Assert.Equal("Do something", editor.Screen.Title());
        Assert.Single(editor.Component<PickComponent>().Stages);

        editor.PressBlock("Inner Door");
        Assert.Equal(2, editor.Component<PickComponent>().Stages.Count);

        editor.PressChoice("Close");
        editor.PressPrimary();

        // One row, even though the sequence has the step and the await after it.
        Assert.Single(editor.Sentence().Steps);
        Assert.Equal("close", Assert.IsType<ActionSlot>(editor.Sentence().Steps[0].Slots[0]).Action.Text);

        var targets = Assert.IsType<EntitiesSlot>(editor.Sentence().Steps[0].Slots[1]);
        Assert.Equal(Door(ship, "Inner Door").Id, Assert.Single(targets.Entities));
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
        Assert.Equal("How long should it wait?", editor.Screen.Title());

        editor.PressChoice("5 seconds - 300 ticks");

        var step = Assert.Single(editor.Sentence().Steps);
        Assert.Equal(StepKind.Wait, step.Kind);
        Assert.Equal(300, Assert.IsType<DurationSlot>(step.Slots[0]).Ticks);
    }

    /// <summary>
    /// The reading follows the clock, and says nothing before there is anything to say.
    /// </summary>
    /// <remarks>
    /// How the number reads is <c>TimeText</c>'s, and asserted against it in <c>TimeTextTests</c>.
    /// What this is about is the editor keeping the reading current: the same sequence, untouched,
    /// reads differently a few seconds later, and starts over when it runs again.
    /// </remarks>
    [Fact]
    public void LastRunIsKeptUpToDate()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();

        editor.SwitchOn();
        ship.Tick();
        Assert.Equal("", editor.Sentence().LastRun);

        ship.Fire();
        ship.Tick(2);
        Assert.Equal("just now", editor.Sentence().LastRun);

        // Time passes and nothing else happens, but the reading has moved on.
        ship.Tick(300);
        Assert.Equal("less than a minute ago", editor.Sentence().LastRun);

        // It fires again, so the count starts over rather than carrying on.
        ship.Fire();
        ship.Tick(2);
        Assert.Equal("just now", editor.Sentence().LastRun);
    }

    /// <summary>
    /// A system made at the screen is by whoever is at it.
    /// </summary>
    /// <remarks>
    /// The namespace is half of what a system is, so it cannot be left for later: a system with no author is
    /// one that collides with everybody else's of the same name the moment it is shared.
    /// </remarks>
    [Fact]
    public void ANewSystemIsByWhoeverMadeIt()
    {
        var ship = Build();
        ship.Editor.Author = "MalTek";

        ship.Editor.NewSystem("Airlock");

        Assert.True(Assert.Single(ship.Automation.Systems).Is("MalTek", "Airlock"));
    }

    /// <summary>A new logic system is numbered, the first number free, and opens with its name in hand to be given.</summary>
    [Fact]
    public void ANewSystemIsNumberedAndOpensToBeNamed()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.PressStart("New logic system");
        Assert.Equal("Logic system 1", ship.Automation.Systems[0].Name);
        Assert.True(editor.Screen.Heading[0].NamedOnArrival);

        editor.GoToRoot();
        editor.PressStart("New logic system");
        Assert.Equal("Logic system 2", ship.Automation.Systems[1].Name);
    }

    [Fact]
    public void RenamingASystemToANameAnotherHasIsRefused()
    {
        var ship = Build();
        ship.Editor.Author = "MalTek";

        ship.Editor.NewSystem("Airlock");
        ship.Editor.GoToRoot();
        ship.Editor.NewSystem("Hangar");

        var rename = ship.Editor.Screen.Heading[0].RenameCommand;
        Assert.False(ship.Editor.Accepts(rename, "Airlock"));
        Assert.True(ship.Editor.Accepts(rename, "Hangar"));

        ship.Editor.SetText(rename, "Airlock");
        Assert.Equal("Hangar", ship.Automation.Systems[1].Name);
    }

    /// <summary>
    /// Ticking a block and choosing a block are two acts.
    /// </summary>
    /// <remarks>
    /// The box collects one at a time, which is right for building a set up by hand and wrong for
    /// the far commoner case of wanting one block: pressing the row is that case, and it drops what
    /// was taken before rather than adding to it.
    /// </remarks>
    [Fact]
    public void PressingARowTakesThatBlockAndDropsTheRest()
    {
        var ship = Build();
        ship.Editor.NewSystem("Airlock");
        ship.Editor.PressStart("When…");

        ship.Editor.PressBlock("Inner Door");
        ship.Editor.PressBlock("Outer Door");

        Assert.Equal(new[] { "Outer Door" }, Taken(ship.Editor));

        // And the box adds to what is there, whatever the row does.
        ship.Editor.TickBlock("Inner Door");
        Assert.Equal(new[] { "Inner Door", "Outer Door" }, Taken(ship.Editor));


        // Twice is off again, which is the whole of what a box does.
        ship.Editor.TickBlock("Inner Door");
        Assert.Equal(new[] { "Outer Door" }, Taken(ship.Editor));
    }

    /// <summary>
    /// And reaching back takes everything between, in the order the blocks are listed.
    /// </summary>
    /// <remarks>
    /// The listed order and not the order they were pressed: a run is what somebody drew down the
    /// screen, so it does not matter which end they started from.
    /// </remarks>
    [Fact]
    public void ReachingBackTakesEverythingBetween()
    {
        var ship = Build();
        ship.Editor.NewSystem("Airlock");
        ship.Editor.PressStart("When…");

        string[] listed = Listed(ship.Editor).Split(new[] { ", " }, StringSplitOptions.None);

        ship.Editor.PressBlock(listed[0]);
        ship.Editor.ThroughBlock(listed[2]);

        Assert.Equal(new[] { listed[0], listed[1], listed[2] }, Taken(ship.Editor));

        // Backwards is the same run. Which end it was drawn from is not part of what it means.
        ship.Editor.PressBlock(listed[2]);
        ship.Editor.ThroughBlock(listed[0]);

        Assert.Equal(new[] { listed[0], listed[1], listed[2] }, Taken(ship.Editor));
    }

    /// <summary>What is taken, in the order the picker lists it rather than in any other.</summary>
    /// <remarks>
    /// Unsorted on purpose: a run reaching back claims to take everything between in listed order,
    /// and sorting here would let that claim pass without being tested.
    /// </remarks>
    static string[] Taken(SprocketEditor editor)
    {
        return editor.Component<PickComponent>().Stages[0].Entries
            .Where(e => e.IsChosen).Select(e => e.Text).ToArray();
    }
}
