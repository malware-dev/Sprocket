using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A watcher or a clock whose value, options or interval is an expression, looked at on its cadence and on what the blocks it reads announce.</summary>
public class ExpressionWatchTests
{
    sealed class Watching
    {
        public readonly Rigged Ship = new();
        public readonly Lamp Low;
        public readonly Lamp High;
        public readonly Lamp Out;
        public readonly Automation Program = new();
        public readonly Sequence Cycle;
        public readonly AutomationRunner Runner;
        int _frame;

        /// <summary>A watcher of the high lamp's radius, firing a step that sets the out lamp's radius to 9, comparing with <paramref name="value" />; or a clock every <paramref name="interval" />.</summary>
        public Watching(Func<Watching, Literal> value, Func<Watching, Expression>? interval = null)
        {
            Low = Lamp(1, "Low", 3);
            High = Lamp(2, "High", 5);
            Out = Lamp(3, "Out", 1);
            Trigger trigger;
            if (interval == null)
            {
                var when = new WhenTrigger("Radius") { Provider = "", Quantifier = Quantifier.All, Relation = Relation.Above, Value = value(this) };
                when.Sources.Add(Name(High));
                trigger = when;
            }
            else
            {
                trigger = new EveryTrigger(0) { IntervalExpression = interval(this) };
            }

            Cycle = Program.AddSequence(trigger);
            var act = new Act("Radius") { Form = ReachKind.Value };
            act.Targets.Add(Name(Out));
            act.Arguments.Add(Literal.Of(9.0));
            Cycle.Steps.Add(act);
            Runner = new AutomationRunner(Program, Ship.World);
        }

        Lamp Lamp(long id, string name, double radius)
        {
            var lamp = Ship.Light(id, name);
            lamp.Carrying("Radius", "Radius", ValueKind.Real, "1", "50000");
            lamp.Set("Radius", Literal.Of(radius));
            return lamp;
        }

        public string Name(Lamp lamp) => Program.Declare(Ship.World, Ship.Entity(lamp.Id)).Name;

        public BlockReading Reading(Lamp lamp) =>
            new BlockReading(MemberKey.Bare("Radius"), Combination.None, new List<Literal>(), new List<string> { Name(lamp) });

        public void Tick(int ticks)
        {
            for (var i = 0; i < ticks; i++)
            {
                Ship.Tick();
                Runner.Tick(++_frame);
            }
        }

        public int Fired => Ship.Rig.Log.Count(line => line.StartsWith("Out:Radius 9"));
    }

    static Literal Higher(Watching w) => Literal.Worked(new Arithmetic(Operator.Add, w.Reading(w.Low), new Operand(Literal.Of(1L))));

    /// <summary>A watcher compares with what its expression comes to as it looks, and fires as the comparison comes to hold.</summary>
    [Fact]
    public void AWatcherComparesWithWhatItsExpressionComesTo()
    {
        var w = new Watching(Higher);
        w.Tick(30);
        Assert.Equal(0, w.Fired);

        // 5 is above 3 + 1, so it held from the start; raising what it reads stops it holding, and lowering it brings it back.
        w.Low.Set("Radius", Literal.Of(8.0));
        w.Tick(30);
        Assert.Equal(0, w.Fired);

        w.Low.Set("Radius", Literal.Of(1.0));
        w.Tick(30);
        Assert.Equal(1, w.Fired);
    }

    /// <summary>A watcher whose expression has no value holds on no look, and fires once it has one.</summary>
    [Fact]
    public void AWatcherWhoseExpressionHasNoValueHoldsOnNoLook()
    {
        var w = new Watching(w => Literal.Worked(new Arithmetic(Operator.Divide, new Operand(Literal.Of(10L)), w.Reading(w.Low))));
        w.Low.Set("Radius", Literal.Of(0.0));
        w.Tick(30);
        Assert.Equal(0, w.Fired);

        // 10 / 20 is below the 5 the watched lamp reads, so it comes to hold; 10 / 1 is not, and it comes to hold again.
        w.Low.Set("Radius", Literal.Of(20.0));
        w.Tick(30);
        Assert.Equal(1, w.Fired);

        w.Low.Set("Radius", Literal.Of(1.0));
        w.Tick(30);
        w.Low.Set("Radius", Literal.Of(20.0));
        w.Tick(30);
        Assert.Equal(2, w.Fired);
    }

    /// <summary>A clock whose interval is an expression comes round as often as the expression says at each look.</summary>
    [Fact]
    public void AClockWithAnExpressionIntervalComesRoundAsOftenAsItSays()
    {
        var w = new Watching(null!, w => new Arithmetic(Operator.Multiply, new Operand(Literal.Of(Duration.OfTicks(10))), w.Reading(w.Low)));
        w.Tick(100);

        // Every thirty ticks, from a reading of 3.
        Assert.InRange(w.Fired, 2, 4);
    }

    /// <summary>Looking at a watcher whose value is an expression of readings allocates no more than looking at one whose value is a literal.</summary>
    [Fact]
    public void LookingAtAnExpressionAllocatesNoMoreThanALiteral()
    {
        var plain = new Watching(_ => Literal.Of(4L));
        var worked = new Watching(Higher);
        plain.Tick(60);
        worked.Tick(60);

        AppDomain.MonitoringIsEnabled = true;
        GC.Collect();
        var before = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
        plain.Tick(20000);
        var middle = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;
        worked.Tick(20000);
        var after = AppDomain.CurrentDomain.MonitoringTotalAllocatedMemorySize;

        // Twenty thousand ticks are two thousand looks each, so the slack, which absorbs the allocation counter's granularity,
        // allows under ten bytes a look: any object made per look shows.
        Assert.True(after - middle <= middle - before + 16384, $"literal {middle - before} bytes, expression {after - middle} bytes");
    }

    /// <summary>A state is set by a step no block announces, so a watcher whose expression reads one is never counted as hearing everything.</summary>
    [Fact]
    public void AWatcherReadingAStateKeepsItsCadence()
    {
        var scope = new WatchScope(null!, null!, null!, () => { });

        scope.Prepare(new List<Expression> { new Operand(Literal.Of(4L)) }, null!, true);
        Assert.True(scope.AllHeard);

        scope.Prepare(new List<Expression> { new Arithmetic(Operator.Add, new StateOperand("level"), new Operand(Literal.Of(1L))) }, null!, true);
        Assert.False(scope.AllHeard);
    }

    /// <summary>A watcher listens to the blocks its expression reads, so a change in a value they announce is looked at at once.</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(33)]
    [InlineData(66)]
    public void AWatcherListensToTheBlocksItsExpressionReads(int offset)
    {
        var ship = new Rigged();
        var first = ship.Door(1, "First");
        var second = ship.Door(2, "Second");
        var third = ship.Door(3, "Third");
        first.ForceOpen();

        var program = new Automation();
        var firstName = program.Declare(ship.World, ship.Entity(first.Id)).Name;
        var secondName = program.Declare(ship.World, ship.Entity(second.Id)).Name;
        var thirdName = program.Declare(ship.World, ship.Entity(third.Id)).Name;

        // When the first door's status is the second's, which the first announces and the second only the expression reads.
        var reading = new BlockReading(new MemberKey("sprocket", "status"), Combination.None, new List<Literal>(), new List<string> { secondName });
        var when = new WhenTrigger("status") { Provider = "sprocket", Quantifier = Quantifier.All, Relation = Relation.Is, Value = Literal.Worked(reading), Interval = Duration.OfTicks(100) };
        when.Sources.Add(firstName);
        var sequence = program.AddSequence(when);
        var act = new Act("open");
        act.Targets.Add(thirdName);
        sequence.Steps.Add(act);

        var runner = new AutomationRunner(program, ship.World);
        var frame = 0;
        for (var i = 0; i < 400 + offset; i++)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.DoesNotContain("Third:open", ship.Rig.Log);

        // Only the second door changes, and the watcher is told, so it looks within a few ticks and not on its hundred-tick cadence.
        second.ForceOpen();
        var looked = 0;
        while (looked < 150 && !ship.Rig.Log.Contains("Third:open"))
        {
            ship.Tick();
            runner.Tick(++frame);
            looked++;
        }

        Assert.InRange(looked, 1, 12);
    }
}
