using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A construct whose automation waits for a press, with nothing changing and no screen open, costs its world nothing on a tick.</summary>
public class QuietTickTests
{
    /// <summary>Settled, a hundred quiet ticks neither do anything on the rota nor read the construct.</summary>
    [Fact]
    public void AQuietTickDoesNothing()
    {
        var rig = Settled();

        var done = rig.Rota.Done;
        var reads = rig.Unit.World.Reads;
        for (var i = 0; i < 100; i++)
            rig.Tick();

        Assert.Equal(done, rig.Rota.Done);
        Assert.Equal(reads, rig.Unit.World.Reads);
    }

    /// <summary>A block welded on is taken in on the tick after, and the ticks after that are quiet again.</summary>
    [Fact]
    public void AChangeIsTakenInOnTheNextTickAndNoLater()
    {
        var rig = Settled();

        var done = rig.Rota.Done;
        rig.Machine.Door(300, "Hangar Door");
        rig.Tick();
        Assert.True(rig.Rota.Done > done, "the weld was not taken in on the next tick");
        Assert.NotNull(rig.Unit.World.FindEntity(300));

        for (var i = 0; i < 5; i++)
            rig.Tick();

        done = rig.Rota.Done;
        var reads = rig.Unit.World.Reads;
        for (var i = 0; i < 100; i++)
            rig.Tick();

        Assert.Equal(done, rig.Rota.Done);
        Assert.Equal(reads, rig.Unit.World.Reads);
    }

    /// <summary>An airlock that opens its door at a press, switched on, its screen closed, and left to settle.</summary>
    static Simulation Settled()
    {
        var rig = Simulation.Fresh();
        rig.Editor.NewButtonSystem("Airlock");
        rig.Editor.PressAdd("task");
        rig.Editor.PressBlock("Airlock Inner Door");
        rig.Editor.PressChoice("open");
        rig.Editor.PressPrimary();
        rig.Editor.SwitchOn();
        rig.Unit.Showing = false;

        for (var i = 0; i < 10; i++)
            rig.Tick();

        return rig;
    }
}
