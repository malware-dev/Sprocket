using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// Off means off, whoever asks and however they ask.
/// </summary>
/// <remarks>
/// Pausing a sequence is worth a great deal - it is how somebody welds on a machine without losing
/// the rule that drives it - so it has to mean what it says. A trigger starts nothing, a step saying
/// <c>run</c> starts nothing, and a button somebody presses starts nothing.
/// <para>
/// What must not happen is the refusal being silent. A call that quietly evaporated would be the one
/// place Sprocket did nothing without saying so, so a refused call is counted and shown.
/// </para>
/// </remarks>
public class SwitchTests
{
    sealed class Ship
    {
        public Rigged Rig = new();
        public Automation Program = new();
        public AutomationRunner Runner = null!;
        public AutomationSystem Airlock = null!;

        public Door Inner = null!;
        public Door Outer = null!;

        public Sequence Secure = null!;

        public GridWorld World => Rig.World;

        /// <summary>What the construct has been told to do, oldest first.</summary>
        public List<string> Log => Rig.Rig.Log;

        /// <summary>Names a block, so a test builds sequences the way the editor does.</summary>
        public string Ref(Fitting fitting) => Program.Declare(World, Seen(fitting)).Name;

        /// <summary>What Sprocket sees of one of the rig's blocks.</summary>
        public GridEntity Seen(Fitting fitting) => Rig.Entity(fitting.Id);

        int _frame;

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++_frame);
            }
        }

        public SequenceRunner State(Sequence sequence) => Runner.Get(sequence)!;
    }

    /// <summary>
    /// One rule: when the inner door opens, shut the outer one.
    /// </summary>
    /// <remarks>
    /// A trigger Sprocket samples rather than one the construct announces, because a test can drive
    /// this one by hand - opening a door is something a player does.
    /// </remarks>
    static Ship Build()
    {
        var ship = new Ship();
        ship.Inner = ship.Rig.Door(1, "Inner Door");
        ship.Outer = ship.Rig.Door(2, "Outer Door");

        ship.Airlock = ship.Program.AddSystem();
        ship.Airlock.Name = "Airlock";

        var opened = new WhenTrigger("opened");
        opened.Sources.Add(ship.Ref(ship.Inner));

        ship.Secure = ship.Program.AddSequence(ship.Airlock, opened);
        ship.Secure.Name = "Secure";
        ship.Secure.Steps.Add(Do("close", ship.Ref(ship.Outer)));

        ship.Runner = new AutomationRunner(ship.Program, ship.World);
        return ship;
    }

    static Act Do(string actionId, params string[] targets)
    {
        var step = new Act(actionId);
        foreach (var target in targets)
            step.Targets.Add(target);
        return step;
    }

    /// <summary>Adds a sequence of the same system with nothing to start it.</summary>
    static Sequence Called(Ship ship, string name, string actionId, string target)
    {
        var sequence = ship.Program.AddSequence(ship.Airlock);
        sequence.Name = name;
        sequence.Steps.Add(Do(actionId, target));
        return sequence;
    }

    /// <summary>
    /// A parked sequence reads its trigger and starts nothing.
    /// </summary>
    /// <remarks>
    /// Sampled anyway, which is what makes switching it on again safe: an edge is a change since the
    /// last reading, so a sequence that slept through one would fire on the next thing it noticed.
    /// </remarks>
    [Fact]
    public void AParkedSequenceDoesNotStart()
    {
        var ship = Build();
        ship.Secure.Enabled = false;
        ship.Tick(3);
        ship.Log.Clear();

        ship.Inner.ForceOpen();
        ship.Tick(3);

        Assert.DoesNotContain("Outer Door:close", ship.Log);

        // Switched on, and the same thing happening again does what it says. The door has to shut
        // first: an edge is a change, and a door that is already open cannot open again.
        ship.World.Invoke(ship.Seen(ship.Inner), "close");
        ship.Tick(90);
        ship.Secure.Enabled = true;

        ship.Inner.ForceOpen();
        ship.Tick(3);

        Assert.Contains("Outer Door:close", ship.Log);
    }

    /// <summary>
    /// A step saying <c>run</c> that meets a parked sequence is refused, and the refusal is counted.
    /// </summary>
    /// <remarks>
    /// Both halves matter. A switch a person threw must hold against another sequence asking, or it
    /// does not mean what it says - and the refusal must be visible, or a rule that ignored a request
    /// is indistinguishable from one nothing asked for.
    /// </remarks>
    [Fact]
    public void ACallOnAParkedSequenceIsRefusedAndSaysSo()
    {
        var ship = Build();

        var warn = Called(ship, "Warn", BlockKinds.SwitchOff, ship.Ref(ship.Outer));
        warn.Enabled = false;
        ship.Secure.Steps.Add(new Run(warn.Number));

        ship.Tick(3);
        ship.Log.Clear();

        ship.Inner.ForceOpen();
        ship.Tick(3);

        Assert.DoesNotContain("Outer Door:off", ship.Log);
        Assert.Equal(1, ship.State(warn).RefusedCount);

        // And switched on it runs, called by the same step.
        ship.World.Invoke(ship.Seen(ship.Inner), "close");
        ship.Tick(90);
        warn.Enabled = true;

        ship.Inner.ForceOpen();
        ship.Tick(3);

        Assert.Contains("Outer Door:off", ship.Log);
        Assert.Equal(1, ship.State(warn).RefusedCount);
    }

    /// <summary>
    /// Switching a sequence off stops what it was in the middle of.
    /// </summary>
    /// <remarks>
    /// Which is what makes editing safe: a sequence may only be changed while it is parked, and
    /// finding one still half way through its steps would be a rule rewritten under a machine that
    /// was acting on it. Whatever it has already done stays done - see the editor's own words for it.
    /// </remarks>
    [Fact]
    public void ParkingASequenceStopsItMidRun()
    {
        var ship = Build();
        ship.Secure.Steps.Add(new Wait(600));
        ship.Secure.Steps.Add(Do("open", ship.Ref(ship.Outer)));

        ship.Tick(3);
        ship.Inner.ForceOpen();
        ship.Tick(3);

        Assert.True(ship.State(ship.Secure).IsRunning);

        ship.Secure.Enabled = false;
        ship.Tick();

        Assert.False(ship.State(ship.Secure).IsRunning);

        // And the step it had not reached stays unreached.
        ship.Log.Clear();
        ship.Tick(700);

        Assert.DoesNotContain("Outer Door:open", ship.Log);
    }

    /// <summary>
    /// Switching the system off reaches everything under it, and leaves the sequences' own switches
    /// alone.
    /// </summary>
    /// <remarks>
    /// Rewiring a machine means stopping the machine, not stopping three rules one at a time - and
    /// starting it again must not arm the one rule somebody had deliberately parked.
    /// </remarks>
    [Fact]
    public void SwitchingTheSystemOffDoesNotDisturbTheSequenceSwitches()
    {
        var ship = Build();
        var warn = Called(ship, "Warn", BlockKinds.SwitchOff, ship.Ref(ship.Outer));
        warn.Enabled = false;

        ship.Airlock.Enabled = false;

        Assert.False(ship.Secure.CanRun);
        Assert.True(ship.Secure.Enabled);

        ship.Airlock.Enabled = true;

        Assert.True(ship.Secure.CanRun);
        Assert.False(warn.Enabled);
    }
}
