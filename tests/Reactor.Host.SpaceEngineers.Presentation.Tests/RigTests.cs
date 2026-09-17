using Reactor.Host.SpaceEngineers.Editing;
using Reactor.Host.SpaceEngineers.Runtime;
using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Host.SpaceEngineers.Storage;
using Reactor.Host.SpaceEngineers.World;
using Reactor.Sim.Machine;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

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
    /// a button that nothing claimed - and Reactor did the right thing with that, which was to drop
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
        Assert.Contains("Reactor_Trigger", store.GetValue(Wiring.Key));

        // And what the program says about it: the construct starts this, which is what the game's
        // own arrangement is. A "when" here would be Reactor claiming it watches the button.
        var program = store.GetValue(ReactorUnit.ProgramKey);
        Assert.Contains("required trigger pressed-1", program);
        Assert.DoesNotContain("when ", program);

        Simulation reopened = Simulation.On(store);
        reopened.Tick();

        // A button is a moment the construct announces, so what the editor wrote is a required
        // trigger naming that button - and that is the half the toolbars have to keep.
        var pressed = Assert.IsType<RequiredTrigger>(reopened.Editor.Program.Sequences[0].Trigger);
        Assert.Single(pressed.Sources);
    }

    /// <summary>
    /// Pressing a wired button runs the sequence, through the slot and nothing else.
    /// </summary>
    /// <remarks>
    /// The whole product in one test: somebody builds a rule, Reactor writes its own action into the
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

        rig.Editor.PressAdd("do something");
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
        SequenceRunner runner = rig.Runner.Get(sequence, sequence.System.Instances[0]);
        Assert.Equal(-1, runner.LastSkippedTick);
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

        var pressed = new RequiredTrigger("pressed-1");
        pressed.Sources.Add(Named(sim, front));

        Sequence cycle = program.AddSequence(system, pressed);
        cycle.Name = "Cycle";

        var close = new Act("close");
        close.Targets.Add(Named(sim, frontDoor));
        cycle.Steps.Add(close);

        // The same system fitted to the port airlock: where it used those blocks, it uses these.
        SystemInstance second = system.AddInstance();
        second.Fill(Named(sim, front), new[] { Named(sim, port) });
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
    /// A press on a button whose sequence is parked does nothing, and the same press works once it is
    /// switched on.
    /// </summary>
    /// <remarks>
    /// The button is Reactor's while the sequence is parked - the slot is written and the panel reads
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

        rig.Editor.PressAdd("do something");
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();

        // Long enough for the slot to be written and looked at again. Left parked, which is where the
        // editor leaves a rule somebody has just finished describing.
        for (var i = 0; i < 5; i++)
            rig.Tick();

        Sequence sequence = rig.Editor.Program.Sequences[0];
        Assert.False(sequence.Enabled);

        rig.Press("Airlock Button");
        rig.Tick();

        Assert.False(Running(rig, sequence), "a parked sequence ran");

        // Refused rather than dropped, which is what the sequence's own screen shows.
        Assert.Equal(1, rig.Runner.Get(sequence, sequence.System.Instances[0]).RefusedCount);

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

        var program = "reactor 4\n\nblock '" + sort + "' door1 [ 2, \"Airlock Inner Door\" ]\n";
        Assert.Equal("", ProgramText.Load(program, rig.Editor.Program, rig.World.Values));

        rig.Tick();

        GridEntity found = rig.Runner.Bindings.Entity("door1");
        Assert.NotNull(found);
        Assert.Equal("Airlock Inner Door", found.Name);

        // In memory it is the definition id, whole, because that is the name the game answers to.
        Assert.Equal("MyObjectBuilder_Door/LargeBlockDoor", found.Kind.Id);

        // Written back the way the language spells it, which is where the prefix comes off.
        var saved = ProgramText.Save(rig.Editor.Program, rig.World.Values);
        Assert.Contains("block 'Door/LargeBlockDoor' door1", saved);
        Assert.DoesNotContain("MyObjectBuilder_", saved);
    }

    /// <summary>Names a block, which is what a program refers to.</summary>
    static string Named(Simulation sim, Fitting fitting)
    {
        GridEntity entity = sim.World.FindEntity(fitting.Id);
        return sim.Editor.Program.Declare(sim.World, entity).Name;
    }

    static void Airlock(ReactorEditor editor)
    {
        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
    }

    static bool Running(Simulation rig, Sequence sequence)
    {
        SequenceRunner runner = rig.Runner.Get(sequence, sequence.System.Instances[0]);
        return runner != null && runner.IsRunning;
    }
}
