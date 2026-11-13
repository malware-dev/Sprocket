using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A light's values in Sprocket's words, and the ones a kind of light carries without using.</summary>
public class LightTests
{
    /// <summary>An interior light's values are named as a sentence says them, the game's misspelled id kept for the program.</summary>
    [Fact]
    public void AnInteriorLightsValuesAreSaidInSprocketsWords()
    {
        var ship = new Rigged();
        var lamp = ship.Light(1, "Lamp");
        lamp.Carrying("Radius", "Radius", ValueKind.Real)
            .Carrying("Falloff", "Falloff", ValueKind.Real)
            .Carrying("Blink Interval", "Blink Interval", ValueKind.Real)
            .Carrying("Blink Lenght", "Blink Lenght", ValueKind.Real)
            .Carrying("Color", "Color", ValueKind.Color);
        var kind = ship.Entity(lamp.Id).Kind;

        Assert.Equal("radius", kind.FindAction("Radius", ReachKind.Value)!.Text);
        Assert.Equal("blink interval", kind.FindAction("Blink Interval", ReachKind.Value)!.Text);
        Assert.Equal("blink length", kind.FindAction("Blink Lenght", ReachKind.Value)!.Text);
        Assert.Equal("color", kind.FindAction("Color", ReachKind.Value)!.Text);
        Assert.False(kind.FindAction("Falloff", ReachKind.Value)!.Hidden);
    }

    /// <summary>
    /// Every value an interior light offers is vouched for, and none of the game's setters and steps that only say one of
    /// them again.
    /// </summary>
    [Fact]
    public void AnInteriorLightVouchesForItsValuesAndNotTheirSteps()
    {
        var ship = new Rigged();
        var lamp = ship.Light(1, "Lamp");
        string[] values = ["Color", "Intensity", "Radius", "Falloff", "Offset", "Blink Interval", "Blink Lenght", "Blink Offset"];
        foreach (var id in values)
            lamp.Carrying(id, id, id == "Color" ? ValueKind.Color : ValueKind.Real);

        string[] steps =
        [
            "SetColor", "SetRadius", "SetIntensity", "SetBlinkInterval", "IncreaseRadius", "DecreaseRadius", "IncreaseFalloff",
            "DecreaseFalloff", "IncreaseBlink Lenght", "DecreaseBlink Lenght"
        ];
        foreach (var id in steps)
            lamp.Offering(id, id);

        var kind = ship.Entity(lamp.Id).Kind;

        foreach (var id in values)
            Assert.True(kind.FindAction(id, ReachKind.Value)!.Vetted, id + " is not vetted");

        foreach (var id in steps)
            Assert.False(kind.FindAction(id, ReachKind.Action)!.Vetted, id + " is vetted");
    }

    /// <summary>
    /// A spotlight vouches for neither the falloff nor the spin it does nothing with, and offers neither, nor the steps made of
    /// them; a rotating warning light vouches for and offers its spin, and still no falloff.
    /// </summary>
    [Theory]
    [InlineData("LargeBlockFrontLight", false)]
    [InlineData("RotatingLightLarge", true)]
    public void ASpotlightVouchesForAndOffersOnlyWhatItUses(string subtype, bool turns)
    {
        var ship = new Rigged();
        var spot = ship.Rig.Spotlight(1, "Spot", subtype);
        spot.Carrying("Radius", "Radius", ValueKind.Real)
            .Carrying("Falloff", "Falloff", ValueKind.Real)
            .Carrying("RotationSpeed", "Rotation speed", ValueKind.Real)
            .Offering("IncreaseFalloff", "Increase Falloff")
            .Offering("IncreaseRotationSpeed", "Increase Rotation speed");
        var kind = ship.Entity(spot.Id).Kind;

        Assert.True(kind.FindAction("Radius", ReachKind.Value)!.Vetted);
        Assert.False(kind.FindAction("Falloff", ReachKind.Value)!.Vetted);
        Assert.Equal(turns, kind.FindAction("RotationSpeed", ReachKind.Value)!.Vetted);
        Assert.False(kind.FindAction("IncreaseRotationSpeed", ReachKind.Action)!.Vetted);

        Assert.True(kind.FindAction("Falloff", ReachKind.Value)!.Hidden);
        Assert.True(kind.FindAction("IncreaseFalloff", ReachKind.Action)!.Hidden);
        Assert.Equal(!turns, kind.FindAction("RotationSpeed", ReachKind.Value)!.Hidden);
        if (turns)
            Assert.Equal("rotation speed", kind.FindAction("RotationSpeed", ReachKind.Value)!.Text);
        else
            Assert.True(kind.FindAction("IncreaseRotationSpeed", ReachKind.Action)!.Hidden);
    }
}
