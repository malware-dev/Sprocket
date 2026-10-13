using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Tests;

/// <summary>
/// A piston extended, retracted and moved to a position, each waited for until it gets there.
/// </summary>
/// <remarks>
/// Extending and retracting are the game's own, run to whichever limit the piston has, and finished
/// when the piston says it is out or in. Moving to a position is the game's own too, which puts a limit
/// at the position; Sprocket's part is knowing when it has got there.
/// </remarks>
public class PistonTests
{
    [Fact]
    public void WhatMattersAboutAPistonIsSaidInSprocketsWords()
    {
        var ship = new Rigged();
        var piston = ship.Piston(1, "Piston");
        var kind = ship.Entity(piston.Id).Kind;

        foreach (var id in new[] { Blocks.Piston.Extend, Blocks.Piston.Retract, Blocks.Piston.MoveTo })
            Assert.True(kind.FindAction(id, Form.Native)!.Lasts, id + " is not waited for");

        // Extending and retracting are said by Sprocket's own acts, and a move to a position stands in
        // for the game's setter that does the same.
        Assert.True(kind.FindAction("Extend", Form.Action)!.Hidden);
        Assert.True(kind.FindAction("Retract", Form.Action)!.Hidden);
        Assert.True(kind.FindAction("SetAndMove", Form.Action)!.Hidden);

        Assert.Equal("Speed", kind.FindAction("Velocity", Form.Value)!.Named);
        Assert.Equal("Upper limit", kind.FindAction("UpperLimit", Form.Value)!.Named);
        Assert.Equal("Reverse", kind.FindAction("Reverse", Form.Action)!.Named);

        var asked = new List<Field>();
        kind.FindAction(Blocks.Piston.MoveTo, Form.Native)!.Fields(asked);
        Assert.Equal(2, asked.Count);
        Assert.Equal("0", asked[0].Least);
        Assert.Equal("10", asked[0].Most);
    }

    [Fact]
    public void ExtendingIsWaitedForUntilItIsOut()
    {
        var ship = new Rigged();
        var piston = ship.Piston(1, "Piston");
        var lamp = ship.Light(2, "Lamp");

        var frames = Run(ship, piston, lamp, Blocks.Piston.Extend);

        Assert.Equal(10f, piston.Position, 3);

        // Ten metres at half a metre a second.
        Assert.InRange(frames, 1200, 1210);
    }

    [Fact]
    public void RetractingIsWaitedForUntilItIsIn()
    {
        var ship = new Rigged();
        var piston = ship.Piston(1, "Piston");
        var lamp = ship.Light(2, "Lamp");
        piston.Stand(3f);

        var frames = Run(ship, piston, lamp, Blocks.Piston.Retract);

        Assert.Equal(0f, piston.Position, 3);
        Assert.InRange(frames, 360, 370);
    }

    [Fact]
    public void MovingToAPositionIsWaitedForUntilItGetsThere()
    {
        var ship = new Rigged();
        var piston = ship.Piston(1, "Piston");
        var lamp = ship.Light(2, "Lamp");

        var frames = Run(ship, piston, lamp, Blocks.Piston.MoveTo, Literal.Of(4L), Literal.Of(2L));

        Assert.Equal(4f, piston.Position, 3);

        // Four metres at two a second.
        Assert.InRange(frames, 120, 130);
    }

    /// <summary>A piston sent to where it already stands has arrived, rather than waiting for ever.</summary>
    [Fact]
    public void MovingToWhereItStandsArrivesAtOnce()
    {
        var ship = new Rigged();
        var piston = ship.Piston(1, "Piston");
        var lamp = ship.Light(2, "Lamp");
        piston.Stand(4f);

        var frames = Run(ship, piston, lamp, Blocks.Piston.MoveTo, Literal.Of(4L), Literal.Of(2L));

        Assert.Equal(4f, piston.Position, 3);
        Assert.InRange(frames, 0, 3);
    }

    [Fact]
    public void APistonWithNoHeadSaysWhyItIsNotMoving()
    {
        var ship = new Rigged();
        var piston = ship.Piston(1, "Piston");
        piston.Attached = false;
        var entity = ship.Entity(piston.Id);

        ship.World.Invoke(entity, Blocks.Piston.Extend);

        Assert.Equal("cannot move because it has no head", TroubleText.Of(new Texts(), entity.Trouble(Blocks.Piston.Extend, new Literal[0])));
    }

    /// <summary>Runs the one act on the piston, and counts the frames until the step after it runs.</summary>
    static int Run(Rigged ship, Piston piston, Lamp lamp, string actionId, params Literal[] given)
    {
        var automation = new Automation();
        var sequence = automation.AddSequence((Trigger)null!);

        var act = new Act(actionId);
        act.Targets.Add(automation.Declare(ship.World, ship.Entity(piston.Id)).Name);
        foreach (var value in given)
            act.Arguments.Add(value);
        sequence.Steps.Add(act);
        sequence.Steps.Add(new Await());

        var on = new Act(BlockKinds.SwitchOn);
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
