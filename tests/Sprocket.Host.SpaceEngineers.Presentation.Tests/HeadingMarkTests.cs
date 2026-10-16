using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Sim.Machine;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Every heading is led by the mark of what it names, the mark saying under the pointer what kind of thing that is.</summary>
public class HeadingMarkTests
{
    /// <summary>A logic system's heading is its mark and its name.</summary>
    [Fact]
    public void ALogicSystemsHeadingIsItsMarkAndName()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        sim.Tick();
        sim.Editor.PressRow(0);
        sim.Tick();

        var heading = sim.Editor.Screen.Heading;
        Assert.Equal(Symbols.LogicSystem, heading[0].Mark);
        Assert.Equal("Logic system", heading[0].MarkAbout);
        Assert.Equal("Airlock", heading[0].Text);
    }

    /// <summary>The installation panel's heading is the installation's mark and name, of the logic system's mark and name.</summary>
    [Fact]
    public void TheInstallationPanelNamesBothByMark()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        sim.Tick();
        sim.Editor.AddInstallation(0);
        sim.Tick();

        var heading = sim.Editor.Screen.Heading;
        Assert.Equal(Symbols.Installation, heading[0].Mark);
        Assert.Equal("Installation", heading[0].MarkAbout);
        Assert.Equal(Symbols.LogicSystem, heading[1].Mark);
        Assert.Equal("Airlock", heading[1].Text);
    }

    /// <summary>A sequence's mark says it is a sequence.</summary>
    [Fact]
    public void ASequencesMarkSaysItIsASequence()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Tick();

        var heading = sim.Editor.Screen.Heading;
        Assert.Equal(Symbols.Sequence, heading[0].Mark);
        Assert.Equal("Sequence", heading[0].MarkAbout);
    }
}
