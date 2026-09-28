using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// The saved script is a language, and this is what it promises.
/// </summary>
/// <remarks>
/// The round-trip tests next door prove nothing is lost. These prove the other half, which matters
/// because the script is the program and not a view of it: what the text looks like, that trouble is
/// reported rather than swallowed, and that a program which did not read cleanly refuses to be saved
/// back over the one that did.
/// </remarks>
public class ScriptTests
{
    const string Identity = "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43";

    const string Head = "sprocket 1 alpha 7\n\nsystem \"" + Identity + "\" as ";

    static readonly Values Store = new Values();

    static string Save(Automation program) => ProgramText.Save(program, Store);

    static string Load(string text, Automation program) => ProgramText.Load(text, program, Store);

    static Automation Airlock()
    {
        var program = new Automation();
        var door = new EntityRef("inner", "Door/LargeBlockDoor");

        // Seventeen digits, which is what a Space Engineers entity id runs to and what would come back
        // a different id through a double.
        door.Keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(74857693847562934L)));
        door.Keys.Add(new EntityKey(ValueKind.Text, Store.Text("Airlock Inner Door")));
        program.Restore(door);

        var system = program.AddSystem();
        system.Identity = new Guid(Identity);
        system.Namespace = "MalTek";
        system.Name = "Airlock";
        system.Changed = new DateTime(2026, 9, 14, 19, 2, 11, DateTimeKind.Utc);
        system.Annotation = "Cycles an airlock.\n\nNothing opens on both sides at once.";

        var part = system.PartFor("inner");
        part.Annotation = "The door onto the ship.";
        part.Expects.Add("Door");

        system.Instances[0].Fill("inner", new[] { "inner" });

        var pressed = new WhenTrigger("pressed");
        pressed.Sources.Add("inner");
        var sequence = program.AddSequence(system, pressed);
        sequence.Name = "Let me out";

        var close = new Act("close");
        close.Targets.Add("inner");
        sequence.Steps.Add(close);
        sequence.Steps.Add(new Await());

        sequence.Steps.Add(new Wait(60));

        var open = new Act("open");
        open.Targets.Add("inner");
        open.Annotation = "The vent needs the full cycle.";
        sequence.Steps.Add(open);
        sequence.Steps.Add(new Await());

        return program;
    }

    static Automation SystemCalled(string space, string name)
    {
        var program = new Automation();
        var system = program.AddSystem();
        system.Identity = new Guid(Identity);
        system.Namespace = space;
        system.Name = name;
        return program;
    }

    /// <summary>
    /// What a program looks like written down, in full.
    /// </summary>
    /// <remarks>
    /// Spelled out rather than described, because the format is a promise: anything that changes what
    /// is written changes what every existing save means, and that should be something somebody has to
    /// look at in a diff rather than something a test tolerates.
    /// </remarks>
    [Fact]
    public void AProgramReadsAsAScript()
    {
        Assert.Equal(
            "sprocket 1 alpha 7\n" +
            "\n" +
            "block 'Door/LargeBlockDoor' inner [ 74857693847562934, \"Airlock Inner Door\" ]\n" +
            "\n" +
            "# Cycles an airlock.\n" +
            "#\n" +
            "# Nothing opens on both sides at once.\n" +
            "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock changed \"2026-09-14T19:02:11Z\" {\n" +
            "    # The door onto the ship.\n" +
            "    required block inner expects Door\n" +
            "\n" +
            "    when any pressed [ inner ] then run 1\n" +
            "\n" +
            "    sequence 1 as 'Let me out' {\n" +
            "        close [ inner ]\n" +
            "        await\n" +
            "        wait 60t\n" +
            "\n" +
            "        # The vent needs the full cycle.\n" +
            "        open [ inner ]\n" +
            "        await\n" +
            "    }\n" +
            "}\n" +
            "\n" +
            "instance 1 of \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" {\n" +
            "    inner = [ inner ]\n" +
            "}\n",
            Save(Airlock()));
    }

    /// <summary>
    /// A system is its identity, and is called by a namespace and a name, whatever they contain.
    /// </summary>
    [Fact]
    public void ASystemIsItsIdentityCalledByANamespaceAndName()
    {
        var text = Save(SystemCalled("MalTek Inc.", "Airlock System"));
        Assert.Contains("system \"" + Identity + "\" as 'MalTek Inc.'.'Airlock System' {", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.Equal(new Guid(Identity), after.Systems[0].Identity);
        Assert.True(after.Systems[0].Is("MalTek Inc.", "Airlock System"));
    }

    /// <summary>
    /// A system with no identity, or one that is not a GUID, is trouble and is not read.
    /// </summary>
    [Theory]
    [InlineData("sprocket 1 alpha 7\n\nsystem Airlock {\n}\n")]
    [InlineData("sprocket 1 alpha 7\n\nsystem \"airlock\" as Airlock {\n}\n")]
    [InlineData("sprocket 1 alpha 7\n\nsystem \"00000000-0000-0000-0000-000000000000\" as Airlock {\n}\n")]
    public void ASystemWithoutAnIdentityIsTrouble(string text)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(text, after));
        Assert.False(after.Intact);
        Assert.Empty(after.Systems);
    }

    /// <summary>
    /// A length of time is a number and its unit as one word, and ticks are the only unit.
    /// </summary>
    /// <remarks>
    /// Loud rather than lenient about the unit: <c>600s</c> is somebody meaning seconds, and reading it as
    /// 600 ticks would run their airlock ten times too fast.
    /// </remarks>
    [Theory]
    [InlineData("wait 60t", "")]
    [InlineData("wait 60s", "line 5")]
    [InlineData("wait 60", "line 5")]
    public void ADurationIsANumberAndItsUnit(string written, string trouble)
    {
        var after = new Automation();
        var said = Load(
            Head + "Airlock {\n    sequence 1 {\n        " + written + "\n    }\n}\n", after);

        Assert.StartsWith(trouble, said);
    }

    [Fact]
    public void EveryNewSystemHasAnIdentityOfItsOwn()
    {
        var program = new Automation();
        var one = program.AddSystem();
        var other = program.AddSystem();

        Assert.NotEqual(Guid.Empty, one.Identity);
        Assert.NotEqual(one.Identity, other.Identity);
    }

    /// <summary>
    /// A part says what may fill it, as one block type or a list of them.
    /// </summary>
    /// <remarks>
    /// A family spans type ids - a door is <c>Door</c>, <c>AirtightSlideDoor</c> and
    /// <c>AirtightHangarDoor</c> - so a list is the ordinary case rather than the awkward one.
    /// </remarks>
    [Fact]
    public void APartSaysWhatMayFillIt()
    {
        var program = SystemCalled("", "Airlock");
        var system = program.Systems[0];

        var inner = system.PartFor("inner");
        inner.Annotation = "The inner door.";
        inner.Expects.Add("Door");
        inner.Expects.Add("AirtightSlideDoor");

        system.PartFor("vent").Expects.Add("AirVent");
        system.PartFor("anything");

        var text = Save(program);
        Assert.Contains("    # The inner door.\n    required block inner expects [ Door, AirtightSlideDoor ]\n", text);
        Assert.Contains("    required block vent expects AirVent\n", text);
        Assert.Contains("    required block anything\n", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));

        var read = after.Systems[0];
        Assert.Equal(new[] { "Door", "AirtightSlideDoor" }, read.FindPart("inner")!.Expects);
        Assert.Equal("The inner door.", read.FindPart("inner")!.Annotation);
        Assert.Equal(new[] { "AirVent" }, read.FindPart("vent")!.Expects);
        Assert.Empty(read.FindPart("anything")!.Expects);
    }

    /// <summary>A part a construct may leave unfilled is written optional, and read back as one.</summary>
    [Fact]
    public void APartMayBeOptional()
    {
        var program = SystemCalled("", "Airlock");
        var system = program.Systems[0];
        system.PartFor("inner").Expects.Add("Door");
        var lamp = system.PartFor("lamp");
        lamp.Expects.Add("InteriorLight");
        lamp.Optional = true;

        var text = Save(program);
        Assert.Contains("    required block inner expects Door\n", text);
        Assert.Contains("    optional block lamp expects InteriorLight\n", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.False(after.Systems[0].FindPart("inner")!.Optional);
        Assert.True(after.Systems[0].FindPart("lamp")!.Optional);
    }

    /// <summary>
    /// A subtype is quoted, because a `/` is not a bare name.
    /// </summary>
    [Fact]
    public void AnExactBlockTypeIsQuoted()
    {
        var program = SystemCalled("", "Hangar");
        program.Systems[0].PartFor("door").Expects.Add("AirtightHangarDoor/LargeBlockAirtightHangarDoor");

        var text = Save(program);
        Assert.Contains("required block door expects 'AirtightHangarDoor/LargeBlockAirtightHangarDoor'\n", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.Equal(new[] { "AirtightHangarDoor/LargeBlockAirtightHangarDoor" },
            after.Systems[0].FindPart("door")!.Expects);
    }

    [Theory]
    [InlineData("required inner expects Door")]
    [InlineData("required block inner expects")]
    public void ARequiredLineThatSaysNothingUsefulIsTrouble(string line)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(Head + "Airlock {\n    " + line + "\n}\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>
    /// One of a system says which system it is one of, by identity or by name.
    /// </summary>
    /// <remarks>
    /// Sprocket writes the identity, because a name changes. A person writing one by hand has the name
    /// in front of them, so both read.
    /// </remarks>
    [Theory]
    [InlineData("\"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\"")]
    [InlineData("MalTek.Airlock")]
    public void OneOfASystemNamesItsSystem(string named)
    {
        var after = new Automation();
        Assert.Equal("", Load(
            Head + "MalTek.Airlock {\n}\n\ninstance 2 of " + named
            + " as 'Port airlock' {\n    inner = [ door1 ]\n    outer = [ door2, door3 ]\n}\n",
            after));

        var one = Assert.Single(after.Systems).Instances[0];
        Assert.Equal(2, one.Number);
        Assert.Equal("Port airlock", one.Name);
        Assert.Equal(new[] { "door1" }, one.FilledWith("inner"));
        Assert.Equal(new[] { "door2", "door3" }, one.FilledWith("outer"));
    }

    [Theory]
    [InlineData("instance 2 MalTek.Airlock {\n}\n")]
    [InlineData("instance of MalTek.Airlock {\n}\n")]
    [InlineData("instance 2 of MalTek.Hangar {\n}\n")]
    public void OneOfASystemThatSaysNothingUsefulIsTrouble(string line)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(Head + "MalTek.Airlock {\n}\n\n" + line, after));
        Assert.False(after.Intact);
    }

    /// <summary>
    /// A part is answered with <c>=</c>, and a line inside one of a system that says anything else is
    /// trouble.
    /// </summary>
    [Fact]
    public void AnInstanceSaysWhatAPartIs()
    {
        var after = new Automation();
        Assert.NotEqual("", Load(
            Head + "MalTek.Airlock {\n}\n\ninstance 1 of MalTek.Airlock {\n    fill inner [ door1 ]\n}\n",
            after));

        Assert.False(after.Intact);
    }

    /// <summary>
    /// A trigger belongs to the system and says which sequence it runs.
    /// </summary>
    /// <remarks>
    /// Which is what a trigger on a sequence could never say: two triggers running one sequence, the
    /// inside button and the outside one.
    /// </remarks>
    [Fact]
    public void ATriggerSaysWhatItRuns()
    {
        var after = new Automation();
        Assert.Equal("", Load(
            Head + "MalTek.Airlock {\n" +
            "    when any pressed [ inside ] then run 1\n" +
            "    when all closed [ inner, outer ] then run 1\n" +
            "    every 600t run 2\n\n" +
            "    sequence 1 {\n    }\n\n" +
            "    sequence 2 {\n    }\n" +
            "}\n", after));

        var system = Assert.Single(after.Systems);
        Assert.Equal(3, system.Triggers.Count);

        var inside = Assert.IsType<WhenTrigger>(system.Triggers[0]);
        Assert.Equal("pressed", inside.EventId);
        Assert.False(inside.RequiresAll);
        Assert.Equal(new[] { "inside" }, inside.Sources);
        Assert.Equal(1, inside.Runs);

        var shut = Assert.IsType<WhenTrigger>(system.Triggers[1]);
        Assert.True(shut.RequiresAll);
        Assert.Equal(new[] { "inner", "outer" }, shut.Sources);
        Assert.Equal(1, shut.Runs);

        var clock = Assert.IsType<EveryTrigger>(system.Triggers[2]);
        Assert.Equal(600, clock.IntervalTicks);
        Assert.Equal(2, clock.Runs);

        // Both of the first two run sequence 1, and the sequence itself says nothing about either.
        Assert.Same(system.Triggers[0], system.Sequences[0].Trigger);

        // And it all comes back the same way.
        var text = Save(after);
        Assert.Contains("    when any pressed [ inside ] then run 1\n", text);
        Assert.Contains("    when all closed [ inner, outer ] then run 1\n", text);
        Assert.Contains("    every 600t run 2\n", text);
    }

    [Theory]
    [InlineData("when any pressed [ inside ] run 1")]
    [InlineData("when pressed [ inside ] then run 1")]
    [InlineData("when any pressed [ inside ] then run")]
    [InlineData("every 600 run 1")]
    [InlineData("every 600t")]
    public void ATriggerThatSaysNothingUsefulIsTrouble(string line)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(
            Head + "MalTek.Airlock {\n    " + line + "\n\n    sequence 1 {\n    }\n}\n", after));

        Assert.False(after.Intact);
    }

    /// <summary>
    /// A trigger the construct provides says only what it runs, and what it is for.
    /// </summary>
    /// <remarks>
    /// Which button answers it is on the ship's toolbars, not here, so two of them running one
    /// sequence is the ordinary way to say "the inside button and the outside one".
    /// </remarks>
    [Fact]
    public void ATriggerTheConstructProvidesSaysWhatItRuns()
    {
        var program = SystemCalled("MalTek", "Airlock");
        var system = program.Systems[0];
        program.AddSequence(system);

        var inside = new RequiredTrigger();
        inside.Annotation = "The call button inside.";
        inside.Runs = 1;
        system.AddTrigger(inside);

        var outside = new RequiredTrigger();
        outside.Runs = 1;
        system.AddTrigger(outside);

        var text = Save(program);
        Assert.Contains(
            "    # The call button inside.\n    required trigger to run 1\n    required trigger to run 1\n",
            text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));

        var read = after.Systems[0];
        Assert.Equal(2, read.Triggers.Count);
        Assert.All(read.Triggers, t => Assert.IsType<RequiredTrigger>(t));
        Assert.All(read.Triggers, t => Assert.Equal(1, t.Runs));
        Assert.Equal("The call button inside.", read.Triggers[0].Annotation);
        Assert.Equal("", read.Triggers[1].Annotation);
    }

    /// <summary>
    /// A required trigger says which block and which of its moments, where somebody said.
    /// </summary>
    /// <remarks>
    /// The other half of what "the construct provides this" means. The arrangement is a toolbar
    /// slot and stays one, but a person reading their own program has to see the button they
    /// picked - and one of a system fitted elsewhere has to have something to remap, which a line
    /// naming nothing could never give it.
    /// </remarks>
    [Fact]
    public void ATriggerTheConstructProvidesSaysWhichBlockAndWhichMoment()
    {
        var program = SystemCalled("MalTek", "Airlock");
        var system = program.Systems[0];
        program.AddSequence(system);

        var pressed = new RequiredTrigger("pressed_1");
        pressed.Sources.Add("call");
        pressed.Runs = 1;
        system.AddTrigger(pressed);

        var text = Save(program);
        Assert.Contains("    required trigger pressed_1 [ call ] to run 1\n", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));

        var read = Assert.IsType<RequiredTrigger>(after.Systems[0].Triggers[0]);
        Assert.Equal("pressed_1", read.EventId);
        Assert.Equal("call", Assert.Single(read.Sources));
        Assert.Equal(1, read.Runs);
    }

    [Theory]
    [InlineData("required trigger run 1")]
    [InlineData("required trigger pressed_1 [call] run 1")]
    [InlineData("required trigger to run")]
    [InlineData("required whatever to run 1")]
    public void ARequiredTriggerThatSaysNothingUsefulIsTrouble(string line)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(
            Head + "MalTek.Airlock {\n    " + line + "\n\n    sequence 1 {\n    }\n}\n", after));

        Assert.False(after.Intact);
    }

    /// <summary>
    /// A step may run another sequence of the same system, by number or by name.
    /// </summary>
    /// <remarks>
    /// By name because a person writing one by hand has the name in front of them, and the name may
    /// belong to a sequence written below - so it is matched once the whole system has been read.
    /// Sprocket writes the number.
    /// </remarks>
    [Fact]
    public void AStepMayRunAnotherSequence()
    {
        var after = new Automation();
        Assert.Equal("", Load(
            Head + "MalTek.Airlock {\n" +
            "    sequence 1 {\n" +
            "        run 'Tidy up'\n" +
            "        run 2\n" +
            "    }\n\n" +
            "    sequence 2 as 'Tidy up' {\n" +
            "    }\n" +
            "}\n", after));

        var steps = after.Sequences[0].Steps;
        Assert.Equal(2, steps.Count);
        Assert.Equal(2, Assert.IsType<Run>(steps[0]).Number);
        Assert.Equal(2, Assert.IsType<Run>(steps[1]).Number);

        // Written back as numbers, both of them.
        Assert.Contains("        run 2\n        run 2\n", Save(after));
    }

    [Theory]
    [InlineData("run 'Nothing like this'")]
    [InlineData("run \"Tidy up\"")]
    [InlineData("run")]
    public void ARunThatNamesNothingIsTrouble(string line)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(
            Head + "MalTek.Airlock {\n    sequence 1 {\n        " + line + "\n    }\n}\n", after));

        Assert.False(after.Intact);
    }

    /// <summary>
    /// A statement that stops early is trouble, and never reads the next statement's words.
    /// </summary>
    /// <remarks>
    /// A reader asking for a value past the end of a statement used to be handed a run of one pointing
    /// into whatever was written next, so a system with nothing after its <c>as</c> either threw out of
    /// the loader or quietly took its name from the statement below and saved that.
    /// </remarks>
    [Fact]
    public void AStatementThatStopsEarlyDoesNotReadTheNextOne()
    {
        var alone = new Automation();
        Assert.NotEqual("", Load(Head + "\n", alone));
        Assert.False(alone.Intact);
        Assert.Empty(alone.Systems);

        var after = new Automation();
        Assert.NotEqual("", Load(
            Head + "\n\nsystem \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Airlock {\n}\n", after));

        Assert.False(after.Intact);

        // The one that did read is itself, and nothing borrowed its name.
        var read = Assert.Single(after.Systems);
        Assert.True(read.Is("MalTek", "Airlock"));
    }

    /// <summary>
    /// A <c>run</c> that names a sequence the system has not got is trouble.
    /// </summary>
    /// <remarks>
    /// Loud, because the alternative was a program that read cleanly and then threw out of the runner
    /// on the tick that reached the step.
    /// </remarks>
    [Theory]
    [InlineData("run 7")]
    [InlineData("run 'Nothing like this'")]
    public void ARunThatNamesNoSequenceOfTheSystemIsTrouble(string line)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(
            Head + "MalTek.Airlock {\n    sequence 1 {\n        " + line + "\n    }\n}\n", after));

        Assert.False(after.Intact);
    }

    /// <summary>
    /// A length of time no clock could keep is trouble, rather than the time it wraps onto.
    /// </summary>
    /// <remarks>
    /// A duration is read into a whole number of ticks, and the number in the text is not bounded by
    /// one: <c>every 4294967296t</c> came out as an interval of nothing and ran every tick, and
    /// <c>wait -5t</c> as no wait at all.
    /// </remarks>
    [Theory]
    [InlineData("    every 0t run 1\n    sequence 1 {\n    }\n")]
    [InlineData("    every -600t run 1\n    sequence 1 {\n    }\n")]
    [InlineData("    every 4294967296t run 1\n    sequence 1 {\n    }\n")]
    [InlineData("    sequence 1 {\n        wait -5t\n    }\n")]
    [InlineData("    sequence 1 {\n        wait 4294967296t\n    }\n")]
    public void ALengthOfTimeThatIsNoLengthOfTimeIsTrouble(string body)
    {
        var program = new Automation();
        Assert.NotEqual("", Load(Head + "MalTek.Airlock {\n" + body + "}\n", program));

        Assert.False(program.Intact);
    }

    /// <summary>
    /// The words above a trigger are the trigger's, and a save keeps them.
    /// </summary>
    /// <remarks>
    /// The annotation is what the config panel shows a person about the button it is asking them for,
    /// so losing it on save loses the only thing that says what the trigger is for.
    /// </remarks>
    [Fact]
    public void AnAnnotationOnATriggerSurvivesASave()
    {
        var program = Airlock();
        program.Systems[0].Triggers[0].Annotation = "The call button.";

        var read = new Automation();
        Assert.Equal("", Load(Save(program), read));

        Assert.Equal("The call button.", read.Systems[0].Triggers[0].Annotation);
    }

    /// <summary>
    /// Two of a system claiming the same number is trouble, and the second one is not read.
    /// </summary>
    /// <remarks>
    /// One number is one of a system: it is what a switch names and what the runner ticks, so a
    /// second one carrying it ran that one twice a tick.
    /// </remarks>
    [Fact]
    public void TwoOfASystemWithTheSameNumberIsTrouble()
    {
        var program = new Automation();
        Assert.NotEqual("", Load(
            Head + "MalTek.Airlock {\n}\n\ninstance 1 of MalTek.Airlock {\n}\n"
            + "\ninstance 1 of MalTek.Airlock {\n}\n", program));

        Assert.False(program.Intact);
        Assert.Single(program.Systems[0].Instances);
    }

    /// <summary>
    /// The dot is an operator, so the spaces around it are nothing and the name is always its parts.
    /// </summary>
    [Fact]
    public void SpacesAroundTheDotAreNothing()
    {
        var after = new Automation();
        Assert.Equal("", Load(Head + "'MalTek Inc.'  .  Airlock {\n}\n", after));
        Assert.True(after.Systems[0].Is("MalTek Inc.", "Airlock"));

        // Written back tight, because the writer has one spelling for a name.
        Assert.Contains(" as 'MalTek Inc.'.Airlock {", Save(after));
    }

    /// <summary>
    /// Two different names can never spell the same.
    /// </summary>
    /// <remarks>
    /// Joined plainly, <c>a.b</c> and <c>c</c> would collide with <c>a</c> and <c>b.c</c>. The quoting is
    /// what keeps them apart.
    /// </remarks>
    [Fact]
    public void TwoDifferentNamesNeverSpellTheSame()
    {
        Assert.Contains(" as 'a.b'.c {", Save(SystemCalled("a.b", "c")));
        Assert.Contains(" as a.'b.c' {", Save(SystemCalled("a", "b.c")));
    }

    /// <summary>
    /// Text is never a name, even when it says the same thing.
    /// </summary>
    /// <remarks>
    /// Once expressions exist, a quote style that meant both would leave <c>"Airlock"</c> undecidable -
    /// text to compare against, or the thing called that. So the two never stand in for each other.
    /// </remarks>
    [Fact]
    public void TextIsNeverAName()
    {
        var after = new Automation();
        Assert.NotEqual("", Load(Head + "\"Airlock\" {\n}\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>
    /// A sequence is its number within its system, and its name is optional and comes first.
    /// </summary>
    [Fact]
    public void ASequenceIsItsNumberWithItsNameOptional()
    {
        var after = new Automation();
        Assert.Equal("", Load(
            Head + "Airlock {\n" +
            "    sequence 1 as 'Let me out' {\n" +
            "    }\n\n" +
            "    sequence 2 {\n" +
            "    }\n" +
            "}\n", after));

        Assert.Equal("Let me out", after.Sequences[0].Name);
        Assert.Equal(2, after.Sequences[1].Number);
        Assert.False(after.Sequences[1].HasName);

        var text = Save(after);
        Assert.Contains("sequence 1 as 'Let me out' {", text);
        Assert.Contains("sequence 2 {", text);
    }

    [Fact]
    public void TwoSequencesWithTheSameNumberInOneSystemAreTrouble()
    {
        var after = new Automation();
        var trouble = Load(
            Head + "Airlock {\n" +
            "    sequence 1 {\n" +
            "    }\n\n" +
            "    sequence 1 {\n" +
            "    }\n" +
            "}\n", after);

        Assert.NotEqual("", trouble);
        Assert.False(after.Intact);
        Assert.Single(after.Sequences);
    }

    [Fact]
    public void TwoSystemsWithTheSameIdentityAreTrouble()
    {
        var after = new Automation();
        var trouble = Load(Head + "MalTek.Airlock {\n}\n\nsystem \"" + Identity + "\" as MalTek.Hangar {\n}\n",
            after);

        Assert.NotEqual("", trouble);
        Assert.False(after.Intact);
        Assert.Single(after.Systems);
    }

    /// <summary>
    /// Two systems with the same name both load, with a warning, since only their identities decide anything.
    /// </summary>
    [Fact]
    public void TwoSystemsWithTheSameNameAreAWarning()
    {
        var after = new Automation();
        string warning;
        var trouble = ProgramText.Load(
            Head + "MalTek.Airlock {\n}\n\nsystem \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Airlock {\n}\n",
            after, Store, out warning);

        Assert.Equal("", trouble);
        Assert.Equal("line 6: two systems are both called MalTek.Airlock", warning);
        Assert.True(after.Intact);
        Assert.Equal(2, after.Systems.Count);
    }

    /// <summary>
    /// A comma separates the items of a list, and appears nowhere else.
    /// </summary>
    [Fact]
    public void ACommaOutsideAListIsTrouble()
    {
        var after = new Automation();
        Assert.NotEqual("", Load(Head + "Airlock, changed \"2026-09-14T19:02:11Z\" {\n}\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>
    /// A changed time is ISO 8601 and nothing else, and is written back as UTC.
    /// </summary>
    [Fact]
    public void AChangedTimeIsIsoAndComesBackAsUtc()
    {
        var after = new Automation();
        Assert.Equal("", Load(Head + "Airlock changed \"2026-09-14T21:02:11+02:00\" {\n}\n",
            after));
        Assert.Contains(" as Airlock changed \"2026-09-14T19:02:11Z\" {", Save(after));

        var local = new Automation();
        Assert.NotEqual("", Load(Head + "Airlock changed \"14/09/2026\" {\n}\n", local));
        Assert.False(local.Intact);
    }

    /// <summary>
    /// The parts of a statement are found by what they say, whatever order they come in.
    /// </summary>
    [Fact]
    public void ThePartsOfAStatementReadInAnyOrder()
    {
        var after = new Automation();
        Assert.Equal("", Load(Head + "Airlock last sequence 4 changed \"2026-09-14T19:02:11Z\" {\n}\n",
            after));

        Assert.Equal(new DateTime(2026, 9, 14, 19, 2, 11, DateTimeKind.Utc), after.Systems[0].Changed);
        Assert.Equal(4, after.Systems[0].LastSequenceNumber);

        // Written back in the writer's own order.
        Assert.Contains(" as Airlock changed \"2026-09-14T19:02:11Z\" last sequence 4 {", Save(after));
    }

    /// <summary>
    /// The last sequence number is written only when the highest has been deleted, as a failsafe.
    /// </summary>
    /// <remarks>
    /// Without it, the next sequence made after a reload would take the deleted one's number, and a button
    /// still holding that number would start running it.
    /// </remarks>
    [Fact]
    public void TheLastSequenceNumberIsWrittenOnlyWhenTheHighestIsGone()
    {
        var program = SystemCalled("", "Airlock");
        var system = program.Systems[0];
        program.AddSequence(system);
        var two = program.AddSequence(system);
        Assert.DoesNotContain("last", Save(program));

        program.RemoveSequence(two);
        var text = Save(program);
        Assert.Contains(" as Airlock last sequence 2 {", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.Equal(3, after.AddSequence(after.Systems[0]).Number);
    }

    /// <summary>
    /// A sequence in the block with a higher number than the failsafe wins over it.
    /// </summary>
    [Fact]
    public void ASequenceHigherThanTheLastNumberWins()
    {
        var after = new Automation();
        Assert.Equal("", Load(Head + "Airlock last sequence 3 {\n    sequence 7 {\n    }\n}\n", after));

        Assert.Equal(7, after.Systems[0].LastSequenceNumber);
        Assert.DoesNotContain("last", Save(after));
    }

    [Theory]
    [InlineData("last 4")]
    [InlineData("last sequence")]
    [InlineData("last sequence \"4\"")]
    public void ALastSequenceThatDoesNotSayANumberIsTrouble(string part)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(Head + "Airlock " + part + " {\n}\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>
    /// A step reads as what it does, so an action named after one of the format's words is quoted.
    /// </summary>
    /// <remarks>
    /// Which is how the language has no reserved words at all. A host may name an action anything, and
    /// what it happens to collide with is not its problem.
    /// </remarks>
    [Fact]
    public void AnActionNamedAfterAKeywordIsQuoted()
    {
        var program = SystemCalled("", "Airlock");
        var sequence = program.AddSequence(program.Systems[0]);

        var odd = new Act("every");
        odd.Targets.Add("thing");
        sequence.Steps.Add(odd);

        var text = Save(program);
        Assert.Contains("'every' [ thing ]\n", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.Equal("every", Assert.IsType<Act>(after.Sequences[0].Steps[0]).ActionId);
    }

    /// <summary>
    /// A block key no kind here can hold is trouble, rather than a key quietly fewer.
    /// </summary>
    /// <remarks>
    /// The keys are how a block is found again, so one dropped on the way in is one missing from the
    /// next save: the block matches less than it did, and nothing said so.
    /// </remarks>
    /// <summary>A block's path is a list of cells among its keys, and it comes back as it was written.</summary>
    [Fact]
    public void APathAmongABlocksKeysComesBackAsWritten()
    {
        const string written = "sprocket 1 alpha 7\n\nblock 'Door/LargeBlockDoor' door1 "
            + "[ 74857693847562934, [ (0 2 -1), (3 0 0) ], \"Hangar Door\" ]\n";

        var after = new Automation();
        Assert.Equal("", Load(written, after));

        var keys = after.FindRef("door1")!.Keys;
        Assert.Equal(3, keys.Count);
        Assert.Equal(new[] { new VRageMath.Vector3I(0, 2, -1), new VRageMath.Vector3I(3, 0, 0) }, keys[1].Cells);
        Assert.Equal(written, Save(after));
    }

    /// <summary>A list among a block's keys that is not a list of cells is trouble rather than a path.</summary>
    [Theory]
    [InlineData("[ 1, 2 ]")]
    [InlineData("[ (0 2 0.5) ]")]
    [InlineData("[]")]
    public void AListThatIsNotCellsIsTrouble(string key)
    {
        var after = new Automation();
        Assert.NotEqual("", Load("sprocket 1 alpha 7\n\nblock 'Door/LargeBlockDoor' door1 [ 7, " + key + " ]\n", after));
        Assert.False(after.Intact);
    }

    [Fact]
    public void AKeyThisBuildCannotHoldIsTrouble()
    {
        var after = new Automation();
        Assert.NotEqual("", Load(
            "sprocket 1 alpha 7\n\nblock 'Door/LargeBlockDoor' door1 [ 74857693847562934, (3 0 -2) ]\n", after));

        Assert.False(after.Intact);
    }

    /// <summary>
    /// A statement nobody recognises is dropped whole, with everything under it.
    /// </summary>
    /// <remarks>
    /// The reason the format has braces. A line-oriented one could only skip the line, so an unknown
    /// construct left its children behind to be read as somebody else's - a gate landing on the wrong
    /// step. It is also why a step always says <c>wait</c> or <c>now</c>: without it, a construct a
    /// later build adds would be read as a step doing something called "run".
    /// </remarks>
    [Fact]
    public void AConstructThisBuildDoesNotKnowIsSkippedAndTheRestSurvives()
    {
        var text = Save(Airlock())
            .Replace("        close [ inner ]\n",
                "        close [ inner ]\n" +
                "        every 3 {\n" +
                "            whatever [ inner ]\n" +
                "        }\n");

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.True(after.Intact);

        // Its five lines, and neither the statement headed by a word a step cannot be, nor the
        // block under it.
        Assert.Equal(5, after.Sequences[0].Steps.Count);
    }

    /// <summary>
    /// A name escapes only its own quote, and text only its own.
    /// </summary>
    /// <remarks>
    /// Single quotes for a name and double for text, each doubling only the quote that would end it, so a
    /// double quote inside a name and a single quote inside text need nothing at all.
    /// </remarks>
    [Fact]
    public void EachQuoteEscapesOnlyItself()
    {
        var program = SystemCalled("", "Mal's \"big\" airlock");
        var sequence = program.AddSequence(program.Systems[0]);
        sequence.Name = "Mal's \"other\" sequence";

        var text = Save(program);
        Assert.Contains(" as 'Mal''s \"big\" airlock' {", text);
        Assert.Contains("sequence 1 as 'Mal''s \"other\" sequence' {", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.Equal("Mal's \"big\" airlock", after.Systems[0].Name);
        Assert.Equal("Mal's \"other\" sequence", after.Sequences[0].Name);
    }

    /// <summary>
    /// Every sort of value a key can be, out and back.
    /// </summary>
    /// <remarks>
    /// The kind is the syntax here - a whole number reads back as one because that is what it looks
    /// like - so this is the test that the kinds are told apart at all. The integer is the one that
    /// matters: a double holds whole numbers exactly only to about 9.0x10^15, and an id is longer than
    /// that.
    /// </remarks>
    [Fact]
    public void EverySortOfKeySurvives()
    {
        var program = new Automation();
        var values = new Values();
        var declared = new EntityRef("thing", "kind");
        declared.Keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(74857693847562934L)));
        declared.Keys.Add(new EntityKey(ValueKind.Real, EntityValue.Of(0.25d)));
        declared.Keys.Add(new EntityKey(ValueKind.Real, EntityValue.Of(1E-05d)));
        declared.Keys.Add(new EntityKey(ValueKind.Text, values.Text("Airlock Inner Door")));
        declared.Keys.Add(new EntityKey(ValueKind.Flag, EntityValue.Of(true)));
        declared.Keys.Add(new EntityKey(ValueKind.Color, EntityValue.Of(255, 136, 0, 204)));
        program.Restore(declared);

        var text = ProgramText.Save(program, values);
        Assert.Contains("[ 74857693847562934, 0.25, 1E-05, \"Airlock Inner Door\", on, #FF8800CC ]", text);

        var read = new Values();
        var after = new Automation();
        Assert.Equal("", ProgramText.Load(text, after, read));

        var keys = after.Refs[0].Keys;
        Assert.Equal(6, keys.Count);

        long whole;
        Assert.True(read.TryInteger(keys[0].Kind, keys[0].Value, out whole));
        Assert.Equal(74857693847562934L, whole);

        double real;
        Assert.True(read.TryReal(keys[1].Kind, keys[1].Value, out real));
        Assert.Equal(0.25d, real);
        Assert.True(read.TryReal(keys[2].Kind, keys[2].Value, out real));
        Assert.Equal(1E-05d, real);

        string name;
        Assert.True(read.TryText(keys[3].Kind, keys[3].Value, out name));
        Assert.Equal("Airlock Inner Door", name);

        Assert.True(read.Flag(keys[4].Kind, keys[4].Value));

        byte r, g, b, a;
        Assert.True(read.TryColor(keys[5].Kind, keys[5].Value, out r, out g, out b, out a));
        Assert.Equal(255, r);
        Assert.Equal(136, g);
        Assert.Equal(0, b);
        Assert.Equal(204, a);
    }

    /// <summary>
    /// A script that will not read keeps what it can, says what was wrong, and refuses to be saved.
    /// </summary>
    /// <remarks>
    /// The one rule that matters most in the whole format. Saving runs whenever the program might have
    /// changed, so a half-read load followed by any edit at all would write the damage over the only
    /// copy there is.
    /// </remarks>
    [Theory]
    [InlineData("        close [ inner\n")]
    [InlineData("        close \"inner ]\n")]
    [InlineData("        close [ inner ] !\n")]
    public void TroubleIsReportedAndTheProgramWillNotBeSavedOverTheSave(string line)
    {
        var text = Save(Airlock()).Replace("        close [ inner ]\n", line);

        var after = new Automation();
        var trouble = Load(text, after);

        Assert.NotEqual("", trouble);
        Assert.False(after.Intact);

        // And the rest of it is still there, which is the other half of the promise.
        Assert.Single(after.Systems);
        Assert.Equal("Airlock", after.Systems[0].Name);
        Assert.Equal(4, after.Sequences[0].Steps.Count);
    }

    [Fact]
    public void AnAnnotationWithNothingUnderItIsTrouble()
    {
        var after = new Automation();
        Assert.NotEqual("", Load("sprocket 1 alpha 7\n\n# about nothing at all\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>
    /// A save in any other version is turned away whole and never written back.
    /// </summary>
    /// <remarks>
    /// Not backwards compatible while the language is being designed, so nothing of an older save is
    /// read - but nothing on disk is lost, because trouble stops the program being saved over it.
    /// </remarks>
    [Fact]
    public void ASaveFromAnotherVersionIsTurnedAwayAndNotWrittenBack()
    {
        var after = new Automation();
        var trouble = Load(Save(Airlock()).Replace("sprocket 1 alpha 7", "sprocket 2"), after);

        Assert.NotEqual("", trouble);
        Assert.False(after.Intact);
        Assert.True(after.Incompatible);
        Assert.Empty(after.Systems);
    }

    /// <summary>
    /// A save from another draft of the same format is refused the same way.
    /// </summary>
    /// <remarks>
    /// The two numbers oblige different things and that is the whole reason there are two: the format
    /// moving means a program somebody else holds has to be brought forward, and a draft moving means only that this build will not read it. Both are refused at the first line, where the refusal can
    /// say something, rather than deep in the file where it cannot.
    /// </remarks>
    [Theory]
    [InlineData("sprocket 1 alpha 6")]
    [InlineData("sprocket 1 alpha 8")]
    [InlineData("sprocket 1")]
    public void ASaveFromAnotherDraftOfTheFormatIsRefused(string head)
    {
        var after = new Automation();
        var trouble = Load(Save(Airlock()).Replace("sprocket 1 alpha 7", head), after);

        Assert.NotEqual("", trouble);
        Assert.False(after.Intact);
        Assert.True(after.Incompatible);
        Assert.Empty(after.Systems);
    }

    [Fact]
    public void SomethingThatIsNotAProgramIsNotReadAsOne()
    {
        var after = new Automation();
        Assert.NotEqual("", Load("system \"" + Identity + "\" as Airlock {\n}\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>
    /// A name is one line, because a statement ends at the end of its line.
    /// </summary>
    /// <remarks>
    /// Text that genuinely wants lines is an annotation, which is written a line at a time. A name
    /// holding a newline would write a save that cannot be read back, so the model folds it - see
    /// <see cref="Naming.Clean" /> - and the writer folds it again, because a writer that can produce
    /// an unreadable save eventually does.
    /// </remarks>
    [Fact]
    public void ANameWithALineBreakInItFolds()
    {
        Assert.Equal("one two", Naming.Clean("one\ntwo"));

        var after = new Automation();
        Assert.Equal("", Load(Save(SystemCalled("", "one\ntwo")), after));
        Assert.Equal("one two", after.Systems[0].Name);
    }

    /// <summary>
    /// A name holding a dash is quoted, because a dash is subtraction.
    /// </summary>
    /// <remarks>
    /// The half that matters is the writer. Were it to leave such a name bare, <c>Air-lock</c> would
    /// read back as a subtraction the moment expressions exist, so a save this wrote could not be read
    /// by the thing that wrote it.
    /// </remarks>
    [Fact]
    public void ANameHoldingADashIsQuoted()
    {
        var text = Save(SystemCalled("Mal-Tek", "Air-lock"));
        Assert.Contains("as 'Mal-Tek'.'Air-lock' {", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.True(after.Systems[0].Is("Mal-Tek", "Air-lock"));
    }

}
