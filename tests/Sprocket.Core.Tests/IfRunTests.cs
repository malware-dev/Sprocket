using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>An <c>if</c> tests its blocks once, when the run reaches it, and runs the branch whose test holds.</summary>
public class IfRunTests
{
    sealed class Airlock
    {
        public readonly Rigged Rig = new();

        public readonly Door Inner;
        public readonly Door Outer;
        public readonly Vent Vent;
        public readonly Lamp Light;

        public readonly Automation Program = new();
        public readonly Sequence Cycle;
        public AutomationRunner Runner = null!;

        int _frame;

        public Airlock()
        {
            Inner = Rig.Door(1, "Inner Door");
            Outer = Rig.Door(2, "Outer Door");
            Vent = Rig.Vent(3, "Airlock Vent");
            Light = Rig.Light(4, "Airlock Light");
            Light.Carrying("OnOff", "On", ValueKind.Flag);

            Cycle = Program.AddSequence((Trigger)null!);
            Cycle.System.Name = "Airlock";
        }

        public List<string> Log => Rig.Rig.Log;

        public SequenceRunner State => Runner.Get(Cycle, Cycle.System.First);

        public string Name(Fitting fitting) => Program.Declare(Rig.World, Rig.Entity(fitting.Id)).Name;

        public void Start()
        {
            Program.Edited();
            Runner = new AutomationRunner(Program, Rig.World);
            Tick(3);
            Runner.Call(Cycle, Cycle.System.First);
        }

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++_frame);
            }
        }

        public Comparison Closed(Door door, bool closed)
        {
            var test = Test("status", "ext", door, Literal.Name("closed"));
            test.Relation = closed ? Relation.Is : Relation.IsNot;
            return test;
        }

        public Comparison Test(string member, string provider, Fitting block, Literal value)
        {
            var test = new Comparison { MemberId = member, Provider = provider, Quantifier = Quantifier.All, Value = value };
            test.Targets.Add(Name(block));
            return test;
        }

        public Act Do(string action, Fitting block)
        {
            var act = new Act(action);
            act.Targets.Add(Name(block));
            return Given.Defaults(act);
        }

        public void Add(params Step[] steps)
        {
            foreach (var step in steps)
                Cycle.Steps.Add(step);
        }
    }

    /// <summary>A test that holds runs its branch and passes over the else.</summary>
    [Fact]
    public void ATestThatHoldsRunsItsBranch()
    {
        var a = new Airlock();
        a.Add(new If(a.Closed(a.Outer, true)), a.Do("open", a.Inner), new Else(null!), a.Do("depressurize", a.Vent), new EndIf(), new Await());

        a.Start();
        a.Tick(5);

        Assert.Contains("Inner Door:open", a.Log);
        Assert.DoesNotContain("Airlock Vent:depressurize", a.Log);
    }

    /// <summary>A test that does not hold runs the else.</summary>
    [Fact]
    public void ATestThatFailsRunsTheElse()
    {
        var a = new Airlock();
        a.Outer.ForceOpen();
        a.Add(new If(a.Closed(a.Outer, true)), a.Do("open", a.Inner), new Else(null!), a.Do("depressurize", a.Vent), new EndIf(), new Await());

        a.Start();
        a.Tick(5);

        Assert.DoesNotContain("Inner Door:open", a.Log);
        Assert.Contains("Airlock Vent:depressurize", a.Log);
    }

    /// <summary>An else if is tested in turn where nothing before it held, and the first that holds is the one that runs.</summary>
    [Fact]
    public void AnElseIfIsTestedInTurn()
    {
        var a = new Airlock();
        a.Add(
            new If(a.Closed(a.Outer, false)), a.Do("open", a.Outer),
            new Else(a.Closed(a.Inner, true)), a.Do("open", a.Inner),
            new Else(null!), a.Do("depressurize", a.Vent),
            new EndIf(),
            new Await());

        a.Start();
        a.Tick(5);

        Assert.DoesNotContain("Outer Door:open", a.Log);
        Assert.Contains("Inner Door:open", a.Log);
        Assert.DoesNotContain("Airlock Vent:depressurize", a.Log);
    }

    /// <summary>The run carries on after the whole if, whichever branch ran, and where none did.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void TheRunCarriesOnAfterTheIf(bool closed)
    {
        var a = new Airlock();
        if (!closed)
            a.Outer.ForceOpen();

        a.Add(new If(a.Closed(a.Outer, true)), a.Do("open", a.Inner), new EndIf(), new Await(), a.Do("depressurize", a.Vent), new Await());

        a.Start();
        a.Tick(300);

        Assert.Equal(closed, a.Log.Contains("Inner Door:open"));
        Assert.Contains("Airlock Vent:depressurize", a.Log);
    }

    /// <summary>An if inside a branch is tested only where its branch runs.</summary>
    [Fact]
    public void AnIfInsideABranchIsTestedWhereItsBranchRuns()
    {
        var a = new Airlock();
        a.Add(
            new If(a.Closed(a.Outer, true)),
            new If(a.Closed(a.Inner, true)), a.Do("open", a.Inner), new EndIf(),
            new Else(null!), a.Do("open", a.Outer),
            new EndIf(),
            new Await(),
            a.Do("depressurize", a.Vent),
            new Await());

        a.Start();
        a.Tick(300);

        Assert.Contains("Inner Door:open", a.Log);
        Assert.DoesNotContain("Outer Door:open", a.Log);
        Assert.Contains("Airlock Vent:depressurize", a.Log);
    }

    /// <summary>A switched-off if takes its branches with it, and the run goes on after it.</summary>
    [Fact]
    public void ASwitchedOffIfIsPassedOverWhole()
    {
        var a = new Airlock();
        var opens = new If(a.Closed(a.Outer, true)) { IsOff = true };
        a.Add(opens, a.Do("open", a.Inner), new Else(null!), a.Do("open", a.Outer), new EndIf(), new Await(), a.Do("depressurize", a.Vent), new Await());

        a.Start();
        a.Tick(300);

        Assert.DoesNotContain("Inner Door:open", a.Log);
        Assert.DoesNotContain("Outer Door:open", a.Log);
        Assert.Contains("Airlock Vent:depressurize", a.Log);
    }

    /// <summary>A value is compared as the block holds it, with any taking one block and all needing every one.</summary>
    [Theory]
    [InlineData(false, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, false)]
    [InlineData(false, false, true)]
    public void AValueIsComparedAsTheBlockHoldsIt(bool on, bool all, bool runs)
    {
        var a = new Airlock();
        a.Light.Enabled = on;
        var test = a.Test("OnOff", "", a.Light, Literal.Of(false));
        test.Quantifier = all ? Quantifier.All : Quantifier.Any;
        a.Add(new If(test), a.Do("open", a.Inner), new EndIf(), new Await());

        a.Start();
        a.Tick(5);

        Assert.Equal(runs, a.Log.Contains("Inner Door:open"));
    }

    /// <summary>A block a test reads that is gone ends the run at the test, and says which.</summary>
    [Fact]
    public void ABlockATestReadsThatIsGoneEndsTheRun()
    {
        var a = new Airlock();
        a.Add(new If(a.Closed(a.Outer, true)), a.Do("open", a.Inner), new EndIf(), new Await());
        a.Program.Edited();
        a.Runner = new AutomationRunner(a.Program, a.Rig.World);
        a.Tick(3);
        a.Rig.Grind(a.Outer);
        a.Tick();

        a.Runner.Call(a.Cycle, a.Cycle.System.First, true);
        a.Tick(5);

        Assert.DoesNotContain("Inner Door:open", a.Log);
        Assert.Equal(SequenceProblem.MissingGate, a.State.Fault.Problem);
        Assert.Equal(0, a.State.Fault.Step);
    }

    /// <summary>An average over no blocks has no number to compare, so the run ends at the test and says why.</summary>
    [Fact]
    public void AnAverageOfNoBlocksEndsTheRun()
    {
        var a = new Airlock();
        var test = new Comparison { MemberId = "OnOff", Provider = "", Quantifier = Quantifier.Average, Relation = Relation.Below, Value = Literal.Of(1d) };
        a.Add(new If(test), a.Do("open", a.Inner), new EndIf(), new Await());

        a.Start();
        a.Tick(5);

        Assert.DoesNotContain("Inner Door:open", a.Log);
        Assert.Equal(SequenceProblem.NothingToAverage, a.State.Fault.Problem);
        Assert.Equal(0, a.State.Fault.Step);
    }

    /// <summary>A sum over no blocks is 0, and is compared as 0.</summary>
    [Fact]
    public void ASumOfNoBlocksIsZero()
    {
        var a = new Airlock();
        var test = new Comparison { MemberId = "OnOff", Provider = "", Quantifier = Quantifier.Sum, Relation = Relation.Below, Value = Literal.Of(1d) };
        a.Add(new If(test), a.Do("open", a.Inner), new EndIf(), new Await());

        a.Start();
        a.Tick(5);

        Assert.Contains("Inner Door:open", a.Log);
    }

    /// <summary>A step that takes time, at the end of a branch, is waited for by an await after the if.</summary>
    [Fact]
    public void AnAwaitAfterTheIfWaitsForWhatItsBranchStarted()
    {
        var a = new Airlock();
        a.Add(new If(a.Closed(a.Outer, true)), a.Do("open", a.Inner), new EndIf(), new Await());
        a.Program.Edited();
        a.Runner = new AutomationRunner(a.Program, a.Rig.World);
        a.Runner.Tick(1);

        var fault = SequenceSummary.Checked(a.Rig.World, a.Runner.Bindings, a.Cycle, a.Cycle.System.First);

        Assert.False(fault.Parks);
    }

    /// <summary>A test of a member its blocks do not have is a fault, found before anything runs.</summary>
    [Fact]
    public void ATestOfAMemberTheBlocksLackIsAFault()
    {
        var a = new Airlock();
        a.Add(new If(a.Test("pressurized", "ext", a.Outer, Literal.Of(true))), a.Do("open", a.Inner), new EndIf(), new Await());
        a.Program.Edited();
        a.Runner = new AutomationRunner(a.Program, a.Rig.World);
        a.Runner.Tick(1);

        var fault = SequenceSummary.Checked(a.Rig.World, a.Runner.Bindings, a.Cycle, a.Cycle.System.First);

        Assert.Equal(SequenceProblem.Unoffered, fault.Problem);
        Assert.Equal(0, fault.Step);
    }

    /// <summary>A run held inside a branch carries on in that branch after a reload, and then after the if.</summary>
    [Fact]
    public void ARunHeldInsideABranchCarriesOnAfterAReload()
    {
        var a = new Airlock();
        a.Add(
            new If(a.Closed(a.Outer, true)), a.Do("close", a.Inner), new Await(), a.Do("depressurize", a.Vent), new Await(),
            new EndIf(),
            a.Do("open", a.Outer),
            new Await());

        a.Start();
        a.Tick(20);
        Assert.Contains("Airlock Vent:depressurize", a.Log);
        Assert.DoesNotContain("Outer Door:open", a.Log);

        var binary = new ProtoBinary();
        var memory = binary.Pack(a.Program.Memory);
        var program = new Automation { Memory = binary.Unpack<ConstructMemory>(memory) };
        Assert.Equal("", ProgramReader.Load(ProgramWriter.Save(a.Program, a.Rig.Values), program, a.Rig.Values));
        a.Runner = new AutomationRunner(program, a.Rig.World);
        a.Log.Clear();
        a.Tick(600);

        Assert.Contains("Outer Door:open", a.Log);
    }
}
