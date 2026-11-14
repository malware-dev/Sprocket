using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Battery = Sprocket.Sim.Machine.Battery;
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

        var recharge = kind.FindAction("Recharge", ReachKind.Action)!;
        Assert.False(recharge.Hidden);
        Assert.Equal("Toggle recharge", recharge.Named);

        var discharge = kind.FindAction("Discharge", ReachKind.Action)!;
        Assert.False(discharge.Hidden);
        Assert.Equal("Toggle discharge", discharge.Named);

        var mode = kind.FindAction("ChargeMode", ReachKind.Value)!;
        Assert.False(mode.Hidden);
        Assert.Equal("Charge mode", mode.Named);

        foreach (var id in new[] { "Recharge_On", "Recharge_Off", "Discharge_On", "Discharge_Off", "Auto" })
            Assert.True(kind.FindAction(id, ReachKind.Action)!.Hidden, id + " is listed");
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
                     "charge", "stored_power", "capacity", "charging", "input", "maximum_input", "output", "maximum_output"
                 })
        {
            var value = kind.FindAction(id, ReachKind.Value)!;
            Assert.NotNull(value.OwnValue);
            Assert.False(value.Sets);
            Assert.Equal(Category.Power, value.Category);
        }

        Assert.Equal(25.0, Read(ship, battery, "charge"), 3);
        Assert.Equal(0.75, Read(ship, battery, "stored_power"), 3);
        Assert.Equal(3.0, Read(ship, battery, "capacity"), 3);
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

    /// <summary>A <c>when</c> on a battery's charge or a thruster's override is seen on the tick after it changes, since both announce it.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AWhenOnChargeOrOverrideIsSeenOnTheTickAfter(bool onCharge)
    {
        var ship = new Rigged();
        var automation = new Automation();
        var lamp = ship.Light(2, "Lamp");
        long id;
        Func<bool> listened;
        Action change;
        if (onCharge)
        {
            var battery = ship.Battery(1, "Battery");
            battery.Charge = 0.25f;
            id = battery.Id;
            listened = () => battery.Listeners > 0;
            change = () => battery.Charge = 0.5f;
        }
        else
        {
            var thruster = ship.Thruster(1, "Thruster");
            id = thruster.Id;
            listened = () => thruster.Listeners > 0;
            change = () => thruster.Override = 0.6f;
        }

        var when = new WhenTrigger(onCharge ? "charge" : "override")
        {
            Provider = BlockKinds.Own, Quantifier = Quantifier.Any, Relation = Relation.AtLeast, Value = Literal.Of(50.0)
        };

        when.Sources.Add(automation.Declare(ship.World, ship.Entity(id)).Name);
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
        Assert.True(listened(), "nothing listens to the block");

        change();
        var changed = frame;
        while (frame < 200 && !ship.Rig.Log.Contains("Lamp:on"))
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.Equal(changed + 1, frame);
    }

    /// <summary>A battery vouches for every member it keeps, and for none of the game's duplicates of charge mode.</summary>
    [Fact]
    public void ABatteryVetsWhatItKeepsAndNotItsDuplicates()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.Battery(1, "Battery").Id).Kind;

        Vetted(kind, ReachKind.Native, Blocks.Battery.Recharge);
        Vetted(kind, ReachKind.Action, "Recharge", "Discharge");
        Vetted(kind, ReachKind.Value, "ChargeMode", "charge", "stored_power", "capacity", "charging", "input", "maximum_input",
            "output", "maximum_output");
        Unvetted(kind, ReachKind.Action, "Recharge_On", "Recharge_Off", "Discharge_On", "Discharge_Off", "Auto");
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

        Vetted(kind, ReachKind.Value, "override", "thrust", "maximum_thrust");
        Unvetted(kind, ReachKind.Value, "Override");
        Unvetted(kind, ReachKind.Action, "SetOverride", "IncreaseOverride", "DecreaseOverride");

        Assert.True(kind.FindAction("Override", ReachKind.Value)!.Hidden);
        foreach (var id in new[] { "SetOverride", "IncreaseOverride", "DecreaseOverride" })
            Assert.True(kind.FindAction(id, ReachKind.Action)!.Hidden, id + " is listed");
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

        Vetted(kind, ReachKind.Native, Blocks.JumpDrive.Recharge);
        Vetted(kind, ReachKind.Action, "Recharge");
        Vetted(kind, ReachKind.Value, "Recharge", "status", "charge", "stored_power", "capacity",
            "minimum_jump_distance", "maximum_jump_distance");
        Unvetted(kind, ReachKind.Value, "JumpDistance");
        Unvetted(kind, ReachKind.Action, "Recharge_On", "Recharge_Off", "Jump", "IncreaseJumpDistance", "DecreaseJumpDistance",
            "SetGravityAcceleration");

        Assert.True(kind.FindAction("JumpDistance", ReachKind.Value)!.Hidden);
        foreach (var id in new[] { "SetGravityAcceleration", "IncreaseJumpDistance", "DecreaseJumpDistance" })
            Assert.True(kind.FindAction(id, ReachKind.Action)!.Hidden, id + " is listed");
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

    static void Vetted(EntityKind kind, ReachKind form, params string[] ids)
    {
        foreach (var id in ids)
        {
            var member = kind.FindAction(id, form);
            Assert.True(member != null, id + " is not offered");
            Assert.True(member.Vetted, id + " is not vetted");
        }
    }

    static void Unvetted(EntityKind kind, ReachKind form, params string[] ids)
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
        var set = new Act(id) { Form = ReachKind.Value, Provider = BlockKinds.Own };
        set.Targets.Add(automation.Declare(ship.World, ship.Entity(fitting.Id)).Name);
        set.Arguments.Add(value);
        return set;
    }

    static double Read(Rigged ship, Fitting fitting, string id)
    {
        var value = ship.Entity(fitting.Id).Kind.FindAction(id, ReachKind.Value)!;
        return value.OwnValue!.Gauge(fitting.Block, new Literal[0], new ReaderMemory()).Read().Number;
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
