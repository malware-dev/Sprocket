using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The programmable block keeping the automation given away, the status panel shows the automation running as before.</summary>
public class GivenAwayKeeperStatusTests
{
    const long Anna = 144115188075855873;
    const long Pirates = 144115188075855896;


    /// <summary>The last programmable block given away, the status shows nothing wrong.</summary>
    [Fact]
    public void GivingTheLastProgrammableBlockAwayShowsNothingWrong()
    {
        Simulation rig = Wired();
        var source = new StatusSource();
        Look(rig, source);

        rig.Machine.Sprocket.Owner = Pirates;
        rig.Tick();
        rig.Tick();
        Look(rig, source);

        Assert.True(rig.Runner.Live);
        Assert.Empty(source.Status.Items);
        Assert.Equal(1, source.Status.Normal);
    }

    static void Look(Simulation rig, StatusSource source)
    {
        source.Look(new List<SprocketUnit> { rig.Unit }, "Test Rig", rig.Now);
    }

    /// <summary>A switched-on logic system whose button opens the inner door, on a construct Anna owns.</summary>
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

        rig.Machine.Own(Anna);
        rig.Tick();
        rig.Tick();
        return rig;
    }
}
