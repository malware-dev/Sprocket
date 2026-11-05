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
        system.ListedOn = "";

        rig.Editor.OpenedFrom = Bay;

        Assert.True(rig.Editor.Lists(system));
    }

    /// <summary>A system kept to a block that isn't found lists everywhere, so nothing is out of reach.</summary>
    [Fact]
    public void ALostBlockLetsTheSystemListEverywhere()
    {
        var rig = Kept();
        var system = rig.Editor.Program.Systems[0];
        system.ListedOn = "gone";

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
        system.ListedOn = program.Declare(rig.World, rig.World.FindEntity(Office)).Name;
        program.Edited();
        rig.Tick();
        return rig;
    }
}
