using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A script with anything more or less than the language says is an error, and nothing of it is kept.</summary>
/// <remarks>
/// Each case changes one line of a script that reads clean. The reader once let almost all of these through, reading what it
/// understood and leaving the rest, so a line somebody broke by hand ran as something they did not write.
/// </remarks>
public class MalformedTests
{
    /// <summary>A script that reads clean, using every statement the cases below break.</summary>
    const string Good = "sprocket 1 alpha 18\n\n"
        + "let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n"
        + "let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 8, \"Button\" ]\n"
        + "let halt = ext.toolbar(1, 1, 2) on [ panel1 ]\n"
        + "let bell = ext.toolbar(1, 1, 3) on [ panel1 ]\n\n"
        + "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Door\n{\n"
        + "    whenever halt then call ext.open on [ door1 ]\n"
        + "}\n\n"
        + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock\n{\n"
        + "    required slot inner expects Door\n"
        + "    optional slot button expects signal\n\n"
        + "    whenever button then run 1\n\n"
        + "    whenever ext.'finishes-closing' on [ inner ] then run 1\n\n"
        + "    every 600t stop 1\n\n"
        + "    sequence 1 as Cycle {\n"
        + "        call ext.close on [ inner ]\n"
        + "        await\n"
        + "        wait 60t\n"
        + "        set Radius on [ inner ] to 4\n"
        + "        run 1\n"
        + "    }\n"
        + "}\n\n"
        + "instance 1 of MalTek.Airlock {\n"
        + "    inner = [ door1 ]\n"
        + "    button = [ ext.toolbar(1, 1, 1) on [ panel1 ], bell ]\n"
        + "}\n";

    [Theory]
    [InlineData("        await\n", "        await now\n")]
    [InlineData("        await\n", "        await 5\n")]
    [InlineData("        wait 60t\n", "        wait 60t 20t\n")]
    [InlineData("        wait 60t\n", "        wait 60t garbage\n")]
    [InlineData("        run 1\n", "        run 1 2\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close on [ inner ] garbage\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close [ inner ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close on inner\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close on [ nothing ]\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] 4\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ]\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] to 4 5\n")]
    [InlineData("    required slot inner expects Door\n", "    required slot inner expects Door garbage\n")]
    [InlineData("    required slot inner expects Door\n", "    required slot inner garbage\n")]
    [InlineData("    whenever button then run 1\n", "    whenever button then run 1 garbage\n")]
    [InlineData("    whenever button then run 1\n", "    whenever button run 1\n")]
    [InlineData("    whenever button then run 1\n", "    whenever inner then run 1\n")]
    [InlineData("    whenever button then run 1\n", "    whenever halt then run 1\n")]
    [InlineData("    whenever halt then call ext.open on [ door1 ]\n", "    whenever door1 then call ext.open on [ door1 ]\n")]
    [InlineData("    whenever halt then call ext.open on [ door1 ]\n", "    whenever halt then call ext.open on [ halt ]\n")]
    [InlineData("    whenever halt then call ext.open on [ door1 ]\n", "    whenever halt then call ext.open on [ inner ]\n")]
    [InlineData("    whenever halt then call ext.open on [ door1 ]\n", "    whenever halt then await\n")]
    [InlineData("    optional slot button expects signal\n", "    optional slot button expects signal garbage\n")]
    [InlineData("    optional slot button expects signal\n", "    optional block button expects signal\n")]
    [InlineData("    optional slot button expects signal\n", "    optional slot call expects signal\n")]
    [InlineData("system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Door\n{\n", "system \"9b1d0e7a-4c2f-4a8e-b3d5-6f7a8c9e0d12\" as MalTek.Door\n{\n    required slot lamp expects InteriorLight\n")]
    [InlineData("instance 1 of MalTek.Airlock {\n", "instance 1 of MalTek.Door {\n")]
    [InlineData("let halt = ext.toolbar(1, 1, 2) on [ panel1 ]\n", "let halt = ext.toolbar(1, 1) on [ panel1 ]\n")]
    [InlineData("let halt = ext.toolbar(1, 1, 2) on [ panel1 ]\n", "let halt = ext.toolbar(1, 0, 2) on [ panel1 ]\n")]
    [InlineData("let halt = ext.toolbar(1, 1, 2) on [ panel1 ]\n", "let halt = ext.shelf(1, 1, 2) on [ panel1 ]\n")]
    [InlineData("let halt = ext.toolbar(1, 1, 2) on [ panel1 ]\n", "let halt = ext.toolbar(1, 1, 2) on [ panel9 ]\n")]
    [InlineData("let halt = ext.toolbar(1, 1, 2) on [ panel1 ]\n", "let halt = ext.toolbar(1, 1, 2) on [ panel1 ] garbage\n")]
    [InlineData("let halt = ext.toolbar(1, 1, 2) on [ panel1 ]\n", "let halt = halt\n")]
    [InlineData("let halt = ext.toolbar(1, 1, 2) on [ panel1 ]\n", "let halt = 4\n")]
    [InlineData("let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 8, \"Button\" ]\n", "")]
    [InlineData("let panel1 = block 'ButtonPanel/ButtonPanelLarge' [ 8, \"Button\" ]\n", "let door1 = block 'ButtonPanel/ButtonPanelLarge' [ 8, \"Button\" ]\n")]
    [InlineData("let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n", "let door1 block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n")]
    [InlineData("let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n", "let let = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n")]
    [InlineData("    inner = [ door1 ]\n", "    inner = [ halt ]\n")]
    [InlineData("    button = [ ext.toolbar(1, 1, 1) on [ panel1 ], bell ]\n", "    button = [ door1 ]\n")]
    [InlineData("    button = [ ext.toolbar(1, 1, 1) on [ panel1 ], bell ]\n", "    button = [ ext.toolbar(1, 1, 1) on [ panel1 ], halt ]\n")]
    [InlineData("    button = [ ext.toolbar(1, 1, 1) on [ panel1 ], bell ]\n", "    button = [ ext.toolbar(1, 1, 2) on [ panel1 ] ]\n")]
    [InlineData("    button = [ ext.toolbar(1, 1, 1) on [ panel1 ], bell ]\n", "    button = [ bell, ext.toolbar(1, 1, 3) on [ panel1 ] ]\n")]
    [InlineData("    whenever halt then call ext.open on [ door1 ]\n", "    whenever halt then call ext.open on [ door1 ]\n    whenever halt then run 1\n")]
    [InlineData("    whenever button then run 1\n", "    whenever button then run 1\n    whenever button then stop 1\n")]
    [InlineData("    button = [ ext.toolbar(1, 1, 1) on [ panel1 ], bell ]\n", "    button = [ ext.toolbar(1, 1) on [ panel1 ] ]\n")]
    [InlineData("    button = [ ext.toolbar(1, 1, 1) on [ panel1 ], bell ]\n", "    button = [ ext.toolbar(1, 1, 1) on [ door9 ] ]\n")]
    [InlineData("    every 600t stop 1\n", "    every 600t stop 1 garbage\n")]
    [InlineData("    whenever ext.'finishes-closing' on [ inner ] then run 1\n", "    when any ext.status on [ inner ] == closed garbage then run 1\n")]
    [InlineData("    sequence 1 as Cycle {\n", "    sequence 1 as Cycle garbage {\n")]
    [InlineData("    inner = [ door1 ]\n", "    inner = [ door1 ] garbage\n")]
    [InlineData("    inner = [ door1 ]\n", "    inner = [ door9 ]\n")]
    [InlineData("    inner = [ door1 ]\n", "    outer = [ door1 ]\n")]
    [InlineData("instance 1 of MalTek.Airlock {\n", "instance 1 of MalTek.Airlock garbage {\n")]
    [InlineData("let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n", "let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ] garbage\n")]
    [InlineData("let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n", "block door1 [ 7, \"Inner\" ]\n")]
    [InlineData("sprocket 1 alpha 18\n", "sprocket 1 alpha 18 garbage\n")]
    [InlineData("    required slot inner expects Door\n", "    // required slot inner expects Door\n")]
    [InlineData("    whenever button then run 1\n", "    // whenever button then run 1\n")]
    [InlineData("let halt = ext.toolbar(1, 1, 2) on [ panel1 ]\n", "// let halt = ext.toolbar(1, 1, 2) on [ panel1 ]\n")]
    [InlineData("    every 600t stop 1\n", "    // every 600t stop 1\n")]
    [InlineData("    sequence 1 as Cycle {\n", "    // sequence 1 as Cycle {\n")]
    [InlineData("    inner = [ door1 ]\n", "    // inner = [ door1 ]\n")]
    [InlineData("let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n", "// let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close on [ inner ] // later\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        / call ext.close on [ inner ]\n")]
    [InlineData("        wait 60t\n", "        // wait 60t garbage\n")]
    [InlineData("        run 1\n", "        // run 9\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        // call ext.close on [ nothing ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close on [ inner ] {\n        }\n")]
    [InlineData("let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ]\n", "let door1 = block 'Door/LargeBlockDoor' [ 7, \"Inner\" ] {\n}\n")]
    [InlineData("    required slot inner expects Door\n", "    required slot inner expects Door {\n    }\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] to [ 4 ]\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] to (1 2 3)\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] to MalTek.Four\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close([ 1, 2 ]) on [ inner ]\n")]
    [InlineData("sprocket 1 alpha 18\n", "sprocket 1.5 alpha 11\n")]
    [InlineData("sprocket 1 alpha 18\n", "sprocket 1 alpha 18.9\n")]
    [InlineData("        run 1\n", "        Run 1\n")]
    [InlineData("        await\n", "        Await\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close ON [ inner ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close on [ Inner ]\n")]
    [InlineData("    inner = [ door1 ]\n", "    inner = [ Door1 ]\n")]
    [InlineData("instance 1 of MalTek.Airlock {\n", "instance 1 of maltek.airlock {\n")]
    [InlineData("sprocket 1 alpha 18\n", "Sprocket 1 alpha 11\n")]
    [InlineData("        run 1\n", "        run all\n")]
    [InlineData("        run 1\n", "        stop every\n")]
    [InlineData("    required slot inner expects Door\n", "    required slot inner expects block\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set on on [ inner ] to 4\n")]
    [InlineData("    inner = [ door1 ]\n", "    inner = [ door1 ]\n    inner = [ door1 ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close on [ button ]\n")]
    [InlineData("    whenever ext.'finishes-closing' on [ inner ] then run 1\n", "    whenever ext.'finishes-closing' on [ button ] then run 1\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call sprocket.close on [ inner ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.on on [ inner ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        act ext.close on [ inner ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close on [ door1 ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.run on [ inner ]\n")]
    [InlineData("    whenever ext.'finishes-closing' on [ inner ] then run 1\n", "    when any sprocket.closed on [ inner ] == on then run 1\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close() on [ inner ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close(1,) on [ inner ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close(1 2) on [ inner ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close(1) [ inner ]\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close(1)\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close on [ inner ] with 2\n")]
    [InlineData("        call ext.close on [ inner ]\n", "        call ext.close on [ inner ], 2\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius(1) on [ inner ] to 4\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set other.radius on [ inner ] to 4\n")]
    [InlineData("    whenever ext.'finishes-closing' on [ inner ] then run 1\n", "    when any ext.closed on [ inner ] then run 1\n")]
    [InlineData("    whenever ext.'finishes-closing' on [ inner ] then run 1\n", "    when any ext.closed on [ inner ] == then run 1\n")]
    [InlineData("    whenever ext.'finishes-closing' on [ inner ] then run 1\n", "    when any ext.closed on [ inner ] < on then run 1\n")]
    [InlineData("    whenever ext.'finishes-closing' on [ inner ] then run 1\n", "    when sum ext.closed on [ inner ] == on then run 1\n")]
    [InlineData("    whenever ext.'finishes-closing' on [ inner ] then run 1\n", "    when ext.closed on [ inner ] == on then run 1\n")]
    [InlineData("    whenever ext.'finishes-closing' on [ inner ] then run 1\n", "    when any ext.closed on [ inner ] = on then run 1\n")]
    [InlineData("    whenever ext.'finishes-closing' on [ inner ] then run 1\n", "    when any ext.closed on [ inner ] ! on then run 1\n")]
    public void ALineNotAsTheLanguageSaysIsAnError(string was, string now)
    {
        Assert.Equal("", ProgramReader.Load(Good, new Automation(), new Values()));
        Assert.Contains(was, Good);

        var after = new Automation();
        Assert.StartsWith("Line ", ProgramReader.Load(Good.Replace(was, now), after, new Values()));
        Assert.False(after.Intact);
        Assert.Empty(after.Systems);
    }

    /// <summary>A header that will not parse is damage to mend, not a program from another version.</summary>
    [Fact]
    public void AHeaderThatWillNotParseIsDamage()
    {
        var after = new Automation();
        var said = ProgramReader.Load(Good.Replace("sprocket 1 alpha 18\n", "sprocket 1 alpha 18 ,\n"), after, new Values());

        Assert.StartsWith("Line 1: ", said);
        Assert.False(after.Intact);
        Assert.False(after.Incompatible);
    }

    /// <summary>A name in parts is named whole where trouble names it.</summary>
    [Fact]
    public void TroubleSpellsANameInParts()
    {
        var said = ProgramReader.Load(Good.Replace("instance 1 of MalTek.Airlock {", "instance 1 of MalTek.Nope {"), new Automation(), new Values());

        Assert.Contains("'MalTek.Nope'", said);
    }

    /// <summary>The switches are never switched off by a leading <c>//</c>.</summary>
    [Fact]
    public void ASwitchSwitchedOffIsAnError()
    {
        var program = new Automation();
        Assert.StartsWith("Line 1: ", StateText.Load("// off \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\"\n", program, null));
        Assert.False(program.Intact);
    }

    /// <summary>A member in a namespace this host has not got is an error.</summary>
    [Theory]
    [InlineData("        call ext.close on [ inner ]\n", "        call Gearbox.close on [ inner ]\n", "no namespace 'Gearbox'")]
    [InlineData("        call ext.close on [ inner ]\n", "        call Sprocket.close on [ inner ]\n", "no namespace 'Sprocket'")]
    [InlineData("    whenever ext.'finishes-closing' on [ inner ] then run 1\n", "    when any Gearbox.closed on [ inner ] == on then run 1\n", "no namespace 'Gearbox'")]
    public void AMemberOrMomentTheHostHasNotGotIsAnError(string was, string now, string said)
    {
        var after = new Automation();
        Assert.Contains(said, ProgramReader.Load(Good.Replace(was, now), after, new Values()));
        Assert.False(after.Intact);
    }


    /// <summary>What somebody wrote above the header, a variable, an instance and one of its answers is kept, and written back.</summary>
    [Fact]
    public void EveryAnnotationIsKept()
    {
        var script = Good
            .Replace("sprocket 1 alpha 18\n", "# The ship's automation.\nsprocket 1 alpha 18\n")
            .Replace("let door1 = ", "# The inner door.\nlet door1 = ")
            .Replace("let halt = ", "# The stop button.\nlet halt = ")
            .Replace("instance 1 of MalTek.Airlock {\n", "# The front airlock.\ninstance 1 of MalTek.Airlock {\n")
            .Replace("    inner = [ door1 ]\n", "    # The door to the ship.\n    inner = [ door1 ]\n");
        var after = new Automation();

        Assert.Equal("", ProgramReader.Load(script, after, new Values()));
        var saved = ProgramWriter.Save(after, new Values());

        Assert.Contains("# The ship's automation.\nsprocket 1 alpha 18", saved);
        Assert.Contains("# The inner door.\nlet door1 = ", saved);
        Assert.Contains("# The stop button.\nlet halt = ", saved);

        Assert.Contains("# The front airlock.\ninstance 1 of ", saved);
        Assert.Contains("    # The door to the ship.\n    inner = [ door1 ]", saved);
    }

    /// <summary>A run of a sequence that is not there is reported on its own line.</summary>
    [Fact]
    public void ARunOfNothingIsReportedWhereItIsWritten()
    {
        var script = Good.Replace("        run 1\n", "        run 9\n");
        var line = script.Substring(0, script.IndexOf("        run 9", StringComparison.Ordinal)).Split('\n').Length;

        var said = ProgramReader.Load(script, new Automation(), new Values());

        Assert.Equal(line, ProgramReader.LineOf(said));
    }
}
