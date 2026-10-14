using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;
using DoorStatus = Sandbox.ModAPI.Ingame.DoorStatus;

namespace Sprocket.Tests;

/// <summary>
/// The world's turn: work looked at now and then is spread across the ticks, and a wait is looked at
/// as often as how close it should be to arriving.
/// </summary>
public class RotaTests
{
    [Fact]
    public void PlacesAreHandedOutInTurnAndComeRound()
    {
        var rota = new Rota();

        for (var i = 0; i < Rota.Hundred; i++)
            Assert.Equal(i, rota.Join());

        Assert.Equal(0, rota.Join());
    }

    /// <summary>
    /// A world's worth of work at every-ten costs a tenth of it on each tick, never all of it on one.
    /// </summary>
    [Theory]
    [InlineData(Rota.Ten)]
    [InlineData(Rota.Hundred)]
    public void EachTickTakesItsShare(int period)
    {
        var rota = new Rota();
        var calls = new int[period * 3];
        for (var i = 0; i < Rota.Hundred; i++)
            rota.Every(new Shift(rota.Join(), tick => calls[tick]++), period);

        for (var tick = 0; tick < calls.Length; tick++)
            rota.Run(tick);

        Assert.All(calls, count => Assert.Equal(Rota.Hundred / period, count));
    }

    /// <summary>A frame counter that jumps leaves nothing behind: every tick between is still run.</summary>
    [Fact]
    public void ASkippedTickIsStillRun()
    {
        var rota = new Rota();
        var ran = new List<int>();
        rota.Every(new Shift(7, ran.Add), Rota.Ten);
        var deadline = new Shift(0, ran.Add);
        rota.At(deadline, 25);

        rota.Run(1);
        rota.Run(40);

        Assert.Equal([7, 17, 25, 27, 37], ran);
        Assert.False(deadline.Listed);
    }

    /// <summary>Something done once is done on its tick and then taken off.</summary>
    [Fact]
    public void ADeadlineIsMetOnItsTickAndOnlyOnce()
    {
        var rota = new Rota();
        var ran = new List<int>();
        rota.Run(0);
        rota.At(new Shift(3, ran.Add), 250);

        for (var tick = 1; tick < 1000; tick++)
            rota.Run(tick);

        Assert.Equal([250], ran);
        Assert.Equal(0, rota.Count);
    }

    /// <summary>
    /// Something moved or taken off while a tick is being run is neither done twice nor done after it
    /// left.
    /// </summary>
    [Fact]
    public void MovingAndLeavingDuringATickAreSafe()
    {
        var rota = new Rota();
        var done = new List<string>();
        Shift mover = null!;
        Shift leaver = null!;

        // ReSharper disable AccessToModifiedClosure
        // Both are named before they exist, because what is under test is a shift that re-lists itself
        // and drops another one while the turn that called it is still running.
        mover = new Shift(0, _ =>
        {
            done.Add("mover");
            rota.Every(mover, Rota.Each);
            rota.Leave(leaver);
        });
        leaver = new Shift(0, _ => done.Add("leaver"));
        // ReSharper restore AccessToModifiedClosure
        rota.Every(mover, Rota.Ten);
        rota.Every(leaver, Rota.Ten);

        rota.Run(10);

        Assert.Equal(["mover"], done);
        Assert.True(mover.Listed);
        Assert.False(leaver.Listed);
    }

    /// <summary>
    /// A running sequence always has something on the rota to move it on, whatever it has stopped at.
    /// </summary>
    [Fact]
    public void ARunIsNeverLeftOffTheRota()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var door = ship.Door(1, "Door");
        var lamp = ship.Light(2, "Lamp");
        door.ForceOpen();

        var sequence = automation.AddSequence((Trigger)null!);
        Then(sequence, "close", automation.Declare(ship.World, ship.Entity(door.Id)).Name);
        sequence.Steps.Add(new Wait(30));
        Then(sequence, Given.Power(true, automation.Declare(ship.World, ship.Entity(lamp.Id)).Name));

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        ship.Tick();
        runner.Tick(++frame);

        // A sequence at rest is on no shift. A run has two: the watch on it, and its next step.
        var idle = runner.Rota.Count;
        runner.Call(sequence);
        var state = runner.Get(sequence)!;

        while (state.IsRunning && frame < 2000)
        {
            Assert.Equal(idle + 2, runner.Rota.Count);
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.False(state.IsRunning);
        Assert.Contains("Lamp:on", ship.Rig.Log);
        Assert.Equal(idle, runner.Rota.Count);
    }

    [Theory]
    [InlineData(500, 0, Rota.Hundred)]
    [InlineData(500, 460, Rota.Ten)]
    [InlineData(500, 490, Rota.Each)]
    [InlineData(500, 510, Rota.Ten)]
    [InlineData(500, 800, Rota.Hundred)]
    public void AWaitIsLookedAtAsOftenAsItsTimeLeftDeserves(int expected, int elapsed, int period)
    {
        var hold = new Hold(1000, expected);

        Assert.Equal(period, hold.Period(1000 + elapsed));
    }

    /// <summary>
    /// A block that cannot say how long it takes is looked at every ten ticks, however long it has
    /// been.
    /// </summary>
    [Fact]
    public void AWaitNobodyCanEstimateIsLookedAtEveryTen()
    {
        var hold = new Hold(0, GridEntity.Unestimated);

        Assert.Equal(Rota.Ten, hold.Period(0));
        Assert.Equal(Rota.Ten, hold.Period(10_000));
    }

    /// <summary>
    /// A late wait is never looked at on every tick: past its time is exactly when it may never come.
    /// </summary>
    [Fact]
    public void ALateWaitNeverGoesBackToEveryTick()
    {
        var hold = new Hold(0, 100);

        for (var tick = 101; tick < 1000; tick++)
            Assert.NotEqual(Rota.Each, hold.Period(tick));
    }

    /// <summary>
    /// A door that has shut is noticed on the sequence's own tick in the turn, and the next step
    /// follows then - within the ten ticks, and not before its place comes round.
    /// </summary>
    [Fact]
    public void AHeldStepMovesOnWhenItsPlaceComesRound()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var inner = ship.Door(1, "Inner Door");
        var vent = ship.Vent(2, "Airlock Vent");
        inner.ForceOpen();

        var sequence = automation.AddSequence((Trigger)null!);
        Then(sequence, "close", automation.Declare(ship.World, ship.Entity(inner.Id)).Name);
        Then(sequence, "depressurize", automation.Declare(ship.World, ship.Entity(vent.Id)).Name);

        // The sequence is the first to join the turn, so it takes place nought and is due on every
        // tick ending in nought.
        var runner = new AutomationRunner(automation, ship.World);

        var frame = 0;
        void Tick()
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Tick();
        runner.Call(sequence);
        Tick();

        var shut = -1;
        while (frame < 1000 && !ship.Rig.Log.Contains("Airlock Vent:depressurize"))
        {
            Tick();
            if (shut < 0 && inner.Status == DoorStatus.Closed)
                shut = frame;
        }

        Assert.True(shut > 0, "the door never shut");
        Assert.Equal(0, frame % Rota.Ten);
        Assert.InRange(frame - shut, 0, Rota.Ten - 1);
    }

    /// <summary>
    /// A timer keeps the game's time though it is only looked at when its place comes round: a
    /// period that is no multiple of ten still fires as often as it says, and does not drift.
    /// </summary>
    [Fact]
    public void ATimerKeepsTimeBetweenLooks()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var lamp = ship.Light(1, "Lamp");

        var sequence = automation.AddSequence(new EveryTrigger(35));
        Then(sequence, Given.Power(true, automation.Declare(ship.World, ship.Entity(lamp.Id)).Name));

        var runner = new AutomationRunner(automation, ship.World);
        for (var frame = 1; frame <= 3500; frame++)
        {
            ship.Tick();
            runner.Tick(frame);
        }

        Assert.InRange(runner.Get(sequence)!.FiredCount, 99, 100);
    }

    /// <summary>
    /// A block's change is seen when the trigger's place comes round, and it fires then.
    /// </summary>
    /// <remarks>
    /// The sequence's runner joins the rota first and takes place 0, and the trigger's watch takes place 1, so a full vent
    /// switched back on after tick 23, which finishes filling there, is seen on tick 31. Nothing announces a vent filling,
    /// which is what leaves its place to decide.
    /// </remarks>
    [Fact]
    public void ATriggerFiresWhenItsPlaceComesRound()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var vent = ship.Vent(1, "Vent");
        var lamp = ship.Light(2, "Lamp");

        vent.Enabled = false;

        var on = SignalTrigger.Written("ext", "finishes-filling");
        on.Sources.Add(automation.Declare(ship.World, ship.Entity(vent.Id)).Name);
        var sequence = automation.AddSequence(on);
        Then(sequence, Given.Power(true, automation.Declare(ship.World, ship.Entity(lamp.Id)).Name));

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        for (; frame < 23; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        vent.Enabled = true;
        while (frame < 100 && runner.Get(sequence)!.FiredCount == 0)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.Equal(31, frame);
    }

    /// <summary>
    /// A wait lasts as long as it says in the game's ticks, however seldom the runner is woken.
    /// </summary>
    [Fact]
    public void AWaitKeepsTheGamesTimeNotTheRunnersCalls()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var lamp = ship.Light(1, "Lamp");

        var sequence = automation.AddSequence((Trigger)null!);
        sequence.Steps.Add(new Wait(35));
        Then(sequence, Given.Power(true, automation.Declare(ship.World, ship.Entity(lamp.Id)).Name));

        var runner = new AutomationRunner(automation, ship.World);
        runner.Tick(10);
        runner.Call(sequence);

        // Woken once in ten ticks: a wait counting its wake-ups would take ten times as long.
        for (var frame = 20; frame <= 50; frame += 10)
            runner.Tick(frame);

        Assert.Contains("Lamp:on", ship.Rig.Log);
    }

    /// <summary>
    /// A wait that follows a held step lasts exactly as long as it says, however that step was being
    /// looked at.
    /// </summary>
    [Fact]
    public void AWaitAfterAHeldStepEndsOnItsTick()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var door = ship.Door(1, "Door");
        var lamp = ship.Light(2, "Lamp");
        door.ForceOpen();

        var sequence = automation.AddSequence((Trigger)null!);
        Then(sequence, "close", automation.Declare(ship.World, ship.Entity(door.Id)).Name);
        sequence.Steps.Add(new Wait(35));
        Then(sequence, Given.Power(true, automation.Declare(ship.World, ship.Entity(lamp.Id)).Name));

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        ship.Tick();
        runner.Tick(++frame);
        runner.Call(sequence);
        var state = runner.Get(sequence)!;

        var waiting = -1;
        while (!ship.Rig.Log.Contains("Lamp:on") && frame < 2000)
        {
            ship.Tick();
            runner.Tick(++frame);
            if (waiting < 0 && state.CurrentStep == 2)
                waiting = frame;
        }

        Assert.Equal(35, frame - waiting);
    }

    /// <summary>
    /// The runners are matched to the program when the program says it changed, and on no other tick.
    /// </summary>
    [Fact]
    public void AnOrdinaryTickMatchesNothing()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var runner = new AutomationRunner(automation, ship.World);

        for (var frame = 1; frame <= 100; frame++)
            runner.Tick(frame);

        Assert.Equal(1, runner.Reconciled);
        Assert.NotNull(runner.Get(sequence));

        var added = automation.AddSequence(sequence.System);
        runner.Tick(101);
        runner.Tick(102);

        Assert.Equal(2, runner.Reconciled);
        Assert.NotNull(runner.Get(added));

        automation.RemoveSequence(added);
        runner.Tick(103);

        Assert.Equal(3, runner.Reconciled);
        Assert.Null(runner.Get(added));
    }

    /// <summary>
    /// What a world with nothing happening costs, counted.
    /// </summary>
    /// <remarks>
    /// The guard on the whole of the runtime's tick, whoever wrote what: three Sprockets, each with
    /// ten sequences started by a door and ten started by a button, and nothing running. A door says
    /// when it has moved, so it is looked at every hundred ticks in case it does not, and a button
    /// costs nothing until it is pressed - spread so no tick carries more than its share, and nothing
    /// matched to the program again after the first tick.
    /// </remarks>
    [Fact]
    public void AnIdleWorldCostsOnlyItsShare()
    {
        var ship = new Rigged();
        var door = ship.Door(1, "Door");
        var rota = new Rota();
        var runners = new List<AutomationRunner>();

        for (var sprocket = 0; sprocket < 3; sprocket++)
        {
            var automation = new Automation();
            var watched = automation.Declare(ship.World, ship.Entity(door.Id)).Name;
            for (var i = 0; i < 10; i++)
            {
                var opened = SignalTrigger.Written("ext", "finishes-opening");
                opened.Sources.Add(watched);
                var sequence = automation.AddSequence(opened);
                Then(sequence, "close", watched);

                var pressed = automation.AddSequence(sequence.System);
                Then(pressed, "close", watched);
            }

            runners.Add(new AutomationRunner(automation, ship.World, rota));
        }

        // Settled first: the first tick matches runners to programs, and every shift starts at the
        // quickest pace it could need until it has looked once.
        var frame = 0;
        for (; frame < 2 * Rota.Hundred; frame++)
        {
            foreach (var runner in runners)
                runner.Tick(frame);
            rota.Run(frame);
        }

        var before = rota.Done;
        var most = 0;
        for (var end = frame + 10 * Rota.Hundred; frame < end; frame++)
        {
            var was = rota.Done;
            foreach (var runner in runners)
                runner.Tick(frame);
            rota.Run(frame);
            most = Math.Max(most, rota.Done - was);
        }

        // Per thousand ticks: 30 doors looked at 10 times each. A program is checked when it changes
        // and on no tick, so an idle one costs nothing here.
        var total = rota.Done - before;
        Assert.Equal(30 * 10, total);

        // No tick carries more than twice its even share. Places are handed out in turn to
        // everything, so what is looked at every ten and what is looked at every hundred interleave
        // and the share is not exactly even; within twice of it is the bound.
        var share = (total + 999) / 1000;
        Assert.InRange(most, 1, 2 * share);
        Assert.All(runners, runner => Assert.Equal(1, runner.Reconciled));
    }

    static void Then(Sequence sequence, string actionId, string target)
    {
        var act = new Act(actionId);
        act.Targets.Add(target);
        sequence.Steps.Add(Given.Defaults(act));
        sequence.Steps.Add(new Await());
    }

    /// <summary>A step written whole, and the await after it.</summary>
    static void Then(Sequence sequence, Act step)
    {
        sequence.Steps.Add(step);
        sequence.Steps.Add(new Await());
    }
}
