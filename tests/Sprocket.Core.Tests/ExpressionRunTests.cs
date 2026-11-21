using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>An expression is worked out as its step is reached, from the blocks as they are then, and a step it cannot be worked out for ends the run and says why.</summary>
public class ExpressionRunTests
{
    sealed class Plant
    {
        public readonly Rigged Ship = new();
        public readonly Lamp First;
        public readonly Lamp Second;
        public readonly Lamp Third;
        public readonly Lamp Out;
        public readonly Automation Program = new();
        public readonly Sequence Cycle;
        public AutomationRunner Runner = null!;

        int _frame;

        public Plant()
        {
            First = Lamp(1, "First", 3);
            Second = Lamp(2, "Second", 5);
            Third = Lamp(3, "Third", 10);
            Out = Lamp(4, "Out", 1);
            Cycle = Program.AddSequence((Trigger)null!);
        }

        Lamp Lamp(long id, string name, double radius)
        {
            var lamp = Ship.Light(id, name);
            lamp.Carrying("Radius", "Radius", ValueKind.Real, "1", "50000");
            lamp.Carrying("Note", "Note", ValueKind.Text);
            lamp.Carrying("OnOff", "On", ValueKind.Flag);
            lamp.Set("Radius", Literal.Of(radius));
            return lamp;
        }

        public SequenceRunner State => Runner.Get(Cycle, Cycle.System.First);

        public string Name(Lamp lamp) => Program.Declare(Ship.World, Ship.Entity(lamp.Id)).Name;

        public BlockReading Reading(Combination combine, params Lamp[] lamps)
        {
            var reading = new BlockReading(MemberKey.Bare("Radius"), combine, new List<Literal>(), new List<string>());
            foreach (var lamp in lamps)
                reading.Targets.Add(Name(lamp));

            return reading;
        }

        public static Expression Number(long value) => new Operand(Literal.Of(value));

        /// <summary>A step setting the radius of <see cref="Out" /> to <paramref name="value" />.</summary>
        public void SetRadius(Expression value)
        {
            var act = new Act("Radius") { Form = ReachKind.Value };
            act.Targets.Add(Name(Out));
            act.Arguments.Add(Literal.Worked(value));
            Cycle.Steps.Add(act);
        }

        public void Start(int ticks = 6)
        {
            Program.Edited();
            Runner = new AutomationRunner(Program, Ship.World);
            Tick(3);
            Runner.Call(Cycle, Cycle.System.First);
            Tick(ticks);
        }

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Ship.Tick();
                Runner.Tick(++_frame);
            }
        }

        public double Radius => Out.Read("Radius").Number;
    }

    /// <summary>A reading combined over blocks takes the sum, average, smallest or largest of what they read now.</summary>
    [Theory]
    [InlineData(Combination.Sum, 18.0)]
    [InlineData(Combination.Average, 6.0)]
    [InlineData(Combination.Min, 3.0)]
    [InlineData(Combination.Max, 10.0)]
    public void AReadingCombinesTheValuesOfItsBlocks(Combination combine, double expected)
    {
        var p = new Plant();
        p.SetRadius(p.Reading(combine, p.First, p.Second, p.Third));
        p.Start();

        Assert.Equal(expected, p.Radius);
    }

    /// <summary>An expression holding readings, a number and operators comes to what the language says it does.</summary>
    [Fact]
    public void ASumTimesTenIsWorkedOutFromTheBlocks()
    {
        var p = new Plant();
        p.SetRadius(new Arithmetic(Operator.Multiply, p.Reading(Combination.Sum, p.First, p.Second), Plant.Number(10)));
        p.Start();

        Assert.Equal(80.0, p.Radius);
    }

    /// <summary>The step takes the blocks as they are when it is reached, each time it is.</summary>
    [Fact]
    public void AStepReachedAgainTakesTheValuesOfThatMoment()
    {
        var p = new Plant();
        p.SetRadius(new Arithmetic(Operator.Add, p.Reading(Combination.None, p.First), Plant.Number(1)));
        p.Start();
        Assert.Equal(4.0, p.Radius);

        p.First.Set("Radius", Literal.Of(7.0));
        p.Runner.Call(p.Cycle, p.Cycle.System.First);
        p.Tick(6);

        Assert.Equal(8.0, p.Radius);
    }

    /// <summary>A sum over no blocks is 0.</summary>
    [Fact]
    public void ASumOfNoBlocksIsZero()
    {
        var p = new Plant();
        p.SetRadius(new Arithmetic(Operator.Add, p.Reading(Combination.Sum), Plant.Number(2)));
        p.Start();

        Assert.Equal(2.0, p.Radius);
    }

    /// <summary>An average, smallest or largest over no blocks has no value, so the step fails and says so.</summary>
    [Theory]
    [InlineData(Combination.Average)]
    [InlineData(Combination.Min)]
    [InlineData(Combination.Max)]
    public void ACombinationOfNoBlocksFailsTheStep(Combination combine)
    {
        var p = new Plant();
        p.SetRadius(p.Reading(combine));
        p.Start();

        Assert.Equal(1.0, p.Radius);
        Assert.Equal(SequenceProblem.NoValue, p.State.Fault.Problem);
        Assert.Equal(0, p.State.Fault.Step);
    }

    /// <summary>Dividing by zero fails the step, and says so.</summary>
    [Fact]
    public void DividingByZeroFailsTheStep()
    {
        var p = new Plant();
        p.SetRadius(new Arithmetic(Operator.Divide, Plant.Number(4), Plant.Number(0)));
        p.Start();

        Assert.Equal(1.0, p.Radius);
        Assert.Equal(SequenceProblem.DividedByZero, p.State.Fault.Problem);
    }

    /// <summary>A step that failed once runs when it is reached again and can be worked out.</summary>
    [Fact]
    public void AStepThatFailedWorksOnceItCanBeWorkedOut()
    {
        var p = new Plant();
        var divisor = p.Reading(Combination.None, p.First);
        p.First.Set("Radius", Literal.Of(0.0));
        p.SetRadius(new Arithmetic(Operator.Divide, Plant.Number(8), divisor));
        p.Start();
        Assert.Equal(SequenceProblem.DividedByZero, p.State.Fault.Problem);

        p.First.Set("Radius", Literal.Of(4.0));
        p.Cycle.System.Enabled = true;
        p.Tick(3);
        p.Runner.Call(p.Cycle, p.Cycle.System.First);
        p.Tick(6);

        Assert.Equal(2.0, p.Radius);
    }

    /// <summary>A reading of several blocks with nothing to combine them fails the step, which the program's check also says before it runs.</summary>
    [Fact]
    public void SeveralBlocksReadAsOneFailTheStep()
    {
        var p = new Plant();
        p.SetRadius(p.Reading(Combination.None, p.First, p.Second));
        p.Start(0);

        var found = SequenceSummary.Checked(p.Ship.World, p.Runner.Bindings, p.Cycle, p.Cycle.System.First);
        Assert.Equal(SequenceProblem.ManyBlocks, found.Problem);

        p.Tick(6);
        Assert.Equal(1.0, p.Radius);
        Assert.Equal(SequenceProblem.ManyBlocks, p.State.Fault.Problem);
    }

    /// <summary>Text is joined with what is on its other side written as text.</summary>
    [Fact]
    public void TextJoinsWithTheOtherSideWrittenAsText()
    {
        var p = new Plant();
        var act = new Act("Note") { Form = ReachKind.Value };
        act.Targets.Add(p.Name(p.Out));
        act.Arguments.Add(Literal.Worked(new Arithmetic(Operator.Add, new Operand(Literal.Of("Level ")), p.Reading(Combination.Average, p.First, p.Second))));
        p.Cycle.Steps.Add(act);
        p.Start();

        Assert.Equal("Level 4", p.Out.Read("Note").Text);
    }

    /// <summary>The functions and the unary minus work numbers as the language says.</summary>
    [Theory]
    [InlineData(FunctionKind.Min, 3.0)]
    [InlineData(FunctionKind.Max, 10.0)]
    public void MinAndMaxTakeTheirArguments(FunctionKind kind, double expected)
    {
        var p = new Plant();
        p.SetRadius(new Function(kind, new[] { p.Reading(Combination.None, p.First), Plant.Number(10), p.Reading(Combination.None, p.Second) }));
        p.Start();

        Assert.Equal(expected, p.Radius);
    }

    /// <summary>Round takes the nearest integer, and abs the size of a number, of a negation too.</summary>
    [Fact]
    public void RoundAndAbsAndNegationWork()
    {
        var p = new Plant();
        var sum = new Arithmetic(Operator.Add, new Operand(Literal.Of(2.5)), new Negation(Plant.Number(8)));
        p.SetRadius(new Function(FunctionKind.Abs, new Expression[] { new Function(FunctionKind.Round, new Expression[] { sum }) }));
        p.Start();

        // 2.5 - 8 is -5.5, which rounds away from nought to -6.
        Assert.Equal(6.0, p.Radius);
    }

    /// <summary>A wait lasts as long as the expression it holds comes to when the step is reached.</summary>
    [Fact]
    public void AWaitLastsAsLongAsItsExpressionComesTo()
    {
        var p = new Plant();
        var wait = new Wait(0)
        {
            Expression = new Arithmetic(Operator.Multiply, new Operand(Literal.Of(Duration.OfTicks(10))), p.Reading(Combination.None, p.First))
        };

        p.Cycle.Steps.Add(wait);
        p.SetRadius(Plant.Number(9));
        p.Start(0);

        // Thirty ticks, from a reading of 3.
        p.Tick(20);
        Assert.Equal(1.0, p.Radius);

        p.Tick(20);
        Assert.Equal(9.0, p.Radius);
    }

    /// <summary>A wait whose expression is no length of time fails the step.</summary>
    [Fact]
    public void AWaitOfSomethingThatIsNoLengthFailsTheStep()
    {
        var p = new Plant();
        p.Cycle.Steps.Add(new Wait(0) { Expression = Plant.Number(4) });
        p.Start();

        Assert.Equal(SequenceProblem.Mistyped, p.State.Fault.Problem);
        Assert.Equal(0, p.State.Fault.Step);
    }

    /// <summary>An if compares what it reads with what an expression comes to.</summary>
    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void AnIfComparesWithAnExpression(long times, bool holds)
    {
        var p = new Plant();
        var test = new Comparison { MemberId = "Radius", Provider = "", Quantifier = Quantifier.All, Relation = Relation.Above };
        test.Targets.Add(p.Name(p.Second));
        test.Value = Literal.Worked(new Arithmetic(Operator.Multiply, p.Reading(Combination.None, p.First), Plant.Number(times)));
        p.Cycle.Steps.Add(new If(test));
        p.SetRadius(Plant.Number(9));
        p.Cycle.Steps.Add(new EndIf());
        p.Start();

        // 5 is above 3 * 1, and not above 3 * 2.
        Assert.Equal(holds ? 9.0 : 1.0, p.Radius);
    }

    /// <summary>An if comparing with an expression that cannot be worked out ends the run at it.</summary>
    [Fact]
    public void AnIfComparingWithAnExpressionThatFailsEndsTheRun()
    {
        var p = new Plant();
        var test = new Comparison { MemberId = "Radius", Provider = "", Quantifier = Quantifier.All, Relation = Relation.Above };
        test.Targets.Add(p.Name(p.Second));
        test.Value = Literal.Worked(new Arithmetic(Operator.Divide, Plant.Number(1), Plant.Number(0)));
        p.Cycle.Steps.Add(new If(test));
        p.SetRadius(Plant.Number(9));
        p.Cycle.Steps.Add(new EndIf());
        p.Start();

        Assert.Equal(1.0, p.Radius);
        Assert.Equal(SequenceProblem.DividedByZero, p.State.Fault.Problem);
    }

    /// <summary>An expression is worked out once as its step is reached, and nothing is worked out on the ticks between.</summary>
    [Fact]
    public void AnExpressionIsWorkedOutOnlyWhenItsStepIsReached()
    {
        var p = new Plant();
        p.SetRadius(new Arithmetic(Operator.Add, p.Reading(Combination.Sum, p.First, p.Second), Plant.Number(1)));
        p.Start(0);
        Assert.Equal(1, p.State.Evaluations);

        p.Tick(300);
        Assert.Equal(1, p.State.Evaluations);

        p.Runner.Call(p.Cycle, p.Cycle.System.First);
        p.Tick(5);
        Assert.Equal(2, p.State.Evaluations);
    }

    /// <summary>Text where a number is taken is a mistake the check names; a number where a number is taken is none.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ATextWhereANumberIsTakenIsFound(bool text)
    {
        var p = new Plant();
        p.SetRadius(text ? new Arithmetic(Operator.Add, new Operand(Literal.Of("a")), Plant.Number(1)) : new Arithmetic(Operator.Add, Plant.Number(2), Plant.Number(1)));
        p.Start(0);

        var found = SequenceSummary.Checked(p.Ship.World, p.Runner.Bindings, p.Cycle, p.Cycle.System.First);
        Assert.Equal(text, found.Problem == SequenceProblem.Mistyped);
    }

    /// <summary>A wait of an expression that is no length of time is found where the program is checked.</summary>
    [Fact]
    public void AWaitOfANumberIsFoundWhereTheProgramIsChecked()
    {
        var p = new Plant();
        p.Cycle.Steps.Add(new Wait(0) { Expression = new Arithmetic(Operator.Add, Plant.Number(1), Plant.Number(2)) });
        p.Start(0);

        var found = SequenceSummary.Checked(p.Ship.World, p.Runner.Bindings, p.Cycle, p.Cycle.System.First);
        Assert.Equal(SequenceProblem.Mistyped, found.Problem);
        Assert.Equal(0, found.Step);
    }

    /// <summary>A reading of a member the block does not offer is found where the program is checked.</summary>
    [Fact]
    public void AReadingOfAMemberNoBlockOffersIsFound()
    {
        var p = new Plant();
        var reading = new BlockReading(MemberKey.Bare("Nothing"), Combination.None, new List<Literal>(), new List<string> { p.Name(p.First) });
        p.SetRadius(reading);
        p.Start(0);

        var found = SequenceSummary.Checked(p.Ship.World, p.Runner.Bindings, p.Cycle, p.Cycle.System.First);
        Assert.Equal(SequenceProblem.Unoffered, found.Problem);
    }

    /// <summary>A whole number too large for an integer fails the step as dividing by zero does, in each place one can arise.</summary>
    [Theory]
    [InlineData(Operator.Add, long.MaxValue, 1L)]
    [InlineData(Operator.Subtract, long.MinValue, 1L)]
    [InlineData(Operator.Multiply, long.MaxValue, 2L)]
    [InlineData(Operator.Multiply, long.MinValue, -1L)]
    public void AnIntegerTooLargeFailsTheStep(Operator op, long left, long right)
    {
        var p = new Plant();
        p.SetRadius(new Arithmetic(op, Plant.Number(left), Plant.Number(right)));
        p.Start();

        Assert.Equal(1.0, p.Radius);
        Assert.Equal(SequenceProblem.Overflowed, p.State.Fault.Problem);
    }

    /// <summary>A negation, an absolute value, a rounding and a sum that leave what an integer holds fail the step; one inside it does not.</summary>
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [InlineData(2, true)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    public void ANumberOutOfRangeFailsTheStep(int which, bool fails)
    {
        var p = new Plant();
        var huge = new Operand(Literal.Of(1e200));
        Expression tree;
        switch (which)
        {
            case 0: tree = new Function(FunctionKind.Round, new[] { (Expression)huge }); break;
            case 1: tree = new Negation(Plant.Number(long.MinValue)); break;
            case 2: tree = new Function(FunctionKind.Abs, new[] { Plant.Number(long.MinValue) }); break;
            case 3: tree = new Arithmetic(Operator.Multiply, huge, huge); break;
            default: tree = new Function(FunctionKind.Round, new[] { (Expression)new Operand(Literal.Of(2.5)) }); break;
        }

        p.SetRadius(tree);
        p.Start();

        Assert.Equal(fails, p.State.Fault.Problem == SequenceProblem.Overflowed);
    }

    /// <summary>A length of time too long to count in ticks fails the step.</summary>
    [Fact]
    public void ALengthOfTimeTooLongFailsTheStep()
    {
        var p = new Plant();
        p.Cycle.Steps.Add(new Wait(0) { Expression = new Arithmetic(Operator.Multiply, new Operand(Literal.Of(Duration.OfTicks(int.MaxValue))), Plant.Number(2)) });
        p.Start();

        Assert.Equal(SequenceProblem.Overflowed, p.State.Fault.Problem);
    }

    /// <summary>An if tests the lowest or the highest value across its blocks once, and compares it as a number.</summary>
    [Theory]
    [InlineData(Quantifier.Min, Relation.Below, 4, true)]
    [InlineData(Quantifier.Min, Relation.Above, 4, false)]
    [InlineData(Quantifier.Max, Relation.Above, 9, true)]
    [InlineData(Quantifier.Max, Relation.Below, 9, false)]
    public void AnIfTestsTheLowestOrHighestValue(Quantifier quantifier, Relation relation, long than, bool holds)
    {
        var p = new Plant();
        var test = new Comparison { MemberId = "Radius", Provider = "", Quantifier = quantifier, Relation = relation, Value = Literal.Of(than) };
        test.Targets.Add(p.Name(p.First));
        test.Targets.Add(p.Name(p.Second));
        test.Targets.Add(p.Name(p.Third));
        p.Cycle.Steps.Add(new If(test));
        p.SetRadius(Plant.Number(7));
        p.Cycle.Steps.Add(new EndIf());
        p.Start();

        // The blocks read 3, 5 and 10.
        Assert.Equal(holds ? 7.0 : 1.0, p.Radius);
    }

    /// <summary>An if taking the lowest or highest of no blocks has no value, so the run ends there as an average of none does.</summary>
    [Theory]
    [InlineData(Quantifier.Min)]
    [InlineData(Quantifier.Max)]
    public void AnIfTestingTheLowestOrHighestOfNoBlocksEndsTheRun(Quantifier quantifier)
    {
        var p = new Plant();
        var test = new Comparison { MemberId = "Radius", Provider = "", Quantifier = quantifier, Relation = Relation.Below, Value = Literal.Of(1.0) };
        p.Cycle.Steps.Add(new If(test));
        p.SetRadius(Plant.Number(7));
        p.Cycle.Steps.Add(new EndIf());
        p.Start();

        Assert.Equal(1.0, p.Radius);
        Assert.Equal(SequenceProblem.NoValue, p.State.Fault.Problem);
    }

    /// <summary>An abort's message is an expression worked out as the step is reached.</summary>
    [Fact]
    public void AnAbortSaysWhatItsExpressionComesTo()
    {
        var p = new Plant();
        p.Cycle.Steps.Add(new Abort
        {
            Level = AbortLevel.Warning,
            Expression = new Arithmetic(Operator.Add, new Operand(Literal.Of("Level ")), p.Reading(Combination.Sum, p.First, p.Second))
        });
        p.Start();

        Assert.Equal(SequenceProblem.AbortedWarning, p.State.Fault.Problem);
        Assert.Equal("Level 8", p.State.Fault.Which);
    }

    /// <summary>A state is set to what an expression comes to, and an expression of another kind fails the step.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AStateIsSetToWhatItsExpressionComesTo(bool flag)
    {
        var p = new Plant();
        var system = p.Cycle.System;
        system.States.Add(new State("lit", "", Literal.Of(false)));
        var reading = new BlockReading(MemberKey.Bare("OnOff"), Combination.None, new List<Literal>(), new List<string> { p.Name(p.First) });
        p.First.Enabled = flag;
        p.Cycle.Steps.Add(new SetState { State = "lit", Expression = flag ? reading : Plant.Number(4) });
        p.Start();

        if (!flag)
        {
            Assert.Equal(SequenceProblem.Mistyped, p.State.Fault.Problem);
            return;
        }

        Assert.True(p.Runner.States.Read(system, system.First, system.FindState("lit")!).Flag);
    }

    /// <summary>The words of the problems an expression comes to are said in both languages.</summary>
    [Theory]
    [InlineData(SequenceProblem.DividedByZero)]
    [InlineData(SequenceProblem.NoValue)]
    [InlineData(SequenceProblem.Overflowed)]
    [InlineData(SequenceProblem.ManyBlocks)]
    public void EveryProblemAnExpressionComesToHasWords(SequenceProblem problem)
    {
        Assert.NotEqual("", SequenceText.Problem(new Texts(), problem));
    }
}
