using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
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

    /// <summary>An alpha 15 blueprint that shows text on a billboard's back screen, naming the screen as its first argument.</summary>
    static string Screened(string screen) => "sprocket 1 alpha 15\n\n"
        + "let board = block 'LCDPanelsBlock/LargeBlockBillboard' [ 1, [ (1 0 0) ], \"Billboard\" ]\n\n"
        + "system blueprint \"5c1d7e2a-9b3f-4a6e-8d20-7f4b1c9e3a55\" as MalTek.Status {\n"
        + "    required slot status\n\n"
        + "    sequence 1 as Show {\n"
        + "        call ext.'show-text'(\"" + screen + "\", \"Docked\") on [ status ]\n"
        + "    }\n"
        + "}\n\n"
        + "instance 1 of \"5c1d7e2a-9b3f-4a6e-8d20-7f4b1c9e3a55\" as 'Installation 1' {\n"
        + "    status = [ board ]\n"
        + "}\n";

    /// <summary>An alpha 15 blueprint that checks how many things a sensor detects by the id the count had.</summary>
    const string ThingsDetected = "sprocket 1 alpha 15\n\n"
        + "system blueprint \"8a3e6f10-2d4c-4b7a-9e15-0c6f2a8d4b71\" as MalTek.Bay {\n"
        + "    required slot sensor\n\n"
        + "    when any ext.'things-detected' on [ sensor ] > 0 then run 1\n\n"
        + "    sequence 1 as Watch {\n"
        + "        if any ext.'things-detected' on [ sensor ] > 1 then {\n"
        + "        }\n"
        + "    }\n"
        + "}\n";

    /// <summary>An alpha 18 blueprint showing text through a block slot filled with <paramref name="block" />, with <paramref name="more" /> steps after it.</summary>
    static string OnABlockSlot(string block, string more = "") => "sprocket 1 alpha 18\n\n"
        + "let shown = block '" + block + "' [ 1, [ (1 0 0) ], \"Shown\" ]\n\n"
        + "system blueprint \"5c1d7e2a-9b3f-4a6e-8d20-7f4b1c9e3a55\" as MalTek.Status {\n"
        + "    required slot status\n\n"
        + "    sequence 1 as Show {\n"
        + "        call ext.'show-text'(\"Docked\") on [ status ]\n"
        + more
        + "    }\n"
        + "}\n\n"
        + "instance 1 of \"5c1d7e2a-9b3f-4a6e-8d20-7f4b1c9e3a55\" as 'Installation 1' {\n"
        + "    status = [ shown ]\n"
        + "}\n";

    /// <summary>The slot <c>status</c> of <paramref name="text" /> once read, with the world of <paramref name="ship" /> where it is not null.</summary>
    static Part Upgraded(string text, Rigged? ship, out AutomationSystem system)
    {
        var program = new Automation();
        Assert.Equal("", ship == null
            ? ProgramReader.Load(text, program, new Values())
            : ProgramReader.Load(text, program, ship.World.Values, out _, ship.World));
        system = program.Systems[0];
        return system.FindPart("status")!;
    }

    /// <summary>An alpha 18 block slot whose every step shows on a block with one screen holds that screen, keeping its name.</summary>
    [Fact]
    public void AnAlpha18OneScreenBlockSlotHoldsItsScreen()
    {
        var ship = new Rigged();
        ship.Rig.TextPanel(1, "Shown");
        ship.Tick();

        var part = Upgraded(OnABlockSlot("TextPanel/LargeTextPanel"), ship, out var system);

        Assert.Equal(HostTypes.Returned("screen")!.Id, part.Holds);
        Assert.Single(system.Instances[0].Facets("status")!);
        Assert.Equal(new[] { "status" }, ((Act)system.Sequences[0].Steps[0]).Targets);
    }

    /// <summary>An alpha 18 block slot a step also uses for the block itself is left holding blocks, for the repair.</summary>
    [Fact]
    public void AnAlpha18BlockSlotABlockStepUsesIsLeftAlone()
    {
        var ship = new Rigged();
        ship.Rig.TextPanel(1, "Shown");
        ship.Tick();

        var part = Upgraded(OnABlockSlot("TextPanel/LargeTextPanel", "        set ext.power on [ status ] to on\n"), ship, out _);

        Assert.Equal("", part.Holds);
    }

    /// <summary>An alpha 18 block slot on a block with more than one screen is left holding blocks, for the repair to ask which.</summary>
    [Fact]
    public void AnAlpha18BlockSlotOnABlockWithTwoScreensIsLeftAlone()
    {
        var ship = new Rigged();
        ship.Rig.Weld(new Billboard(1, "Shown"));
        ship.Tick();

        var part = Upgraded(OnABlockSlot("LCDPanelsBlock/LargeBlockBillboard"), ship, out _);

        Assert.Equal("", part.Holds);
    }

    /// <summary>An alpha 18 block slot read with no world, where its screens cannot be seen, is left holding blocks.</summary>
    [Fact]
    public void AnAlpha18BlockSlotWithNoWorldIsLeftAlone()
    {
        Assert.Equal("", Upgraded(OnABlockSlot("TextPanel/LargeTextPanel"), null, out _).Holds);
    }

    /// <summary>A screen named in a step moves onto a slot of screens of its own, which the installation fills with the screen of that name.</summary>
    [Fact]
    public void AnAlpha15ScreenIsHeldByItsSlot()
    {
        var ship = new Rigged();
        ship.Rig.Weld(new Billboard(1, "Billboard"));
        ship.Tick();
        var program = new Automation();

        Assert.Equal("", ProgramReader.Load(Screened("Back"), program, ship.World.Values, out _, ship.World));

        var system = program.Systems[0];
        var show = (Act)system.Sequences[0].Steps[0];
        var slot = Assert.Single(show.Targets);
        Assert.Equal(HostTypes.Returned("screen")!.Id, system.FindPart(slot)!.Holds);
        Assert.Equal(new[] { 2 }, system.Instances[0].Facets(slot));
        Assert.Equal(new[] { Literal.Of("Docked") }, show.Arguments);
    }

    /// <summary>An upgraded screen step is written in the current format, its screen in the installation, and reads back the same.</summary>
    [Fact]
    public void AnUpgradedScreenIsWrittenAndReadBack()
    {
        var ship = new Rigged();
        ship.Rig.Weld(new Billboard(1, "Billboard"));
        ship.Tick();
        var program = new Automation();
        ProgramReader.Load(Screened("Back"), program, ship.World.Values, out _, ship.World);

        var written = ProgramWriter.Save(program, ship.World.Values);

        Assert.Contains("expects ext.screen", written);
        Assert.Contains("ext.screen(2) on [ board ]", written);
        Assert.DoesNotContain("\"Back\"", written);
        var again = new Automation();
        Assert.Equal("", ProgramReader.Load(written, again, ship.World.Values));
        Assert.Equal(written, ProgramWriter.Save(again, ship.World.Values));
    }

    /// <summary>Show text with its text alone is a block with one screen's, which named no screen, and is left as it is.</summary>
    [Fact]
    public void ShowTextOnABlockWithOneScreenIsLeftAlone()
    {
        var text = Screened("Back").Replace("ext.'show-text'(\"Back\", \"Docked\")", "ext.'show-text'(\"Docked\")");
        var program = new Automation();

        Assert.Equal("", ProgramReader.Load(text, program, new Values()));

        var show = (Act)program.Systems[0].Sequences[0].Steps[0];
        Assert.Equal(new[] { "status" }, show.Targets);
        Assert.Equal(new[] { Literal.Of("Docked") }, show.Arguments);
    }

    /// <summary>Setting a screen up in alpha 15 is asked which app it runs since, after its content, which is left empty.</summary>
    [Fact]
    public void AnAlpha15SetUpLeavesItsAppAsItIs()
    {
        var ship = new Rigged();
        ship.Rig.Weld(new Billboard(1, "Billboard"));
        ship.Tick();
        var text = Screened("Back").Replace("ext.'show-text'(\"Back\", \"Docked\")",
            "ext.'set-up-screen'(\"Back\", \"text and images\", #FF0000, \"\", \"\", 2.5, \"\", \"\", \"\", \"\", \"\", \"\")");
        var program = new Automation();

        Assert.Equal("", ProgramReader.Load(text, program, ship.World.Values, out _, ship.World));

        var setUp = (Act)program.Systems[0].Sequences[0].Steps[0];
        Assert.Equal(12, setUp.Arguments.Count);
        Assert.Equal("text and images", setUp.Arguments[0].Text);
        Assert.Equal("", setUp.Arguments[1].Text);
        Assert.Equal(ValueKind.Color, setUp.Arguments[2].Kind);
    }

    /// <summary>A screen no block has by that name leaves the installation's slot of screens empty, to be filled on its panel.</summary>
    [Fact]
    public void AnAlpha15ScreenNoBlockHasIsLeftToFill()
    {
        var ship = new Rigged();
        ship.Rig.Weld(new Billboard(1, "Billboard"));
        ship.Tick();
        var program = new Automation();

        Assert.Equal("", ProgramReader.Load(Screened("Side"), program, ship.World.Values, out _, ship.World));

        var system = program.Systems[0];
        var slot = ((Act)system.Sequences[0].Steps[0]).Targets[0];
        Assert.Equal(HostTypes.Returned("screen")!.Id, system.FindPart(slot)!.Holds);
        Assert.Null(system.Instances[0].FilledWith(slot));
    }

    /// <summary>A sensor's count named things-detected is named detections wherever it is checked or watched.</summary>
    [Fact]
    public void AnAlpha15ThingsDetectedIsDetections()
    {
        Assert.Equal("", ProgramReader.Lone(ThingsDetected, new Values(), out var read));

        Assert.Equal("detections", ((If)read!.Sequences[0].Steps[0]).Compared.MemberId);
        Assert.Equal("detections", ((WhenTrigger)read.Triggers[0]).EventId);
    }

    /// <summary>Each of Sprocket's old switch actions becomes the setting of its power value, on the same blocks.</summary>
    [Fact]
    public void AnAlpha14SwitchSetsPower()
    {
        Assert.Equal("", ProgramReader.Lone(Switching, new Values(), out var read));

        var steps = read!.Sequences[0].Steps;
        Power((Act)steps[0], true);
        Power((Act)steps[2], false);
        Power((Act)read.Triggers[0].Does, false);
    }

    /// <summary>The power value named switched-on is named power wherever it is set, checked or watched.</summary>
    [Fact]
    public void AnAlpha14SwitchedOnIsPower()
    {
        Assert.Equal("", ProgramReader.Lone(SwitchedOn, new Values(), out var read));

        var steps = read!.Sequences[0].Steps;
        Assert.Equal("power", ((Act)steps[0]).ActionId);
        Assert.Equal("power", ((If)steps[1]).Compared.MemberId);
        Assert.Equal("power", ((WhenTrigger)read.Triggers[0]).EventId);

        var written = ProgramWriter.Blueprint(read);
        Assert.DoesNotContain("switched-on", written);
        Assert.Equal("", ProgramReader.Lone(written, new Values(), out var again));
        Assert.Equal(written, ProgramWriter.Blueprint(again!));
    }

    /// <summary>What was upgraded is written in the current format, and reads back the same.</summary>
    [Fact]
    public void AnUpgradedProgramIsWrittenInTheCurrentFormat()
    {
        ProgramReader.Lone(Switching, new Values(), out var read);

        var written = ProgramWriter.Blueprint(read!);

        Assert.StartsWith("sprocket 1 alpha 20", written);
        Assert.DoesNotContain("ext.'on'", written);
        Assert.DoesNotContain("ext.'off'", written);
        Assert.Equal("", ProgramReader.Lone(written, new Values(), out var again));
        Assert.Equal(written, ProgramWriter.Blueprint(again!));
    }

    /// <summary>
    /// An alpha 16 header on one line, with <c>last sequence</c> and <c>from</c>, reads as alpha 17: the number an attribute,
    /// <c>from blueprint</c>, and each clause on a line of its own.
    /// </summary>
    [Fact]
    public void AnAlpha16HeaderIsUpgraded()
    {
        const string text = "sprocket 1 alpha 16\n\n"
            + "system blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Lamp changed \"2026-09-14T19:02:11Z\" last sequence 4"
            + " from \"9d0f1a2b-3c4d-4e5f-8a6b-7c8d9e0f1a2b\" \"2026-09-10T08:00:00Z\" {\n"
            + "    required slot lamp expects InteriorLight\n\n"
            + "    sequence 1 as Flash {\n"
            + "        wait 60t\n"
            + "    }\n"
            + "}\n";

        Assert.Equal("", ProgramReader.Lone(text, new Values(), out var read));

        Assert.Equal(4, read!.LastSequenceNumber);
        Assert.Equal(new Guid("9d0f1a2b-3c4d-4e5f-8a6b-7c8d9e0f1a2b"), read.Origin);
        Assert.Contains("#@ lastSequence: 4\nsystem blueprint \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\" as MalTek.Lamp\n"
            + "changed \"2026-09-14T19:02:11Z\"\nfrom blueprint \"9d0f1a2b-3c4d-4e5f-8a6b-7c8d9e0f1a2b\" \"2026-09-10T08:00:00Z\"\n{\n",
            ProgramWriter.Linked(read));
    }

    /// <summary>A name that happens to be one of the old ids is a name, and is left as it is.</summary>
    [Fact]
    public void ANameIsLeftAlone()
    {
        var text = Switching.Replace("slot lamp", "slot 'on'").Replace("[ lamp ]", "[ 'on' ]");

        Assert.Equal("", ProgramReader.Lone(text, new Values(), out var read));

        Assert.Equal("on", ((Act)read!.Sequences[0].Steps[0]).Targets[0]);
    }

    /// <summary>Only the formats an upgrade is chosen for are read; every other draft, and any other version, is refused.</summary>
    [Theory]
    [InlineData("sprocket 1 alpha 13")]
    [InlineData("sprocket 1 alpha 21")]
    [InlineData("sprocket 2 alpha 14")]
    [InlineData("sprocket 1")]
    public void AnyOtherFormatIsRefused(string header)
    {
        var text = Switching.Replace("sprocket 1 alpha 14", header);

        Assert.NotEqual("", ProgramReader.Lone(text, new Values(), out var read));
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
