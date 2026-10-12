using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A construct's automation belongs to its big owners, counted over every grid of it, and reaches only the blocks the
/// game's own access test lets one of them use.
/// </summary>
[Collection("Serially")]
public class OwnershipTests
{
    const long Anna = 144115188075855873;
    const long Bert = 144115188075855874;

    /// <summary>The player owning most of the construct's blocks is its big owner; nobody where nobody owns any.</summary>
    [Fact]
    public void TheBigOwnerOwnsMostBlocks()
    {
        var ship = Airlock();
        Assert.Empty(ship.World.BigOwners);

        ship.Rig.Own(Anna);
        ship.Rig.Fitted.First(fitting => fitting.Name == "Inner Door").Owner = Bert;

        Assert.Equal(new[] { Anna }, ship.World.BigOwners);
    }

    /// <summary>Players owning as many blocks as each other are big owners together.</summary>
    [Fact]
    public void ATieMakesSeveralBigOwners()
    {
        var ship = Airlock();
        ship.Rig.Own(Anna);
        ship.Rig.Fitted.First(fitting => fitting.Name == "Inner Door").Owner = Bert;
        ship.Rig.Fitted.First(fitting => fitting.Name == "Outer Door").Owner = Bert;

        Assert.Equal(new[] { Anna, Bert }, ship.World.BigOwners);
    }

    /// <summary>A block the big owners may not use reads as gone, and comes back by itself when it is shared with them.</summary>
    [Fact]
    public void ABlockNotSharedIsGoneUntilItIs()
    {
        var ship = Airlock();
        ship.Rig.Own(Anna);
        var door = ship.Rig.Fitted.First(fitting => fitting.Name == "Inner Door");
        door.Owner = Bert;

        Assert.DoesNotContain(ship.World.Entities, entity => entity.Name == "Inner Door");

        door.Share = Sharing.All;
        ship.World.Reshare();

        Assert.Contains(ship.World.Entities, entity => entity.Name == "Inner Door");
    }

    /// <summary>A block shared with its owner's faction is the big owners' to use where one of them is in it, and stops being so when they leave.</summary>
    [Fact]
    public void AFactionShareFollowsTheFaction()
    {
        var ship = Airlock();
        ship.Rig.Own(Anna);
        var door = ship.Rig.Fitted.First(fitting => fitting.Name == "Inner Door");
        door.Share = Sharing.Faction;
        door.Owner = Bert;
        ship.Rig.Factions.Join(Anna, "MAL");
        ship.Rig.Factions.Join(Bert, "MAL");
        ship.World.Reshare();

        Assert.Contains(ship.World.Entities, entity => entity.Name == "Inner Door");

        ship.Rig.Factions.Leave(Bert);
        ship.World.Reshare();

        Assert.DoesNotContain(ship.World.Entities, entity => entity.Name == "Inner Door");
    }

    /// <summary>A block changing hands is heard from the grid, and the construct is read again for it.</summary>
    [Fact]
    public void AChangeOfHandsIsHeard()
    {
        var ship = Airlock();
        ship.Rig.Own(Anna);
        _ = ship.World.Entities;
        var heard = 0;
        ship.World.HandsChanged += () => heard++;

        ship.Rig.Fitted.First(fitting => fitting.Name == "Outer Door").Owner = Bert;

        Assert.Equal(1, heard);
        Assert.DoesNotContain(ship.World.Entities, entity => entity.Name == "Outer Door");
    }

    static Rigged Airlock()
    {
        var ship = new Rigged();
        ship.Door(2, "Inner Door");
        ship.Door(3, "Outer Door");
        ship.Vent(4, "Vent");
        return ship;
    }
}
