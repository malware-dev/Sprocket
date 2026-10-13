using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A run stopped from outside says so, what stopped it and at which step, until the sequence runs again.</summary>
public class StoppedTests
{
    sealed class Airlock
    {
        public readonly Rigged Rig = new();
        public readonly Automation Program = new();
        public readonly Sequence Cycle;
        public readonly Sequence Abort;
        public readonly AutomationRunner Runner;

        int _frame;

        public Airlock()
        {
            var inner = Rig.Door(1, "Inner Door");
            var vent = Rig.Vent(2, "Airlock Vent");

            Cycle = Program.AddSequence((Trigger)null!);
            Cycle.System.Name = "Airlock";
            Cycle.Name = "Cycle";
            Do(Cycle, "close", inner);
            Do(Cycle, "depressurize", vent);

            Abort = Program.AddSequence(Cycle.System);
            Abort.Name = "Abort";
            Abort.Steps.Add(new Run(Cycle.Number, true));

            Program.Edited();
            Runner = new AutomationRunner(Program, Rig.World);
            Tick(3);
        }

        public SequenceRunner State => Runner.Get(Cycle, Cycle.System.First);

        public SequenceStatus Status => SequenceSummary.Status(Cycle, State);

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++_frame);
            }
        }

        void Do(Sequence sequence, string action, Fitting block)
        {
            var act = new Act(action);
            act.Targets.Add(Program.Declare(Rig.World, Rig.Entity(block.Id)).Name);
            sequence.Steps.Add(Given.Defaults(act));
            sequence.Steps.Add(new Await());
        }
    }

    /// <summary>A sequence stopped by another says which, and the step it was held at.</summary>
    [Fact]
    public void AStopFromAnotherSequenceIsSaid()
    {
        var a = new Airlock();
        a.Runner.Call(a.Cycle, a.Cycle.System.First, true);
        a.Tick(5);
        Assert.True(a.State.IsRunning);
        var at = a.State.CurrentStep;

        a.Runner.Call(a.Abort, a.Abort.System.First, true);
        a.Tick();

        Assert.False(a.State.IsRunning);
        Assert.Equal(SequenceStatus.Stopped, a.Status);
        Assert.Equal(Stopper.Sequence, a.State.StoppedBy);
        Assert.Equal(a.Abort.Number, a.State.StoppedFrom);
        Assert.Equal(at, a.State.StoppedAt);

        // Nothing is wrong: the one thing said about it is that nothing starts it, which this rig never gave it.
        Assert.Equal(SequenceProblem.NothingStarts, SequenceSummary.Fault(a.State).Problem);
    }

    /// <summary>A trigger stopping it, and a hand at the editor, are said as such.</summary>
    [Theory]
    [InlineData(Stopper.Trigger)]
    [InlineData(Stopper.Hand)]
    public void AStopByATriggerOrByHandIsSaid(Stopper by)
    {
        var a = new Airlock();
        a.Runner.Call(a.Cycle, a.Cycle.System.First, true);
        a.Tick(5);

        if (by == Stopper.Trigger)
            a.Runner.Halt(a.Cycle, a.Cycle.System.First, Stopper.Trigger, 0);
        else
            a.State.Interrupt(Stopper.Hand, 0);

        Assert.Equal(SequenceStatus.Stopped, a.Status);
        Assert.Equal(by, a.State.StoppedBy);
    }

    /// <summary>A run cut short by switching its logic system off keeps the step it got to, and says so once it is switched on.</summary>
    [Fact]
    public void ASwitchOffPartWayIsSaid()
    {
        var a = new Airlock();
        a.Runner.Call(a.Cycle, a.Cycle.System.First);
        a.Tick(5);
        Assert.True(a.State.IsRunning);
        var at = a.State.CurrentStep;

        a.Cycle.System.Enabled = false;
        a.Tick(101);

        Assert.False(a.State.IsRunning);
        Assert.Equal(SequenceStatus.Off, a.Status);
        Assert.Equal(Stopper.Switch, a.State.StoppedBy);
        Assert.Equal(at, a.State.StoppedAt);

        a.Cycle.System.Enabled = true;
        Assert.Equal(SequenceStatus.Stopped, a.Status);
    }

    /// <summary>An installation switched off part way through is said the same way.</summary>
    [Fact]
    public void AnInstallationSwitchedOffPartWayIsSaid()
    {
        var a = new Airlock();
        a.Runner.Call(a.Cycle, a.Cycle.System.First);
        a.Tick(5);

        a.Cycle.System.First.Enabled = false;
        a.Tick(101);

        Assert.False(a.State.IsRunning);
        Assert.Equal(Stopper.Switch, a.State.StoppedBy);
    }

    /// <summary>A run started by hand ignores the switch, so switching off leaves it running.</summary>
    [Fact]
    public void ARunByHandIsLeftToRun()
    {
        var a = new Airlock();
        a.Runner.Call(a.Cycle, a.Cycle.System.First, true);
        a.Tick(5);

        a.Cycle.System.Enabled = false;
        a.Tick(5);

        Assert.True(a.State.IsRunning);
        Assert.Equal(Stopper.None, a.State.StoppedBy);
    }

    /// <summary>The next run forgets the stop, and a sequence stopped while at rest has nothing to say.</summary>
    [Fact]
    public void TheNextRunForgetsTheStop()
    {
        var a = new Airlock();
        a.State.Interrupt(Stopper.Hand, 0);
        Assert.Equal(Stopper.None, a.State.StoppedBy);

        a.Runner.Call(a.Cycle, a.Cycle.System.First, true);
        a.Tick(5);
        a.State.Interrupt(Stopper.Hand, 0);
        Assert.Equal(SequenceStatus.Stopped, a.Status);

        a.Runner.Call(a.Cycle, a.Cycle.System.First, true);
        a.Tick();
        Assert.Equal(Stopper.None, a.State.StoppedBy);
        Assert.NotEqual(SequenceStatus.Stopped, a.Status);
    }
}
