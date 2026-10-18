using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Tests;

/// <summary>
/// A run the world remembered keeps what it met through the first check after it is built again, and takes what that
/// check finds wrong with the program, as a server builds a Sprocket again for every change a client sends.
/// </summary>
public class RunFaultsTests
{
    /// <summary>A slot found empty before, filled since, is no longer empty once the program is checked again.</summary>
    [Fact]
    public void ARestoredRunTakesWhatTheFirstCheckFinds()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Tick();
        var system = sim.Editor.Program.Systems[0];
        var sequence = system.Sequences[0];

        var empty = new SequenceFault(SequenceProblem.EmptySlot, 0, BlockTrouble.None, "Interior Light");
        var stopped = new SequenceFault(SequenceProblem.MissingTarget, 1, BlockTrouble.None, "Inner Door");
        var memory = new RunMemory { Found = KeptSpellings.Kept(empty), Met = KeptSpellings.Kept(stopped) };
        var faults = new RunFaults(sequence, system.First!, sim.Runner, memory, true);
        Assert.True(faults.Found.Waits);

        faults.Checked(SequenceFault.None);

        Assert.False(faults.Found.Exists);
        Assert.True(faults.Fault.Same(stopped), "the first check forgot what the run met");
    }

    /// <summary>A later check forgets what the run met, as it always has.</summary>
    [Fact]
    public void ALaterCheckForgetsWhatTheRunMet()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Tick();
        var system = sim.Editor.Program.Systems[0];

        var stopped = new SequenceFault(SequenceProblem.MissingTarget, 1, BlockTrouble.None, "Inner Door");
        var memory = new RunMemory { Met = KeptSpellings.Kept(stopped) };
        var faults = new RunFaults(system.Sequences[0], system.First!, sim.Runner, memory, true);

        faults.Checked(SequenceFault.None);
        faults.Checked(SequenceFault.None);

        Assert.False(faults.Fault.Exists);
    }

    /// <summary>An installation switched off and on forgets every problem, and what is still wrong is found again.</summary>
    [Fact]
    public void SwitchingOnForgetsEveryProblem()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Tick();
        var system = sim.Editor.Program.Systems[0];
        var instance = system.First!;
        system.Enabled = true;
        sim.Tick();
        var faults = sim.Runner.Get(system.Sequences[0], instance)!.Faults;
        var found = faults.Found;

        faults.Met(new SequenceFault(SequenceProblem.MissingTarget, 1, BlockTrouble.None, "Inner Door"));
        faults.Quit = new SequenceFault(SequenceProblem.MissingTarget, 2, BlockTrouble.None, "Outer Door");
        instance.Enabled = false;
        sim.Tick();
        Assert.True(faults.Quit.Exists, "switching off forgot what the run met");

        instance.Enabled = true;
        sim.Tick();

        Assert.False(faults.Quit.Exists);
        Assert.True(faults.Fault.Same(found), "switching on kept a problem it should have forgotten");
    }
}
