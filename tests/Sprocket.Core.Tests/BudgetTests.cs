using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Xunit;

namespace Sprocket.Tests;

/// <summary>What the runtime asks the game, counted: nothing where nothing is watched, and one look per cadence where something is.</summary>
/// <remarks>
/// A server runs every program of every player, so a question asked on a tick where nothing has happened is asked
/// thousands of times over. The harness counts every call into the game's interfaces (<see cref="Pretend.Asked" />), and
/// these hold the runtime to it. Only the runner is ticked while counting: what the rig does to move its own blocks is
/// the game's work, not Sprocket's.
/// </remarks>
public class BudgetTests
{
    const int Span = 1000;

    [Fact]
    public void AProgramWithNothingToWatchAsksTheGameNothing()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var door = ship.Door(1, "Door");
        var sequence = automation.AddSequence((Trigger)null!);
        var act = new Act("open");
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(door.Id)).Name);
        sequence.Steps.Add(act);

        var runner = new AutomationRunner(automation, ship.World);
        var frame = Settle(ship, runner);

        Assert.Equal(0, Counted(runner, ref frame));
    }

    /// <summary>A door announces opening, so its <c>when</c> looks only as a backstop: once a hundred ticks, one question.</summary>
    [Fact]
    public void AnAnnouncedMomentIsLookedAtOnceAHundredTicks()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var door = ship.Door(1, "Door");
        var lamp = ship.Light(2, "Lamp");
        Watch(ship, automation, "finishes-opening", door.Id, lamp.Id);

        var runner = new AutomationRunner(automation, ship.World);
        var frame = Settle(ship, runner);

        Assert.Equal(new[] { "IMyDoor.get_Status" }, Look(runner, ref frame, 100));
        Assert.Equal(Span / 100, Counted(runner, ref frame));
    }

    /// <summary>
    /// A vent announces nothing, so its <c>when</c> looks on its cadence, ten ticks unless it says otherwise, and a look
    /// costs the reading and nothing besides.
    /// </summary>
    /// <remarks>
    /// A full room is read to the end of the reading: whether the vent works, how full the room is, the gas it is still
    /// moving and the frame that is seen on. The gas is the harness's own meter here, which finds the vent by its id; the
    /// game's reads the vent's components instead.
    /// </remarks>
    [Fact]
    public void ALookedForMomentIsLookedAtOnItsCadence()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var vent = ship.Vent(1, "Vent");
        var lamp = ship.Light(2, "Lamp");
        Watch(ship, automation, "finishes-filling", vent.Id, lamp.Id);

        var runner = new AutomationRunner(automation, ship.World);
        var frame = Settle(ship, runner);

        var look = Look(runner, ref frame, 10);
        Assert.Equal(new[]
        {
            "IMyCubeBlock.get_IsWorking", "IMyAirVent.GetOxygenLevel", "IMyEntity.get_EntityId", "IMyEntity.get_EntityId",
            "IMySession.get_GameplayFrameCounter"
        }, look);
        Assert.Equal(Span / 10 * look.Count, Counted(runner, ref frame));
    }

    /// <summary>
    /// A <c>when</c> on one of the block's own values looks on its cadence and asks the game nothing by name: the value was
    /// made ready to read when the program was bound.
    /// </summary>
    /// <remarks>
    /// The harness reads the value off its fitting, so a look here asks the game nothing at all; the game's own reads the
    /// property it was handed once, which is one call a look.
    /// </remarks>
    [Fact]
    public void AWhenOnAValueAsksNothingByNameWhenItLooks()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var lamp = ship.Light(1, "Lamp");
        var door = ship.Door(2, "Door");
        lamp.Carrying("Radius", "Radius", ValueKind.Real);
        lamp.Set("Radius", Literal.Of(2.0));

        var when = new WhenTrigger("Radius") { Provider = "", Relation = Relation.Above, Value = Literal.Of(5.0) };
        when.Sources.Add(automation.Declare(ship.World, ship.Entity(lamp.Id)).Name);
        var sequence = automation.AddSequence(when);
        var act = new Act("open");
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(door.Id)).Name);
        sequence.Steps.Add(act);
        sequence.Steps.Add(new Await());

        var runner = new AutomationRunner(automation, ship.World);
        var frame = Settle(ship, runner);

        Assert.Empty(Look(runner, ref frame, 10));
        Assert.Equal(0, Counted(runner, ref frame));

        // Still watching: what it reads coming to hold starts the step.
        lamp.Set("Radius", Literal.Of(6.0));
        for (var i = 0; i < 20; i++)
            runner.Tick(++frame);

        Assert.Contains("Door:open", ship.Rig.Log);
    }

    /// <summary>An <c>on</c> <paramref name="signal" /> of <paramref name="watched" /> that switches <paramref name="lit" /> on.</summary>
    static void Watch(Rigged ship, Automation automation, string signal, long watched, long lit)
    {
        var when = SignalTrigger.Written(BlockKinds.Own, signal);
        when.Sources.Add(automation.Declare(ship.World, ship.Entity(watched)).Name);
        var sequence = automation.AddSequence(when);
        var act = new Act(BlockKinds.SwitchOn);
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(lit)).Name);
        sequence.Steps.Add(act);
    }

    /// <summary>Runs the rig and the runner until everything has been bound and looked at, and answers the frame reached.</summary>
    static int Settle(Rigged ship, AutomationRunner runner)
    {
        var frame = 0;
        for (; frame < 300; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        return frame;
    }

    /// <summary>The questions the runner asks over one <paramref name="cadence" />, by name, which is one look.</summary>
    static List<string> Look(AutomationRunner runner, ref int frame, int cadence)
    {
        Pretend.Heard = new List<string>();
        try
        {
            for (var i = 0; i < cadence; i++)
                runner.Tick(++frame);

            return Pretend.Heard;
        }
        finally
        {
            Pretend.Heard = null!;
        }
    }

    /// <summary>How many questions the runner asks the game over <see cref="Span" /> ticks of its own.</summary>
    static int Counted(AutomationRunner runner, ref int frame)
    {
        var before = Pretend.Asked;
        for (var i = 0; i < Span; i++)
            runner.Tick(++frame);

        return Pretend.Asked - before;
    }
}
