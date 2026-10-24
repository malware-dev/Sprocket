using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// The automation is written into its block as it changes, so a blueprint or a copy taken at any moment carries it, and a
/// world save writes only what has not been written yet.
/// </summary>
public class BlockStorageTests
{
    /// <summary>A logic system switched off is in its block on the tick after, with no save between.</summary>
    [Fact]
    public void ASwitchIsWrittenOnTheTickAfter()
    {
        var sim = Airlock();
        var store = Store(sim);
        var written = store.Writes(BlockStorage.StateKey);

        sim.Editor.Program.Systems[0].Enabled = false;
        sim.Tick();

        Assert.Equal(written + 1, store.Writes(BlockStorage.StateKey));
        AssertSwitchedOff(sim);
    }

    /// <summary>A program that does not change is not written, however many times the world saves.</summary>
    [Fact]
    public void NothingChangedIsNotWritten()
    {
        var sim = Airlock();
        var store = Store(sim);
        var program = store.Writes(BlockStorage.ProgramKey);
        var state = store.Writes(BlockStorage.StateKey);

        for (var i = 0; i < 300; i++)
            sim.Tick();
        sim.Save();

        Assert.Equal(program, store.Writes(BlockStorage.ProgramKey));
        Assert.Equal(state, store.Writes(BlockStorage.StateKey));
    }

    /// <summary>A change the world saves before it is written goes into the save all the same.</summary>
    [Fact]
    public void ASaveWritesWhatIsStillWaiting()
    {
        var sim = Airlock();

        sim.Editor.Program.Systems[0].Enabled = false;
        sim.Save();

        AssertSwitchedOff(sim);
    }

    /// <summary>An airlock logic system, switched on and settled, with what it wrote into its block.</summary>
    static Simulation Airlock()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Tick();
        sim.Editor.Program.Systems[0].Enabled = true;
        for (var i = 0; i < 3; i++)
            sim.Tick();

        return sim;
    }

    /// <summary>Asserts that the block keeps the airlock logic system switched off.</summary>
    static void AssertSwitchedOff(Simulation sim)
    {
        var kept = new ProtoBinary().Unpack<BlockState>(BlockStorage.Stored(sim.Machine.Sprocket.Block, BlockStorage.StateKey));
        var off = Assert.Single(kept.Switches);
        Assert.Equal(sim.Editor.Program.Systems[0].Identity, off.System);
        Assert.Equal(0, off.Instance);
    }

    static Store Store(Simulation sim)
    {
        return (Store)sim.Machine.Sprocket.Storage;
    }
}
