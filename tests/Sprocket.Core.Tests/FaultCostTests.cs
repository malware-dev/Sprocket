using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Tests;

/// <summary>What is wrong with a program is found when it is built and where a run meets it, and nothing checks on a clock.</summary>
public class FaultCostTests
{
    /// <summary>A program nothing changes is checked once, and never again however long it runs.</summary>
    [Fact]
    public void AnIdleProgramIsCheckedOnce()
    {
        var (ship, runner, _) = Airlock();

        for (var frame = 1; frame <= 10; frame++)
        {
            ship.Tick();
            runner.Tick(frame);
        }

        var checks = runner.Checks;
        for (var frame = 11; frame <= 10000; frame++)
        {
            ship.Tick();
            runner.Tick(frame);
        }

        Assert.Equal(1, checks);
        Assert.Equal(checks, runner.Checks);
    }

    /// <summary>An edit to the program is checked on the next tick, once.</summary>
    [Fact]
    public void AnEditIsCheckedOnce()
    {
        var (ship, runner, automation) = Airlock();
        ship.Tick();
        runner.Tick(1);
        var checks = runner.Checks;

        automation.Edited();
        for (var frame = 2; frame <= 100; frame++)
        {
            ship.Tick();
            runner.Tick(frame);
        }

        Assert.Equal(checks + 1, runner.Checks);
    }

    /// <summary>An open editor looking at a construct nothing has changed looks at nothing, and one block gone is looked at once.</summary>
    [Fact]
    public void AnIdleEditorLooksAtNothing()
    {
        var (ship, runner, _) = Airlock();
        ship.Tick();
        runner.Tick(1);

        runner.Look();
        var looks = runner.Looks;
        for (var i = 0; i < 100; i++)
            runner.Look();

        Assert.Equal(looks, runner.Looks);

        ship.Rig.Grind(ship.Rig.Fitted.First(fitting => fitting.Name == "Inner Door"));
        runner.Look();
        runner.Look();

        Assert.Equal(looks + 1, runner.Looks);
    }

    /// <summary>A block gone from an idle airlock is found by the run that reaches it, which stops there and switches it off.</summary>
    [Fact]
    public void ABlockGoneIsFoundByTheRunThatNeedsIt()
    {
        var (ship, runner, automation) = Airlock();
        ship.Tick();
        runner.Tick(1);
        var sequence = automation.Sequences[0];

        ship.Rig.Grind(ship.Rig.Fitted.First(fitting => fitting.Name == "Inner Door"));
        ship.Tick();
        runner.Tick(2);
        Assert.True(sequence.System.Enabled);

        runner.Call(sequence, sequence.System.First);
        ship.Tick();
        runner.Tick(3);

        Assert.False(sequence.System.Enabled);
        Assert.Equal(SequenceProblem.MissingTarget, sequence.System.Halt!.Problem);
        Assert.Equal(SequenceProblem.MissingTarget, SequenceSummary.Fault(runner.Get(sequence, sequence.System.First)).Problem);
    }

    /// <summary>A system with one sequence that closes a door and waits for it, run by nothing.</summary>
    static (Rigged ship, AutomationRunner runner, Automation automation) Airlock()
    {
        var ship = new Rigged();
        ship.Door(2, "Inner Door");

        var automation = new Automation();
        var system = automation.AddSystem();
        var sequence = automation.AddSequence(system);
        sequence.Name = "Cycle";

        var close = new Act("close");
        close.Targets.Add(automation.Declare(ship.World, ship.Entity(2)).Name);
        sequence.Steps.Add(close);
        sequence.Steps.Add(new Await());

        return (ship, new AutomationRunner(automation, ship.World), automation);
    }
}
