using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>An open screen shows what the runtime changed on the tick it changed, and an idle one recounts nothing.</summary>
public class LiveScreenTests
{
    /// <summary>A door sent the other way while its step waits: the step's line says so on the tick the run finds it.</summary>
    [Fact]
    public void AFaultShowsOnTheTickItIsFound()
    {
        var rig = Opening(out var sequence);
        var page = rig.Editor.Sentence();
        rig.Runner.Call(sequence, sequence.System.First);
        rig.Tick();
        rig.Tick();

        Outer(rig).Perform("Open_Off", new List<Literal>());

        var state = rig.Runner.Get(sequence, sequence.System.First)!;
        for (var i = 0; i < 200; i++)
        {
            rig.Tick();
            var fault = SequenceSummary.Fault(state);
            if (!fault.Exists)
            {
                Assert.Equal("", rig.Editor.StepRows()[0].Problem);
                continue;
            }

            Assert.Equal(SequenceProblem.Conflicted, fault.Problem);
            Assert.Equal(SequenceText.Problem(rig.Texts, fault, sequence), rig.Editor.StepRows()[0].Problem);
            Assert.Same(page, rig.Editor.Current);
            return;
        }

        Assert.Fail("the run never found the door sent the other way");
    }

    /// <summary>An open screen with nothing happening recounts nothing, on the home screen, a logic system's and a sequence's.</summary>
    [Theory]
    [InlineData("home")]
    [InlineData("logic system")]
    [InlineData("sequence")]
    public void AnIdleScreenRecountsNothing(string screen)
    {
        var rig = Opening(out _);
        if (screen == "home")
            rig.Editor.GoToRoot();
        else if (screen == "logic system")
            rig.Editor.Back();

        for (var i = 0; i < 10; i++)
            rig.Tick();

        var page = rig.Editor.Current;
        var before = page.Recounts;
        for (var i = 0; i < 600; i++)
            rig.Tick();

        Assert.Same(page, rig.Editor.Current);
        Assert.Equal(before, page.Recounts);
    }

    /// <summary>A run takes the same ticks with a screen open on it as with none.</summary>
    [Fact]
    public void ARunTimesTheSameWithAScreenOpen()
    {
        Assert.Equal(Timed(false), Timed(true));
    }

    /// <summary>What the blocks were told, each with the tick it was told on, over a run of the sequence with a screen open or not.</summary>
    static List<string> Timed(bool showing)
    {
        var rig = Opening(out var sequence);
        rig.Unit.Showing = showing;
        rig.Machine.Forget();

        var told = new List<string>();
        rig.Runner.Call(sequence, sequence.System.First);
        for (var tick = 0; tick < 300; tick++)
        {
            rig.Tick();
            foreach (var line in rig.Machine.Log)
                told.Add(tick + " " + line);
            rig.Machine.Forget();
        }

        Assert.NotEmpty(told);
        return told;
    }

    /// <summary>A switched-on logic system whose sequence opens the outer door and waits for it, open on the sequence's screen.</summary>
    static Simulation Opening(out Sequence sequence)
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Tick();

        var system = rig.Editor.Program.Systems[0];
        sequence = system.Sequences[0];
        var door = rig.World.Entities.First(entity => entity.Name == "Airlock Outer Door");
        var act = new Act("open");
        act.Targets.Add(rig.Editor.Program.Declare(rig.World, door).Name);
        sequence.Steps.Clear();
        sequence.Steps.Add(act);
        sequence.Steps.Add(new Await());
        system.Enabled = true;
        rig.Editor.Program.Edited();

        rig.Editor.GoToRoot();
        rig.Tick();
        rig.Editor.Push(new LogicSystemPage(rig.Editor, system));
        rig.Tick();
        rig.Editor.Push(new SequencePage(rig.Editor, sequence));
        rig.Tick();
        return rig;
    }

    static Fitting Outer(Simulation rig) => rig.Machine.Fitted.First(fitting => fitting.Name == "Airlock Outer Door");
}
