using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// What a vent and a tank say of the air and the gas they hold, and what they send as it changes, on the rig's own vent and
/// tank, and what Sprocket vouches for on them and on a cryo chamber.
/// </summary>
/// <remarks>The rig carries no resource components, so a tank's running dry is looked for here rather than announced.</remarks>
public class GasTests
{
    /// <summary>A vent says its room in percent, whether it is airtight, the gas it moves, and the world's airtightness.</summary>
    [Fact]
    public void AVentSaysWhatItsRoomHolds()
    {
        var ship = new Rigged();
        var vent = ship.Vent(1, "Vent");

        Assert.Equal(100.0, Read(ship, vent, "oxygen-level").Real, 3);
        Assert.True(Read(ship, vent, "airtight").Flag);
        Assert.True(Read(ship, vent, "in-a-world-with-airtightness").Flag);
        Assert.Equal(0.0, Read(ship, vent, "flow-out-of-the-room").Real, 3);

        vent.Perform("Depressurize_On");
        for (var i = 0; i < 10; i++)
            ship.Tick();

        Assert.InRange(Read(ship, vent, "oxygen-level").Real, 0.0, 99.0);
        Assert.Equal(300.0, Read(ship, vent, "flow-out-of-the-room").Real, 3);
        Assert.Equal(0.0, Read(ship, vent, "flow-into-the-room").Real, 3);

        vent.Leaking = true;
        Assert.False(Read(ship, vent, "airtight").Flag);
    }

    /// <summary>A <c>when</c> on a vent's oxygen level fires as the room falls below it, in percent.</summary>
    [Fact]
    public void AWhenOnAVentsOxygenLevelFiresAsTheRoomEmpties()
    {
        var ship = new Rigged();
        var vent = ship.Vent(1, "Vent");
        var lamp = ship.Light(2, "Lamp");
        var when = new WhenTrigger("oxygen-level") { Provider = BlockKinds.Own, Relation = Relation.Below, Value = Literal.Of(50.0) };

        var frames = Fired(ship, vent, lamp, when, () => vent.Perform("Depressurize_On"));

        Assert.InRange(frames, 45, 60);
    }

    /// <summary>A tank's level is named for the gas it holds and read in percent, beside how much it holds.</summary>
    [Fact]
    public void ATanksLevelIsNamedForItsGas()
    {
        var ship = new Rigged();
        var tank = ship.Tank(1, "Tank");
        tank.Filled = 0.25;
        var kind = ship.Entity(tank.Id).Kind;

        var level = kind.FindAction("hydrogen-level", Form.Value)!;
        Assert.Equal("Hydrogen level", level.Named);
        Assert.Equal(Category.Gas, level.Category);
        Assert.Null(kind.FindAction("oxygen-level", Form.Value));
        Assert.Equal(25.0, Read(ship, tank, "hydrogen-level").Real, 3);
        Assert.Equal(tank.Capacity, Read(ship, tank, "capacity").Real, 3);
    }

    /// <summary>A tank sends that it has filled up as stockpiling brings it to full.</summary>
    [Fact]
    public void ATankSendsThatItFillsUp()
    {
        var ship = new Rigged();
        var tank = ship.Tank(1, "Tank");
        var lamp = ship.Light(2, "Lamp");
        tank.Filled = 0.5;

        var frames = Fired(ship, tank, lamp, SignalTrigger.Written(BlockKinds.Own, "fills-up"), () => tank.Perform("Stockpile_On"));

        Assert.InRange(frames, 55, 75);
    }

    /// <summary>A tank sends that it has run dry as its gas reaches nothing, and not while it still has some.</summary>
    /// <remarks>
    /// In the game the tank's resource source announces it. The rig has none, and the switch every block announces stands
    /// for the block's whole announcing, so here it is seen by the look every hundred ticks that backs an announcement up.
    /// </remarks>
    [Fact]
    public void ATankSendsThatItRunsDry()
    {
        var ship = new Rigged();
        var tank = ship.Tank(1, "Tank");
        var lamp = ship.Light(2, "Lamp");
        tank.Filled = 0.5;

        var frames = Fired(ship, tank, lamp, SignalTrigger.Written(BlockKinds.Own, "runs-dry"), () => tank.Filled = 0.0);

        Assert.InRange(frames, 1, 100);
    }

    /// <summary>A tank told to stockpile without waiting switches stockpile on and lets the sequence carry on at the next look at it, long before full.</summary>
    [Fact]
    public void StockpilingWithoutWaitingCarriesOnAtOnce()
    {
        var ship = new Rigged();
        var tank = ship.Tank(1, "Tank");
        var lamp = ship.Light(2, "Lamp");
        tank.Filled = 0.25;

        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var stockpile = new Act(Blocks.Tank.Stockpile);
        stockpile.Arguments.Add(Literal.Of(false));
        stockpile.Targets.Add(automation.Declare(ship.World, ship.Entity(tank.Id)).Name);
        sequence.Steps.Add(stockpile);
        sequence.Steps.Add(new Await());
        var on = Given.Power(true);
        on.Targets.Add(automation.Declare(ship.World, ship.Entity(lamp.Id)).Name);
        sequence.Steps.Add(on);

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        ship.Tick();
        runner.Tick(++frame);
        runner.Call(sequence);
        while (!ship.Rig.Log.Contains("Lamp:on") && frame < 200)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.True(tank.Stockpile);
        Assert.True(frame <= 10, "the sequence waited " + frame + " frames for a tank it was told not to wait for");
        Assert.True(tank.Filled < 1.0);
    }

    /// <summary>Stockpile asks whether to wait until full, says only the choice not to, and is written and read so.</summary>
    [Fact]
    public void StockpileAsksWhetherToWait()
    {
        var ship = new Rigged();
        var tank = ship.Tank(1, "Tank");
        var action = ship.Entity(tank.Id).Kind.FindAction(Blocks.Tank.Stockpile, Form.Native)!;

        var fields = new List<Field>();
        action.Fields(fields);
        var field = Assert.Single(fields);
        Assert.Equal(new[] { "until full", "without waiting" }, field.Words);
        Assert.True(new ArgumentsSlot(new[] { Literal.Of(true) }, fields, null).Quiet);
        Assert.Equal("without waiting", SentenceText.Given(new ArgumentsSlot(new[] { Literal.Of(false) }, fields, null)));

        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var stockpile = new Act(Blocks.Tank.Stockpile);
        stockpile.Arguments.Add(Literal.Of(false));
        stockpile.Targets.Add(automation.Declare(ship.World, ship.Entity(tank.Id)).Name);
        sequence.Steps.Add(stockpile);

        var saved = ProgramText.Save(automation, ship.World.Values);
        Assert.Contains("call ext.stockpile(off) on [ ", saved);
        Assert.Equal("", ProgramText.Load(saved, new Automation(), ship.World.Values));
    }

    /// <summary>A tank's own switches are offered in its words: the bottles it refills, and filling them by itself.</summary>
    [Fact]
    public void ATanksBottlesAreSaidInItsWords()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.Tank(1, "Tank").Id).Kind;

        Assert.Equal("refill bottles", kind.FindAction("Refill", Form.Action)!.Text);

        var automatic = kind.FindAction("Auto-Refill", Form.Value)!;
        Assert.Equal("Refilling bottles automatically", automatic.Named);
        Assert.False(automatic.Hidden);

        var toggle = kind.FindAction("Auto-Refill", Form.Action)!;
        Assert.Equal("toggle automatic refill", toggle.Text);
        Assert.True(toggle.Hidden);
        Assert.True(automatic.Flips);
    }

    /// <summary>
    /// A vent vouches for everything the survey keeps of it, its own members and the switch it words as depressurizing, and
    /// not for the pair beside that switch, which pressurize and depressurize say.
    /// </summary>
    [Fact]
    public void AVentVouchesForWhatItKeeps()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.Vent(1, "Vent").Id).Kind;

        foreach (var id in new[]
                 {
                     "room-status", "airtight", "oxygen-level", "flow-into-the-room", "flow-out-of-the-room",
                     "in-a-world-with-airtightness", "Depressurize"
                 })
            Assert.True(Vetted(kind, id, Form.Value), id);

        Assert.True(Vetted(kind, Blocks.Vent.Pressurize, Form.Native));
        Assert.True(Vetted(kind, Blocks.Vent.Depressurize, Form.Native));
        Assert.True(Vetted(kind, "Depressurize", Form.Action));

        Assert.False(Vetted(kind, "Depressurize_On", Form.Action));
        Assert.False(Vetted(kind, "Depressurize_Off", Form.Action));
    }

    /// <summary>
    /// A tank vouches for everything the survey keeps of it, its own members and the switches it words, and not for the pair
    /// beside stockpiling, which stockpile and stockpiling say.
    /// </summary>
    [Fact]
    public void ATankVouchesForWhatItKeeps()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.Tank(1, "Tank").Id).Kind;

        foreach (var id in new[] { "hydrogen-level", "capacity", "Stockpile", "Auto-Refill" })
            Assert.True(Vetted(kind, id, Form.Value), id);

        Assert.True(Vetted(kind, Blocks.Tank.Stockpile, Form.Native));
        foreach (var id in new[] { "Stockpile", "Refill", "Auto-Refill" })
            Assert.True(Vetted(kind, id, Form.Action), id);

        Assert.False(Vetted(kind, "Stockpile_On", Form.Action));
        Assert.False(Vetted(kind, "Stockpile_Off", Form.Action));
    }

    /// <summary>
    /// A cryo chamber vouches for who sleeps in it, its air, the ship's speed, and the seat's switches it words, and not for
    /// the pairs beside those switches or what a pilot steers with, which do nothing in a bed; it has no horizon, which the
    /// game's terminal never shows on one.
    /// </summary>
    [Fact]
    public void ACryoChamberVouchesForWhatItKeeps()
    {
        var ship = new Rigged();
        var chamber = ship.Rig.CryoChamber(1, "Bed");
        foreach (var id in new[] { "DampenersOverride", "HandBrake", "Park", "MainCockpit", "ControlThrusters", "HorizonIndicator" })
        {
            chamber.Carrying(id, id, ValueKind.Flag).Offering(id, id + " On/Off");
            chamber.Offering(id + "_On", id + " On").Offering(id + "_Off", id + " Off");
        }

        var kind = ship.Entity(chamber.Id).Kind;

        foreach (var id in new[] { "occupied", "occupant-name", "oxygen-level", "speed" })
            Assert.True(Vetted(kind, id, Form.Value), id);

        foreach (var id in new[] { "DampenersOverride", "HandBrake", "Park", "MainCockpit" })
        {
            Assert.True(Vetted(kind, id, Form.Value), id);
            Assert.True(Vetted(kind, id, Form.Action), id);
            Assert.False(Vetted(kind, id + "_On", Form.Action), id + "_On");
            Assert.False(Vetted(kind, id + "_Off", Form.Action), id + "_Off");
        }

        Assert.False(Vetted(kind, "ControlThrusters", Form.Value));
        Assert.False(Vetted(kind, "ControlThrusters", Form.Action));

        Assert.Null(kind.FindAction("HorizonIndicator", Form.Value));
        Assert.Null(kind.FindAction("HorizonIndicator", Form.Action));
    }

    /// <summary>A cryo chamber says the ship's speed once, as every ship controller does, under the cockpit's heading.</summary>
    [Fact]
    public void ACryoChamberSaysTheShipsSpeedOnce()
    {
        var ship = new Rigged();
        var chamber = ship.Rig.CryoChamber(1, "Bed");
        var cockpit = ship.Rig.Cockpit(2, "Cockpit");
        chamber.Speed = 12.5;
        var kind = ship.Entity(chamber.Id).Kind;

        var speeds = 0;
        foreach (var one in kind.Actions)
        {
            if (one.Id == "speed")
                speeds++;
        }

        Assert.Equal(1, speeds);
        var seated = ship.Entity(cockpit.Id).Kind.FindAction("speed", Form.Value)!;
        Assert.Equal(seated.Category, kind.FindAction("speed", Form.Value)!.Category);
        Assert.Equal(12.5, Ran.Read(ship, chamber, "speed").Real, 3);
    }

    /// <summary>Whether the member <paramref name="id" /> of <paramref name="kind" />, reached as <paramref name="form" />, is one Sprocket vouches for.</summary>
    static bool Vetted(EntityKind kind, string id, Form form)
    {
        var member = kind.FindAction(id, form);
        Assert.True(member != null, id + " is not on the block as " + form);
        return member.Vetted;
    }

    /// <summary>The value <paramref name="id" /> of Sprocket's own on <paramref name="fitting" />, read now.</summary>
    static Literal Read(Rigged ship, Fitting fitting, string id)
    {
        var entity = ship.Entity(fitting.Id);
        var being = entity.Kind.FindAction(id, Form.Value)!.Being!;
        return being.Gauge(entity.Block, new Literal[0], being.Remembers ? new Recall() : null).Read();
    }

    /// <summary>How many frames after <paramref name="change" /> a trigger on <paramref name="fitting" /> switched the lamp on.</summary>
    static int Fired(Rigged ship, Fitting fitting, Lamp lamp, Trigger trigger, Action change)
    {
        var automation = new Automation();
        ((MomentTrigger)trigger).Sources.Add(automation.Declare(ship.World, ship.Entity(fitting.Id)).Name);
        var sequence = automation.AddSequence(trigger);
        var on = Given.Power(true);
        on.Targets.Add(automation.Declare(ship.World, ship.Entity(lamp.Id)).Name);
        sequence.Steps.Add(on);

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        for (; frame < 23; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        Assert.DoesNotContain("Lamp:on", ship.Rig.Log);
        change();
        var changed = frame;
        while (frame < 2000 && !ship.Rig.Log.Contains("Lamp:on"))
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        return frame - changed;
    }
}
