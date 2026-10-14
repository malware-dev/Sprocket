using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A program written in a format this build upgrades from loads as the current format; any other format is refused whole.
/// </summary>
public class UpgradeTests
{
    /// <summary>An alpha 14 blueprint whose sequence and trigger switch a lamp with Sprocket's old switch actions.</summary>
    const string Switching = "sprocket 1 alpha 14\n\n"
        + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Lamp {\n"
        + "    required slot lamp expects InteriorLight\n\n"
        + "    every 600t call ext.'off' on [ lamp ]\n\n"
        + "    sequence 1 as Flash {\n"
        + "        call ext.'on' on [ lamp ]\n"
        + "        wait 60t\n"
        + "        call ext.'off' on [ lamp ]\n"
        + "    }\n"
        + "}\n";

    /// <summary>An alpha 14 blueprint that sets, checks and watches the power value by the id it had before it was named power.</summary>
    const string SwitchedOn = "sprocket 1 alpha 14\n\n"
        + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Lamp {\n"
        + "    required slot lamp expects InteriorLight\n\n"
        + "    when any ext.'switched-on' on [ lamp ] == off then run 1\n\n"
        + "    sequence 1 as Flash {\n"
        + "        set ext.'switched-on' on [ lamp ] to on\n"
        + "        if any ext.'switched-on' on [ lamp ] == on then {\n"
        + "        }\n"
        + "    }\n"
        + "}\n";

    /// <summary>Each of Sprocket's old switch actions becomes the setting of its power value, on the same blocks.</summary>
    [Fact]
    public void AnAlpha14SwitchSetsPower()
    {
        Assert.Equal("", ProgramText.Lone(Switching, new Values(), out var read));

        var steps = read!.Sequences[0].Steps;
        Power((Act)steps[0], true);
        Power((Act)steps[2], false);
        Power((Act)read.Triggers[0].Does, false);
    }

    /// <summary>The power value named switched-on is named power wherever it is set, checked or watched.</summary>
    [Fact]
    public void AnAlpha14SwitchedOnIsPower()
    {
        Assert.Equal("", ProgramText.Lone(SwitchedOn, new Values(), out var read));

        var steps = read!.Sequences[0].Steps;
        Assert.Equal("power", ((Act)steps[0]).ActionId);
        Assert.Equal("power", ((If)steps[1]).Test.MemberId);
        Assert.Equal("power", ((WhenTrigger)read.Triggers[0]).EventId);

        var written = ProgramText.Blueprint(read);
        Assert.DoesNotContain("switched-on", written);
        Assert.Equal("", ProgramText.Lone(written, new Values(), out var again));
        Assert.Equal(written, ProgramText.Blueprint(again!));
    }

    /// <summary>What was upgraded is written in the current format, and reads back the same.</summary>
    [Fact]
    public void AnUpgradedProgramIsWrittenInTheCurrentFormat()
    {
        ProgramText.Lone(Switching, new Values(), out var read);

        var written = ProgramText.Blueprint(read!);

        Assert.StartsWith("sprocket 1 alpha 15", written);
        Assert.DoesNotContain("ext.'on'", written);
        Assert.DoesNotContain("ext.'off'", written);
        Assert.Equal("", ProgramText.Lone(written, new Values(), out var again));
        Assert.Equal(written, ProgramText.Blueprint(again!));
    }

    /// <summary>A name that happens to be one of the old ids is a name, and is left as it is.</summary>
    [Fact]
    public void ANameIsLeftAlone()
    {
        var text = Switching.Replace("slot lamp", "slot 'on'").Replace("[ lamp ]", "[ 'on' ]");

        Assert.Equal("", ProgramText.Lone(text, new Values(), out var read));

        Assert.Equal("on", ((Act)read!.Sequences[0].Steps[0]).Targets[0]);
    }

    /// <summary>Only the formats an upgrade is chosen for are read; every other draft, and any other version, is refused.</summary>
    [Theory]
    [InlineData("sprocket 1 alpha 13")]
    [InlineData("sprocket 1 alpha 16")]
    [InlineData("sprocket 2 alpha 14")]
    [InlineData("sprocket 1")]
    public void AnyOtherFormatIsRefused(string header)
    {
        var text = Switching.Replace("sprocket 1 alpha 14", header);

        Assert.NotEqual("", ProgramText.Lone(text, new Values(), out var read));
        Assert.Null(read);
    }

    static void Power(Act act, bool on)
    {
        Assert.True(act.Sets);
        Assert.Equal("power", act.ActionId);
        Assert.Equal(BlockKinds.Own, act.Provider);
        Assert.Equal(on, Assert.Single(act.Arguments).Flag);
        Assert.Equal("lamp", Assert.Single(act.Targets));
    }
}
