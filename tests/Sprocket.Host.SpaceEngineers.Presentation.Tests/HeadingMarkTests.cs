using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
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
        Assert.Equal(sim.Texts.Codex.TermLogicSystem, heading[0].MarkAbout);
        Assert.Equal("Airlock", heading[0].Text);
    }

    /// <summary>The installation panel's heading is a path: the logic system's mark and name, then the installation's.</summary>
    [Fact]
    public void TheInstallationPanelReadsAsAPath()
    {
        var sim = Simulation.Fresh();
        sim.Editor.NewButtonSystem("Airlock");
        sim.Editor.GoToRoot();
        sim.Tick();
        sim.Editor.AddInstallation(0);
        sim.Tick();

        var heading = sim.Editor.Screen.Heading;
        Assert.Equal(Symbols.LogicSystem, heading[0].Mark);
        Assert.Equal("Airlock", heading[0].Text);
        Assert.NotNull(heading[0].Opens);
        Assert.Equal("", heading[0].Lead);
        Assert.Equal(Symbols.Installation, heading[1].Mark);
        Assert.Equal(sim.Texts.Codex.TermInstallation, heading[1].MarkAbout);
        Assert.True(heading[1].CanRename);
        Assert.Equal("", heading[1].Lead);
    }

    /// <summary>A sequence opened from its logic system draws the sequence's mark in its heading, not the logic system's.</summary>
    /// <remarks>The two headings have the same shape, so the heading keeps its parts from one screen to the next.</remarks>
    [Fact]
    public void ASequenceOpenedFromItsLogicSystemDrawsItsOwnMark()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        screen.Run(2);
        screen.Editor.GoToRoot();
        screen.Run(2);
        screen.Editor.PressRow(0);
        screen.Run(2);
        Assert.Equal(Symbols.LogicSystem, HeadingMark(screen));

        screen.Editor.PressRow(0);
        screen.Run(2);

        Assert.IsType<SequencePage>(screen.Editor.Current);
        Assert.Equal(Symbols.Sequence, HeadingMark(screen));
    }

    /// <summary>The kind mark drawn highest on the screen, the heading's, which is above every row.</summary>
    static string HeadingMark(Screen screen)
    {
        var kinds = new[] { Symbols.LogicSystem, Symbols.Sequence, Symbols.Installation };
        return screen.Drawings.Where(drawn => drawn.IsSymbol && kinds.Contains(drawn.Text)).OrderBy(drawn => drawn.At.Y).First().Text;
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
        Assert.Equal(sim.Texts.Codex.TermSequence, heading[0].MarkAbout);
    }
}
