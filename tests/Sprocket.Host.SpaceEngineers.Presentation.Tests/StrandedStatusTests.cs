using Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// Automation stopped because none of the construct's big owners owns its programmable block shows on the status panel as a
/// line of its own, until a big owner owns it again.
/// </summary>
public class StrandedStatusTests
{
    const long Anna = 144115188075855873;
    const long Pirates = 144115188075855896;

    /// <summary>The last programmable block given away, the status says the automation has stopped.</summary>
    [Fact]
    public void GivingTheLastProgrammableBlockAwayShowsTheStop()
    {
        Simulation rig = Wired();
        var source = new StatusSource();
        Look(rig, source);

        rig.Machine.Sprocket.Owner = Pirates;
        rig.Tick();
        rig.Tick();
        Look(rig, source);

        Assert.True(rig.Unit.Stranded);
        Assert.Single(source.Status.Items);
        Assert.Equal(StatusLevel.Stranded, source.Status.Items[0].Level);
        Assert.Equal(0, source.Status.Normal);
    }

    /// <summary>The programmable block given back to a big owner, the line for the stop goes.</summary>
    [Fact]
    public void GivingItBackTakesTheStopAway()
    {
        Simulation rig = Wired();
        var source = new StatusSource();
        rig.Machine.Sprocket.Owner = Pirates;
        rig.Tick();
        rig.Tick();
        Look(rig, source);

        rig.Machine.Sprocket.Owner = Anna;
        rig.Tick();
        rig.Tick();
        Look(rig, source);

        Assert.False(rig.Unit.Stranded);
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
