using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Xunit;

namespace Sprocket.Tests;

/// <summary>How each signal and each value of Sprocket's own is watched: announced, ruled out while a reading holds, or looked for with a reason.</summary>
public class WatchingTests
{
    /// <summary>
    /// Every signal and every value of Sprocket's own on every kind of block says how it is watched: the block announces it,
    /// something it announces rules it out, a toolbar answers it, or it is looked for and says why nothing better is possible.
    /// </summary>
    [Fact]
    public void EverySignalAndValueSaysHowItIsWatched()
    {
        var ship = new Rigged();
        var blocks = new[]
        {
            ship.Door(1, "Door").Id, ship.Vent(2, "Vent").Id, ship.Rotor(3, "Rotor").Id, ship.Panel(4, "Panel").Id,
            ship.Light(5, "Lamp").Id, ship.Beacon(6, "Beacon").Id, ship.Hinge(7, "Hinge").Id, ship.Piston(8, "Piston").Id,
            ship.Battery(9, "Battery").Id, ship.Tank(10, "Tank").Id, ship.Speaker(11, "Speaker").Id,
            ship.Rig.Spotlight(12, "Spotlight").Id, ship.Rig.Jukebox(13, "Jukebox").Id, ship.Rig.Cockpit(14, "Cockpit").Id,
            ship.Rig.TextPanel(15, "Screen").Id, ship.Rig.Camera(16, "Camera").Id, ship.Rig.EmotionController(17, "Face").Id,
            ship.Rig.Timer(18, "Timer").Id, ship.Rig.Programmable(19, "Script Block").Id, ship.Warhead(20, "Warhead").Id,
            ship.Turret(21, "Turret").Id
        };

        var unsaid = new List<string>();
        foreach (var id in blocks)
        {
            var kind = ship.Entity(id).Kind;
            foreach (var signal in kind.Events)
            {
                if (!signal.Announced && signal.RuledOutBy == null && !signal.Answered && signal.LookedBecause.Length == 0)
                    unsaid.Add(kind.Id + " " + signal.Id);
            }

            foreach (var value in kind.Actions)
            {
                if (value.Being != null && !value.Announced && value.RuledOutBy == null && value.LookedBecause.Length == 0)
                    unsaid.Add(kind.Id + " " + value.Id);
            }
        }

        Assert.Empty(unsaid);
    }

    /// <summary>
    /// How a value is heard is the kind's own: a hangar door announces its status, and a door beside it in the same world is
    /// still looked at only while it is not shut.
    /// </summary>
    [Fact]
    public void EachKindHearsItsValueItsOwnWay()
    {
        var ship = new Rigged();
        var hangar = ship.Entity(ship.HangarDoor(1, "Hangar").Id).Kind.FindAction("status", Nature.Value)!;
        var door = ship.Entity(ship.Door(2, "Door").Id).Kind.FindAction("status", Nature.Value)!;

        Assert.True(hangar.Announced);
        Assert.False(door.Announced);
        Assert.NotNull(door.RuledOutBy);
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

        var on = SignalTrigger.Written("ext", "turns-on");
        on.Sources.Add(automation.Declare(ship.World, ship.Entity(door.Id)).Name);
        var sequence = automation.AddSequence(on);
        var act = Given.Power(true);
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

        var closing = SignalTrigger.Written("ext", "starts-closing");
        closing.Sources.Add(automation.Declare(ship.World, ship.Entity(hangar.Id)).Name);
        var sequence = automation.AddSequence(closing);
        var act = Given.Power(true);
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

        var closing = SignalTrigger.Written("ext", "starts-closing");
        closing.Sources.Add(automation.Declare(ship.World, ship.Entity(hangar.Id)).Name);
        var sequence = automation.AddSequence(closing);
        var act = Given.Power(true);
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
