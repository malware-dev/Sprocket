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

    /// <summary>
    /// Sets Sprocket's own value <paramref name="id" /> of <paramref name="fitting" /> to <paramref name="value" /> as a one-step
    /// sequence, and gives back what checking that sequence found wrong with it.
    /// </summary>
    /// <remarks>The sequence repeats on a long interval, so a check finds nothing wrong with how it starts.</remarks>
    public static SequenceProblem Set(Rigged ship, Fitting fitting, string id, Literal value)
    {
        var automation = new Automation();
        var sequence = automation.AddSequence(new EveryTrigger(600));
        var set = new Act(id) { Form = ReachKind.Value, Provider = BlockKinds.Own };
        set.Targets.Add(automation.Declare(ship.World, ship.Entity(fitting.Id)).Name);
        set.Arguments.Add(value);
        sequence.Steps.Add(set);

        var runner = new AutomationRunner(automation, ship.World);
        ship.Tick();
        runner.Tick(1);
        var problem = SequenceSummary.Checked(ship.World, runner.Bindings, sequence, sequence.System.First).Problem;
        runner.Call(sequence);
        ship.Tick();
        runner.Tick(2);
        return problem;
    }

    /// <summary>What Sprocket's own value <paramref name="id" /> of <paramref name="fitting" /> reads now, given <paramref name="arguments" />.</summary>
    public static Literal Read(Rigged ship, Fitting fitting, string id, params Literal[] arguments)
    {
        var value = ship.Entity(fitting.Id).Kind.FindAction(id, ReachKind.Value)!;
        return value.OwnValue!.Gauge(fitting.Block, arguments, new ReaderMemory()).Read();
    }
}
