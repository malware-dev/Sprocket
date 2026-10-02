using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The automation outlives the programmable block it is kept in, while the construct has another.</summary>
public class HeroTests
{
    /// <summary>The hero ground off, another programmable block keeps the automation, and the button goes on working.</summary>
    [Fact]
    public void GrindingTheHeroMovesTheAutomation()
    {
        Simulation rig = Wired();
        Programmable spare = rig.Machine.Programmable(200, "Spare");
        rig.Tick();

        rig.Machine.Grind(rig.Machine.Sprocket);
        rig.Tick();

        Assert.Same(spare.Block, rig.Unit.Block);
        Assert.False(rig.Unit.Lost);
        Assert.True(SprocketUnit.HasProgram(spare.Block), "the automation was not written into the block that keeps it now");

        // The button named the block that was ground off; it is mended to name the one that keeps the automation now.
        Sequence sequence = rig.Editor.Program.Sequences[0];
        rig.Press("Airlock Button");
        rig.Tick();

        Assert.True(Running(rig, sequence), "the press did not reach the sequence");
    }

    /// <summary>With no programmable block left, the automation is lost.</summary>
    [Fact]
    public void GrindingTheLastProgrammableBlockLosesTheAutomation()
    {
        Simulation rig = Wired();

        rig.Machine.Grind(rig.Machine.Sprocket);
        rig.Tick();

        Assert.True(rig.Unit.Lost);
    }

    /// <summary>A construct whose blocks change leaves a hero that is still there where it is.</summary>
    [Fact]
    public void AnotherBlockChangingKeepsTheHero()
    {
        Simulation rig = Wired();
        var was = rig.Unit.Block;
        rig.Machine.Programmable(200, "Spare");
        rig.Tick();

        rig.Machine.Grind(rig.Machine.Door(300, "Spare Door"));
        rig.Tick();

        Assert.Same(was, rig.Unit.Block);
    }

    /// <summary>An airlock whose button runs a sequence that opens the inner door, switched on.</summary>
    static Simulation Wired()
    {
        Simulation rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Editor.PressAdd("task…");
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Editor.PressChoice("Open");
        rig.Editor.PressPrimary();
        rig.Editor.SwitchOn();

        for (var i = 0; i < 5; i++)
            rig.Tick();

        return rig;
    }

    static bool Running(Simulation rig, Sequence sequence)
    {
        SequenceRunner runner = rig.Runner.Get(sequence, sequence.System.First);
        return runner != null && runner.IsRunning;
    }
}
