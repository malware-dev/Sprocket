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

        Assert.Equal("radius", kind.FindAction("Radius", Form.Value)!.Text);
        Assert.Equal("blink interval", kind.FindAction("Blink Interval", Form.Value)!.Text);
        Assert.Equal("blink length", kind.FindAction("Blink Lenght", Form.Value)!.Text);
        Assert.Equal("color", kind.FindAction("Color", Form.Value)!.Text);
        Assert.False(kind.FindAction("Falloff", Form.Value)!.Hidden);
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

        Assert.True(kind.FindAction("Falloff", Form.Value)!.Hidden);
        Assert.True(kind.FindAction("IncreaseFalloff", Form.Action)!.Hidden);
        Assert.True(kind.FindAction("RotationSpeed", Form.Value)!.Hidden);
        Assert.True(kind.FindAction("IncreaseRotationSpeed", Form.Action)!.Hidden);
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

        var spin = kind.FindAction("RotationSpeed", Form.Value)!;
        Assert.False(spin.Hidden);
        Assert.Equal("spin speed", spin.Text);
        Assert.True(kind.FindAction("Falloff", Form.Value)!.Hidden);
    }
}
