using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A rotor, turned to an angle by the game's own limit and waited for until it gets there, and the
/// members of its terminal said in Sprocket's words.
/// </summary>
public class RotorTests
{
    /// <summary>Directly, clockwise and counter-clockwise, as the game numbers them.</summary>
    const long Directly = 0;
    const long Clockwise = 1;

    [Fact]
    public void WhatMattersAboutARotorIsSaidInSprocketsWords()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var kind = ship.Entity(rotor.Id).Kind;

        foreach (var id in new[] { RotorExtension.RotateTo, "reverse", "attach", "detach", "lock", "unlock" })
            Assert.NotNull(kind.FindAction(id, false));

        foreach (var id in new[] { "speed", "lower-limit", "upper-limit", "torque", "braking-torque" })
            Assert.NotNull(kind.FindAction(id, true));

        // The game's own are still there to be shown, and left out of the list otherwise.
        Assert.True(kind.FindAction("Reverse", false)!.Hidden);
        Assert.True(kind.FindAction("Velocity", true)!.Hidden);
        Assert.True(kind.FindAction("RotateToAngle", false)!.Hidden);

        // What is not promoted stays in the well as the game says it.
        Assert.False(kind.FindAction("Displacement", true)!.Hidden);
    }

    /// <summary>Sprocket's words reach the game's own members.</summary>
    [Fact]
    public void APromotedMemberDoesWhatTheGamesDoes()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var entity = ship.Entity(rotor.Id);

        ship.World.Invoke(entity, "reverse");
        ship.World.Invoke(entity, "speed", [Literal.Of(5.0)], true);

        Assert.Contains("Rotor:Reverse", ship.Rig.Log);
        Assert.Contains("Rotor:Velocity 5", ship.Rig.Log);
    }

    [Fact]
    public void TurningToAnAngleIsWaitedForUntilItGetsThere()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var lamp = ship.Light(2, "Lamp");

        var frame = Run(ship, rotor, lamp, Directly, 90, 10);

        Assert.InRange(rotor.Degrees, 89.9f, 90.1f);

        // Ninety degrees at a tenth of a degree a tick for each revolution a minute.
        Assert.InRange(frame, 90, 100);
    }

    /// <summary>
    /// Clockwise from 350 to 10 goes the short way past nought, and is not taken to have arrived
    /// because ten is less than three hundred and fifty.
    /// </summary>
    [Fact]
    public void TurningPastNoughtArrivesAtTheFarSide()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var lamp = ship.Light(2, "Lamp");
        rotor.Stand(350);

        var frame = Run(ship, rotor, lamp, Clockwise, 10, 10);

        Assert.InRange(rotor.Degrees, 9.9f, 10.1f);
        Assert.InRange(frame, 20, 30);
    }

    /// <summary>
    /// A turn to where the rotor already is arrives at once, and leaves the rotor held there as any
    /// finished turn does - which is the ordinary case of a sequence run twice.
    /// </summary>
    [Fact]
    public void TurningToWhereItAlreadyIsArrivesAtOnce()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var lamp = ship.Light(2, "Lamp");
        rotor.Stand(90);

        var frame = Run(ship, rotor, lamp, Directly, 90, 10);

        Assert.InRange(frame, 0, 1);
        Assert.InRange(rotor.Degrees, 89.9f, 90.1f);
    }

    /// <summary>The same sequence run a second time, after the first has left the rotor where it sent it.</summary>
    [Fact]
    public void ASecondRunToTheSameAngleArrivesAtOnce()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var lamp = ship.Light(2, "Lamp");

        Run(ship, rotor, lamp, Directly, 90, 10);
        ship.Rig.Log.Clear();
        var frame = Run(ship, rotor, lamp, Directly, 90, 10);

        Assert.InRange(frame, 0, 1);
    }

    /// <summary>
    /// A turn to a negative angle, which the game turns into the same angle a whole turn on, arrives
    /// like any other - from either side.
    /// </summary>
    [Theory]
    [InlineData(0f, -90.0, 270f)]
    [InlineData(180f, -90.0, 270f)]
    [InlineData(300f, -90.0, 270f)]
    public void TurningToANegativeAngleArrives(float from, double degrees, float lands)
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var lamp = ship.Light(2, "Lamp");
        rotor.Stand(from);

        var frame = Run(ship, rotor, lamp, Directly, degrees, 10);

        Assert.True(frame < 5000, "the turn never arrived");
        Assert.InRange(rotor.Degrees, lands - 0.1f, lands + 0.1f);
    }

    [Fact]
    public void TheTurnSaysHowLongItShouldTake()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var entity = ship.Entity(rotor.Id);

        var ticks = entity.Expected(RotorExtension.RotateTo, [Literal.Of(Directly), Literal.Of(90L), Literal.Of(10L)]);

        Assert.Equal(90, ticks);
    }

    /// <summary>
    /// A turn needs every one of its values, and a speed of nought is a turn that can never arrive.
    /// </summary>
    [Fact]
    public void ATurnRefusesWhatItCannotDo()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var action = ship.Entity(rotor.Id).Kind.FindAction(RotorExtension.RotateTo, false)!;
        var fields = new List<Field>();
        action.Fields(fields);

        var speed = fields[2];
        Assert.Equal("speed needs a value.", speed.Refuses("speed", ""));
        Assert.Equal("speed has to be above 0 and at most 30.", speed.Refuses("speed", "0"));
        Assert.Equal("speed has to be above 0 and at most 30.", speed.Refuses("speed", "31"));
        Assert.Equal("", speed.Refuses("speed", "0.5"));
        Assert.Equal("(0 - 30)", speed.Takes);

        Assert.Equal("angle needs a value.", fields[1].Refuses("angle", ""));
        Assert.Equal("direction needs a value.", fields[0].Refuses("direction", ""));
    }

    /// <summary>
    /// A step that was never given a value it needs is at fault, on its own line, rather than waiting
    /// for something that can never happen.
    /// </summary>
    [Fact]
    public void AStepMissingAValueIsAFault()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);

        var turn = new Act(RotorExtension.RotateTo);
        turn.Targets.Add(automation.Declare(ship.World, ship.Entity(rotor.Id)).Name);
        turn.Values.Add(Literal.Of(Directly));
        turn.Values.Add(Literal.Of(90L));
        sequence.Steps.Add(turn);
        sequence.Steps.Add(new Await());

        var runner = new AutomationRunner(automation, ship.World);
        runner.Tick(1);

        var fault = SequenceSummary.Fault(ship.World, runner.Bindings, sequence, runner.Get(sequence));

        Assert.Equal(SequenceProblem.Unfilled, fault.Problem);
        Assert.Equal(0, fault.Step);
        Assert.True(fault.Parks);

        // And with the speed it asks for, that is no longer what is wrong with it.
        turn.Values.Add(Literal.Of(1L));
        Assert.NotEqual(SequenceProblem.Unfilled,
            SequenceSummary.Fault(ship.World, runner.Bindings, sequence, runner.Get(sequence)).Problem);
    }

    /// <summary>
    /// A run that ends while the rotor is turning stops the rotor, whichever way it ended: a stop
    /// that leaves a machine moving is not a stop.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StoppingARunStopsTheRotor(bool byHand)
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);

        var turn = new Act(RotorExtension.RotateTo);
        turn.Targets.Add(automation.Declare(ship.World, ship.Entity(rotor.Id)).Name);
        turn.Values.Add(Literal.Of(Clockwise));
        turn.Values.Add(Literal.Of(350L));
        turn.Values.Add(Literal.Of(1L));
        sequence.Steps.Add(turn);
        sequence.Steps.Add(new Await());

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        ship.Tick();
        runner.Tick(++frame);
        runner.Call(sequence);

        for (; frame < 100; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        Assert.NotEqual(0f, rotor.Speed);
        var reached = rotor.Degrees;

        if (byHand)
            runner.Get(sequence)!.Stop();
        else
            sequence.Enabled = false;

        for (var end = frame + 2 * Rota.Ten; frame < end; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        Assert.Equal(0f, rotor.Speed);
        Assert.InRange(rotor.Degrees, reached, reached + 1f);
        Assert.False(runner.Get(sequence)!.IsRunning);
    }

    /// <summary>
    /// A rotor something is holding says so, and the sequence waiting on it is at fault rather than
    /// waiting for ever.
    /// </summary>
    [Fact]
    public void ARotorThatIsHeldSaysSo()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var automation = new Automation();
        var sequence = Turning(ship, automation, rotor, 350, 5);
        var runner = new AutomationRunner(automation, ship.World);

        var frame = 0;
        ship.Tick();
        runner.Tick(++frame);
        runner.Call(sequence);

        for (; frame < 30; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        Assert.NotEqual(SequenceProblem.Stuck, Faulted(ship, runner, sequence).Problem);

        // Something leans on it: the speed and the torque say nothing, and the angle stops.
        rotor.Held = true;
        for (var end = frame + 120; frame < end; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        // Waited through: the sequence says what is wrong and holds, so a cleared obstruction lets it
        // finish by itself.
        var fault = Faulted(ship, runner, sequence);
        Assert.Equal(SequenceProblem.Stuck, fault.Problem);
        Assert.Equal("cannot turn - something is holding it", fault.Because);
        Assert.True(runner.Get(sequence)!.IsRunning);
    }

    /// <summary>
    /// The watch on a rotor is what says it is held, and a turn starts it afresh: a rotor standing
    /// where the last turn left it has not been held, it has been still.
    /// </summary>
    [Fact]
    public void AFreshTurnIsNotJudgedByTheLastOne()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");

        var block = (IMyMotorStator)rotor.Block;
        var watch = new RotorTurn();
        Assert.True(watch.Turning(block), "a rotor just looked at is already called held");

        for (var frame = 0; frame < 120; frame++)
            ship.Tick();

        Assert.False(watch.Turning(block), "a rotor that has not moved for two seconds reads as turning");

        watch.Began(block);
        Assert.True(watch.Turning(block), "a turn just begun is called held before it can move");
    }

    /// <summary>What is plainly wrong with a rotor is read off it, and needs no watching.</summary>
    [Theory]
    [InlineData("power", "cannot turn - it is switched off or has no power")]
    [InlineData("head", "cannot turn - it has no head")]
    [InlineData("lock", "cannot turn - it is locked")]
    [InlineData("torque", "cannot turn - its torque is nought")]
    public void ARotorThatCannotTurnSaysWhy(string what, string said)
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var automation = new Automation();
        var sequence = Turning(ship, automation, rotor, 350, 5);
        var runner = new AutomationRunner(automation, ship.World);

        ship.Tick();
        runner.Tick(1);
        runner.Call(sequence);

        if (what == "power")
            rotor.Enabled = false;
        else if (what == "head")
            rotor.Attached = false;
        else if (what == "lock")
            rotor.Locked = true;
        else
            rotor.Torque = 0f;

        for (var frame = 2; frame < 20; frame++)
        {
            ship.Tick();
            runner.Tick(frame);
        }

        var fault = Faulted(ship, runner, sequence);
        Assert.Equal(SequenceProblem.Stuck, fault.Problem);
        Assert.Equal(said, fault.Because);
    }

    static SequenceFault Faulted(Rigged ship, AutomationRunner runner, Sequence sequence)
    {
        return SequenceSummary.Fault(ship.World, runner.Bindings, sequence, runner.Get(sequence));
    }

    /// <summary>A sequence that turns the rotor and waits for it.</summary>
    static Sequence Turning(Rigged ship, Automation automation, Rotor rotor, long degrees, long rpm)
    {
        var sequence = automation.AddSequence((Trigger)null!);
        var turn = new Act(RotorExtension.RotateTo);
        turn.Targets.Add(automation.Declare(ship.World, ship.Entity(rotor.Id)).Name);
        turn.Values.Add(Literal.Of(Clockwise));
        turn.Values.Add(Literal.Of(degrees));
        turn.Values.Add(Literal.Of(rpm));
        sequence.Steps.Add(turn);
        sequence.Steps.Add(new Await());
        return sequence;
    }

    [Fact]
    public void TheTurnReadsAsASentence()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var action = ship.Entity(rotor.Id).Kind.FindAction(RotorExtension.RotateTo, false)!;
        var fields = new List<Field>();
        action.Fields(fields);

        Assert.Equal("directly to 90° at 10 rpm",
            SentenceText.Given(new ValuesSlot([Literal.Of(Directly), Literal.Of(90L), Literal.Of(10L)], fields, 0)));
        Assert.Equal("clockwise to 45° at 2 rpm",
            SentenceText.Given(new ValuesSlot([Literal.Of(Clockwise), Literal.Of(45L), Literal.Of(2L)], fields, 0)));
    }

    static Literal Written(double number)
    {
        return number == Math.Floor(number) ? Literal.Of((long)number) : Literal.Of(number);
    }

    /// <summary>A number read out of a program the way somebody typed it, whole or not, turns the rotor.</summary>
    [Theory]
    [InlineData(-90.0, 1.0)]
    [InlineData(45.5, 2.5)]
    public void ATurnAsTypedArrives(double degrees, double rpm)
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var lamp = ship.Light(2, "Lamp");

        var frame = Run(ship, rotor, lamp, Directly, degrees, rpm);

        Assert.True(frame < 5000, "the turn never arrived");
    }

    /// <summary>Turns the rotor, waits for it, then switches the lamp on; answers the tick the lamp came on.</summary>
    static int Run(Rigged ship, Rotor rotor, Plain lamp, long way, double degrees, double rpm)
    {
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);

        var turn = new Act(RotorExtension.RotateTo);
        turn.Targets.Add(automation.Declare(ship.World, ship.Entity(rotor.Id)).Name);
        // Whole where they are whole, as a program reads them: "90" is a whole number and "90.5" is not.
        turn.Values.Add(Literal.Of(way));
        turn.Values.Add(Written(degrees));
        turn.Values.Add(Written(rpm));
        sequence.Steps.Add(turn);
        sequence.Steps.Add(new Await());

        var on = new Act(BlockKinds.SwitchOn);
        on.Targets.Add(automation.Declare(ship.World, ship.Entity(lamp.Id)).Name);
        sequence.Steps.Add(on);
        sequence.Steps.Add(new Await());

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        ship.Tick();
        runner.Tick(++frame);
        runner.Call(sequence);
        var started = frame;

        while (!ship.Rig.Log.Contains("Lamp:on") && frame < 20000)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        return frame - started;
    }
}
