using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;

namespace Sprocket.Tests;

/// <summary>One member of a block run or read the way a program does, for a test about what a block offers.</summary>
static class Ran
{
    /// <summary>Runs <paramref name="actionId" />, given <paramref name="arguments" />, on <paramref name="fitting" /> as a one-step sequence.</summary>
    public static void Step(Rigged ship, Fitting fitting, string actionId, params Literal[] arguments)
    {
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);
        var act = new Act(actionId);
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(fitting.Id)).Name);
        foreach (var argument in arguments)
            act.Arguments.Add(argument);

        sequence.Steps.Add(act);

        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);
        runner.Call(sequence);
        ship.Tick();
        runner.Tick(2);
    }

    /// <summary>What Sprocket's own value <paramref name="id" /> of <paramref name="fitting" /> reads now, given <paramref name="arguments" />.</summary>
    public static Literal Read(Rigged ship, Fitting fitting, string id, params Literal[] arguments)
    {
        var value = ship.Entity(fitting.Id).Kind.FindAction(id, Form.Value)!;
        return value.Being!.Gauge(fitting.Block, arguments, new Recall()).Read();
    }
}
