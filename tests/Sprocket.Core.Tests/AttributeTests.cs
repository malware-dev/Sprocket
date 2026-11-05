using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// An annotation's attributes are data about the statement below it: the ones Sprocket uses are read by name, and the rest
/// are written back exactly as they were.
/// </summary>
public class AttributeTests
{
    /// <summary>A program with an attribute Sprocket does not use on every kind of statement that can carry one.</summary>
    const string Everywhere = "#@ headerNote: 1\n"
        + "sprocket 1 alpha 21\n\n"
        + "#@ doorNote: [ 1, 2 ]\n"
        + "let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n"
        + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 8, \"Button\" ]\n"
        + "#@ bellNote: 600t\n"
        + "let bell = ext.toolbar(1, 1, 3) on [ panel1 ]\n\n"
        + "# The airlock.\n"
        + "#@ systemNote: \"one\"\n"
        + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock\n"
        + "{\n"
        + "    #@ slotNote: on\n"
        + "    required slot inner expects Door\n"
        + "    optional slot button expects signal\n\n"
        + "    #@ triggerNote: 2.5\n"
        + "    whenever button then run 1\n\n"
        + "    #@ sequenceNote: MalTek.Thing\n"
        + "    sequence 1 as Cycle {\n"
        + "        #@ stepNote: #FF0000\n"
        + "        call ext.close on [ inner ]\n"
        + "    }\n"
        + "}\n\n"
        + "#@ instanceNote: 3\n"
        + "instance 1 of MalTek.Airlock {\n"
        + "    #@ answerNote: \"door\"\n"
        + "    inner = [ door1 ]\n"
        + "    button = [ bell ]\n"
        + "}\n";

    /// <summary>Every attribute Sprocket does not use is written back as it was, above the statement it was above.</summary>
    [Theory]
    [InlineData("#@ headerNote: 1\nsprocket 1 alpha 21\n")]
    [InlineData("#@ doorNote: [ 1, 2 ]\nlet door1 = ")]
    [InlineData("#@ bellNote: 600t\nlet bell = ")]
    [InlineData("# The airlock.\n#@ systemNote: \"one\"\nsystem blueprint ")]
    [InlineData("    #@ slotNote: on\n    required slot inner expects Door\n")]
    [InlineData("    #@ triggerNote: 2.5\n    whenever button then run 1\n")]
    [InlineData("    #@ sequenceNote: MalTek.Thing\n    sequence 1 as Cycle {\n")]
    [InlineData("        #@ stepNote: #FF0000\n        call ext.close on [ inner ]\n")]
    [InlineData("#@ instanceNote: 3\ninstance 1 of \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\"")]
    [InlineData("    #@ answerNote: \"door\"\n    inner = [ door1 ]\n")]
    public void AnAttributeNothingUsesIsWrittenBackAsItWas(string line)
    {
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(Everywhere, program, new Values()));

        Assert.Contains(line, ProgramWriter.Save(program, new Values()));
    }

    /// <summary>After the space that follows <c>#@</c>, spaces are nothing, as anywhere else in the language; the value is written back as it was typed.</summary>
    [Fact]
    public void SpacesAfterTheFirstAreNothing()
    {
        var program = new Automation();
        var text = Everywhere.Replace("#@ systemNote: \"one\"", "#@    systemNote   :    \"one\"");
        Assert.Equal("", ProgramReader.Load(text, program, new Values()));

        Assert.Contains("#@ systemNote: \"one\"\nsystem blueprint ", ProgramWriter.Save(program, new Values()));
    }

    /// <summary><c>#@</c> is followed by a space.</summary>
    [Fact]
    public void AnAttributeNeedsTheSpaceAfterItsMark()
    {
        var trouble = ProgramReader.Load(Everywhere.Replace("#@ systemNote:", "#@systemNote:"), new Automation(), new Values());
        Assert.Contains("expected '#@ ' and an attribute's name after '#@'.", trouble);
    }

    /// <summary>An annotation's description comes before its attributes, never after them.</summary>
    [Fact]
    public void ADescriptionAfterAnAttributeIsTrouble()
    {
        var text = Everywhere.Replace("# The airlock.\n#@ systemNote: \"one\"\n", "#@ systemNote: \"one\"\n# The airlock.\n");
        var trouble = ProgramReader.Load(text, new Automation(), new Values());
        Assert.Contains("unexpected annotation line after an attribute", trouble);
    }

    /// <summary>An attribute stands alone without a description, or after one.</summary>
    [Fact]
    public void AnAttributeStandsAloneOrAfterADescription()
    {
        var alone = Everywhere.Replace("# The airlock.\n", "");
        Assert.Equal("", ProgramReader.Load(alone, new Automation(), new Values()));
        Assert.Equal("", ProgramReader.Load(Everywhere, new Automation(), new Values()));
    }

    /// <summary>An attribute is unique per statement.</summary>
    [Fact]
    public void AnAttributeGivenTwiceIsTrouble()
    {
        var text = Everywhere.Replace("#@ systemNote: \"one\"\n", "#@ systemNote: \"one\"\n#@ systemNote: \"two\"\n");
        var trouble = ProgramReader.Load(text, new Automation(), new Values());
        Assert.Contains("the attribute systemNote is given twice.", trouble);
    }

    /// <summary>An attribute always belongs to a statement.</summary>
    [Fact]
    public void AnAttributeWithNoStatementIsTrouble()
    {
        var text = Everywhere.Replace("#@ instanceNote: 3\n", "#@ instanceNote: 3\n\n");
        var trouble = ProgramReader.Load(text, new Automation(), new Values());
        Assert.Contains("expected a statement under the annotation.", trouble);
    }

    /// <summary>An attribute holds one value.</summary>
    [Theory]
    [InlineData("#@ systemNote: \"one\" \"two\"\n")]
    [InlineData("#@ systemNote:\n")]
    [InlineData("#@ systemNote \"one\"\n")]
    public void AnAttributeHoldsOneValue(string line)
    {
        var text = Everywhere.Replace("#@ systemNote: \"one\"\n", line);
        Assert.NotEqual("", ProgramReader.Load(text, new Automation(), new Values()));
    }

    /// <summary>A name spelled like a keyword is quoted, as everywhere else.</summary>
    [Fact]
    public void AnAttributeNamedLikeAKeywordIsQuoted()
    {
        Assert.NotEqual("", ProgramReader.Load(Everywhere.Replace("#@ systemNote:", "#@ on:"), new Automation(), new Values()));

        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(Everywhere.Replace("#@ systemNote:", "#@ 'on':"), program, new Values()));
        Assert.Contains("#@ 'on': \"one\"\n", ProgramWriter.Save(program, new Values()));
    }

    /// <summary>A signal slot's <c>short</c> is the few words shown with its toolbar slots, read as text and written back.</summary>
    [Fact]
    public void ASignalSlotsShortIsRead()
    {
        var text = Everywhere.Replace("    optional slot button expects signal\n", "    #@ short: \"Call\"\n    optional slot button expects signal\n");
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, new Values()));

        Assert.Equal("Call", program.Systems[0].FindPart("button")!.Short);
        Assert.Contains("    #@ short: \"Call\"\n    optional slot button expects signal\n", ProgramWriter.Save(program, new Values()));
    }

    /// <summary>An attribute Sprocket uses, written with the wrong type, is trouble.</summary>
    [Fact]
    public void AShortThatIsNoTextIsTrouble()
    {
        var text = Everywhere.Replace("    optional slot button expects signal\n", "    #@ short: 14\n    optional slot button expects signal\n");
        var trouble = ProgramReader.Load(text, new Automation(), new Values());
        Assert.Contains("expected text for the attribute short.", trouble);
    }

    /// <summary>A slot's <c>category</c> is read as the category it stands under, and written back on the slot alone.</summary>
    [Fact]
    public void ASlotsCategoryIsReadAndWrittenBack()
    {
        var text = Everywhere.Replace("    optional slot button expects signal\n", "    #@ category: \"Calls\"\n    optional slot button expects signal\n");
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, new Values()));

        var system = program.Systems[0];
        Assert.Equal("Calls", system.FindPart("button")!.Category);
        Assert.Equal("", system.FindPart("inner")!.Category);

        var written = ProgramWriter.Save(program, new Values());
        Assert.Contains("    required slot inner expects Door\n    #@ category: \"Calls\"\n    optional slot button expects signal\n", written);
        Assert.DoesNotContain("categories", written);
    }

    /// <summary>
    /// A program that lists its categories on the system has its slots stood as its panel showed them, the slots under no
    /// category first, and is written back with the slots in that order and no list.
    /// </summary>
    [Fact]
    public void AnEarlierListOfCategoriesStandsTheSlotsAsThePanelDid()
    {
        var text = Everywhere
            .Replace("#@ systemNote: \"one\"\n", "#@ systemNote: \"one\"\n#@ categories: [ \"Doors\" ]\n")
            .Replace("    required slot inner expects Door\n", "    #@ category: \"Doors\"\n    required slot inner expects Door\n");
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, new Values()));

        Assert.Equal(new[] { "button", "inner" }, program.Systems[0].Parts.Select(part => part.Id));

        var written = ProgramWriter.Save(program, new Values());
        Assert.True(written.IndexOf("slot button", StringComparison.Ordinal) < written.IndexOf("slot inner", StringComparison.Ordinal), written);
        Assert.Contains("#@ category: \"Doors\"", written);
        Assert.DoesNotContain("categories", written);
    }

    /// <summary>An earlier list of categories that cannot be read says nothing, and is not written back.</summary>
    [Theory]
    [InlineData("[ \"Doors\", 4 ]")]
    [InlineData("\"Doors\"")]
    public void AnEarlierListThatCannotBeReadIsDropped(string written)
    {
        var text = Everywhere.Replace("#@ systemNote: \"one\"\n", "#@ systemNote: \"one\"\n#@ categories: " + written + "\n");
        var program = new Automation();

        Assert.Equal("", ProgramReader.Load(text, program, new Values()));
        Assert.DoesNotContain("categories", ProgramWriter.Save(program, new Values()));
    }

    /// <summary>A <c>{</c> may stand on a line of its own under any statement that opens a block; only a system's header is written so.</summary>
    [Fact]
    public void ABraceMayStandOnItsOwnLine()
    {
        var text = Everywhere.Replace("    sequence 1 as Cycle {\n", "    sequence 1 as Cycle\n    {\n");
        var program = new Automation();
        Assert.Equal("", ProgramReader.Load(text, program, new Values()));

        var written = ProgramWriter.Save(program, new Values());
        Assert.Contains("    sequence 1 as Cycle {\n", written);
        Assert.Contains(" as MalTek.Airlock\n{\n", written);
    }
}
