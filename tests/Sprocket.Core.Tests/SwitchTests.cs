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
        ship.Secure.Steps.Add(new Await());

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

    /// <summary>
    /// A sequence in a system switched off reads its trigger and starts nothing.
    /// </summary>
    /// <remarks>
    /// Sampled anyway, which is what makes switching it on again safe: an edge is a change since the
    /// last reading, so a sequence that slept through one would fire on the next thing it noticed.
    /// </remarks>
    [Fact]
    public void ASystemSwitchedOffStartsNothing()
    {
        var ship = Build();
        ship.Airlock.Enabled = false;
        ship.Tick(3);
        ship.Log.Clear();

        ship.Inner.ForceOpen();
        ship.Tick(3);

        Assert.DoesNotContain("Outer Door:close", ship.Log);

        // Switched on, and the same thing happening again does what it says. The door has to shut
        // first: an edge is a change, and a door that is already open cannot open again.
        ship.World.Invoke(ship.Seen(ship.Inner), "close");
        ship.Tick(90);
        ship.Airlock.Enabled = true;

        ship.Inner.ForceOpen();
        ship.Tick(Rota.Ten);

        Assert.Contains("Outer Door:close", ship.Log);
    }

    /// <summary>
    /// Switching a system off stops what its sequences were in the middle of.
    /// </summary>
    /// <remarks>
    /// Which is what makes editing safe: a system may only be changed while it is switched off, and
    /// finding a sequence still half way through its steps would be a rule rewritten under a machine
    /// that was acting on it. Whatever it has already done stays done.
    /// </remarks>
    [Fact]
    public void SwitchingASystemOffStopsItMidRun()
    {
        var ship = Build();
        ship.Secure.Steps.Add(new Wait(600));
        ship.Secure.Steps.Add(Do("open", ship.Ref(ship.Outer)));

        ship.Tick(Rota.Ten);
        ship.Inner.ForceOpen();
        ship.Tick(Rota.Ten);

        Assert.True(ship.State(ship.Secure).IsRunning);

        ship.Airlock.Enabled = false;
        ship.Tick(Rota.Ten);

        Assert.False(ship.State(ship.Secure).IsRunning);

        // And the step it had not reached stays unreached.
        ship.Log.Clear();
        ship.Tick(700);

        Assert.DoesNotContain("Outer Door:open", ship.Log);
    }

    /// <summary>
    /// Switching a logic system on, or one of it, forgets why Sprocket switched it off, and leaves the other switch alone.
    /// </summary>
    /// <remarks>
    /// Starting the machine must not start the one of it somebody had stopped.
    /// </remarks>
    [Fact]
    public void SwitchingOnForgetsWhyItWentOff()
    {
        var ship = Build();
        var airlock = ship.Airlock.First!;
        airlock.Enabled = false;
        airlock.Halt = new Halt(SequenceProblem.MissingTarget, ship.Secure.Number, 0, null, "Outer Door");
        ship.Airlock.Enabled = false;
        ship.Airlock.Halt = new Halt(SequenceProblem.NeverTriggered, ship.Secure.Number, -1, null, null);

        ship.Airlock.Enabled = true;

        Assert.Null(ship.Airlock.Halt);
        Assert.NotNull(airlock.Halt);
        Assert.False(ship.Secure.CanRun(airlock));

        airlock.Enabled = true;

        Assert.Null(airlock.Halt);
        Assert.True(ship.Secure.CanRun(airlock));
    }
}
