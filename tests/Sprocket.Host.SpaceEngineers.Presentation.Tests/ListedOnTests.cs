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

    /// <summary>A system kept to a block lists from that block and from no other.</summary>
    [Fact]
    public void AKeptSystemListsOnlyOnItsBlock()
    {
        var rig = Kept();
        var system = rig.Editor.Program.Systems[0];

        rig.Editor.OpenedFrom = Office;
        Assert.True(rig.Editor.Lists(system));

        rig.Editor.OpenedFrom = Bay;
        Assert.False(rig.Editor.Lists(system));
    }

    /// <summary>A system kept to nothing lists everywhere.</summary>
    [Fact]
    public void AnUnkeptSystemListsEverywhere()
    {
        var rig = Kept();
        var system = rig.Editor.Program.Systems[0];
        system.ListedOn = 0;

        rig.Editor.OpenedFrom = Bay;

        Assert.True(rig.Editor.Lists(system));
    }

    /// <summary>A system kept to a block that isn't found lists everywhere, so nothing is out of reach.</summary>
    [Fact]
    public void ALostBlockLetsTheSystemListEverywhere()
    {
        var rig = Kept();
        var system = rig.Editor.Program.Systems[0];
        system.ListedOn = 999;

        rig.Editor.OpenedFrom = Bay;

        Assert.True(rig.Editor.Lists(system));
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
        Assert.Equal(Office, kept.ListedOn);
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
        Assert.Equal(Office, system.ListedOn);

        screen.Do("press", "LIST ON EVERY BLOCK").Run(2);
        Assert.Equal(0L, system.ListedOn);
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
        Assert.Equal(Office, system.ListedOn);
    }

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
        system.ListedOn = Office;
        program.Edited();
        rig.Tick();
        return rig;
    }
}
