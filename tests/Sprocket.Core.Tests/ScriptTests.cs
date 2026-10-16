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

    const string Version = "sprocket 1 alpha 15\n\n";

    const string Head = Version + "system \"" + Identity + "\" as ";

    const string Blueprint = Version + "system blueprint \"" + Identity + "\" as ";

    /// <summary>The identity of a stored blueprint a system blueprint was made from.</summary>
    const string Origin = "9d0f1a2b-3c4d-4e5f-8a6b-7c8d9e0f1a2b";

    /// <summary>Three doors a program may name.</summary>
    const string Doors = "let door1 = block 'Door/LargeBlockDoor' [ 1 ]\nlet door2 = block 'Door/LargeBlockDoor' [ 2 ]\n"
        + "let door3 = block 'Door/LargeBlockDoor' [ 3 ]\n";

    static readonly Values Store = new Values();

    static string Save(Automation program) => ProgramText.Save(program, Store);

    static string Load(string text, Automation program) => ProgramText.Load(text, program, Store);

    /// <summary>The same three doors before a system blueprint, so the blueprint's instances may answer with them.</summary>
    const string DoorsBlueprint = Version + Doors + "\nsystem blueprint \"" + Identity + "\" as ";

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
        system.IsBlueprint = true;

        var part = system.PartFor("inner");
        part.Annotation = "The door onto the ship.";
        part.Expects = "Door";

        system.AddInstance().Fill("inner", new[] { "inner" });

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

    /// <summary>A variable nothing names is not written, whether it holds a block or a trigger.</summary>
    [Fact]
    public void AVariableNothingNamesIsNotWritten()
    {
        var after = new Automation();
        Assert.Equal("", Load(Version + "let door1 = block 'Door/LargeBlockDoor' [ 1 ]\n"
            + "let spare = block 'Door/LargeBlockDoor' [ 2 ]\n"
            + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 3 ]\n"
            + "let button = ext.toolbar(1, 1, 4) on [ panel1 ]\n\n" + Using, after));

        var text = Save(after);
        Assert.Contains("let door1 = ", text);
        Assert.DoesNotContain("spare", text);
        Assert.DoesNotContain("panel1", text);
        Assert.DoesNotContain("button", text);
    }

    /// <summary>A system closing <c>door1</c>, written as the writer writes it, so a variable written above it is kept.</summary>

    const string Using = "system \"" + Identity + "\" as Airlock {\n    sequence 1 {\n        call ext.close on [ door1 ]\n    }\n}\n";

    /// <summary>Gives <paramref name="program" /> a system that closes <paramref name="name" />, since a variable nothing names is not written.</summary>
    static void Uses(Automation program, string name)
    {
        var system = program.AddSystem();
        var close = new Act("close");
        close.Targets.Add(name);
        program.AddSequence(system).Steps.Add(close);
    }

    /// <summary>Gives the first system of <paramref name="program" /> a step that closes <paramref name="slots" />, since a slot nothing uses is not written.</summary>
    static void Closes(Automation program, params string[] slots)
    {
        var close = new Act("close");
        foreach (var slot in slots)
            close.Targets.Add(slot);
        program.AddSequence(program.Systems[0]).Steps.Add(close);
    }

    static Automation BlueprintCalled(string space, string name)

    {
        var program = SystemCalled(space, name);
        program.Systems[0].IsBlueprint = true;
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
            "sprocket 1 alpha 15\n" +
            "\n" +
            "let inner = block 'Door/LargeBlockDoor' [ 74857693847562934, \"Airlock Inner Door\" ]\n" +
            "\n" +
            "# Cycles an airlock.\n" +
            "#\n" +
            "# Nothing opens on both sides at once.\n" +
            "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock changed \"2026-09-14T19:02:11Z\" {\n" +
            "    # The door onto the ship.\n" +
            "    required slot inner expects Door\n" +
            "\n" +
            "    when any ext.pressed on [ inner ] == on then run 1\n" +
            "\n" +
            "    sequence 1 as 'Let me out' {\n" +
            "        call ext.close on [ inner ]\n" +
            "        await\n" +
            "        wait 60t\n" +
            "\n" +
            "        # The vent needs the full cycle.\n" +
            "        call ext.open on [ inner ]\n" +
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
    /// A system on the program's own construct, written down in full: the variables it names, a trigger among them, and the
    /// system naming them directly.
    /// </summary>
    [Fact]
    public void ASystemOnItsOwnConstructReadsAsAScript()
    {
        var program = new Automation();
        var door = new EntityRef("inner", "Door/LargeBlockDoor");
        door.Keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(74857693847562934L)));
        program.Restore(door);
        var panel = new EntityRef("panel1", "ButtonPanel/ButtonPanelLarge");
        panel.Keys.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(74857693847562937L)));
        program.Restore(panel);
        program.DeclareSignal("panel1", new ToolbarSlot(1, 1, 4), "button");

        var system = program.AddSystem();
        system.Identity = new Guid(Identity);
        system.Namespace = "MalTek";
        system.Name = "Airlock";

        var sequence = program.AddSequence(system);
        var close = new Act("close");
        close.Targets.Add("inner");
        sequence.Steps.Add(close);

        var pressed = SignalTrigger.Named("button");
        pressed.Does = new Run(sequence.Number);
        system.AddTrigger(pressed);

        const string written =
            "sprocket 1 alpha 15\n" +
            "\n" +
            "let inner = block 'Door/LargeBlockDoor' [ 74857693847562934 ]\n" +
            "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 74857693847562937 ]\n" +
            "let button = ext.toolbar(1, 1, 4) on [ panel1 ]\n" +
            "\n" +
            "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock {\n" +
            "    whenever button then run 1\n" +
            "\n" +
            "    sequence 1 {\n" +
            "        call ext.close on [ inner ]\n" +
            "    }\n" +
            "}\n";
        Assert.Equal(written, Save(program));

        var after = new Automation();
        Assert.Equal("", Load(written, after));
        Assert.False(after.Systems[0].IsBlueprint);
        Assert.Empty(after.Systems[0].Instances);
        Assert.Equal(written, Save(after));
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
    [InlineData("sprocket 1 alpha 15\n\nsystem Airlock {\n}\n")]
    [InlineData("sprocket 1 alpha 15\n\nsystem \"airlock\" as Airlock {\n}\n")]
    [InlineData("sprocket 1 alpha 15\n\nsystem \"00000000-0000-0000-0000-000000000000\" as Airlock {\n}\n")]
    public void ASystemWithoutAnIdentityIsTrouble(string text)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(text, after));
        Assert.False(after.Intact);
        Assert.Empty(after.Systems);
    }

    /// <summary>
    /// A length of time is a number and its unit as one word, in ticks or in seconds.
    /// </summary>
    /// <remarks>
    /// Loud rather than lenient about the unit: <c>600m</c> is somebody meaning minutes, and reading it as anything else
    /// would run their airlock at the wrong pace. Ticks are whole; seconds may have a fraction.
    /// </remarks>
    [Theory]
    [InlineData("wait 60t", "")]
    [InlineData("wait 60s", "")]
    [InlineData("wait 1.5s", "")]
    [InlineData("wait 60", "Line 5")]
    [InlineData("wait 60m", "Line 5")]
    [InlineData("wait 1.5t", "Line 5")]
    public void ADurationIsANumberAndItsUnit(string written, string trouble)
    {
        var after = new Automation();
        var said = Load(
            Head + "Airlock {\n    sequence 1 {\n        " + written + "\n    }\n}\n", after);

        Assert.StartsWith(trouble, said);
    }

    /// <summary>A length in seconds lasts the nearest whole tick, and is written back as it was written.</summary>
    [Theory]
    [InlineData("wait 2s", 120)]
    [InlineData("wait 1.5s", 90)]
    [InlineData("wait 0.25s", 15)]
    [InlineData("wait 0.01s", 1)]
    [InlineData("wait 120t", 120)]
    public void ALengthInSecondsIsKeptAndRounded(string written, int ticks)
    {
        var after = new Automation();
        Assert.Equal("", Load(Head + "Airlock {\n    sequence 1 {\n        " + written + "\n    }\n}\n", after));

        var wait = Assert.IsType<Wait>(after.Systems[0].Sequences[0].Steps[0]);
        Assert.Equal(ticks, wait.Ticks);
        Assert.Contains("\n        " + written + "\n", Save(after).Replace("\r\n", "\n"));
    }

    /// <summary>A repeat takes seconds as a wait does, and keeps them.</summary>
    [Fact]
    public void ARepeatKeepsItsSeconds()
    {
        var text = Head + "Airlock {\n    every 10s run 1\n    sequence 1 {\n        wait 1t\n    }\n}\n";
        var after = new Automation();
        Assert.Equal("", Load(text, after));

        var every = Assert.IsType<EveryTrigger>(after.Systems[0].Triggers[0]);
        Assert.Equal(600, every.IntervalTicks);
        Assert.Contains("every 10s run 1", Save(after));
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
    /// A part says what may fill it as one block type, a type the game builds others on as readily as any: <c>DoorBase</c>
    /// takes every door.
    /// </summary>
    [Fact]
    public void APartSaysWhatMayFillIt()
    {
        var program = BlueprintCalled("", "Airlock");
        var system = program.Systems[0];

        var inner = system.PartFor("inner");
        inner.Annotation = "The inner door.";
        inner.Expects = "DoorBase";

        system.PartFor("vent").Expects = "AirVent";
        system.PartFor("anything");
        Closes(program, "inner", "vent", "anything");

        var text = Save(program);
        Assert.Contains("    # The inner door.\n    required slot inner expects DoorBase\n", text);
        Assert.Contains("    required slot vent expects AirVent\n", text);
        Assert.Contains("    required slot anything\n", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));

        var read = after.Systems[0];
        Assert.Equal("DoorBase", read.FindPart("inner")!.Expects);
        Assert.Equal("The inner door.", read.FindPart("inner")!.Annotation);
        Assert.Equal("AirVent", read.FindPart("vent")!.Expects);
        Assert.Equal("", read.FindPart("anything")!.Expects);
    }

    /// <summary>A slot expects one type, so a list after <c>expects</c> is a mistake the reader names.</summary>
    [Fact]
    public void ASlotExpectingAListIsAMistake()
    {
        var program = BlueprintCalled("", "Airlock");
        program.Systems[0].PartFor("inner").Expects = "Door";
        Closes(program, "inner");
        var text = Save(program).Replace("expects Door\n", "expects [ Door, AirtightSlideDoor ]\n");

        Assert.Contains("expected signal, one block type or a type the host provides after 'expects'.", Load(text, new Automation()));
    }

    /// <summary>A part a construct may leave unfilled is written optional, and read back as one.</summary>
    [Fact]
    public void APartMayBeOptional()
    {
        var program = BlueprintCalled("", "Airlock");
        var system = program.Systems[0];
        system.PartFor("inner").Expects = "Door";
        var lamp = system.PartFor("lamp");
        lamp.Expects = "InteriorLight";
        lamp.Optional = true;
        Closes(program, "inner", "lamp");

        var text = Save(program);
        Assert.Contains("    required slot inner expects Door\n", text);
        Assert.Contains("    optional slot lamp expects InteriorLight\n", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.False(after.Systems[0].FindPart("inner")!.Optional);
        Assert.True(after.Systems[0].FindPart("lamp")!.Optional);
    }

    /// <summary>A slot nothing uses and no installation holds blocks in is not written, and neither is an instance's answer to it.</summary>
    [Fact]
    public void ASlotNothingUsesIsNotWritten()
    {
        var program = BlueprintCalled("", "Airlock");
        var system = program.Systems[0];
        system.PartFor("inner");
        var spare = system.PartFor("spare");
        spare.Annotation = "Written about, and still unused.";
        Closes(program, "inner");

        var instance = system.AddInstance();
        instance.Fill("inner", new[] { "door1" });
        instance.Fill("spare", new string[0]);

        var text = Save(program);
        Assert.Contains("required slot inner", text);
        Assert.DoesNotContain("spare", text);
    }

    /// <summary>A slot nothing uses that an installation holds blocks in is kept with the construct, and left out of the blueprint.</summary>
    [Fact]
    public void ASlotAnInstallationHoldsIsWrittenWithTheConstruct()
    {
        var program = BlueprintCalled("", "Airlock");
        var system = program.Systems[0];
        system.PartFor("inner");
        system.PartFor("spare");
        Closes(program, "inner");

        var instance = system.AddInstance();
        instance.Fill("inner", new[] { "door1" });
        instance.Fill("spare", new[] { "door2" });

        var text = Save(program);
        Assert.Contains("required slot spare", text);
        Assert.Contains("spare = [ door2 ]", text);
        Assert.DoesNotContain("spare", ProgramText.Blueprint(system));
    }

    /// <summary>
    /// A subtype is quoted, because a `/` is not a bare name.
    /// </summary>
    [Fact]
    public void AnExactBlockTypeIsQuoted()
    {
        var program = BlueprintCalled("", "Hangar");
        program.Systems[0].PartFor("door").Expects = "AirtightHangarDoor/LargeBlockAirtightHangarDoor";
        Closes(program, "door");

        var text = Save(program);
        Assert.Contains("required slot door expects 'AirtightHangarDoor/LargeBlockAirtightHangarDoor'\n", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.Equal("AirtightHangarDoor/LargeBlockAirtightHangarDoor", after.Systems[0].FindPart("door")!.Expects);
    }

    [Theory]
    [InlineData("required inner expects Door")]
    [InlineData("required block inner expects Door")]
    [InlineData("required slot inner expects")]
    public void ARequiredLineThatSaysNothingUsefulIsTrouble(string line)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(Blueprint + "Airlock {\n    " + line + "\n}\n", after));
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
            DoorsBlueprint + "MalTek.Airlock {\n    required slot inner\n    required slot outer\n}\n\ninstance 2 of " + named
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
        Assert.NotEqual("", Load(Blueprint + "MalTek.Airlock {\n}\n\n" + line, after));
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
            DoorsBlueprint + "MalTek.Airlock {\n    required slot inner\n}\n\ninstance 1 of MalTek.Airlock {\n    fill inner [ door1 ]\n}\n",
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
            Blueprint + "MalTek.Airlock {\n" +
            "    required slot inside\n    required slot inner\n    required slot outer\n\n" +
            "    when any ext.pressed on [ inside ] == on then run 1\n" +
            "    when all ext.status on [ inner, outer ] == closed then run 1\n" +
            "    every 600t run 2\n\n" +
            "    sequence 1 {\n    }\n\n" +
            "    sequence 2 {\n    }\n" +
            "}\n", after));

        var system = Assert.Single(after.Systems);
        Assert.Equal(3, system.Triggers.Count);

        var inside = Assert.IsType<WhenTrigger>(system.Triggers[0]);
        Assert.Equal("pressed", inside.EventId);
        Assert.Equal(Quantifier.Any, inside.Quantifier);
        Assert.Equal(new[] { "inside" }, inside.Sources);
        Assert.Equal(1, inside.Runs);

        var shut = Assert.IsType<WhenTrigger>(system.Triggers[1]);
        Assert.Equal(Quantifier.All, shut.Quantifier);
        Assert.Equal(new[] { "inner", "outer" }, shut.Sources);
        Assert.Equal(1, shut.Runs);

        var clock = Assert.IsType<EveryTrigger>(system.Triggers[2]);
        Assert.Equal(600, clock.IntervalTicks);
        Assert.Equal(2, clock.Runs);

        // Both of the first two run sequence 1, and the sequence itself says nothing about either.
        Assert.Same(system.Triggers[0], system.Sequences[0].Trigger);

        // And it all comes back the same way.
        var text = Save(after);
        Assert.Contains("    when any ext.pressed on [ inside ] == on then run 1\n", text);
        Assert.Contains("    when all ext.status on [ inner, outer ] == closed then run 1\n", text);
        Assert.Contains("    every 600t run 2\n", text);
    }

    [Theory]
    [InlineData("when any ext.pressed [ inside ] run 1")]
    [InlineData("when pressed [ inside ] then run 1")]
    [InlineData("when any ext.pressed on [ inside ] == on then run")]
    [InlineData("every 600 run 1")]
    [InlineData("every 600t")]
    [InlineData("when any ext.pressed [ inside ] every 30 then run 1")]
    [InlineData("when any ext.pressed on [ inside ] == on every 0t then run 1")]
    [InlineData("when any ext.on on [ inside ] == on then run 1")]
    [InlineData("whatever [ inside ]")]
    [InlineData("whenever inside then run 1")]
    [InlineData("on button run 1")]
    [InlineData("on then run 1")]
    [InlineData("on [ button ] then run 1")]
    public void ATriggerThatSaysNothingUsefulIsTrouble(string line)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(
            Blueprint + "MalTek.Airlock {\n    required slot inside\n    optional slot button expects signal\n    " + line
            + "\n\n    sequence 1 {\n    }\n}\n", after));

        Assert.False(after.Intact);
    }

    /// <summary>A line in a sequence that is no step is trouble, and so is a step that does not say what it needs to.</summary>
    [Theory]
    [InlineData("close [ inner ]")]
    [InlineData("call ext.close [ inner ]")]
    [InlineData("act ext.close on [ inner ]")]
    [InlineData("call on [ inner ]")]
    [InlineData("stop")]
    public void ALineThatIsNoStepIsTrouble(string line)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(Blueprint + "MalTek.Airlock {\n    required slot inner\n\n    sequence 1 {\n        " + line
            + "\n    }\n}\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>A statement at the top of a program that is no variable, system or instance is trouble.</summary>
    [Fact]
    public void AStatementAtTheTopThatIsNothingKnownIsTrouble()
    {
        var after = new Automation();
        Assert.NotEqual("", Load(Head + "MalTek.Airlock {\n}\n\nwhatever [ inner ]\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>A step that stops a sequence is written <c>stop</c> and read back as one, by number or by name.</summary>
    [Fact]
    public void AStepMayStopAnotherSequence()
    {
        var after = new Automation();
        Assert.Equal("", Load(Head + "MalTek.Airlock {\n    sequence 1 {\n        stop 2\n        stop 'Warn'\n    }\n\n"
            + "    sequence 2 as Warn {\n    }\n}\n", after));

        var steps = after.Systems[0].FindSequence(1)!.Steps;
        Assert.True(Assert.IsType<Run>(steps[0]).Stops);
        Assert.Equal(2, Assert.IsType<Run>(steps[1]).Number);
        Assert.True(Assert.IsType<Run>(steps[1]).Stops);
        Assert.Contains("        stop 2\n        stop 2\n", Save(after));
    }

    /// <summary>A <c>when</c> may say how often its moment is looked for, and says nothing where it is left to the default.</summary>
    [Fact]
    public void AWhenMaySayHowOftenItLooks()
    {
        var after = new Automation();
        Assert.Equal("", Load(Blueprint + "MalTek.Airlock {\n    required slot vent\n    required slot inner\n\n"
            + "    when any ext.'room-status' on [ vent ] == full every 30t then run 1\n"
            + "    when any ext.status on [ inner ] == closed then run 1\n\n    sequence 1 {\n    }\n}\n", after));

        var triggers = after.Systems[0].Triggers;
        Assert.Equal(30, Assert.IsType<WhenTrigger>(triggers[0]).IntervalTicks);
        Assert.Equal(0, Assert.IsType<WhenTrigger>(triggers[1]).IntervalTicks);

        var text = Save(after);
        Assert.Contains("    when any ext.'room-status' on [ vent ] == full every 30t then run 1\n", text);
        Assert.Contains("    when any ext.status on [ inner ] == closed then run 1\n", text);
    }

    /// <summary>A trigger the construct provides is a slot that expects one, and an <c>on</c> says what it does.</summary>
    /// <remarks>Two of them running one sequence is the ordinary way to say "the inside button and the outside one".</remarks>
    [Fact]
    public void ATriggerTheConstructProvidesSaysWhatItRuns()
    {
        var program = BlueprintCalled("MalTek", "Airlock");
        var system = program.Systems[0];
        program.AddSequence(system);

        var inside = system.PartFor("inside");
        inside.IsSignal = true;
        inside.Annotation = "The call button inside.";
        var insideTrigger = SignalTrigger.Named("inside");
        insideTrigger.Does = new Run(1);
        system.AddTrigger(insideTrigger);

        var outside = system.PartFor("outside");
        outside.IsSignal = true;
        outside.Optional = true;
        var outsideTrigger = SignalTrigger.Named("outside");
        outsideTrigger.Does = new Run(1);
        system.AddTrigger(outsideTrigger);

        var text = Save(program);
        Assert.Contains(
            "    # The call button inside.\n    required slot inside expects signal\n    optional slot outside expects signal\n",
            text);
        Assert.Contains("    whenever inside then run 1\n\n    whenever outside then run 1\n", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));

        var read = after.Systems[0];
        Assert.Equal(2, read.Triggers.Count);
        Assert.All(read.Triggers, t => Assert.Equal(1, Assert.IsType<SignalTrigger>(t).Runs));
        Assert.Equal("inside", ((SignalTrigger)read.Triggers[0]).Part);
        Assert.Equal("The call button inside.", read.FindPart("inside")!.Annotation);
        Assert.True(read.FindPart("inside")!.IsSignal);
        Assert.False(read.FindPart("inside")!.Optional);
        Assert.True(read.FindPart("outside")!.Optional);
    }

    /// <summary>
    /// An instance answers a signal slot with host functions written in place, or with variables holding what one named,
    /// and they come back as written.
    /// </summary>
    [Fact]
    public void AnInstanceAnswersATriggerWithHostFunctions()
    {
        const string written = "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 1 ]\n"
            + "let panel2 = block 'ButtonPanel/ButtonPanelLarge' [ 2 ]\n"
            + "let halt = ext.toolbar(1, 3, 1) on [ panel2 ]\n";

        var after = new Automation();
        Assert.Equal("", Load(Version + written + "\n"
            + "system blueprint \"" + Identity + "\" as MalTek.Airlock {\n    optional slot button expects signal\n\n"
            + "    whenever button then run 1\n\n    sequence 1 {\n    }\n}\n\n"
            + "instance 1 of MalTek.Airlock {\n    button = [ ext.toolbar(1, 1, 4) on [ panel1 ], ext.toolbar(1, 2, 3) on [ panel2 ], halt ]\n}\n", after));

        var instance = after.Systems[0].Instances[0];
        Assert.Equal(new[] { "panel1", "panel2", "panel2" }, instance.FilledWith("button"));
        var at = instance.Positions("button")!;
        Assert.Equal(4, at[0].Slot);
        Assert.Equal(2, at[1].Page);
        Assert.Equal(3, at[1].Slot);
        Assert.Equal(3, at[2].Page);

        var text = Save(after);
        Assert.Contains(written, text);
        Assert.Contains("    button = [ ext.toolbar(1, 1, 4) on [ panel1 ], ext.toolbar(1, 2, 3) on [ panel2 ], halt ]\n", text);
    }

    /// <summary>A signal slot answered by anything but a signal is trouble, and so is a slot for a block answered by one.</summary>
    [Theory]
    [InlineData("button = [ panel1 ]")]
    [InlineData("button = [ ext.toolbar(1, 1) on [ panel1 ] ]")]
    [InlineData("button = [ ext.toolbar(0, 1, 4) on [ panel1 ] ]")]
    [InlineData("button = [ panel1:shelf(1, 1, 4) ]")]
    [InlineData("button = [ ext.toolbar(1, 1, 4) on [ panel9 ] ]")]
    [InlineData("inner = [ ext.toolbar(1, 1, 4) on [ panel1 ] ]")]
    [InlineData("inner = [ halt ]")]
    public void AnAnswerOfTheWrongKindIsTrouble(string line)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(Version + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 1 ]\nlet halt = ext.toolbar(1, 1, 2) on [ panel1 ]\n\n"
            + "system blueprint \"" + Identity + "\" as MalTek.Airlock {\n    required slot inner\n    optional slot button expects signal\n\n"
            + "    whenever button then run 1\n\n    sequence 1 {\n    }\n}\n\ninstance 1 of MalTek.Airlock {\n    " + line + "\n}\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>A system names its signals as it names its blocks: by the variables above that hold them.</summary>
    [Fact]
    public void ASystemIsStartedByATriggerVariable()
    {
        var after = new Automation();
        Assert.Equal("", Load(Version + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 1 ]\n"
            + "let button = ext.toolbar(1, 1, 4) on [ panel1 ]\n\n"
            + "system \"" + Identity + "\" as MalTek.Airlock {\n    whenever button then run 1\n\n    sequence 1 {\n    }\n}\n", after));

        var started = Assert.IsType<SignalTrigger>(Assert.Single(after.Systems[0].Triggers));
        Assert.Equal("button", started.Part);
        var held = after.FindSignalRef("button")!;
        Assert.Equal(new[] { "panel1" }, held.Blocks);
        Assert.Equal(4, held.At.Slot);
    }

    /// <summary>A signal variable that is not one, or is used before it is defined, is trouble, and so is a signal variable named as a block.</summary>
    [Theory]
    [InlineData("let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 1 ]\n", "    whenever button then run 1\n")]
    [InlineData("let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 1 ]\n", "    whenever panel1 then run 1\n")]
    [InlineData("let button = ext.toolbar(1, 1, 4) on [ panel1 ]\nlet panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 1 ]\n", "    whenever button then run 1\n")]
    [InlineData("let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 1 ]\nlet button = ext.toolbar(1, 1, 4) on [ panel1 ]\n", "    whenever ext.'finishes-closing' on [ button ] then run 1\n")]
    [InlineData("let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 1 ]\nlet button = ext.toolbar(1, 1, 4) on [ panel1 ]\n", "    every 600t call ext.close on [ button ]\n")]
    public void ATriggerVariableThatIsNoneIsTrouble(string lets, string trigger)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(Version + lets + "\nsystem \"" + Identity + "\" as MalTek.Airlock {\n" + trigger
            + "\n    sequence 1 {\n    }\n}\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>A step switched off is written and read whole, after <c>//</c>, with the words above it.</summary>
    [Fact]
    public void AStepSwitchedOffIsReadAndWrittenWhole()
    {
        var text = Blueprint + "MalTek.Airlock {\n    required slot inner\n\n    sequence 1 {\n"
            + "        call ext.close on [ inner ]\n"
            + "        await\n\n"
            + "        # Not yet\n"
            + "        // call ext.open(2) on [ inner ]\n"
            + "        // await\n"
            + "        wait 60t\n"
            + "    }\n}\n";

        var after = new Automation();
        Assert.Equal("", Load(text, after));

        var steps = after.Sequences[0].Steps;
        Assert.False(steps[0].IsOff);
        var off = Assert.IsType<Act>(steps[2]);
        Assert.True(off.IsOff);
        Assert.Equal("open", off.ActionId);
        Assert.Equal(new[] { "inner" }, off.Targets);
        Assert.Equal(2, off.Arguments[0].Whole);
        Assert.Equal("Not yet", off.Annotation);
        Assert.True(steps[3].IsOff);
        Assert.False(steps[4].IsOff);

        Assert.Contains("        # Not yet\n        // call ext.open(2) on [ inner ]\n        // await\n        wait 60t\n", Save(after));
    }

    /// <summary>A trigger does one step, written as a sequence writes it: run, stop, call or set.</summary>
    [Fact]
    public void ATriggerDoesAnyStepThatStartsSomething()
    {
        var after = new Automation();
        Assert.Equal("", Load(Blueprint + "MalTek.Airlock {\n    required slot outer\n    required slot light\n"
            + "    optional slot open expects signal\n    optional slot dim expects signal\n    optional slot halt expects signal\n\n"
            + "    whenever open then call ext.open on [ outer ]\n"
            + "    whenever dim then set Radius on [ light ] to 4.5\n"
            + "    whenever halt then stop 'Cycle'\n\n"
            + "    when all ext.status on [ outer ] == closed then call Reverse(2) on [ outer ]\n\n"
            + "    every 600t stop 1\n\n"
            + "    sequence 1 as Cycle {\n    }\n}\n", after));

        var triggers = after.Systems[0].Triggers;
        var open = Assert.IsType<Act>(triggers[0].Does);
        Assert.Equal("open", open.ActionId);
        Assert.Equal(Nature.Native, open.Form);
        Assert.Equal(new[] { "outer" }, open.Targets);

        var dim = Assert.IsType<Act>(triggers[1].Does);
        Assert.True(dim.Sets);
        Assert.Equal(4.5, dim.Arguments[0].Real);

        Assert.Equal(1, triggers[2].Runs);
        Assert.True(triggers[2].Stops);

        var reverse = Assert.IsType<Act>(Assert.IsType<WhenTrigger>(triggers[3]).Does);
        Assert.Equal(Nature.Action, reverse.Form);
        Assert.Equal(2, reverse.Arguments[0].Whole);

        Assert.True(Assert.IsType<EveryTrigger>(triggers[4]).Stops);

        var text = Save(after);
        Assert.Contains("    whenever open then call ext.open on [ outer ]\n", text);
        Assert.Contains("    whenever dim then set Radius on [ light ] to 4.5\n", text);
        Assert.Contains("    whenever halt then stop 1\n", text);
        Assert.Contains("    when all ext.status on [ outer ] == closed then call Reverse(2) on [ outer ]\n", text);
        Assert.Contains("    every 600t stop 1\n", text);
    }

    /// <summary>A trigger whose step is not one that starts something, or that names a sequence the system has not got, is trouble.</summary>
    [Theory]
    [InlineData("whenever button then await")]
    [InlineData("whenever button then wait 60t")]
    [InlineData("on button then")]
    [InlineData("whenever button then run 9")]
    [InlineData("whenever ext.'finishes-closing' on [ outer ] then stop 'Nothing'")]
    [InlineData("on ext.'finishes-closing' on [ outer ] then")]
    [InlineData("every 600t await")]
    [InlineData("every 600t call on [ outer ]")]
    public void ATriggerWhoseStepStartsNothingIsTrouble(string line)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(
            Blueprint + "MalTek.Airlock {\n    required slot outer\n    optional slot button expects signal\n    " + line
            + "\n\n    sequence 1 {\n    }\n}\n", after));

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
        Assert.Empty(after.Systems);
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
    /// Two of a system claiming the same number is trouble, and nothing of the program is kept.
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
            Blueprint + "MalTek.Airlock {\n}\n\ninstance 1 of MalTek.Airlock {\n}\n"
            + "\ninstance 1 of MalTek.Airlock {\n}\n", program));

        Assert.False(program.Intact);
        Assert.Empty(program.Systems);
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
        Assert.Empty(after.Sequences);
    }

    [Fact]
    public void TwoSystemsWithTheSameIdentityAreTrouble()
    {
        var after = new Automation();
        var trouble = Load(Head + "MalTek.Airlock {\n}\n\nsystem \"" + Identity + "\" as MalTek.Hangar {\n}\n",
            after);

        Assert.NotEqual("", trouble);
        Assert.False(after.Intact);
        Assert.Empty(after.Systems);
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
        Assert.Equal("Line 6: another system is also called MalTek.Airlock.", warning);
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

    /// <summary>A system's <c>changed</c> and <c>last sequence</c> may come in either order, each at most once.</summary>
    [Theory]
    [InlineData("changed \"2026-09-14T19:02:11Z\" last sequence 4")]
    [InlineData("last sequence 4 changed \"2026-09-14T19:02:11Z\"")]
    public void ASystemsMetadataComesInEitherOrder(string said)
    {
        var after = new Automation();
        Assert.Equal("", Load(Head + "Airlock " + said + " {\n}\n", after));

        Assert.Equal(new DateTime(2026, 9, 14, 19, 2, 11, DateTimeKind.Utc), after.Systems[0].Changed);
        Assert.Equal(4, after.Systems[0].LastSequenceNumber);

        // Written back in the writer's own order.
        Assert.Contains(" as Airlock changed \"2026-09-14T19:02:11Z\" last sequence 4 {", Save(after));
    }

    /// <summary>Either of them said twice is trouble.</summary>
    [Theory]
    [InlineData("changed \"2026-09-14T19:02:11Z\" changed \"2026-09-15T19:02:11Z\"")]
    [InlineData("last sequence 4 changed \"2026-09-14T19:02:11Z\" last sequence 5")]
    public void ASystemsMetadataSaidTwiceIsTrouble(string said)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(Head + "Airlock " + said + " {\n}\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>
    /// A system blueprint's <c>from</c> names the blueprint it was made from and the version of it, may come before or after
    /// <c>changed</c>, and is written last.
    /// </summary>
    [Theory]
    [InlineData("changed \"2026-09-14T19:02:11Z\" from \"" + Origin + "\" \"2026-09-10T08:00:00Z\"")]
    [InlineData("from \"" + Origin + "\" \"2026-09-10T08:00:00Z\" changed \"2026-09-14T19:02:11Z\"")]
    public void ABlueprintsFromNamesWhatItWasMadeFrom(string said)
    {
        var after = new Automation();
        Assert.Equal("", Load(Blueprint + "MalTek.Airlock " + said + " {\n}\n", after));

        var system = after.Systems[0];
        Assert.Equal(new Guid(Origin), system.Origin);
        Assert.Equal(new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc), system.OriginChanged);
        Assert.Equal(new DateTime(2026, 9, 14, 19, 2, 11, DateTimeKind.Utc), system.Changed);
        Assert.Contains(" as MalTek.Airlock changed \"2026-09-14T19:02:11Z\" from \"" + Origin + "\" \"2026-09-10T08:00:00Z\" {",
            Save(after));
    }

    /// <summary>A <c>from</c> without the version names the blueprint alone.</summary>
    [Fact]
    public void AFromWithoutItsVersionNamesTheBlueprint()
    {
        var after = new Automation();
        Assert.Equal("", Load(Blueprint + "MalTek.Airlock from \"" + Origin + "\" last sequence 4 {\n}\n", after));

        Assert.Equal(new Guid(Origin), after.Systems[0].Origin);
        Assert.Null(after.Systems[0].OriginChanged);
        Assert.Equal(4, after.Systems[0].LastSequenceNumber);
    }

    /// <summary>A blueprint as the library keeps it carries no <c>from</c>; a logic system written alone with its link does.</summary>
    [Fact]
    public void ABlueprintAsStoredCarriesNoFrom()
    {
        var after = new Automation();
        Assert.Equal("", Load(Blueprint + "MalTek.Airlock from \"" + Origin + "\" \"2026-09-10T08:00:00Z\" {\n}\n", after));

        var system = after.Systems[0];
        Assert.DoesNotContain(" from ", ProgramText.Blueprint(system));
        Assert.Contains(" from \"" + Origin + "\" \"2026-09-10T08:00:00Z\" {", ProgramText.Linked(system));
    }

    /// <summary>A <c>from</c> that does not name a blueprint by its GUID, gives a version that is no time, comes twice, or heads a plain system, is trouble.</summary>
    [Theory]
    [InlineData(Blueprint + "MalTek.Airlock from {\n}\n")]
    [InlineData(Blueprint + "MalTek.Airlock from \"not a guid\" {\n}\n")]
    [InlineData(Blueprint + "MalTek.Airlock from \"" + Origin + "\" \"14/09/2026\" {\n}\n")]
    [InlineData(Blueprint + "MalTek.Airlock from \"" + Origin + "\" from \"" + Origin + "\" {\n}\n")]
    [InlineData(Head + "Airlock from \"" + Origin + "\" {\n}\n")]
    public void AFromThatNamesNoBlueprintIsTrouble(string text)
    {
        var after = new Automation();
        Assert.NotEqual("", Load(text, after));
        Assert.False(after.Intact);
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
    /// A game action named after one of the format's words is quoted, and one of Sprocket's own is not, since beside a dot
    /// only a name can stand.
    /// </summary>
    /// <remarks>
    /// Which is how the language has no reserved words at all. A host may name an action anything, and
    /// what it happens to collide with is not its problem.
    /// </remarks>
    [Fact]
    public void AnActionNamedAfterAKeywordIsQuoted()
    {
        var program = BlueprintCalled("", "Airlock");
        var sequence = program.AddSequence(program.Systems[0]);

        program.Systems[0].PartFor("thing");

        var odd = new Act("every");
        odd.Form = Nature.Action;
        odd.Targets.Add("thing");
        sequence.Steps.Add(odd);

        var own = new Act("every");
        own.Targets.Add("thing");
        sequence.Steps.Add(own);

        var text = Save(program);
        Assert.Contains("call 'every' on [ thing ]\n", text);
        Assert.Contains("call ext.'every' on [ thing ]\n", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        var game = Assert.IsType<Act>(after.Sequences[0].Steps[0]);
        Assert.Equal("every", game.ActionId);
        Assert.Equal(Nature.Action, game.Form);
        var native = Assert.IsType<Act>(after.Sequences[0].Steps[1]);
        Assert.Equal("every", native.ActionId);
        Assert.Equal(Nature.Native, native.Form);
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
        const string written = "sprocket 1 alpha 15\n\nlet door1 = block 'Door/LargeBlockDoor' "
            + "[ 74857693847562934, [ (0 2 -1), (3 0 0) ], \"Hangar Door\" ]\n\n"
            + Using;

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
        Assert.NotEqual("", Load("sprocket 1 alpha 15\n\nlet door1 = block 'Door/LargeBlockDoor' [ 7, " + key + " ]\n", after));
        Assert.False(after.Intact);
    }

    [Fact]
    public void AKeyThisBuildCannotHoldIsTrouble()
    {
        var after = new Automation();
        Assert.NotEqual("", Load(
            "sprocket 1 alpha 15\n\nlet door1 = block 'Door/LargeBlockDoor' [ 74857693847562934, (3 0 -2) ]\n", after));

        Assert.False(after.Intact);
    }

    /// <summary>A statement the language does not know is trouble, saying where, and nothing of the program is kept.</summary>
    /// <remarks>
    /// The program is marked damaged as well, so saving cannot write what was not understood out of the save.
    /// </remarks>
    [Fact]
    public void AStatementTheLanguageDoesNotKnowIsTrouble()
    {
        var text = Save(Airlock())
            .Replace("        call ext.close on [ inner ]\n",
                "        call ext.close on [ inner ]\n" +
                "        every 3 {\n" +
                "            whatever [ inner ]\n" +
                "        }\n");

        var after = new Automation();
        Assert.StartsWith("Line ", Load(text, after));
        Assert.False(after.Intact);
        Assert.Empty(after.Sequences);
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
        Uses(program, "thing");

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
    /// A script that will not read keeps none of itself, says what was wrong, and refuses to be saved.
    /// </summary>
    /// <remarks>
    /// The one rule that matters most in the whole format. Saving runs whenever the program might have
    /// changed, so a load followed by any edit at all would write over the only copy there is. A text
    /// with an error in it is trusted in no part, so nothing of it runs.
    /// </remarks>
    [Theory]
    [InlineData("        call ext.close on [ inner\n")]
    [InlineData("        close \"inner ]\n")]
    [InlineData("        call ext.close on [ inner ] !\n")]
    public void TroubleIsReportedAndTheProgramWillNotBeSavedOverTheSave(string line)
    {
        var text = Save(Airlock()).Replace("        call ext.close on [ inner ]\n", line);

        var after = new Automation();
        var trouble = Load(text, after);

        Assert.NotEqual("", trouble);
        Assert.False(after.Intact);
        Assert.Empty(after.Systems);
        Assert.Empty(after.Refs);
    }

    [Fact]
    public void AnAnnotationWithNothingUnderItIsTrouble()
    {
        var after = new Automation();
        Assert.NotEqual("", Load("sprocket 1 alpha 15\n\n# about nothing at all\n", after));
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
        var trouble = Load(Save(Airlock()).Replace("sprocket 1 alpha 15", "sprocket 2"), after);

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
    [InlineData("sprocket 1 alpha 11")]
    [InlineData("sprocket 1 alpha 12")]
    [InlineData("sprocket 1")]

    public void ASaveFromAnotherDraftOfTheFormatIsRefused(string head)
    {
        var after = new Automation();
        var trouble = Load(Save(Airlock()).Replace("sprocket 1 alpha 15", head), after);

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
