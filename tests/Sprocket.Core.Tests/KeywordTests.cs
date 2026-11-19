using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Format;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A reserved keyword is a keyword wherever it stands, and a name spelled like one is always quoted; a contextual keyword is
/// one only where its construct reads it, and a name anywhere else, quoted only there.
/// </summary>
public class KeywordTests
{
    static readonly string Version = $"sprocket {ScriptFormat.Version} alpha {ScriptFormat.Draft}\n\n";

    const string Blueprint = "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Words\n{\n";

    const string System = "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Gear\n{\n";

    static readonly Values Store = new Values();

    /// <summary>A slot, a state and a sequence named like contextual keywords read bare, and are written back bare.</summary>
    [Fact]
    public void NamesSpelledLikeContextualKeywordsReadBareAndAreWrittenBare()
    {
        var text = Version + Blueprint
            + "    required slot of expects Door\n"
            + "    optional slot as expects signal\n\n"
            + "    type mode = to | all\n\n"
            + "    let installed = off\n"
            + "    let where = to\n\n"
            + "    whenever as then run 1\n\n"
            + "    sequence 1 as signal {\n"
            + "        call sprocket.close on [ of ]\n"
            + "        set installed to on\n"
            + "        set where to all\n"
            + "        if installed == on then {\n"
            + "            set where to to\n"
            + "        }\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var system = program.Systems[0];
        Assert.NotNull(system.FindPart("of"));
        Assert.True(system.FindPart("as")!.IsSignal);
        Assert.Equal("mode", system.FindState("where")!.Type);
        Assert.True(system.FindState("installed")!.IsFlag);
        Assert.Equal("signal", system.FindSequence(1)!.Name);

        var written = ProgramWriter.Save(program, Store);
        Assert.Contains("    required slot of expects Door\n", written);
        Assert.Contains("    optional slot as expects signal\n", written);
        Assert.Contains("    type mode = to | all\n", written);
        Assert.Contains("    let installed = off\n", written);
        Assert.Contains("    whenever as then run 1\n", written);
        Assert.Contains("    sequence 1 as signal {\n", written);
        Assert.Contains("        call sprocket.close on [ of ]\n", written);
        Assert.Contains("        set installed to on\n", written);
        Assert.Contains("        set where to all\n", written);
        Assert.Contains("        if installed == on then {\n", written);
        Assert.Contains("            set where to to\n", written);
        Assert.DoesNotContain("'", written);

        var again = new Automation();
        Assert.Equal("", ProgramReader.Load(written, again, Store));
        Assert.Equal(written, ProgramWriter.Save(again, Store));
    }

    /// <summary>A name spelled like a contextual keyword is quoted where its construct reads that keyword: a state called all where a test starts, a type called signal after expects.</summary>
    [Fact]
    public void ANameIsQuotedWhereItsConstructReadsTheKeyword()
    {
        var text = Version + Blueprint
            + "    required slot mode expects 'signal'\n\n"
            + "    type signal = high | low\n\n"
            + "    let all = off\n\n"
            + "    sequence 1 {\n"
            + "        set all to on\n"
            + "        if 'all' == on then {\n"
            + "        }\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        Assert.Equal("signal", program.Systems[0].FindPart("mode")!.Setting);

        var written = ProgramWriter.Save(program, Store);
        Assert.Contains("    required slot mode expects 'signal'\n", written);
        Assert.Contains("    let all = off\n", written);
        Assert.Contains("        set all to on\n", written);
        Assert.Contains("        if 'all' == on then {\n", written);
        Assert.Equal("", ProgramReader.Load(written, new Automation(), Store));

        // Bare where a test starts, all is the quantifier, which the rest of the line is not written for.
        Assert.NotEqual("", ProgramReader.Load(text.Replace("if 'all' == on", "if all == on"), new Automation(), Store));
    }

    /// <summary>A name spelled like a reserved keyword is quoted wherever it stands, and is trouble bare.</summary>
    [Theory]
    [InlineData("every")]
    [InlineData("is")]
    [InlineData("not")]
    [InlineData("then")]
    public void ANameSpelledLikeAReservedKeywordIsQuoted(string word)
    {
        var text = Version + Blueprint
            + $"    let '{word}' = off\n\n"
            + "    sequence 1 {\n"
            + $"        set '{word}' to on\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var written = ProgramWriter.Save(program, Store);
        Assert.Contains($"    let '{word}' = off\n", written);
        Assert.Contains($"        set '{word}' to on\n", written);

        Assert.NotEqual("", ProgramReader.Load(text.Replace($"'{word}'", word), new Automation(), Store));
    }

    /// <summary>A part of a qualified name spelled like a contextual keyword is bare, and one spelled like a reserved keyword is quoted.</summary>
    [Fact]
    public void APartOfAQualifiedNameIsQuotedOnlyWhereReserved()
    {
        var text = Version + Blueprint
            + "    required slot door expects Door\n\n"
            + "    sequence 1 {\n"
            + "        call gearbox.on on [ door ]\n"
            + "        call gearbox.'every' on [ door ]\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var written = ProgramWriter.Save(program, Store);
        Assert.Contains("        call gearbox.on on [ door ]\n", written);
        Assert.Contains("        call gearbox.'every' on [ door ]\n", written);

        Assert.NotEqual("", ProgramReader.Load(text.Replace("gearbox.'every'", "gearbox.every"), new Automation(), Store));
    }

    /// <summary>on and off are the flag's choices, bare or written with their kind, and flag(on) is written as on where alone it means only the flag.</summary>
    [Fact]
    public void OnAndOffAreTheFlagsChoices()
    {
        var text = Version + Blueprint
            + "    let lit = flag(on)\n"
            + "    let dark = off\n\n"
            + "    sequence 1 {\n"
            + "        set lit to flag(off)\n"
            + "        set dark to on\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var system = program.Systems[0];
        Assert.Equal(Literal.Of(true), system.FindState("lit")!.Start);
        Assert.Equal(Literal.Of(false), system.FindState("dark")!.Start);

        var written = ProgramWriter.Save(program, Store);
        Assert.Contains("    let lit = on\n", written);
        Assert.Contains("        set lit to off\n", written);
        Assert.Contains("        set dark to on\n", written);
    }

    /// <summary>Where the system has a thing called on, a bare on could be either and is trouble, flag(on) is the flag, and the flag is written so.</summary>
    [Theory]
    [InlineData("    let on = off\n")]
    [InlineData("    type power = on | off\n")]
    [InlineData("    required slot on expects flag\n")]
    public void ABareFlagAThingIsCalledLikeIsAmbiguous(string named)
    {
        var start = named.Contains("type") ? "    let lit = flag(off)\n" : "    let lit = off\n";
        var text = Version + Blueprint + named + start + "\n"
            + "    sequence 1 {\n"
            + "        set lit to flag(on)\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var set = Assert.IsType<SetState>(program.Systems[0].Sequences[0].Steps[0]);
        Assert.Equal(Literal.Of(true), set.Value);

        var written = ProgramWriter.Save(program, Store);
        Assert.Equal("", ProgramReader.Load(written, new Automation(), Store));

        var bare = text.Replace("set lit to flag(on)", "set lit to on");
        if (named.Contains("type"))
        {
            // Where the state's type is known, a flag, a choice of another type is no rival; where it is not, as it starts, it is.
            Assert.Contains("        set lit to on\n", written);
            Assert.Contains("    let lit = flag(off)\n", written);
            Assert.Equal("", ProgramReader.Load(bare, new Automation(), Store));
            var started = ProgramReader.Load(text.Replace("let lit = flag(off)", "let lit = off"), new Automation(), Store);
            Assert.Contains("power(off)", started);
            Assert.Contains("flag(off)", started);
        }
        else
        {
            Assert.Contains("        set lit to flag(on)\n", written);
            var said = ProgramReader.Load(bare, new Automation(), Store);
            Assert.Contains("write the one meant with its kind", said);
            Assert.Contains("flag(on)", said);
        }
    }

    /// <summary>A quoted on is the same choice as a bare one: the flag where a flag stands, a choice of the state's type called on where that type is known.</summary>
    [Fact]
    public void AQuotedOnIsTheSameChoiceAsABareOne()
    {
        var text = Version + Blueprint
            + "    type power = on | off\n\n"
            + "    let p = power(off)\n"
            + "    let lit = flag(off)\n\n"
            + "    sequence 1 {\n"
            + "        set p to 'on'\n"
            + "        set p to on\n"
            + "        set lit to 'on'\n"
            + "        set lit to on\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var steps = program.Systems[0].Sequences[0].Steps;
        Assert.Equal(Literal.Name("on"), ((SetState)steps[0]).Value);
        Assert.Equal(Literal.Name("on"), ((SetState)steps[1]).Value);
        Assert.Equal(Literal.Of(true), ((SetState)steps[2]).Value);
        Assert.Equal(Literal.Of(true), ((SetState)steps[3]).Value);

        var written = ProgramWriter.Save(program, Store);
        Assert.Contains("        set p to on\n", written);
        Assert.Contains("        set lit to on\n", written);
        Assert.DoesNotContain("'on'", written);
        Assert.Equal("", ProgramReader.Load(written, new Automation(), Store));
    }

    /// <summary>A quoted on where a thing is called on could be either, as a bare one could, and each is written with its kind.</summary>
    [Fact]
    public void AQuotedOnAThingIsCalledLikeIsAmbiguous()
    {
        var text = Version + Blueprint
            + "    let on = off\n"
            + "    let lit = off\n\n"
            + "    sequence 1 {\n"
            + "        set lit to state(on)\n"
            + "        set lit to flag(on)\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var written = ProgramWriter.Save(program, Store);
        Assert.Contains("        set lit to state(on)\n", written);
        Assert.Contains("        set lit to flag(on)\n", written);
        Assert.Equal("", ProgramReader.Load(written, new Automation(), Store));

        Assert.Contains("write the one meant with its kind", ProgramReader.Load(text.Replace("set lit to state(on)", "set lit to 'on'"), new Automation(), Store));
    }

    /// <summary>A program written before contextual keywords reads as it did: a quoted name stays a name and a bare on stays the flag, though a thing is called on.</summary>
    [Fact]
    public void AnEarlierProgramReadsAsItDid()
    {
        var text = "sprocket 2 alpha 1\n\n" + Blueprint
            + "    let 'on' = off\n"
            + "    let is = off\n\n"
            + "    sequence 1 {\n"
            + "        set 'on' to on\n"
            + "        set is to on\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var system = program.Systems[0];
        Assert.NotNull(system.FindState("on"));
        Assert.NotNull(system.FindState("is"));
        Assert.Equal(Literal.Of(true), ((SetState)system.Sequences[0].Steps[0]).Value);

        var written = ProgramWriter.Save(program, Store);
        Assert.StartsWith(Version, written);
        Assert.Contains("    let on = off\n", written);
        Assert.Contains("    let 'is' = off\n", written);
        Assert.Contains("        set on to flag(on)\n", written);
        Assert.Contains("        set 'is' to flag(on)\n", written);

        var again = new Automation();
        Assert.Equal("", ProgramReader.Load(written, again, Store));
        Assert.Equal(Literal.Of(true), ((SetState)again.Systems[0].Sequences[0].Steps[0]).Value);
    }

    /// <summary>An if testing the host reads, installed or not, among else ifs and an else, and is written back as it was.</summary>
    [Fact]
    public void AHostTestReadsAndIsWrittenBack()
    {
        var text = Version + System
            + "    let up = off\n\n"
            + "    sequence 1 {\n"
            + "        if gearbox.lifts is installed then {\n"
            + "            set up to on\n"
            + "        }\n"
            + "        else if gearbox is not installed then {\n"
            + "            set up to off\n"
            + "        }\n"
            + "        else if 'all' is installed then {\n"
            + "        }\n"
            + "        else {\n"
            + "        }\n"
            + "    }\n"
            + "}\n";

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, Store));
        var steps = program.Systems[0].Sequences[0].Steps;
        var lifts = Assert.IsType<HostTest>(((If)steps[0]).Test);
        Assert.Equal(("gearbox.lifts", true), (lifts.Namespace, lifts.Installed));
        var gearbox = Assert.IsType<HostTest>(((Else)steps[2]).Test);
        Assert.Equal(("gearbox", false), (gearbox.Namespace, gearbox.Installed));
        Assert.Equal("all", Assert.IsType<HostTest>(((Else)steps[4]).Test).Namespace);

        var written = ProgramWriter.Save(program, Store);
        Assert.Contains("if gearbox.lifts is installed then {\n", written);
        Assert.Contains("else if gearbox is not installed then {\n", written);
        Assert.Contains("else if 'all' is installed then {\n", written);
        Assert.Equal("if gearbox.lifts is installed then", ProgramWriter.Line(steps[0]));

        var again = new Automation();
        Assert.Equal("", ProgramReader.Load(written, again, Store));
        Assert.Equal(written, ProgramWriter.Save(again, Store));
    }

    /// <summary>A host test not as the language says is trouble.</summary>
    [Theory]
    [InlineData("if gearbox.lifts is then {")]
    [InlineData("if gearbox.lifts is not then {")]
    [InlineData("if gearbox.lifts is installed {")]
    [InlineData("if gearbox.lifts is installed then now {")]
    [InlineData("if \"gearbox\" is installed then {")]
    public void AHostTestNotAsTheLanguageSaysIsTrouble(string line)
    {
        var text = Version + System
            + "    sequence 1 {\n"
            + $"        {line}\n"
            + "        }\n"
            + "    }\n"
            + "}\n";

        Assert.NotEqual("", ProgramReader.Load(text, new Automation(), Store));
    }

    /// <summary>A host test holds by whether the host has the namespace when the run reaches it, installed or not, and the else after it runs where it fails.</summary>
    [Theory]
    [InlineData(true, true, "lifted")]
    [InlineData(false, true, "lowered")]
    [InlineData(true, false, "lowered")]
    [InlineData(false, false, "lifted")]
    public void AHostTestRunsTheBranchItHolds(bool registered, bool installed, string set)
    {
        try
        {
            if (registered)
                Assert.True(HostNamespaces.Register("gearbox.lifts", "Gearbox", "Gearbox", "Gearbox"));

            var program = new Automation();
            Assert.Equal("", ProgramReader.Load(Version + System
                + "    let lifted = off\n"
                + "    let lowered = off\n\n"
                + "    sequence 1 {\n"
                + $"        if gearbox.lifts is {(installed ? "" : "not ")}installed then {{\n"
                + "            set lifted to on\n"
                + "        }\n"
                + "        else {\n"
                + "            set lowered to on\n"
                + "        }\n"
                + "    }\n"
                + "}\n", program, Store));

            var runner = new AutomationRunner(program, new Rigged().World);
            runner.Tick(1);
            var system = program.Systems[0];
            var self = system.Uses[0];
            runner.Call(system.FindSequence(1)!, self, true);
            runner.Tick(2);
            runner.Tick(3);

            Assert.Equal(Literal.Of(set == "lifted"), runner.States.Read(system, self, system.FindState("lifted")!));
            Assert.Equal(Literal.Of(set == "lowered"), runner.States.Read(system, self, system.FindState("lowered")!));
        }
        finally
        {
            HostNamespaces.Clear();
        }
    }
}
