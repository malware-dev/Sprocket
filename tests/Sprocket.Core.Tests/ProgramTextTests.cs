using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

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

    /// <summary>Declares a block called <paramref name="name" />, which is what a program names.</summary>
    static void Block(Automation program, string name) =>
        program.Restore(new EntityRef(name, BlockKinds.Definition("Door/LargeBlockDoor")));

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
        out1.Annotation = "Inside to outside.";
        airlock.Enabled = false;
        airlock.Halt = new Halt(SequenceProblem.MissingTarget, out1.Number, 2, null, "Front Inner Door");

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
        rig.Instances[0].Enabled = false;

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

        Assert.False(after.Systems[1].Instances[0].Enabled);

        var letMeOut = after.Sequences[0];
        Assert.Equal("Let me out", letMeOut.Name);
        Assert.Equal(SequenceProblem.MissingTarget, letMeOut.System.Halt!.Problem);
        Assert.Equal("Front Inner Door", letMeOut.System.Halt!.Block);

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
    /// A step carrying values comes back with them, each as the kind it was written as.
    /// </summary>
    /// <remarks>
    /// The program is the only record of what kind a value is. An action declares how many it wants
    /// and not one thing about them, and the block it is for may not be present when the program is
    /// read - so a colour that came back as a number would be a step that quietly does something
    /// else.
    /// <para>
    /// Written in the words the language already has for it, which is how a value gets somewhere to
    /// hang: a bare head has nowhere to put one.
    /// </para>
    /// </remarks>
    /// <summary>
    /// A step is written in the form that says whose it is, and read back as the same.
    /// </summary>
    /// <remarks>
    /// A bare head is Sprocket's own word, <c>action</c> is one of the game's actions and <c>set</c>
    /// one of the block's values. Whether values are given has no bearing on which: the game's
    /// reverse takes none and is still the game's, and Sprocket's own rotate-to takes values and is
    /// still a head.
    /// </remarks>
    [Fact]
    public void AStepIsWrittenAsWhoseItIs()
    {
        var program = new Automation();
        var values = new Values();
        program.Restore(Declared("rotor", "rotor", values, 1, "Arm Rotor"));

        var arm = program.AddSystem();
        arm.Name = "Arm";

        var sequence = program.AddSequence(arm, null);

        var reverse = new Act("Reverse") { Form = Form.Action };
        reverse.Targets.Add("rotor");
        sequence.Steps.Add(reverse);

        var rotate = new Act(RotorExtension.RotateTo) { Form = Form.Native };
        rotate.Targets.Add("rotor");
        rotate.Values.Add(Literal.Of(90L));
        rotate.Values.Add(Literal.Of(2.5));
        sequence.Steps.Add(rotate);

        var speed = new Act("Velocity") { Form = Form.Value };
        speed.Targets.Add("rotor");
        speed.Values.Add(Literal.Of(5.0));
        sequence.Steps.Add(speed);

        var saved = Save(program);
        Assert.Contains("act Reverse on [ rotor ]", saved);
        Assert.Contains("act sprocket.'rotate-to' on [ rotor ] with 90 2.5", saved);
        Assert.Contains("set Velocity on [ rotor ] to 5", saved);

        var after = new Automation();
        LoadAll(program, after);
        var steps = after.Sequences[0].Steps;

        var game = Assert.IsType<Act>(steps[0]);
        Assert.Equal(Form.Action, game.Form);
        Assert.Equal("Reverse", game.ActionId);
        Assert.Empty(game.Values);

        var own = Assert.IsType<Act>(steps[1]);
        Assert.Equal(Form.Native, own.Form);
        Assert.Equal(RotorExtension.RotateTo, own.ActionId);
        Assert.Equal(2, own.Values.Count);
        Assert.Equal(90, own.Values[0].Whole);
        Assert.Equal(2.5, own.Values[1].Real);

        var value = Assert.IsType<Act>(steps[2]);
        Assert.Equal(Form.Value, value.Form);
        Assert.Equal("Velocity", value.ActionId);
    }

    [Fact]
    public void TheValuesAnActionWasGivenSurvive()
    {
        var program = new Automation();
        var values = new Values();
        program.Restore(Declared("light", "light", values, 1, "Airlock Light"));

        var lights = program.AddSystem();
        lights.Name = "Lights";

        var sequence = program.AddSequence(lights, null);

        var colour = new Act("SetColor") { Form = Form.Action };
        colour.Targets.Add("light");
        colour.Values.Add(Literal.Of(255));
        colour.Values.Add(Literal.Of(136));
        colour.Values.Add(Literal.Of(0));
        sequence.Steps.Add(colour);

        var each = new Act("SetEverything") { Form = Form.Action };
        each.Targets.Add("light");
        each.Values.Add(Literal.Of("four and a half"));
        each.Values.Add(Literal.Of(4.5));
        each.Values.Add(Literal.Of(true));
        each.Values.Add(Literal.Colour(255, 136, 0, 255));
        sequence.Steps.Add(each);

        // The language's own words for it, rather than a spelling invented here.
        Assert.Contains("act SetColor on [ light ] with 255 136 0", Save(program));

        var after = new Automation();
        LoadAll(program, after);

        var back = Assert.IsType<Act>(after.Sequences[0].Steps[0]);
        Assert.Equal("SetColor", back.ActionId);
        Assert.Equal(new[] { "light" }, back.Targets);
        Assert.Equal(3, back.Values.Count);
        Assert.Equal(ValueKind.Integer, back.Values[0].Kind);
        Assert.Equal(255, back.Values[0].Whole);
        Assert.Equal(136, back.Values[1].Whole);
        Assert.Equal(0, back.Values[2].Whole);

        var kinds = Assert.IsType<Act>(after.Sequences[0].Steps[1]);
        Assert.Equal(4, kinds.Values.Count);

        Assert.Equal(ValueKind.Text, kinds.Values[0].Kind);
        Assert.Equal("four and a half", kinds.Values[0].Text);

        Assert.Equal(ValueKind.Real, kinds.Values[1].Kind);
        Assert.Equal(4.5, kinds.Values[1].Real);

        Assert.Equal(ValueKind.Flag, kinds.Values[2].Kind);
        Assert.True(kinds.Values[2].Flag);

        Assert.Equal(ValueKind.Color, kinds.Values[3].Kind);
        Assert.Equal(255, kinds.Values[3].Red);
        Assert.Equal(136, kinds.Values[3].Green);
        Assert.Equal(0, kinds.Values[3].Blue);
    }

    /// <summary>An action that asked for nothing is written as the action and its parts, with no <c>with</c>.</summary>
    [Fact]
    public void AStepWithNoValuesHasNoWith()
    {
        var program = new Automation();
        var values = new Values();
        program.Restore(Declared("inner", "door", values, 1, "Inner Door"));

        var airlock = program.AddSystem();
        airlock.Name = "Airlock";

        var sequence = program.AddSequence(airlock, null);
        var shut = new Act("close");
        shut.Targets.Add("inner");
        sequence.Steps.Add(shut);

        var written = Save(program);
        Assert.Contains("act sprocket.close on [ inner ]\n", written);
        Assert.DoesNotContain("with", written);
    }

    /// <summary>One of a block's own values is written as a set, and comes back as one.</summary>
    [Fact]
    public void AValueIsWrittenAsASet()
    {
        var program = new Automation();
        var values = new Values();
        program.Restore(Declared("light", "light", values, 1, "Airlock Light"));

        var lights = program.AddSystem();
        lights.Name = "Lights";

        var sequence = program.AddSequence(lights, null);
        var radius = new Act("Radius");
        radius.Form = Form.Value;
        radius.Targets.Add("light");
        radius.Values.Add(Literal.Of(4.5));
        sequence.Steps.Add(radius);

        Assert.Contains("set Radius on [ light ] to 4.5", Save(program));

        var after = new Automation();
        LoadAll(program, after);

        var back = Assert.IsType<Act>(after.Sequences[0].Steps[0]);
        Assert.True(back.Sets);
        Assert.Equal("Radius", back.ActionId);
        Assert.Equal(new[] { "light" }, back.Targets);
        Assert.Equal(4.5, Assert.Single(back.Values).Real);
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

        Block(program, "entity1");
        var halt = Pressed.On(system, "entity1");
        halt.Does = new Run(sequence.Number, true);
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
    /// A sequence's name is optional, and that is worth a test, because the one thing that would break
    /// it is the writer deciding to name everything.
    /// </remarks>
    [Fact]
    public void AnUnnamedSequenceAndItsTriggerSurviveBeingSaved()
    {
        var program = new Automation();
        var system = program.AddSystem();
        system.Name = "Airlock";

        Block(program, "entity1");
        Block(program, "entity2");

        var open = new Act("open");
        open.Targets.Add("entity1");

        var sequence = program.AddSequence(system);
        sequence.Steps.Add(open);

        var pressed = Pressed.On(system, "entity2");
        pressed.Does = new Run(sequence.Number);
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
