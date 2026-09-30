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
    const string Good = "sprocket 1 alpha 8\n\nblock 'Door/LargeBlockDoor' door1 [ 7, \"Inner\" ]\n\n"
        + "system \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Airlock {\n"
        + "    required block inner expects Door\n"
        + "    required trigger call to run 1\n\n"
        + "    when any 'sprocket'.closed [ inner ] then run 1\n\n"
        + "    every 600t stop 1\n\n"
        + "    sequence 1 as Cycle {\n"
        + "        act 'sprocket'.close on [ inner ]\n"
        + "        await\n"
        + "        wait 60t\n"
        + "        set Radius on [ inner ] to 4\n"
        + "        run 1\n"
        + "    }\n"
        + "}\n\n"
        + "instance 1 of MalTek.Airlock {\n"
        + "    inner = [ door1 ]\n"
        + "    call = [ door1:toolbar(1, 1, 1) ]\n"
        + "}\n";

    [Theory]
    [InlineData("        await\n", "        await now\n")]
    [InlineData("        await\n", "        await 5\n")]
    [InlineData("        wait 60t\n", "        wait 60t 20t\n")]
    [InlineData("        wait 60t\n", "        wait 60t garbage\n")]
    [InlineData("        run 1\n", "        run 1 2\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act 'sprocket'.close on [ inner ] garbage\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act 'sprocket'.close [ inner ]\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act 'sprocket'.close on inner\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act 'sprocket'.close on [ nothing ]\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] 4\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ]\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] to 4 5\n")]
    [InlineData("    required block inner expects Door\n", "    required block inner expects Door garbage\n")]
    [InlineData("    required block inner expects Door\n", "    required block inner garbage\n")]
    [InlineData("    required trigger call to run 1\n", "    required trigger call to run 1 garbage\n")]
    [InlineData("    every 600t stop 1\n", "    every 600t stop 1 garbage\n")]
    [InlineData("    when any 'sprocket'.closed [ inner ] then run 1\n", "    when any 'sprocket'.closed [ inner ] garbage then run 1\n")]
    [InlineData("    sequence 1 as Cycle {\n", "    sequence 1 as Cycle garbage {\n")]
    [InlineData("    inner = [ door1 ]\n", "    inner = [ door1 ] garbage\n")]
    [InlineData("    inner = [ door1 ]\n", "    inner = [ door9 ]\n")]
    [InlineData("    inner = [ door1 ]\n", "    outer = [ door1 ]\n")]
    [InlineData("instance 1 of MalTek.Airlock {\n", "instance 1 of MalTek.Airlock garbage {\n")]
    [InlineData("block 'Door/LargeBlockDoor' door1 [ 7, \"Inner\" ]\n", "block 'Door/LargeBlockDoor' door1 [ 7, \"Inner\" ] garbage\n")]
    [InlineData("block 'Door/LargeBlockDoor' door1 [ 7, \"Inner\" ]\n", "block door1 [ 7, \"Inner\" ]\n")]
    [InlineData("sprocket 1 alpha 8\n", "sprocket 1 alpha 8 garbage\n")]
    [InlineData("    required block inner expects Door\n", "    // required block inner expects Door\n")]
    [InlineData("    required trigger call to run 1\n", "    // required trigger call to run 1\n")]
    [InlineData("    every 600t stop 1\n", "    // every 600t stop 1\n")]
    [InlineData("    sequence 1 as Cycle {\n", "    // sequence 1 as Cycle {\n")]
    [InlineData("    inner = [ door1 ]\n", "    // inner = [ door1 ]\n")]
    [InlineData("block 'Door/LargeBlockDoor' door1 [ 7, \"Inner\" ]\n", "// block 'Door/LargeBlockDoor' door1 [ 7, \"Inner\" ]\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act 'sprocket'.close on [ inner ] // later\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        / act 'sprocket'.close on [ inner ]\n")]
    [InlineData("        wait 60t\n", "        // wait 60t garbage\n")]
    [InlineData("        run 1\n", "        // run 9\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        // act 'sprocket'.close on [ nothing ]\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act 'sprocket'.close on [ inner ] {\n        }\n")]
    [InlineData("block 'Door/LargeBlockDoor' door1 [ 7, \"Inner\" ]\n", "block 'Door/LargeBlockDoor' door1 [ 7, \"Inner\" ] {\n}\n")]
    [InlineData("    required block inner expects Door\n", "    required block inner expects Door {\n    }\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] to [ 4 ]\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] to (1 2 3)\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] to MalTek.Four\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act 'sprocket'.close on [ inner ] with [ 1, 2 ]\n")]
    [InlineData("sprocket 1 alpha 8\n", "sprocket 1.5 alpha 8\n")]
    [InlineData("sprocket 1 alpha 8\n", "sprocket 1 alpha 8.9\n")]
    [InlineData("        run 1\n", "        Run 1\n")]
    [InlineData("        await\n", "        Await\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act 'sprocket'.close ON [ inner ]\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act 'sprocket'.close on [ Inner ]\n")]
    [InlineData("    inner = [ door1 ]\n", "    inner = [ Door1 ]\n")]
    [InlineData("instance 1 of MalTek.Airlock {\n", "instance 1 of maltek.airlock {\n")]
    [InlineData("sprocket 1 alpha 8\n", "Sprocket 1 alpha 8\n")]
    [InlineData("        run 1\n", "        run all\n")]
    [InlineData("        run 1\n", "        stop every\n")]
    [InlineData("    required block inner expects Door\n", "    required block inner expects block\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set on on [ inner ] to 4\n")]
    [InlineData("    inner = [ door1 ]\n", "    inner = [ door1 ]\n    inner = [ door1 ]\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act 'sprocket'.close on [ call ]\n")]
    [InlineData("    when any 'sprocket'.closed [ inner ] then run 1\n", "    when any 'sprocket'.closed [ call ] then run 1\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act sprocket.close on [ inner ]\n")]
    [InlineData("        act 'sprocket'.close on [ inner ]\n", "        act 'sprocket'.run on [ inner ]\n")]
    [InlineData("    when any 'sprocket'.closed [ inner ] then run 1\n", "    when any sprocket.closed [ inner ] then run 1\n")]
    public void ALineNotAsTheLanguageSaysIsAnError(string was, string now)
    {
        Assert.Equal("", ProgramText.Load(Good, new Automation(), new Values()));
        Assert.Contains(was, Good);

        var after = new Automation();
        Assert.StartsWith("Line ", ProgramText.Load(Good.Replace(was, now), after, new Values()));
        Assert.False(after.Intact);
        Assert.Empty(after.Systems);
    }

    /// <summary>A header that will not parse is damage to mend, not a program from another version.</summary>
    [Fact]
    public void AHeaderThatWillNotParseIsDamage()
    {
        var after = new Automation();
        var said = ProgramText.Load(Good.Replace("sprocket 1 alpha 8\n", "sprocket 1 alpha 8 ,\n"), after, new Values());

        Assert.StartsWith("Line 1: ", said);
        Assert.False(after.Intact);
        Assert.False(after.Incompatible);
    }

    /// <summary>A name in parts is named whole where trouble names it.</summary>
    [Fact]
    public void TroubleSpellsANameInParts()
    {
        var said = ProgramText.Load(Good.Replace("instance 1 of MalTek.Airlock {", "instance 1 of MalTek.Nope {"), new Automation(), new Values());

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

    /// <summary>What adds a member is the host's to know, so one it does not have reads as written, is written back as it was, and is never taken for its own.</summary>
    [Theory]
    [InlineData("Gearbox")]
    [InlineData("Sprocket")]
    public void AMemberAddedByAnotherReadsAsWritten(string provider)
    {
        var script = Good.Replace("act 'sprocket'.close on", "act " + provider + ".close on").Replace("when any 'sprocket'.closed", "when any " + provider + ".closed");
        var after = new Automation();

        Assert.Equal("", ProgramText.Load(script, after, new Values()));
        var saved = ProgramText.Save(after, new Values());
        Assert.Contains("act " + provider + ".close on", saved);
        Assert.Contains("when any " + provider + ".closed", saved);

        var act = (Act)after.Sequences[0].Steps[0];
        Assert.Equal(provider, act.Provider);
        Assert.Null(act.Names(new[] { new EntityAction("close", "close", null, null) }));
    }

    /// <summary>What somebody wrote above the header, a block, an instance and one of its answers is kept, and written back.</summary>
    [Fact]
    public void EveryAnnotationIsKept()
    {
        var script = Good
            .Replace("sprocket 1 alpha 8\n", "# The ship's automation.\nsprocket 1 alpha 8\n")
            .Replace("block 'Door/LargeBlockDoor'", "# The inner door.\nblock 'Door/LargeBlockDoor'")
            .Replace("instance 1 of MalTek.Airlock {\n", "# The front airlock.\ninstance 1 of MalTek.Airlock {\n")
            .Replace("    inner = [ door1 ]\n", "    # The door to the ship.\n    inner = [ door1 ]\n");
        var after = new Automation();

        Assert.Equal("", ProgramText.Load(script, after, new Values()));
        var saved = ProgramText.Save(after, new Values());

        Assert.Contains("# The ship's automation.\nsprocket 1 alpha 8", saved);
        Assert.Contains("# The inner door.\nblock ", saved);
        Assert.Contains("# The front airlock.\ninstance 1 of ", saved);
        Assert.Contains("    # The door to the ship.\n    inner = [ door1 ]", saved);
    }

    /// <summary>A run of a sequence that is not there is reported on its own line.</summary>
    [Fact]
    public void ARunOfNothingIsReportedWhereItIsWritten()
    {
        var script = Good.Replace("        run 1\n", "        run 9\n");
        var line = script.Substring(0, script.IndexOf("        run 9", StringComparison.Ordinal)).Split('\n').Length;

        var said = ProgramText.Load(script, new Automation(), new Values());

        Assert.Equal(line, ProgramText.LineOf(said));
    }
}
