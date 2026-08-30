using Reactor.Sequences;
using Reactor.Storage;
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
    static Automation Build()
    {
        var program = new Automation();

        var airlock = program.AddSystem();
        airlock.Name = "Airlock";
        airlock.Annotation = "Cycles the front airlock";
        airlock.AddInstance().Name = "Port";

        var when = new WhenTrigger("pressed");
        when.Sources.Add(4);
        when.Sources.Add(5);
        when.RequiresAll = true;

        var out1 = program.AddSequence(airlock, when);
        out1.Name = "Let me out";
        out1.Enabled = false;
        out1.Annotation = "Inside to outside.";

        var close = new Step("close");
        close.Targets.Add(1);
        close.WaitsForCompletion = true;
        close.Annotation = "the vent needs the full sixty seconds";
        out1.Steps.Add(close);

        var pause = new Step("");
        pause.Gate = new AfterGate(90);
        out1.Steps.Add(pause);

        var vent = new Step("depressurize");
        vent.Targets.Add(3);
        var only = new Condition("is-shut");
        only.Entities.Add(1);
        vent.Gate = new OnlyIfGate(only);
        out1.Steps.Add(vent);

        // What the author wrote about a part, and a second one of the system that fills the
        // parts with its own doors.
        var inner = airlock.RoleFor(1);
        inner.Name = "the door onto the ship";
        inner.Instructions = "The one you walk through to get inside.";
        inner.Optional = false;

        var spare = airlock.RoleFor(3);
        spare.Name = "the vent";
        spare.Optional = true;

        var port = airlock.Instances[1];
        port.Fill(1, new long[] { 21 });
        port.Fill(3, new long[] { 23, 24 });

        var rig = program.AddSystem();
        rig.Name = "Drill Rig";
        rig.Enabled = false;

        var timed = program.AddSequence(rig, new EveryTrigger(600));
        timed.Name = "Top up";

        var lower = new Step("open");
        lower.Targets.Add(2);
        var once = new Condition("is-low");
        once.Entities.Add(7);
        lower.Gate = new OnceGate(once);
        timed.Steps.Add(lower);

        return program;
    }

    [Fact]
    public void AProgramSurvivesBeingWrittenAndReadBack()
    {
        var before = Build();
        var text = ProgramText.Save(before);

        var after = new Automation();
        ProgramText.Load(text, after);

        Assert.Equal(text, ProgramText.Save(after));
    }

    [Fact]
    public void IdentityIsKept_BecauseSavedSequencesReferToItByNumber()
    {
        var before = Build();
        var after = new Automation();
        ProgramText.Load(ProgramText.Save(before), after);

        Assert.Equal(before.Systems[0].Id, after.Systems[0].Id);
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
        ProgramText.Load(ProgramText.Save(Build()), after);

        var airlock = after.Systems[0];
        Assert.Equal("Airlock", airlock.Name);
        Assert.Equal("Cycles the front airlock", airlock.Annotation);
        Assert.True(airlock.Enabled);
        Assert.Equal(2, airlock.Instances.Count);
        Assert.Equal("Port", airlock.Instances[1].Name);

        // What the author wrote about the parts.
        Assert.Equal("the door onto the ship", airlock.FindRole(1)!.Name);
        Assert.Equal("The one you walk through to get inside.", airlock.FindRole(1)!.Instructions);
        Assert.False(airlock.FindRole(1)!.Optional);
        Assert.True(airlock.FindRole(3)!.Optional);

        // And which of its own things the second one uses for them, including a part filled
        // with more than one.
        var port = airlock.Instances[1];
        Assert.Equal(new long[] { 21 }, port.FilledWith(1));
        Assert.Equal(new long[] { 23, 24 }, port.FilledWith(3));
        Assert.Null(port.FilledWith(99));

        Assert.False(after.Systems[1].Enabled);

        var letMeOut = after.Sequences[0];
        Assert.Equal("Let me out", letMeOut.Name);
        Assert.False(letMeOut.Enabled);

        var when = Assert.IsType<WhenTrigger>(letMeOut.Trigger);
        Assert.Equal("pressed", when.EventId);
        Assert.True(when.RequiresAll);
        Assert.Equal(new long[] { 4, 5 }, when.Sources);

        Assert.Equal(3, letMeOut.Steps.Count);
        Assert.Equal("close", letMeOut.Steps[0].ActionId);
        Assert.True(letMeOut.Steps[0].WaitsForCompletion);
        Assert.Equal(90, Assert.IsType<AfterGate>(letMeOut.Steps[1].Gate).Ticks);

        var only = Assert.IsType<OnlyIfGate>(letMeOut.Steps[2].Gate);
        Assert.Equal("is-shut", only.Condition.StateId);
        Assert.Equal(new long[] { 1 }, only.Condition.Entities);

        var timed = after.Sequences[1];
        Assert.Equal(600, Assert.IsType<EveryTrigger>(timed.Trigger).IntervalTicks);
        Assert.Equal("is-low", Assert.IsType<OnceGate>(timed.Steps[0].Gate).Condition.StateId);
    }

    /// <summary>
    /// Names are whatever somebody typed, which includes the separator.
    /// </summary>
    [Fact]
    public void AwkwardNamesSurvive()
    {
        var program = new Automation();
        var system = program.AddSystem();
        system.Name = @"Air|lock \ 2";
        system.Annotation = "line one\nline two";

        var sequence = program.AddSequence(system, new CalledTrigger());
        sequence.Name = "|||";

        var after = new Automation();
        ProgramText.Load(ProgramText.Save(program), after);

        Assert.Equal(@"Air|lock \ 2", after.Systems[0].Name);
        Assert.Equal("line one\nline two", after.Systems[0].Annotation);
        Assert.Equal("|||", after.Sequences[0].Name);
    }

    /// <summary>
    /// A save from a later build loses what this one cannot read, not everything.
    /// </summary>
    [Fact]
    public void LinesThisBuildDoesNotUnderstandAreSkipped()
    {
        var text = ProgramText.Save(Build()) + "somethingnew|1|2|3\n";

        var after = new Automation();
        ProgramText.Load(text, after);

        Assert.Equal(2, after.Systems.Count);
        Assert.Equal(2, after.Sequences.Count);
    }

    [Fact]
    public void NothingSavedLoadsAsNothing()
    {
        var after = new Automation();
        ProgramText.Load("", after);
        ProgramText.Load(null, after);

        Assert.Empty(after.Systems);
        Assert.Empty(after.Sequences);
    }
}
