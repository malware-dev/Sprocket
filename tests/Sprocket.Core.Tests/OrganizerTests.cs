using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// What the organized view leaves to the terminal, for any block, whoever made it.
/// </summary>
/// <remarks>
/// Rules read off the game's own ids rather than lists of blocks, so a modded block that follows
/// the game's conventions is tidied the same way without anybody having heard of it.
/// </remarks>
public class OrganizerTests
{
    /// <summary>
    /// A value is offered once, not again as every action the game made out of it.
    /// </summary>
    /// <remarks>
    /// A switch arrives as its value, its pair and the act that flips it, and a slider as its value
    /// and a step each way. The value says what the pair and the steps say. The act that flips it and
    /// the reset say something the value cannot, so they stay.
    /// </remarks>
    [Fact]
    public void AValueIsNotOfferedAgainAsTheActionsMadeOutOfIt()
    {
        var ship = new Rigged();
        var tank = ship.Light(1, "Tank")
            .Carrying("Stockpile", "Stockpile", ValueKind.Flag)
            .Offering("Stockpile", "Stockpile On/Off")
            .Offering("Stockpile_On", "Stockpile On")
            .Offering("Stockpile_Off", "Stockpile Off")
            .Carrying("Radius", "Radius", ValueKind.Real, "1", "20")
            .Offering("IncreaseRadius", "Increase Radius")
            .Offering("DecreaseRadius", "Decrease Radius")
            .Offering("ResetRadius", "Reset Radius");
        var kind = ship.Entity(tank.Id).Kind;

        foreach (var id in new[] { "Stockpile_On", "Stockpile_Off", "IncreaseRadius", "DecreaseRadius" })
            Assert.True(kind.FindAction(id, Form.Action)!.Hidden, id + " is offered beside its value");

        Assert.False(kind.FindAction("Stockpile", Form.Value)!.Hidden);
        Assert.False(kind.FindAction("Stockpile", Form.Action)!.Hidden);
        Assert.False(kind.FindAction("Radius", Form.Value)!.Hidden);
        Assert.False(kind.FindAction("ResetRadius", Form.Action)!.Hidden);
    }

    /// <summary>
    /// A setter somebody wrote by hand beside a value is left out where the block is known to have
    /// one, and nowhere else.
    /// </summary>
    /// <remarks>
    /// Nothing the game generates makes a setter, so its id says nothing about what it does: a
    /// thruster's looks like one and takes a percentage where its value takes newtons. A beacon's is
    /// known to say what its value says. A door carrying the same two is left as it is.
    /// </remarks>
    [Fact]
    public void AHandWrittenSetterIsLeftOutOnlyWhereItIsKnown()
    {
        var ship = new Rigged();
        var beacon = ship.Beacon(1, "Beacon");
        beacon.Carrying("HudText", "Text", ValueKind.Text).Offering("SetHudText", "Set HUD Text", 1);
        var hatch = ship.Door(2, "Hatch");
        hatch.Carrying("HudText", "Text", ValueKind.Text).Offering("SetHudText", "Set HUD Text", 1);

        Assert.True(ship.Entity(beacon.Id).Kind.FindAction("SetHudText", Form.Action)!.Hidden);
        Assert.False(ship.Entity(hatch.Id).Kind.FindAction("SetHudText", Form.Action)!.Hidden);
    }
}
