using Sprocket.Host.SpaceEngineers.Features.Installations;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Sim.Machine;
using VRage.Game.ModAPI;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The rig the harness invents, and what the mod does on it.
/// </summary>
/// <remarks>
/// These are about the construct rather than about the screens: that closing it and opening it again
/// gives back the same construct, and that a button somebody wired actually runs their sequence. Both
/// go through the game's own arrangement - the program in mod storage, the wiring in the toolbars -
/// and neither could be tested at all until the harness drove the mod's own world layer.
/// </remarks>
public class RigTests
{
    /// <summary>
    /// A button wired in one session is still wired in the next.
    /// </summary>
    /// <remarks>
    /// The playground kept the program and not the toolbars, so the next run loaded a trigger naming
    /// a button that nothing claimed - and Sprocket did the right thing with that, which was to drop
    /// the source. The sentence then read "When … …" and the fault looked like the editor's.
    /// </remarks>
    [Fact]
    public void WiringSurvivesClosingTheConstruct()
    {
        Store store = Store.Nowhere();

        Simulation built = Simulation.On(store);
        Airlock(built.Editor);

        built.Tick();
        built.Save();

        // Which button was chosen is in the toolbars rather than in the program, so this is the half
        // that used to be lost.
        Assert.Contains("Sprocket_Trigger", store.GetValue(Wiring.Key));

        // And what the program says about it: the construct starts this, which is what the game's
        // own arrangement is, through the signal slot the installation fills with the button. A "when" here
        // would be Sprocket claiming it watches the button.
        var program = store.GetValue(BlockStorage.ProgramKey);
        Assert.Contains("whenever 'signal slot 1' then run 1", program);
        Assert.Contains("'signal slot 1' = [ ext.toolbar(1, 1, 1) on [ 'Airlock Button' ] ]", program);
        Assert.DoesNotContain("when ", program);


        Simulation reopened = Simulation.On(store);
        reopened.Tick();

        // A button is a moment the construct announces, so what the editor wrote is a required
        // trigger naming that button - and that is the half the toolbars have to keep.
        var pressed = Assert.IsType<SignalTrigger>(reopened.Editor.Program.Sequences[0].Trigger);
        Assert.Single(pressed.Sources);
    }

    /// <summary>
    /// Pressing a wired button runs the sequence, through the slot and nothing else.
    /// </summary>
    /// <remarks>
    /// The whole product in one test: somebody builds a rule, Sprocket writes its own action into the
    /// button they chose, and pressing that button resolves the slot's key back to their sequence.
    /// <para>
    /// It used to arrive twice - once by the slot invoking the action, and once by the runner
    /// sampling a property the panel does not really have - and the second arrival was silently
    /// refused because the sequence was already running.
    /// </para>
    /// </remarks>
    [Fact]
    public void PressingAWiredButtonRunsTheSequence()
    {
        Simulation rig = Simulation.On(Store.Nowhere());

        Airlock(rig.Editor);

        rig.Editor.PressAdd("task");
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();
        rig.Editor.SwitchOn();

        // Long enough for the slot to be written and looked at again.
        for (var i = 0; i < 5; i++)
            rig.Tick();

        Sequence sequence = rig.Editor.Program.Sequences[0];
        Assert.False(Running(rig, sequence), "it ran without being pressed");

        rig.Press("Airlock Button");
        rig.Tick();

        Assert.True(Running(rig, sequence), "the press did not reach the sequence");

        // And exactly once: a second arrival would be refused for being already running, and the
        // refusal recorded. What made that happen was the panel declaring a readable "pressed"
        // property that nothing could really read; it declares a moment the construct answers now,
        // so there is nothing left to sample.
        SequenceRunner runner = rig.Runner.Get(sequence, sequence.System.First);
        Assert.Equal(-1, runner.LastSkippedTick);
    }

    /// <summary>
    /// A timer's toolbar slot sends a signal as a button does: picked to start a sequence, its slot is written, and the timer
    /// going off runs the sequence.
    /// </summary>
    [Fact]
    public void ATimersToolbarSlotIsATrigger()
    {
        var rig = new Rig();
        TimerBlock timer = rig.Timer(1, "Cycle Timer");
        rig.Door(2, "Inner Door");

        Simulation sim = Simulation.On(rig);
        sim.Editor.NewSystem("Clock");
        sim.Editor.PressStart("when");
        sim.Editor.PressBlock("Cycle Timer");
        sim.Editor.PressChoice("goes off");
        sim.Editor.PressPrimary();

        sim.Editor.PressAdd("task");
        sim.Editor.PressBlock("Inner Door");
        sim.Editor.PressChoice("open");
        sim.Editor.PressPrimary();
        sim.Editor.SwitchOn();

        for (var i = 0; i < 5; i++)
            sim.Tick();

        var pressed = Assert.IsType<SignalTrigger>(sim.Editor.Program.Sequences[0].Trigger);
        var at = sim.Editor.Program.Systems[0].First!.Positions(pressed.Part)![0];
        Assert.Equal(1, at.Slot);
        Assert.True(timer.Toolbar.Holds(0), "the timer's slot was not written");

        rig.Forget();
        timer.GoOff();
        sim.Tick();

        Assert.Contains("Inner Door:open", rig.Log);
    }

    /// <summary>Two sequences started by one timer each take a slot of their own, and both run when it goes off.</summary>
    [Fact]
    public void TwoSequencesOnOneTimerBothRun()
    {
        var rig = new Rig();
        TimerBlock timer = rig.Timer(1, "Cycle Timer");
        rig.Door(2, "Inner Door");
        rig.Door(3, "Outer Door");

        Simulation sim = Simulation.On(rig);
        sim.Editor.NewSystem("Clock");
        foreach (var door in new[] { "Inner Door", "Outer Door" })
        {
            sim.Editor.PressStart("when");
            sim.Editor.PressBlock("Cycle Timer");
            sim.Editor.PressChoice("goes off");
            sim.Editor.PressPrimary();
            sim.Editor.PressAdd("task");
            sim.Editor.PressBlock(door);
            sim.Editor.PressChoice("open");
            sim.Editor.PressPrimary();
            sim.Editor.BackKeeping();
            sim.Tick();
        }

        sim.Editor.SwitchOn();
        for (var i = 0; i < 5; i++)
            sim.Tick();

        Assert.False(sim.Editor.HasChoice("Take it over"), "the second trigger was asked about a slot it need not share");
        Assert.True(timer.Toolbar.Holds(0));
        Assert.True(timer.Toolbar.Holds(1));

        rig.Forget();
        timer.GoOff();
        sim.Tick();

        Assert.Contains("Inner Door:open", rig.Log);
        Assert.Contains("Outer Door:open", rig.Log);
    }

    /// <summary>A signal slot's picker offers a block's signals and nothing else it can do.</summary>

    [Fact]
    public void ATriggerSlotOffersOnlyTriggers()
    {
        var rig = new Rig();
        rig.Timer(1, "Cycle Timer");
        rig.Door(2, "Inner Door");

        Simulation sim = Simulation.On(rig);
        sim.Editor.NewSystem("Clock");
        sim.Editor.PressStart("when");
        sim.Editor.PressBlock("Cycle Timer");
        Assert.True(sim.Editor.HasChoice("turns on"), "the timer offers nothing but triggers, so this proves nothing");
        sim.Editor.PressChoice("goes off");
        sim.Editor.PressPrimary();
        sim.Tick();

        AutomationSystem system = sim.Editor.Program.Systems[0];
        sim.Editor.Picks.Blueprinted(system);
        sim.Editor.Push(new BlueprintPage(sim.Editor, system));
        sim.Tick();

        sim.Editor.Parts()[0].Fills!.Execute();
        sim.Tick();

        Assert.DoesNotContain(sim.Editor.Offered(), row => row.Text == "Inner Door");
        sim.Editor.PressBlock("Cycle Timer");
        Assert.True(sim.Editor.HasChoice("goes off"));
        Assert.False(sim.Editor.HasChoice("turns on"), "a signal Sprocket watches was offered as a toolbar signal");
    }

    /// <summary>
    /// Two of a system are two buttons, and each one starts its own.
    /// </summary>

    /// <remarks>
    /// The other half of what a slot carries. A slot naming only a sequence could say "cycle the
    /// airlock" and never which airlock, so the port button ran the front one - and on a ship with
    /// two of anything that is the whole feature failing quietly.
    /// <para>
    /// Written as a program rather than through the screens because fitting a second one of a
    /// system is a flow of its own; what is under test is that the unit wires a button per one of
    /// it and that a press resolves back to the right one.
    /// </para>
    /// </remarks>
    [Fact]
    public void EachOneOfASystemHasItsOwnButton()
    {
        var rig = new Rig();
        Panel front = rig.Panel(1, "Front Button");
        Door frontDoor = rig.Door(2, "Front Inner");
        Panel port = rig.Panel(11, "Port Button");
        Door portDoor = rig.Door(12, "Port Inner");

        Simulation sim = Simulation.On(rig);
        Automation program = sim.Editor.Program;

        AutomationSystem system = program.AddSystem();
        system.Name = "Airlock";
        system.IsBlueprint = true;
        system.AddInstance();

        var pressed = Pressed.On(program, system, Named(sim, front));

        Sequence cycle = program.AddSequence(system, pressed);
        cycle.Name = "Cycle";

        var close = new Act("close");
        close.Targets.Add(Named(sim, frontDoor));
        cycle.Steps.Add(close);
        cycle.Steps.Add(new Await());

        // The same system fitted to the port airlock: where it used those blocks, it uses these.
        SystemInstance second = system.AddInstance();
        second.Fill(pressed.Part, new[] { Named(sim, port) }, new[] { new ToolbarSlot(1, 1, 1) });
        second.Fill(Named(sim, frontDoor), new[] { Named(sim, portDoor) });

        frontDoor.ForceOpen();
        portDoor.ForceOpen();

        // Long enough for both slots to be written and looked at again.
        for (var i = 0; i < 5; i++)
            sim.Tick();

        rig.Forget();

        sim.Press("Port Button");
        sim.Tick();

        Assert.Equal(new[] { "Port Inner:close" }, rig.Log);

        rig.Forget();
        sim.Press("Front Button");
        sim.Tick();

        Assert.Equal(new[] { "Front Inner:close" }, rig.Log);
    }

    /// <summary>
    /// A button whose trigger acts on two blocks itself is Sprocket's to run, keyed by its signal part, and each blockset's
    /// button acts on that blockset's blocks.
    /// </summary>
    [Fact]
    public void ATriggerThatActsOnSeveralBlocksIsSprocketsToRun()
    {
        var rig = new Rig();
        Panel front = rig.Panel(1, "Front Button");
        Door frontInner = rig.Door(2, "Front Inner");
        Door frontOuter = rig.Door(3, "Front Outer");
        Panel port = rig.Panel(11, "Port Button");
        Door portInner = rig.Door(12, "Port Inner");
        Door portOuter = rig.Door(13, "Port Outer");

        Simulation sim = Simulation.On(rig);
        Automation program = sim.Editor.Program;

        AutomationSystem system = program.AddSystem();
        system.Name = "Airlock";
        system.IsBlueprint = true;
        system.AddInstance();

        var pressed = Pressed.On(program, system, Named(sim, front));
        var close = new Act("close");
        close.Targets.Add(Named(sim, frontInner));
        close.Targets.Add(Named(sim, frontOuter));
        pressed.Does = close;
        system.AddTrigger(pressed);

        SystemInstance second = system.AddInstance();
        second.Fill(pressed.Part, new[] { Named(sim, port) }, new[] { new ToolbarSlot(1, 1, 1) });
        second.Fill(Named(sim, frontInner), new[] { Named(sim, portInner) });
        second.Fill(Named(sim, frontOuter), new[] { Named(sim, portOuter) });
        program.Edited();
        program.Rewire();

        frontInner.ForceOpen();
        frontOuter.ForceOpen();
        portInner.ForceOpen();
        portOuter.ForceOpen();

        for (var i = 0; i < 5; i++)
            sim.Tick();

        MyToolbarItemInfo held;
        Assert.True(port.Toolbar.TryGetItem(0, out held), "the port button holds nothing");
        Assert.True(SignalAction.IsOurs(held), "two doors went into the slot as the game's own action");

        rig.Forget();
        sim.Press("Port Button");
        sim.Tick();

        Assert.Equal(new[] { "Port Inner:close", "Port Outer:close" }, rig.Log.OrderBy(line => line));
    }

    /// <summary>
    /// A blockset answers the trigger with any button of any panel, and only that button starts it.
    /// </summary>
    [Fact]
    public void ABlocksetMayAnswerWithAnotherButton()
    {
        var rig = new Rig();
        Panel front = rig.Panel(1, "Front Button");
        Door frontDoor = rig.Door(2, "Front Inner");
        Panel four = rig.Panel(11, "Four Button", 4);
        Door portDoor = rig.Door(12, "Port Inner");

        Simulation sim = Simulation.On(rig);
        Automation program = sim.Editor.Program;

        AutomationSystem system = program.AddSystem();
        system.Name = "Airlock";
        system.IsBlueprint = true;
        system.AddInstance();

        var pressed = Pressed.On(program, system, Named(sim, front));

        Sequence cycle = program.AddSequence(system, pressed);
        cycle.Name = "Cycle";

        var close = new Act("close");
        close.Targets.Add(Named(sim, frontDoor));
        cycle.Steps.Add(close);
        cycle.Steps.Add(new Await());

        SystemInstance second = system.AddInstance();
        second.Fill(pressed.Part, new[] { Named(sim, four) }, new[] { new ToolbarSlot(1, 1, 3) });
        second.Fill(Named(sim, frontDoor), new[] { Named(sim, portDoor) });

        frontDoor.ForceOpen();
        portDoor.ForceOpen();

        for (var i = 0; i < 5; i++)
            sim.Tick();

        rig.Forget();

        four.Press(0);
        sim.Tick();

        Assert.Empty(rig.Log);

        four.Press(2);
        sim.Tick();

        Assert.Equal(new[] { "Port Inner:close" }, rig.Log);
    }

    /// <summary>
    /// A press on a button whose sequence is parked does nothing, and the same press works once it is
    /// switched on.
    /// </summary>
    /// <remarks>
    /// The button is Sprocket's while the sequence is parked - the slot is written and the panel reads
    /// as what it does - because what the program wants wired is not a question about a switch. What
    /// the switch answers is whether anything runs.
    /// <para>
    /// Which is also the state a new rule is in: the editor parks the one it has just built, so
    /// pressing the button before switching it on is the ordinary first thing somebody does.
    /// </para>
    /// </remarks>
    [Fact]
    public void APressOnAParkedSequenceStartsNothing()
    {
        Simulation rig = Simulation.On(Store.Nowhere());

        Airlock(rig.Editor);

        rig.Editor.PressAdd("task");
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();

        // Long enough for the slot to be written and looked at again. Left switched off, which is where
        // the editor leaves a system somebody has just finished describing.
        for (var i = 0; i < 5; i++)
            rig.Tick();

        Sequence sequence = rig.Editor.Program.Sequences[0];
        Assert.False(sequence.System.Enabled);

        rig.Press("Airlock Button");
        rig.Tick();

        Assert.False(Running(rig, sequence), "a parked sequence ran");

        // Refused rather than dropped, which is what the sequence's own screen shows.
        Assert.Equal(1, rig.Runner.Get(sequence, sequence.System.First).RefusedCount);

        rig.Editor.SwitchOn();
        rig.Tick();

        rig.Press("Airlock Button");
        rig.Tick();

        Assert.True(Running(rig, sequence), "the press did not reach the sequence");
    }

    /// <summary>
    /// A declaration finds its block whether or not it carries the game's type prefix.
    /// </summary>
    /// <remarks>
    /// The language spells a block's sort without <c>MyObjectBuilder_</c>, the same way a part's
    /// <c>expects</c> does, and a declaration says which sort it points at so that a block of another
    /// sort cannot answer it. The kind minted from a real block kept the game's prefix, so a program
    /// written by hand against the spec found nothing at all: every reference read as something that
    /// is not here any more. The prefixed spelling still reads, because saves already carry it.
    /// </remarks>
    [Theory]
    [InlineData("Door/LargeBlockDoor")]
    [InlineData("MyObjectBuilder_Door/LargeBlockDoor")]
    public void ADeclarationFindsItsBlockWithOrWithoutTheGamesPrefix(string sort)
    {
        Simulation rig = Simulation.On(Store.Nowhere());
        rig.Tick();

        var program = "sprocket 1 alpha 18\n\nlet door1 = block '" + sort + "' [ 2, \"Airlock Inner Door\" ]\n\n"
            + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as Airlock\n{\n    sequence 1 {\n        call ext.close on [ door1 ]\n    }\n}\n";

        Assert.Equal("", ProgramReader.Load(program, rig.Editor.Program, rig.World.Values));

        rig.Tick();

        GridEntity found = rig.Runner.Bindings.Entity("door1");
        Assert.NotNull(found);
        Assert.Equal("Airlock Inner Door", found.Name);

        // In memory it is the definition id, whole, because that is the name the game answers to.
        Assert.Equal("MyObjectBuilder_Door/LargeBlockDoor", found.Kind.Id);

        // Written back the way the language spells it, which is where the prefix comes off.
        var saved = ProgramWriter.Save(rig.Editor.Program, rig.World.Values);
        Assert.Contains("let door1 = block 'Door/LargeBlockDoor'", saved);
        Assert.DoesNotContain("MyObjectBuilder_", saved);
    }

    /// <summary>Names a block, which is what a program refers to.</summary>
    static string Named(Simulation sim, Fitting fitting)
    {
        GridEntity entity = sim.World.FindEntity(fitting.Id);
        return sim.Editor.Program.Declare(sim.World, entity).Name;
    }

    /// <summary>A trigger that does one plain thing itself, on a system, the way the picker makes one; and what the runtime runs that act in.</summary>
    static Sequence PlainTrigger(Simulation rig)
    {
        Automation program = rig.Editor.Program;
        AutomationSystem system = program.AddSystem();
        system.Name = "Airlock";

        var open = new Act("open");
        open.Targets.Add(program.Declare(rig.World, rig.World.FindEntity(2)).Name);

        var pressed = Pressed.On(program, system, program.Declare(rig.World, rig.World.FindEntity(21)).Name);
        pressed.Does = open;
        system.AddTrigger(pressed);
        program.Edited();
        program.Rewire();

        return rig.Editor.Runner.Alone(system, pressed)!;
    }

    /// <summary>
    /// A button still wired to a program this block no longer holds is taken over by the one it does.
    /// </summary>
    /// <remarks>
    /// Replacing a program replaces what the block holds and leaves the buttons in the world as they
    /// were, still carrying this block's trigger with a key from the program that has gone. Nothing
    /// answers that key any more, so it is nobody's decision about the button: it is this block's own
    /// leftover, and a trigger that picks the button has to be able to have it.
    /// </remarks>
    [Fact]
    public void AButtonStillWiredToThisBlocksOldProgramIsTakenOver()
    {
        Simulation rig = Simulation.Fresh();
        rig.Tick();

        // What the program before this one wrote into the button, and never took back.
        Panel button = Named(rig, "Airlock Button");
        Toolbar.Changes changes = button.Toolbar.Changing();
        changes.Set(0, MyToolbarItemInfo.BlockAction(rig.Machine.Sprocket.Id, SignalAction.ActionId),
            SignalAction.Argument("00000000-0000-0000-0000-000000000000/1/1"));
        Assert.True(changes.Apply());

        Airlock(rig.Editor);
        rig.Editor.PressAdd("task");
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();
        rig.Editor.SwitchOn();

        for (var i = 0; i < 5; i++)
            rig.Tick();

        Sequence sequence = rig.Editor.Program.Sequences[0];
        Assert.Single(((SignalTrigger)sequence.System.Triggers[0]).Sources);

        // Written over, and never taken for somebody else's wiring.
        Assert.Contains(rig.Log, line => line.StartsWith("wired slot 0", StringComparison.Ordinal));
        Assert.DoesNotContain(rig.Log, line => line.StartsWith("dropped", StringComparison.Ordinal));

        rig.Press("Airlock Button");
        rig.Tick();

        Assert.True(Running(rig, sequence), "the button still answers the program that has gone");
    }

    /// <summary>
    /// Two buttons still wired to the old program can both be given to one sequence of the new one.
    /// </summary>
    /// <remarks>
    /// Two triggers running one sequence need two slots running it. A button left holding the old key
    /// and skipped as already done leaves the count one short, and then neither trigger can start
    /// anything.
    /// </remarks>
    [Fact]
    public void TwoButtonsFromTheOldProgramCanBothStartOneSequence()
    {
        Simulation rig = Simulation.Fresh();
        rig.Machine.Panel(30, "Inside Button");
        rig.Tick();

        foreach (var name in new[] { "Airlock Button", "Inside Button" })
        {
            Toolbar.Changes changes = Named(rig, name).Toolbar.Changing();
            changes.Set(0, MyToolbarItemInfo.BlockAction(rig.Machine.Sprocket.Id, SignalAction.ActionId),
                SignalAction.Argument("00000000-0000-0000-0000-000000000000/1/1"));
            Assert.True(changes.Apply());
        }

        Airlock(rig.Editor);
        rig.Editor.PressAdd("task");
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();
        rig.Editor.BackKeeping();

        Sequence sequence = rig.Editor.Program.Sequences[0];

        rig.Editor.PressStart("when");
        rig.Editor.PressBlock("Inside Button");
        rig.Editor.PressChoice("button 1 is pressed");
        rig.Editor.PressChoice(sequence.Name);
        rig.Editor.PressPrimary();
        rig.Editor.SwitchOn();

        for (var i = 0; i < 5; i++)
            rig.Tick();

        Assert.True(rig.World.Unstartable.CanStart(sequence, sequence.System.First), "nothing can start the sequence both buttons were given");

        rig.Press("Inside Button");
        rig.Tick();

        Assert.True(Running(rig, sequence), "the inside button still answers the program that has gone");
    }

    static Panel Named(Simulation rig, string name)
    {
        foreach (Panel found in rig.Panels())
            if (found.Name == name)
                return found;

        throw new InvalidOperationException("no panel called " + name);
    }

    /// <summary>A trigger that does one plain thing is Sprocket's own, and stops when its system is switched off.</summary>
    [Fact]
    public void ATriggerThatDoesOneThingIsSprocketsOwn()
    {
        Simulation rig = Simulation.Fresh();
        rig.Tick();

        Sequence sequence = PlainTrigger(rig);

        for (var i = 0; i < 5; i++)
            rig.Tick();

        MyToolbarItemInfo held;
        Assert.True(Named(rig, "Airlock Button").Toolbar.TryGetItem(0, out held), "the button holds nothing");
        Assert.True(SignalAction.IsOurs(held), "the button holds the game's own action");

        rig.Machine.Log.Clear();
        rig.Press("Airlock Button");
        for (var i = 0; i < 3; i++)
            rig.Tick();

        Assert.Contains("Airlock Inner Door:open", rig.Machine.Log);

        sequence.System.Enabled = false;
        rig.Machine.Log.Clear();
        rig.Press("Airlock Button");
        for (var i = 0; i < 3; i++)
            rig.Tick();

        Assert.DoesNotContain("Airlock Inner Door:open", rig.Machine.Log);
    }

    /// <summary>
    /// A slot somebody has changed leaves the trigger naming its button, and the sequence says the slot holds something
    /// else.
    /// </summary>
    /// <remarks>The trigger is the reference; what it names no longer answers, until picking the button again writes it back.</remarks>
    [Fact]
    public void ASlotSomebodyChangedLeavesTheTriggerNamingItAndSaysSo()
    {
        Simulation rig = Simulation.Fresh();
        rig.Tick();

        Sequence sequence = PlainTrigger(rig);

        for (var i = 0; i < 5; i++)
            rig.Tick();

        var trigger = (SignalTrigger)sequence.System.Triggers[0];
        var filled = Pressed.By(rig.Editor.Program, sequence.System, trigger.Part)!.ToList();

        // Somebody points the button at the other door instead.
        Panel panel = Named(rig, "Airlock Button");
        Toolbar.Changes changes = panel.Toolbar.Changing();
        changes.Set(0, MyToolbarItemInfo.BlockAction(3, "Open_On"), null);
        Assert.True(changes.Apply());

        // Sprocket looks again when the program changes, which is when a screen is doing anything.
        rig.Editor.Program.Rewire();
        for (var i = 0; i < 3; i++)
            rig.Tick();

        Assert.Equal(filled, Pressed.By(rig.Editor.Program, sequence.System, trigger.Part));
        Assert.False(rig.World.Unstartable.CanStart(sequence, sequence.System.First));
        Assert.Equal(Unwired.Taken, rig.World.Unstartable.Why(sequence, sequence.System.First)!.Why);
    }

    /// <summary>
    /// A button can stop a sequence, and its slot says so.
    /// </summary>
    /// <remarks>
    /// The one thing a sequence cannot do for itself: a cycle half way through has no line in it
    /// that says "unless somebody says otherwise". The slot names the sequence with the stopping
    /// verb on the end, so a button written before stopping existed still means what it meant.
    /// </remarks>
    [Fact]
    public void AButtonCanStopASequence()
    {
        Simulation rig = Simulation.Fresh();
        rig.Machine.Panel(31, "Stop Button");
        rig.Tick();

        rig.Editor.NewButtonSystem("Airlock");
        // A wait, so the run lasts long enough to be stopped.
        rig.Editor.PressAdd("delay");
        rig.Editor.GiveLength("10");
        rig.Editor.SwitchOn();

        Sequence sequence = rig.Editor.Program.Sequences[0];

        var halt = Pressed.On(rig.Editor.Program, sequence.System, rig.Editor.Program.Declare(rig.World, rig.World.FindEntity(31)).Name);
        halt.Does = new Run(sequence.Number, true);
        sequence.System.AddTrigger(halt);
        rig.Editor.Program.Rewire();

        for (var i = 0; i < 5; i++)
            rig.Tick();

        rig.Press("Airlock Button");
        rig.Tick();
        Assert.True(Running(rig, sequence), "the sequence never started");

        rig.Press("Stop Button");
        rig.Tick();

        Assert.False(Running(rig, sequence), "the button did not stop it");
    }

    /// <summary>
    /// Two buttons can start one sequence, and both get their slot written.
    /// </summary>
    /// <remarks>
    /// The inside button and the outside one, which is the case that made somebody duplicate a
    /// sequence instead. The runtime always sampled every trigger of a sequence; what only ever
    /// reached a toolbar was the first of them, so the second button held nothing and did nothing.
    /// </remarks>
    [Fact]
    public void TwoButtonsCanStartOneSequence()
    {
        Simulation rig = Simulation.Fresh();
        rig.Machine.Panel(30, "Inside Button");

        // A tick, so the world has seen the panel: a reference declared against a block the world
        // does not know yet binds to nothing, and nothing is what would then be wired.
        rig.Tick();

        Airlock(rig.Editor);
        rig.Editor.PressAdd("task");
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();
        rig.Editor.SwitchOn();

        Sequence sequence = rig.Editor.Program.Sequences[0];

        // The second way in, written into the program rather than through the screens: the editor
        // has no way to add one yet, which is what #14 is for.
        GridEntity button = rig.World.FindEntity(30);
        Assert.NotNull(button);
        var inside = Pressed.On(rig.Editor.Program, sequence.System, rig.Editor.Program.Declare(rig.World, button).Name);
        inside.Does = new Run(sequence.Number);
        sequence.System.AddTrigger(inside);
        rig.Editor.Program.Rewire();

        for (var i = 0; i < 5; i++)
            rig.Tick();

        rig.Press("Inside Button");
        rig.Tick();

        Assert.True(Running(rig, sequence), "the second button did not reach the sequence");
    }

    /// <summary>
    /// Cutting the power to the machine itself holds its automation, and restoring it lets it go on.
    /// </summary>
    /// <remarks>
    /// The rule is in the core tests; what this one holds is the wiring. The unit asks the block
    /// whether it is working once and then listens for the block saying so, rather than asking sixty
    /// times a second - so if the listening were dropped, nothing else would notice.
    /// </remarks>
    [Fact]
    public void CuttingThePowerToTheMachineHoldsItsAutomation()
    {
        Simulation rig = Simulation.Fresh();

        rig.Editor.NewButtonSystem("Airlock");
        rig.Editor.PressAdd("task");
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();
        rig.Editor.SwitchOn();

        for (var i = 0; i < 5; i++)
            rig.Tick();

        Assert.True(rig.Runner.Live, "a working machine is not live");

        rig.Machine.Sprocket.Enabled = false;
        rig.Tick();

        Assert.False(rig.Runner.Live, "the machine lost power and the automation did not hear");

        Sequence sequence = rig.Editor.Program.Sequences[0];
        rig.Press("Airlock Button");
        rig.Tick();

        Assert.False(Running(rig, sequence), "it ran on a machine with no power");

        rig.Machine.Sprocket.Enabled = true;
        rig.Press("Airlock Button");
        rig.Tick();

        Assert.True(rig.Runner.Live);
        Assert.True(Running(rig, sequence), "the power came back and the press did nothing");
    }

    /// <summary>
    /// Taking a button over leaves it wired to the trigger that took it.
    /// </summary>
    /// <remarks>
    /// Two triggers naming one button is a state the construct cannot be put into, and Sprocket asked
    /// to arrange both arranges neither: the slot ends up empty, both triggers name a button that
    /// answers nothing, and the host then takes the block out of one of them because the button it
    /// names is not its own. That is a trigger with nothing left on its row to press.
    /// <para>
    /// So the answer decides it. What is asserted here is the construct rather than the program: the
    /// slot holds Sprocket's action, and the sequence that took it can be started. In a logic system a
    /// trigger is a variable holding its button, so the trigger that lost it goes.
    /// </para>
    /// </remarks>
    [Fact]
    public void TakingAButtonOverRewiresIt()
    {
        Simulation rig = Simulation.On(Store.Nowhere());
        Airlock(rig.Editor);

        for (var i = 0; i < 20; i++)
            rig.Tick();

        rig.Editor.BackKeeping();
        rig.Editor.PressStart("when");
        rig.Editor.PressBlock("Airlock Button");
        rig.Editor.PressChoice("button 1 is pressed");
        rig.Editor.PressPrimary();
        rig.Editor.PressChoice("Take it over");

        for (var i = 0; i < 60; i++)
            rig.Tick();

        Panel panel = null!;
        foreach (Panel found in rig.Panels())
            if (found.Name == "Airlock Button")
                panel = found;

        Assert.NotNull(panel);

        Assert.True(panel.Toolbar.TryGetItem(0, out _), "the button ended up wired to nothing");

        Sequence had = rig.Editor.Program.Sequences[0];
        Sequence took = rig.Editor.Program.Sequences[1];

        Assert.True(rig.World.Unstartable.CanStart(took, took.System.First), "the trigger that took the button cannot start");

        var left = Assert.Single(rig.Editor.Program.Systems[0].Triggers);
        Assert.True(left.IsAbout(took), "the trigger that lost the button is still there");
        Assert.False(left.IsAbout(had));
    }


    /// <summary>
    /// Cutting the block Sprocket runs in and pasting it back keeps the buttons working.
    /// </summary>
    /// <remarks>
    /// The arrival is a different block as far as the game is concerned, so every slot on the
    /// construct names one that is gone. Sprocket wrote those slots and puts its own block into the
    /// action it writes, so it can see that for itself and say the id again.
    /// <para>
    /// What it did instead was ask whether the action in the slot was its own <em>by name</em>, find
    /// that it was in every one of them, and report the whole construct as wired while not one
    /// button did anything. A report that says everything is fine is worse than no report.
    /// </para>
    /// </remarks>
    [Fact]
    public void CuttingAndPastingTheBlockKeepsTheButtons()
    {
        Store store = Store.Nowhere();

        Simulation built = Simulation.On(store);
        Airlock(built.Editor);

        built.Editor.PressAdd("task");
        built.Editor.PressBlock("Airlock Inner Door");
        built.Editor.PressChoice("open");
        built.Editor.PressPrimary();
        built.Editor.SwitchOn();

        for (var i = 0; i < 5; i++)
            built.Tick();

        built.Save();

        Simulation pasted = Simulation.Pasted(store);
        for (var i = 0; i < 120; i++)
            pasted.Tick();

        Sequence sequence = pasted.Editor.Program.Sequences[0];

        // Still says what starts it, and the construct agrees that something does.
        var pressed = Assert.IsType<SignalTrigger>(sequence.Trigger);
        Assert.Single(pressed.Sources);
        Assert.True(pasted.World.Unstartable.CanStart(sequence, sequence.System.First), "the trigger was reported as unstartable");

        // And the button does it, which is the half that was being reported without being true.
        pasted.Press("Airlock Button");
        pasted.Tick();

        Assert.True(Running(pasted, sequence), "the press did not reach the sequence");
    }

    /// <summary>
    /// A button held by a Sprocket that is still there is left alone.
    /// </summary>
    /// <remarks>
    /// The other half, and the reason the mend cannot simply take every slot that carries Sprocket's
    /// action: a second programmable block holding the same systems is a legitimate thing to build,
    /// as redundancy, and the copy helping itself to the original's buttons would be two Sprockets
    /// trading one slot back and forth forever.
    /// <para>
    /// What decides it is whether the block the slot names is still in the world. Here it is - and
    /// it is not this Sprocket - so the slot stays as it is and the sequence reports that nothing
    /// starts it, which is true and is mended by choosing a button.
    /// </para>
    /// </remarks>
    [Fact]
    public void AButtonHeldBySomeoneStillThereIsLeftAlone()
    {
        Store store = Store.Nowhere();

        Simulation built = Simulation.On(store);
        Airlock(built.Editor);

        built.Editor.PressAdd("task");
        built.Editor.PressBlock("Airlock Inner Door");
        built.Editor.PressChoice("open");
        built.Editor.PressPrimary();
        built.Editor.SwitchOn();

        for (var i = 0; i < 5; i++)
            built.Tick();

        built.Save();

        // Sprocket's own action, naming a block that is alive and is neither the slot's own block nor the Sprocket opening
        // this construct - which is what a slot written by the other holder of these systems looks like.
        string wired = store.GetValue(Wiring.Key);
        store.SetValue(Wiring.Key, System.Text.RegularExpressions.Regex.Replace(wired, @"^(\d+ \d+ )\d+( " + SignalAction.ActionId + ")",
            "${1}2$2", System.Text.RegularExpressions.RegexOptions.Multiline));

        Simulation pasted = Simulation.Pasted(store);
        for (var i = 0; i < 120; i++)
            pasted.Tick();

        Sequence sequence = pasted.Editor.Program.Sequences[0];
        Assert.False(pasted.World.Unstartable.CanStart(sequence, sequence.System.First), "it claimed a button somebody else holds");

        // And it says whose button, and why.
        var fault = SequenceSummary.Fault(pasted.Runner.Get(sequence, sequence.System.First));
        Assert.Equal("Airlock Button's button was changed in its toolbar.", SequenceText.Problem(new Texts(), fault, sequence));

        Panel panel = null!;
        foreach (Panel found in pasted.Panels())
            if (found.Name == "Airlock Button")
                panel = found;

        MyToolbarItemInfo held;
        Assert.True(panel.Toolbar.TryGetItem(0, out held), "the slot was emptied");
        Assert.Equal(2, held.BlockEntityId);
    }

    static void Airlock(SprocketEditor editor)
    {
        editor.NewButtonSystem("Airlock");
    }

    static bool Running(Simulation rig, Sequence sequence)
    {
        SequenceRunner runner = rig.Runner.Get(sequence, sequence.System.First);
        return runner != null && runner.IsRunning;
    }
}
