using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;
using Sprocket.Sim.Machine;
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;
using SpaceEngineers.Game.ModAPI;
using VentStatus = SpaceEngineers.Game.ModAPI.Ingame.VentStatus;

namespace Sprocket.Tests;

/// <summary>
/// The airlock, built the way a player describes it out loud.
/// </summary>
/// <remarks>
/// "I hit the button. The inner door shuts. The air gets pumped out. The outer door
/// opens."
///
/// Three sentences, three steps, and nothing else - no gates, no waits, no
/// conditions. The ordering is carried by the list being a list, exactly as it is in
/// a recipe. If the model cannot express a player's own sentence without additions,
/// the model is wrong; that is what these tests are really checking.
/// </remarks>
public class AirlockTests
{
    sealed class Airlock
    {
        public Rigged Ship = new();
        public Automation Automation = new();
        public AutomationRunner Runner = null!;

        public Door Inner = null!;
        public Door Outer = null!;
        public Vent Vent = null!;
        public Panel Inside = null!;

        public Sequence LetMeOut = null!;

        public GridWorld World => Ship.World;

        /// <summary>What the construct has been told to do, oldest first.</summary>
        public List<string> Log => Ship.Rig.Log;

        /// <summary>Names a block, so a test builds sequences the way the editor does.</summary>
        public string Ref(GridEntity entity) => Automation.Declare(World, entity).Name;

        /// <summary>What Sprocket sees of one of the rig's blocks.</summary>
        public GridEntity Seen(Fitting fitting) => Ship.Entity(fitting.Id);

        int _frame;

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Ship.Tick();
                Runner.Tick(++_frame);
            }
        }

        /// <summary>
        /// The button being pressed, which is the host calling this sequence.
        /// </summary>
        /// <remarks>
        /// A button raises nothing a rule can sample: its slot carries which sequence to run and the
        /// unit resolves that. Which slot carries which key is the harness's business - see RigTests.
        /// </remarks>
        public void Press()
        {
            Runner.Call(LetMeOut);
        }

        public SequenceRunner State => Runner.Get(LetMeOut)!;
    }

    /// <summary>One thing done to some blocks, on its own, with nothing waiting for it.</summary>
    static Act Do(string actionId, params string[] targets)
    {
        var step = new Act(actionId);
        foreach (var target in targets)
            step.Targets.Add(target);
        return step;
    }

    /// <summary>
    /// A step and the await after it, which is how the editor writes one.
    /// </summary>
    /// <remarks>
    /// So a test reads as the procedure it is about, and so that leaving the await out - which is what
    /// makes two steps overlap - is visible where a test means it.
    /// </remarks>
    static void Then(Sequence sequence, string actionId, params string[] targets)
    {
        sequence.Steps.Add(Do(actionId, targets));
        sequence.Steps.Add(new Await());
    }

    static Airlock Build()
    {
        var a = new Airlock();
        a.Inner = a.Ship.Door(1, "Inner Door");
        a.Outer = a.Ship.Door(2, "Outer Door");
        a.Vent = a.Ship.Vent(3, "Airlock Vent");
        a.Inside = a.Ship.Panel(4, "Inside Button");

        var trigger = new WhenTrigger(new ToolbarSlot(1, 1, 1).EventId);
        trigger.Sources.Add(a.Ref(a.Seen(a.Inside)));

        a.LetMeOut = a.Automation.AddSequence(trigger);
        a.LetMeOut.Name = "Let me out";

        Then(a.LetMeOut, "close", a.Ref(a.Seen(a.Inner)));
        Then(a.LetMeOut, "depressurize", a.Ref(a.Seen(a.Vent)));
        Then(a.LetMeOut, "open", a.Ref(a.Seen(a.Outer)));

        a.Runner = new AutomationRunner(a.Automation, a.World);
        return a;
    }

    [Fact]
    public void EachStepFinishesBeforeTheNextBegins()
    {
        var a = Build();
        a.Inner.ForceOpen();
        a.Tick(3);
        a.Log.Clear();

        a.Press();
        a.Tick();
        Assert.Equal(["Inner Door:close"], a.Log);

        // The door takes time, and nothing else may happen while it travels. Nobody
        // wrote "once it is shut" - the list being a list is what says that.
        a.Tick(5);
        Assert.Equal(["Inner Door:close"], a.Log);
        Assert.True(a.State.IsWaiting);
        Assert.Equal(1, a.State.CurrentStep);

        a.Tick(50);
        Assert.Contains("Airlock Vent:depressurize", a.Log);
        Assert.DoesNotContain("Outer Door:open", a.Log);

        a.Tick(100);
        Assert.Equal(["Inner Door:close", "Airlock Vent:depressurize", "Outer Door:open"], a.Log);

        a.Tick(50);
        Assert.False(a.State.IsRunning);
    }

    /// <summary>
    /// An emptied airlock counts as empty, although the vent never says so.
    /// </summary>
    /// <remarks>
    /// The game works a vent's status out from its room, and an airtight room never reads
    /// Depressurized - emptied to nothing it still says Depressurizing, because Depressurized is
    /// what a room that cannot hold air reports. So Sprocket reads the oxygen left instead.
    /// <para>
    /// Worth its own test rather than leaving it to the cycle above, because the harness used to
    /// report a state the game does not produce, and every airlock test passed on it while a real
    /// airlock cycle waited for ever at the vent.
    /// </para>
    /// </remarks>
    [Fact]
    public void AnEmptiedAirlockIsEmptyThoughTheVentStillSaysItIsEmptying()
    {
        var a = Build();

        // A tick before the press, so the trigger is bound: nothing is wired until the runner has
        // seen the world once.
        a.Tick(3);
        a.Press();
        a.Tick(200);

        var vent = a.Seen(a.Vent);
        Assert.Contains("Airlock Vent:depressurize", a.Log);
        Assert.Equal(VentStatus.Depressurizing, ((IMyAirVent)a.Vent.Block).Status);

        var empty = vent.Kind.FindState("is-empty");
        Assert.True(empty.Holds(a.World.Values, vent.GetValue(empty.PropertyId)),
            "the airlock has no air left and does not read as empty");
    }

    /// <summary>
    /// A door destroyed while it is being waited for does not finish the wait.
    /// </summary>
    /// <remarks>
    /// Nothing to ask is not nothing to wait for. The inner door has not shut - it has gone - and
    /// the next step opens the outer one, so treating the absence as "arrived" vents the ship on
    /// behalf of somebody whose door was shot off.
    /// <para>
    /// It holds here, and being at fault is what switches the sequence off - within the second, by
    /// the same machinery that parks a sequence whose step cannot act at all. See ProblemTests.
    /// </para>
    /// </remarks>
    [Fact]
    public void ADoorLostWhileWaitingDoesNotOpenTheOtherOne()
    {
        var a = Build();
        a.Inner.ForceOpen();
        a.Tick(3);

        a.Press();
        a.Tick();
        Assert.Equal(["Inner Door:close"], a.Log);

        a.Ship.Grind(a.Inner);
        a.Tick(300);

        // A block that is gone is a fault, and a system with a sequence at fault is switched off, which
        // is what ends the run: holding for it and then stopping, never walking on.
        Assert.DoesNotContain("Outer Door:open", a.Log);
        Assert.False(a.LetMeOut.System.Enabled, "a system waiting on a door that is not there was left on");
        Assert.Equal(SequenceProblem.MissingTarget, a.LetMeOut.System.Halt!.Problem);
        Assert.Equal("Inner Door", a.LetMeOut.System.Halt!.Block);
    }

    /// <summary>
    /// A cycle holds while the machine has no power, and carries on when it comes back.
    /// </summary>
    /// <remarks>
    /// Held rather than abandoned, which is the difference between a blackout and somebody pressing
    /// the switch: switching a sequence off is a decision and drops what it was doing, where losing
    /// power is something that happened to it. The same promise as surviving a reload, and the same
    /// rule the programmable block follows when it refuses to run a script on a block that is not
    /// working.
    /// </remarks>
    [Fact]
    public void ACycleHoldsWhileThereIsNoPower()
    {
        var a = Build();
        a.Tick(3);
        a.Press();
        a.Tick(20);

        var step = a.State.CurrentStep;
        Assert.True(a.State.IsRunning);

        a.Runner.Live = false;
        a.Log.Clear();
        a.Tick(200);

        Assert.True(a.State.IsRunning, "the run was dropped rather than held");
        Assert.Equal(step, a.State.CurrentStep);
        Assert.Empty(a.Log);

        a.Runner.Live = true;
        a.Tick(300);

        Assert.Contains("Outer Door:open", a.Log);
    }

    /// <summary>
    /// A vent that cannot answer is not saying the room is empty.
    /// </summary>
    /// <remarks>
    /// An unpowered vent reports nothing left whatever the room holds, and so does one whose room
    /// the game has not worked out yet - which is what the first ticks of a loading world look
    /// like. Reading that as "empty" would open the outer door on a room full of air.
    /// <para>
    /// So anything unreadable is "not yet" and never "done". A sequence held at a wait can be
    /// recovered from by switching the vent on; the other way round cannot.
    /// </para>
    /// </remarks>
    [Fact]
    public void AVentWithNoPowerDoesNotSayTheRoomIsEmpty()
    {
        var a = Build();
        a.Tick(3);

        var vent = a.Seen(a.Vent);
        var empty = vent.Kind.FindState("is-empty");

        a.Vent.Enabled = false;
        a.Tick();

        Assert.False(empty.Holds(a.World.Values, vent.GetValue(empty.PropertyId)),
            "a vent that cannot be read is saying the room is empty");
    }

    [Fact]
    public void AJammedDoorStopsTheSequence_RatherThanVentingTheShip()
    {
        // The whole argument for waiting on the thing itself rather than on a
        // stopwatch. A guessed duration produces an airlock that works until the day
        // a door sticks.
        var a = Build();
        a.Inner.ForceOpen();
        a.Tick(3);
        a.Log.Clear();

        a.Press();
        a.Tick();

        for (var i = 0; i < 60; i++)
        {
            a.Inner.ForceOpen();
            a.Tick();
        }

        Assert.True(a.State.IsWaiting);
        Assert.Equal(1, a.State.CurrentStep);
        Assert.Equal(["Inner Door:close"], a.Log);
    }

    [Fact]
    public void InstantActionsAreNotWaitedOn()
    {
        // Only things that genuinely take time hold the sequence up. An action that
        // reaches for no particular state is finished the moment it is issued.
        var a = Build();
        var beacon = a.Ship.Light(5, "Beacon");

        var sequence = a.Automation.AddSequence(new EveryTrigger(1));
        Then(sequence, BlockKinds.SwitchOn, a.Ref(a.Seen(beacon)));
        Then(sequence, "close", a.Ref(a.Seen(a.Inner)));

        // Fired on the first turn it is sampled, and both land in the tick it fired.
        a.Tick(2 * Rota.Ten);
        Assert.Equal(["Beacon:on", "Inner Door:close"], a.Log.Take(2));
    }

    [Fact]
    public void DoingTwoThingsAtOnceHasToBeAskedFor()
    {
        var a = Build();
        a.Inner.ForceOpen();
        a.Outer.ForceOpen();
        a.Tick(3);
        a.Log.Clear();

        var sequence = a.Automation.AddSequence(new EveryTrigger(1000));
        sequence.Steps.Add(Do("close", a.Ref(a.Seen(a.Inner))));
        Then(sequence, "close", a.Ref(a.Seen(a.Outer)));

        a.Tick();                       // lets the runner pick the new sequence up
        a.Log.Clear();

        // By hand: a step that takes time and is not waited for is a fault, so the check switches this
        // off, and running it by hand is how somebody sees what it does all the same.
        a.Runner.Call(sequence, sequence.System.First, true);

        // Both doors are moving together, which would not happen by default.
        Assert.Equal(["Inner Door:close", "Outer Door:close"], a.Log);
    }

    [Fact]
    public void PressingMidCycleIsIgnored_AndItSaysWhatItIsWaitingOn()
    {
        var a = Build();
        a.Inner.ForceOpen();
        a.Tick(3);

        a.Press();
        a.Tick(2);
        a.Press();
        a.Tick(2);

        Assert.Equal(1, a.State.FiredCount);
        Assert.Equal(1, a.State.SkippedCount);
        Assert.True(a.State.IsWaiting);
        Assert.Equal(1, a.State.CurrentStep);
    }

    /// <summary>
    /// Two triggers on one sequence both start it, and each keeps its own reading.
    /// </summary>
    /// <remarks>
    /// A trigger belonging to a sequence could not say this at all, which is why they moved onto the
    /// system - and with one shared reading between them, the second one fired nothing.
    ///
    /// Two lamps rather than two buttons, because a button raises nothing to read: its slot carries
    /// which sequence to run. A trigger has to watch something that changes.
    /// </remarks>
    [Fact]
    public void TwoTriggersOnOneSequenceBothStartIt()
    {
        var a = Build();
        var one = a.Ship.Light(5, "Inside Lamp");
        var other = a.Ship.Light(6, "Outside Lamp");
        var system = a.Automation.Systems[0];

        foreach (var lamp in new[] { one, other })
        {
            var trigger = new WhenTrigger("switched-on");
            trigger.Sources.Add(a.Ref(a.Seen(lamp)));
            trigger.Does = new Run(a.LetMeOut.Number);
            system.AddTrigger(trigger);
        }

        // Both off to begin with, so switching one on is an edge either trigger could miss.
        a.World.Invoke(a.Seen(one), BlockKinds.SwitchOff);
        a.World.Invoke(a.Seen(other), BlockKinds.SwitchOff);
        a.Tick(Rota.Ten);

        a.World.Invoke(a.Seen(one), BlockKinds.SwitchOn);
        a.Tick(Rota.Ten);
        Assert.Equal(1, a.State.FiredCount);

        // Let the cycle finish, then the other one, which has its own edge to detect.
        a.Tick(220);
        Assert.False(a.State.IsRunning);

        a.World.Invoke(a.Seen(other), BlockKinds.SwitchOn);
        a.Tick(Rota.Ten);
        Assert.Equal(2, a.State.FiredCount);
    }

    /// <summary>
    /// A sequence can start another one of its own system.
    /// </summary>
    /// <remarks>
    /// Started and left to it: the calling sequence carries on, because an await waits for what it
    /// did itself rather than for a sequence somebody called.
    /// </remarks>
    [Fact]
    public void ASequenceCanRunAnother()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var warn = a.Automation.AddSequence(system);
        warn.Name = "Warn";
        Then(warn, "close", a.Ref(a.Seen(a.Outer)));

        var caller = a.Automation.AddSequence(system);
        caller.Steps.Add(new Run(warn.Number));
        Then(caller, "close", a.Ref(a.Seen(a.Inner)));

        a.Inner.ForceOpen();
        a.Outer.ForceOpen();
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(caller);
        a.Tick();

        // Both doors closing: the called sequence started, and the caller did not wait for it.
        Assert.Equal(["Outer Door:close", "Inner Door:close"], a.Log);
    }

    /// <summary>A sequence can stop another one of its own system where it is running, so the rest of it never happens.</summary>
    [Fact]
    public void ASequenceCanStopAnother()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var slow = a.Automation.AddSequence(system);
        slow.Steps.Add(new Wait(600));
        Then(slow, "close", a.Ref(a.Seen(a.Outer)));

        var stopper = a.Automation.AddSequence(system);
        stopper.Steps.Add(new Run(slow.Number) { Stops = true });

        a.Outer.ForceOpen();
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(slow);
        a.Tick();
        Assert.True(a.Runner.Get(slow)!.IsRunning);

        a.Runner.Call(stopper);
        a.Tick();
        Assert.False(a.Runner.Get(slow)!.IsRunning);

        // Past where the wait would have ended: the outer door was never closed.
        a.Tick(700);
        Assert.Empty(a.Log);
    }

    /// <summary>A sequence that would start itself, directly or through one it runs, is at fault at the step that does it, and its system does not run.</summary>
    [Fact]
    public void ASequenceThatWouldStartItselfIsAtFault()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var first = a.Automation.AddSequence(system);
        var second = a.Automation.AddSequence(system);
        Then(first, "close", a.Ref(a.Seen(a.Outer)));
        first.Steps.Add(new Run(second.Number));
        second.Steps.Add(new Run(first.Number));

        var alone = a.Automation.AddSequence(system);
        alone.Steps.Add(new Run(alone.Number));

        system.Enabled = true;
        a.Automation.Edited();
        a.Tick(3);

        var fault = SequenceSummary.Fault(a.Runner.Get(first));
        Assert.Equal(SequenceProblem.Circular, fault.Problem);
        Assert.Equal(2, fault.Step);
        Assert.Equal(SequenceProblem.Circular, SequenceSummary.Fault(a.Runner.Get(alone)).Problem);
        Assert.False(system.Enabled);
    }

    /// <summary>A <c>when</c> on a block that never reaches its moment is at fault, naming the block, rather than waiting forever.</summary>
    [Fact]
    public void AMomentTheBlockHasNotGotIsAtFault()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var never = new WhenTrigger("no-such-moment");
        never.Sources.Add(a.Ref(a.Seen(a.Inner)));
        var sequence = a.Automation.AddSequence(system, never);
        Then(sequence, "close", a.Ref(a.Seen(a.Outer)));

        system.Enabled = true;
        a.Automation.Edited();
        a.Tick(3);

        var fault = SequenceSummary.Fault(a.Runner.Get(sequence));
        Assert.Equal(SequenceProblem.NeverTriggered, fault.Problem);
        Assert.Equal(Unwired.NoMoment, fault.Unwired);
        Assert.Equal("Inner Door", fault.Which);
    }

    /// <summary>Running another sequence, and stopping one, start nothing again.</summary>

    [Fact]
    public void RunningOnAndStoppingAreNoCycle()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var first = a.Automation.AddSequence(system);
        var second = a.Automation.AddSequence(system);
        first.Steps.Add(new Run(second.Number));
        second.Steps.Add(new Run(first.Number, true));
        first.Steps.Add(new Run(first.Number, true));

        a.Automation.Edited();
        a.Tick(3);

        Assert.NotEqual(SequenceProblem.Circular, SequenceSummary.Fault(a.Runner.Get(first)).Problem);
        Assert.NotEqual(SequenceProblem.Circular, SequenceSummary.Fault(a.Runner.Get(second)).Problem);
    }

    /// <summary>An await waits for a sequence started above it to finish, as it does for an action.</summary>
    [Fact]
    public void AnAwaitWaitsForASequenceItStarted()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var slow = a.Automation.AddSequence(system);
        slow.Steps.Add(new Wait(100));
        Then(slow, "close", a.Ref(a.Seen(a.Inner)));

        var waiting = a.Automation.AddSequence(system);
        waiting.Steps.Add(new Run(slow.Number));
        waiting.Steps.Add(new Await());
        Then(waiting, "close", a.Ref(a.Seen(a.Outer)));

        a.Inner.ForceOpen();
        a.Outer.ForceOpen();
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(waiting);
        a.Tick(90);
        Assert.Empty(a.Log);
        Assert.True(a.Runner.Get(waiting)!.IsWaiting);

        a.Tick(200);
        Assert.Equal(["Inner Door:close", "Outer Door:close"], a.Log);
    }

    /// <summary>A step switched off is never done, and the steps around it run as though it were not there.</summary>
    [Fact]
    public void AStepSwitchedOffIsNeverDone()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var cycle = a.Automation.AddSequence(system);
        Then(cycle, "close", a.Ref(a.Seen(a.Inner)));
        Then(cycle, "close", a.Ref(a.Seen(a.Outer)));
        cycle.Steps[2].IsOff = true;
        cycle.Steps[3].IsOff = true;
        var wait = new Wait(600) { IsOff = true };
        cycle.Steps.Add(wait);
        Then(cycle, "open", a.Ref(a.Seen(a.Outer)));

        a.Inner.ForceOpen();
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(cycle);
        a.Tick(150);

        Assert.Equal(["Inner Door:close", "Outer Door:open"], a.Log);
        Assert.False(a.Runner.Get(cycle)!.IsRunning);
    }

    /// <summary>A step switched off between an action and its await leaves the await covering the action.</summary>
    [Fact]
    public void AStepSwitchedOffBetweenAnActionAndItsAwaitLeavesItAwaited()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var cycle = a.Automation.AddSequence(system);
        cycle.Steps.Add(Do("close", a.Ref(a.Seen(a.Inner))));
        cycle.Steps.Add(new Wait(600) { IsOff = true });
        cycle.Steps.Add(new Await());
        Then(cycle, "open", a.Ref(a.Seen(a.Outer)));

        a.Inner.ForceOpen();
        system.Enabled = true;
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(cycle);
        a.Tick(150);

        Assert.Equal(["Inner Door:close", "Outer Door:open"], a.Log);
        Assert.NotEqual(SequenceProblem.Unawaited, SequenceSummary.Fault(a.Runner.Get(cycle)).Problem);
    }

    /// <summary>A block switched off that is already where the step would take it has nothing to do, and is no trouble.</summary>
    [Fact]
    public void ABlockSwitchedOffAlreadyThereIsNoTrouble()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var cycle = a.Automation.AddSequence(system);
        Then(cycle, "close", a.Ref(a.Seen(a.Inner)));
        Then(cycle, "open", a.Ref(a.Seen(a.Outer)));

        a.Inner.Enabled = false;
        system.Enabled = true;
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(cycle);
        a.Tick(150);

        Assert.Contains("Outer Door:open", a.Log);
        Assert.NotEqual(SequenceProblem.Unable, SequenceSummary.Fault(a.Runner.Get(cycle)).Problem);
        Assert.True(system.Enabled);
    }

    /// <summary>An await switched off waits for nothing, so the step above it goes on without waiting, which is a fault.</summary>
    [Fact]
    public void AnAwaitSwitchedOffLeavesItsStepUnawaited()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var cycle = a.Automation.AddSequence(system);
        Then(cycle, "close", a.Ref(a.Seen(a.Inner)));
        cycle.Steps[1].IsOff = true;

        var both = a.Automation.AddSequence(system);
        Then(both, "close", a.Ref(a.Seen(a.Inner)));
        both.Steps[0].IsOff = true;
        both.Steps[1].IsOff = true;

        a.Automation.Edited();
        a.Tick(3);

        Assert.Equal(SequenceProblem.Unawaited, SequenceSummary.Fault(a.Runner.Get(cycle)).Problem);
        Assert.NotEqual(SequenceProblem.Unawaited, SequenceSummary.Fault(a.Runner.Get(both)).Problem);
    }

    /// <summary>A run switched off starts nothing, so it makes no cycle.</summary>
    [Fact]
    public void ARunSwitchedOffMakesNoCycle()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var alone = a.Automation.AddSequence(system);
        alone.Steps.Add(new Run(alone.Number) { IsOff = true });

        a.Automation.Edited();
        a.Tick(3);

        Assert.NotEqual(SequenceProblem.Circular, SequenceSummary.Fault(a.Runner.Get(alone)).Problem);
    }

    /// <summary>An action asked of a block that is switched off ends the run at that step, naming the block, and switches the system off.</summary>
    [Fact]
    public void AnActionOnABlockSwitchedOffEndsTheRun()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var cycle = a.Automation.AddSequence(system);
        Then(cycle, "close", a.Ref(a.Seen(a.Inner)));
        Then(cycle, "open", a.Ref(a.Seen(a.Outer)));

        a.Inner.ForceOpen();
        a.Inner.Enabled = false;
        system.Enabled = true;
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(cycle);
        a.Tick(20);

        var fault = SequenceSummary.Fault(a.Runner.Get(cycle));
        Assert.Equal(SequenceProblem.Unable, fault.Problem);
        Assert.Equal(0, fault.Step);
        Assert.Equal("Inner Door", fault.Which);
        Assert.DoesNotContain("Outer Door:open", a.Log);
        Assert.False(system.Enabled);
    }

    /// <summary>A block switched off by the step before is as unable as one switched off by hand.</summary>
    [Fact]
    public void ABlockSwitchedOffInTheStepBeforeCannotAct()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var cycle = a.Automation.AddSequence(system);
        cycle.Steps.Add(Do(BlockKinds.SwitchOff, a.Ref(a.Seen(a.Outer))));
        Then(cycle, "open", a.Ref(a.Seen(a.Outer)));

        system.Enabled = true;
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(cycle);
        a.Tick(20);

        var fault = SequenceSummary.Fault(a.Runner.Get(cycle));
        Assert.Equal(SequenceProblem.Unable, fault.Problem);
        Assert.Equal(1, fault.Step);
        Assert.Equal("Outer Door", fault.Which);
        Assert.DoesNotContain("Outer Door:open", a.Log);
    }

    /// <summary>The game's own value is a setting even where one of Sprocket's own lasts over the same control, so a block switched off takes it.</summary>
    [Fact]
    public void TheGamesOwnValueIsTakenByABlockSwitchedOff()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var cycle = a.Automation.AddSequence(system);
        var empty = new Act("Depressurize") { Form = Form.Value };
        empty.Targets.Add(a.Ref(a.Seen(a.Vent)));
        empty.Values.Add(Literal.Of(true));
        cycle.Steps.Add(empty);

        a.Vent.Enabled = false;
        system.Enabled = true;
        a.Tick(3);

        a.Runner.Call(cycle);
        a.Tick(20);

        Assert.NotEqual(SequenceProblem.Unable, SequenceSummary.Fault(a.Runner.Get(cycle)).Problem);
    }

    /// <summary>A setting given to a block that is switched off is taken as ever, since nothing has to happen.</summary>
    [Fact]
    public void ASettingIsTakenByABlockSwitchedOff()
    {
        var a = Build();
        var lamp = a.Ship.Light(9, "Lamp").Carrying("Radius", "Radius", ValueKind.Real, "1", "20");
        var system = a.Automation.Systems[0];

        var cycle = a.Automation.AddSequence(system);
        var radius = new Act("Radius") { Form = Form.Value };
        radius.Targets.Add(a.Ref(a.Seen(lamp)));
        radius.Values.Add(Literal.Of(4.5));
        cycle.Steps.Add(radius);
        Then(cycle, "close", a.Ref(a.Seen(a.Inner)));

        lamp.Enabled = false;
        a.Inner.ForceOpen();
        system.Enabled = true;
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(cycle);
        a.Tick(100);

        Assert.Contains("Inner Door:close", a.Log);
        Assert.NotEqual(SequenceProblem.Unable, SequenceSummary.Fault(a.Runner.Get(cycle)).Problem);
    }

    /// <summary>A block that is switched off can still be switched on, and then does what it is asked.</summary>
    [Fact]
    public void ABlockSwitchedOffCanBeSwitchedOnFirst()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var cycle = a.Automation.AddSequence(system);
        cycle.Steps.Add(Do(BlockKinds.SwitchOn, a.Ref(a.Seen(a.Inner))));
        Then(cycle, "close", a.Ref(a.Seen(a.Inner)));

        a.Inner.ForceOpen();
        a.Inner.Enabled = false;
        system.Enabled = true;
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(cycle);
        a.Tick(100);

        Assert.Contains("Inner Door:close", a.Log);
        Assert.NotEqual(SequenceProblem.Unable, SequenceSummary.Fault(a.Runner.Get(cycle)).Problem);
        Assert.True(system.Enabled);
    }

    /// <summary>A sequence may stop itself, and ends there.</summary>
    [Fact]
    public void ASequenceThatStopsItselfEndsThere()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var halting = a.Automation.AddSequence(system);
        Then(halting, "close", a.Ref(a.Seen(a.Inner)));
        halting.Steps.Add(new Run(halting.Number, true));
        Then(halting, "close", a.Ref(a.Seen(a.Outer)));

        a.Inner.ForceOpen();
        a.Outer.ForceOpen();
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(halting);
        a.Tick(200);

        Assert.Equal(["Inner Door:close"], a.Log);
        Assert.False(a.Runner.Get(halting)!.IsRunning);
    }

    /// <summary>A sequence stopped by one it ran ends there, as though it had stopped itself.</summary>
    [Fact]
    public void ASequenceStoppedByOneItRanEndsThere()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var first = a.Automation.AddSequence(system);
        var second = a.Automation.AddSequence(system);
        second.Steps.Add(new Run(first.Number, true));

        first.Steps.Add(new Run(second.Number));
        Then(first, "close", a.Ref(a.Seen(a.Outer)));

        a.Outer.ForceOpen();
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(first);
        a.Tick(200);

        Assert.Empty(a.Log);
        Assert.False(a.Runner.Get(first)!.IsRunning);
    }

    /// <summary>A sequence that meets trouble in one it ran goes no further, since the system is switched off.</summary>
    [Fact]
    public void ASequenceWhoseRunMeetsTroubleEndsThere()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var first = a.Automation.AddSequence(system);
        var second = a.Automation.AddSequence(system);
        Then(second, "close", a.Ref(a.Seen(a.Inner)));

        first.Steps.Add(new Run(second.Number));
        Then(first, "open", a.Ref(a.Seen(a.Outer)));

        a.Inner.ForceOpen();
        a.Inner.Enabled = false;
        system.Enabled = true;
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(first);
        a.Tick(100);

        Assert.DoesNotContain("Outer Door:open", a.Log);
        Assert.False(a.Runner.Get(first)!.IsRunning);
        Assert.False(system.Enabled);
    }

    /// <summary>A clock can stop a sequence, where a clock that ran it would start it.</summary>
    [Fact]
    public void AClockCanStopASequence()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var slow = a.Automation.AddSequence(system);
        slow.Steps.Add(new Wait(600));
        Then(slow, "close", a.Ref(a.Seen(a.Outer)));

        var clock = new EveryTrigger(30);
        clock.Does = new Run(slow.Number, true);
        system.AddTrigger(clock);
        a.Automation.Edited();

        a.Outer.ForceOpen();
        a.Tick(3);
        a.Log.Clear();

        a.Runner.Call(slow);
        a.Tick();
        Assert.True(a.Runner.Get(slow)!.IsRunning);

        a.Tick(100);
        Assert.False(a.Runner.Get(slow)!.IsRunning);

        a.Tick(700);
        Assert.Empty(a.Log);
    }

    /// <summary>A <c>when</c> may act on the blocks itself, with no sequence to run.</summary>
    [Fact]
    public void AWhenMayActItself()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var shut = new WhenTrigger("closed");
        shut.Sources.Add(a.Ref(a.Seen(a.Inner)));
        shut.Does = Do("open", a.Ref(a.Seen(a.Outer)));
        system.AddTrigger(shut);
        a.Automation.Edited();

        var closing = a.Automation.AddSequence(system);
        Then(closing, "close", a.Ref(a.Seen(a.Inner)));

        a.Inner.ForceOpen();
        a.Tick(20);
        a.Log.Clear();

        a.Runner.Call(closing);
        a.Tick(200);

        Assert.Equal(["Inner Door:close", "Outer Door:open"], a.Log);
        Assert.Single(a.Automation.Sequences, s => s.System == system && s != a.LetMeOut);
    }

    /// <summary>A <c>when</c> looks no more often than the runner allows, whatever it asks for.</summary>
    /// <remarks>
    /// Held to every thirty ticks, a vent switched on on tick 40 is seen on tick 61, as it is by a <c>when</c> that asks for
    /// thirty. Nothing announces a block being switched on, which is what leaves the cadence to decide.
    /// </remarks>
    [Fact]
    public void AWhenIsHeldToTheQuickestLook()
    {
        var a = Build();
        a.Runner.QuickestLook = 30;
        var system = a.Automation.Systems[0];

        var on = new WhenTrigger("switched-on");
        on.Sources.Add(a.Ref(a.Seen(a.Vent)));
        on.Does = Do("close", a.Ref(a.Seen(a.Inner)));
        system.AddTrigger(on);
        a.Automation.Edited();

        a.Vent.Enabled = false;
        a.Inner.ForceOpen();
        a.Tick(39);
        a.Log.Clear();

        a.Vent.Enabled = true;
        a.Tick(21);
        Assert.Empty(a.Log);

        a.Tick();
        Assert.Equal(["Inner Door:close"], a.Log);
    }

    /// <summary>With <c>any</c>, each block that reaches the moment fires on its own, even when both are found there at one look.</summary>
    [Fact]
    public void AnyFiresOnceForEachBlock()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var on = new WhenTrigger("switched-on");
        on.Sources.Add(a.Ref(a.Seen(a.Vent)));
        on.Sources.Add(a.Ref(a.Seen(a.Outer)));
        on.Does = Do("close", a.Ref(a.Seen(a.Inner)));
        system.AddTrigger(on);
        a.Automation.Edited();

        a.Vent.Enabled = false;
        a.Outer.Enabled = false;
        a.Inner.ForceOpen();
        a.Tick(20);
        a.Log.Clear();

        a.Vent.Enabled = true;
        a.Outer.Enabled = true;
        a.Tick(20);

        Assert.Equal(["Inner Door:close", "Inner Door:close"], a.Log);
        Assert.Equal(2, a.Runner.Poller(on, system.First)!.FiredCount);
    }

    /// <summary>The names a <c>when</c> watches stand for one set of blocks, so a block two variables hold fires once.</summary>
    [Fact]
    public void ABlockTwoVariablesHoldIsWatchedOnce()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var vent = a.Automation.FindRef(a.Ref(a.Seen(a.Vent)))!;
        var again = new EntityRef("Vent again", vent.KindId);
        again.Keys.AddRange(vent.Keys);
        a.Automation.Restore(again);

        var on = new WhenTrigger("switched-on");
        on.Sources.Add(vent.Name);
        on.Sources.Add(again.Name);

        on.Does = Do("close", a.Ref(a.Seen(a.Inner)));
        system.AddTrigger(on);
        a.Automation.Edited();

        a.Vent.Enabled = false;
        a.Inner.ForceOpen();
        a.Tick(20);
        a.Log.Clear();

        a.Vent.Enabled = true;
        a.Tick(20);

        Assert.Equal(["Inner Door:close"], a.Log);
        Assert.Equal(1, a.Runner.Poller(on, system.First)!.FiredCount);
    }


    /// <summary>A moment the block announces is looked for on the tick after, whatever the <c>when</c> asks for.</summary>
    [Fact]
    public void AnAnnouncedMomentIsSeenOnTheTickAfter()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var opened = new WhenTrigger("opened");
        opened.Sources.Add(a.Ref(a.Seen(a.Outer)));
        opened.IntervalTicks = 600;
        opened.Does = Do("close", a.Ref(a.Seen(a.Inner)));
        system.AddTrigger(opened);
        a.Automation.Edited();

        a.Inner.ForceOpen();
        a.Tick(39);
        a.Log.Clear();

        a.Outer.ForceOpen();
        a.Tick();

        Assert.Equal(["Inner Door:close"], a.Log);

        // One for finishing opening and one for finishing closing.
        Assert.Equal(2, a.Outer.Listeners);
    }

    /// <summary>A moment whose announcement never comes is still seen, by the look every hundred ticks.</summary>
    [Fact]
    public void AnAnnouncementThatNeverComesIsCaughtByTheBackstop()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var opened = new WhenTrigger("opened");
        opened.Sources.Add(a.Ref(a.Seen(a.Outer)));
        opened.Does = Do("close", a.Ref(a.Seen(a.Inner)));
        system.AddTrigger(opened);
        a.Automation.Edited();

        a.Inner.ForceOpen();
        a.Tick(39);
        a.Log.Clear();

        a.Outer.Announces = false;
        a.Outer.ForceOpen();
        a.Tick(40);
        Assert.Empty(a.Log);

        a.Tick(100);
        Assert.Equal(["Inner Door:close"], a.Log);
    }

    /// <summary>Deleting a <c>when</c> stops it listening to its blocks.</summary>
    [Fact]
    public void AWhenGoneListensToNothing()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var opened = new WhenTrigger("opened");
        opened.Sources.Add(a.Ref(a.Seen(a.Outer)));
        opened.Does = Do("close", a.Ref(a.Seen(a.Inner)));
        system.AddTrigger(opened);
        a.Automation.Edited();
        a.Tick(3);
        Assert.Equal(2, a.Outer.Listeners);

        system.ReplaceTrigger(opened, null);
        a.Automation.Edited();
        a.Tick(3);
        Assert.Equal(0, a.Outer.Listeners);
    }

    /// <summary>A <c>when</c> that says how often it looks looks that often, and fires on the look that sees the moment.</summary>
    /// <remarks>
    /// Its first look is on the first tick after it is made, which is tick 1, and every thirtieth tick after that; so a vent
    /// switched on on tick 40 is seen on tick 61, where one left to the default of ten would be seen on tick 41. Nothing
    /// announces a block being switched on, which is what leaves the cadence to decide.
    /// </remarks>
    [Fact]
    public void AWhenLooksAsOftenAsItSays()
    {
        var a = Build();
        var system = a.Automation.Systems[0];

        var on = new WhenTrigger("switched-on");
        on.Sources.Add(a.Ref(a.Seen(a.Vent)));
        on.IntervalTicks = 30;
        on.Does = Do("close", a.Ref(a.Seen(a.Inner)));
        system.AddTrigger(on);
        a.Automation.Edited();

        a.Vent.Enabled = false;
        a.Inner.ForceOpen();
        a.Tick(39);
        a.Log.Clear();

        a.Vent.Enabled = true;
        a.Tick(21);
        Assert.Empty(a.Log);

        a.Tick();
        Assert.Equal(["Inner Door:close"], a.Log);
    }

    /// <summary>
    /// The airlock as a player would actually write it: secure the side you are not
    /// using, then open the side you are.
    /// </summary>
    /// <remarks>
    /// There is no safety feature here. No interlock, no guard, no second set of
    /// sequences watching the doors. Safety is simply the first half of the
    /// procedure, which is how a careful person operates a real airlock.
    ///
    /// Note that step 1 needs no condition on it. Closing an already-closed door
    /// finishes instantly, so "make sure the outer door is shut" and "close the outer
    /// door" are the same sentence - and nobody ever has to write "if it is open,
    /// close it".
    ///
    /// Locking is switching off, because that is the only lock a block has: a disabled door
    /// cannot be operated, by a sequence or by hand.
    /// </remarks>
    static Sequence Cycle(Airlock a, string name, Door leave, Door enter, string pump)
    {
        var sequence = a.Automation.AddSequence(a.Automation.Systems[0]);
        sequence.Name = name;
        Then(sequence, "close", a.Ref(a.Seen(leave)));
        Then(sequence, BlockKinds.SwitchOff, a.Ref(a.Seen(leave)));
        Then(sequence, pump, a.Ref(a.Seen(a.Vent)));
        Then(sequence, BlockKinds.SwitchOn, a.Ref(a.Seen(enter)));
        Then(sequence, "open", a.Ref(a.Seen(enter)));
        return sequence;
    }

    [Fact]
    public void BothDoorsAreNeverOpenAtOnce_WithNoSafetyFeatureAtAll()
    {
        var a = Build();
        var letMeOut = Cycle(a, "Let me out", a.Inner, a.Outer, "depressurize");

        // Someone has just walked in from inside, so the inner door is open.
        a.Inner.ForceOpen();
        a.Tick(3);
        a.Runner.Call(letMeOut);

        for (var i = 0; i < 200; i++)
        {
            a.Tick();
            Assert.False(a.Inner.Status == DoorStatus.Open && a.Outer.Status == DoorStatus.Open,
                $"both doors open at tick {i}");
        }

        Assert.Equal(DoorStatus.Open, a.Outer.Status);
        Assert.Equal(DoorStatus.Closed, a.Inner.Status);
    }

    [Fact]
    public void MakingSureIsTheSameAsDoing()
    {
        // The outer door is already shut, so step 1 costs nothing and the sequence
        // moves straight on. This is what removes the need for conditionals.
        var a = Build();
        var letMeOut = Cycle(a, "Let me out", a.Inner, a.Outer, "depressurize");

        a.Tick(3);
        a.Log.Clear();
        a.Runner.Call(letMeOut);

        Assert.Equal(["Inner Door:close", "Inner Door:off", "Airlock Vent:depressurize"], a.Log);
    }

    [Fact]
    public void LockingBeforeTheDoorHasShutPinsItOpen()
    {
        // Why the await is written after every step. Locking is disabling, and a disabled door
        // stops where it is, so overlapping the two leaves the door the sequence was securing
        // standing open.
        var a = Build();
        var letMeOut = Cycle(a, "Let me out", a.Inner, a.Outer, "depressurize");

        // The await after the first step taken out, which is what makes it overlap the next one.
        letMeOut.Steps.RemoveAt(1);

        a.Inner.ForceOpen();
        a.Tick(3);
        a.Runner.Call(letMeOut);
        a.Tick(200);

        Assert.Equal(DoorStatus.Open, a.Inner.Status);

        // And the outer door is never opened onto it. A step that does not wait is a fault, so the
        // system is switched off rather than left to carry on.
        Assert.Equal(DoorStatus.Closed, a.Outer.Status);
        Assert.False(letMeOut.System.Enabled, "a system with a step that does not wait was left on");
    }

    /// <summary>
    /// The other way people build airlocks: no cycle button at all, just a mutual
    /// exclusion between the doors, so operating one by hand secures the other.
    /// </summary>
    /// <remarks>
    /// Neither style is privileged by the system, which is the point - Sprocket is an
    /// automation system, not a shipped airlock.
    ///
    /// This only ever locks and unlocks; it never closes. That matters. A version
    /// that closed the opposite door would feed back on itself: closing the outer
    /// door raises "outer door finishes closing", which would then close the inner
    /// door the player had just walked through. Locking raises no such event.
    /// </remarks>
    static void Mutex(Airlock a, Door door, Door other)
    {
        var opened = new WhenTrigger("opened");
        opened.Sources.Add(a.Ref(a.Seen(door)));
        var secure = a.Automation.AddSequence(opened);
        secure.Name = $"Secure the other door while {door.Name} is open";
        Then(secure, BlockKinds.SwitchOff, a.Ref(a.Seen(other)));

        var shut = new WhenTrigger("closed");
        shut.Sources.Add(a.Ref(a.Seen(door)));
        var release = a.Automation.AddSequence(shut);
        release.Name = $"Release the other door when {door.Name} shuts";
        Then(release, BlockKinds.SwitchOn, a.Ref(a.Seen(other)));
    }

    [Fact]
    public void OperatingOneDoorByHandSecuresTheOther()
    {
        var a = Build();
        Mutex(a, a.Inner, a.Outer);
        Mutex(a, a.Outer, a.Inner);

        a.Tick(3);

        // The player opens the inner door themselves - Sprocket never touches it.
        a.Inner.ForceOpen();
        a.Tick(3);

        // Anything trying to open the outer door now gets nowhere.
        var careless = a.Automation.AddSequence(new EveryTrigger(1));
        Then(careless, "open", a.Ref(a.Seen(a.Outer)));
        a.Tick(60);

        Assert.NotEqual(DoorStatus.Open, a.Outer.Status);
    }

    [Fact]
    public void ClosingTheDoorReleasesTheOtherOne()
    {
        var a = Build();
        Mutex(a, a.Inner, a.Outer);
        Mutex(a, a.Outer, a.Inner);

        a.Tick(3);
        a.Inner.ForceOpen();
        a.Tick(3);

        a.World.Invoke(a.Seen(a.Inner), "close");
        a.Tick(60);

        var opener = a.Automation.AddSequence(new EveryTrigger(1));
        Then(opener, "open", a.Ref(a.Seen(a.Outer)));
        a.Tick(60 + 2 * Rota.Ten);

        Assert.Equal(DoorStatus.Open, a.Outer.Status);
    }

    [Fact]
    public void TheReverseDirectionIsTheSameSequenceMirrored()
    {
        var a = Build();
        var letMeIn = Cycle(a, "Let me in", a.Outer, a.Inner, "pressurize");

        a.Outer.ForceOpen();
        a.Tick(3);
        a.Runner.Call(letMeIn);
        a.Tick(200);

        Assert.Equal(DoorStatus.Open, a.Inner.Status);
        Assert.Equal(DoorStatus.Closed, a.Outer.Status);
    }
}

/// <summary>
/// A reading is a reading, whatever sort it is.
/// </summary>
/// <remarks>
/// The model used to have boolean signals as its primitive, so a tank had to decide it was
/// "low" and hand core a flag, throwing the number away before anything could ask a different
/// question of it. A flag is just a property whose readings are true and false; the level and
/// the label are properties too, and a state is a relation against whichever one it names.
/// </remarks>
public class PropertyTests
{
    readonly Values _store = new Values();

    /// <summary>
    /// A kind with a reading of each sort, declared here rather than taken off a block.
    /// </summary>
    /// <remarks>
    /// These tests are about the vocabulary and nothing else: what a state compares, and what it
    /// answers. A ship would only be a way of getting hold of a kind, and the fixture that used to
    /// supply one had to invent a tank the game does not offer.
    /// </remarks>
    EntityKind Tank()
    {
        return new EntityKind("tank", "Hydrogen Tank")
            .WithProperty("fill", ValueKind.Real, "fill level")
            .WithState("is-low", "fill", Relation.Below, EntityValue.Of(0.25), "is low")
            .WithState("is-fine", "fill", Relation.AtLeast, EntityValue.Of(0.25), "has plenty")
            .WithProperty("label", ValueKind.Text, "label")
            .WithState("is-spare", "label", Relation.Is, _store.Text("spare"), "is the spare");
    }

    [Fact]
    public void AStateOverANumberComparesAgainstIt()
    {
        EntityKind tank = Tank();
        var low = tank.FindState("is-low")!;
        var fine = tank.FindState("is-fine")!;

        Assert.True(low.Holds(_store, EntityValue.Of(0.10)));
        Assert.False(fine.Holds(_store, EntityValue.Of(0.10)));

        // The boundary belongs to "has plenty": is-low is Below, is-fine is AtLeast, so the
        // pair covers every reading exactly once and no level is both or neither.
        Assert.False(low.Holds(_store, EntityValue.Of(0.25)));
        Assert.True(fine.Holds(_store, EntityValue.Of(0.25)));
    }

    [Fact]
    public void AStateOverTextComparesAgainstIt()
    {
        var spare = Tank().FindState("is-spare")!;

        Assert.False(spare.Holds(_store, _store.Text("main")));
        Assert.True(spare.Holds(_store, _store.Text("spare")));
    }

    [Fact]
    public void AFlagIsJustAPropertyWhoseReadingsAreTrueAndFalse()
    {
        EntityKind door = new EntityKind("door", "Door")
            .WithProperty("open", "open")
            .WithState("is-open", "open", true, "is open");

        var open = door.FindState("is-open")!;

        Assert.Equal(ValueKind.Flag, door.FindProperty("open")!.Kind);
        Assert.Equal(Relation.Is, open.Relation);
        Assert.True(_store.Flag(ValueKind.Flag, open.Operand));
    }

    /// <summary>Only a number can be above something; anything can equal something.</summary>
    [Fact]
    public void TheRelationsOfferedDependOnWhatIsBeingRead()
    {
        Assert.True(Relations.Suits(Relation.Is, ValueKind.Text));
        Assert.False(Relations.Suits(Relation.Above, ValueKind.Text));
        Assert.False(Relations.Suits(Relation.Above, ValueKind.Flag));
        Assert.True(Relations.Suits(Relation.Above, ValueKind.Real));
    }

    /// <summary>
    /// The switch a pair of words has already said does not reach the list a third time.
    /// </summary>
    /// <remarks>
    /// A vent's switch is called Depressurize, and the game offers it three ways: the value, the act
    /// that flips it, and the pair that turns it each way. Sprocket says the pair as pressurize and
    /// depressurize, so the value beneath it is the same sentence written backwards.
    /// </remarks>
    [Fact]
    public void AValueThePairAlreadySaysIsLeftOut()
    {
        var ship = new Rigged();
        var vent = ship.Vent(1, "Vent");
        var kind = ship.Entity(vent.Id).Kind;

        Assert.True(kind.FindAction("Depressurize", Form.Value)!.Hidden);

        // The act that flips it stays: nothing else in the list says "whichever way it is not".
        Assert.False(kind.FindAction("Depressurize", Form.Action)!.Hidden);
    }

}
