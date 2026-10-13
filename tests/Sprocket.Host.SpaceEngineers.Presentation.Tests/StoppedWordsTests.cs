using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A sequence stopped from outside says so in its status, with the step it was at, in place of "ready".</summary>
public class StoppedWordsTests
{
    /// <summary>Stopped by hand while it waits on its first step, it says it was stopped at step 1 by hand.</summary>
    [Fact]
    public void AStopByHandIsSaidWithItsStep()
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Tick();

        var system = rig.Editor.Program.Systems[0];
        var sequence = system.Sequences[0];
        var door = rig.World.Entities.First(entity => entity.Name == "Airlock Outer Door");
        var act = new Act("open");
        act.Targets.Add(rig.Editor.Program.Declare(rig.World, door).Name);
        sequence.Steps.Clear();
        sequence.Steps.Add(act);
        sequence.Steps.Add(new Await());
        system.Enabled = false;
        rig.Editor.Program.Edited();

        rig.Editor.GoToRoot();
        rig.Tick();
        rig.Editor.Push(new SequencePage(rig.Editor, sequence));
        var page = rig.Editor.Sentence();
        rig.Tick();

        rig.Editor.Screen.Runs!.Execute();
        rig.Tick();
        rig.Tick();
        rig.Tick();
        rig.Editor.Screen.Stops!.Execute();
        rig.Tick();

        Assert.Equal(Template(rig.Texts.SequenceStoppedByHand, "1"), page.Bar.Status);

        rig.Editor.Screen.Runs!.Execute();
        rig.Tick();
        Assert.NotEqual(Template(rig.Texts.SequenceStoppedByHand, "1"), page.Bar.Status);
    }

    /// <summary>Cut short by switching its logic system off, it says so with its step once the system is on again.</summary>
    [Fact]
    public void ASwitchOffPartWayIsSaidWithItsStep()
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Tick();

        var system = rig.Editor.Program.Systems[0];
        var sequence = system.Sequences[0];
        var door = rig.World.Entities.First(entity => entity.Name == "Airlock Outer Door");
        var act = new Act("open");
        act.Targets.Add(rig.Editor.Program.Declare(rig.World, door).Name);
        sequence.Steps.Clear();
        sequence.Steps.Add(act);
        sequence.Steps.Add(new Await());
        rig.Editor.Program.Edited();

        rig.Editor.GoToRoot();
        rig.Tick();
        rig.Editor.Push(new SequencePage(rig.Editor, sequence));
        var page = rig.Editor.Sentence();
        rig.Tick();

        rig.Runner.Call(sequence, system.First);
        rig.Tick();
        rig.Tick();
        system.Enabled = false;
        for (var i = 0; i < 101; i++)
            rig.Tick();
        system.Enabled = true;
        rig.Tick();

        Assert.Equal(Template(rig.Texts.SequenceStoppedBySwitch, "1"), page.Bar.Status);
    }

    static string Template(string text, string step) => text.Replace("{step}", step);
}
