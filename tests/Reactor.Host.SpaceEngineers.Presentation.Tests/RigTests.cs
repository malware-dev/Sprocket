using Reactor.Host.SpaceEngineers.Core.Editing;
using Reactor.Host.SpaceEngineers.Core.Runtime;
using Reactor.Host.SpaceEngineers.Core.Sequences;
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
