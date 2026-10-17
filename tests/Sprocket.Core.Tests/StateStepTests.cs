using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A step setting a state of its blocks reads as enabling or disabling it; any other setting reads as a set.</summary>
public class StateStepTests
{
    /// <summary>A state set on reads as enabled, and set off as disabled.</summary>
    [Theory]
    [InlineData(true, "enable stockpiling on Tank")]
    [InlineData(false, "disable stockpiling on Tank")]
    public void AStateSetReadsAsEnabledOrDisabled(bool on, string said)
    {
        var ship = new Rigged();
        var tank = ship.Tank(1, "Tank");
        tank.Carrying("Stockpile", "Stockpile", ValueKind.Flag);

        Assert.Equal(said, Said(ship, tank.Id, "Stockpile", Literal.Of(on)));
    }

    /// <summary>A setting that is no state of its block reads as a set.</summary>
    [Fact]
    public void ASettingReadsAsASet()
    {
        var ship = new Rigged();
        var timer = ship.Rig.Timer(1, "Timer");
        timer.Carrying("Silent", "Silent", ValueKind.Flag);

        Assert.Equal("set silent on Timer to on", Said(ship, timer.Id, "Silent", Literal.Of(true)));
    }

    /// <summary>What a step setting <paramref name="id" /> on the block <paramref name="block" /> to <paramref name="value" /> says.</summary>
    static string Said(Rigged ship, long block, string id, Literal value)
    {
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var set = new Act(id) { Form = ReachKind.Value };
        set.Targets.Add(automation.Declare(ship.World, ship.Entity(block)).Name);
        set.Arguments.Add(value);
        sequence.Steps.Add(set);
        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);

        return SequenceText.Step(runner.Bindings, sequence, set, null);
    }
}
