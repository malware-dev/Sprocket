using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>What every block offers whatever it is: whether it works, its damage, its Custom Data, and its switch's toggle.</summary>
public class CommonMembersTests
{
    /// <summary>A lamp and a door, with a program on them and a runner ticking it.</summary>
    sealed class Ship
    {
        public readonly Rigged Rig = new();
        public readonly Automation Program = new();
        public readonly Lamp Lamp;
        public readonly Door Door;
        public AutomationRunner Runner = null!;

        public Ship()
        {
            Lamp = Rig.Light(1, "Lamp");
            Door = Rig.Door(2, "Door");
        }

        public int Frame { get; private set; }

        public int Fired => Rig.Rig.Log.Count(line => line == "Door:off");

        public string Name(Fitting fitting) => Program.Declare(Rig.World, Rig.Entity(fitting.Id)).Name;

        /// <summary>A when on Sprocket's own <paramref name="member" /> of the lamp being <paramref name="value" />, which switches the door off.</summary>
        public void When(string member, Literal value)
        {
            var when = new WhenTrigger(member) { Provider = BlockKinds.Own, Quantifier = Quantifier.Any, Relation = Relation.Is, Value = value };
            when.Sources.Add(Name(Lamp));
            when.Does = Off();
            Program.AddSequence((Trigger)null!).System.AddTrigger(when);
        }

        /// <summary>A sequence started by the lamp's signal <paramref name="signal" />, which switches the door off.</summary>
        public void On(string signal)
        {
            var on = SignalTrigger.Written(BlockKinds.Own, signal);
            on.Sources.Add(Name(Lamp));
            Program.AddSequence(on).Steps.Add(Off());
        }

        public void Start()
        {
            Program.Edited();
            Runner = new AutomationRunner(Program, Rig.World);
            Tick(23);
        }

        public void Tick(int ticks = 1)
        {
            for (var i = 0; i < ticks; i++)
            {
                Rig.Tick();
                Runner.Tick(++Frame);
            }
        }

        /// <summary>Ticks until the door has been switched off, or a hundred ticks have gone by.</summary>
        public void TickUntilFired()
        {
            var until = Frame + 100;
            while (Frame < until && Fired == 0)
                Tick();
        }

        Act Off()
        {
            var off = new Act(BlockKinds.SwitchOff) { Form = Form.Native };
            off.Targets.Add(Name(Door));
            return off;
        }
    }

    /// <summary>Every block has the members every block shares, and each says how a change in it is heard.</summary>
    [Fact]
    public void EveryBlockHasWhatEveryBlockShares()
    {
        var ship = new Ship();
        var kind = ship.Rig.Entity(ship.Lamp.Id).Kind;

        foreach (var id in new[] { "working", "being-hacked", "custom-data", "integrity", "damaged", "broken", "build-level" })
            Assert.NotNull(kind.FindAction(id, Form.Value));

        foreach (var id in new[] { "starts-working", "stops-working", "breaks", "gets-repaired" })
            Assert.NotNull(kind.FindEvent(id));

        Assert.True(kind.FindAction("working", Form.Value)!.Announced);
        Assert.True(kind.FindAction("custom-data", Form.Value)!.Announced);
        Assert.True(kind.FindAction("integrity", Form.Value)!.Announced);
        Assert.NotEqual("", kind.FindAction("being-hacked", Form.Value)!.LookedBecause);
    }

    /// <summary>A block the harness gives no inventory, power or screen offers none of what those bring.</summary>
    [Fact]
    public void WhatABlockHasNotGotIsNotOffered()
    {
        var ship = new Ship();
        var kind = ship.Rig.Entity(ship.Lamp.Id).Kind;

        foreach (var id in new[] { "inventory-fill", "inventory-mass", "powered", "power-use" })
            Assert.Null(kind.FindAction(id, Form.Value));

        Assert.Null(kind.FindAction("show-text", Form.Native));
    }

    /// <summary>A block that starts working is seen on the tick after, since every block announces it.</summary>
    [Fact]
    public void ABlockStartingToWorkIsSeenOnTheTickAfter()
    {
        var ship = new Ship();
        ship.Lamp.Enabled = false;
        ship.On("starts-working");
        ship.Start();
        Assert.Equal(0, ship.Fired);

        ship.Lamp.Enabled = true;
        var changed = ship.Frame;
        ship.TickUntilFired();

        Assert.Equal(1, ship.Fired);
        Assert.Equal(changed + 1, ship.Frame);
    }

    /// <summary>A block that stops working fires the signal for it, and not the one for starting.</summary>
    [Fact]
    public void ABlockStoppingWorkFiresStopsWorking()
    {
        var ship = new Ship();
        ship.On("starts-working");
        ship.Start();

        ship.Lamp.Enabled = false;
        ship.Tick(30);
        Assert.Equal(0, ship.Fired);

        var stops = new Ship();
        stops.On("stops-working");
        stops.Start();

        stops.Lamp.Enabled = false;
        stops.TickUntilFired();
        Assert.Equal(1, stops.Fired);
    }

    /// <summary>Custom Data written is seen on the tick after, since every block announces it, and a when on it fires once.</summary>
    [Fact]
    public void CustomDataWrittenIsSeenOnTheTickAfter()
    {
        var ship = new Ship();
        ship.When("custom-data", Literal.Of("go"));
        ship.Start();
        Assert.Equal(0, ship.Fired);

        ship.Lamp.CustomData = "go";
        var written = ship.Frame;
        ship.TickUntilFired();

        Assert.Equal(1, ship.Fired);
        Assert.Equal(written + 1, ship.Frame);

        ship.Lamp.CustomData = "go on";
        ship.Tick(30);
        Assert.Equal(1, ship.Fired);
    }

    /// <summary>The game's toggle of a block's switch is offered as toggle on/off.</summary>
    [Fact]
    public void TheSwitchToggleIsToggleOnOff()
    {
        var ship = new Ship();
        var toggle = ship.Rig.Entity(ship.Lamp.Id).Kind.FindAction("OnOff", Form.Action)!;

        Assert.False(toggle.Hidden);
        Assert.Equal("Toggle on/off", toggle.Named);
        Assert.Equal("toggle on/off", toggle.Text);
    }
}
