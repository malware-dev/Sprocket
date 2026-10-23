using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using VRage.Game.ModAPI;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A construct is called what its largest grid is called, the grid holding the most blocks, as the game's own list of
/// entities names a group of grids; renaming the construct renames that grid, and the name follows the grids as they
/// split, merge and lose blocks.
/// </summary>
[Collection("Serially")]
public class ConstructNameTests
{
    /// <summary>A construct of one grid is called what that grid is.</summary>
    [Fact]
    public void OneGridNamesTheConstruct()
    {
        var ship = new Rigged();

        Assert.Equal("Test Rig", ship.World.Name);
    }

    /// <summary>A rotor's head holding more blocks than the grid the programmable block is on names the construct.</summary>
    [Fact]
    public void TheLargestGridNamesTheConstruct()
    {
        var ship = Armed(3);

        Assert.Equal("Arm Rotor Head", ship.World.Name);
    }

    /// <summary>Where two grids hold as many blocks, the first the construct's group gives names it, as the game's own list of entities does.</summary>
    [Fact]
    public void OfTwoAlikeTheFirstInTheGroupNamesTheConstruct()
    {
        // The rig's group gives the base first.
        // The base holds the programmable block and the rotor; the head its top and one door.
        var ship = Armed(1);

        Assert.Equal("Test Rig", ship.World.Name);
    }

    /// <summary>Of two grids holding as many blocks, the one given first is the largest, whichever entity id is lower.</summary>
    [Fact]
    public void OfTwoAlikeTheOneGivenFirstIsTheLargest()
    {
        var ship = Armed(1, out var rotor);
        var baseGrid = ship.Rig.Sprocket.Block.CubeGrid;
        var head = rotor.TopGrid;
        var sizes = new Dictionary<long, int> { { baseGrid.EntityId, 2 }, { head.EntityId, 2 } };

        Assert.Same(head, Constructs.Largest(new List<IMyCubeGrid> { head, baseGrid }, sizes));
        Assert.Same(baseGrid, Constructs.Largest(new List<IMyCubeGrid> { baseGrid, head }, sizes));
    }

    /// <summary>Renaming the construct renames its largest grid and leaves the others as they were.</summary>
    [Fact]
    public void RenamingRenamesTheLargestGrid()
    {
        var ship = Armed(3, out var rotor);

        ship.World.Rename("Crane");

        Assert.Equal("Crane", rotor.TopGrid.CustomName);
        Assert.Equal("Crane", ship.World.Name);
        Assert.Equal("Test Rig", ship.Rig.Sprocket.Block.CubeGrid.CustomName);
    }

    /// <summary>The blocks that made the head the largest splitting off leaves the base the largest, and the name follows.</summary>
    [Fact]
    public void TheNameFollowsASplit()
    {
        var ship = Armed(3);
        Assert.Equal("Arm Rotor Head", ship.World.Name);

        ship.Rig.Split(ship.Rig.Fitted.Where(fitting => fitting.Id >= 10 && fitting.Id < 20));

        Assert.Equal("Test Rig", ship.World.Name);
    }

    /// <summary>A grid merged into the base makes it the largest again, and the name follows.</summary>
    [Fact]
    public void TheNameFollowsAMerge()
    {
        var ship = Armed(2);
        var tug = ship.Rig.Apart("Tug");
        ship.Rig.Door(20, "Tug Door");
        ship.Rig.Door(21, "Tug Hatch");
        ship.Rig.OnBase();
        Assert.Equal("Arm Rotor Head", ship.World.Name);

        ship.Rig.Merge(tug);

        Assert.Equal("Test Rig", ship.World.Name);
    }

    /// <summary>Armor ground off the head until it holds no more than the base takes the name back to the base, without the construct being read again.</summary>
    [Fact]
    public void TheNameFollowsAGrind()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(2, "Arm Rotor");
        ship.Rig.OnHeadOf(rotor);
        var first = ship.Rig.Armor();
        var second = ship.Rig.Armor();
        ship.Rig.Armor();
        ship.Rig.OnBase();
        Assert.Equal("Arm Rotor Head", ship.World.Name);
        var revision = ship.World.Revision;

        Pretend.Heard = new List<string>();
        try
        {
            ship.Rig.Strip(first);
            Assert.Equal("Arm Rotor Head", ship.World.Name);

            ship.Rig.Strip(second);
            Assert.Equal("Test Rig", ship.World.Name);

            Assert.DoesNotContain(Pretend.Heard, asked => asked.EndsWith("GetBlocks", StringComparison.Ordinal));
        }
        finally
        {
            Pretend.Heard = null!;
        }

        Assert.Equal(revision, ship.World.Revision);
    }

    /// <summary>A block welded on that makes the head the largest moves the name to the head.</summary>
    [Fact]
    public void TheNameFollowsAWeld()
    {
        var ship = Armed(1, out var rotor);
        Assert.Equal("Test Rig", ship.World.Name);

        ship.Rig.OnHeadOf(rotor);
        ship.Rig.Armor();
        ship.Rig.OnBase();

        Assert.Equal("Arm Rotor Head", ship.World.Name);
    }

    /// <summary>The status panel's name for a construct with a Sprocket on it is the one the Sprocket's own construct gives.</summary>
    [Fact]
    public void TheStatusPanelNamesTheLargestGrid()
    {
        var rig = new Rig();
        var rotor = rig.Rotor(2, "Arm Rotor");
        rig.OnHeadOf(rotor);
        rig.Door(10, "Arm Door 1");
        rig.Door(11, "Arm Door 2");
        rig.OnBase();
        var sim = Simulation.On(rig);
        var constructs = new Constructs(sim.Roster);

        Assert.Equal("Arm Rotor Head", constructs.Name(rig.Sprocket.Block.CubeGrid));
        Assert.Equal("Arm Rotor Head", constructs.Name(rotor.TopGrid));
    }

    /// <summary>A construct with no automation has no name, and nothing of its grids is read for one.</summary>
    [Fact]
    public void AConstructWithNoAutomationHasNoName()
    {
        var ship = Armed(3);
        var constructs = new Constructs(Simulation.On(ship.Rig).Roster);
        var tug = ship.Rig.Apart("Tug");
        ship.Rig.Door(20, "Tug Door");

        Pretend.Heard = new List<string>();
        try
        {
            Assert.Equal("", constructs.Name(tug.Block));
            Assert.Equal("", Constructs.Name(new List<SprocketUnit>()));
            Assert.DoesNotContain(Pretend.Heard, asked => asked.EndsWith("GetBlocks", StringComparison.Ordinal)
                || asked.EndsWith("CustomName", StringComparison.Ordinal));
        }
        finally
        {
            Pretend.Heard = null!;
        }
    }

    /// <summary>A construct of the base and a rotor's head with <paramref name="doors" /> doors on the head, read once.</summary>
    static Rigged Armed(int doors)
    {
        return Armed(doors, out _);
    }

    /// <summary>A construct of the base and a rotor's head with <paramref name="doors" /> doors on the head, read once, and the rotor in <paramref name="rotor" />.</summary>
    static Rigged Armed(int doors, out Rotor rotor)
    {
        var ship = new Rigged();
        rotor = ship.Rotor(2, "Arm Rotor");
        ship.Rig.OnHeadOf(rotor);
        for (var i = 0; i < doors; i++)
            ship.Door(10 + i, "Arm Door " + (i + 1));

        ship.Rig.OnBase();
        _ = ship.World.Entities;
        return ship;
    }
}
