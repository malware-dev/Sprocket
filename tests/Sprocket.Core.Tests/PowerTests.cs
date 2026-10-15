using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;
using ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode;

namespace Sprocket.Tests;

/// <summary>
/// The power family's members: a battery's recharge with or without waiting, its toggles and charge mode in Sprocket's words
/// and its own readings; a thruster's override and a jump drive's distance set by a step; and what each vouches for.
/// </summary>
/// <remarks>The rig models the battery, the thruster and the jump drive of the power family; the other blocks are proved in the game alone.</remarks>
public class PowerTests
{
    /// <summary>A battery offers its toggles and charge mode in Sprocket's words, and leaves out what charge mode says.</summary>
    [Fact]
    public void ABatterySaysItsTogglesAndChargeModeInSprocketsWords()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.Battery(1, "Battery").Id).Kind;

        var recharge = kind.FindAction("Recharge", Nature.Action)!;
        Assert.False(recharge.Hidden);
        Assert.Equal("Toggle recharge", recharge.Named);

        var discharge = kind.FindAction("Discharge", Nature.Action)!;
        Assert.False(discharge.Hidden);
        Assert.Equal("Toggle discharge", discharge.Named);

        var mode = kind.FindAction("ChargeMode", Nature.Value)!;
        Assert.False(mode.Hidden);
        Assert.Equal("Charge mode", mode.Named);

        foreach (var id in new[] { "Recharge_On", "Recharge_Off", "Discharge_On", "Discharge_Off", "Auto" })
            Assert.True(kind.FindAction(id, Nature.Action)!.Hidden, id + " is listed");
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
            var value = kind.FindAction(id, Nature.Value)!;
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
        var recharge = ship.Entity(ship.Battery(1, "Battery").Id).Kind.FindAction(Blocks.Battery.Recharge, Nature.Native)!;

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
        var on = Given.Power(true);
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

    /// <summary>A battery vouches for every member it keeps, and for none of the game's duplicates of charge mode.</summary>
    [Fact]
    public void ABatteryVetsWhatItKeepsAndNotItsDuplicates()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.Battery(1, "Battery").Id).Kind;

        Vetted(kind, Nature.Native, Blocks.Battery.Recharge);
        Vetted(kind, Nature.Action, "Recharge", "Discharge");
        Vetted(kind, Nature.Value, "ChargeMode", "charge", "stored-power", "capacity", "charging", "input", "maximum-input",
            "output", "maximum-output");
        Unvetted(kind, Nature.Action, "Recharge_On", "Recharge_Off", "Discharge_On", "Discharge_Off", "Auto");
    }

    /// <summary>
    /// A thruster vouches for its override in percent and its readings, and for none of the game's override in newtons, its
    /// setter or its steps, which are hidden behind it.
    /// </summary>
    [Fact]
    public void AThrusterVetsWhatItKeepsAndNotItsDuplicates()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.Thruster(1, "Thruster").Id).Kind;

        Vetted(kind, Nature.Value, "override", "thrust", "maximum-thrust");
        Unvetted(kind, Nature.Value, "Override");
        Unvetted(kind, Nature.Action, "SetOverride", "IncreaseOverride", "DecreaseOverride");

        Assert.True(kind.FindAction("Override", Nature.Value)!.Hidden);
        foreach (var id in new[] { "SetOverride", "IncreaseOverride", "DecreaseOverride" })
            Assert.True(kind.FindAction(id, Nature.Action)!.Hidden, id + " is listed");
    }

    /// <summary>
    /// A jump drive vouches for what it keeps: its recharge, recharging and its toggle, its status, charge and reach, and its
    /// jump distance in kilometers; and for none of the game's duplicates of them, nor its jump, which waits for a pilot.
    /// </summary>
    [Fact]
    public void AJumpDriveVetsWhatItKeepsAndNotItsDuplicates()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.JumpDrive(1, "Drive").Id).Kind;

        Vetted(kind, Nature.Native, Blocks.JumpDrive.Recharge);
        Vetted(kind, Nature.Action, "Recharge");
        Vetted(kind, Nature.Value, "Recharge", "status", "charge", "stored-power", "capacity",
            "minimum-jump-distance", "maximum-jump-distance");
        Unvetted(kind, Nature.Value, "JumpDistance");
        Unvetted(kind, Nature.Action, "Recharge_On", "Recharge_Off", "Jump", "IncreaseJumpDistance", "DecreaseJumpDistance",
            "SetGravityAcceleration");

        Assert.True(kind.FindAction("JumpDistance", Nature.Value)!.Hidden);
        foreach (var id in new[] { "SetGravityAcceleration", "IncreaseJumpDistance", "DecreaseJumpDistance" })
            Assert.True(kind.FindAction(id, Nature.Action)!.Hidden, id + " is listed");
    }

    /// <summary>A step sets a thruster's override in percent, and it reads back so.</summary>
    [Fact]
    public void AStepSetsAThrustersOverride()
    {
        var ship = new Rigged();
        var thruster = ship.Thruster(1, "Thruster");

        Set(ship, thruster, "override", Literal.Of(40.0));

        Assert.Equal(0.4f, thruster.Override, 3);
        Assert.Equal(40.0, Read(ship, thruster, "override"), 3);
    }

    /// <summary>An override outside nought to a hundred percent is a fault on the step.</summary>
    [Theory]
    [InlineData(101.0, SequenceProblem.Mistyped)]
    [InlineData(-1.0, SequenceProblem.Mistyped)]
    [InlineData(100.0, SequenceProblem.None)]
    [InlineData(0.0, SequenceProblem.None)]
    public void AnOverrideOutsideItsBoundsIsAFault(double given, SequenceProblem problem)
    {
        var ship = new Rigged();
        Assert.Equal(problem, Checked(ship, ship.Thruster(1, "Thruster"), "override", Literal.Of(given)));
    }

    /// <summary>A <c>when</c> on a thruster's override is seen on the tick after somebody changes it, since the thruster announces it.</summary>
    [Fact]
    public void AWhenOnOverrideIsSeenOnTheTickAfter()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var thruster = ship.Thruster(1, "Thruster");
        var lamp = ship.Light(2, "Lamp");

        var when = new WhenTrigger("override")
        {
            Provider = BlockKinds.Own, Quantifier = Quantifier.Any, Relation = Relation.AtLeast, Value = Literal.Of(50.0)
        };

        when.Sources.Add(automation.Declare(ship.World, ship.Entity(thruster.Id)).Name);
        var on = Given.Power(true);
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
        Assert.True(thruster.Listeners > 0, "nothing listens to the thruster");

        thruster.Override = 0.6f;
        var changed = frame;
        while (frame < 200 && !ship.Rig.Log.Contains("Lamp:on"))
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.Equal(changed + 1, frame);
    }

    static void Vetted(EntityKind kind, Nature form, params string[] ids)
    {
        foreach (var id in ids)
        {
            var member = kind.FindAction(id, form);
            Assert.True(member != null, id + " is not offered");
            Assert.True(member.Vetted, id + " is not vetted");
        }
    }

    static void Unvetted(EntityKind kind, Nature form, params string[] ids)
    {
        foreach (var id in ids)
        {
            var member = kind.FindAction(id, form);
            Assert.True(member != null, id + " is not offered");
            Assert.False(member.Vetted, id + " is vetted");
        }
    }

    /// <summary>Runs a step setting Sprocket's own value <paramref name="id" /> on <paramref name="fitting" /> to <paramref name="value" />.</summary>
    static void Set(Rigged ship, Fitting fitting, string id, Literal value)
    {
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        sequence.Steps.Add(Setting(automation, ship, fitting, id, value));
        automation.Edited();

        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);
        runner.Call(sequence);
        ship.Tick();
        runner.Tick(2);
    }

    /// <summary>What is wrong with a sequence whose one step sets Sprocket's own value <paramref name="id" /> on <paramref name="fitting" /> to <paramref name="value" />.</summary>
    static SequenceProblem Checked(Rigged ship, Fitting fitting, string id, Literal value)
    {
        var automation = new Automation();
        var sequence = automation.AddSequence(new EveryTrigger(600));
        sequence.Steps.Add(Setting(automation, ship, fitting, id, value));
        automation.Edited();

        var runner = new AutomationRunner(automation, ship.World);
        return SequenceSummary.Checked(ship.World, runner.Bindings, sequence, sequence.System.First).Problem;
    }

    static Act Setting(Automation automation, Rigged ship, Fitting fitting, string id, Literal value)
    {
        var set = new Act(id) { Form = Nature.Value, Provider = BlockKinds.Own };
        set.Targets.Add(automation.Declare(ship.World, ship.Entity(fitting.Id)).Name);
        set.Arguments.Add(value);
        return set;
    }

    static double Read(Rigged ship, Fitting fitting, string id)
    {
        var value = ship.Entity(fitting.Id).Kind.FindAction(id, Nature.Value)!;
        return value.Being!.Gauge(fitting.Block, new Literal[0], new Recall()).Read().Number;
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

        var on = Given.Power(true);
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
