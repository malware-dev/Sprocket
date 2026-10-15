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

        Assert.Equal("radius", kind.FindAction("Radius", Nature.Value)!.Text);
        Assert.Equal("blink interval", kind.FindAction("Blink Interval", Nature.Value)!.Text);
        Assert.Equal("blink length", kind.FindAction("Blink Lenght", Nature.Value)!.Text);
        Assert.Equal("color", kind.FindAction("Color", Nature.Value)!.Text);
        Assert.False(kind.FindAction("Falloff", Nature.Value)!.Hidden);
    }

    /// <summary>A spotlight carries a falloff and a spin it does nothing with, so neither is offered, nor the steps made of them.</summary>
    [Fact]
    public void ASpotlightOffersNeitherFalloffNorSpin()
    {
        var ship = new Rigged();
        var spot = ship.Rig.Spotlight(1, "Spot");
        spot.Carrying("Falloff", "Falloff", ValueKind.Real)
            .Carrying("RotationSpeed", "Rotation speed", ValueKind.Real)
            .Offering("IncreaseFalloff", "Increase Falloff")
            .Offering("IncreaseRotationSpeed", "Increase Rotation speed");
        var kind = ship.Entity(spot.Id).Kind;

        Assert.True(kind.FindAction("Falloff", Nature.Value)!.Hidden);
        Assert.True(kind.FindAction("IncreaseFalloff", Nature.Action)!.Hidden);
        Assert.True(kind.FindAction("RotationSpeed", Nature.Value)!.Hidden);
        Assert.True(kind.FindAction("IncreaseRotationSpeed", Nature.Action)!.Hidden);
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
            Assert.True(kind.FindAction(id, Nature.Value)!.Vetted, id + " is not vetted");

        foreach (var id in steps)
            Assert.False(kind.FindAction(id, Nature.Action)!.Vetted, id + " is vetted");
    }

    /// <summary>
    /// A spotlight vouches for neither the falloff nor the spin it does nothing with; a rotating warning light vouches for its
    /// spin.
    /// </summary>
    [Theory]
    [InlineData("LargeBlockFrontLight", false)]
    [InlineData("RotatingLightLarge", true)]
    public void ASpotlightVouchesOnlyForWhatItUses(string subtype, bool turns)
    {
        var ship = new Rigged();
        var spot = ship.Rig.Spotlight(1, "Spot", subtype);
        spot.Carrying("Radius", "Radius", ValueKind.Real)
            .Carrying("Falloff", "Falloff", ValueKind.Real)
            .Carrying("RotationSpeed", "Rotation speed", ValueKind.Real)
            .Offering("IncreaseRotationSpeed", "Increase Rotation speed");
        var kind = ship.Entity(spot.Id).Kind;

        Assert.True(kind.FindAction("Radius", Nature.Value)!.Vetted);
        Assert.False(kind.FindAction("Falloff", Nature.Value)!.Vetted);
        Assert.Equal(turns, kind.FindAction("RotationSpeed", Nature.Value)!.Vetted);
        Assert.False(kind.FindAction("IncreaseRotationSpeed", Nature.Action)!.Vetted);
    }

    /// <summary>A rotating warning light offers its spin speed, and still no falloff.</summary>
    [Fact]
    public void ARotatingLightOffersItsSpin()
    {
        var ship = new Rigged();
        var spot = ship.Rig.Spotlight(1, "Beacon", "RotatingLightLarge");
        spot.Carrying("Falloff", "Falloff", ValueKind.Real)
            .Carrying("RotationSpeed", "Rotation speed", ValueKind.Real);
        var kind = ship.Entity(spot.Id).Kind;

        var spin = kind.FindAction("RotationSpeed", Nature.Value)!;
        Assert.False(spin.Hidden);
        Assert.Equal("rotation speed", spin.Text);
        Assert.True(kind.FindAction("Falloff", Nature.Value)!.Hidden);
    }
}
