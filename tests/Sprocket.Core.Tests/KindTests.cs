using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A thing written with its kind, <c>kind(thing)</c>: it only chooses among what the thing could already be, a name shared
/// by things of different kinds is trouble written bare, and each is written with its kind where alone it could mean more.
/// </summary>
public class KindTests
{
    const string Identity = "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43";

    const string Head = "sprocket 3 alpha 3\n\nsystem blueprint \"" + Identity + "\" as MalTek.Airlock\n{\n";

    readonly Values _store = new();

    string Load(string text, Automation program) => ProgramReader.Load(text, program, _store);

    /// <summary>A literal with a built-in kind it already has is that literal, and is written back bare.</summary>
    [Theory]
    [InlineData("real", "real(45)", "45")]
    [InlineData("real", "integer(45)", "45")]
    [InlineData("text", "text(\"Door jammed\")", "\"Door jammed\"")]
    [InlineData("flag", "flag(on)", "on")]
    [InlineData("duration", "duration(2s)", "2s")]
    public void ABuiltInKindChoosesWhatTheLiteralAlreadyIs(string type, string written, string saved)
    {
        var program = new Automation();
        Assert.Equal("", Load(Head + "    optional slot it expects " + type + " = " + written + "\n}\n", program));
        Assert.Contains("    optional slot it expects " + type + " = " + saved + "\n", ProgramWriter.Save(program, _store));
    }

    /// <summary>A kind never turns one thing into another: a literal that is never of the kind is trouble.</summary>
    [Theory]
    [InlineData("text", "text(4)")]
    [InlineData("integer", "integer(4.5)")]
    [InlineData("real", "text(\"4\")")]
    [InlineData("flag", "flag(4)")]
    public void AKindALiteralNeverHasIsTrouble(string type, string written)
    {
        Assert.NotEqual("", Load(Head + "    optional slot it expects " + type + " = " + written + "\n}\n", new Automation()));
    }

    /// <summary>A setting that holds text is never a real, and a kind does not make it one.</summary>
    [Fact]
    public void AKindNeverTurnsASettingIntoAnotherType()
    {
        Assert.NotEqual("", Load(Head + "    optional slot label expects text = \"x\"\n\n"
            + "    sequence 1 {\n        wait real(setting(label))\n    }\n}\n", new Automation()));
    }

    /// <summary>A sequence sharing a type's name is run with its kind, and alone it could be either, which is trouble.</summary>
    [Fact]
    public void ASequenceSharingATypesNameIsRunWithItsKind()
    {
        const string body = "    type cycle = fast | slow\n\n    sequence 1 {\n        run {0}\n    }\n\n    sequence 2 as cycle {\n    }\n}\n";

        var program = new Automation();
        Assert.Equal("", Load(Head + body.Replace("{0}", "sequence(cycle)"), program));
        Assert.Contains("        run 2\n", ProgramWriter.Save(program, _store));

        Assert.Contains("'cycle' could be the type 'cycle' or the sequence 'cycle'; write the one meant with its kind: type(cycle) or sequence(cycle).",
            Load(Head + body.Replace("{0}", "cycle"), new Automation()));
    }

    /// <summary>A type and a state may share a name, and each use is written with its kind where alone it could be either.</summary>
    [Fact]
    public void ATypeAndAStateShareAName()
    {
        var text = Head + "    type side = inside | outside\n\n"
            + "    let side = inside\n\n"
            + "    sequence 1 {\n        set state(side) to outside\n    }\n}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));
        Assert.Equal("side", program.Systems[0].FindState("side")!.Type);
        Assert.Equal(text, ProgramWriter.Save(program, _store));

        Assert.Contains("'side' could be the type 'side' or the state 'side'",
            Load(text.Replace("set state(side) to", "set side to"), new Automation()));
    }

    /// <summary>A bare name in <c>expects</c> that is a block type of the game's and a type of the system's could be either, which is trouble.</summary>
    [Fact]
    public void ABareBlockTypeSharingATypesNameIsTrouble()
    {
        _ = new Rigged();
        Assert.Contains("'Door' could be the block type 'Door' or the type 'Door'; write the one meant with its kind: block_type(Door) or type(Door).",
            Load(Head + "    required slot hatch expects Door\n\n    type Door = open | shut\n}\n", new Automation()));
    }

    /// <summary>A signal slot sharing a type's name is watched with its kind, and alone it could be either, which is trouble.</summary>
    [Fact]
    public void ASignalSlotSharingATypesNameIsWatchedWithItsKind()
    {
        var text = Head + "    optional slot button expects signal\n\n"
            + "    type button = pressed | released\n\n"
            + "    whenever slot(button) then run 1\n\n"
            + "    sequence 1 {\n    }\n}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));
        Assert.Equal(text, ProgramWriter.Save(program, _store));

        Assert.Contains("'button' could be the slot 'button' or the type 'button'",
            Load(text.Replace("whenever slot(button)", "whenever button"), new Automation()));
    }

    /// <summary>A version 1 program's choice qualified by its type reads, and is written with its type as a kind where it must be.</summary>
    [Fact]
    public void AnEarlierProgramsQualifiedChoiceIsWrittenWithItsKind()
    {
        var text = "sprocket 1\n\nsystem blueprint \"" + Identity + "\" as MalTek.Airlock\n{\n"
            + "    type valve = open | shut\n    type hatch = open | shut\n\n"
            + "    let intake = valve.open\n    let lid = hatch.shut\n\n"
            + "    sequence 1 {\n        set intake to valve.shut\n    }\n}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));

        var saved = ProgramWriter.Save(program, _store);
        Assert.Contains("    let intake = valve(open)\n", saved);
        Assert.Contains("    let lid = hatch(shut)\n", saved);
        Assert.Contains("        set intake to shut\n", saved);
        Assert.Equal("", Load(saved, new Automation()));
    }

    /// <summary>A version 1 program compares a speaker's selected sound with a bare word, and sets it to one; each reads as the text it is.</summary>
    [Fact]
    public void AnEarlierProgramsSoundIsText()
    {
        var text = "sprocket 1\n\nsystem blueprint \"" + Identity + "\" as MalTek.Horn\n{\n"
            + "    required slot horn expects SoundBlock\n\n"
            + "    sequence 1 {\n        if any sprocket.selected_sound on [ horn ] == SoundBlockAlert1 then {\n"
            + "            set sprocket.selected_sound on [ horn ] to SoundBlockAlert2\n        }\n    }\n}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));

        var saved = ProgramWriter.Save(program, _store);
        Assert.Contains("== \"SoundBlockAlert1\" then", saved);
        Assert.Contains("to \"SoundBlockAlert2\"", saved);
    }

    /// <summary>A version 1 program's sequence sharing a slot's name is run as the sequence it was.</summary>
    [Fact]
    public void AnEarlierProgramsSharedSequenceNameRunsTheSequence()
    {
        var text = "sprocket 1\n\nsystem blueprint \"" + Identity + "\" as MalTek.Airlock\n{\n"
            + "    required slot door expects Door\n\n"
            + "    sequence 1 {\n        run door\n        call sprocket.close on [ door ]\n    }\n\n"
            + "    sequence 2 as door {\n    }\n}\n";

        var program = new Automation();
        Assert.Equal("", Load(text, program));

        var saved = ProgramWriter.Save(program, _store);
        Assert.Contains("        run 2\n        call sprocket.close on [ slot(door) ]\n", saved);
        Assert.Equal("", Load(saved, new Automation()));
    }
}
