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
    /// A switch set once when the machine is built keeps its value and loses the act that flips it.
    /// </summary>
    [Fact]
    public void ASwitchSetWhenTheMachineIsBuiltIsNotFlipped()
    {
        var ship = new Rigged();
        var lamp = ship.Light(1, "Lamp");
        lamp.Carrying("UseConveyor", "Use Conveyor System", ValueKind.Flag).Offering("UseConveyor", "Use Conveyor System On/Off");
        var kind = ship.Entity(lamp.Id).Kind;

        Assert.True(kind.FindAction("UseConveyor", Form.Action)!.Hidden);
        Assert.False(kind.FindAction("UseConveyor", Form.Value)!.Hidden);
    }

    /// <summary>
    /// The mark on the HUD is a value read after "is" and its toggle, each in Sprocket's words and vetted; the game's pair
    /// that sets the value each way says what the value says, so it stays in the game's words, unvetted.
    /// </summary>
    [Fact]
    public void TheHudMarkIsAValueAndItsToggle()
    {
        var ship = new Rigged();
        var lamp = ship.Light(1, "Lamp");
        lamp.Carrying("ShowOnHUD", "Show on HUD", ValueKind.Flag)
            .Offering("ShowOnHUD", "Show on HUD On/Off")
            .Offering("ShowOnHUD_On", "Show on HUD On")
            .Offering("ShowOnHUD_Off", "Show on HUD Off");
        var kind = ship.Entity(lamp.Id).Kind;

        var value = kind.FindAction("ShowOnHUD", Form.Value)!;
        Assert.False(value.Hidden);
        Assert.Equal("Shown on HUD", value.Named);
        Assert.True(value.Vetted);

        var toggle = kind.FindAction("ShowOnHUD", Form.Action)!;
        Assert.True(toggle.Hidden);
        Assert.Equal("Toggle show on HUD", toggle.Named);
        Assert.True(toggle.Vetted);
        Assert.True(value.Flips);

        foreach (var id in new[] { "ShowOnHUD_On", "ShowOnHUD_Off" })
        {
            var each = kind.FindAction(id, Form.Action)!;
            Assert.False(each.Renamed, id + " is renamed");
            Assert.False(each.Vetted, id + " is vetted");
        }
    }

    /// <summary>
    /// What Sprocket vouches for is its own members and the block's members a capability renamed or vetted; anything else
    /// the block declares is not, until something vouches for it.
    /// </summary>
    [Fact]
    public void OnlyWhatSprocketVouchesForIsVetted()
    {
        var ship = new Rigged();
        var lamp = ship.Light(1, "Lamp");
        lamp.Carrying("Radius", "Radius", ValueKind.Real).Offering("Strobe", "Strobe", 0);

        var kind = ship.Entity(lamp.Id).Kind;
        Assert.True(kind.FindAction(BlockKinds.SwitchOn, Form.Native)!.Vetted);

        var renamed = kind.FindAction("Radius", Form.Value)!;
        Assert.True(renamed.Renamed);
        Assert.True(renamed.Vetted);

        var strobe = kind.FindAction("Strobe", Form.Action)!;
        Assert.False(strobe.Vetted);
        strobe.Vouch();
        Assert.True(strobe.Vetted);
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
