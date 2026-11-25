using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using Xunit;

namespace Sprocket.Tests;

/// <summary>The bytes a piece of work allocates of its own are counted apart from what the stand-ins for the game's objects allocate as it asks them.</summary>
public class StandInLedgerTests
{
    /// <summary>Questions to a stand-in, with arguments and without, count as no bytes of the work's own.</summary>
    [Fact]
    public void QuestionsToAStandInAreNotTheWorks()
    {
        var ship = new Rigged();
        var door = (IMyDoor)ship.Door(1001, "Door").Block;
        _ = door.Status;
        Action<IMyDoor> heard = _ => { };
        door.OnDoorOpened += heard;
        door.OnDoorOpened -= heard;

        var own = StandInLedger.Own(() =>
        {
            for (var i = 0; i < 100; i++)
            {
                _ = door.Status;
                door.OnDoorOpened += heard;
                door.OnDoorOpened -= heard;
            }
        });

        Assert.Equal(0, own);
    }

    /// <summary>What the work allocates itself is counted in full, beside the questions it asks.</summary>
    [Fact]
    public void TheWorksOwnBytesAreCountedInFull()
    {
        var ship = new Rigged();
        var door = (IMyDoor)ship.Door(1001, "Door").Block;
        var arrays = new object[10];
        _ = door.Status;
        var alone = Allocated(() =>
        {
            for (var i = 0; i < arrays.Length; i++)
                arrays[i] = new byte[100];
        });

        var own = StandInLedger.Own(() =>
        {
            for (var i = 0; i < arrays.Length; i++)
            {
                _ = door.Status;
                arrays[i] = new byte[100];
            }
        });

        Assert.True(alone > 1000, $"{alone} bytes for ten arrays of a hundred");
        Assert.Equal(alone, own);
        Assert.All(arrays, Assert.NotNull);
    }

    static long Allocated(Action work)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        work();
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
