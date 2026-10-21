using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The automation outlives the programmable block it is kept in, while the construct has another.</summary>
public class HeroTests
{
    const long Anna = 144115188075855873;
    const long Pirates = 144115188075855896;

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
        Assert.True(BlockStorage.HasProgram(spare.Block), "the automation was not written into the block that keeps it now");

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

    /// <summary>The hero given to somebody the big owners share nothing with is out of their reach, and another programmable block keeps the automation.</summary>
    [Fact]
    public void TheHeroOutOfReachMovesTheAutomation()
    {
        Simulation rig = Wired();
        Programmable spare = rig.Machine.Programmable(200, "Spare");
        Owned(rig, Anna);

        rig.Machine.Sprocket.Owner = Pirates;
        rig.Tick();
        rig.Tick();

        Assert.Same(spare.Block, rig.Unit.Block);
        Assert.False(rig.Unit.Lost);
        Assert.False(rig.Editor.Program.Paused);
    }

    /// <summary>The hero given away with no other programmable block a big owner owns, the automation stops in it, kept.</summary>
    [Fact]
    public void TheLastProgrammableBlockGivenAwayStopsTheAutomation()
    {
        Simulation rig = Wired();
        Owned(rig, Anna);

        rig.Machine.Sprocket.Owner = Pirates;
        rig.Tick();
        rig.Tick();

        Assert.False(rig.Unit.Lost);
        Assert.True(rig.Unit.Stranded);
        Assert.False(rig.Runner.Live);
        Assert.True(BlockStorage.HasProgram(rig.Machine.Sprocket.Block), "the stopped automation was not kept");
    }

    /// <summary>The hero given back to a big owner, a stopped automation carries on.</summary>
    [Fact]
    public void TheLastProgrammableBlockGivenBackCarriesOn()
    {
        Simulation rig = Wired();
        Owned(rig, Anna);
        rig.Machine.Sprocket.Owner = Pirates;
        rig.Tick();
        rig.Tick();

        rig.Machine.Sprocket.Owner = Anna;
        rig.Tick();
        rig.Tick();

        Assert.False(rig.Unit.Stranded);
        Assert.True(rig.Runner.Live);
        Assert.False(rig.Editor.Program.Paused);
    }

    /// <summary>The block that kept the automation before it went out of reach holds none of it after a reload.</summary>
    [Fact]
    public void TheHeroOutOfReachKeepsNoAutomation()
    {
        Simulation rig = Simulation.On(Store.Nowhere());
        rig.Editor.NewButtonSystem("Airlock");
        rig.Tick();
        rig.Machine.Programmable(200, "Spare");
        Owned(rig, Anna);
        rig.Machine.Sprocket.Owner = Pirates;
        rig.Tick();
        rig.Tick();

        rig.Reload();
        rig.Tick();

        Assert.Equal(1, rig.Roster.Count);
        Assert.False(BlockStorage.HasProgram(rig.Machine.Sprocket.Block), "the block out of reach still keeps the automation");
    }

    /// <summary>The construct's grid deleted whole, the automation goes with it.</summary>
    [Fact]
    public void DeletingTheGridLosesTheAutomation()
    {
        Simulation rig = Wired();

        rig.Machine.Delete(rig.Machine.Sprocket.Grid);
        rig.Tick();

        Assert.True(rig.Unit.Lost);
    }

    /// <summary>The hero's grid deleted whole, a programmable block on a grid of the construct that is left keeps the automation.</summary>
    [Fact]
    public void DeletingTheHerosGridMovesTheAutomationToAGridThatIsLeft()
    {
        Simulation rig = Wired();
        Rotor rotor = rig.Machine.Rotor(300, "Rotor");
        rig.Machine.OnHeadOf(rotor);
        Programmable spare = rig.Machine.Programmable(400, "Spare");
        rig.Tick();

        rig.Machine.Delete(rig.Machine.Sprocket.Grid);
        rig.Tick();

        Assert.False(rig.Unit.Lost);
        Assert.Same(spare.Block, rig.Unit.Block);
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

    /// <summary>The block that keeps the automation switched off, another programmable block on the construct keeps it running.</summary>
    [Fact]
    public void AnyWorkingProgrammableBlockKeepsTheAutomationRunning()
    {
        Simulation rig = Wired();
        rig.Machine.Programmable(200, "Spare");
        rig.Tick();

        rig.Machine.Sprocket.Enabled = false;
        rig.Tick();

        Assert.True(rig.Runner.Live, "another programmable block works and the automation held");

        Sequence sequence = rig.Editor.Program.Sequences[0];
        rig.Press("Airlock Button");
        rig.Tick();

        Assert.True(Running(rig, sequence), "the press did nothing while a programmable block works");
    }

    /// <summary>Every programmable block on the construct switched off holds the automation, and any one switched on again lets it go.</summary>
    [Fact]
    public void NoWorkingProgrammableBlockHoldsTheAutomation()
    {
        Simulation rig = Wired();
        Programmable spare = rig.Machine.Programmable(200, "Spare");
        rig.Tick();

        rig.Machine.Sprocket.Enabled = false;
        spare.Enabled = false;
        rig.Tick();

        Assert.False(rig.Runner.Live, "no programmable block works and the automation ran");

        spare.Enabled = true;
        rig.Tick();

        Assert.True(rig.Runner.Live, "the spare came back and the automation held");
    }

    /// <summary>A programmable block welded on after the one that keeps it is off lets the automation run.</summary>
    [Fact]
    public void AProgrammableBlockWeldedOnLetsAHeldAutomationRun()
    {
        Simulation rig = Wired();
        rig.Machine.Sprocket.Enabled = false;
        rig.Tick();

        Assert.False(rig.Runner.Live);

        rig.Machine.Programmable(200, "Spare");
        rig.Tick();

        Assert.True(rig.Runner.Live, "the new programmable block works and the automation held");
    }

    /// <summary>The last working programmable block ground off holds the automation kept in one that is off.</summary>
    [Fact]
    public void GrindingTheLastWorkingProgrammableBlockHoldsTheAutomation()
    {
        Simulation rig = Wired();
        Programmable spare = rig.Machine.Programmable(200, "Spare");
        rig.Tick();

        rig.Machine.Sprocket.Enabled = false;
        rig.Tick();
        rig.Machine.Grind(spare);
        rig.Tick();

        Assert.False(rig.Runner.Live, "the only working programmable block is gone and the automation ran");
    }

    /// <summary>An airlock whose button runs a sequence that opens the inner door, switched on.</summary>
    /// <summary>Hands every block on the construct to <paramref name="player" />, shared with nobody, and lets the change land.</summary>
    static void Owned(Simulation rig, long player)
    {
        rig.Machine.Own(player);
        rig.Tick();
        rig.Tick();
    }

    static Simulation Wired()
    {
        Simulation rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Editor.PressAdd("task");
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Editor.PressChoice("open");
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
