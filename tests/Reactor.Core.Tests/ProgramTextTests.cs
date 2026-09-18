using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Host.SpaceEngineers.Storage;
using Reactor.Host.SpaceEngineers.World;
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

    /// <summary>
    /// The program and the switches beside it, which is how a host keeps them.
    /// </summary>
    /// <remarks>
    /// Two documents, because what is switched off is state and must not travel with a system
    /// somebody copies out. A round trip therefore has to carry both to be a round trip at all.
    /// </remarks>
    static void LoadAll(Automation before, Automation after)
    {
        Load(Save(before), after);
        Assert.Equal("", StateText.Load(StateText.Save(before, null), after, null));
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

        var close = new Act("close");
        close.Targets.Add("inner");
        close.Annotation = "the vent needs the full sixty seconds";
        out1.Steps.Add(close);
        out1.Steps.Add(new Await());

        out1.Steps.Add(new Wait(90));

        var vent = new Act("depressurize");
        vent.Targets.Add("vent");
        out1.Steps.Add(vent);

        // What the author wrote about a part, and a second one of the system that fills the
        // parts with its own doors.
        var inner = airlock.PartFor("inner");
        inner.Annotation = "The door onto the ship.\n\nThe one you walk through to get inside.";
        inner.Expects.Add("Door");
        inner.Expects.Add("AirtightSlideDoor");

        var spare = airlock.PartFor("vent");
        spare.Annotation = "The vent.";

        var port = airlock.Instances[1];
        port.Enabled = false;
        port.Fill("inner", new[] { "outer" });
        port.Fill("vent", new[] { "call", "spare" });

        var rig = program.AddSystem();
        rig.Name = "Drill Rig";
        rig.Enabled = false;

        var timed = program.AddSequence(rig, new EveryTrigger(600));
        timed.Name = "Top up";

        var lower = new Act("open");
        lower.Targets.Add("outer");
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

        // A system is its identity, and keeps its namespace and name, including no namespace at all.
        Assert.Equal(before.Systems[0].Identity, after.Systems[0].Identity);
        Assert.Equal(before.Systems[1].Identity, after.Systems[1].Identity);
        Assert.True(after.Systems[0].Is("MalTek", "Airlock"));
        Assert.True(after.Systems[1].Is("", "Drill Rig"));

        // A sequence is its number within its system, so two systems each have a sequence 1.
        Assert.Equal(1, after.Sequences[0].Number);
        Assert.Equal(1, after.Sequences[1].Number);

        // And a new one takes the next number its own system has free, whatever the other system holds.
        var fresh = after.AddSequence(after.Systems[0]);
        Assert.Equal(2, fresh.Number);
    }

    /// <summary>
    /// A sequence's number is one past the last its system handed out, so no number is handed out twice.
    /// </summary>
    [Fact]
    public void ANewSequenceTakesOnePastTheLastItsSystemHandedOut()
    {
        var program = new Automation();
        var system = program.AddSystem();
        system.Name = "Airlock";

        var one = program.AddSequence(system);
        var two = program.AddSequence(system);
        var three = program.AddSequence(system);
        Assert.Equal(new[] { 1, 2, 3 }, new[] { one.Number, two.Number, three.Number });

        program.RemoveSequence(two);
        Assert.Equal(4, program.AddSequence(system).Number);

        // Including the highest, which is the case a count of what exists would get wrong.
        program.RemoveSequence(program.Sequences[program.Sequences.Count - 1]);
        Assert.Equal(5, program.AddSequence(system).Number);
    }

    [Fact]
    public void EverySavedDetailComesBack()
    {
        var after = new Automation();
        LoadAll(Build(), after);

        var airlock = after.Systems[0];
        Assert.Equal("Airlock", airlock.Name);
        Assert.Equal("Cycles the front airlock", airlock.Annotation);
        Assert.True(airlock.Enabled);
        Assert.Equal(2, airlock.Instances.Count);
        Assert.Equal("Port", airlock.Instances[1].Name);

        // One of them parked. The format could not say this at all until it became a language.
        Assert.True(airlock.Instances[0].Enabled);
        Assert.False(airlock.Instances[1].Enabled);

        // The parts, what they expect, and what the author wrote about them.
        Assert.Equal("The door onto the ship.\n\nThe one you walk through to get inside.",
            airlock.FindPart("inner")!.Annotation);
        Assert.Equal(new[] { "Door", "AirtightSlideDoor" }, airlock.FindPart("inner")!.Expects);
        Assert.Equal("The vent.", airlock.FindPart("vent")!.Annotation);
        Assert.Empty(airlock.FindPart("vent")!.Expects);

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

        Assert.Equal(4, letMeOut.Steps.Count);
        Assert.Equal("close", Assert.IsType<Act>(letMeOut.Steps[0]).ActionId);
        Assert.IsType<Await>(letMeOut.Steps[1]);
        Assert.Equal(90, Assert.IsType<Wait>(letMeOut.Steps[2]).Ticks);
        Assert.Equal("depressurize", Assert.IsType<Act>(letMeOut.Steps[3]).ActionId);

        var timed = after.Sequences[1];
        Assert.Equal(600, Assert.IsType<EveryTrigger>(timed.Trigger).IntervalTicks);
        Assert.Equal("open", Assert.IsType<Act>(timed.Steps[0]).ActionId);
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

        var sequence = program.AddSequence(system);
        sequence.Name = @"\\\";

        var after = new Automation();
        Load(Save(program), after);

        Assert.Equal(@"Air\lock ""2""", after.Systems[0].Name);
        Assert.Equal("line one\nline two", after.Systems[0].Annotation);
        Assert.Equal(@"\\\", after.Sequences[0].Name);
    }

    /// <summary>A trigger that stops a sequence writes down, and reads back as stopping it.</summary>
    [Fact]
    public void ATriggerThatStopsASequenceSurvivesBeingSaved()
    {
        var program = new Automation();
        var system = program.AddSystem();
        system.Name = "Airlock";

        var sequence = program.AddSequence(system);
        sequence.Name = "Cycle";

        var halt = new RequiredTrigger("pressed-1");
        halt.Runs = sequence.Number;
        halt.Stops = true;
        halt.Sources.Add("entity1");
        system.AddTrigger(halt);

        var text = Save(program);
        Assert.Contains("to stop 1", text);

        var after = new Automation();
        Load(text, after);

        Assert.True(after.Systems[0].Triggers[0].Stops);
        Assert.Equal(1, after.Systems[0].Triggers[0].Runs);
    }

    /// <summary>
    /// A sequence nobody named writes down, and reads back, with the trigger that runs it.
    /// </summary>
    /// <remarks>
    /// What a trigger holds when it does one thing Reactor has to perform itself: nothing stands
    /// alone on screen, and there is no name to show. The format needs no spelling of its own for it
    /// - a sequence's name is optional - and that is worth a test, because the one thing that would
    /// break it is the writer deciding to name everything.
    /// </remarks>
    [Fact]
    public void AnUnnamedSequenceAndItsTriggerSurviveBeingSaved()
    {
        var program = new Automation();
        var system = program.AddSystem();
        system.Name = "Airlock";

        var open = new Act("open");
        open.Targets.Add("entity1");

        var sequence = program.AddSequence(system);
        sequence.Steps.Add(open);

        var pressed = new RequiredTrigger();
        pressed.EventId = "pressed-1";
        pressed.Runs = sequence.Number;
        pressed.Sources.Add("entity2");
        system.AddTrigger(pressed);

        var after = new Automation();
        Load(Save(program), after);

        var read = after.Systems[0].Sequences[0];
        Assert.False(read.HasName, "the writer named a sequence nobody named");
        Assert.Single(read.Steps);
        Assert.Single(after.Systems[0].Triggers);
        Assert.Equal(read.Number, after.Systems[0].Triggers[0].Runs);
    }

    /// <summary>
    /// A system nobody named still writes down, and reads back.
    /// </summary>
    /// <remarks>
    /// The format has no spelling for a system with no name: it wrote an empty one and then refused
    /// to read it, so a program built by anything that did not name its system was lost on the next
    /// load. Named from birth now - see <see cref="Automation.AddSystem" />.
    /// </remarks>
    [Fact]
    public void ASystemNobodyNamedSurvivesBeingSaved()
    {
        var program = new Automation();
        var system = program.AddSystem();
        program.AddSequence(system);

        Assert.True(system.HasName);

        var after = new Automation();
        Load(Save(program), after);

        Assert.Single(after.Systems);
        Assert.Equal(system.Name, after.Systems[0].Name);
    }

    /// <summary>Two of them are not both called the same thing.</summary>
    [Fact]
    public void SystemsNobodyNamedAreNamedApart()
    {
        var program = new Automation();

        Assert.NotEqual(program.AddSystem().Name, program.AddSystem().Name);
    }

    [Fact]
    public void NothingSavedLoadsAsNothing()
    {
        var after = new Automation();
        Load("", after);
        Load(null!, after);

        Assert.Empty(after.Systems);
        Assert.Empty(after.Sequences);
    }
}
