using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Xunit;

namespace Sprocket.Tests;

/// <summary>The construct is the grids mechanically joined to the automation's own, as they are now: a head that floats away leaves it, one that attaches joins it.</summary>
public class ConstructTests
{
    /// <summary>A rotor's head detaching, or its grid deleted whole, takes the blocks on it out of the construct, though none of them was removed or reported removed.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AHeadThatLeavesTakesItsBlocksAway(bool detaches)
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(2, "Arm Rotor");
        ship.Rig.OnHeadOf(rotor);
        ship.Door(3, "Arm Door");
        ship.Rig.OnBase();
        Assert.NotNull(ship.Entity(3));

        if (detaches)
            ship.Rig.Detach(rotor);
        else
            ship.Rig.Delete(rotor.TopGrid);

        Assert.Null(ship.Entity(3));
        Assert.NotNull(ship.Entity(2));
    }

    /// <summary>A head attached after the construct was first read joins it, with what is on it.</summary>
    [Fact]
    public void AHeadThatAttachesBringsItsBlocks()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(2, "Arm Rotor");
        Assert.NotNull(ship.Entity(2));

        ship.Rig.OnHeadOf(rotor);
        ship.Door(3, "Arm Door");
        ship.Rig.OnBase();

        Assert.NotNull(ship.Entity(3));
    }

    /// <summary>A step acting on a door that floated away with its head stops the run there, naming the door.</summary>
    [Fact]
    public void AStepOnABlockThatFloatedAwayStopsTheRunThere()
    {
        var ship = new Rigged();
        var rotor = ship.Rotor(2, "Arm Rotor");
        ship.Rig.OnHeadOf(rotor);
        var door = ship.Door(3, "Arm Door");
        ship.Rig.OnBase();

        var program = new Automation();
        var sequence = program.AddSequence((Trigger)null!);
        var act = new Act("close");
        act.Targets.Add(program.Declare(ship.World, ship.Entity(door.Id)).Name);
        sequence.Steps.Add(Given.Defaults(act));
        sequence.Steps.Add(new Await());
        program.Edited();
        var runner = new AutomationRunner(program, ship.World);
        var frame = 0;
        for (var i = 0; i < 3; i++)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        ship.Rig.Detach(rotor);
        ship.Tick();
        runner.Tick(++frame);
        runner.Call(sequence, sequence.System.First);
        for (var i = 0; i < 3; i++)
        {
            ship.Tick();
            runner.Tick(++frame);
        }

        Assert.DoesNotContain("Arm Door:close", ship.Rig.Log);
        var halt = sequence.System.Halt;
        Assert.NotNull(halt);
        Assert.Equal(SequenceProblem.MissingTarget, halt.Problem);
        Assert.Equal(0, halt.Step);
        Assert.Equal("Arm Door", halt.Block);
    }
}
