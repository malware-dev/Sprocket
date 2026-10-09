using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>How each moment a block can reach is watched: announced, ruled out while a reading holds, or looked for with a reason.</summary>
public class WatchingTests
{
    /// <summary>
    /// Every moment of every kind of block says how it is watched: the block announces it, something it announces rules it
    /// out, a toolbar answers it, or it is looked for and says why nothing better is possible.
    /// </summary>
    [Fact]
    public void EveryMomentSaysHowItIsWatched()
    {
        var ship = new Rigged();
        var blocks = new[]
        {
            ship.Door(1, "Door").Id, ship.Vent(2, "Vent").Id, ship.Rotor(3, "Rotor").Id, ship.Panel(4, "Panel").Id,
            ship.Light(5, "Lamp").Id, ship.Beacon(6, "Beacon").Id, ship.Hinge(7, "Hinge").Id, ship.Piston(8, "Piston").Id,
            ship.Battery(9, "Battery").Id, ship.Tank(10, "Tank").Id, ship.Speaker(11, "Speaker").Id
        };

        var unsaid = new List<string>();
        foreach (var id in blocks)
        {
            var kind = ship.Entity(id).Kind;
            foreach (var moment in kind.Events)
            {
                if (!moment.Announced && moment.RuledOutBy == null && !moment.Answered && moment.LookedBecause.Length == 0)
                    unsaid.Add(kind.Id + " " + moment.Id);
            }
        }

        Assert.Empty(unsaid);
    }

    /// <summary>A block switched on is seen on the tick after, whatever the <c>when</c> asks for, since every functional block announces it.</summary>
    [Fact]
    public void ABlockSwitchedOnIsSeenOnTheTickAfter()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var door = ship.Door(1, "Door");
        var lamp = ship.Light(2, "Lamp");
        door.Enabled = false;

        var on = new WhenTrigger("switched-on");
        on.Sources.Add(automation.Declare(ship.World, ship.Entity(door.Id)).Name);
        on.IntervalTicks = 600;
        var sequence = automation.AddSequence(on);
        var act = new Act(BlockKinds.SwitchOn);
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(lamp.Id)).Name);
        sequence.Steps.Add(act);

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        for (; frame < 23; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        door.Enabled = true;
        var switched = frame;
        while (frame < 100 && runner.Get(sequence)!.FiredCount == 0)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.Equal(switched + 1, frame);
    }

    /// <summary>
    /// A hangar door somebody else starts closing is seen on the tick after, whatever the <c>when</c> asks for, since a
    /// hangar door announces starting to move either way.
    /// </summary>
    [Fact]
    public void AHangarDoorStartingToCloseIsSeenOnTheTickAfter()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var hangar = ship.HangarDoor(1, "Hangar");
        var lamp = ship.Light(2, "Lamp");
        hangar.ForceOpen();

        var closing = new WhenTrigger("closing");
        closing.Sources.Add(automation.Declare(ship.World, ship.Entity(hangar.Id)).Name);
        closing.IntervalTicks = 600;
        var sequence = automation.AddSequence(closing);
        var act = new Act(BlockKinds.SwitchOn);
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(lamp.Id)).Name);
        sequence.Steps.Add(act);

        var runner = new AutomationRunner(automation, ship.World);
        var frame = 0;
        for (; frame < 23; frame++)
        {
            ship.Tick();
            runner.Tick(frame + 1);
        }

        hangar.Perform("Open_Off", null!);
        var told = frame;
        while (frame < 100 && runner.Get(sequence)!.FiredCount == 0)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.Equal(told + 1, frame);
    }

    /// <summary>A hangar door's <c>when</c> listens for it starting to move, and deleting the <c>when</c> stops all of it.</summary>
    [Fact]
    public void AHangarDoorIsLetGoOf()
    {
        var ship = new Rigged();
        var automation = new Automation();
        var hangar = ship.HangarDoor(1, "Hangar");
        var lamp = ship.Light(2, "Lamp");

        var closing = new WhenTrigger("closing");
        closing.Sources.Add(automation.Declare(ship.World, ship.Entity(hangar.Id)).Name);
        var sequence = automation.AddSequence(closing);
        var act = new Act(BlockKinds.SwitchOn);
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(lamp.Id)).Name);
        sequence.Steps.Add(act);

        var runner = new AutomationRunner(automation, ship.World);
        runner.Tick(1);
        runner.Tick(2);
        Assert.Equal(4, hangar.Listeners);

        automation.RemoveSequence(sequence);
        automation.Edited();
        runner.Tick(3);
        runner.Tick(4);
        Assert.Equal(0, hangar.Listeners);
    }
}
