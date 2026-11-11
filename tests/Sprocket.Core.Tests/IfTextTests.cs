using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>An <c>if</c>, its <c>else if</c> and its <c>else</c> are read as the lines they are, and written back as blocks.</summary>
public class IfTextTests
{
    const string Identity = "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43";

    const string Top = "sprocket 1 alpha 24\n\n"
        + "let door1 = block 'Door/LargeBlockDoor' [ 1 ]\n"
        + "let door2 = block 'Door/LargeBlockDoor' [ 2 ]\n"
        + "\n"
        + "system \"" + Identity + "\" as Airlock\n{\n"
        + "    sequence 1 {\n";

    const string Bottom = "    }\n}\n";

    readonly Values _store = new Values();

    string Load(string text, Automation program) => ProgramReader.Load(text, program, _store);

    static string Steps(string lines) => Top + lines + Bottom;

    /// <summary>A whole if, nested, reads and writes back exactly as it was written.</summary>
    [Fact]
    public void AnIfReadsAndWritesBackAsItWas()
    {
        var written = Steps(
            "        if all ext.status on [ door1 ] == closed then {\n" +
            "            call ext.open on [ door2 ]\n" +
            "            if any ext.open on [ door2 ] == on then {\n" +
            "                await\n" +
            "            }\n" +
            "        }\n" +
            "        else if any ext.status on [ door2 ] != closed then {\n" +
            "            call ext.close on [ door1 ]\n" +
            "        }\n" +
            "        else {\n" +
            "            call SetColor(255, 136, 0) on [ door1 ]\n" +
            "        }\n" +
            "        await\n");

        var program = new Automation();
        Assert.Equal("", Load(written, program));
        Assert.Equal(written, ProgramWriter.Save(program, _store));
    }

    /// <summary>The lines of an if are its line, its branches' lines and an end, in the order they are read.</summary>
    [Fact]
    public void AnIfIsItsLines()
    {
        var program = new Automation();
        Assert.Equal("", Load(Steps(
            "        if all ext.status on [ door1 ] == closed then {\n" +
            "            call ext.open on [ door2 ]\n" +
            "        }\n" +
            "        else {\n" +
            "            call ext.close on [ door2 ]\n" +
            "        }\n" +
            "        await\n"), program));

        var steps = program.Systems[0].Sequences[0].Steps;
        Assert.IsType<If>(steps[0]);
        Assert.IsType<Act>(steps[1]);
        var branch = Assert.IsType<Else>(steps[2]);
        Assert.Null(branch.Test);
        Assert.IsType<Act>(steps[3]);
        Assert.IsType<EndIf>(steps[4]);
        Assert.IsType<Await>(steps[5]);

        var test = ((If)steps[0]).Compared!;
        Assert.Equal(Quantifier.All, test.Quantifier);
        Assert.Equal("status", test.MemberId);
        Assert.Equal("ext", test.Provider);
        Assert.Equal(new[] { "door1" }, test.Targets);
        Assert.Equal(Relation.Is, test.Relation);
        Assert.Equal(Literal.Name("closed"), test.Value);
    }

    /// <summary>What an if may not be is trouble, and the program is read as nothing.</summary>
    [Theory]
    [InlineData("        else {\n        }\n")]
    [InlineData("        if all ext.status on [ door1 ] == closed then {\n        }\n        else {\n        }\n        else {\n        }\n")]
    [InlineData("        if all ext.status on [ door1 ] == closed {\n        }\n")]
    [InlineData("        if all ext.closed on [ door1 ] = on then {\n        }\n")]
    [InlineData("        if ext.closed on [ door1 ] == on then {\n        }\n")]
    [InlineData("        if all ext.status on [ door9 ] == closed then {\n        }\n")]
    [InlineData("        if all ext.status on [ door1 ] == closed then\n")]
    [InlineData("        if all ext.closed on [ door1 ] == [ 1 ] then {\n        }\n")]
    [InlineData("        if all ext.status on [ door1 ] == closed then {\n        }\n        // else {\n        }\n")]
    public void AnIfThatIsWrongIsTrouble(string lines)
    {
        var program = new Automation();
        Assert.NotEqual("", Load(Steps(lines), program));
        Assert.False(program.Intact);
    }

    /// <summary>A member's arguments are separated by commas, one between each two, and a comma stands nowhere else outside a list.</summary>
    [Theory]
    [InlineData("        call SetColor(255 136 0) on [ door1 ]\n")]
    [InlineData("        call SetColor(255, 136,) on [ door1 ]\n")]
    [InlineData("        call SetColor(, 255) on [ door1 ]\n")]
    [InlineData("        call ext.close on [ door1 ], [ door2 ]\n")]
    public void ValuesWithoutTheirCommasAreTrouble(string lines)
    {
        var program = new Automation();
        Assert.NotEqual("", Load(Steps(lines), program));
    }

    /// <summary>A switched-off if is written switched off, its branches with it.</summary>
    [Fact]
    public void ASwitchedOffIfIsWrittenOff()
    {
        var written = Steps(
            "        // if all ext.status on [ door1 ] == closed then {\n" +
            "            call ext.open on [ door2 ]\n" +
            "        }\n" +
            "        else {\n" +
            "            call ext.close on [ door2 ]\n" +
            "        }\n");

        var program = new Automation();
        Assert.Equal("", Load(written, program));
        Assert.True(program.Systems[0].Sequences[0].Steps[0].IsOff);
        Assert.Equal(written, ProgramWriter.Save(program, _store));
    }
}
