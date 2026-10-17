using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Sim.Machine;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Tests;

/// <summary>
/// A step's values are put into the kinds its action takes once, when the program is bound.
/// </summary>
/// <remarks>
/// A running step hands its blocks what they take and converts nothing. A value of a kind the action
/// does not take is the step's fault, found before it runs, and the block is left alone.
/// </remarks>
public class CompileTests
{
    const long Directly = 0;

    [Fact]
    public void AWholeNumberIsARealWhereARealIsWanted()
    {
        Literal became;
        Assert.True(Literal.Of(90L).Becomes(ValueKind.Real, out became));
        Assert.Equal(ValueKind.Real, became.Kind);
        Assert.Equal(90.0, became.Real);
    }

    [Fact]
    public void NothingElseBecomesAnything()
    {
        Assert.False(Literal.Of(2.5).Becomes(ValueKind.Integer, out _));
        Assert.False(Literal.Of("ninety").Becomes(ValueKind.Real, out _));
        Assert.False(Literal.Of(1L).Becomes(ValueKind.Flag, out _));
        Assert.False(Literal.Of(42L).Becomes(ValueKind.Text, out _));
    }

    [Fact]
    public void AStepGivenTheWrongKindIsAFault()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        sequence.Steps.Add(Turn(ship, automation, rotor, Literal.Of("ninety"), Literal.Of(10L)));
        sequence.Steps.Add(new Await());

        var runner = new AutomationRunner(automation, ship.World);
        runner.Tick(1);

        var fault = SequenceSummary.Checked(ship.World, runner.Bindings, sequence, sequence.System.First);

        Assert.Equal(SequenceProblem.Mistyped, fault.Problem);
        Assert.Equal(0, fault.Step);
        Assert.True(fault.Parks);
    }

    [Fact]
    public void AMistypedStepLeavesTheBlockAlone()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        sequence.Steps.Add(Turn(ship, automation, rotor, Literal.Of("ninety"), Literal.Of(10L)));

        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);
        runner.Call(sequence);

        for (var frame = 2; frame < 200; frame++)
        {
            ship.Tick();
            runner.Tick(frame);
        }

        Assert.Equal(0f, rotor.Speed);
        Assert.InRange(rotor.Degrees, -0.1f, 0.1f);
    }

    /// <summary>
    /// A turn waited on for ninety ticks is worked out once, however many times the run asks whether
    /// it has arrived.
    /// </summary>
    [Fact]
    public void AStepIsWorkedOutOnceHoweverLongItRuns()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        sequence.Steps.Add(Turn(ship, automation, rotor, Literal.Of(90L), Literal.Of(10L)));
        sequence.Steps.Add(new Await());

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        ship.Tick();
        runner.Tick(++frame);
        runner.Call(sequence);

        var running = runner.Get(sequence)!;
        while (running.IsRunning && frame < 2000)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.InRange(rotor.Degrees, 89.9f, 90.1f);
        Assert.InRange(frame, 80, 200);
        Assert.Equal(1, running.Compilations);
    }

    /// <summary>A value changed in the editor is what the step hands its block the next time it runs.</summary>
    [Fact]
    public void AnEditIsWorkedOutAgain()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(1, "Rotor");
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var turn = Turn(ship, automation, rotor, Literal.Of(90L), Literal.Of(10L));
        sequence.Steps.Add(turn);
        sequence.Steps.Add(new Await());

        var runner = new AutomationRunner(automation, ship.World);
        var editor = new SprocketEditor(ship.World, automation, runner, new Texts());
        var frame = 0;

        frame = RunOut(ship, runner, sequence, frame);
        Assert.InRange(rotor.Degrees, 89.9f, 90.1f);

        editor.Do(sequence, new ChangeStep(0, turn, turn.Targets, turn.ActionId, turn.Form, turn.Provider,
            [Literal.Of(Directly), Literal.Of(45.0), Literal.Of(10L)], true));

        RunOut(ship, runner, sequence, frame);
        Assert.InRange(rotor.Degrees, 44.9f, 45.1f);
    }

    /// <summary>
    /// A game action declaring text where it takes numbers is taken at what it takes, so a whole number
    /// written for a piston's position arrives as a real one.
    /// </summary>
    [Fact]
    public void AnActionsWrongDeclarationIsCorrected()
    {
        var ship = new Rigged();
        var block = ship.Rig.Panel(1, "Mover").Offering("SetAndMove", "Set and move",
            Primitive.String, Primitive.String);
        var action = ship.Entity(block.Id).Kind.FindAction("SetAndMove", ReachKind.Action)!;

        var converted = new List<Literal>();
        Assert.True(action.Converts([Literal.Of(4L), Literal.Of(0.5)], converted));
        Assert.All(converted, value => Assert.Equal(ValueKind.Real, value.Kind));

        var fields = new List<Field>();
        action.Fields(fields);
        Assert.All(fields, field => Assert.Equal(FieldInput.Decimal, field.Entry));
    }

    /// <summary>What an action declares stands where nothing corrects it.</summary>
    [Fact]
    public void AnUncorrectedDeclarationStands()
    {
        var ship = new Rigged();
        var block = ship.Rig.Panel(1, "Brain").Offering("Run", "Run", Primitive.String);
        var action = ship.Entity(block.Id).Kind.FindAction("Run", ReachKind.Action)!;

        Assert.True(action.Takes([Literal.Of("doors open")]));
        Assert.False(action.Takes([Literal.Of(5L)]));
    }

    /// <summary>A value whose kind nothing says goes as it was written.</summary>
    [Fact]
    public void AnUndeclaredValueGoesAsWritten()
    {
        var ship = new Rigged();
        var block = ship.Rig.Panel(1, "Oddment").Offering("Twiddle", "Twiddle", 1);
        var action = ship.Entity(block.Id).Kind.FindAction("Twiddle", ReachKind.Action)!;

        var converted = new List<Literal>();
        Assert.True(action.Converts([Literal.Of(5L)], converted));
        Assert.Equal(ValueKind.Integer, Assert.Single(converted).Kind);
    }

    [Fact]
    public void AWholeNumberFitsOnlyATypeLargeEnoughForIt()
    {
        Assert.True(Literal.Of(255L).Fits(Primitive.Byte, out _));
        Assert.False(Literal.Of(256L).Fits(Primitive.Byte, out _));
        Assert.False(Literal.Of(-1L).Fits(Primitive.UInt32, out _));
        Assert.True(Literal.Of(-128L).Fits(Primitive.SByte, out _));
        Assert.False(Literal.Of(2.5).Fits(Primitive.Int32, out _));
        Assert.False(Literal.Of("ab").Fits(Primitive.Char, out _));
        Assert.True(Literal.Of("a").Fits(Primitive.Char, out _));

        Literal became;
        Assert.True(Literal.Of(3L).Fits(Primitive.Single, out became));
        Assert.Equal(ValueKind.Real, became.Kind);
    }

    /// <summary>
    /// A block handed values by a running step gets each as the very type it takes, built once when
    /// the program was bound.
    /// </summary>
    [Fact]
    public void AStepHandsEachValueAsTheTypeTheActionTakes()
    {
        var ship = new Rigged();
        var remote = ship.Rig.Panel(1, "Remote").Offering("Steer", "Steer", Primitive.Byte, Primitive.Single,
            Primitive.Int32);
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);

        var steer = new Act("Steer") { Form = ReachKind.Action };
        steer.Targets.Add(automation.Declare(ship.World, ship.Entity(remote.Id)).Name);
        steer.Arguments.Add(Literal.Of(3L));
        steer.Arguments.Add(Literal.Of(2L));
        steer.Arguments.Add(Literal.Of(7L));
        sequence.Steps.Add(steer);

        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);
        runner.Call(sequence);

        var handed = remote.Handed;
        Assert.Equal((byte)3, Assert.IsType<byte>(handed[0]));
        Assert.Equal(2f, Assert.IsType<float>(handed[1]));
        Assert.Equal(7, Assert.IsType<int>(handed[2]));
    }

    /// <summary>A value too large for the type it is taken as is the step's fault, and nothing is handed.</summary>
    [Fact]
    public void AValueTooLargeForItsTypeIsAFault()
    {
        var ship = new Rigged();
        var remote = ship.Rig.Panel(1, "Remote").Offering("Steer", "Steer", Primitive.Byte);
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);

        var steer = new Act("Steer") { Form = ReachKind.Action };
        steer.Targets.Add(automation.Declare(ship.World, ship.Entity(remote.Id)).Name);
        steer.Arguments.Add(Literal.Of(300L));
        sequence.Steps.Add(steer);

        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);

        Assert.Equal(SequenceProblem.Mistyped,
            SequenceSummary.Checked(ship.World, runner.Bindings, sequence, sequence.System.First).Problem);

        runner.Call(sequence);
        Assert.Null(remote.Handed);
    }

    /// <summary>A box for a byte says what a byte holds.</summary>
    [Fact]
    public void ABoxForASmallTypeIsBounded()
    {
        var ship = new Rigged();
        var remote = ship.Rig.Panel(1, "Remote").Offering("Steer", "Steer", Primitive.Byte);
        var fields = new List<Field>();
        ship.Entity(remote.Id).Kind.FindAction("Steer", ReachKind.Action)!.Fields(fields);

        var field = Assert.Single(fields);
        Assert.Equal(FieldInput.Whole, field.Entry);
        Assert.Equal("0", field.Least);
        Assert.Equal("255", field.Most);
    }

    [Fact]
    public void AWholeNumberBoxRefusesAFraction()
    {
        var field = Field.Between("count", "0", "10");
        field.Entry = FieldInput.Whole;

        Assert.Equal("count must be a whole number.", field.Refuses(new Texts(), "count", "2.5"));
        Assert.Equal("", field.Refuses(new Texts(), "count", "2"));
    }

    [Fact]
    public void ADecimalBoxShowsAPlaceAfterThePoint()
    {
        var field = Field.Between("speed", "0", "5");
        field.Entry = FieldInput.Decimal;

        Assert.Equal("1.0", field.Shown("1"));
        Assert.Equal("0.25", field.Shown("0.25"));
        Assert.Equal("", field.Refuses(new Texts(), "speed", "0.25"));
    }

    static Act Turn(Rigged ship, Automation automation, Rotor rotor, Literal angle, Literal speed)
    {
        var turn = new Act(Blocks.Rotor.RotateTo);
        turn.Targets.Add(automation.Declare(ship.World, ship.Entity(rotor.Id)).Name);
        turn.Arguments.Add(Literal.Of(Directly));
        turn.Arguments.Add(angle);
        turn.Arguments.Add(speed);
        return turn;
    }

    static int RunOut(Rigged ship, AutomationRunner runner, Sequence sequence, int frame)
    {
        ship.Tick();
        runner.Tick(++frame);
        runner.Call(sequence);

        var running = runner.Get(sequence)!;
        var until = frame + 2000;
        while (running.IsRunning && frame < until)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        return frame;
    }
}
