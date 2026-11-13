using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A logic system kept to one block lists only there, while it works and that block is surely there.</summary>
public class ListedOnTests
{
    const long Office = 300;
    const long Bay = 301;
    const long Pasted = 302;

    /// <summary>A system kept to a block lists from that block and from no other, one kept to nothing lists everywhere, and so does one kept to a block that isn't found, so nothing is out of reach.</summary>
    [Fact]
    public void AKeptSystemListsOnlyOnItsBlock()
    {
        var rig = Kept();
        var system = rig.Editor.Program.Systems[0];

        rig.Editor.OpenedFrom = Office;
        Assert.True(rig.Editor.Lists(system));

        rig.Editor.OpenedFrom = Bay;
        Assert.False(rig.Editor.Lists(system));

        system.ListedOn = "";

        rig.Editor.OpenedFrom = Bay;

        Assert.True(rig.Editor.Lists(system));

        system.ListedOn = "nowhere";

        rig.Editor.OpenedFrom = Bay;

        Assert.True(rig.Editor.Lists(system));
    }

    /// <summary>A kept block that comes back under a new id, as a pasted copy's blocks do, is found again by what else it is known by.</summary>
    [Fact]
    public void AKeptBlockIsFoundAgainUnderANewId()
    {
        var made = Rig.Demo();
        var office = made.Programmable(Office, "Back Office PB");
        made.Programmable(Bay, "Bay PB");
        var rig = Simulation.On(made);
        rig.Editor.NewButtonSystem("Doors");
        rig.Tick();
        var system = rig.Editor.Program.Systems[0];
        system.ListedOn = Declared(rig.Editor, Office);
        rig.Editor.Program.Edited();
        rig.Tick();

        made.Grind(office);
        made.Programmable(Pasted, "Back Office PB");
        for (var tick = 0; tick < 120; tick++)
            rig.Tick();

        Assert.Equal(Pasted, KeptId(rig.Editor, system));
    }

    /// <summary>The block a system is kept to goes in the construct's program, and comes back from it.</summary>
    [Fact]
    public void TheBlockIsSavedWithTheProgram()
    {
        var rig = Kept();
        var system = rig.Editor.Program.Systems[0];

        var saved = ProgramWriter.Save(rig.Editor.Program, new Values());
        Assert.Contains("#@ listedOn: \"" + system.ListedOn + "\"", saved);

        var read = new Automation();
        Assert.Equal("", ProgramReader.Load(saved, read, new Values()));
        Assert.Equal(system.ListedOn, read.Systems[0].ListedOn);
    }

    /// <summary>A blueprint leaves the block out, since it means nothing on another construct.</summary>
    [Fact]
    public void ABlueprintLeavesItOut()
    {
        var rig = Kept();

        Assert.DoesNotContain("listedOn", ProgramWriter.Blueprint(rig.Editor.Program.Systems[0]));
    }

    /// <summary>Pressed on one block, keeping takes the system off the list every other block opens.</summary>
    [Fact]
    public void KeptByTheFootItLeavesOtherBlocks()
    {
        var made = Rig.Demo();
        made.Programmable(Office, "Back Office PB");
        made.Programmable(Bay, "Bay PB");
        var screen = Screen.On(made);
        screen.Editor.DeveloperMode = true;
        screen.Editor.OpenedFrom = Office;
        screen.Editor.NewSystem("Airlock");
        screen.Run(2);
        var system = screen.Editor.Program.Systems[0];

        screen.Do("press", "LIST ONLY ON THIS BLOCK").Run(120);
        Assert.True(screen.Editor.Lists(system));

        screen.Do("press", "Done").Run(120);
        var kept = screen.Editor.Program.Systems[0];
        Assert.Equal(Office, KeptId(screen.Editor, kept));
        Assert.Same(system, kept);

        screen.Scene.Reopen();
        screen.Run(120);
        screen.Editor.OpenedFrom = Bay;
        screen.Editor.Invalidate();
        screen.Run(2);
        var listed = screen.Editor.Program.Systems[0];
        Assert.False(screen.Editor.Lists(listed), $"kept to '{listed.ListedOn}', ended on {screen.Ended}");
        Assert.False(screen.Shows("Airlock"), screen.Dump);
    }

    /// <summary>The foot of a developer's logic system, keeps it to the block the editor was opened from and lets it go.</summary>
    [Fact]
    public void TheFootKeepsAnyLogicSystemToItsBlock()
    {
        var made = Rig.Demo();
        made.Programmable(Office, "Back Office PB");
        var screen = Screen.On(made);
        screen.Editor.DeveloperMode = true;
        screen.Editor.OpenedFrom = Office;
        screen.Editor.NewSystem("Airlock");
        screen.Run(2);
        var system = screen.Editor.Program.Systems[0];

        screen.Do("press", "LIST ONLY ON THIS BLOCK").Run(2);
        Assert.Equal(Office, KeptId(screen.Editor, system));

        screen.Do("press", "LIST ON EVERY BLOCK").Run(2);
        Assert.Equal("", system.ListedOn);
        Assert.True(screen.Shows("LIST ONLY ON THIS BLOCK"));
    }

    /// <summary>The foot offers keeping to a developer, and it applies to a logic system that is switched on.</summary>
    [Fact]
    public void TheFootOffersKeepingOnOrOff()
    {
        var made = Rig.Demo();
        made.Programmable(Office, "Back Office PB");
        var screen = Screen.On(made);
        screen.Editor.OpenedFrom = Office;
        screen.Editor.NewSystem("Airlock");
        screen.Run(2);
        Assert.False(screen.Shows("LIST ONLY ON THIS BLOCK"));

        screen.Editor.DeveloperMode = true;
        screen.Run(2);
        Assert.True(screen.Shows("LIST ONLY ON THIS BLOCK"));

        var system = screen.Editor.Program.Systems[0];
        system.Enabled = true;
        screen.Editor.Invalidate();
        screen.Run(2);
        screen.Do("press", "LIST ONLY ON THIS BLOCK").Run(2);

        Assert.True(system.Enabled);
        Assert.Equal(Office, KeptId(screen.Editor, system));
    }

    /// <summary>The home screen, open and idle on a construct with a kept system, gathers its list no more.</summary>
    [Fact]
    public void AnIdleHomeScreenListsOnce()
    {
        var screen = HomeOn(Bay, out _, out _);
        var home = (HomePage)screen.Editor.Current;
        var before = home.Listings;

        screen.Run(600);

        Assert.Same(home, screen.Editor.Current);
        Assert.Equal(before, home.Listings);
    }

    /// <summary>The home screen lists a kept system again as soon as the block it is kept to is ground off.</summary>
    [Fact]
    public void TheHomeScreenSeesTheBlockGo()
    {
        var screen = HomeOn(Bay, out var made, out var office);
        Assert.False(screen.Shows("Airlock"), screen.Dump);

        made.Grind(office);
        screen.Run(2);

        Assert.True(screen.Shows("Airlock"), screen.Dump);
    }

    /// <summary>The home screen, opened from <paramref name="from" />, on a construct with a logic system kept to the office block.</summary>
    static Screen HomeOn(long from, out Rig made, out Programmable office)
    {
        made = Rig.Demo();
        office = made.Programmable(Office, "Back Office PB");
        made.Programmable(Bay, "Bay PB");
        var screen = Screen.On(made);
        screen.Editor.OpenedFrom = from;
        screen.Editor.NewSystem("Airlock");
        screen.Run(2);

        var program = screen.Editor.Program;
        program.Systems[0].ListedOn = Declared(screen.Editor, Office);
        program.Edited();
        screen.Editor.GoToRoot();
        screen.Run(10);
        return screen;
    }

    /// <summary>The variable naming block <paramref name="id" /> in <paramref name="editor" />'s program, declared as keeping a system to it declares it.</summary>
    static string Declared(SprocketEditor editor, long id) => editor.Program.Declare(editor.World, editor.World.FindEntity(id)).Name;

    /// <summary>The id of the block <paramref name="system" /> is kept to, or 0 where it is kept to none found.</summary>
    static long KeptId(SprocketEditor editor, AutomationSystem system) => SystemSummary.KeptTo(editor.Runner, system);

    /// <summary>A rig with two programmable blocks and one logic system, kept to the first.</summary>
    static Simulation Kept()
    {
        var made = Rig.Demo();
        made.Programmable(Office, "Back Office PB");
        made.Programmable(Bay, "Bay PB");
        var rig = Simulation.On(made);
        rig.Editor.NewButtonSystem("Doors");
        rig.Tick();

        var program = rig.Editor.Program;
        var system = program.Systems[0];
        system.ListedOn = Declared(rig.Editor, Office);
        program.Edited();
        rig.Tick();
        return rig;
    }
}
