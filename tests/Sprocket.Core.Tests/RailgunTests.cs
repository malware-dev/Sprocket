using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A railgun's readings off its capacitor: its charge, the time until it is charged, and whether it is charging or charged.</summary>
public class RailgunTests
{
    /// <summary>A railgun on a grid with no power has endless time to go, which is not charging.</summary>
    [Fact]
    public void ARailgunWithNoPowerIsNotCharging()
    {
        var ship = new Rigged();
        var railgun = ship.Rig.Railgun(1, "Railgun");
        railgun.Capacitor.Charging(0.1f, 30f);
        railgun.Capacitor.Unpowered();

        Assert.True(double.IsPositiveInfinity(Read(ship, railgun, "time_until_charged").Number));
        Assert.False(Read(ship, railgun, "charging").Flag);
        Assert.False(Read(ship, railgun, "charged").Flag);
        Assert.Equal(20.0, Read(ship, railgun, "charge").Number, 3);
    }

    /// <summary>A railgun switched off has no time to go and is not charged, which is not charging.</summary>
    [Fact]
    public void ASwitchedOffRailgunIsNotCharging()
    {
        var ship = new Rigged();
        var railgun = ship.Rig.Railgun(1, "Railgun");
        railgun.Capacitor.Charging(0.1f, 30f);
        railgun.Capacitor.SwitchedOff();

        Assert.Equal(0.0, Read(ship, railgun, "time_until_charged").Number, 3);
        Assert.False(Read(ship, railgun, "charging").Flag);
        Assert.False(Read(ship, railgun, "charged").Flag);
    }

    /// <summary>A railgun charging on power reads charging, with the seconds its capacitor says are left.</summary>
    [Fact]
    public void ARailgunOnPowerIsCharging()
    {
        var ship = new Rigged();
        var railgun = ship.Rig.Railgun(1, "Railgun");
        railgun.Capacitor.Charging(0.25f, 30f);

        Assert.Equal(30.0, Read(ship, railgun, "time_until_charged").Number, 3);
        Assert.True(Read(ship, railgun, "charging").Flag);
        Assert.False(Read(ship, railgun, "charged").Flag);
    }

    /// <summary>A full railgun reads charged, and neither charging nor any time to go.</summary>
    [Fact]
    public void AFullRailgunIsCharged()
    {
        var ship = new Rigged();
        var railgun = ship.Rig.Railgun(1, "Railgun");
        railgun.Capacitor.Full();

        Assert.Equal(0.0, Read(ship, railgun, "time_until_charged").Number, 3);
        Assert.False(Read(ship, railgun, "charging").Flag);
        Assert.True(Read(ship, railgun, "charged").Flag);
        Assert.Equal(100.0, Read(ship, railgun, "charge").Number, 3);
    }

    /// <summary>Endless time to go is above any number a step can say, and below none.</summary>
    [Fact]
    public void EndlessTimeToGoIsAboveAnyNumber()
    {
        var endless = Literal.Of(double.PositiveInfinity);

        Assert.True(Relations.Holds(endless, Relation.Above, Literal.Of(1e9)));
        Assert.False(Relations.Holds(endless, Relation.Below, Literal.Of(1e9)));
    }

    static Literal Read(Rigged ship, Fitting fitting, string id)
    {
        var value = ship.Entity(fitting.Id).Kind.FindAction(id, ReachKind.Value)!;
        return value.OwnValue!.Gauge(fitting.Block, new Literal[0], new ReaderMemory()).Read();
    }
}
