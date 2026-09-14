using Reactor.Sequences;
using Reactor.Storage;
using Reactor.World;
using Xunit;

namespace Reactor.Tests;

/// <summary>
/// A program written out and read back must be the same program.
/// </summary>
/// <remarks>
/// The same round-trip discipline the edits get, and for a sharper reason: a save format that
/// quietly drops a field loses somebody's work permanently, and it does so on the one path
/// nobody exercises until they reload. Adding anything to the model without adding it here is
/// how that happens.
/// </remarks>
public class ProgramTextTests
{
    static readonly Values Store = new Values();

    static string Save(Automation program) => ProgramText.Save(program, Store);

    /// <summary>Loads, and refuses anything this file writes that does not read back cleanly.</summary>
    static void Load(string text, Automation program)
    {
        Assert.Equal("", ProgramText.Load(text, program, Store));
    }

    /// <summary>A declaration with the keys a world would have minted for it.</summary>
    static EntityRef Declared(string name, string kindId, Values values, long id, string blockName)
    {
        var declared = new EntityRef(name, kindId);
        declared.Keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(id)));
        declared.Keys.Add(new EntityKey(ValueKind.Text, values.Text(blockName)));
        return declared;
    }

    static Automation Build()
    {
        var program = new Automation();

        // Declared by hand, because this test has no world to mint keys from. The names are what
        // everything below points at, exactly as they would be after the editor made them.
        var values = new Values();
        program.Restore(Declared("inner", "door", values, 1, "Inner Door"));
        program.Restore(Declared("outer", "door", values, 2, "Outer Door"));
        program.Restore(Declared("vent", "vent", values, 3, "Airlock Vent"));
        program.Restore(Declared("call", "button", values, 4, "Airlock Button"));
        program.Restore(Declared("spare", "button", values, 5, "Spare Button"));

        var airlock = program.AddSystem();
        airlock.Namespace = "MalTek";
        airlock.Name = "Airlock";
        airlock.Annotation = "Cycles the front airlock";
        airlock.AddInstance().Name = "Port";

        var when = new WhenTrigger("pressed");
        when.Sources.Add("call");
        when.Sources.Add("spare");
        when.RequiresAll = true;

        var out1 = program.AddSequence(airlock, when);
        out1.Name = "Let me out";
        out1.Enabled = false;
        out1.Annotation = "Inside to outside.";

        var close = new Step("close");
        close.Targets.Add("inner");
        close.WaitsForCompletion = true;
        close.Annotation = "the vent needs the full sixty seconds";
        out1.Steps.Add(close);

        var pause = new Step("");
        pause.Gate = new AfterGate(90);
        out1.Steps.Add(pause);

        var vent = new Step("depressurize");
        vent.Targets.Add("vent");
        var only = new Condition("is-shut");
        only.Entities.Add("inner");
        vent.Gate = new OnlyIfGate(only);
        out1.Steps.Add(vent);

        // What the author wrote about a part, and a second one of the system that fills the
        // parts with its own doors.
        var inner = airlock.RoleFor("inner");
        inner.Name = "the door onto the ship";
        inner.Instructions = "The one you walk through to get inside.";
        inner.Optional = false;

        var spare = airlock.RoleFor("vent");
        spare.Name = "the vent";
        spare.Optional = true;

        var port = airlock.Instances[1];
        port.Enabled = false;
        port.Fill("inner", new[] { "outer" });
        port.Fill("vent", new[] { "call", "spare" });

        var rig = program.AddSystem();
        rig.Name = "Drill Rig";
        rig.Enabled = false;

        var timed = program.AddSequence(rig, new EveryTrigger(600));
        timed.Name = "Top up";

        var lower = new Step("open");
        lower.Targets.Add("outer");
        var once = new Condition("is-low");
        once.Entities.Add("inner");
        lower.Gate = new OnceGate(once);
        timed.Steps.Add(lower);

        return program;
    }

    [Fact]
    public void AProgramSurvivesBeingWrittenAndReadBack()
    {
        var before = Build();
        var text = Save(before);

        var after = new Automation();
        Load(text, after);

        Assert.Equal(text, Save(after));
    }

    [Fact]
    public void IdentityIsKept_BecauseButtonsNameWhatTheyRun()
    {
        var before = Build();
        var after = new Automation();
        Load(Save(before), after);

        // A system is its namespace and name, including one with no namespace at all.
        Assert.True(after.Systems[0].Is("MalTek", "Airlock"));
        Assert.True(after.Systems[1].Is("", "Drill Rig"));

        Assert.Equal(before.Sequences[0].Id, after.Sequences[0].Id);
        Assert.Equal(before.Sequences[1].Id, after.Sequences[1].Id);

        // And the counters move past what was loaded, or the next thing made would collide
        // with something already there.
        var fresh = after.AddSequence(after.Systems[0], new CalledTrigger());
        Assert.True(fresh.Id > before.Sequences[1].Id);
    }

    [Fact]
    public void EverySavedDetailComesBack()
    {
        var after = new Automation();
        Load(Save(Build()), after);

        var airlock = after.Systems[0];
        Assert.Equal("Airlock", airlock.Name);
        Assert.Equal("Cycles the front airlock", airlock.Annotation);
        Assert.True(airlock.Enabled);
        Assert.Equal(2, airlock.Instances.Count);
        Assert.Equal("Port", airlock.Instances[1].Name);

        // One of them parked. The format could not say this at all until it became a language.
        Assert.True(airlock.Instances[0].Enabled);
        Assert.False(airlock.Instances[1].Enabled);

        // What the author wrote about the parts.
        Assert.Equal("the door onto the ship", airlock.FindRole("inner")!.Name);
        Assert.Equal("The one you walk through to get inside.", airlock.FindRole("inner")!.Instructions);
        Assert.False(airlock.FindRole("inner")!.Optional);
        Assert.True(airlock.FindRole("vent")!.Optional);

        // And which of its own things the second one uses for them, including a part filled
        // with more than one.
        var port = airlock.Instances[1];
        Assert.Equal(new[] { "outer" }, port.FilledWith("inner"));
        Assert.Equal(new[] { "call", "spare" }, port.FilledWith("vent"));
        Assert.Null(port.FilledWith("nothing-like-this"));

        Assert.False(after.Systems[1].Enabled);

        var letMeOut = after.Sequences[0];
        Assert.Equal("Let me out", letMeOut.Name);
        Assert.False(letMeOut.Enabled);

        var when = Assert.IsType<WhenTrigger>(letMeOut.Trigger);
        Assert.Equal("pressed", when.EventId);
        Assert.True(when.RequiresAll);
        Assert.Equal(new[] { "call", "spare" }, when.Sources);

        Assert.Equal(3, letMeOut.Steps.Count);
        Assert.Equal("close", letMeOut.Steps[0].ActionId);
        Assert.True(letMeOut.Steps[0].WaitsForCompletion);
        Assert.Equal(90, Assert.IsType<AfterGate>(letMeOut.Steps[1].Gate).Ticks);

        var only = Assert.IsType<OnlyIfGate>(letMeOut.Steps[2].Gate);
        Assert.Equal("is-shut", only.Condition.StateId);
        Assert.Equal(new[] { "inner" }, only.Condition.Entities);

        var timed = after.Sequences[1];
        Assert.Equal(600, Assert.IsType<EveryTrigger>(timed.Trigger).IntervalTicks);
        Assert.Equal("is-low", Assert.IsType<OnceGate>(timed.Steps[0].Gate).Condition.StateId);
    }

    /// <summary>
    /// Names are whatever somebody typed, and a backslash is an ordinary character.
    /// </summary>
    /// <remarks>
    /// Which is what quoting by doubling buys: no character changes the meaning of the next one, so a
    /// name holding one cannot come back as something else. A forgotten escape can only fail loudly.
    /// </remarks>
    [Fact]
    public void AwkwardNamesSurvive()
    {
        var program = new Automation();
        var system = program.AddSystem();
        system.Name = @"Air\lock ""2""";
        system.Annotation = "line one\nline two";

        var sequence = program.AddSequence(system, new CalledTrigger());
        sequence.Name = @"\\\";

        var after = new Automation();
        Load(Save(program), after);

        Assert.Equal(@"Air\lock ""2""", after.Systems[0].Name);
        Assert.Equal("line one\nline two", after.Systems[0].Annotation);
        Assert.Equal(@"\\\", after.Sequences[0].Name);
    }

    [Fact]
    public void NothingSavedLoadsAsNothing()
    {
        var after = new Automation();
        Load("", after);
        Load(null, after);

        Assert.Empty(after.Systems);
        Assert.Empty(after.Sequences);
    }
}
