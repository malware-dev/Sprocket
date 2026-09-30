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
        + "    when any sprocket.closed [ inner ] then run 1\n\n"
        + "    every 600t stop 1\n\n"
        + "    sequence 1 as Cycle {\n"
        + "        act sprocket.close on [ inner ]\n"
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
    [InlineData("        act sprocket.close on [ inner ]\n", "        act sprocket.close on [ inner ] garbage\n")]
    [InlineData("        act sprocket.close on [ inner ]\n", "        act sprocket.close [ inner ]\n")]
    [InlineData("        act sprocket.close on [ inner ]\n", "        act sprocket.close on inner\n")]
    [InlineData("        act sprocket.close on [ inner ]\n", "        act sprocket.close on [ nothing ]\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] 4\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ]\n")]
    [InlineData("        set Radius on [ inner ] to 4\n", "        set Radius on [ inner ] to 4 5\n")]
    [InlineData("    required block inner expects Door\n", "    required block inner expects Door garbage\n")]
    [InlineData("    required block inner expects Door\n", "    required block inner garbage\n")]
    [InlineData("    required trigger call to run 1\n", "    required trigger call to run 1 garbage\n")]
    [InlineData("    every 600t stop 1\n", "    every 600t stop 1 garbage\n")]
    [InlineData("    when any sprocket.closed [ inner ] then run 1\n", "    when any sprocket.closed [ inner ] garbage then run 1\n")]
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
    [InlineData("        act sprocket.close on [ inner ]\n", "        act sprocket.close on [ inner ] // later\n")]
    [InlineData("        act sprocket.close on [ inner ]\n", "        / act sprocket.close on [ inner ]\n")]
    [InlineData("        wait 60t\n", "        // wait 60t garbage\n")]
    [InlineData("        run 1\n", "        // run 9\n")]
    [InlineData("        act sprocket.close on [ inner ]\n", "        // act sprocket.close on [ nothing ]\n")]
    public void ALineNotAsTheLanguageSaysIsAnError(string was, string now)
    {
        Assert.Equal("", ProgramText.Load(Good, new Automation(), new Values()));
        Assert.Contains(was, Good);

        var after = new Automation();
        Assert.StartsWith("Line ", ProgramText.Load(Good.Replace(was, now), after, new Values()));
        Assert.False(after.Intact);
        Assert.Empty(after.Systems);
    }
}
