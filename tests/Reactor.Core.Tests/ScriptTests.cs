using Reactor.Sequences;
using Reactor.Storage;
using Reactor.World;
using Xunit;

namespace Reactor.Tests;

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
        system.Namespace = "MalTek";
        system.Name = "Airlock";
        system.Changed = new DateTime(2026, 9, 14, 19, 2, 11, DateTimeKind.Utc);
        system.Annotation = "Cycles an airlock.\n\nNothing opens on both sides at once.";

        var role = system.RoleFor("inner");
        role.Name = "the inner door";
        role.Instructions = "The door onto the ship.";

        system.Instances[0].Fill("inner", new[] { "inner" });

        var pressed = new WhenTrigger("pressed");
        pressed.Sources.Add("inner");
        var sequence = program.AddSequence(system, pressed);
        sequence.Name = "Let me out";

        var close = new Step("close");
        close.Targets.Add("inner");
        close.WaitsForCompletion = true;
        sequence.Steps.Add(close);

        var pause = new Step("");
        pause.Gate = new AfterGate(60);
        sequence.Steps.Add(pause);

        var open = new Step("open");
        open.Targets.Add("inner");
        open.Annotation = "The vent needs the full cycle.";
        var shut = new Condition("is-shut");
        shut.Entities.Add("inner");
        open.Gate = new OnlyIfGate(shut);
        sequence.Steps.Add(open);

        return program;
    }

    static Automation SystemCalled(string space, string name)
    {
        var program = new Automation();
        var system = program.AddSystem();
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
            "reactor 3\n" +
            "\n" +
            "block 'Door/LargeBlockDoor' inner [ 74857693847562934, \"Airlock Inner Door\" ]\n" +
            "\n" +
            "# Cycles an airlock.\n" +
            "#\n" +
            "# Nothing opens on both sides at once.\n" +
            "system MalTek.Airlock is on, changed \"2026-09-14T19:02:11Z\" {\n" +
            "    # The door onto the ship.\n" +
            "    role inner required \"the inner door\"\n" +
            "\n" +
            "    instance 1 on {\n" +
            "        fill inner [ inner ]\n" +
            "    }\n" +
            "\n" +
            "    sequence 1 on \"Let me out\" {\n" +
            "        when any pressed [ inner ]\n" +
            "        close [ inner ] wait\n" +
            "        step [] wait after 60\n" +
            "\n" +
            "        # The vent needs the full cycle.\n" +
            "        open [ inner ] wait onlyif is-shut [ inner ]\n" +
            "    }\n" +
            "}\n",
            Save(Airlock()));
    }

    /// <summary>
    /// A system is what it is called: its namespace and its name, whatever they contain.
    /// </summary>
    [Fact]
    public void ASystemIsItsNamespaceAndName()
    {
        var text = Save(SystemCalled("MalTek Inc.", "Airlock System"));
        Assert.Contains("system 'MalTek Inc.'.'Airlock System' is on {", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.True(after.Systems[0].Is("MalTek Inc.", "Airlock System"));
    }

    /// <summary>
    /// The dot is an operator, so the spaces around it are nothing and the name is always its parts.
    /// </summary>
    [Fact]
    public void SpacesAroundTheDotAreNothing()
    {
        var after = new Automation();
        Assert.Equal("", Load("reactor 3\n\nsystem 'MalTek Inc.'  .  Airlock is on {\n}\n", after));
        Assert.True(after.Systems[0].Is("MalTek Inc.", "Airlock"));

        // Written back tight, because the writer has one spelling for a name.
        Assert.Contains("system 'MalTek Inc.'.Airlock is on {", Save(after));
    }

    /// <summary>
    /// Two different names can never spell the same, which is what lets the spelling stand for a system.
    /// </summary>
    /// <remarks>
    /// Joined plainly, <c>a.b</c> and <c>c</c> would collide with <c>a</c> and <c>b.c</c>. The quoting is
    /// what keeps them apart, and a button's key relies on it.
    /// </remarks>
    [Fact]
    public void TwoDifferentNamesNeverSpellTheSame()
    {
        var one = SystemCalled("a.b", "c").Systems[0];
        var other = SystemCalled("a", "b.c").Systems[0];

        Assert.NotEqual(ProgramText.Identity(one), ProgramText.Identity(other));
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
        Assert.NotEqual("", Load("reactor 3\n\nsystem \"Airlock\" is on {\n}\n", after));
        Assert.False(after.Intact);
    }

    [Fact]
    public void TwoSystemsWithTheSameNameAreTrouble()
    {
        var after = new Automation();
        var trouble = Load("reactor 3\n\nsystem MalTek.Airlock is on {\n}\n\nsystem MalTek.Airlock is off {\n}\n",
            after);

        Assert.NotEqual("", trouble);
        Assert.False(after.Intact);
        Assert.Single(after.Systems);
    }

    [Fact]
    public void ASwitchSaysOnOrOff()
    {
        var after = new Automation();
        Assert.NotEqual("", Load("reactor 3\n\nsystem Airlock is maybe {\n}\n", after));
        Assert.False(after.Intact);

        // Still there, because a reader never loses more than it has to.
        Assert.Single(after.Systems);
    }

    /// <summary>
    /// Optional parts come after the comma, where a person looks for them.
    /// </summary>
    [Fact]
    public void AnOptionalPartComesAfterTheComma()
    {
        var after = new Automation();
        Assert.NotEqual("", Load("reactor 3\n\nsystem Airlock is on changed \"2026-09-14T19:02:11Z\" {\n}\n",
            after));
        Assert.False(after.Intact);
    }

    /// <summary>
    /// A changed time is ISO 8601 and nothing else, and is written back as UTC.
    /// </summary>
    [Fact]
    public void AChangedTimeIsIsoAndComesBackAsUtc()
    {
        var after = new Automation();
        Assert.Equal("", Load("reactor 3\n\nsystem Airlock is on, changed \"2026-09-14T21:02:11+02:00\" {\n}\n",
            after));
        Assert.Contains("system Airlock is on, changed \"2026-09-14T19:02:11Z\" {", Save(after));

        var local = new Automation();
        Assert.NotEqual("", Load("reactor 3\n\nsystem Airlock is on, changed \"14/09/2026\" {\n}\n", local));
        Assert.False(local.Intact);
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
        var sequence = program.AddSequence(program.Systems[0], new CalledTrigger());

        var odd = new Step("every");
        odd.Targets.Add("thing");
        sequence.Steps.Add(odd);

        var text = Save(program);
        Assert.Contains("'every' [ thing ] wait\n", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.Equal("every", after.Sequences[0].Steps[0].ActionId);
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
            .Replace("        close [ inner ] wait\n",
                "        close [ inner ] wait\n" +
                "        run \"Tidy up\"\n" +
                "        future 3 {\n" +
                "            whatever [ inner ] wait\n" +
                "        }\n");

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.True(after.Intact);
        Assert.Equal(3, after.Sequences[0].Steps.Count);
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
        program.Systems[0].RoleFor("inner").Name = "the \"inner\" door's";

        var text = Save(program);
        Assert.Contains("system 'Mal''s \"big\" airlock' is on {", text);
        Assert.Contains("\"the \"\"inner\"\" door's\"", text);

        var after = new Automation();
        Assert.Equal("", Load(text, after));
        Assert.Equal("Mal's \"big\" airlock", after.Systems[0].Name);
        Assert.Equal("the \"inner\" door's", after.Systems[0].FindRole("inner")!.Name);
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
    [InlineData("        close [ inner wait\n")]
    [InlineData("        close [ inner ] \"wait\n")]
    [InlineData("        close [ inner ] wait !\n")]
    public void TroubleIsReportedAndTheProgramWillNotBeSavedOverTheSave(string line)
    {
        var text = Save(Airlock()).Replace("        close [ inner ] wait\n", line);

        var after = new Automation();
        var trouble = Load(text, after);

        Assert.NotEqual("", trouble);
        Assert.False(after.Intact);

        // And the rest of it is still there, which is the other half of the promise.
        Assert.Single(after.Systems);
        Assert.Equal("Airlock", after.Systems[0].Name);
        Assert.Equal(2, after.Sequences[0].Steps.Count);
    }

    [Fact]
    public void AnAnnotationWithNothingUnderItIsTrouble()
    {
        var after = new Automation();
        Assert.NotEqual("", Load("reactor 3\n\n# about nothing at all\n", after));
        Assert.False(after.Intact);
    }

    /// <summary>
    /// A save in any other version is read as far as it goes and never written back.
    /// </summary>
    /// <remarks>
    /// Not backwards compatible while the language is being designed, so an older save does not load
    /// cleanly - but nothing on disk is lost, because trouble stops the program being saved over it.
    /// </remarks>
    [Fact]
    public void ASaveFromAnotherVersionIsReadAsFarAsItGoesAndNotWrittenBack()
    {
        var after = new Automation();
        var trouble = Load(Save(Airlock()).Replace("reactor 3", "reactor 2"), after);

        Assert.NotEqual("", trouble);
        Assert.False(after.Intact);
        Assert.Equal("Airlock", after.Systems[0].Name);
    }

    [Fact]
    public void SomethingThatIsNotAProgramIsNotReadAsOne()
    {
        var after = new Automation();
        Assert.NotEqual("", Load("system Airlock is on {\n}\n", after));
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
}
