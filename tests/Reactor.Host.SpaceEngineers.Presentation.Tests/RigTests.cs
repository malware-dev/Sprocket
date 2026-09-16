using Reactor.Host.SpaceEngineers.Core.Editing;
using Reactor.Host.SpaceEngineers.Core.Runtime;
using Reactor.Host.SpaceEngineers.Core.Sequences;
using Reactor.Host.SpaceEngineers.Core.World;
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

        Simulation reopened = Simulation.On(store);
        reopened.Tick();

        var when = Assert.IsType<WhenTrigger>(reopened.Editor.Program.Sequences[0].Trigger);
        Assert.Single(when.Sources);
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

        var when = new WhenTrigger("pressed-1");
        when.Sources.Add(Named(sim, front));

        Sequence cycle = program.AddSequence(system, when);
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

    /// <summary>Names a block, which is what a program refers to.</summary>
    static string Named(Simulation sim, Fitting fitting)
    {
        IEntity entity = sim.World.FindEntity(fitting.Id);
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
