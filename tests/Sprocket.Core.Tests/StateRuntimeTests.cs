using Sprocket.Host.SpaceEngineers.Features.States.Edits;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A logic system's states as its installations run: set by steps, tested by ifs, and watched by whens.</summary>
public class StateRuntimeTests
{
    const string Version = "sprocket 1 alpha 18\n\n";
    const string Header = "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Door\n{\n";

    static readonly Values Store = new Values();

    /// <summary>A program read from <paramref name="text" />, running in a world of its own, on its first tick.</summary>
    static (Automation Program, AutomationRunner Runner) Running(string text)
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var runner = new AutomationRunner(program, new Rigged().World);
        runner.Tick(1);
        return (program, runner);
    }

    static Literal Held(AutomationRunner runner, AutomationSystem system, SystemInstance instance, string state) =>
        runner.States.Read(system, instance, system.FindState(state)!);

    /// <summary>A step that sets a state starts the when on it that came to hold, at once, and not for the value it starts at.</summary>
    [Fact]
    public void AWhenFiresOnASetAndNotOnTheStart()
    {
        var (program, runner) = Running(Version + Header
            + "    type side = inside | outside\n\n"
            + "    let where = inside\n"
            + "    let hits = off\n\n"
            + "    when where == outside then run 2\n\n"
            + "    sequence 1 {\n        set where to outside\n    }\n\n"
            + "    sequence 2 {\n        set hits to on\n    }\n"
            + "}\n");
        var system = program.Systems[0];
        var self = system.Uses[0];

        runner.Tick(2);
        Assert.Equal(Literal.Of(false), Held(runner, system, self, "hits"));

        runner.Call(system.FindSequence(1)!, self);

        Assert.Equal(Literal.Name("outside"), Held(runner, system, self, "where"));
        Assert.Equal(Literal.Of(true), Held(runner, system, self, "hits"));
        Assert.Equal(1, runner.Get(system.FindSequence(2)!, self)!.FiredCount);
    }

    /// <summary>A when that starts out holding does not fire, and a set to the value already held starts nothing.</summary>
    [Fact]
    public void NeitherTheStartNorTheSameValueFires()
    {
        var (program, runner) = Running(Version + Header
            + "    type side = inside | outside\n\n"
            + "    let where = outside\n\n"
            + "    when where == outside then run 2\n\n"
            + "    sequence 1 {\n        set where to outside\n    }\n\n"
            + "    sequence 2 {\n        wait 1t\n    }\n"
            + "}\n");
        var system = program.Systems[0];
        var self = system.Uses[0];

        runner.Call(system.FindSequence(1)!, self);
        runner.Tick(3);
        runner.Call(system.FindSequence(1)!, self);

        Assert.Equal(0, runner.Get(system.FindSequence(2)!, self)!.FiredCount);
    }

    /// <summary>An if reads the state, and a state is set from another.</summary>
    [Fact]
    public void AnIfReadsTheStateAndAStateTakesAnothers()
    {
        var (program, runner) = Running(Version + Header
            + "    type side = inside | outside\n\n"
            + "    let where = inside\n"
            + "    let home = outside\n"
            + "    let hits = off\n\n"
            + "    sequence 1 {\n"
            + "        if where == home then {\n"
            + "            set hits to off\n"
            + "        }\n"
            + "        else {\n"
            + "            set hits to on\n"
            + "            set where to home\n"
            + "        }\n"
            + "    }\n"
            + "}\n");
        var system = program.Systems[0];
        var self = system.Uses[0];

        runner.Call(system.FindSequence(1)!, self);
        Assert.Equal(Literal.Of(true), Held(runner, system, self, "hits"));
        Assert.Equal(Literal.Name("outside"), Held(runner, system, self, "where"));

        runner.Call(system.FindSequence(1)!, self);
        Assert.Equal(Literal.Of(false), Held(runner, system, self, "hits"));
    }

    /// <summary>Each installation holds its own values.</summary>
    [Fact]
    public void TwoInstallationsKeepTheirOwnValues()
    {
        var (program, runner) = Running(Version
            + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock\n{\n"
            + "    let cycling = off\n\n"
            + "    sequence 1 {\n        set cycling to on\n    }\n"
            + "}\n\n"
            + "instance 1 of MalTek.Airlock {\n}\n\n"
            + "instance 2 of MalTek.Airlock {\n}\n");
        var system = program.Systems[0];
        var first = system.Instances[0];
        var second = system.Instances[1];

        runner.Call(system.FindSequence(1)!, first);

        Assert.Equal(Literal.Of(true), Held(runner, system, first, "cycling"));
        Assert.Equal(Literal.Of(false), Held(runner, system, second, "cycling"));
    }

    /// <summary>A when that starts the sequence already running, by a set inside it, lets it run on rather than start it again.</summary>
    [Fact]
    public void ARunningSequenceIsNotStartedAgain()
    {
        var (program, runner) = Running(Version + Header
            + "    type side = inside | outside\n\n"
            + "    let where = inside\n\n"
            + "    when where == outside then run 1\n\n"
            + "    sequence 1 {\n        set where to outside\n        set where to inside\n    }\n"
            + "}\n");
        var system = program.Systems[0];
        var self = system.Uses[0];
        var cycle = runner.Get(system.FindSequence(1)!, self)!;

        runner.Call(system.FindSequence(1)!, self);

        Assert.Equal(1, cycle.FiredCount);
        Assert.Equal(1, cycle.SkippedCount);
        Assert.Equal(Literal.Name("inside"), Held(runner, system, self, "where"));
    }

    /// <summary>A renamed state keeps the value it holds, and one whose type changed starts over from its starting value.</summary>
    [Fact]
    public void AnEditKeepsValuesWhereItCan()
    {
        var (program, runner) = Running(Version + Header
            + "    type side = inside | outside\n\n"
            + "    let where = inside\n"
            + "    let cycling = off\n\n"
            + "    sequence 1 {\n        set where to outside\n        set cycling to on\n    }\n"
            + "}\n");
        var system = program.Systems[0];
        var self = system.Uses[0];
        runner.Call(system.FindSequence(1)!, self);
        system.FindSequence(1)!.Steps.Clear();

        new RenameState(system.FindState("where")!, "place").Apply(system);
        Assert.Equal(Literal.Name("outside"), Held(runner, system, self, "place"));

        new ChangeStart(system, system.FindState("cycling")!, Literal.Name("inside"), "side").Apply(system);
        Assert.Equal(Literal.Name("inside"), Held(runner, system, self, "cycling"));
    }

    const string Saved = Version + Header
        + "    type side = inside | outside\n\n"
        + "    let where = inside\n"
        + "    let hits = off\n\n"
        + "    when where == outside then run 2\n\n"
        + "    sequence 1 {\n        set where to outside\n    }\n\n"
        + "    sequence 2 {\n        set hits to on\n    }\n"
        + "}\n";

    /// <summary>What <paramref name="program" /> holds is written as the world saves and read back into <paramref name="text" />, as a world loads it.</summary>
    static (Automation Program, AutomationRunner Runner) Reloaded(Automation program, AutomationRunner runner, string text)
    {
        program.Memory.Keeper = 42;
        runner.States.Settle();
        var packed = new Memories(new ProtoBinary()).Write(100, new List<ConstructMemory> { program.Memory });

        var loaded = new Memories(new ProtoBinary());
        loaded.Read(packed, 100);
        var again = new Automation { Memory = loaded.Claim(42) };
        Assert.Equal("", ProgramReader.Load(text, again, Store));
        var rerun = new AutomationRunner(again, new Rigged().World);
        rerun.Tick(101);
        return (again, rerun);
    }

    /// <summary>A value set is kept as the world saves and holds again as it loads, starting nothing as it does.</summary>
    [Fact]
    public void AValueIsKeptWithTheWorld()
    {
        var (program, runner) = Running(Saved);
        var system = program.Systems[0];
        runner.Call(system.FindSequence(1)!, system.Uses[0]);
        runner.States.Write(system, system.Uses[0], system.FindState("hits")!, Literal.Of(false));

        var (again, rerun) = Reloaded(program, runner, Saved);
        var restored = again.Systems[0];
        Assert.Equal(Literal.Name("outside"), Held(rerun, restored, restored.Uses[0], "where"));
        Assert.Equal(Literal.Of(false), Held(rerun, restored, restored.Uses[0], "hits"));
    }

    /// <summary>A state renamed since its value was set is saved under its new name.</summary>
    [Fact]
    public void ARenameReachesWhatIsSaved()
    {
        var (program, runner) = Running(Saved);
        var system = program.Systems[0];
        runner.Call(system.FindSequence(1)!, system.Uses[0]);
        new RenameState(system.FindState("where")!, "place").Apply(system);

        var (again, rerun) = Reloaded(program, runner, ProgramWriter.Save(program, Store));
        var restored = again.Systems[0];
        Assert.Equal(Literal.Name("outside"), Held(rerun, restored, restored.Uses[0], "place"));
    }

    /// <summary>A value its state can no longer hold, the state retyped since it was saved, is dropped, and the state starts over.</summary>
    [Fact]
    public void AValueTheStateCanNoLongerHoldIsDropped()
    {
        var (program, runner) = Running(Saved);
        var system = program.Systems[0];
        runner.Call(system.FindSequence(1)!, system.Uses[0]);

        var retyped = Saved.Replace("let where = inside", "let where = off").Replace("set where to outside", "set where to on")
            .Replace("when where == outside", "when where == on");
        var (again, rerun) = Reloaded(program, runner, retyped);
        var restored = again.Systems[0];

        Assert.Equal(Literal.Of(false), Held(rerun, restored, restored.Uses[0], "where"));
        Assert.DoesNotContain(again.Memory.States, kept => kept.State == "where");
    }

    /// <summary>
    /// A logic system made a blueprint goes on as its first installation, holding what it held: whether its value was set
    /// this session, or is only in what the world saved while it was plain.
    /// </summary>
    [Fact]
    public void TheFirstInstallationHoldsWhatAPlainSystemHeld()
    {
        var (program, runner) = Running(Saved);
        var system = program.Systems[0];
        var self = system.Uses[0];
        runner.Call(system.FindSequence(1)!, self);
        var (again, rerun) = Reloaded(program, runner, Saved);

        var first = system.AddInstance();
        runner.States.Move(system, self, first);
        Assert.Equal(Literal.Name("outside"), Held(runner, system, first, "where"));
        Assert.Equal(Literal.Name("inside"), Held(runner, system, self, "where"));

        var restored = again.Systems[0];
        var plain = restored.Uses[0];
        var made = restored.AddInstance();
        rerun.States.Move(restored, plain, made);
        Assert.Equal(Literal.Name("outside"), Held(rerun, restored, made, "where"));
        Assert.All(again.Memory.States, kept => Assert.Equal(made.Number, kept.Instance));
    }

    /// <summary>A when on a state starts nothing while its system is switched off.</summary>
    [Fact]
    public void ASwitchedOffSystemSetsNothingByAWhen()
    {
        var (program, runner) = Running(Version + Header
            + "    let cycling = off\n"
            + "    let alarm = off\n\n"
            + "    when cycling == on then run 2\n\n"
            + "    sequence 1 {\n        set cycling to on\n    }\n\n"
            + "    sequence 2 {\n        set alarm to on\n    }\n"
            + "}\n");
        var system = program.Systems[0];
        var self = system.Uses[0];
        system.Enabled = false;

        runner.Set(system, self, (SetState)system.FindSequence(1)!.Steps[0]);

        Assert.Equal(Literal.Of(true), Held(runner, system, self, "cycling"));
        Assert.Equal(Literal.Of(false), Held(runner, system, self, "alarm"));
    }
}
