using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>A wheel suspension's wheel moved to a height, waited for until it stops moving.</summary>
public class SuspensionTests
{
    [Fact]
    public void MovingAWheelAsksForAHeightWithinTheSuspensionsTravel()
    {
        var ship = new Rigged();
        var suspension = ship.Suspension(1, "Wheel");
        var kind = ship.Entity(suspension.Id).Kind;
        var move = kind.FindAction(Blocks.Suspension.MoveTo, ReachKind.Native)!;

        Assert.True(move.Lasts);
        var asked = new List<Field>();
        move.Fields(asked);
        Assert.Single(asked);
        Assert.Equal("-1.5", asked[0].Least);
        Assert.Equal("1.3", asked[0].Most);
    }

    /// <summary>The step after waits for the wheel to get there and stand still for the half second that says it has stopped.</summary>
    [Fact]
    public void MovingAWheelIsWaitedForUntilItStopsMoving()
    {
        var ship = new Rigged();
        var suspension = ship.Suspension(1, "Wheel");
        var lamp = ship.Light(2, "Lamp");

        var frames = Run(ship, suspension, lamp, Literal.Of(1.2));

        Assert.Equal(1.2f, suspension.Travel, 3);

        // 1.2 m at a meter a second, then thirty ticks of standing still, looked at every ten.
        Assert.InRange(frames, 100, 125);
    }

    /// <summary>A wheel sent to where it stands is waited for only the half second that says it is still.</summary>
    [Fact]
    public void MovingAWheelToWhereItStandsFinishesOnceItIsSeenStill()
    {
        var ship = new Rigged();
        var suspension = ship.Suspension(1, "Wheel");
        var lamp = ship.Light(2, "Lamp");

        var frames = Run(ship, suspension, lamp, Literal.Of(0.0));

        Assert.InRange(frames, 30, 45);
    }

    [Fact]
    public void ASuspensionWithNoWheelSaysWhyItIsNotMoving()
    {
        var ship = new Rigged();
        var suspension = ship.Suspension(1, "Wheel");
        suspension.Attached = false;
        var entity = ship.Entity(suspension.Id);

        ship.World.Invoke(entity, Blocks.Suspension.MoveTo, new[] { Literal.Of(1.0) });

        Assert.Equal("cannot move because it has no wheel",
            TroubleText.Of(new Texts(), entity.Trouble(Blocks.Suspension.MoveTo, new[] { Literal.Of(1.0) })));
    }

    /// <summary>Runs the one act on the suspension, and counts the frames until the step after it runs.</summary>
    static int Run(Rigged ship, Suspension suspension, Lamp lamp, params Literal[] given)
    {
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);

        var act = new Act(Blocks.Suspension.MoveTo);
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(suspension.Id)).Name);
        foreach (var value in given)
            act.Arguments.Add(value);
        sequence.Steps.Add(act);
        sequence.Steps.Add(new Await());

        var on = Given.Power(true);
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
