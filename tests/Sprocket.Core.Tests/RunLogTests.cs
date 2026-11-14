using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>The debug log: a line for everything a run does while it is on, and nothing at all while it is off.</summary>
public class RunLogTests
{
    sealed class Airlock
    {
        public readonly Rigged Rig = new();

        public readonly Door Inner;
        public readonly Door Outer;
        public readonly Vent Vent;

        public readonly Automation Program = new();
        public readonly Sequence Cycle;
        public readonly List<string> Lines = new();
        public AutomationRunner Runner = null!;

        int _frame;

        public Airlock()
        {
            Inner = Rig.Door(1, "Inner Door");
            Outer = Rig.Door(2, "Outer Door");
            Vent = Rig.Vent(3, "Airlock Vent");

            Cycle = Program.AddSequence((Trigger)null!);
            Cycle.System.Name = "Airlock";
            Cycle.Name = "Cycle";
        }

        public void Start(bool logged = true)
        {
            Program.Edited();
            Runner = new AutomationRunner(Program, Rig.World);
            if (logged)
                Runner.Log = new RunLog(Lines.Add, () => "Mining Barge");

            Tick(3);
            Runner.Call(Cycle, Cycle.System.First);
        }

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++_frame);
            }
        }

        public Comparison Closed(Door door)
        {
            var test = new Comparison { MemberId = "status", Provider = "ext", Quantifier = Quantifier.All, Value = Literal.Name("closed") };
            test.Targets.Add(Name(door));
            return test;
        }

        public Act Do(string action, Fitting block)
        {
            var act = new Act(action);
            act.Targets.Add(Name(block));
            return Given.Defaults(act);
        }

        public void Add(params Step[] steps)
        {
            foreach (var step in steps)
                Cycle.Steps.Add(step);
        }

        string Name(Fitting fitting) => Program.Declare(Rig.World, Rig.Entity(fitting.Id)).Name;
    }

    /// <summary>A Sprocket's own log names the programmable block running the automation on every line.</summary>
    [Fact]
    public void TheLogNamesTheProgrammableBlock()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Tick();
        var sequence = sim.Editor.Program.Systems[0].Sequences[0];
        sim.Unit.Apply(Settings.From("[Sprocket]\ndebugLog=true\n", null));
        var said = GameLog.Lines.Count;

        sim.Runner.Call(sequence, sequence.System.First!);
        sim.Tick();

        var lines = GameLog.Lines.Skip(said).Where(line => line.StartsWith("tick ", StringComparison.Ordinal)).ToList();
        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.Contains(", \"Automation\"", line));
    }

    /// <summary>A run is told from its start to its end: each step as it is written, what each block was asked, which way the if went, and the wait.</summary>
    [Fact]
    public void ARunIsToldStepByStep()
    {
        var a = new Airlock();
        a.Add(new If(a.Closed(a.Outer)), a.Do("open", a.Inner), new Else(null!), a.Do("depressurize", a.Vent), new EndIf(), new Await());

        a.Start();
        a.Tick(200);

        Assert.All(a.Lines, line => Assert.StartsWith("tick ", line));
        Assert.All(a.Lines, line => Assert.Contains("\"Mining Barge\", \"Airlock\" on installation 1, sequence 1 \"Cycle\": ", line));
        In(a.Lines,
            "sequence 1 \"Cycle\": started by a call",
            "step 1: if all ext.status on",
            "step 1 holds: \"Outer Door\" reads closed",
            "step 2: call ext.open on [ 'Inner Door' ]",
            "step 2 asked \"Inner Door\" to open",
            "step 3: else",
            "step 6: await",
            "step 6 waits for step 2",
            "step 6 done waiting, after",
            "finished");
        Assert.DoesNotContain(a.Lines, line => line.Contains("depressurize"));
    }

    /// <summary>A start that does not happen says why, and a run stopped by hand says where it was.</summary>
    [Fact]
    public void ARefusedStartAndAStopAreTold()
    {
        var a = new Airlock();
        a.Add(a.Do("open", a.Inner), new Await());

        a.Start();
        a.Tick();
        a.Runner.Call(a.Cycle, a.Cycle.System.First);
        a.Runner.Halt(a.Cycle, a.Cycle.System.First, StopCause.Hand, 0);

        In(a.Lines, "did not start: it is already running", "stopped at step 2 by hand");
    }

    /// <summary>A step a block cannot do is told with the step, what went wrong and the block.</summary>
    [Fact]
    public void AFaultNamesItsStepAndItsBlock()
    {
        var a = new Airlock();
        a.Inner.Enabled = false;
        a.Add(a.Do("open", a.Inner), new Await());

        a.Start();
        a.Tick();

        Assert.Contains(a.Lines, line => line.Contains("step 1 failed: Unable, \"Inner Door\""));
    }

    /// <summary>With the log off the same run writes nothing, and a log taken away mid-run stops at once.</summary>
    [Fact]
    public void OffWritesNothing()
    {
        var a = new Airlock();
        a.Add(a.Do("open", a.Inner), new Await(), a.Do("close", a.Inner), new Await());

        a.Start();
        a.Tick();
        var said = a.Lines.Count;
        a.Runner.Log = null;
        a.Tick(400);

        Assert.True(said > 0);
        Assert.Equal(said, a.Lines.Count);
        Assert.False(a.Runner.Get(a.Cycle, a.Cycle.System.First)!.IsRunning);
    }

    /// <summary>Each of <paramref name="expected" /> is found in a line of <paramref name="lines" />, in that order.</summary>
    static void In(List<string> lines, params string[] expected)
    {
        var at = 0;
        foreach (var part in expected)
        {
            while (at < lines.Count && !lines[at].Contains(part))
                at++;

            Assert.True(at < lines.Count, "no line after the ones before says \"" + part + "\":\n" + string.Join("\n", lines));
            at++;
        }
    }
}
