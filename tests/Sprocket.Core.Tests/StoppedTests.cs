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
        Assert.Equal(StopCause.Sequence, a.State.StoppedBy);
        Assert.Equal(a.Abort.Number, a.State.StoppedFrom);
        Assert.Equal(at, a.State.StoppedAt);

        // Nothing is wrong: the one thing said about it is that nothing starts it, which this rig never gave it.
        Assert.Equal(SequenceProblem.NothingStarts, SequenceSummary.Fault(a.State).Problem);
    }

    /// <summary>A trigger stopping it, and a hand at the editor, are said as such.</summary>
    [Theory]
    [InlineData(StopCause.Trigger)]
    [InlineData(StopCause.Hand)]
    public void AStopByATriggerOrByHandIsSaid(StopCause by)
    {
        var a = new Airlock();
        a.Runner.Call(a.Cycle, a.Cycle.System.First, true);
        a.Tick(5);

        if (by == StopCause.Trigger)
            a.Runner.Halt(a.Cycle, a.Cycle.System.First, StopCause.Trigger, 0);
        else
            a.State.Interrupt(StopCause.Hand, 0);

        Assert.Equal(SequenceStatus.Stopped, a.Status);
        Assert.Equal(by, a.State.StoppedBy);
    }

    /// <summary>A run cut short by switching its logic system or its installation off, or one started by hand, stops on the switch, keeps the step it got to, and says so once it is switched on.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void ASwitchOffPartWayIsSaid(bool installation, bool byHand)
    {
        var a = new Airlock();
        a.Runner.Call(a.Cycle, a.Cycle.System.First, byHand);
        a.Tick(5);
        Assert.True(a.State.IsRunning);
        var at = a.State.CurrentStep;

        if (installation)
            a.Cycle.System.First.Enabled = false;
        else
            a.Cycle.System.Enabled = false;

        a.Tick(101);

        Assert.False(a.State.IsRunning);
        if (!installation && !byHand)
            Assert.Equal(SequenceStatus.Off, a.Status);

        Assert.Equal(StopCause.Switch, a.State.StoppedBy);
        if (!installation && !byHand)
        {
            Assert.Equal(at, a.State.StoppedAt);

            a.Cycle.System.Enabled = true;
            Assert.Equal(SequenceStatus.Stopped, a.Status);
        }
    }

    /// <summary>A run is stopped on the tick its logic system is switched off, however long it was to wait before it was next looked at.</summary>
    [Fact]
    public void ASwitchOffStopsTheRunAtOnce()
    {
        var a = new Airlock();
        a.Cycle.Steps.Insert(0, new Wait(6000));
        a.Runner.Call(a.Cycle, a.Cycle.System.First);
        a.Tick(5);
        Assert.True(a.State.IsWaiting);

        a.Cycle.System.Enabled = false;
        a.Tick();

        Assert.False(a.State.IsRunning);
        Assert.Equal(StopCause.Switch, a.State.StoppedBy);
    }

    /// <summary>A run switched off while the machine has no power is kept until the power is back, and stopped then.</summary>
    [Fact]
    public void ASwitchOffWithoutPowerWaitsForThePower()
    {
        var a = new Airlock();
        a.Runner.Call(a.Cycle, a.Cycle.System.First);
        a.Tick(5);
        a.Runner.Live = false;

        a.Cycle.System.Enabled = false;
        a.Tick(50);
        Assert.True(a.State.IsRunning);

        a.Runner.Live = true;
        a.Tick(20);

        Assert.False(a.State.IsRunning);
        Assert.Equal(StopCause.Switch, a.State.StoppedBy);
    }

    /// <summary>A run by hand of a switched-off logic system is refused, as a trigger's or a call's is.</summary>
    [Fact]
    public void ARunByHandOfASwitchedOffSystemIsRefused()
    {
        var a = new Airlock();
        a.Cycle.System.Enabled = false;
        a.Tick(5);

        a.Runner.Call(a.Cycle, a.Cycle.System.First, true);
        a.Tick(5);

        Assert.False(a.State.IsRunning);
        Assert.Equal(1, a.State.RefusedCount);
    }

    /// <summary>The next run forgets the stop, and a sequence stopped while at rest has nothing to say.</summary>
    [Fact]
    public void TheNextRunForgetsTheStop()
    {
        var a = new Airlock();
        a.State.Interrupt(StopCause.Hand, 0);
        Assert.Equal(StopCause.None, a.State.StoppedBy);

        a.Runner.Call(a.Cycle, a.Cycle.System.First, true);
        a.Tick(5);
        a.State.Interrupt(StopCause.Hand, 0);
        Assert.Equal(SequenceStatus.Stopped, a.Status);

        a.Runner.Call(a.Cycle, a.Cycle.System.First, true);
        a.Tick();
        Assert.Equal(StopCause.None, a.State.StoppedBy);
        Assert.NotEqual(SequenceStatus.Stopped, a.Status);
    }
}
