using Sprocket.Host.SpaceEngineers.Features.Sharing.Text;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A system blueprint's settings: slots each instance answers with one value of a type, a default for an optional one, and
/// a setting standing in for a value, by its name or as <c>setting('Open angle')</c>.
/// </summary>
public class SettingTests
{
    const string Identity = "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43";

    const string Version = "sprocket 3 alpha 3\n\n";

    const string Head = "system blueprint \"" + Identity + "\" as MalTek.Airlock\n{\n";

    /// <summary>A blueprint with a setting of each type and each use a step makes of one, as the writer writes it.</summary>
    const string Settings = Head
        + "    required slot lamp expects InteriorLight\n"
        + "    optional slot 'Open angle' expects real = 45\n"
        + "    required slot 'Outer light' expects flag\n"
        + "    optional slot delay expects duration = 1.5s\n"
        + "    optional slot tint expects color = #FF8800FF\n"
        + "    optional slot greeting expects text = \"Hello\"\n"
        + "    optional slot count expects integer = 3\n"
        + "    optional slot home expects side = outside\n\n"
        + "    type side = inside | outside\n\n"
        + "    let where = inside\n"
        + "    let lit = off\n"
        + "    let start = home\n\n"
        + "    every delay run 2\n\n"
        + "    when 'Outer light' == lit then run 3\n\n"
        + "    when all Radius on [ lamp ] >= count every delay then run 4\n\n"
        + "    sequence 1 {\n"
        + "        set Radius on [ lamp ] to 'Open angle'\n"
        + "        set Color on [ lamp ] to tint\n"
        + "        call sprocket.label(greeting) on [ lamp ]\n"
        + "        set where to home\n"
        + "        wait delay\n"
        + "        if 'Outer light' == on then {\n"
        + "            set lit to on\n"
        + "        }\n"
        + "        else if where != home then {\n"
        + "            set lit to off\n"
        + "        }\n"
        + "        else if home == where then {\n"
        + "            set lit to off\n"
        + "        }\n"
        + "        if all Radius on [ lamp ] >= count then {\n"
        + "            await\n"
        + "        }\n"
        + "    }\n\n"
        + "    sequence 2 {\n"
        + "        set lit to on\n"
        + "    }\n\n"
        + "    sequence 3 {\n"
        + "        set lit to off\n"
        + "    }\n\n"
        + "    sequence 4 {\n"
        + "        set lit to on\n"
        + "    }\n"
        + "}\n";

    /// <summary>The blueprint with settings, and an instance answering some of them, as the writer writes it.</summary>
    const string Everything = Version
        + "let lamp1 = block 'InteriorLight/LargeBlockLight_1corner' [ 4 ]\n\n"
        + Settings + "\n"
        + "instance 1 of \"" + Identity + "\" as Port {\n"
        + "    'Open angle' = 85\n"
        + "    'Outer light' = on\n"
        + "    lamp = [ lamp1 ]\n"
        + "}\n";

    readonly Values _store = new();

    string Load(string text, Automation program) => ProgramReader.Load(text, program, _store);

    /// <summary>A blueprint whose slots and sequence are <paramref name="body" />, with one instance holding <paramref name="answers" />.</summary>
    static string Blueprint(string body, string answers = "") =>
        Version + Head + body + "}\n\n" + "instance 1 of \"" + Identity + "\" {\n" + answers + "}\n";

    /// <summary>Every setting, its default and every use of one reads, and is written back exactly as it was written.</summary>
    [Fact]
    public void EverySettingReadsAndIsWrittenBackAsItWas()
    {
        var program = new Automation();
        Assert.Equal("", Load(Everything, program));
        Assert.Equal(Everything, ProgramWriter.Save(program, _store));
    }

    /// <summary>A program copied, as the editor copies one, keeps its settings and every installation's answers.</summary>
    [Fact]
    public void ACopyKeepsTheSettingsAndTheirAnswers()
    {
        var program = new Automation();
        Assert.Equal("", Load(Everything, program));

        var into = new Values();
        var copy = new Automation();
        ProgramCopy.Into(program, _store, copy, into);

        Assert.Equal(Everything, ProgramWriter.Save(copy, into));
    }

    /// <summary>
    /// A setting's editor is read from its slot's <c>#@ editor</c> attribute, carried by a copy and written back; a name this build
    /// has no editor for is written back as it was read.
    /// </summary>
    [Theory]
    [InlineData("percent")]
    [InlineData("dial")]
    public void ASettingsEditorIsReadCopiedAndWrittenBack(string editor)
    {
        var text = Blueprint("    #@ editor: \"" + editor + "\"\n    optional slot opening expects real = 50\n").Replace(" {\n}\n", "\n");

        var program = new Automation();
        Assert.Equal("", Load(text, program));
        Assert.Equal(editor, program.Systems[0].FindPart("opening")!.Editor);
        Assert.Equal(text, ProgramWriter.Save(program, _store));

        var into = new Values();
        var copy = new Automation();
        ProgramCopy.Into(program, _store, copy, into);
        Assert.Equal(text, ProgramWriter.Save(copy, into));
    }

    /// <summary>A blueprint with settings shared as text comes back as it went.</summary>
    [Fact]
    public void ABlueprintWithSettingsIsSharedWhole()
    {
        var shared = Version + Settings;
        var decoded = TokenCode.Decode(TokenCode.Encode(shared))!;

        Assert.Equal("", ProgramReader.Lone(decoded, new Values(), out var system));
        Assert.Equal(shared, ProgramWriter.Blueprint(system));
    }

    /// <summary>A setting is a slot holding its type and default; an instance keeps its own answer, and an unanswered one is its default.</summary>
    [Fact]
    public void ASettingHoldsItsTypeAndDefaultAndAnInstanceItsAnswer()
    {
        var program = new Automation();
        Assert.Equal("", Load(Everything, program));
        var system = program.Systems[0];
        var instance = system.Instances[0];

        var angle = system.FindPart("Open angle")!;
        Assert.True(angle.IsSetting);
        Assert.Equal("real", angle.Setting);
        Assert.Equal(Literal.Of(45.0), angle.Default);
        Assert.Equal("side", system.FindPart("home")!.Setting);
        Assert.False(system.FindPart("lamp")!.IsSetting);
        Assert.False(system.FindPart("Outer light")!.HasDefault);

        Assert.True(instance.Gives("Open angle", out var answered));
        Assert.Equal(Literal.Of(85.0), answered);
        Assert.True(instance.Gives("home", out var home));
        Assert.Equal(Literal.Name("outside"), home);
        Assert.True(instance.Gives("delay", out var delay));
        Assert.Equal(90, delay.Length.Ticks);
        Assert.False(instance.AnswerTo("home", out _));
    }

    /// <summary>What the spec calls an error about settings is trouble, said on its line.</summary>
    [Theory]
    [InlineData("    optional slot angle expects real\n", "", "optional setting 'angle' needs a default")]
    [InlineData("    required slot angle expects real = 45\n", "", "required setting 'angle' takes no default")]
    [InlineData("    optional slot angle expects real = on\n", "", "setting 'angle' holds a real")]
    [InlineData("    optional slot lamp expects InteriorLight = 4\n", "", "only a setting takes a default")]
    [InlineData("    optional slot mode expects side = sideways\n    type side = inside | outside\n", "", "setting 'mode' holds a choice of side")]
    [InlineData("    required slot angle expects real\n", "    angle = [ 85 ]\n", "is answered with one value, not a list")]
    [InlineData("    required slot angle expects real\n", "    angle = \"far\"\n", "setting 'angle' holds a real")]
    [InlineData("    required slot angle expects real\n", "    angle = 85\n    angle = 90\n", "slot 'angle' is already answered")]
    [InlineData("    sequence 1 {\n        wait setting(gone)\n    }\n", "", "no setting called 'gone'")]
    [InlineData("    required slot lamp expects InteriorLight\n    sequence 1 {\n        wait setting(lamp)\n    }\n", "", "'lamp' is a slot for blocks; expected a setting")]
    [InlineData("    required slot angle expects real\n    sequence 1 {\n        wait angle\n    }\n", "", "setting 'angle' holds a real; expected a duration here")]
    [InlineData("    required slot lamp expects InteriorLight\n    required slot hi expects text\n    sequence 1 {\n        if any Radius on [ lamp ] > hi then {\n        }\n    }\n", "", "setting 'hi' holds text; expected a number here")]
    [InlineData("    required slot on_ expects integer\n    let lit = off\n    sequence 1 {\n        set lit to on_\n    }\n", "", "setting 'on_' holds an integer; expected a flag here")]
    [InlineData("    required slot lit expects flag\n    sequence 1 {\n        if lit == 3 then {\n        }\n    }\n", "", "unexpected 3; expected on or off")]
    [InlineData("    required slot lit expects flag\n    sequence 1 {\n        if lit >= on then {\n        }\n    }\n", "", "expected == or != after the setting")]
    [InlineData("    required slot lit expects flag\n    required slot n expects integer\n    sequence 1 {\n        if lit == n then {\n        }\n    }\n", "", "setting 'n' holds an integer; expected a flag here")]
    public void AnErrorTheSpecNamesIsTrouble(string body, string answers, string why)
    {
        Assert.Contains(why, Load(Blueprint(body, answers), new Automation()));
    }

    /// <summary>A plain system has no slots, and so no settings to name.</summary>
    [Fact]
    public void APlainSystemNamesNoSetting()
    {
        var text = Version + "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Door\n{\n"
            + "    sequence 1 {\n        wait setting(delay)\n    }\n}\n";

        Assert.Contains("only a system blueprint has settings", Load(text, new Automation()));
    }

    /// <summary><c>setting</c> is a kind and no keyword, so a state, a choice and a sequence may be called it; where they share it, each is written with its kind.</summary>
    [Fact]
    public void ANameSpelledSettingIsWrittenBare()
    {
        var text = Version + "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Door\n{\n"
            + "    type level = setting | high\n\n"
            + "    let lvl = level(setting)\n"
            + "    let setting = off\n\n"
            + "    sequence 1 as setting {\n"
            + "        set state(setting) to on\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));
        Assert.Equal(text, ProgramWriter.Save(program, _store));
    }

    /// <summary>A name that could mean more than one thing where it stands is trouble, and its qualified forms read.</summary>
    [Theory]
    [InlineData("        set where to inside\n", "'inside' could be the setting 'inside' or a choice of side")]
    [InlineData("        if where == inside then {\n        }\n", "'inside' could be the setting 'inside' or a choice of side")]
    [InlineData("        if outside == inside then {\n        }\n", "'inside' could be the setting 'inside' or a choice of side")]
    [InlineData("        set where to setting(inside)\n        set where to side(inside)\n        if outside == side(inside) then {\n        }\n", "")]
    public void AnAmbiguousNameIsTrouble(string steps, string why)
    {
        var text = Blueprint("    optional slot inside expects side = outside\n"
            + "    optional slot outside expects side = inside\n\n"
            + "    type side = inside | outside\n\n"
            + "    let where = side(outside)\n\n"
            + "    sequence 1 {\n" + steps + "    }\n");

        var program = new Automation();
        if (why.Length > 0)
        {
            Assert.Contains(why, Load(text, program));
            return;
        }

        Assert.Equal("", Load(text, program));
        Assert.Equal(text.Replace(" {\n}\n", "\n"), ProgramWriter.Save(program, _store));
    }

    /// <summary>A bare name that could be a state or a choice is trouble, and each reads, and is written back, qualified where it would be.</summary>
    [Theory]
    [InlineData("        set where to outside\n", "'outside' could be the state 'outside' or a choice of side; write the one meant with its kind: state(outside) or side(outside)")]
    [InlineData("        set valve to open\n", "'open' could be the state 'open' or a choice of tap")]
    [InlineData("        set setting(where) to side(outside)\n", "no state 'setting(where)' declared above this line.")]
    [InlineData("        set where to state(outside)\n        set where to side(outside)\n        set valve to tap(open)\n        set lit to open\n        if where == state(outside) then {\n        }\n", "")]
    public void AStateAndAChoiceOfOneNameAreQualified(string steps, string why)
    {
        var text = Version + "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Door\n{\n"
            + "    type side = inside | outside\n"
            + "    type tap = open | shut\n\n"
            + "    let outside = inside\n"
            + "    let open = off\n"
            + "    let where = inside\n"
            + "    let valve = shut\n"
            + "    let lit = off\n\n"
            + "    sequence 1 {\n" + steps + "    }\n}\n";

        var program = new Automation();
        if (why.Length > 0)
        {
            Assert.Contains(why, Load(text, program));
            return;
        }

        Assert.Equal("", Load(text, program));
        Assert.Equal(text, ProgramWriter.Save(program, _store));
    }

    /// <summary>
    /// A version 1 program, a draft or finished, read a bare name that was a state and a choice as the state; it reads so
    /// still, written <c>state(outside)</c>, and runs the same.
    /// </summary>
    [Theory]
    [InlineData("sprocket 1 alpha 28")]
    [InlineData("sprocket 1")]
    public void AnEarlierProgramsStateKeepsItsMeaning(string version)
    {
        var body = "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Door\n{\n"
            + "    type side = inside | outside\n\n"
            + "    let outside = side(inside)\n"
            + "    let where = side(outside)\n"
            + "    let same = off\n\n"
            + "    when where == outside then run 2\n\n"
            + "    sequence 1 {\n"
            + "        set where to outside\n"
            + "        if where != outside then {\n"
            + "            set where to outside\n"
            + "        }\n"
            + "    }\n\n"
            + "    sequence 2 {\n"
            + "        set same to on\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", Load(version + "\n\n" + body, program));

        var system = program.Systems[0];
        var runner = new AutomationRunner(program, new Rigged().World);
        runner.Tick(1);
        runner.Call(system.FindSequence(1)!, system.Uses[0]);
        Assert.Equal(Literal.Name("inside"), runner.States.Read(system, system.Uses[0], system.FindState("where")!));
        Assert.Equal(Literal.Of(true), runner.States.Read(system, system.Uses[0], system.FindState("same")!));

        var saved = ProgramWriter.Save(program, _store);
        Assert.Contains("    when where == state(outside) then run 2\n", saved);
        Assert.Contains("        set where to state(outside)\n", saved);
        Assert.Contains("        if where != state(outside) then {\n", saved);
        Assert.Contains("            set where to state(outside)\n", saved);
        Assert.Equal("", Load(saved, new Automation()));
    }

    /// <summary>
    /// Where the system has a type named like one of the game's block types, the block type is written <c>block_type(Door)</c> and
    /// the system's own <c>type(Door)</c>; a version 1 program's bare one, which could only be the block type, reads as it.
    /// </summary>
    [Theory]
    [InlineData("sprocket 1 alpha 28")]
    [InlineData("sprocket 1")]
    [InlineData("sprocket 2")]
    [InlineData("sprocket 3 alpha 3")]
    public void AGameTypeSharingATypesNameIsWrittenWithItsKind(string version)
    {
        // A rig stands behind the game's gateway and its kinds of object, which telling a block type by its name asks.
        _ = new Rigged();
        var text = version + "\n\nsystem blueprint \"" + Identity + "\" as MalTek.Airlock\n{\n"
            + "    required slot hatch expects " + (!version.StartsWith("sprocket 1") ? "block_type(Door)" : "Door") + "\n"
            + (!version.StartsWith("sprocket 1") ? "    optional slot mode expects type(Door) = open\n" : "")
            + "\n    type Door = open | shut\n\n"
            + "    sequence 1 {\n        call sprocket.close on [ hatch ]\n    }\n}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));
        var system = program.Systems[0];
        Assert.Equal("Door", system.FindPart("hatch")!.Expects);
        Assert.False(system.FindPart("hatch")!.IsSetting);

        var saved = ProgramWriter.Save(program, _store);
        Assert.Contains("    required slot hatch expects block_type(Door)\n", saved);
        if (!version.StartsWith("sprocket 1"))
        {
            Assert.Equal("Door", system.FindPart("mode")!.Setting);
            Assert.Contains("    optional slot mode expects type(Door) = open\n", saved);
        }

        Assert.Equal("", Load(saved, new Automation()));
    }

    /// <summary>
    /// A type and a slot may share a name, in a version 1 program too, and each is then written with its kind; a version 1
    /// program's bare slot reads as the slot.
    /// </summary>
    [Theory]
    [InlineData("sprocket 1", "", "side")]
    [InlineData("sprocket 3 alpha 3", "    optional slot mode expects type(side) = inside\n", "slot(side)")]
    public void ATypeAndASlotShareAName(string version, string setting, string slot)
    {
        var text = version + "\n\nsystem blueprint \"" + Identity + "\" as MalTek.Airlock\n{\n"
            + "    required slot side expects Door\n" + setting
            + "\n    type side = inside | outside\n\n"
            + "    sequence 1 {\n        call sprocket.close on [ " + slot + " ]\n    }\n}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));
        var system = program.Systems[0];
        Assert.NotNull(system.FindType("side"));
        Assert.Equal("Door", system.FindPart("side")!.Expects);

        var saved = ProgramWriter.Save(program, _store);
        Assert.Contains("        call sprocket.close on [ slot(side) ]\n", saved);
        if (setting.Length > 0)
            Assert.Contains(setting, saved);

        Assert.Equal("", Load(saved, new Automation()));
    }

    /// <summary>A type written bare where a slot shares its name could be either, and is trouble.</summary>
    [Fact]
    public void ABareTypeSharingASlotsNameIsTrouble()
    {
        Assert.Contains("'side' could be the slot 'side' or the type 'side'; write the one meant with its kind: slot(side) or type(side)",
            Load(Version + Head + "    required slot side expects Door\n    optional slot mode expects side = inside\n\n"
                + "    type side = inside | outside\n}\n", new Automation()));
    }

    /// <summary>A choice qualified by a type sharing a slot's name is read and written with the type qualified too.</summary>
    [Fact]
    public void AChoiceOfATypeSharingASlotsNameIsQualifiedTwice()
    {
        var text = Version + Head + "    required slot side expects Door\n"
            + "    optional slot mode expects type(side) = type(side)(inside)\n\n"
            + "    type side = inside | outside\n    type hatch = inside | shut\n\n"
            + "    let where = type(side)(inside)\n\n"
            + "    sequence 1 {\n        set where to type(side)(outside)\n    }\n}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));
        var system = program.Systems[0];
        Assert.Equal("side", system.FindState("where")!.Type);
        Assert.Equal("inside", system.FindPart("mode")!.Default.Text);

        var saved = ProgramWriter.Save(program, _store);
        Assert.Contains("    let where = type(side)(inside)\n", saved);
        Assert.Contains("        set where to outside\n", saved);
        Assert.Equal("", Load(saved, new Automation()));
    }

    /// <summary>A choice qualified by a bare type that shares a slot's name could be either, and is trouble wherever it stands.</summary>
    [Theory]
    [InlineData("    optional slot mode expects type(side) = side(inside)\n", "")]
    [InlineData("", "    let where = side(inside)\n")]
    [InlineData("", "    let where = type(side)(inside)\n\n    sequence 1 {\n        set where to side(outside)\n    }\n")]
    public void AChoiceQualifiedByABareTypeSharingASlotsNameIsTrouble(string setting, string body)
    {
        Assert.Contains("'side' could be the slot 'side' or the type 'side'; write the one meant with its kind: slot(side) or type(side).",
            Load(Version + Head + "    required slot side expects Door\n" + setting
                + "\n    type side = inside | outside\n\n" + body + "}\n", new Automation()));
    }

    /// <summary>A setting written alone where its member also takes a word of that name is the problem the player sees; written with its kind it is the setting.</summary>
    [Theory]
    [InlineData("closed", true)]
    [InlineData("setting(closed)", false)]
    [InlineData("door_status(closed)", false)]
    public void ASettingSpelledLikeAMembersWordIsAmbiguous(string written, bool ambiguous)
    {
        var lit = new Lit("    optional slot closed expects text = \"x\"\n\n"
            + "    sequence 1 {\n        if all sprocket.status on [ door ] == " + written + " then {\n        }\n    }\n", "");

        lit.Run(1);
        lit.Tick(2);

        Assert.Equal(ambiguous, lit.Of(1).Fault.Problem == SequenceProblem.Ambiguous);
        Assert.Contains("== " + written + " then", ProgramWriter.Save(lit.Program, lit.Rig.Values));
    }

    /// <summary>A member's word written with its host type, bare or in the host's namespace, is the word: compared with, and written bare where nothing else shares its name.</summary>
    [Theory]
    [InlineData("    optional slot closed expects text = \"x\"\n", "door_status(closed)", "door_status(closed)")]
    [InlineData("", "door_status(closed)", "closed")]
    [InlineData("", "sprocket.door_status(closed)", "closed")]
    public void AMembersWordWithItsTypeIsTheWord(string slots, string written, string saved)
    {
        var lit = new Lit(slots + "\n    let shut = off\n\n"
            + "    sequence 1 {\n        if all sprocket.status on [ door ] == " + written + " then {\n            set shut to on\n        }\n    }\n", "");

        lit.Run(1);
        lit.Tick(3);

        Assert.Equal(Literal.Of(true), lit.Held("shut"));
        Assert.Contains("== " + saved + " then", ProgramWriter.Save(lit.Program, lit.Rig.Values));
    }

    /// <summary>A word written with a host type it is not a choice of is trouble where it is read.</summary>
    [Fact]
    public void AWordOfNoneOfItsTypesChoicesIsTrouble()
    {
        Assert.Contains("no choice 'jammed' in sprocket.door_status",
            Load(Blueprint("    required slot door expects Door\n    sequence 1 {\n        if all sprocket.status on [ door ] == door_status(jammed) then {\n        }\n    }\n"), new Automation()));
    }

    /// <summary>A word written with another host type than its member's is the problem the player sees, though the member has a word of that name too.</summary>
    [Fact]
    public void AWordOfAnotherTypeThanItsMembersIsTrouble()
    {
        var lit = new Lit("    sequence 1 {\n        if all sprocket.status on [ door ] == lock_status(locked) then {\n        }\n    }\n", "");

        lit.Run(1);
        lit.Tick(2);

        Assert.Equal(SequenceProblem.Mistyped, lit.Of(1).Fault.Problem);
    }
    /// <summary>A state's starting value that could be a setting or a choice is trouble; a setting alone gives each installation its own start.</summary>
    [Fact]
    public void AStateStartsAtASetting()
    {
        Assert.Contains("'inside' could be the setting 'inside' or a choice of side", Load(Blueprint(
            "    optional slot inside expects side = outside\n\n    type side = inside | outside\n\n    let where = inside\n"), new Automation()));

        var lit = new Lit("    optional slot start expects side = outside\n\n"
            + "    type side = inside | outside\n\n"
            + "    let where = start\n", "    start = inside\n");

        Assert.Equal(Literal.Name("inside"), lit.Held("where"));
    }

    /// <summary>
    /// A version 1 program, a draft or finished, names Sprocket's own members, signals and types with <c>ext</c>; they read as
    /// <c>sprocket</c>, run the same, and are written so. A system's own namespace called <c>ext</c> stays as it was.
    /// </summary>
    [Theory]
    [InlineData("sprocket 1 alpha 28")]
    [InlineData("sprocket 1")]
    public void AnEarlierProgramsOwnNamespaceIsSprocket(string version)
    {
        var body = "let door1 = block 'Door/LargeBlockDoor' [ 1 ]\n"
            + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 2 ]\n"
            + "let button = ext.toolbar(1, 1, 1) on [ panel1 ]\n\n"
            + "system blueprint \"" + Identity + "\" as ext.Airlock\n{\n"
            + "    required slot door expects Door\n"
            + "    optional slot board expects ext.screen\n"
            + "    optional slot press expects signal\n\n"
            + "    whenever ext.finishes_opening on [ door ] then run 1\n"
            + "    when any ext.status on [ door ] == closed every 30t then run 2\n\n"
            + "    sequence 1 {\n"
            + "        call ext.close on [ door ]\n"
            + "        set ext.power on [ door ] to on\n"
            + "        if all ext.status on [ door ] == closed then {\n"
            + "            call ext.show_text(\"Shut\") on [ board ]\n"
            + "        }\n"
            + "    }\n\n"
            + "    sequence 2 {\n"
            + "        call ext.open on [ door ]\n"
            + "    }\n"
            + "}\n\n"
            + "instance 1 of ext.Airlock {\n"
            + "    door = [ door1 ]\n"
            + "    press = [ button ]\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", Load(version + "\n\n" + body, program));

        var saved = ProgramWriter.Save(program, _store);
        Assert.StartsWith(Version, saved);
        Assert.DoesNotContain("ext.toolbar", saved);
        Assert.Contains("let button = sprocket.toolbar(1, 1, 1) on [ panel1 ]\n", saved);
        Assert.Contains("as ext.Airlock\n", saved);
        Assert.Contains("    optional slot board expects sprocket.screen\n", saved);
        Assert.Contains("    whenever sprocket.finishes_opening on [ door ] then run 1\n", saved);
        Assert.Contains("    when any sprocket.status on [ door ] == closed every 30t then run 2\n", saved);
        Assert.Contains("        call sprocket.open on [ door ]\n", saved);
        Assert.Contains("        call sprocket.close on [ door ]\n", saved);
        Assert.Contains("        set sprocket.power on [ door ] to on\n", saved);
        Assert.Contains("        if all sprocket.status on [ door ] == closed then {\n", saved);
        Assert.Contains("            call sprocket.show_text(\"Shut\") on [ board ]\n", saved);

        var again = new Automation();
        Assert.Equal("", Load(saved, again));
        Assert.Equal(saved, ProgramWriter.Save(again, _store));
    }

    /// <summary>A lamp and a hinge, and a blueprint acting on them by its settings, running with one installation of its own answers.</summary>
    sealed class Lit
    {
        public readonly Rigged Rig = new();
        public readonly Automation Program = new();
        public readonly AutomationRunner Runner;
        public readonly AutomationSystem System;
        public readonly SystemInstance Instance;
        public readonly Rotor Hinge;

        int _frame;

        public Lit(string body, string answers)
        {
            Rig.Light(4, "Lamp").Carrying("Radius", "Radius", ValueKind.Real);
            Hinge = Rig.Hinge(5, "Hinge");
            Rig.Door(6, "Door");
            Assert.Equal("", ProgramReader.Load(Version + "let lamp1 = block 'InteriorLight/LargeBlockLight_1corner' [ 4 ]\n"
                + "let hinge1 = block 'MotorAdvancedStator/LargeHinge' [ 5 ]\n"
                + "let door1 = block 'Door/LargeBlockDoor' [ 6 ]\n\n"
                + Head + "    required slot lamp expects InteriorLight\n    optional slot hinge expects MotorAdvancedStator\n"
                + "    optional slot door expects Door\n" + body + "}\n\n"
                + "instance 1 of \"" + Identity + "\" {\n    door = [ door1 ]\n    hinge = [ hinge1 ]\n    lamp = [ lamp1 ]\n" + answers + "}\n",
                Program, Rig.Values));

            System = Program.Systems[0];
            Instance = System.Instances[0];
            Runner = new AutomationRunner(Program, Rig.World);
            Tick(3);
        }

        public List<string> Log => Rig.Rig.Log;

        public void Run(int sequence) => Runner.Call(System.FindSequence(sequence)!, Instance);

        public SequenceRunner Of(int sequence) => Runner.Get(System.FindSequence(sequence)!, Instance)!;

        public Literal Held(string state) => Runner.States.Read(System, Instance, System.FindState(state)!);

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++_frame);
            }
        }
    }

    const string SetsRadius = "    optional slot radius expects real = 4.5\n\n"
        + "    sequence 1 {\n        set Radius on [ lamp ] to radius\n    }\n";

    /// <summary>An installation that leaves an optional setting unanswered gives a step its default.</summary>
    [Fact]
    public void AnUnansweredSettingGivesItsDefault()
    {
        var lit = new Lit(SetsRadius, "");

        lit.Run(1);
        lit.Tick(2);

        Assert.Contains("Lamp:Radius 4.5", lit.Log);
    }

    /// <summary>An installation's answer is what a step is given in place of the default.</summary>
    [Fact]
    public void AnAnswerOverridesTheDefault()
    {
        var lit = new Lit(SetsRadius, "    radius = 2\n");

        lit.Run(1);
        lit.Tick(2);

        Assert.Contains("Lamp:Radius 2", lit.Log);
        Assert.DoesNotContain("Lamp:Radius 4.5", lit.Log);
    }

    /// <summary>A setting given as an action's argument is the installation's value.</summary>
    [Theory]
    [InlineData("", 45f)]
    [InlineData("    angle = 20\n", 20f)]
    public void ASettingIsAnArgument(string answers, float degrees)
    {
        var lit = new Lit("    optional slot angle expects real = 45\n\n"
            + "    sequence 1 {\n        call sprocket.rotate_to(angle, 5) on [ hinge ]\n        await\n    }\n", answers);

        lit.Run(1);
        lit.Tick(200);

        Assert.InRange(lit.Hinge.Degrees, degrees - 0.1f, degrees + 0.1f);
    }

    /// <summary>A wait as long as a setting lasts as long as the installation's value says.</summary>
    [Theory]
    [InlineData("", 30)]
    [InlineData("    pause = 10t\n", 10)]
    public void AWaitLastsAsLongAsItsSetting(string answers, int ticks)
    {
        var lit = new Lit("    optional slot pause expects duration = 0.5s\n\n"
            + "    let done = off\n\n"
            + "    sequence 1 {\n        wait pause\n        set done to on\n    }\n", answers);

        lit.Run(1);
        lit.Tick(ticks - 2);
        Assert.Equal(Literal.Of(false), lit.Held("done"));

        lit.Tick(4);
        Assert.Equal(Literal.Of(true), lit.Held("done"));
    }

    /// <summary>An abort saying a setting says the installation's text, and is written back naming the setting.</summary>
    [Theory]
    [InlineData("", "Jammed")]
    [InlineData("    why = \"Blocked\"\n", "Blocked")]
    public void AnAbortSaysItsSettingsText(string answers, string said)
    {
        var lit = new Lit("    optional slot why expects text = \"Jammed\"\n\n"
            + "    sequence 1 {\n        abort saying why as warning\n    }\n", answers);

        Assert.Contains("        abort saying why as warning\n", ProgramWriter.Save(lit.Program, lit.Rig.Values));

        lit.Run(1);
        lit.Tick(3);

        var fault = SequenceSummary.Fault(lit.Of(1));
        Assert.Equal(SequenceProblem.AbortedWarning, fault.Problem);
        Assert.Equal(said, fault.Which);
    }

    /// <summary>A setting an abort says must hold text.</summary>
    [Fact]
    public void AnAbortSaysOnlyATextSetting()
    {
        Assert.Contains("setting 'angle' holds a real; expected text here",
            Load(Blueprint("    required slot angle expects real\n    sequence 1 {\n        abort saying angle\n    }\n"), new Automation()));
    }

    /// <summary>An if tests a setting as it tests a state, and a state is set to one and compared with one.</summary>
    [Theory]
    [InlineData("", "off", "outside")]
    [InlineData("    bright = on\n    start = inside\n", "on", "inside")]
    public void AnIfTestsASettingAndAStateTakesOne(string answers, string lit, string where)
    {
        var run = new Lit("    optional slot bright expects flag = off\n"
            + "    optional slot start expects side = outside\n\n"
            + "    type side = inside | outside\n\n"
            + "    let where = inside\n"
            + "    let lit = off\n"
            + "    let same = off\n\n"
            + "    sequence 1 {\n"
            + "        if bright == on then {\n            set lit to on\n        }\n"
            + "        set where to start\n"
            + "        if where == start then {\n            set same to on\n        }\n"
            + "    }\n", answers);

        run.Run(1);
        run.Tick(2);

        Assert.Equal(lit == "on" ? Literal.Of(true) : Literal.Of(false), run.Held("lit"));
        Assert.Equal(Literal.Name(where), run.Held("where"));
        Assert.Equal(Literal.Of(true), run.Held("same"));
    }

    /// <summary>A setting compared with a block's value is compared as the installation's value.</summary>
    [Theory]
    [InlineData("", false)]
    [InlineData("    least = 0\n", true)]
    public void AComparisonTakesTheSettingsValue(string answers, bool holds)
    {
        var lit = new Lit("    optional slot least expects real = 10\n\n"
            + "    let over = off\n\n"
            + "    sequence 1 {\n"
            + "        if all Radius on [ lamp ] >= least then {\n            set over to on\n        }\n"
            + "    }\n", answers);

        lit.Run(1);
        lit.Tick(2);

        Assert.Equal(Literal.Of(holds), lit.Held("over"));
    }

    /// <summary>A when compares a block's value with the installation's value of a setting.</summary>
    [Theory]
    [InlineData("", false)]
    [InlineData("    least = 3\n", true)]
    public void AWhenComparesWithTheSettingsValue(string answers, bool fires)
    {
        var lit = new Lit("    optional slot least expects real = 10\n\n"
            + "    let over = off\n\n"
            + "    when all Radius on [ lamp ] >= least then run 2\n\n"
            + "    sequence 1 {\n        set Radius on [ lamp ] to 5\n    }\n\n"
            + "    sequence 2 {\n        set over to on\n    }\n", answers);

        lit.Tick(20);
        lit.Run(1);
        lit.Tick(60);

        Assert.Equal(Literal.Of(fires), lit.Held("over"));
    }

    /// <summary>A clock starts as often as its setting says on each installation.</summary>
    [Theory]
    [InlineData("", false)]
    [InlineData("    often = 10t\n", true)]
    public void AClockTicksAsOftenAsItsSetting(string answers, bool started)
    {
        var lit = new Lit("    optional slot often expects duration = 10s\n\n"
            + "    let done = off\n\n"
            + "    every often run 1\n\n"
            + "    sequence 1 {\n        set done to on\n    }\n", answers);

        lit.Tick(40);

        Assert.Equal(Literal.Of(started), lit.Held("done"));
    }

    /// <summary>A when on a state compares it with a setting, and a when may test a setting as it tests a state.</summary>
    [Theory]
    [InlineData("", false)]
    [InlineData("    goal = on\n", true)]
    public void AWhenOnAStateTakesASetting(string answers, bool fired)
    {
        var lit = new Lit("    optional slot goal expects flag = off\n\n"
            + "    let lit = off\n"
            + "    let seen = off\n\n"
            + "    when goal == lit then run 2\n\n"
            + "    sequence 1 {\n        set lit to on\n    }\n\n"
            + "    sequence 2 {\n        set seen to on\n    }\n", answers);

        lit.Run(1);
        lit.Tick(2);

        Assert.Equal(Literal.Of(fired), lit.Held("seen"));
    }

    /// <summary>An installation that leaves a required setting unanswered is missing it: the sequence waits for it, and starts once it is answered.</summary>
    [Fact]
    public void AnUnansweredRequiredSettingIsAnEmptySlot()
    {
        var lit = new Lit("    required slot radius expects real\n\n"
            + "    sequence 1 {\n        set Radius on [ lamp ] to radius\n    }\n", "");

        lit.Run(1);
        lit.Tick(2);

        Assert.DoesNotContain(lit.Log, line => line.StartsWith("Lamp:Radius"));
        Assert.Equal(SequenceProblem.EmptySlot, lit.Of(1).Fault.Problem);
        Assert.Equal("radius", lit.Of(1).Fault.Which);

        lit.Instance.Answer("radius", Literal.Of(3.0));
        lit.Program.Edited();
        lit.Tick(2);
        lit.Run(1);
        lit.Tick(2);

        Assert.Contains("Lamp:Radius 3", lit.Log);
    }

    /// <summary>A setting renamed is renamed everywhere it is named, its installations' answers included.</summary>
    [Fact]
    public void ARenamedSettingIsRenamedWhereverItIsNamed()
    {
        var program = new Automation();
        Assert.Equal("", Load(Everything, program));
        var system = program.Systems[0];

        system.RenamePart(system.FindPart("Open angle")!, "Angle");
        system.RenamePart(system.FindPart("home")!, "base");

        var saved = ProgramWriter.Save(program, _store);
        Assert.Contains("        set Radius on [ lamp ] to Angle\n", saved);
        Assert.Contains("        set where to base\n", saved);
        Assert.Contains("        else if base == where then {\n", saved);
        Assert.Contains("    Angle = 85\n", saved);
        Assert.Equal("", Load(saved, new Automation()));
    }
}
