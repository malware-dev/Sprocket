using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Tests;

/// <summary>
/// A rotor, turned to an angle by the game's own limit and waited for until it gets there, and the
/// members of its terminal said in Sprocket's words.
/// </summary>
public class RotorTests
{
    const long Directly = 0;
    const long Clockwise = 1;

    /// <summary>
    /// What matters about a rotor is said in Sprocket's words, and is still the rotor's own.
    /// </summary>
    /// <remarks>
    /// A rename changes what a list and a sentence call a member and nothing else: the id is the
    /// block's, so a program names the same member whatever Sprocket calls it, and the terminal view
    /// goes on showing the block's own words.
    /// </remarks>
    [Fact]
    public void WhatMattersAboutARotorIsSaidInSprocketsWords()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var kind = ship.Entity(rotor.Id).Kind;

        Assert.Equal("reverse", kind.FindAction("Reverse", Form.Action)!.Text);
        Assert.Equal("lock", kind.FindAction("RotorLock_On", Form.Action)!.Text);
        Assert.Equal("Velocity", kind.FindAction("Velocity", Form.Value)!.Named);
        Assert.Equal("Braking torque", kind.FindAction("BrakingTorque", Form.Value)!.Named);

        // One member each, under the block's id, offered in the organized view and shown in the
        // terminal one as the block words it.
        var lockOn = kind.FindAction("RotorLock_On", Form.Action)!;
        Assert.False(lockOn.Hidden);
        Assert.True(lockOn.Unaltered);
        Assert.Equal("Rotor lock On", lockOn.Declared);
        Assert.Null(kind.FindAction("lock", Form.Native));
        Assert.Null(kind.FindAction("speed", Form.Value));

        // Lock and unlock are the lock's own pair, offered because the rotor keeps the value they
        // were made out of to the terminal - not because they have been renamed.
        Assert.True(kind.FindAction("RotorLock", Form.Value)!.Hidden);
        Assert.False(kind.FindAction("RotorLock_Off", Form.Action)!.Hidden);

        // Rotating to an angle is Sprocket's own, performed its own way, so the game's is left out.
        Assert.NotNull(kind.FindAction(Blocks.Rotor.RotateTo, Form.Native));
        Assert.True(kind.FindAction("RotateToAngle", Form.Action)!.Hidden);

        // Displacement is the rotor's own word for it, which Sprocket keeps.
        Assert.Equal("Displacement", kind.FindAction("Displacement", Form.Value)!.Named);
    }

    /// <summary>
    /// Adding a head is one action asking for a size, standing in for the game's three, and is the game's own when it runs.
    /// </summary>
    [Fact]
    public void AHeadIsAddedInTheSizeAsked()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var kind = ship.Entity(rotor.Id).Kind;

        foreach (var id in new[]
                 {
                     "AddRotorTopPart", "AddSmallRotorTopPart", "AddMediumRotorTopPart"
                 })
        {
            Assert.True(kind.FindAction(id, Form.Action)!.Hidden, id + " is offered as a step");
        }

        var add = kind.FindAction(Blocks.Rotor.AddHead, Form.Native)!;
        Assert.False(add.Hidden);
        var fields = new List<Field>();
        add.Fields(fields);
        Assert.Equal("size", Assert.Single(fields).Name);

        rotor.Attached = false;
        ship.Entity(rotor.Id).Invoke(Blocks.Rotor.AddHead, new[] { Literal.Of(2L) });
        Assert.Contains("Rotor:AddSmallRotorTopPart", ship.Rig.Log);
    }

    /// <summary>
    /// A hinge is asked for an angle within a quarter turn either way, and a speed, and never a way round.
    /// </summary>
    /// <remarks>
    /// It goes the short way whatever it is told, so a direction would be a question with one answer.
    /// Its angle is what its definition allows, which is what the dial shows as a half circle. Its lock
    /// is its own, beside the rotor's, and is the one said as lock and unlock.
    /// </remarks>
    [Fact]
    public void AHingeIsTurnedWithoutAWayRound()
    {
        var ship = new Rigged();
        var hinge = ship.Hinge(1, "Hinge");
        var kind = ship.Entity(hinge.Id).Kind;

        var asked = new List<Field>();
        kind.FindAction(Blocks.Rotor.RotateTo, Form.Native)!.Fields(asked);

        Assert.Equal(2, asked.Count);
        Assert.Equal("-90", asked[0].Least);
        Assert.Equal("90", asked[0].Most);
        Assert.Equal(Editor.Angle, asked[0].Edits);

        Assert.Equal("Lock", kind.FindAction("HingeLock_On", Form.Action)!.Named);
        Assert.Equal("Unlock", kind.FindAction("HingeLock_Off", Form.Action)!.Named);
        Assert.True(kind.FindAction("HingeLock", Form.Value)!.Hidden);
    }

    [Fact]
    public void AHingeTurnedToAnAngleArrives()
    {
        var ship = new Rigged();
        var hinge = ship.Hinge(1, "Hinge");
        var lamp = ship.Light(2, "Lamp");

        var frames = Turned(ship, hinge, lamp, Literal.Of(45L), Literal.Of(5L));

        Assert.InRange(hinge.Degrees, 44.9f, 45.1f);

        // Forty-five degrees at half a degree a tick.
        Assert.InRange(frames, 90, 100);
    }

    /// <summary>Sprocket's own words are spelled for a list the way the game spells its own.</summary>
    [Fact]
    public void AListNamesSprocketsOwnMembersTheWayTheGameNamesIts()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var kind = ship.Entity(rotor.Id).Kind;

        Assert.Equal("Rotate to", kind.FindAction(Blocks.Rotor.RotateTo, Form.Native)!.Named);
        Assert.Equal("Unlock", kind.FindAction("RotorLock_Off", Form.Action)!.Named);
        Assert.Equal("Braking torque", kind.FindAction("BrakingTorque", Form.Value)!.Named);

        // And the sentence keeps the word it needs, which is the one the capability wrote.
        Assert.Equal("rotate", kind.FindAction(Blocks.Rotor.RotateTo, Form.Native)!.Text);
    }

    /// <summary>A renamed member is the game's own, and does what the game's does.</summary>
    [Fact]
    public void ARenamedMemberDoesWhatTheGamesDoes()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var entity = ship.Entity(rotor.Id);

        ship.World.Invoke(entity, "Reverse", null, Form.Action);
        ship.World.Invoke(entity, "Velocity", [Literal.Of(5.0)], Form.Value);

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

        var ticks = entity.Expected(Blocks.Rotor.RotateTo, [Literal.Of(Directly), Literal.Of(90.0), Literal.Of(10.0)]);

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
        var action = ship.Entity(rotor.Id).Kind.FindAction(Blocks.Rotor.RotateTo, Form.Native)!;
        var fields = new List<Field>();
        action.Fields(fields);

        var speed = fields[2];
        Assert.Equal("speed must be given.", speed.Refuses(new Texts(), "speed", ""));
        Assert.Equal("speed must be above 0 and at most 30.", speed.Refuses(new Texts(), "speed", "0"));
        Assert.Equal("speed must be above 0 and at most 30.", speed.Refuses(new Texts(), "speed", "31"));
        Assert.Equal("", speed.Refuses(new Texts(), "speed", "0.5"));
        Assert.Equal("(0 - 30)", speed.Takes(new Texts()));

        Assert.Equal("angle must be given.", fields[1].Refuses(new Texts(), "angle", ""));
        Assert.Equal("direction must be given.", fields[0].Refuses(new Texts(), "direction", ""));
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

        var turn = new Act(Blocks.Rotor.RotateTo);
        turn.Targets.Add(automation.Declare(ship.World, ship.Entity(rotor.Id)).Name);
        turn.Arguments.Add(Literal.Of(Directly));
        turn.Arguments.Add(Literal.Of(90L));
        sequence.Steps.Add(turn);
        sequence.Steps.Add(new Await());

        var runner = new AutomationRunner(automation, ship.World);
        runner.Tick(1);

        var fault = SequenceSummary.Checked(ship.World, runner.Bindings, sequence, sequence.System.First);

        Assert.Equal(SequenceProblem.Unfilled, fault.Problem);
        Assert.Equal(0, fault.Step);
        Assert.True(fault.Parks);

        // And with the speed it asks for, that is no longer what is wrong with it.
        turn.Arguments.Add(Literal.Of(1L));
        Assert.NotEqual(SequenceProblem.Unfilled,
            SequenceSummary.Checked(ship.World, runner.Bindings, sequence, sequence.System.First).Problem);
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

        var turn = new Act(Blocks.Rotor.RotateTo);
        turn.Targets.Add(automation.Declare(ship.World, ship.Entity(rotor.Id)).Name);
        turn.Arguments.Add(Literal.Of(Clockwise));
        turn.Arguments.Add(Literal.Of(350L));
        turn.Arguments.Add(Literal.Of(1L));
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
            sequence.System.Enabled = false;

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

        Assert.NotEqual(SequenceProblem.Stuck, Faulted(runner, sequence).Problem);

        // Something leans on it: the speed and the torque say nothing, and the angle stops. A turn this long is looked at
        // every hundred ticks, and the run calls it held at its second look after the angle stopped.
        rotor.Held = true;
        for (var end = frame + 200; frame < end; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        // Waited through: the sequence says what is wrong and holds, so a cleared obstruction lets it
        // finish by itself.
        var fault = Faulted(runner, sequence);
        Assert.Equal(SequenceProblem.Stuck, fault.Problem);
        Assert.Equal("cannot turn because something is holding it", TroubleText.Of(new Texts(), fault.Because));
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
        var seen = new Recall { Clock = () => MyAPIGateway.Session.GameplayFrameCounter };
        Assert.True(Blocks.RotorTurn.Turning(block, seen), "a rotor just looked at is already called held");

        for (var frame = 0; frame < 120; frame++)
            ship.Tick();

        Assert.False(Blocks.RotorTurn.Turning(block, seen), "a rotor that has not moved for two seconds reads as turning");

        Blocks.RotorTurn.Began(block, seen);
        Assert.True(Blocks.RotorTurn.Turning(block, seen), "a turn just begun is called held before it can move");
    }

    /// <summary>What is plainly wrong with a rotor is read off it, and needs no watching.</summary>
    [Theory]
    [InlineData("power", "Rotor")]
    [InlineData("head", "cannot turn because it has no head")]
    [InlineData("lock", "cannot turn because it is locked")]
    [InlineData("torque", "cannot turn because its torque is zero")]
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

        // Read at the run's first look while it waits, which for a turn this long is a hundred ticks after it began.
        for (var frame = 2; frame < 110; frame++)
        {
            ship.Tick();
            runner.Tick(frame);
        }

        var fault = Faulted(runner, sequence);
        if (what == "power")
        {
            Assert.Equal(SequenceProblem.Stalled, fault.Problem);
            Assert.Equal(said, fault.Which);
            return;
        }

        Assert.Equal(SequenceProblem.Stuck, fault.Problem);
        Assert.Equal(said, TroubleText.Of(new Texts(), fault.Because));
    }

    static SequenceFault Faulted(AutomationRunner runner, Sequence sequence)
    {
        runner.Look();
        return SequenceSummary.Fault(runner.Get(sequence));
    }

    /// <summary>A sequence that turns the rotor and waits for it.</summary>
    static Sequence Turning(Rigged ship, Automation automation, Rotor rotor, long degrees, long rpm)
    {
        var sequence = automation.AddSequence((Trigger)null!);
        var turn = new Act(Blocks.Rotor.RotateTo);
        turn.Targets.Add(automation.Declare(ship.World, ship.Entity(rotor.Id)).Name);
        turn.Arguments.Add(Literal.Of(Clockwise));
        turn.Arguments.Add(Literal.Of(degrees));
        turn.Arguments.Add(Literal.Of(rpm));
        sequence.Steps.Add(turn);
        sequence.Steps.Add(new Await());
        return sequence;
    }

    /// <summary>A turn missing an option it needs names that option, on the sequence and as the reason its run stopped.</summary>
    [Fact]
    public void AMissingOptionIsNamed()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var automation = new Automation();
        var sequence = Turning(ship, automation, rotor, 90, 5);
        var turn = (Act)sequence.Steps[0];
        turn.Arguments.RemoveAt(2);
        turn.Arguments.RemoveAt(1);
        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);

        var fault = Faulted(runner, sequence);
        Assert.Equal(SequenceProblem.Unfilled, fault.Problem);
        Assert.Equal("This step is missing its angle.", SequenceText.Problem(new Texts(), fault, sequence));

        // Switched off for it, the reason it stopped names the option as well.
        for (var frame = 2; frame < 600; frame++)
        {
            ship.Tick();
            runner.Tick(frame);
        }

        var halt = sequence.System.Halt!;
        Assert.Equal("angle", halt.Block);
        Assert.EndsWith("its angle was missing.", SequenceText.Stopped(new Texts(), halt, sequence.System, false));
    }

    /// <summary>A turn given an option it cannot take names that option.</summary>
    [Fact]
    public void AWrongOptionIsNamed()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var automation = new Automation();
        var sequence = Turning(ship, automation, rotor, 90, 5);
        ((Act)sequence.Steps[0]).Arguments[1] = Literal.Of("far");
        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);

        var fault = Faulted(runner, sequence);
        Assert.Equal(SequenceProblem.Mistyped, fault.Problem);
        Assert.Equal("This step's angle doesn't fit its action.", SequenceText.Problem(new Texts(), fault, sequence));
    }

    [Fact]
    public void TheTurnReadsAsASentence()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var action = ship.Entity(rotor.Id).Kind.FindAction(Blocks.Rotor.RotateTo, Form.Native)!;
        var fields = new List<Field>();
        action.Fields(fields);

        Assert.Equal("directly to 90° at 10 rpm",
            SentenceText.Given(new ArgumentsSlot([Literal.Of(Directly), Literal.Of(90L), Literal.Of(10L)], fields, null)));
        Assert.Equal("clockwise to 45° at 2 rpm",
            SentenceText.Given(new ArgumentsSlot([Literal.Of(Clockwise), Literal.Of(45L), Literal.Of(2L)], fields, null)));
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
    static int Run(Rigged ship, Rotor rotor, Lamp lamp, long way, double degrees, double rpm)
    {
        // Whole where they are whole, as a program reads them: "90" is a whole number and "90.5" is not.
        return Turned(ship, rotor, lamp, Literal.Of(way), Written(degrees), Written(rpm));
    }

    /// <summary>Turns the block with what a step gave it, and counts the frames until the next step runs.</summary>
    static int Turned(Rigged ship, Rotor rotor, Lamp lamp, params Literal[] given)
    {
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);

        var turn = new Act(Blocks.Rotor.RotateTo);
        turn.Targets.Add(automation.Declare(ship.World, ship.Entity(rotor.Id)).Name);
        foreach (var value in given)
            turn.Arguments.Add(value);
        sequence.Steps.Add(turn);
        sequence.Steps.Add(new Await());

        var on = Given.Power(true);
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
