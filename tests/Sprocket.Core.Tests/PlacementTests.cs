using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using SpaceEngineers.Game.ModAPI;
using Xunit;
using TextPanel = Sprocket.Host.SpaceEngineers.Blocks.TextPanel;

namespace Sprocket.Tests;

/// <summary>
/// Where a block's members are listed: under the heading a player looks for them under, each block judged on its own,
/// whether the member is Sprocket's own, the game's placed by the kind of block, or the game's placed by a capability.
/// </summary>
public class PlacementTests
{
    /// <summary>A sequence brakes and drives a wheel while it runs, so a suspension's brake, power and speed limit are Movement.</summary>
    [Fact]
    public void ASuspensionDrivesUnderMovement()
    {
        var ship = new Rigged();
        var wheel = ship.Rig.Weld(new Standing<IMyMotorSuspension>(1, "Wheel", typeof(MyObjectBuilder_MotorSuspension), "Wheel"))
            .Carrying("Braking", "Brake", ValueKind.Flag)
            .Carrying("Power", "Power", ValueKind.Real, "0", "100")
            .Carrying("Speed Limit", "Speed Limit", ValueKind.Real, "0", "360")
            .Carrying("ShareInertiaTensor", "Share inertia tensor", ValueKind.Flag);
        var kind = ship.Entity(wheel.Id).Kind;

        Assert.Equal(Category.Movement, kind.FindAction("Braking", ReachKind.Value)!.Category);
        Assert.Equal(Category.Movement, kind.FindAction("Power", ReachKind.Value)!.Category);
        Assert.Equal(Category.Movement, kind.FindAction("Speed Limit", ReachKind.Value)!.Category);
        Assert.Equal(Category.Behaviour, kind.FindAction("ShareInertiaTensor", ReachKind.Value)!.Category);
    }

    /// <summary>A timer's delay is set by the sequence that starts it, and its countdown is read beside start and stop.</summary>
    [Fact]
    public void ATimersDelayIsAutomation()
    {
        var ship = new Rigged();
        var timer = ship.Rig.Timer(1, "Timer").Carrying("TriggerDelay", "Delay", ValueKind.Real, "1", "3600");
        var kind = ship.Entity(timer.Id).Kind;

        Assert.Equal(Category.Automation, kind.FindAction("TriggerDelay", ReachKind.Value)!.Category);
        Assert.Equal(Category.Automation, kind.FindAction("counting-down", ReachKind.Value)!.Category);
    }

    /// <summary>A survival kit's respawn name is its name, as a medical room's is; the kit is known by its capability.</summary>
    [Fact]
    public void ASurvivalKitsRespawnNameIsIdentity()
    {
        var ship = new Rigged();
        var kit = ship.Rig.Weld(new Standing<IMyAssembler>(1, "Survival Kit", typeof(MyObjectBuilder_SurvivalKit), "SurvivalKit"))
            .Carrying("SpawnName", "Spawn name", ValueKind.Text);

        Assert.Equal(Category.Identity, ship.Entity(kit.Id).Kind.FindAction("SpawnName", ReachKind.Value)!.Category);
    }

    /// <summary>The pull a gravity generator exerts fits no heading there is, so it is Other.</summary>
    [Fact]
    public void AGravityGeneratorsPullIsOther()
    {
        var ship = new Rigged();
        var field = ship.Rig.Weld(new Standing<IMyGravityGenerator>(1, "Gravity", typeof(MyObjectBuilder_GravityGenerator), "Gravity"))
            .Carrying("Gravity", "Acceleration", ValueKind.Real, "-9.81", "9.81")
            .Carrying("Width", "Width", ValueKind.Real, "1", "150");
        var kind = ship.Entity(field.Id).Kind;

        Assert.Equal(Category.Other, kind.FindAction("Gravity", ReachKind.Value)!.Category);
        Assert.Equal(Category.Other, kind.FindAction("Width", ReachKind.Value)!.Category);
        Assert.Equal(Category.Other, kind.FindAction("reverse", ReachKind.Native)!.Category);
    }

    /// <summary>How whole a block is reads under Repair, on any block.</summary>
    [Fact]
    public void HowWholeABlockIsIsRepair()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.Door(1, "Door").Id).Kind;

        Assert.Equal(Category.Repair, kind.FindAction("integrity", ReachKind.Value)!.Category);
        Assert.Equal(Category.Repair, kind.FindAction("damaged", ReachKind.Value)!.Category);
        Assert.Equal(Category.Repair, kind.FindAction("build-level", ReachKind.Value)!.Category);
    }

    /// <summary>Who sits in a cockpit, and whether they control it, read under Usage.</summary>
    [Fact]
    public void WhoSitsInACockpitIsUsage()
    {
        var ship = new Rigged();
        var kind = ship.Entity(ship.Rig.Cockpit(1, "Cockpit").Id).Kind;

        Assert.Equal(Category.Usage, kind.FindAction("occupant-name", ReachKind.Value)!.Category);
        Assert.Equal(Category.Usage, kind.FindAction("under-control", ReachKind.Value)!.Category);
    }

    /// <summary>What is fitted to an upgrade module, counted, reads under State.</summary>
    [Fact]
    public void AnUpgradeModulesCountsAreState()
    {
        var ship = new Rigged();
        var module = ship.Rig.Weld(new Standing<IMyUpgradeModule>(1, "Module", typeof(MyObjectBuilder_UpgradeModule), "Module"));
        var kind = ship.Entity(module.Id).Kind;

        Assert.Equal(Category.State, kind.FindAction("connections", ReachKind.Value)!.Category);
        Assert.Equal(Category.State, kind.FindAction("upgrade-count", ReachKind.Value)!.Category);
    }

    /// <summary>A panel's text, said as text, and its app are what it writes on itself.</summary>
    [Fact]
    public void APanelsTextIsScreen()
    {
        var ship = new Rigged();
        var panel = ship.Rig.TextPanel(1, "Panel");
        var text = ship.Entity(panel.Id).Kind.FindAction(TextPanel.Shown, ReachKind.Value)!;

        Assert.Equal("text", text.Text);
        Assert.Equal(Category.Screen, text.Category);
        Assert.Equal(Category.Screen, ship.Entity(panel.Id).Kind.FindAction(TextPanel.App, ReachKind.Value)!.Category);
    }
}
