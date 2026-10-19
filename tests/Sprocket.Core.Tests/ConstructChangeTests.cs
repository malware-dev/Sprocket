using Sandbox.Common.ObjectBuilders;
using Sprocket.Sim.Fakes;
using Sprocket.Sim.Machine;
using VRage.ObjectBuilders;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A block welded on or ground off is taken in or let go by itself, whatever the size of the construct; the construct is
/// read again whole only where it changes as a whole.
/// </summary>
[Collection("Serially")]
public class ConstructChangeTests
{
    const long Anna = 144115188075855873;
    const long Bert = 144115188075855874;

    /// <summary>Armor laid and ground off is no part of what the construct reads, and changes nothing in it.</summary>
    [Fact]
    public void ArmorChangesNothing()
    {
        var ship = Doors();
        var revision = ship.World.Revision;

        var armor = ship.Rig.Armor();
        Assert.Equal(revision, ship.World.Revision);

        ship.Rig.Strip(armor);
        Assert.Equal(revision, ship.World.Revision);
    }

    /// <summary>A block welded on joins the construct without the construct's blocks being asked for again, and every block already there keeps its entity.</summary>
    [Fact]
    public void AWeldTakesInOneBlock()
    {
        var ship = Doors();
        var inner = ship.World.FindEntity(1);
        var revision = ship.World.Revision;

        Pretend.Heard = new List<string>();
        try
        {
            ship.Door(3, "Hangar Door");
            Assert.NotEqual(revision, ship.World.Revision);
            Assert.DoesNotContain(Pretend.Heard, asked => asked.EndsWith("GetBlocks", StringComparison.Ordinal));
        }
        finally
        {
            Pretend.Heard = null!;
        }

        Assert.NotNull(ship.World.FindEntity(3));
        Assert.Contains(ship.World.Entities, entity => entity.Name == "Hangar Door");
        Assert.Same(inner, ship.World.FindEntity(1));
    }

    /// <summary>A block ground off leaves the construct by itself.</summary>
    [Fact]
    public void AGrindLetsOneBlockGo()
    {
        var ship = Doors();
        var inner = ship.World.FindEntity(1);

        Pretend.Heard = new List<string>();
        try
        {
            ship.Grind(2);
            Assert.Null(ship.World.FindEntity(2));
            Assert.DoesNotContain(Pretend.Heard, asked => asked.EndsWith("GetBlocks", StringComparison.Ordinal));
        }
        finally
        {
            Pretend.Heard = null!;
        }

        Assert.DoesNotContain(ship.World.Entities, entity => entity.Name == "Outer Door");
        Assert.Same(inner, ship.World.FindEntity(1));
    }

    /// <summary>Blocks welded on that make somebody else the big owner have the construct read again, and what the new owner may not use is gone.</summary>
    [Fact]
    public void AWeldThatChangesTheBigOwnersReadsTheConstructAgain()
    {
        var ship = Doors();
        ship.Rig.Own(Anna);
        Assert.Equal(new[] { Anna }, ship.World.BigOwners);

        for (var i = 0; i < 4; i++)
        {
            var light = new Lamp(10 + i, "Light " + i, new MyObjectBuilderType(typeof(MyObjectBuilder_InteriorLight)),
                "LargeBlockLight_1corner", "Interior Light");
            light.Owner = Bert;
            ship.Rig.Weld(light);
        }

        Assert.Equal(new[] { Bert }, ship.World.BigOwners);
        Assert.Null(ship.World.FindEntity(1));
        Assert.Contains(ship.World.Unshared, name => name == "Inner Door");
        Assert.NotNull(ship.World.FindEntity(10));
    }

    /// <summary>A block welded on that the big owners may not use is counted among those, and leaves them when it is ground off.</summary>
    [Fact]
    public void AnUnsharedWeldIsUnsharedUntilGroundOff()
    {
        var ship = Doors();
        ship.Rig.Own(Anna);
        _ = ship.World.Revision;

        var light = new Lamp(10, "Bert's Light", new MyObjectBuilderType(typeof(MyObjectBuilder_InteriorLight)),
            "LargeBlockLight_1corner", "Interior Light");
        light.Owner = Bert;
        ship.Rig.Weld(light);

        Assert.Null(ship.World.FindEntity(10));
        Assert.Contains("Bert's Light", ship.World.Unshared);

        ship.Grind(10);
        Assert.DoesNotContain("Bert's Light", ship.World.Unshared);
    }

    /// <summary>The grid saying a block changed hands, where none did and sharing stands, leaves the construct as it was, and asks no grid for its blocks.</summary>
    [Fact]
    public void HandsChangedWithNothingChangedLeavesTheConstruct()
    {
        var ship = Doors();
        ship.Rig.Own(Anna);
        var inner = ship.World.FindEntity(1);
        var revision = ship.World.Revision;

        Pretend.Heard = new List<string>();
        try
        {
            ship.World.Reshare();
            Assert.Equal(revision, ship.World.Revision);
            Assert.DoesNotContain(Pretend.Heard, asked => asked.EndsWith("GetBlocks", StringComparison.Ordinal));
        }
        finally
        {
            Pretend.Heard = null!;
        }

        Assert.Same(inner, ship.World.FindEntity(1));
    }

    /// <summary>A block handed to somebody the big owners may not use is gone from the construct, and the rest keep their entities.</summary>
    [Fact]
    public void ABlockHandedAwayIsGone()
    {
        var ship = Doors();
        ship.Rig.Own(Anna);
        var inner = ship.World.FindEntity(1);
        var revision = ship.World.Revision;

        ship.Rig.Fitted.First(fitting => fitting.Id == 2).Owner = Bert;

        Assert.NotEqual(revision, ship.World.Revision);
        Assert.Null(ship.World.FindEntity(2));
        Assert.Contains("Outer Door", ship.World.Unshared);
        Assert.Same(inner, ship.World.FindEntity(1));
    }

    /// <summary>A construct read again whole keeps the entity of every block still on it.</summary>
    [Fact]
    public void AReadAgainKeepsTheEntities()
    {
        var ship = Doors();
        var inner = ship.World.FindEntity(1);
        var revision = ship.World.Revision;

        ship.Rig.Split(new[] { ship.Rig.Fitted.First(fitting => fitting.Id == 2) });

        Assert.NotEqual(revision, ship.World.Revision);
        Assert.Same(inner, ship.World.FindEntity(1));
        Assert.Null(ship.World.FindEntity(2));
    }

    /// <summary>A block welded on is heard being renamed, and one ground off is not.</summary>
    [Fact]
    public void AWeldedBlockIsHeardRenamed()
    {
        var ship = Doors();
        var renamed = 0;
        ship.World.Renamed += () => renamed++;

        var hangar = ship.Door(3, "Hangar Door");
        _ = ship.World.Revision;
        hangar.Name = "Bay Door";
        Assert.Equal(1, renamed);

        ship.Grind(3);
        _ = ship.World.Revision;
        hangar.Name = "Old Door";
        Assert.Equal(1, renamed);
    }

    /// <summary>Two doors on a construct read once, so what follows is a change to it.</summary>
    static Rigged Doors()
    {
        var ship = new Rigged();
        ship.Door(1, "Inner Door");
        ship.Door(2, "Outer Door");
        _ = ship.World.Entities;
        return ship;
    }
}
