using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;
using ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Tests;

/// <summary>
/// A battery recharged and a tank stockpiling, each waited for until it is full.
/// </summary>
/// <remarks>
/// The rig models no gas, so what a tank with nothing coming in says is proved in the game alone.
/// </remarks>
public class StoreTests
{
    [Fact]
    public void RechargingIsSaidInSprocketsWordsAndWaitedFor()
    {
        var ship = new Rigged();
        var battery = ship.Battery(1, "Battery");
        var kind = ship.Entity(battery.Id).Kind;

        var recharge = kind.FindAction(Blocks.Battery.Recharge, Form.Native)!;
        Assert.True(recharge.Lasts);
        Assert.False(recharge.Hidden);

        // Recharging stands in for the game's own act, and charge mode says the rest.
        Assert.True(kind.FindAction("Recharge_On", Form.Action)!.Hidden);
        Assert.True(kind.FindAction("Discharge_On", Form.Action)!.Hidden);
        Assert.False(kind.FindAction("ChargeMode", Form.Value)!.Hidden);
    }

    [Fact]
    public void RechargingHoldsTheSequenceUntilTheBatteryIsFull()
    {
        var ship = new Rigged();
        var battery = ship.Battery(1, "Battery");
        var lamp = ship.Light(2, "Lamp");
        battery.Charge = 0.5f;

        var frames = Run(ship, battery, lamp, Blocks.Battery.Recharge);

        Assert.Equal(1f, battery.Charge, 3);
        Assert.InRange(frames, 60, 80);

        // Changing its mode again is a step of its own.
        Assert.Equal(ChargeMode.Recharge, battery.ChargeMode);
    }

    [Fact]
    public void ABatteryWithNoPowerComingInSaysSo()
    {
        var ship = new Rigged();
        var battery = ship.Battery(1, "Battery");
        battery.Powered = false;
        var entity = ship.Entity(battery.Id);

        ship.World.Invoke(entity, Blocks.Battery.Recharge);

        var trouble = entity.Trouble(Blocks.Battery.Recharge, new Literal[0]);
        Assert.Equal("cannot charge - no power coming in", TroubleText.Of(new Texts(), trouble));
        Assert.False(trouble.Ends);
    }

    [Fact]
    public void ABatterySetToAnotherModeIsGivenUpOn()
    {
        var ship = new Rigged();
        var battery = ship.Battery(1, "Battery");
        var entity = ship.Entity(battery.Id);

        ship.World.Invoke(entity, Blocks.Battery.Recharge);
        battery.Perform("Discharge_On");

        Assert.True(entity.Trouble(Blocks.Battery.Recharge, new Literal[0]).Ends);
    }

    [Fact]
    public void AFullBatteryHasNothingWrongWithIt()
    {
        var ship = new Rigged();
        var battery = ship.Battery(1, "Battery");
        battery.Powered = false;
        battery.Charge = 1f;
        var entity = ship.Entity(battery.Id);

        ship.World.Invoke(entity, Blocks.Battery.Recharge);

        Assert.False(entity.Trouble(Blocks.Battery.Recharge, new Literal[0]).Exists);
    }

    [Fact]
    public void StockpilingIsSaidInSprocketsWordsAndWaitedFor()
    {
        var ship = new Rigged();
        var tank = ship.Tank(1, "Tank");
        var kind = ship.Entity(tank.Id).Kind;

        var stockpile = kind.FindAction(Blocks.Tank.Stockpile, Form.Native)!;
        Assert.True(stockpile.Lasts);
        Assert.False(stockpile.Hidden);

        // Stockpiling stands in for the game's own act and for the switch, and the game's act
        // switches it off.
        Assert.True(kind.FindAction("Stockpile_On", Form.Action)!.Hidden);
        Assert.True(kind.FindAction("Stockpile", Form.Value)!.Hidden);
        Assert.False(kind.FindAction("Stockpile_Off", Form.Action)!.Hidden);
    }

    [Fact]
    public void StockpilingHoldsTheSequenceUntilTheTankIsFull()
    {
        var ship = new Rigged();
        var tank = ship.Tank(1, "Tank");
        var lamp = ship.Light(2, "Lamp");
        tank.Filled = 0.25;

        var frames = Run(ship, tank, lamp, Blocks.Tank.Stockpile);

        Assert.Equal(1.0, tank.Filled, 3);
        Assert.InRange(frames, 90, 110);

        // Switching it off again is a step of its own.
        Assert.True(tank.Stockpile);
    }

    [Fact]
    public void ATankSwitchedOffSaysSo()
    {
        var ship = new Rigged();
        var tank = ship.Tank(1, "Tank");
        tank.Enabled = false;
        var entity = ship.Entity(tank.Id);

        ship.World.Invoke(entity, Blocks.Tank.Stockpile);

        var trouble = entity.Trouble(Blocks.Tank.Stockpile, new Literal[0]);
        Assert.Equal("cannot fill - it is switched off or has no power", TroubleText.Of(new Texts(), trouble));
        Assert.False(trouble.Ends);
    }

    [Fact]
    public void ATankWhoseStockpileIsSwitchedOffIsGivenUpOn()
    {
        var ship = new Rigged();
        var tank = ship.Tank(1, "Tank");
        var entity = ship.Entity(tank.Id);

        ship.World.Invoke(entity, Blocks.Tank.Stockpile);
        tank.Perform("Stockpile_Off");

        Assert.True(entity.Trouble(Blocks.Tank.Stockpile, new Literal[0]).Ends);
    }

    /// <summary>Runs the one act on the block, and counts the frames until the step after it runs.</summary>
    static int Run(Rigged ship, Fitting fitting, Lamp lamp, string actionId)
    {
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);

        var act = new Act(actionId);
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(fitting.Id)).Name);
        sequence.Steps.Add(act);
        sequence.Steps.Add(new Await());

        var on = new Act(BlockKinds.SwitchOn);
        on.Targets.Add(automation.Declare(ship.World, ship.Entity(lamp.Id)).Name);
        sequence.Steps.Add(on);
        sequence.Steps.Add(new Await());

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
