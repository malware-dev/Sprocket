using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;
using Xunit;
using Cockpit = Sprocket.Host.SpaceEngineers.Blocks.Cockpit;

namespace Sprocket.Tests;

/// <summary>A seat: who sits in it and when, the ship it flies, and its flight switches in Sprocket's words.</summary>
public class CockpitTests
{
    /// <summary>Somebody sitting down is heard on the tick after, since a cockpit announces it, and so is getting up.</summary>
    [Theory]
    [InlineData("becomes-occupied", true)]
    [InlineData("becomes-empty", false)]
    public void SittingDownAndGettingUpAreHeardOnTheTickAfter(string signal, bool sits)
    {
        var ship = new Rigged();
        var automation = new Automation();
        var seat = ship.Rig.Cockpit(1, "Cockpit");
        var lamp = ship.Light(2, "Lamp");
        if (!sits)
            seat.Sit("Mal");

        var heard = SignalTrigger.Written("ext", signal);
        heard.Sources.Add(automation.Declare(ship.World, ship.Entity(seat.Id)).Name);
        var sequence = automation.AddSequence(heard);
        var act = Given.Power(false);
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(lamp.Id)).Name);
        sequence.Steps.Add(act);

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        for (; frame < 23; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        if (sits)
            seat.Sit("Mal");
        else
            seat.Rise();

        var told = frame;
        while (frame < 300 && runner.Get(sequence)!.FiredCount == 0)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.Equal(told + 1, frame);
    }

    /// <summary>Who sits in the seat is read by name, and nobody reads as empty.</summary>
    [Fact]
    public void TheOccupantIsReadByName()
    {
        var ship = new Rigged();
        var seat = ship.Rig.Cockpit(1, "Cockpit");

        Assert.Equal(Literal.Of(false), Ran.Read(ship, seat, Cockpit.Occupied));
        Assert.Equal("", Ran.Read(ship, seat, "occupant-name").Text);

        seat.Sit("Mal");
        Assert.Equal(Literal.Of(true), Ran.Read(ship, seat, Cockpit.Occupied));
        Assert.Equal("Mal", Ran.Read(ship, seat, "occupant-name").Text);
    }

    /// <summary>An enclosed cockpit's air is read in percent, and an open seat, which has none, offers none.</summary>
    [Fact]
    public void OnlyAnEnclosedCockpitOffersItsAir()
    {
        var ship = new Rigged();
        var enclosed = ship.Rig.Cockpit(1, "Cockpit");
        var open = ship.Rig.Cockpit(2, "Seat", false);
        enclosed.Air = 0.25f;

        Assert.Equal(25.0, Ran.Read(ship, enclosed, "oxygen-level").Real, 3);
        Assert.Null(ship.Entity(open.Id).Kind.FindAction("oxygen-level", Form.Value));
    }

    /// <summary>The ship's speed is in metres a second, its gravity in g, and its mass in kilograms.</summary>
    [Fact]
    public void TheShipIsReadThroughTheSeat()
    {
        var ship = new Rigged();
        var seat = ship.Rig.Cockpit(1, "Cockpit");
        seat.Speed = 12.5;
        seat.Gravity = new Vector3D(0, -9.81 / 2, 0);
        seat.Mass = 40000f;

        Assert.Equal(12.5, Ran.Read(ship, seat, "speed").Real, 3);
        Assert.Equal(0.5, Ran.Read(ship, seat, "gravity").Real, 3);
        Assert.Equal(40000.0, Ran.Read(ship, seat, "ship-mass").Real, 3);
    }

    /// <summary>
    /// A cockpit vouches for who sits in it, the ship it reads and each switch with its toggle, and not for the pairs that
    /// set a switch or for taking its controls. It offers nothing that puts its occupant out, which no player can do.
    /// </summary>
    [Fact]
    public void ACockpitVouchesForItsSeatAndSwitchesAndNotTheirPairs()
    {
        var ship = new Rigged();
        var seat = ship.Rig.Cockpit(1, "Cockpit");
        string[] switches =
        [
            "HandBrake", "Park", "DampenersOverride", "ControlThrusters", "ControlWheels", "ControlGyros", "MainCockpit",
            "TargetLocking", "HorizonIndicator"
        ];
        foreach (var id in switches)
        {
            seat.Carrying(id, id, ValueKind.Flag)
                .Offering(id, id)
                .Offering(id + "_On", id + " On")
                .Offering(id + "_Off", id + " Off");
        }

        seat.Offering("Control", "Control");
        var kind = ship.Entity(seat.Id).Kind;

        foreach (var id in new[] { "occupied", "occupant-name", "under-control", "oxygen-level", "gravity", "ship-mass", "speed" })
            Assert.True(kind.FindAction(id, Form.Value)!.Vetted, id + " is not vetted");

        foreach (var id in switches)
        {
            Assert.True(kind.FindAction(id, Form.Value)!.Vetted, id + " is not vetted");
            Assert.True(kind.FindAction(id, Form.Action)!.Vetted, "toggle " + id + " is not vetted");
            Assert.False(kind.FindAction(id + "_On", Form.Action)!.Vetted, id + "_On is vetted");
            Assert.False(kind.FindAction(id + "_Off", Form.Action)!.Vetted, id + "_Off is vetted");
        }

        Assert.False(kind.FindAction("Control", Form.Action)!.Vetted);
        Assert.Null(kind.FindAction("eject-occupant", Form.Native));
    }

    /// <summary>
    /// A seat that cannot fly the ship has none of the settings for flying it, in either view, as the game's terminal shows
    /// none of them on it.
    /// </summary>
    [Fact]
    public void ASeatThatCannotFlyTheShipHasNoFlightSettings()
    {
        var ship = new Rigged();
        var toilet = ship.Rig.Toilet(1, "Toilet");
        string[] switches =
        [
            "HandBrake", "Park", "DampenersOverride", "ControlThrusters", "ControlWheels", "ControlGyros", "MainCockpit",
            "HorizonIndicator"
        ];
        foreach (var id in switches)
        {
            toilet.Carrying(id, id, ValueKind.Flag)
                .Offering(id, id)
                .Offering(id + "_On", id + " On")
                .Offering(id + "_Off", id + " Off");
        }

        var kind = ship.Entity(toilet.Id).Kind;

        foreach (var id in switches)
        {
            Assert.Null(kind.FindAction(id, Form.Value));
            Assert.Null(kind.FindAction(id, Form.Action));
            Assert.Null(kind.FindAction(id + "_On", Form.Action));
            Assert.Null(kind.FindAction(id + "_Off", Form.Action));
        }

        Assert.True(kind.FindAction("occupied", Form.Value)!.Vetted);
    }

    /// <summary>The seat's own switches are said as states, each with its toggle beside it.</summary>
    [Theory]
    [InlineData("HandBrake", "handbrake")]
    [InlineData("Park", "parked")]
    [InlineData("DampenersOverride", "dampeners")]
    [InlineData("ControlThrusters", "controls thrusters")]
    [InlineData("ControlGyros", "controls gyroscopes")]
    [InlineData("MainCockpit", "main cockpit")]
    [InlineData("TargetLocking", "target locking allowed")]
    [InlineData("HorizonIndicator", "shows horizon")]
    public void ItsSwitchesAreSaidAsStates(string id, string words)
    {
        var ship = new Rigged();
        var seat = ship.Rig.Cockpit(1, "Cockpit");
        seat.Carrying(id, id, ValueKind.Flag).Offering(id, id);
        var kind = ship.Entity(seat.Id).Kind;

        Assert.Equal(words, kind.FindAction(id, Form.Value)!.Text);
        Assert.Equal("toggle " + words, kind.FindAction(id, Form.Action)!.Text);
    }
}
