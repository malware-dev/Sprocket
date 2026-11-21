using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Door = Sprocket.Sim.Machine.Door;
using Vent = Sprocket.Sim.Machine.Vent;
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
            var test = Test("status", "sprocket", door, Literal.Name("closed"));
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

    /// <summary>A test that holds runs its branch and passes over the else; one that does not hold runs the else.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ATestRunsItsBranchWhereItHoldsAndTheElseWhereItFails(bool holds)
    {
        var a = new Airlock();
        if (!holds)
            a.Outer.ForceOpen();

        a.Add(new If(a.Closed(a.Outer, true)), a.Do("open", a.Inner), new Else(null!), a.Do("depressurize", a.Vent), new EndIf(), new Await());

        a.Start();
        a.Tick(5);

        if (holds)
        {
            Assert.Contains("Inner Door:open", a.Log);
            Assert.DoesNotContain("Airlock Vent:depressurize", a.Log);
        }
        else
        {
            Assert.DoesNotContain("Inner Door:open", a.Log);
            Assert.Contains("Airlock Vent:depressurize", a.Log);
        }
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

    /// <summary>An average over no blocks has no number to compare, so the run ends at the test and says why; a sum over no blocks is 0, and is compared as 0.</summary>
    [Theory]
    [InlineData(Quantifier.Average)]
    [InlineData(Quantifier.Sum)]
    public void ASumOfNoBlocksIsZeroAndAnAverageOfNoBlocksEndsTheRun(Quantifier quantifier)
    {
        var a = new Airlock();
        var test = new Comparison { MemberId = "OnOff", Provider = "", Quantifier = quantifier, Relation = Relation.Below, Value = Literal.Of(1d) };
        a.Add(new If(test), a.Do("open", a.Inner), new EndIf(), new Await());

        a.Start();
        a.Tick(5);

        if (quantifier == Quantifier.Sum)
        {
            Assert.Contains("Inner Door:open", a.Log);
            return;
        }

        Assert.DoesNotContain("Inner Door:open", a.Log);
        Assert.Equal(SequenceProblem.NothingToAverage, a.State.Fault.Problem);
        Assert.Equal(0, a.State.Fault.Step);
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

    /// <summary>A task in a namespace no mod has registered is not offered, found before anything runs, as any missing member is.</summary>
    [Fact]
    public void ATaskInANamespaceNoModRegisteredIsNotOffered()
    {
        var a = new Airlock();
        var lift = a.Do("lift", a.Inner);
        lift.Provider = "malforge.hover_repulsors";
        a.Add(lift, new Await());
        a.Program.Edited();
        a.Runner = new AutomationRunner(a.Program, a.Rig.World);
        a.Runner.Tick(1);

        var fault = SequenceSummary.Checked(a.Rig.World, a.Runner.Bindings, a.Cycle, a.Cycle.System.First);

        Assert.Equal(SequenceProblem.Unoffered, fault.Problem);
        Assert.Equal(0, fault.Step);
    }

    /// <summary>
    /// A task in a namespace the host lacks, in a branch taken only while the host has that namespace, is no fault: in the
    /// branch of an if testing it is installed, in the branch after one testing it is not, and in an if inside either.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ATaskInABranchTakenOnlyWhileItsNamespaceIsInstalledIsNoFault(bool installed)
    {
        var a = new Airlock();
        var lift = a.Do("lift", a.Inner);
        lift.Provider = "malforge.hover_repulsors";
        var guard = new HostTest { Namespace = "malforge.hover_repulsors", Installed = installed };
        if (installed)
            a.Add(new If(guard), new If(a.Closed(a.Outer, true)), lift, new EndIf(), new EndIf());
        else
            a.Add(new If(guard), a.Do("open", a.Outer), new Else(null!), new If(a.Closed(a.Outer, true)), lift, new EndIf(), new EndIf());

        a.Program.Edited();
        a.Runner = new AutomationRunner(a.Program, a.Rig.World);
        a.Runner.Tick(1);

        var fault = SequenceSummary.Checked(a.Rig.World, a.Runner.Bindings, a.Cycle, a.Cycle.System.First);

        Assert.NotEqual(SequenceProblem.Unoffered, fault.Problem);
    }

    /// <summary>
    /// A task in a namespace the host lacks is not offered outside the branch its namespace is tested in, after the if and in
    /// the branch taken where it is not installed, and in another namespace's branch.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void ATaskOutsideItsNamespacesBranchIsNotOffered(int where)
    {
        var a = new Airlock();
        var lift = a.Do("lift", a.Inner);
        lift.Provider = "malforge.hover_repulsors";
        var guard = new HostTest { Namespace = where == 2 ? "malforge.other" : "malforge.hover_repulsors" };
        if (where == 0)
            a.Add(new If(guard), a.Do("open", a.Outer), new EndIf(), lift);
        else if (where == 1)
            a.Add(new If(guard), a.Do("open", a.Outer), new Else(null!), lift, new EndIf());
        else
            a.Add(new If(guard), lift, new EndIf());

        a.Program.Edited();
        a.Runner = new AutomationRunner(a.Program, a.Rig.World);
        a.Runner.Tick(1);

        var fault = SequenceSummary.Checked(a.Rig.World, a.Runner.Bindings, a.Cycle, a.Cycle.System.First);

        Assert.Equal(SequenceProblem.Unoffered, fault.Problem);
        Assert.Equal(a.Cycle.Steps.IndexOf(lift), fault.Step);
    }

    /// <summary>A test of the host runs the branch it holds in, as the host is when the run reaches it, and the else where it does not.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AHostTestRunsItsBranchWhereTheHostHasTheNamespace(bool registered)
    {
        try
        {
            if (registered)
                Assert.True(HostNamespaces.Register("malforge.hover_repulsors", "Hover Repulsors", "Hover Repulsors", "Malforge"));

            var a = new Airlock();
            a.Add(new If(new HostTest { Namespace = "malforge.hover_repulsors" }), a.Do("open", a.Inner), new Else(null!), a.Do("open", a.Outer),
                new EndIf());

            a.Start();
            a.Tick(5);

            Assert.Equal(registered, a.Log.Contains("Inner Door:open"));
            Assert.Equal(!registered, a.Log.Contains("Outer Door:open"));
        }
        finally
        {
            HostNamespaces.Clear();
        }
    }

    /// <summary>A trigger watching a member or a signal in a namespace the host lacks never fires, and is no fault: it waits for the namespace.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ATriggerInANamespaceTheHostLacksIsNoFault(bool signal)
    {
        var a = new Airlock();
        Trigger trigger;
        if (signal)
        {
            var written = SignalTrigger.Written("malforge.hover_repulsors", "landed");
            written.Sources.Add(a.Name(a.Inner));
            trigger = written;
        }
        else
        {
            var when = new WhenTrigger("hovering") { Provider = "malforge.hover_repulsors", Value = Literal.Of(true) };
            when.Sources.Add(a.Name(a.Inner));
            trigger = when;
        }

        var watched = a.Program.AddSequence(a.Cycle.System, trigger);
        watched.Steps.Add(a.Do("open", a.Outer));
        a.Program.Edited();
        a.Runner = new AutomationRunner(a.Program, a.Rig.World);
        a.Runner.Tick(1);

        var fault = SequenceSummary.Checked(a.Rig.World, a.Runner.Bindings, watched, watched.System.First);

        Assert.False(fault.Exists, $"{fault.Problem} {fault.Unwired}");
    }

    /// <summary>
    /// The host gaining or losing a namespace checks the program again with no reload: a task guarded by its namespace and a
    /// trigger in it wait while the host lacks it, and are checked as any other once the host has it.
    /// </summary>
    /// <remarks>No mod adds members yet, so once checked, both are found wanting; that they are checked at all is what is shown.</remarks>
    [Fact]
    public void GainingOrLosingANamespaceChecksTheProgramAgain()
    {
        try
        {
            var a = new Airlock();
            var lift = a.Do("lift", a.Inner);
            lift.Provider = "malforge.hover_repulsors";
            a.Add(new If(new HostTest { Namespace = "malforge.hover_repulsors" }), lift, new EndIf());

            var when = new WhenTrigger("hovering") { Provider = "malforge.hover_repulsors", Value = Literal.Of(true) };
            when.Sources.Add(a.Name(a.Inner));
            var watched = a.Program.AddSequence(a.Cycle.System, when);
            watched.Steps.Add(a.Do("open", a.Outer));

            a.Program.Edited();
            a.Runner = new AutomationRunner(a.Program, a.Rig.World);
            a.Runner.Tick(1);
            var self = a.Cycle.System.First;
            Assert.NotEqual(SequenceProblem.Unoffered, SequenceSummary.Fault(a.Runner.Get(a.Cycle, self)).Problem);
            Assert.False(SequenceSummary.Fault(a.Runner.Get(watched, self)).Exists);
            var checks = a.Runner.Checks;

            Assert.True(HostNamespaces.Register("malforge.hover_repulsors", "Hover Repulsors", "Hover Repulsors", "Malforge"));
            a.Runner.Tick(2);

            Assert.Equal(checks + 1, a.Runner.Checks);
            Assert.Equal(SequenceProblem.Unoffered, SequenceSummary.Fault(a.Runner.Get(a.Cycle, self)).Problem);
            Assert.Equal(Unwired.NoMoment, SequenceSummary.Fault(a.Runner.Get(watched, self)).Unwired);

            a.Runner.Tick(3);
            Assert.Equal(checks + 1, a.Runner.Checks);
        }
        finally
        {
            HostNamespaces.Clear();
        }
    }

    /// <summary>
    /// A sequence with a step in a namespace the host lacks waits for it, as one with an empty slot does: its system stays
    /// on, it says what it waits for, a call runs none of it, and its clock is not watched; once the host has the namespace it
    /// is checked again with no reload.
    /// </summary>
    /// <remarks>No mod adds members yet, so once checked the step is found unoffered; that it is checked again is what is shown.</remarks>
    [Fact]
    public void ASequenceWaitsForANamespaceTheHostLacks()
    {
        try
        {
            var a = new Airlock();
            var lift = a.Do("lift", a.Inner);
            lift.Provider = "malforge.hover_repulsors";
            a.Add(a.Do("open", a.Outer), lift);
            var clock = new EveryTrigger(60);
            clock.Does = new Run(a.Cycle.Number);
            a.Cycle.System.AddTrigger(clock);

            a.Start();
            a.Tick(5);
            var self = a.Cycle.System.First;
            var fault = SequenceSummary.Fault(a.State);

            Assert.True(fault.Waits);
            Assert.Equal(SequenceProblem.Unoffered, fault.Problem);
            Assert.Equal(1, fault.Step);
            Assert.False(fault.Parks);
            Assert.True(a.Cycle.System.Enabled);
            Assert.DoesNotContain("Outer Door:open", a.Log);
            Assert.True(a.State.RefusedCount > 0);
            Assert.Null(a.Runner.Watches.Poller(clock, self));
            Assert.Equal("This step needs a mod that isn't installed: malforge.hover_repulsors. The sequence waits for it.",
                SequenceText.Problem(new Texts(), fault, a.Cycle));

            // The refusal says why in the run log, with a reason of its own.
            var lines = new List<string>();
            a.Runner.Log = new RunLog(lines.Add, () => "Hover Pad");
            a.Runner.Call(a.Cycle, self);
            Assert.Contains(lines, line => line.EndsWith("did not start: a step needs a mod that is not installed"));
            a.Runner.Log = null;

            var checks = a.Runner.Checks;
            Assert.True(HostNamespaces.Register("malforge.hover_repulsors", "Hover Repulsors", "Hover Repulsors", "Malforge"));
            a.Tick();

            Assert.Equal(checks + 1, a.Runner.Checks);
            Assert.False(SequenceSummary.Fault(a.State).Waits);
            Assert.Equal(SequenceProblem.Unoffered, SequenceSummary.Fault(a.State).Problem);
        }
        finally
        {
            HostNamespaces.Clear();
        }
    }

    /// <summary>A test of a member its blocks do not have is a fault, found before anything runs.</summary>
    [Fact]
    public void ATestOfAMemberTheBlocksLackIsAFault()
    {
        var a = new Airlock();
        a.Add(new If(a.Test("pressurized", "sprocket", a.Outer, Literal.Of(true))), a.Do("open", a.Inner), new EndIf(), new Await());
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
