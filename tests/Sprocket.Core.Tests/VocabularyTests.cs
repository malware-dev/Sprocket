using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Door = Sprocket.Sim.Machine.Door;
using Sandbox.ModAPI;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// The language's members as they are written and as they run: arguments in parentheses, every member on its blocks, the six
/// comparisons, <c>sum</c>, and a <c>when</c> on any value.
/// </summary>
public class VocabularyTests
{
    const string Identity = "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43";

    /// <summary>Two lamps and a door, with a program on them and a runner ticking it.</summary>
    sealed class Ship
    {
        public readonly Rigged Rig = new();
        public readonly Automation Program = new();
        public readonly Lamp Left;
        public readonly Lamp Right;
        public readonly Door Door;
        public AutomationRunner Runner = null!;

        int _frame;

        public Ship()
        {
            Left = Rig.Light(1, "Left Lamp");
            Right = Rig.Light(2, "Right Lamp");
            Door = Rig.Door(3, "Door");
            Left.Carrying("Radius", "Radius", ValueKind.Real);
            Right.Carrying("Radius", "Radius", ValueKind.Real);
        }

        public string Name(Fitting fitting) => Program.Declare(Rig.World, Rig.Entity(fitting.Id)).Name;

        public void Start()
        {
            Program.Edited();
            Runner = new AutomationRunner(Program, Rig.World);
            Tick(3);
        }

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++_frame);
            }
        }

        /// <summary>A when on <paramref name="member" /> of the lamps, compared by <paramref name="relation" /> with <paramref name="value" />, that switches the door off.</summary>
        public WhenTrigger When(string provider, string member, Quantifier quantifier, Relation relation, Literal value,
            params Fitting[] blocks)
        {
            var when = new WhenTrigger(member) { Provider = provider, Quantifier = quantifier, Relation = relation, Value = value };
            foreach (var block in blocks)
                when.Sources.Add(Name(block));

            var off = Given.Power(false);
            off.Targets.Add(Name(Door));
            Program.AddSequence(when).Steps.Add(off);
            return when;
        }

        public int Fired => Rig.Rig.Log.Count(line => line == "Door:off");
    }

    /// <summary>Every new shape reads clean and is written back as it was read.</summary>
    [Fact]
    public void EveryShapeReadsAndIsWrittenBackAsItWas()
    {
        const string written = "sprocket 2\n\n"
            + "let door1 = block 'Door/LargeBlockDoor' [ 1 ]\n"
            + "let light = block 'InteriorLight/SmallLight' [ 2 ]\n"
            + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 3 ]\n"
            + "let panel2 = block 'ButtonPanel/ButtonPanelLarge' [ 4 ]\n"
            + "let button = sprocket.toolbar(1, 1, 4) on [ panel1, panel2 ]\n\n"
            + "system \"" + Identity + "\" as MalTek.Airlock\n{\n"
            + "    whenever button then run 1\n\n"
            + "    whenever sprocket.finishes_closing on [ door1 ] then run 1\n\n"
            + "    whenever sprocket.toolbar(1, 1, 2) on [ panel2 ] then run 1\n\n"
            + "    when all sprocket.status on [ door1 ] != closed then run 1\n\n"
            + "    when sum Radius on [ light ] >= 2.5 then run 2\n\n"
            + "    sequence 1 {\n"
            + "        call SetColor(0, 255, 0) on [ light ]\n"
            + "        if any Radius on [ light ] < 4 then {\n"
            + "            set Radius on [ light ] to 4\n"
            + "        }\n"
            + "        else if sum Radius on [ light ] > 10 then {\n"
            + "            call sprocket.close on [ door1 ]\n"
            + "        }\n"
            + "        else if all Radius on [ light ] <= 2 then {\n"
            + "            call sprocket.open on [ door1 ]\n"
            + "        }\n"
            + "        else if average Radius on [ light ] > 3 then {\n"
            + "            call sprocket.close on [ door1 ]\n"
            + "        }\n"
            + "    }\n\n"
            + "    sequence 2 {\n"
            + "        call SetColor(255, 136, 0) on [ light ]\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(written, program, new Values()));
        Assert.Equal(written, ProgramWriter.Save(program, new Values()));

        var when = (WhenTrigger)program.Systems[0].Triggers[4];
        Assert.Equal(Quantifier.Sum, when.Quantifier);
        Assert.Equal(Relation.AtLeast, when.Relation);
        Assert.Equal("", when.Provider);
        Assert.Equal(new[] { "panel1", "panel2" }, program.FindSignalRef("button")!.Blocks);
    }

    /// <summary>
    /// A move whose speed is in total says so as a third value, and one written before that existed leaves it out; both read
    /// clean and are written back as they were.
    /// </summary>
    [Fact]
    public void AMoveInTotalAndOneWithoutAreWrittenBackAsTheyWere()
    {
        const string written = "sprocket 2\n\n"
            + "let piston = block 'ExtendedPistonBase/LargePistonBase' [ 1 ]\n\n"
            + "system \"" + Identity + "\" as MalTek.Arm\n{\n"
            + "    sequence 1 {\n"
            + "        call sprocket.move_to(10, 0.3, total) on [ piston ]\n"
            + "        call sprocket.move_to(4, 2) on [ piston ]\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(written, program, new Values()));
        Assert.Equal(written, ProgramWriter.Save(program, new Values()));

        var steps = program.Systems[0].Sequences[0].Steps;
        Assert.Equal(Literal.Name(PistonMove.Total).Said, ((Act)steps[0]).Arguments[2].Said);
        Assert.Equal(2, ((Act)steps[1]).Arguments.Count);
    }

    /// <summary>A comparison is spaced or not as anybody likes, and reads the same.</summary>
    [Theory]
    [InlineData("if any Radius on [ light ] >=4 then {")]
    [InlineData("if any Radius on [light] >= 4 then {")]
    [InlineData("if any Radius on [ light ]>=  4 then {")]
    public void AComparisonReadsWhateverItsSpacing(string line)
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load("sprocket 2\n\nlet light = block 'InteriorLight/SmallLight' [ 2 ]\n\n"
            + "system \"" + Identity + "\" as MalTek.Lamp\n{\n    sequence 1 {\n        " + line + "\n        }\n    }\n}\n",
            program, new Values()));

        Assert.Contains("        if any Radius on [ light ] >= 4 then {\n", ProgramWriter.Save(program, new Values()));
    }

    /// <summary>Each comparison holds where the block's value stands that way to what it is compared with.</summary>
    [Theory]
    [InlineData(Relation.Is, 3.0, true)]
    [InlineData(Relation.Is, 4.0, false)]
    [InlineData(Relation.IsNot, 4.0, true)]
    [InlineData(Relation.Above, 2.5, true)]
    [InlineData(Relation.Above, 3.0, false)]
    [InlineData(Relation.AtLeast, 3.0, true)]
    [InlineData(Relation.Below, 3.0, false)]
    [InlineData(Relation.Below, 3.5, true)]
    [InlineData(Relation.AtMost, 3.0, true)]
    [InlineData(Relation.AtMost, 2.9, false)]
    public void EachComparisonHoldsAsItsNumbersStand(Relation relation, double than, bool holds)
    {
        var ship = new Ship();
        ship.Left.Set("Radius", Literal.Of(3.0));
        var cycle = ship.Program.AddSequence((Trigger)null!);
        var test = new Comparison { MemberId = "Radius", Relation = relation, Value = Literal.Of(than) };
        test.Targets.Add(ship.Name(ship.Left));
        var off = Given.Power(false);
        off.Targets.Add(ship.Name(ship.Door));
        cycle.Steps.Add(new If(test));
        cycle.Steps.Add(off);
        cycle.Steps.Add(new EndIf());

        ship.Start();
        ship.Runner.Call(cycle);
        ship.Tick(5);

        Assert.Equal(holds ? 1 : 0, ship.Fired);
    }

    /// <summary>An integer stands for a real: a lamp at 3 is at least 3 written whole.</summary>
    [Fact]
    public void AnIntegerStandsForAReal()
    {
        var ship = new Ship();
        ship.Left.Set("Radius", Literal.Of(3.0));
        var cycle = ship.Program.AddSequence((Trigger)null!);
        var test = new Comparison { MemberId = "Radius", Relation = Relation.AtLeast, Value = Literal.Of(3L) };
        test.Targets.Add(ship.Name(ship.Left));
        var off = Given.Power(false);
        off.Targets.Add(ship.Name(ship.Door));
        cycle.Steps.Add(new If(test));
        cycle.Steps.Add(off);
        cycle.Steps.Add(new EndIf());

        ship.Start();
        ship.Runner.Call(cycle);
        ship.Tick(5);

        Assert.Equal(1, ship.Fired);
    }

    /// <summary><c>sum</c> compares the total of what every block reads once, and <c>average</c> compares the readings added and divided by how many blocks were read.</summary>
    [Theory]
    [InlineData(Quantifier.Sum, Relation.AtLeast, 7.0, true)]
    [InlineData(Quantifier.Sum, Relation.Above, 7.0, false)]
    [InlineData(Quantifier.Sum, Relation.Is, 7.0, true)]
    [InlineData(Quantifier.Average, Relation.Is, 3.5, true)]
    [InlineData(Quantifier.Average, Relation.Above, 3.5, false)]
    [InlineData(Quantifier.Average, Relation.Below, 4.0, true)]
    public void ASumComparesTheTotalAndAnAverageTheMean(Quantifier quantifier, Relation relation, double than, bool holds)
    {
        var ship = new Ship();
        ship.Left.Set("Radius", Literal.Of(3.0));
        ship.Right.Set("Radius", Literal.Of(4.0));
        var cycle = ship.Program.AddSequence((Trigger)null!);
        var test = new Comparison { MemberId = "Radius", Quantifier = quantifier, Relation = relation, Value = Literal.Of(than) };
        test.Targets.Add(ship.Name(ship.Left));
        test.Targets.Add(ship.Name(ship.Right));
        var off = Given.Power(false);
        off.Targets.Add(ship.Name(ship.Door));
        cycle.Steps.Add(new If(test));
        cycle.Steps.Add(off);
        cycle.Steps.Add(new EndIf());

        ship.Start();
        ship.Runner.Call(cycle);
        ship.Tick(5);

        Assert.Equal(holds ? 1 : 0, ship.Fired);
    }

    /// <summary><c>when average</c> fires once, as the average comes to hold.</summary>
    [Fact]
    public void AWhenAverageFiresAsTheMeanComesToHold()
    {
        var ship = new Ship();
        ship.Left.Set("Radius", Literal.Of(2.0));
        ship.Right.Set("Radius", Literal.Of(2.0));
        ship.When("", "Radius", Quantifier.Average, Relation.AtLeast, Literal.Of(3.0), ship.Left, ship.Right);
        ship.Start();
        ship.Tick(30);
        Assert.Equal(0, ship.Fired);

        ship.Left.Set("Radius", Literal.Of(4.0));
        ship.Tick(30);
        Assert.Equal(1, ship.Fired);
    }

    /// <summary>A <c>when</c> on one of the block's own values fires as its comparison comes to hold, and once.</summary>
    [Fact]
    public void AWhenOnAValueFiresAsItsComparisonComesToHold()
    {
        var ship = new Ship();
        ship.Left.Set("Radius", Literal.Of(2.0));
        ship.When("", "Radius", Quantifier.Any, Relation.Above, Literal.Of(5.0), ship.Left);
        ship.Start();
        ship.Tick(30);
        Assert.Equal(0, ship.Fired);

        ship.Left.Set("Radius", Literal.Of(6.0));
        ship.Tick(30);
        Assert.Equal(1, ship.Fired);

        ship.Left.Set("Radius", Literal.Of(7.0));
        ship.Tick(30);
        Assert.Equal(1, ship.Fired);
    }

    /// <summary>A <c>when</c> on a value being other than a name fires as the block leaves it.</summary>
    [Fact]
    public void AWhenOnIsNotFiresAsTheBlockLeavesTheState()
    {
        var ship = new Ship();
        ship.When(BlockKinds.Own, "status", Quantifier.Any, Relation.IsNot, Literal.Name("closed"), ship.Door);
        ship.Start();
        ship.Tick(30);
        Assert.Equal(0, ship.Fired);

        ship.Door.ForceOpen();
        ship.Tick(30);
        Assert.Equal(1, ship.Fired);
    }

    /// <summary><c>when sum</c> fires once, as the total comes to hold, and not again until it has stopped holding.</summary>
    [Fact]
    public void AWhenSumFiresOnceAsTheTotalComesToHold()
    {
        var ship = new Ship();
        ship.Left.Set("Radius", Literal.Of(2.0));
        ship.Right.Set("Radius", Literal.Of(2.0));
        ship.When("", "Radius", Quantifier.Sum, Relation.AtLeast, Literal.Of(7.0), ship.Left, ship.Right);
        ship.Start();
        ship.Tick(30);

        ship.Left.Set("Radius", Literal.Of(4.0));
        ship.Tick(30);
        Assert.Equal(0, ship.Fired);

        ship.Right.Set("Radius", Literal.Of(3.0));
        ship.Tick(30);
        Assert.Equal(1, ship.Fired);

        ship.Right.Set("Radius", Literal.Of(4.0));
        ship.Tick(30);
        Assert.Equal(1, ship.Fired);

        ship.Right.Set("Radius", Literal.Of(1.0));
        ship.Tick(30);
        ship.Right.Set("Radius", Literal.Of(5.0));
        ship.Tick(30);
        Assert.Equal(2, ship.Fired);
    }

    /// <summary>A function's argument is one of the words its field lists, and within the bounds it sets.</summary>
    [Theory]
    [InlineData("up", 5.0, SequenceProblem.None)]
    [InlineData("sideways", 5.0, SequenceProblem.Mistyped)]
    [InlineData("up", 11.0, SequenceProblem.Mistyped)]
    [InlineData("down", -1.0, SequenceProblem.Mistyped)]
    public void AFunctionsArgumentIsOneOfItsWordsAndWithinItsBounds(string way, double by, SequenceProblem problem)
    {
        var ship = new Ship();
        var ways = new[] { "up", "down" };
        var amount = Field.Between("by", "0", "10");
        amount.Entry = FieldInput.Decimal;
        ship.Rig.Entity(ship.Left.Id).Kind.WithValue("brighter", "brighter",
            OwnValue.Number<IMyTerminalBlock, double>(arguments => arguments[1].Real, (_, more) => more)
                .Taking(Field.Among("way", ways, ways), amount));

        var cycle = ship.Program.AddSequence(new EveryTrigger(600));
        var test = new Comparison
        {
            MemberId = "brighter", Provider = "sprocket", Quantifier = Quantifier.Any, Relation = Relation.AtLeast,
            Value = Literal.Of(1.0)
        };

        test.Arguments.Add(Literal.Name(way));
        test.Arguments.Add(Literal.Of(by));
        test.Targets.Add(ship.Name(ship.Left));
        cycle.Steps.Add(new If(test));
        cycle.Steps.Add(new EndIf());

        ship.Start();
        Assert.Equal(problem, SequenceSummary.Checked(ship.Rig.World, ship.Runner.Bindings, cycle, cycle.System.First).Problem);
    }

    /// <summary>A value of Sprocket's own that can be set is set by a step, qualified by its namespace, and read back so.</summary>
    [Fact]
    public void AnOwnValueThatCanBeSetIsSetByAStep()
    {
        var ship = new Ship();
        var level = 0.0;
        ship.Rig.Entity(ship.Left.Id).Kind.WithValue("level", "level",
                OwnValue.Number<IMyTerminalBlock>(_ => level).Writing<IMyTerminalBlock>((_, value) => level = value.Number))
            .Looked("A test value.");

        var cycle = ship.Program.AddSequence((Trigger)null!);
        var set = new Act("level") { Form = ReachKind.Value, Provider = BlockKinds.Own };
        set.Targets.Add(ship.Name(ship.Left));
        set.Arguments.Add(Literal.Of(3.0));
        cycle.Steps.Add(set);

        ship.Start();
        Assert.Contains("    sequence 1 {\n        set sprocket.level on [ 'Left Lamp' ] to 3\n",
            ProgramWriter.Save(ship.Program, new Values()));

        ship.Runner.Call(cycle, cycle.System.First);
        ship.Tick();
        Assert.Equal(3.0, level);
    }

    /// <summary>
    /// A set of a value of Sprocket's own is a fault where the value is only read, and where it is given none of its names.
    /// </summary>
    [Theory]
    [InlineData("status", "closed", SequenceProblem.Unoffered)]
    [InlineData("speed", "medium", SequenceProblem.Mistyped)]
    [InlineData("speed", "high", SequenceProblem.None)]
    public void ASetOfAnOwnValueItCannotTakeIsAFault(string value, string name, SequenceProblem problem)
    {
        var ship = new Ship();
        var at = 0;
        ship.Rig.Entity(ship.Door.Id).Kind.WithValue("speed", "speed",
                OwnValue.OneOf<IMyTerminalBlock>(new ChoiceType(BlockKinds.Own, "level", "low", "high"), _ => at).Writing<IMyTerminalBlock>((_, given) => at = (int)given.Whole))
            .Looked("A test value.");

        var cycle = ship.Program.AddSequence(new EveryTrigger(600));
        var set = new Act(value) { Form = ReachKind.Value, Provider = BlockKinds.Own };
        set.Targets.Add(ship.Name(ship.Door));
        set.Arguments.Add(Literal.Name(name));
        cycle.Steps.Add(set);

        ship.Start();
        Assert.Equal(problem, SequenceSummary.Checked(ship.Rig.World, ship.Runner.Bindings, cycle, cycle.System.First).Problem);
    }

    /// <summary>A value of Sprocket's own given bounds holds a step setting it to them, at either end.</summary>
    [Theory]
    [InlineData(11.0, SequenceProblem.Mistyped)]
    [InlineData(-1.0, SequenceProblem.Mistyped)]
    [InlineData(10.0, SequenceProblem.None)]
    [InlineData(0.0, SequenceProblem.None)]
    public void ASetOfAnOwnValueIsHeldToItsBounds(double given, SequenceProblem problem)
    {
        var ship = new Ship();
        var level = 0.0;
        ship.Rig.Entity(ship.Left.Id).Kind.WithValue("level", "level",
                OwnValue.Number<IMyTerminalBlock>(_ => level).Writing<IMyTerminalBlock>((_, value) => level = value.Number))
            .Looked("A test value.").Between("0", "10");

        var cycle = ship.Program.AddSequence(new EveryTrigger(600));
        var set = new Act("level") { Form = ReachKind.Value, Provider = BlockKinds.Own };
        set.Targets.Add(ship.Name(ship.Left));
        set.Arguments.Add(Literal.Of(given));
        cycle.Steps.Add(set);

        ship.Start();
        Assert.Equal(problem, SequenceSummary.Checked(ship.Rig.World, ship.Runner.Bindings, cycle, cycle.System.First).Problem);
    }

    /// <summary>Bounds belong to a value of Sprocket's own, so a kind whose last addition is none refuses them.</summary>
    [Fact]
    public void BoundsWithNoOwnValueBeforeThemAreRefused()
    {
        var kind = new EntityKind("Test/Block", "Block");
        Assert.Throws<InvalidOperationException>(() => kind.Between("0", "10"));

        kind.WithValue("level", "level", OwnValue.Number<IMyTerminalBlock>(_ => 0.0))
            .WithEvent("bumped", Reading.Of<IMyTerminalBlock>(_ => false), SignalEdge.Rising, "bumped");
        Assert.Throws<InvalidOperationException>(() => kind.Between("0", "10"));
    }

    /// <summary>A set qualified by Sprocket's namespace reads and is written back as it was.</summary>
    [Fact]
    public void ASetOfAnOwnValueReadsAndIsWrittenBack()
    {
        var written = "sprocket 2\n\n"
            + "let door1 = block 'Door/LargeBlockDoor' [ 1 ]\n\n"
            + "system \"" + Identity + "\" as MalTek.Airlock\n{\n"
            + "    sequence 1 {\n"
            + "        set sprocket.speed on [ door1 ] to high\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(written, program, new Values()));
        Assert.Equal(written, ProgramWriter.Save(program, new Values()));
        var set = Assert.IsType<Act>(program.Systems[0].Sequences[0].Steps[0]);
        Assert.Equal(BlockKinds.Own, set.Provider);
        Assert.Equal(ReachKind.Value, set.Form);
    }

    /// <summary>
    /// A step's value is one its field admits: one of the words it lists, for an action as for a value, and within the bounds
    /// the block declares.
    /// </summary>
    [Theory]
    [InlineData("depressurize", 5L, SequenceProblem.Mistyped)]
    [InlineData("depressurize", 1L, SequenceProblem.None)]
    [InlineData("Intensity", 10L, SequenceProblem.Mistyped)]
    [InlineData("Intensity", 3L, SequenceProblem.None)]
    public void AStepsValueIsOneItsFieldAdmits(string member, long given, SequenceProblem problem)
    {
        var ship = new Ship();
        var vent = ship.Rig.Vent(4, "Vent");
        ship.Left.Carrying("Intensity", "Intensity", ValueKind.Real, "0.5", "5");

        var cycle = ship.Program.AddSequence(new EveryTrigger(600));
        var vents = member == "depressurize";
        var step = new Act(member) { Form = vents ? ReachKind.Native : ReachKind.Value };
        step.Targets.Add(vents ? ship.Name(vent) : ship.Name(ship.Left));
        step.Arguments.Add(Literal.Of(given));
        cycle.Steps.Add(step);
        if (vents)
            cycle.Steps.Add(new Await());

        ship.Start();
        Assert.Equal(problem, SequenceSummary.Checked(ship.Rig.World, ship.Runner.Bindings, cycle, cycle.System.First).Problem);
    }

    /// <summary>A test compares with whatever it likes: a number outside a value's bounds still says something.</summary>
    [Fact]
    public void ATestIsNotHeldToAValuesBounds()
    {
        var ship = new Ship();
        ship.Left.Carrying("Intensity", "Intensity", ValueKind.Real, "0.5", "5");
        var cycle = ship.Program.AddSequence(new EveryTrigger(600));
        var test = new Comparison
        {
            MemberId = "Intensity", Provider = "", Quantifier = Quantifier.Any, Relation = Relation.Below, Value = Literal.Of(10.0)
        };

        test.Targets.Add(ship.Name(ship.Left));
        cycle.Steps.Add(new If(test));
        cycle.Steps.Add(new EndIf());

        ship.Start();
        Assert.Equal(SequenceProblem.None, SequenceSummary.Checked(ship.Rig.World, ship.Runner.Bindings, cycle, cycle.System.First).Problem);
    }

    /// <summary>A <c>when sum</c> with one of its blocks gone holds no total, so what is left never fires it.</summary>
    [Fact]
    public void AWhenSumMissingABlockNeverHolds()
    {
        var ship = new Ship();
        ship.Left.Set("Radius", Literal.Of(2.0));
        ship.Right.Set("Radius", Literal.Of(4.0));
        ship.When("", "Radius", Quantifier.Sum, Relation.AtMost, Literal.Of(3.0), ship.Left, ship.Right);
        ship.Start();
        ship.Tick(30);

        ship.Rig.Grind(ship.Right);
        ship.Tick(30);
        Assert.Equal(0, ship.Fired);
    }

    /// <summary>A number the block was never told is still a number, and a test comparing it as one is sound.</summary>
    [Fact]
    public void AnUntoldNumberIsComparedAsANumber()
    {
        var ship = new Ship();
        var cycle = ship.Program.AddSequence(new EveryTrigger(600));
        var test = new Comparison
        {
            MemberId = "Radius", Provider = "", Quantifier = Quantifier.Any, Relation = Relation.Above, Value = Literal.Of(2.0)
        };

        test.Targets.Add(ship.Name(ship.Left));
        cycle.Steps.Add(new If(test));
        cycle.Steps.Add(new EndIf());

        ship.Start();
        Assert.Equal(SequenceProblem.None, SequenceSummary.Checked(ship.Rig.World, ship.Runner.Bindings, cycle, cycle.System.First).Problem);
    }

    /// <summary>A <c>when</c> on a value reads as its comparison; one on Sprocket's own value read for off, as leaving the state; one over several blocks that must all get there says its blocks' words in the plural.</summary>
    [Fact]
    public void AWhenReadsAsItsComparisonAndTakesThePluralOverSeveralBlocks()
    {
        var ship = new Ship();
        var radius = ship.When("", "Radius", Quantifier.Sum, Relation.AtLeast, Literal.Of(7.0), ship.Left, ship.Right);
        var open = ship.When(BlockKinds.Own, "status", Quantifier.Any, Relation.IsNot, Literal.Name("closed"), ship.Door);
        var on = ship.When(BlockKinds.Own, "power", Quantifier.All, Relation.Is, Literal.Of(true), ship.Left, ship.Right);
        ship.Start();

        Assert.Equal("When the sum of the radius on Left Lamp and Right Lamp is at least 7 m",
            SequenceText.Trigger(null!, ship.Runner.Bindings, null!, radius, null));
        Assert.Equal("When Door is not closed", SequenceText.Trigger(null!, ship.Runner.Bindings, null!, open, null));
        Assert.Equal("When Left Lamp and Right Lamp turn on", SequenceText.Trigger(null!, ship.Runner.Bindings, null!, on, null));
    }

    /// <summary>A comparison a block's member cannot be read for is a fault found before anything runs, and a state tested by one of the names its value has is sound.</summary>
    [Theory]
    [InlineData("sprocket", "status", Quantifier.Sum, Relation.Is, "closed", SequenceProblem.Mistyped)]
    [InlineData("sprocket", "status", Quantifier.Any, Relation.Above, "closed", SequenceProblem.Mistyped)]
    [InlineData("sprocket", "status", Quantifier.Any, Relation.Is, "ajar", SequenceProblem.Mistyped)]
    [InlineData("", "Radius", Quantifier.Any, Relation.Is, "", SequenceProblem.Mistyped)]
    [InlineData("sprocket", "status", Quantifier.Any, Relation.Is, "closed", SequenceProblem.None)]
    public void ATestItsBlockCannotReadIsMistypedAndAStateByItsNameIsSound(string provider, string member, Quantifier quantifier,
        Relation relation, string name, SequenceProblem problem)
    {
        Assert.Equal(problem, Checked(provider, member, quantifier, relation, name).Problem);
    }

    /// <summary>
    /// A function of Sprocket's own is given an argument for each of its parameters, of the kind each asks for: one left out
    /// is a fault, and so is one of another kind or one too many.
    /// </summary>
    [Theory]
    [InlineData("", SequenceProblem.Unfilled)]
    [InlineData("x", SequenceProblem.Mistyped)]
    [InlineData("2 3", SequenceProblem.Mistyped)]
    [InlineData("2", SequenceProblem.None)]
    public void AFunctionIsGivenAnArgumentForEachParameter(string given, SequenceProblem problem)
    {
        var ship = new Ship();
        var by = Field.Called("by");
        by.Entry = FieldInput.Decimal;
        ship.Rig.Entity(ship.Left.Id).Kind.WithValue("brighter", "brighter",
            OwnValue.Number<IMyTerminalBlock, double>(arguments => arguments[0].Real, (_, more) => more).Taking(by));

        var cycle = ship.Program.AddSequence(new EveryTrigger(600));
        var test = new Comparison
        {
            MemberId = "brighter", Provider = "sprocket", Quantifier = Quantifier.Any, Relation = Relation.AtLeast,
            Value = Literal.Of(1.0)
        };

        foreach (var word in given.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            test.Arguments.Add(double.TryParse(word, out var number) ? Literal.Of(number) : Literal.Of(word));

        test.Targets.Add(ship.Name(ship.Left));
        cycle.Steps.Add(new If(test));
        cycle.Steps.Add(new EndIf());

        ship.Start();
        Assert.Equal(problem, SequenceSummary.Checked(ship.Rig.World, ship.Runner.Bindings, cycle, cycle.System.First).Problem);
    }

    /// <summary>The fault of an <c>if</c> on <paramref name="member" /> of the door, or of a lamp for Radius.</summary>
    static SequenceFault Checked(string provider, string member, Quantifier quantifier, Relation relation, string name)
    {
        var ship = new Ship();
        var cycle = ship.Program.AddSequence(new EveryTrigger(600));
        var test = new Comparison
        {
            MemberId = member, Provider = provider, Quantifier = quantifier, Relation = relation,
            Value = name.Length == 0 ? Literal.Of(true) : Literal.Name(name)
        };

        test.Targets.Add(ship.Name(member == "Radius" ? ship.Left : ship.Door));
        cycle.Steps.Add(new If(test));
        cycle.Steps.Add(new EndIf());

        ship.Start();
        return SequenceSummary.Checked(ship.Rig.World, ship.Runner.Bindings, cycle, cycle.System.First);
    }

    /// <summary>
    /// A change picked on blocks starts a sequence as the signal it is, sent from any of them; asked of all of them, as the
    /// value it is a change in coming to be so on the last of them.
    /// </summary>
    [Fact]
    public void AChangePickedOnBlocksIsItsSignalOrItsValueOnAll()
    {
        var ship = new Ship();
        var events = new List<EntityEvent>(ship.Rig.Entity(ship.Door.Id).Kind.Events);

        var any = Assert.IsType<SignalTrigger>(Vocabulary.Starter(events, new MemberKey(BlockKinds.Own, "finishes_closing"), false));
        Assert.True(any.IsWatched);
        Assert.Equal(BlockKinds.Own, any.Provider);
        Assert.Equal("finishes_closing", any.EventId);

        var all = Assert.IsType<WhenTrigger>(Vocabulary.Starter(events, new MemberKey(BlockKinds.Own, "finishes_closing"), true));
        Assert.Equal("status", all.EventId);
        Assert.Equal(BlockKinds.Own, all.Provider);
        Assert.Equal(Quantifier.All, all.Quantifier);
        Assert.Equal(Relation.Is, all.Relation);
        Assert.Equal(Literal.Name("closed"), all.Value);
    }

    /// <summary>A toolbar signal written in place is answered by its slot on the blocks it names.</summary>
    [Fact]
    public void ASignalWrittenInPlaceIsAnsweredOnItsBlocks()
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load("sprocket 2\n\n"
            + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 3 ]\n"
            + "let panel2 = block 'ButtonPanel/ButtonPanelLarge' [ 4 ]\n\n"
            + "system \"" + Identity + "\" as MalTek.Bell\n{\n    whenever sprocket.toolbar(1, 2, 3) on [ panel2 ] then run 1\n\n"
            + "    sequence 1 {\n    }\n}\n",
            program, new Values()));

        var trigger = Assert.IsType<SignalTrigger>(program.Systems[0].Triggers[0]);
        Assert.False(trigger.IsWatched);
        var blocks = new List<string>();
        var at = new List<ToolbarSlot>();
        program.Answers(program.Systems[0], null, trigger, blocks, at);

        Assert.Equal(new[] { "panel2" }, blocks);
        var slot = Assert.Single(at);
        Assert.Equal(2, slot.Page);
        Assert.Equal(3, slot.Slot);
    }

    /// <summary>Every parameter is given an argument: one left out is a fault, and so is one left over.</summary>
    [Theory]
    [InlineData(0, SequenceProblem.Unfilled)]
    [InlineData(2, SequenceProblem.Mistyped)]
    public void EveryParameterIsGivenOneArgument(int given, SequenceProblem problem)
    {
        var ship = new Ship();
        var vent = ship.Rig.Vent(6, "Vent");
        var cycle = ship.Program.AddSequence((Trigger)null!);
        var act = new Act(Blocks.Vent.Depressurize) { Form = ReachKind.Native };
        act.Targets.Add(ship.Name(vent));
        for (var i = 0; i < given; i++)
            act.Arguments.Add(Literal.Of(Blocks.Vent.Hold));

        cycle.Steps.Add(act);

        ship.Start();
        var fault = SequenceSummary.Checked(ship.Rig.World, ship.Runner.Bindings, cycle, cycle.System.First);

        Assert.Equal(problem, fault.Problem);
    }

    /// <summary>An argument to an action that declares no parameters is one left over.</summary>
    [Fact]
    public void AnArgumentToAnActionWithoutParametersIsLeftOver()
    {
        var ship = new Ship();
        var cycle = ship.Program.AddSequence((Trigger)null!);
        var act = new Act("close") { Form = ReachKind.Native };
        act.Targets.Add(ship.Name(ship.Door));
        act.Arguments.Add(Literal.Of(1L));
        cycle.Steps.Add(act);

        ship.Start();
        var fault = SequenceSummary.Checked(ship.Rig.World, ship.Runner.Bindings, cycle, cycle.System.First);

        Assert.Equal(SequenceProblem.Mistyped, fault.Problem);
    }

    /// <summary>A signal named on two blocks is sent from either: an <c>on</c> naming it is answered by the slot on both.</summary>
    [Fact]
    public void ASignalOnTwoBlocksIsAnsweredOnBoth()
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load("sprocket 2\n\n"
            + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 3 ]\n"
            + "let panel2 = block 'ButtonPanel/ButtonPanelLarge' [ 4 ]\n"
            + "let button = sprocket.toolbar(1, 1, 4) on [ panel1, panel2 ]\n\n"
            + "system \"" + Identity + "\" as MalTek.Bell\n{\n    whenever button then run 1\n\n    sequence 1 {\n    }\n}\n",
            program, new Values()));

        var blocks = new List<string>();
        var at = new List<ToolbarSlot>();
        program.Answers(program.Systems[0], null, "button", blocks, at);

        Assert.Equal(new[] { "panel1", "panel2" }, blocks);
        Assert.All(at, slot => Assert.Equal(4, slot.Slot));
    }

    /// <summary>A signal picked on one block is a variable of its own, never one that is also sent from other blocks.</summary>
    [Fact]
    public void ASignalPickedOnOneBlockIsNotOneOnSeveral()
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load("sprocket 2\n\n"
            + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 3 ]\n"
            + "let panel2 = block 'ButtonPanel/ButtonPanelLarge' [ 4 ]\n"
            + "let button = sprocket.toolbar(1, 1, 4) on [ panel1, panel2 ]\n\n",
            program, new Values()));

        var picked = program.DeclareSignal("panel1", new ToolbarSlot(1, 1, 4), "panel1 4");

        Assert.NotEqual("button", picked.Name);
        Assert.Equal(new[] { "panel1" }, picked.Blocks);
        Assert.Same(picked, program.DeclareSignal("panel1", new ToolbarSlot(1, 1, 4), "panel1 4"));
    }

    /// <summary>An instance answered by a signal variable on several blocks writes the variable back once, and a part of one in place.</summary>
    [Fact]
    public void ASignalVariableIsWrittenOnlyWhereAllOfItAnswers()
    {
        const string head = "sprocket 2\n\n"
            + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 3 ]\n"
            + "let panel2 = block 'ButtonPanel/ButtonPanelLarge' [ 4 ]\n"
            + "let button = sprocket.toolbar(1, 1, 4) on [ panel1, panel2 ]\n\n"
            + "system blueprint \"" + Identity + "\" as MalTek.Bell\n{\n    optional slot bell expects signal\n\n"
            + "    whenever bell then run 1\n\n    sequence 1 {\n    }\n}\n\n";

        var whole = new Automation();
        Assert.Equal("", ProgramReader.Load(head + "instance 1 of MalTek.Bell {\n    bell = [ button ]\n}\n", whole, new Values()));
        Assert.Contains("    bell = [ button ]\n", ProgramWriter.Save(whole, new Values()));

        var part = new Automation();
        Assert.Equal("", ProgramReader.Load(head.Replace("let button = sprocket.toolbar(1, 1, 4) on [ panel1, panel2 ]\n", "")
            + "instance 1 of MalTek.Bell {\n    bell = [ sprocket.toolbar(1, 1, 4) on [ panel1 ] ]\n}\n", part, new Values()));
        part.Restore(new SignalRef("button", new[] { "panel1", "panel2" }, new ToolbarSlot(1, 1, 4)));

        Assert.Contains("    bell = [ sprocket.toolbar(1, 1, 4) on [ panel1 ] ]\n", ProgramWriter.Save(part, new Values()));
    }
}
