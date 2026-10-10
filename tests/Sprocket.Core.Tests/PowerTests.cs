using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;
using ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode;

namespace Sprocket.Tests;

/// <summary>A battery's members: recharge with or without waiting, its toggles and charge mode in Sprocket's words, and its own readings.</summary>
/// <remarks>The rig models only the battery of the power family; the other blocks are proved in the game alone.</remarks>
public class PowerTests
{
    /// <summary>A battery offers its toggles and charge mode in Sprocket's words, and leaves out what charge mode says.</summary>
    [Fact]
    public void ABatterySaysItsTogglesAndChargeModeInSprocketsWords()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.Battery(1, "Battery").Id).Kind;

        var recharge = kind.FindAction("Recharge", Form.Action)!;
        Assert.False(recharge.Hidden);
        Assert.Equal("Toggle recharge", recharge.Named);

        var discharge = kind.FindAction("Discharge", Form.Action)!;
        Assert.False(discharge.Hidden);
        Assert.Equal("Toggle discharge", discharge.Named);

        var mode = kind.FindAction("ChargeMode", Form.Value)!;
        Assert.False(mode.Hidden);
        Assert.Equal("Charge mode", mode.Named);

        foreach (var id in new[] { "Recharge_On", "Recharge_Off", "Discharge_On", "Discharge_Off", "Auto" })
            Assert.True(kind.FindAction(id, Form.Action)!.Hidden, id + " is listed");
    }

    /// <summary>A battery's readings are Sprocket's own values, under power, each read and none set.</summary>
    [Fact]
    public void ABatteryReadsItsChargeAndPower()
    {
        var ship = new Rigged();
        var battery = ship.Battery(1, "Battery");
        battery.Charge = 0.25f;
        var kind = ship.Entity(battery.Id).Kind;

        foreach (var id in new[]
                 {
                     "charge", "stored-power", "capacity", "charging", "input", "maximum-input", "output", "maximum-output"
                 })
        {
            var value = kind.FindAction(id, Form.Value)!;
            Assert.NotNull(value.Being);
            Assert.False(value.Sets);
            Assert.Equal(Category.Power, value.Category);
        }

        Assert.Equal(25.0, Read(ship, battery, "charge"), 3);
        Assert.Equal(0.75, Read(ship, battery, "stored-power"), 3);
        Assert.Equal(3.0, Read(ship, battery, "capacity"), 3);
    }

    /// <summary>Recharge asks whether it waits until the battery is charged, waiting where nothing says otherwise.</summary>
    [Fact]
    public void RechargeAsksWhetherItWaits()
    {
        var ship = new Rigged();
        var recharge = ship.Entity(ship.Battery(1, "Battery").Id).Kind.FindAction(Blocks.Battery.Recharge, Form.Native)!;

        var fields = new List<Field>();
        recharge.Fields(fields);
        var wait = Assert.Single(fields);
        Assert.Equal("wait until charged", wait.Name);
        Assert.Equal("wait", wait.Default);
        Assert.Equal("on", wait.Meaning("wait"));
        Assert.Equal("off", wait.Meaning("move on"));
    }

    /// <summary>A recharge told to move on sets the battery recharging, and the sequence carries on long before it is full.</summary>
    [Fact]
    public void ARechargeThatMovesOnDoesNotHoldTheSequence()
    {
        var ship = new Rigged();
        var battery = ship.Battery(1, "Battery");
        var lamp = ship.Light(2, "Lamp");
        battery.Charge = 0.5f;

        var frames = Run(ship, battery, lamp, Literal.Of(false));

        Assert.Equal(ChargeMode.Recharge, battery.ChargeMode);
        Assert.True(battery.Charge < 1f, "the sequence waited for the battery to fill");
        Assert.InRange(frames, 0, 60);
    }

    /// <summary>A battery set to another mode is no trouble to a recharge that moved on: it was never waiting for it.</summary>
    [Fact]
    public void ARechargeThatMovesOnIsNotGivenUpOn()
    {
        var ship = new Rigged();
        var battery = ship.Battery(1, "Battery");
        var entity = ship.Entity(battery.Id);
        var off = new[] { Literal.Of(false) };

        ship.World.Invoke(entity, Blocks.Battery.Recharge, off);
        battery.Perform("Discharge_On");

        Assert.False(entity.Trouble(Blocks.Battery.Recharge, off).Exists);
    }

    /// <summary>A <c>when</c> on a battery's charge is seen on the tick after the charge moves, since the battery announces it.</summary>
    [Fact]
    public void AWhenOnChargeIsSeenOnTheTickAfter()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var battery = ship.Battery(1, "Battery");
        var lamp = ship.Light(2, "Lamp");
        battery.Charge = 0.25f;

        var when = new WhenTrigger("charge")
        {
            Provider = BlockKinds.Own, Quantifier = Quantifier.Any, Relation = Relation.AtLeast, Value = Literal.Of(50.0)
        };

        when.Sources.Add(automation.Declare(ship.World, ship.Entity(battery.Id)).Name);
        var on = new Act(BlockKinds.SwitchOn);
        on.Targets.Add(automation.Declare(ship.World, ship.Entity(lamp.Id)).Name);
        when.Does = on;
        automation.AddSequence((Trigger)null!).System.AddTrigger(when);
        automation.Edited();

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        for (; frame < 23; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        Assert.DoesNotContain("Lamp:on", ship.Rig.Log);
        Assert.True(battery.Listeners > 0, "nothing listens to the battery");

        battery.Charge = 0.5f;
        var charged = frame;
        while (frame < 200 && !ship.Rig.Log.Contains("Lamp:on"))
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.Equal(charged + 1, frame);
    }

    static double Read(Rigged ship, Battery battery, string id)
    {
        var value = ship.Entity(battery.Id).Kind.FindAction(id, Form.Value)!;
        return value.Being!.Gauge(battery.Block, new Literal[0], new Recall()).Read().Number;
    }

    /// <summary>Runs a recharge given <paramref name="wait" /> on the battery, and counts the frames until the step after its await runs.</summary>
    static int Run(Rigged ship, Battery battery, Lamp lamp, Literal wait)
    {
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);

        var act = new Act(Blocks.Battery.Recharge);
        act.Arguments.Add(wait);
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(battery.Id)).Name);
        sequence.Steps.Add(act);
        sequence.Steps.Add(new Await());

        var on = new Act(BlockKinds.SwitchOn);
        on.Targets.Add(automation.Declare(ship.World, ship.Entity(lamp.Id)).Name);
        sequence.Steps.Add(on);

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        ship.Tick();
        runner.Tick(++frame);
        runner.Call(sequence);
        var started = frame;

        while (!ship.Rig.Log.Contains("Lamp:on") && frame < 20000)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        return frame - started;
    }
}
