using Sprocket.Host.SpaceEngineers;
using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Triggers.Edits;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Door = Sprocket.Sim.Machine.Door;
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
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner, new Texts());
        return ship;
    }

    /// <summary>
    /// A member that cannot be made into a step yet is left out of the organized view and listed in the terminal view,
    /// where it says why when it is chosen.
    /// </summary>
    /// <remarks>
    /// Offered as a number box, it would be a number nobody can know the meaning of, declined in silence when the step runs.
    /// </remarks>
    [Fact]
    public void AMemberThatCannotBeAStepSaysWhy()
    {
        var ship = new Ship();
        ship.Rig.Panel(3, "Airlock Button");
        ship.Rig.Light(4, "Airlock Light").CarryingUnread("Pattern", "Pattern");
        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner, new Texts());
        ship.Rig.Entity(4).Kind.FindAction("Pattern", ReachKind.Value)!.Vouch();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");

        Assert.DoesNotContain(editor.Pick().Stages.SelectMany(s => s.Entries()), e => e.Text == "Pattern");
        editor.Pick().Stages.Last().Views[1].Command!.Execute();

        StageColumn What() => editor.Pick().Stages
            .Single(s => s.Entries().Any(e => e.Text == "Pattern"));

        var pattern = What().Entries().Single(e => e.Text == "Pattern");
        Assert.True(pattern.IsWithheld);
        Assert.Equal("pending", pattern.Status);
        Assert.False(What().Entries().Single(e => e.Text == "Toggle block On/Off").IsWithheld);

        // It says why where the pointer rests, and gives a press nothing to do.
        Assert.StartsWith("*The game doesn't let mods read", pattern.Why);
        Assert.False(pattern.IsPickable);
        Assert.Null(What().Entries().Single(e => e.Text == "Toggle block On/Off").Why);
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
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner, new Texts());
        ship.Rig.Entity(4).Kind.FindAction("AIBehavior", ReachKind.Value)!.Vouch();
        ship.Rig.Entity(4).Kind.FindAction("AIBehavior", ReachKind.Action)!.Vouch();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");

        IEnumerable<PickRow> Offered() =>
            editor.Pick().Stages.SelectMany(s => s.Entries());

        editor.PressChoice("AI behavior on/off");
        Assert.True(Offered().Single(e => e.Text == "AI behavior on/off").IsChosen);
        Assert.False(Offered().Single(e => e.Text == "AI behavior").IsChosen);
        Assert.Empty(editor.Picking().Fields);

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
    /// label with nothing after it, so the organized view says "Mode", and the terminal view shows the
    /// block's own words as they are.
    /// </remarks>
    [Fact]
    public void TheGamesLabelColonIsLeftToTheTerminalView()
    {
        var ship = new Ship();
        ship.Rig.Panel(3, "Airlock Button");
        ship.Rig.Light(4, "Airlock Light").Carrying("Mode", "Mode:", ValueKind.Flag);
        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner, new Texts());
        ship.Rig.Entity(4).Kind.FindAction("Mode", ReachKind.Value)!.Vouch();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");

        IEnumerable<StageColumn> Stages() => editor.Pick().Stages;

        Assert.Contains(Stages().SelectMany(s => s.Entries()), e => e.Text == "mode");
        Assert.DoesNotContain(Stages().SelectMany(s => s.Entries()), e => e.Text == "Mode:");

        Stages().Single(s => s.Views.Count > 0).PressView("Terminal");
        Assert.Contains(Stages().SelectMany(s => s.Entries()), e => e.Text == "Mode:");
    }

    /// <summary>Where the game's own screens list a block is left to the terminal view.</summary>
    [Fact]
    public void WhereTheGameListsABlockIsLeftToTheTerminalView()
    {
        var ship = new Ship();
        ship.Rig.Panel(3, "Airlock Button");
        ship.Rig.Light(4, "Airlock Light")
            .Carrying("ShowInTerminal", "Show block in terminal", ValueKind.Flag)
            .Carrying("ShowInInventory", "Show block in inventory screen", ValueKind.Flag)
            .Carrying("ShowInToolbarConfig", "Show block in toolbar config", ValueKind.Flag);
        ship.Runner = new AutomationRunner(ship.Automation, ship.World);
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner, new Texts());
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");

        IEnumerable<StageColumn> Stages() => editor.Pick().Stages;
        var shown = new[] { "Show block in terminal", "Show block in inventory screen", "Show block in toolbar config" };

        Assert.DoesNotContain(Stages().SelectMany(s => s.Entries()), e => shown.Contains(e.Text));

        Stages().Single(s => s.Views.Count > 0).PressView("Terminal");
        foreach (var one in shown)
            Assert.Contains(Stages().SelectMany(s => s.Entries()), e => e.Text == one);
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
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner, new Texts());
        var editor = ship.Editor;

        // What a block's own knowledge says about it, the way the event controller's says it.
        foreach (var one in ship.Rig.Entity(light.Id).Kind.Actions)
        {
            if (one.GamePropertyId == "Pattern")
                one.Withhold(Readiness.Unsupported, UnreadyReason.Events);
        }

        StartOnTheButton(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");

        IEnumerable<PickRow> Offered() =>
            editor.Pick().Stages.SelectMany(s => s.Entries());

        Assert.DoesNotContain(Offered(), e => e.Text == "Pattern");

        editor.Pick().Stages.Single(s => s.Views.Count > 0).PressView("Terminal");
        var pattern = Offered().Single(e => e.Text == "Pattern");
        Assert.True(pattern.IsWithheld);
        Assert.Equal("not supported", pattern.Status);
        Assert.False(pattern.IsPickable);
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

        var pressed = Assert.IsType<SignalTrigger>(ship.Automation.Sequences[0].Trigger);
        Assert.Single(pressed.Sources);
        Assert.Equal(1, Assert.Single(Pressed.Where(ship.Automation, ship.Automation.Systems[0], pressed.Part)!).Slot);

        // And the same question about a door, which the game announces nothing about.
        ship.Editor.GoToRoot();
        ship.Tick();
        ship.Editor.PressRow(0);
        ship.Editor.PressStart("when");
        ship.Editor.PressBlock("Inner Door");
        ship.Editor.PressChoice("finishes closing");
        ship.Editor.PressPrimary();

        var closed = Assert.IsType<SignalTrigger>(ship.Automation.Sequences[1].Trigger);
        Assert.True(closed.IsWatched);
        Assert.Equal("finishes_closing", closed.EventId);
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
        editor.PressStart("when");
        editor.PressBlock("Inner Door");
        editor.PressChoice("finishes closing");
        editor.PressPrimary();
        ship.Tick();

        Assert.True(Assert.IsType<SignalTrigger>(ship.Automation.Sequences[0].Trigger).IsWatched);

        // Back to the system, where what starts a sequence is listed. Its row opens the same picker
        // the wizard used.
        editor.Back();
        ship.Tick();

        editor.PressWhen(0);
        editor.PressBlock("Airlock Button");
        editor.PressChoice("button 1 is pressed");
        editor.PressPrimary();
        ship.Tick();

        var pressed = Assert.IsType<SignalTrigger>(ship.Automation.Sequences[0].Trigger);
        Assert.Equal(1, Assert.Single(Pressed.Where(ship.Automation, ship.Automation.Systems[0], pressed.Part)!).Slot);
        Assert.Equal("When signal slot 1 button 1 is pressed", editor.Says(0));

        // One trigger for the sequence, not the old one left beside the new.
        Assert.Single(ship.Automation.Systems[0].Triggers);
    }

    /// <summary>
    /// A block picked for a logic system's sentence is a variable of the program's, named after the block, and a button
    /// picked to start it is a signal variable on that button.
    /// </summary>
    [Fact]
    public void APickedBlockIsAVariable()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
        ship.Tick();

        var system = ship.Automation.Systems[0];
        var installation = system.First!;
        var step = Assert.IsType<Act>(ship.Automation.Sequences[0].Steps[0]);
        var trigger = Assert.IsType<SignalTrigger>(Assert.Single(system.Triggers));

        Assert.False(system.FindPart(step.Targets[0])!.IsSignal);
        Assert.Equal("block slot 1", step.Targets[0]);
        Assert.Equal("Airlock Light", ship.Runner.Bindings.Entity(installation.FilledWith(step.Targets[0])![0])!.Name);

        Assert.True(system.FindPart(trigger.Part)!.IsSignal);
        Assert.Equal("Airlock Button", ship.Runner.Bindings.Entity(installation.FilledWith(trigger.Part)![0])!.Name);
        Assert.Equal(1, installation.Positions(trigger.Part)![0].Slot);
    }

    /// <summary>The same block picked again is the same variable, not a second one for the same block.</summary>
    [Fact]
    public void PickingTheSameBlockAgainIsTheSamePart()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        for (var i = 0; i < 2; i++)
        {
            editor.PressAdd("task");
            editor.PressBlock("Airlock Light");
            editor.PressChoice("power");
            editor.Pick().Arguments!.Form.Fields[0].Text = "on";
            editor.PressPrimary();
            ship.Tick();
        }

        var steps = ship.Automation.Sequences[0].Steps.OfType<Act>().ToList();
        Assert.Equal(2, steps.Count);
        Assert.Equal(steps[0].Targets[0], steps[1].Targets[0]);
        Assert.Single(ship.Automation.Refs, declared => declared.Name.StartsWith("Airlock Light", StringComparison.Ordinal));
    }

    /// <summary>
    /// A new logic system has its first installation from the start, with no name of its own, and stands on the construct's
    /// own list as its heading with that installation under it.
    /// </summary>
    [Fact]
    public void ANewLogicSystemHasItsFirstInstallation()
    {
        var ship = Build();
        var editor = ship.Editor;
        OneLogicSystem(ship);

        var system = ship.Automation.Systems[0];
        Assert.True(system.IsBlueprint);
        Assert.Equal("Airlock", system.Name);
        Assert.Equal("Installation 1", Assert.Single(system.Instances).Name);

        Assert.Equal(new[] { "Airlock" }, editor.RootHeadingNames());
        Assert.Equal("Installation 1", Assert.Single(editor.RootRows()).Summary);
        Assert.Equal("+ installation", editor.RootAdds().Single().Text);
    }


    /// <summary>Another installation is named by the first number free, counting on from those there are.</summary>
    [Fact]
    public void AnotherInstallationIsNumbered()
    {
        var ship = Build();
        var editor = ship.Editor;
        OneLogicSystem(ship);

        editor.AddInstallation(0);
        ship.Tick();

        // Opened on the logic system through the new one, which has blocks to be given.
        Assert.Equal("Installation 2", editor.Blueprint().Title);

        var system = ship.Automation.Systems[0];
        Assert.Equal(2, system.Instances.Count);
        Assert.Equal("Installation 2", system.Instances[1].Name);

        // Something done to it, switched on, so it stays when the panel is left.
        ship.Automation.Systems[0].Instances[ship.Automation.Systems[0].Instances.Count - 1].Enabled = true;
        ship.Tick();

        editor.GoToRoot();
        ship.Tick();
        Assert.Equal("Installation 2", editor.RootRows()[1].Summary);

        editor.AddInstallation(0);
        Assert.Equal("Installation 3", system.Instances[2].Name);
        Assert.Equal("Installation 3", editor.Blueprint().Title);
    }

    /// <summary>Done keeps what was just made, left untouched or not; only Back takes it back.</summary>
    [Fact]
    public void DoneKeepsWhatWasJustMade()
    {
        var ship = Build();
        var editor = ship.Editor;
        OneLogicSystem(ship);
        var system = ship.Automation.Systems[0];

        editor.AddInstallation(0);
        ship.Tick();
        editor.PressPrimary();
        ship.Tick();
        Assert.Equal(2, system.Instances.Count);

        editor.GoToRoot();
        ship.Tick();
        editor.NewUntouchedSystem();
        ship.Tick();
        editor.PressPrimary();
        ship.Tick();
        Assert.Equal(2, ship.Automation.Systems.Count);

        editor.GoToRoot();
        ship.Tick();
        editor.NewUntouchedSystem();
        ship.Tick();
        editor.Back();
        ship.Tick();
        Assert.Equal(2, ship.Automation.Systems.Count);
    }

    /// <summary>Adding another and backing straight out adds nothing; giving it a name of its own keeps it.</summary>
    [Fact]
    public void AnotherLeftUntouchedIsNotAdded()
    {
        var ship = Build();
        var editor = ship.Editor;
        OneLogicSystem(ship);
        var system = ship.Automation.Systems[0];

        editor.AddInstallation(0);
        ship.Tick();
        Assert.Equal(2, system.Instances.Count);

        editor.GoToRoot();
        ship.Tick();
        Assert.Single(system.Instances);
        Assert.Single(editor.RootRows());

        editor.AddInstallation(0);
        ship.Tick();
        editor.Screen.Heading.Single(part => part.CanRename).Renames!("Port airlock");
        ship.Tick();

        editor.GoToRoot();
        ship.Tick();
        Assert.Equal(2, system.Instances.Count);
        Assert.Equal("Port airlock", system.Instances[1].Name);
    }

    /// <summary>
    /// A logic system's sentences show the blocks of one of its installations: another shows nothing until its slots are
    /// filled, and then shows its own.
    /// </summary>
    [Fact]
    public void ALogicSystemIsEditedThroughTheBlocksOfOneInstallation()
    {
        var ship = Build();
        var editor = ship.Editor;
        OneLogicSystem(ship);
        editor.OpenBlueprintSystem(0);
        ship.Tick();

        // The logic system's slots, through the first installation.
        Assert.Single(Assert.Single(editor.SystemSlots()).Blocks);

        editor.GoToRoot();
        ship.Tick();
        editor.AddInstallation(0);
        ship.Tick();

        Assert.Equal("Installation 2", editor.Blueprint().Title);
        Assert.Empty(editor.Parts()[0].Blocks);

        // Something done to it, switched on, so it stays when the panel is left.
        ship.Automation.Systems[0].Instances[ship.Automation.Systems[0].Instances.Count - 1].Enabled = true;
        ship.Tick();

        // Through the new one, the trigger names nothing yet.
        editor.GoToRoot();
        ship.Tick();
        editor.OpenBlueprintSystem(0);
        ship.Tick();
        Assert.Equal("Installation 2", editor.UsingName());
        Assert.Empty(Assert.IsType<EntitiesSlot>(editor.Triggers()[0].Starts[0]).Entities);

        // Filled with a button of its own, it names that one.
        editor.GoToRoot();
        ship.Tick();
        editor.RootRows()[1].Opens!.Execute();
        ship.Tick();
        editor.Parts()[0].Fills!.Execute();
        ship.Tick();

        // A signal slot's pick offers only blocks that send signals, and of a block only its signals.
        Assert.NotEmpty(editor.Offered());
        Assert.All(editor.Offered(), row => Assert.Contains("Button", row.Secondary));
        editor.PressBlock("Airlock Button");
        Assert.True(editor.HasChoice("button is pressed"));
        editor.PressChoice("button 1 is pressed");

        ship.Tick();
        editor.PressPrimary();
        ship.Tick();

        // That button is the first blockset's, so it is asked about before it moves, and taking it empties the first's slot.
        Assert.StartsWith("Airlock Button already starts ", editor.Question());
        editor.PressChoice(editor.Texts.ButtonTakeOver);
        ship.Tick();

        Assert.Single(editor.Parts()[0].Blocks);
        var cargo = ship.Automation.Systems[0].Instances[1];
        Assert.Single(cargo.Filled);
        Assert.Empty(ship.Automation.Systems[0].Instances[0].Filled);
    }

    /// <summary>
    /// A logic system's variables each become a slot when it is made a blueprint, and its trigger and step name the slots,
    /// which the first instance answers with the variables.
    /// </summary>
    [Fact]
    public void ABlueprintOfBlocksNamedDirectlyGetsASlotForEach()
    {
        var ship = Build();
        var button = ship.Automation.Declare(ship.World, ship.World.Entities.Single(e => e.Name == "Airlock Button")).Name;
        var door = ship.Automation.Declare(ship.World, ship.World.Entities.Single(e => e.Name == "Inner Door")).Name;

        var system = ship.Automation.AddSystem();
        var pressed = Pressed.On(ship.Automation, system, button);
        var sequence = ship.Automation.AddSequence(system, pressed);
        var close = new Act("close");
        close.Targets.Add(door);
        sequence.Steps.Add(close);
        sequence.Steps.Add(new Await());

        Assert.Empty(system.Parts);
        ship.Tick();

        // Arriving at the construct's list gives it slots and a first installation.
        ship.Editor.GoToRoot();
        ship.Tick();

        Assert.True(system.IsBlueprint);
        Assert.Equal(2, system.Parts.Count);
        var buttonSlot = Assert.Single(pressed.Sources);
        var doorSlot = Assert.Single(close.Targets);
        Assert.True(system.FindPart(buttonSlot)!.IsSignal);
        Assert.False(system.FindPart(doorSlot)!.IsSignal);
        Assert.Equal(new[] { button }, system.First!.FilledWith(buttonSlot));
        Assert.Equal(1, system.First!.Positions(buttonSlot)![0].Slot);
        Assert.Equal(new[] { door }, system.First!.FilledWith(doorSlot));

        ship.Editor.RootRows()[0].Opens!.Execute();
        ship.Tick();
        Assert.Equal(2, ship.Editor.Parts().Count);

    }

    /// <summary>A logic system with a trigger, and the editor back on the construct's own list.</summary>
    static void OneLogicSystem(Ship ship)
    {
        StartOnTheButton(ship.Editor);
        ship.Editor.GoToRoot();
        ship.Tick();
    }

    static void StartOnTheButton(SprocketEditor editor)
    {
        editor.NewButtonSystem("Airlock");
    }

    /// <summary>
    /// A system's switch and the switch of one of it are separate, and both have to hold.
    /// </summary>
    /// <remarks>
    /// Stopping a whole airlock to rewire it, and then starting it again, must not quietly start
    /// the one of it somebody had deliberately left off. Those are two different intentions and
    /// neither may overwrite the other.
    /// </remarks>
    [Fact]
    public void SwitchingASystemDoesNotForgetWhichOfItWasOff()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        var sequence = ship.Automation.Sequences[0];
        var one = editor.Picks.Blueprinted(sequence.System);
        editor.SwitchOn();
        ship.Tick();

        Assert.True(sequence.CanRun(one));


        // Deliberately stop this one of it.
        one.Enabled = false;
        ship.Tick();
        Assert.False(sequence.CanRun(one));

        // Now the whole system, from its own screen, off and on again.
        editor.Back();
        ship.Tick();
        Assert.True(editor.Screen.HasSwitch);

        // A blueprint asks first, since switching it off stops every instance.
        editor.Screen.Flip!.Execute();
        editor.PressChoice(editor.Texts.BlueprintSwitchThemOff);
        ship.Tick();
        Assert.False(sequence.System.Enabled);


        editor.Screen.Flip!.Execute();
        ship.Tick();
        Assert.True(sequence.System.Enabled);
        Assert.False(sequence.CanRun(one));
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
        Assert.True(Assert.IsType<PlusMenu>(Assert.Single(editor.Adders())).Offers);

        editor.SwitchOn();
        ship.Tick();

        // Live: the rows are all still there and the screen has not changed shape, but
        // nothing on it will do anything, and it says why.
        sentence = editor.Sentence();
        Assert.False(sentence.Editable);
        Assert.NotEmpty(sentence.LockedText);
        Assert.False(Assert.IsType<PlusMenu>(Assert.Single(editor.Adders())).Offers);

        // Deleting it is an edit too: the bin stays where it is, cannot be pressed, and says why.
        Assert.Null(editor.Screen.Deletes);
        Assert.Equal(sentence.LockedText, editor.Screen.DeleteLocked);

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
    public void AStepThatTakesTimeIsWaitedForAndOneThatDoesNotIsNot()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
        ship.Tick();

        editor.PressAdd("task");
        editor.PressBlock("Outer Door");
        editor.PressChoice("open");
        editor.PressPrimary();
        ship.Tick();

        var sequence = ship.Automation.Sequences[0];
        Assert.Equal(3, sequence.Steps.Count);
        Assert.IsType<Act>(sequence.Steps[0]);
        Assert.IsType<Act>(sequence.Steps[1]);
        Assert.IsType<Await>(sequence.Steps[2]);

        // A step with nothing to wait for goes straight on, and that is no fault.
        Assert.False(SequenceSummary.Checked(ship.World, ship.Runner.Bindings, sequence, sequence.System.First).Exists);
    }

    [Fact]
    public void UndoAndRedoWalkTheSameList()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
        ship.Tick();

        var sequence = ship.Automation.Sequences[0];

        // Switching a light on takes no time, so nothing waits for it.
        Assert.Single(sequence.Steps);

        // Nothing has been walked back, so there is nothing to put back.
        Assert.NotNull(editor.Screen.Undo);
        Assert.Null(editor.Screen.Redo);
        var added = Filled.With(editor.Texts.ChangeAddStep, "number", "1", "place", SequenceText.Called(editor.Texts, sequence), "thing",
            editor.Reads(0));
        Assert.Equal(added, editor.Screen.UndoText);

        editor.Screen.Undo!.Execute();
        ship.Tick();
        Assert.Empty(sequence.Steps);
        Assert.Equal(added, editor.Screen.RedoText);

        editor.Screen.Redo!.Execute();
        ship.Tick();
        Assert.Single(sequence.Steps);
        var acted = Assert.IsType<Act>(sequence.Steps[0]);
        var block = SystemInstance.Through(sequence.System.First, acted.Targets)[0];
        Assert.Equal("Airlock Light", ship.Runner.Bindings.Entity(block)!.Name);

        // Walk back, then do something different: the road not taken is gone, because putting
        // a step back at an index that now means something else is worse than not offering.
        editor.Screen.Undo!.Execute();
        ship.Tick();
        editor.Name("Let me out");
        ship.Tick();

        Assert.Null(editor.Screen.Redo);
        Assert.Equal(Filled.With(editor.Texts.ChangeRename, "was", SequenceText.DefaultName(editor.Texts, sequence.Number), "now", "Let me out"),
            editor.Screen.UndoText);
    }

    /// <summary>
    /// A step that changes what runs waits for the switch, both ways.
    /// </summary>
    [Fact]
    public void AStepIsNotWalkedWhileItRuns()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
        editor.Screen.Undo!.Execute();
        ship.Tick();

        Assert.NotNull(editor.Screen.Redo);

        editor.SwitchOn();
        ship.Tick();

        // Both steps change what runs, so both wait for the switch.
        Assert.Null(editor.Screen.Undo);
        Assert.Null(editor.Screen.Redo);
    }

    /// <summary>A press held from before a rebuild still does what it did.</summary>
    /// <remarks>The case that matters is the shape changing underneath: a step added between holding it and pressing it.</remarks>
    [Fact]
    public void APressHeldAcrossARebuildStillWorks()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("task");
        editor.PressBlock("Inner Door");
        editor.PressChoice("close");
        editor.PressPrimary();
        ship.Tick();

        var toggle = editor.Screen.Flip;

        editor.PressAdd("task");
        editor.PressBlock("Outer Door");
        editor.PressChoice("open");
        editor.PressPrimary();
        ship.Tick();

        Assert.Equal(4, editor.StepRows().Count);

        Assert.False(ship.Automation.Systems[0].Enabled);
        toggle!.Execute();
        ship.Tick();
        Assert.True(ship.Automation.Systems[0].Enabled);
    }

    /// <summary>Narrowing a list keeps what comes after it: the box being typed into, the kinds, and the next column's rows.</summary>
    /// <remarks>A control made again under somebody would take the caret and the pointer's place with it.</remarks>
    [Fact]
    public void NarrowingAListKeepsWhatComesAfterIt()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("when");
        editor.PressBlock("Inner Door");
        ship.Tick();

        var stages = editor.Pick().Stages;
        var narrow = stages[0].Narrow;
        var kinds = stages[0].Kinds()[0];
        var firstChoice = stages[1].Entries()[0];
        var rows = stages[0].Entries().Count;

        // Type something that leaves fewer rows in the first stage.
        stages[0].Search("inner");
        ship.Tick();

        stages = editor.Pick().Stages;
        Assert.True(stages[0].Entries().Count < rows, "the search did not narrow anything");

        Assert.Same(narrow, stages[0].Narrow);
        Assert.Same(kinds, stages[0].Kinds()[0]);
        Assert.Same(firstChoice, stages[1].Entries()[0]);
    }

    /// <summary>A block picked outside the search stays on the list when another is picked in its place, until the search changes.</summary>
    [Fact]
    public void APickedBlockOutsideTheSearchStaysUntilTheSearchChanges()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("when");
        editor.PressBlock("Inner Door");
        ship.Tick();

        editor.Pick().Stages[0].Search("outer");
        ship.Tick();
        Assert.Contains(Rows(editor), text => text == "Inner Door");

        editor.PressBlock("Outer Door");
        ship.Tick();
        Assert.Contains(Rows(editor), text => text == "Inner Door");

        editor.Pick().Stages[0].Search("oute");
        ship.Tick();
        Assert.DoesNotContain(Rows(editor), text => text == "Inner Door");
        Assert.Contains(Rows(editor), text => text == "Outer Door");
    }

    /// <summary>The names on the rows of the picker's first column.</summary>
    static List<string> Rows(SprocketEditor editor) =>
        editor.Pick().Stages[0].Entries().Select(entry => entry.Text).ToList();

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
        ship.Editor = new SprocketEditor(ship.World, ship.Automation, ship.Runner, new Texts());

        ship.Editor.NewSystem("Airlock");

        // Throws before returning if two blocks claim one command.
        ship.Editor.PressStart("when");

        // Four rows for three blocks: the construct's own programmable block is a block on the
        // grid like any other, and it has a switch, so it is offered like any other.
        var rows = ship.Editor.Pick().Stages[0].Entries();
        Assert.Equal(4, rows.Count);

        // Each row takes its own block, and only that one.
        for (var i = 0; i < rows.Count; i++)
        {
            rows[i].Pick(HeldModifier.Alone, 1);
            Assert.True(rows[i].IsChosen, $"row {i} did not take its own block");
            Assert.Single(rows, r => r.IsChosen);
        }
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
        ship.Tick();

        var system = ship.Automation.Systems[0];
        var sequence = ship.Automation.Sequences[0];

        editor.BackKeeping();
        ship.Tick();

        // On its own screen: renameable, the same way a sequence is.
        Assert.True(editor.Screen.CanRename);
        Assert.Equal("Airlock", editor.Screen.CustomName());
        editor.Name("Front Airlock");
        ship.Tick();
        Assert.Equal("Front Airlock", system.Name);

        editor.SwitchOn();
        ship.Tick();
        Assert.True(sequence.CanRun(sequence.System.First));

        editor.BackKeeping();
        ship.Tick();

        // On the ship's list: the system's own heading carries its switch.
        var heading = editor.RootHeadings()[0];

        Assert.NotNull(heading.Flips);
        Assert.True(heading.SwitchIsOn);

        // A logic system with installations asks before it stops them.
        heading.Flips!.Execute();
        ship.Tick();
        editor.PressChoice("Switch them off");
        ship.Tick();

        Assert.False(system.Enabled);
        Assert.True(system.First!.Enabled);
        Assert.False(sequence.CanRun(sequence.System.First));
        Assert.False(editor.RootHeadings()[0].SwitchIsOn);
    }

    /// <summary>A sequence's row has no switch: the system's is the one switch for everything in it.</summary>
    [Fact]
    public void ASequenceRowHasNoSwitchOfItsOwn()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.Back();
        ship.Tick();

        Assert.Null(editor.Rows()[0].Flips);
    }

    /// <summary>
    /// Deleting a sequence happens at once, and its logic system's undo brings it back with the trigger that runs it.
    /// </summary>
    [Fact]
    public void DeletingASequenceIsUndone()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);
        ship.Tick();

        _ = ship.Editor.Sentence();
        ship.Editor.Screen.Deletes!.Execute();
        ship.Tick();

        // Gone without a question, and standing on the system.
        Assert.Equal("", ship.Editor.Question());
        Assert.Empty(ship.Automation.Sequences);
        Assert.Equal("Airlock", ship.Editor.Screen.Title());
        var system = ship.Automation.Systems[0];
        Assert.Empty(system.Triggers);

        // One step back, on the system, brings it back where it was, still run by its trigger.
        Assert.Equal(Filled.With(ship.Editor.Texts.ChangeDeleteSequence, "thing", SequenceText.DefaultName(ship.Editor.Texts, 1)),
            ship.Editor.Screen.UndoText);
        ship.Editor.Screen.Undo!.Execute();
        ship.Tick();

        var back = Assert.Single(ship.Automation.Sequences);
        Assert.Same(back, Assert.Single(system.Sequences));
        Assert.Equal(back.Number, Assert.Single(system.Triggers).Runs);
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
    /// A new sequence takes the first free number counting on from the sequences its system already has.
    /// </summary>
    /// <remarks>
    /// So the number says which one it is. The numbering is per system because that is the list it has to be told apart in.
    /// </remarks>
    [Fact]
    public void ANewSequenceCountsOnFromThoseThereAre()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);
        ship.Tick();
        Assert.Equal("Sequence 1", ship.Editor.Screen.CustomName());

        AnotherOnTheButton(ship);
        Assert.Equal("Sequence 2", ship.Editor.Screen.CustomName());

        // The first one goes; one is left, so the count starts at two, which is taken.
        var system = ship.Automation.Systems[0];
        ship.Automation.RemoveSequence(system.Sequences[0]);

        AnotherOnTheButton(ship);
        Assert.Equal("Sequence 3", ship.Editor.Screen.CustomName());
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
        ship.Editor.PressStart("when");
        ship.Editor.PressBlock("Airlock Button");
        ship.Editor.PressChoice("button 1 is pressed");
        ship.Editor.PressPrimary();

        // That button is already spoken for, which is asked about before anything else - and here
        // two on one button is the point of the walk.
        ship.Editor.PressChoice("Take it over");
        ship.Tick();
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

        var before = (ActionSlot)ship.Editor.StepRows()[0].Slots[0];
        Assert.Equal("close", before.Action.Text);

        ship.Editor.PressStepSlot(0, 0);
        ship.Tick();
        ship.Editor.PressChoice("open");
        ship.Editor.PressPrimary();
        ship.Tick();

        var after = (ActionSlot)ship.Editor.StepRows()[0].Slots[0];
        Assert.Equal("open", after.Action.Text);

        // A new object, because a target words a slot once and skips it while it is the same one.
        Assert.NotSame(before, after);
    }

    /// <summary>A ship with a button system whose sequence closes the inner door, open on that sequence.</summary>
    static Ship Built()
    {
        var ship = Build();
        StartOnTheButton(ship.Editor);
        ship.Tick();

        ship.Editor.PressAdd("task");
        ship.Editor.PressBlock("Inner Door");
        ship.Editor.PressChoice("close");
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

        ship.Editor.Name(new string('x', NameRules.MaxLength + 20));
        ship.Tick();

        Assert.Equal(NameRules.MaxLength, ship.Editor.Screen.CustomName().Length);
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

        ship.Editor.BackKeeping();

        Assert.Equal("Airlock", ship.Editor.Screen.Title());
        Assert.True(ship.Editor.Screen.CanGoBack);

        ship.Editor.BackKeeping();
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
        ship.Editor.PressStart("when");

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
    /// Blocks of different kinds offer together what every block has, and nothing only one of them has; a list left empty
    /// would say so.
    /// </summary>
    [Fact]
    public void BlocksOfDifferentKindsShareWhatEveryBlockHas()
    {
        var ship = Build();
        ship.Editor.NewSystem("Airlock");
        ship.Editor.PressStart("when");
        ship.Editor.PressBlock("Airlock Button");
        ship.Editor.TickBlock("Inner Door");

        var what = ship.Editor.Pick().Stages[1];
        var said = what.Entries().Select(e => e.Text).ToList();
        Assert.Contains(said, text => text.Contains("starts working"));
        Assert.DoesNotContain(said, text => text.Contains("finishes closing"));
        Assert.NotEqual("", what.EmptyText);
    }

    static string Listed(SprocketEditor editor) =>
        string.Join(", ", editor.Pick().Stages[0].Entries().Select(e => e.Text));

    [Fact]
    public void ItOpensOnTheMachines_NotOnAListOfBlocks()
    {
        var ship = Build();

        // The same screen an established ship gets, with nothing in the list yet - not a
        // different screen that rearranges itself the moment anything is built.
        Assert.Equal("Automation for Test Rig", ship.Editor.Screen.Title());
        Assert.Empty(ship.Editor.RootRows());

        // Both ways in are offered, and neither asks anything first.
        Assert.Equal(new[] { "+ logic system" }, ship.Editor.RootStarts());
        Assert.NotNull(ship.Editor.Screen.Library);
    }

    [Fact]
    public void TheQuestionsComeInReadingOrder()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("when");

        // One pane to begin with; the second appears once it has something to be about.
        Assert.Single(editor.Pick().Stages);
        Assert.Null(editor.Screen.Primary);

        editor.PressBlock("Airlock Button");
        Assert.Equal(2, editor.Pick().Stages.Count);

        // Still nothing to move on to - the path is not finished.
        Assert.Null(editor.Screen.Primary);

        // What happens, and then beside it what the trigger does.
        editor.PressChoice("button 1 is pressed");
        var stages = editor.Pick().Stages;
        Assert.Equal(3, stages.Count);
        Assert.Contains(stages[2].Entries(), e => e.Text == "A new sequence" && e.IsChosen);
        Assert.NotNull(editor.Screen.Primary);

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
        editor.PressStart("when");
        editor.PressBlock("Inner Door");
        editor.TickBlock("Outer Door");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();

        // The two doors picked together are one slot, holding both.
        var starts = Assert.IsType<SignalTrigger>(ship.Automation.Sequences[0].Trigger);
        Assert.Equal(2, ship.Automation.Systems[0].First!.FilledWith(Assert.Single(starts.Sources))!.Count);
        Assert.True(starts.IsWatched);

        editor.PressAdd("task");
        editor.PressBlock("Inner Door");
        editor.TickBlock("Outer Door");
        editor.PressChoice("close");
        editor.PressPrimary();

        var targets = Assert.IsType<EntitiesSlot>(editor.StepRows()[0].Slots[1]);
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
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
        editor.SwitchOn();

        // Back to the system, where the sequence has a row of its own.
        editor.Back();
        ship.Tick();

        var row = editor.Rows()[0];
        Assert.NotNull(row.Runs);

        row.Runs!.Execute();
        ship.Tick(3);

        Assert.Contains("Airlock Light:on", ship.Log);
    }

    /// <summary>A switched-off sequence's play button says to switch its logic system on, and runs it once it is.</summary>
    [Fact]
    public void AParkedSequenceHoldsItsPlayButton()
    {
        var ship = Build();
        var editor = ship.Editor;

        StartOnTheButton(editor);
        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
        editor.PressPrimary();
        ship.Tick();

        Assert.False(ship.Automation.Systems[0].Enabled);
        Assert.Null(editor.Screen.Runs);
        Assert.Equal(editor.Texts.Codex.SequenceRunSystemOff, editor.Screen.RunLocked);

        editor.SwitchSystem(ship.Automation.Systems[0], true);
        ship.Tick();

        Assert.Null(editor.Screen.RunLocked);
        editor.Screen.Runs!.Execute();
        ship.Tick(3);

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

        editor.PressStart("when");
        editor.PressBlock("Inner Door");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();
        editor.Back();

        editor.PressStart("when");
        editor.PressBlock("Outer Door");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();
        editor.Back();

        // Sampled only while they may run, which is what the system's switch is for.
        ship.Automation.Systems[0].Enabled = true;

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
        editor.PressStart("when");
        editor.PressBlock("Inner Door");
        editor.TickBlock("Outer Door");
        editor.PressChoice("finishes opening");
        editor.PressPrimary();

        var starts = Assert.IsType<SignalTrigger>(ship.Automation.Sequences[0].Trigger);
        Assert.Equal(2, ship.Automation.Systems[0].First!.FilledWith(Assert.Single(starts.Sources))!.Count);

        var all = new WhenTrigger("status") { Quantifier = Quantifier.All, Value = Literal.Name("open") };
        foreach (var source in starts.Sources)
            all.Sources.Add(source);
        new ChangeTrigger(starts, all).Apply(ship.Automation.Systems[0]);

        editor.PressAdd("task");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("power");
        editor.Pick().Arguments!.Form.Fields[0].Text = "on";
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
        editor.PressStart("when");

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
        editor.PressStart("when");
        editor.PressBlock("Airlock Button");
        editor.BackKeeping();

        Assert.Empty(ship.Automation.Sequences);

        // And one more back off the system itself.
        editor.BackKeeping();
        Assert.Equal("Automation for Test Rig", editor.Screen.Title());
    }

    [Fact]
    public void BackRetreatsOneQuestionAtATime()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("when");
        Assert.Equal("block slot 1", editor.Screen.Title());

        editor.PressBlock("Airlock Button");
        editor.PressChoice("button 1 is pressed");

        // Both stages are one screen now, and the starters are no longer a screen at all, so
        // one step back reaches the system and a second reaches the ship. Leaving a picker that
        // has been filled in asks what to do with it first, and discarding is what back used to
        // mean on its own.
        editor.Back();
        editor.PressChoice("Discard them");
        Assert.Equal("Airlock", editor.Screen.Title());

        editor.BackKeeping();
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

        editor.NewButtonSystem("Airlock");

        ship.Tick();
        Assert.Equal("Sequence 1", editor.Screen.Title());

        editor.Name("Let me out");
        ship.Tick();

        // The sequence's own screen is what it is called and nothing about why it runs.
        Assert.Single(editor.Screen.Heading);
        Assert.Equal("Let me out", editor.Screen.Heading[0].Text);

        // What starts it is a row of its own, naming the sequence back, and the name it was just
        // given did not touch it.
        editor.BackKeeping();
        ship.Tick();

        Assert.Equal("Airlock", editor.Screen.Title());
        Assert.Equal("Let me out", editor.Rows()[0].Summary);
        Assert.Equal("When signal slot 1 button 1 is pressed", editor.Says(0));
        Assert.Equal("then run Let me out", editor.Doing(0));
    }

    [Fact]
    public void NamingDoesNotRebuildTheScreen()
    {
        // Rebuilding would recreate the text field mid-word and drop the caret.
        var ship = Build();
        var editor = ship.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("every");
        editor.TimerRate().Give("100", true);
        editor.PressPrimary();

        // The view must survive, or its controls are made again and the field loses focus and the caret mid-word.
        var before = editor.Sentence().View;
        editor.Name("Housekeeping");

        Assert.Same(before, editor.Sentence().View);
    }

    [Fact]
    public void AddingAStep_AsksWhatThenAsksHow()
    {
        var ship = Build();
        var editor = ship.Editor;
        StartOnTheButton(editor);

        editor.PressAdd("task");
        Assert.Equal("block slot 1", editor.Screen.Title());
        Assert.Single(editor.Pick().Stages);

        editor.PressBlock("Inner Door");
        Assert.Equal(2, editor.Pick().Stages.Count);

        editor.PressChoice("close");
        editor.PressPrimary();

        // Two rows: the step, and the await after it as a row of its own.
        Assert.Equal(2, editor.StepRows().Count);
        Assert.Equal(StepKind.Await, editor.StepRows()[1].Kind);
        Assert.Equal("close", Assert.IsType<ActionSlot>(editor.StepRows()[0].Slots[0]).Action.Text);

        var targets = Assert.IsType<EntitiesSlot>(editor.StepRows()[0].Slots[1]);
        Assert.Equal(Door(ship, "Inner Door").Id, Assert.Single(targets.Entities));
    }

    [Fact]
    public void BackingOutOfAddingAStep_LeavesNoHalfWrittenLine()
    {
        var ship = Build();
        var editor = ship.Editor;
        StartOnTheButton(editor);

        editor.PressAdd("task");
        editor.PressBlock("Inner Door");
        editor.Back();

        Assert.Empty(editor.StepRows());
    }

    [Fact]
    public void WaitingIsItsOwnKindOfStep_NotAnActionOnABlock()
    {
        // A timed wait has no block to act on, so forcing it through the same
        // question would mean answering one that does not apply.
        var ship = Build();
        var editor = ship.Editor;
        StartOnTheButton(editor);

        editor.PressAdd("delay");
        Assert.NotNull(editor.Lengths());

        editor.GiveLength("5");
        Assert.Equal(300, Assert.IsType<Wait>(Assert.Single(ship.Automation.Sequences[0].Steps)).Ticks);
        ship.Tick();

        var step = Assert.Single(editor.StepRows());
        Assert.Equal(StepKind.Wait, step.Kind);
        Assert.Equal(300, Assert.IsType<DurationSlot>(step.Slots[0]).Length.Ticks);
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

        editor.NewButtonSystem("Airlock");

        editor.SwitchOn();
        ship.Tick();
        Assert.Equal("", editor.Sentence().Bar.LastRun);

        ship.Fire();
        ship.Tick(2);
        Assert.Equal("just now", editor.Sentence().Bar.LastRun);

        // Time passes and nothing else happens, but the reading has moved on.
        ship.Tick(300);
        Assert.Equal("less than a minute ago", editor.Sentence().Bar.LastRun);

        // It fires again, so the count starts over rather than carrying on.
        ship.Fire();
        ship.Tick(2);
        Assert.Equal("just now", editor.Sentence().Bar.LastRun);
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

    /// <summary>
    /// A new logic system is numbered, the first number free, and opens with its name in hand to be given; left without anything
    /// done to it, it goes again.
    /// </summary>
    [Fact]
    public void ANewSystemIsNumberedAndOpensToBeNamed()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.PressStart("logic system");
        Assert.Equal("Logic system 1", ship.Automation.Systems[0].Name);
        Assert.True(editor.Screen.Heading[0].NamedOnArrival);

        editor.GoToRoot();
        Assert.Empty(ship.Automation.Systems);

        editor.PressStart("logic system");
        Assert.Equal("Logic system 1", ship.Automation.Systems[0].Name);
        editor.Screen.Flip!.Execute();

        editor.GoToRoot();
        editor.PressStart("logic system");
        Assert.Equal("Logic system 2", ship.Automation.Systems[1].Name);
    }

    /// <summary>A sequence made on its own or copied, and left without anything done to it, goes again, and so does a new logic system it leaves empty.</summary>
    [Fact]
    public void ASequenceLeftUntouchedIsNotAdded()
    {
        var ship = Build();
        var editor = ship.Editor;

        editor.PressStart("logic system");
        ship.Tick();
        editor.PressStart("sequence");
        ship.Tick();
        Assert.Single(ship.Automation.Sequences);

        editor.Back();
        ship.Tick();
        Assert.Empty(ship.Automation.Sequences);
        Assert.Single(ship.Automation.Systems);

        editor.Back();
        ship.Tick();
        Assert.Empty(ship.Automation.Systems);

        editor.PressStart("logic system");
        ship.Tick();
        editor.PressStart("sequence");
        ship.Tick();
        editor.PressAdd("delay");
        editor.GiveLength("2");
        ship.Tick();

        editor.GoToRoot();
        ship.Tick();
        Assert.Single(ship.Automation.Systems);
        Assert.Single(ship.Automation.Sequences);

        // A copy is made the same way, and goes the same way, with the logic system switched off for it.
        ship.Automation.Systems[0].Enabled = false;
        editor.RootHeadings()[0].Opens!.Execute();
        ship.Tick();
        editor.Rows().Single(row => row.Copies != null).Copies!.Execute();
        ship.Tick();
        Assert.Equal(2, ship.Automation.Sequences.Count);

        editor.Back();
        ship.Tick();
        Assert.Single(ship.Automation.Sequences);
    }

    [Fact]
    public void RenamingASystemToANameAnotherHasIsRefused()
    {
        var ship = Build();
        ship.Editor.Author = "MalTek";

        ship.Editor.NewSystem("Airlock");
        ship.Editor.GoToRoot();
        ship.Editor.NewSystem("Hangar");

        var part = ship.Editor.Screen.Heading[0];
        Assert.False(part.Takes("Airlock"));
        Assert.True(part.Takes("Hangar"));

        part.Renames!("Airlock");
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
        ship.Editor.PressStart("when");

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
        ship.Editor.PressStart("when");

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
        return editor.Pick().Stages[0].Entries()
            .Where(e => e.IsChosen).Select(e => e.Text).ToArray();
    }
}
