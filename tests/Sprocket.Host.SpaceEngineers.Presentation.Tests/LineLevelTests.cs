using Lumel;
using Lumel.Images;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Sim.Machine;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Every line says a remark at one of three levels: a problem in red on a red ground, a warning in amber on an amber one, a note in its own ink.</summary>
public class LineLevelTests
{
    const string Said = "Worth a word.";

    /// <summary>A list row writes a problem red, a warning amber and a note in the ink of its status, with a flag for the first two.</summary>
    [Theory]
    [InlineData(NoticeLevel.Problem)]
    [InlineData(NoticeLevel.Warning)]
    [InlineData(NoticeLevel.Info)]
    public void AListRowSaysEachLevel(NoticeLevel level)
    {
        var row = new StatusRow { Summary = "Sequence 1", Problem = Said, Level = level, Status = "ready" };
        row.Show(Simulation.Fresh().Texts);
        row.Restyle();

        Assert.Equal(Ink(level, Palette.Faint), Words(row).Color);
        var flag = row.Within().OfType<Glyph>().Single(glyph => glyph.Symbol == Symbols.Warning);
        Assert.Equal(level != NoticeLevel.Info, flag.IsVisible);
    }

    /// <summary>A trigger's line writes each level in its ink, on the ground of its level: a problem red and a warning amber, each striped.</summary>
    [Theory]
    [InlineData(NoticeLevel.Problem)]
    [InlineData(NoticeLevel.Warning)]
    [InlineData(NoticeLevel.Info)]
    public void ATriggerLineSaysEachLevel(NoticeLevel level)
    {
        var row = new TriggerRow { Problem = Said, Level = level };
        row.Show(null!, Simulation.Fresh().Texts);
        row.Restyle();

        Assert.Equal(Ink(level, Palette.Faint), Words(row).Color);
        Assert.Equal(RemarkLook.Wash(level, Palette.None), row.Background);
        Assert.Same(RemarkLook.Stripes(level), row.Pattern);
    }

    /// <summary>A slot's line writes each level in its ink.</summary>
    [Theory]
    [InlineData(NoticeLevel.Problem)]
    [InlineData(NoticeLevel.Warning)]
    [InlineData(NoticeLevel.Info)]
    public void ASlotLineSaysEachLevel(NoticeLevel level)
    {
        var row = new PartRow(new Localization.Texts()) { Problem = Said, Level = level };
        row.Restyle();

        Assert.Equal(Ink(level, Palette.Faint), Words(row).Color);
    }

    /// <summary>A sequence's bar writes each level in its ink, and the step it is about is washed red for a problem, amber for a warning and not at all for a note.</summary>
    [Theory]
    [InlineData(NoticeLevel.Problem)]
    [InlineData(NoticeLevel.Warning)]
    [InlineData(NoticeLevel.Info)]
    public void ASequenceSaysEachLevel(NoticeLevel level)
    {
        var bar = new SequenceBar { Problem = Said, Level = level };
        bar.Show(Simulation.Fresh().Texts);
        bar.Restyle();
        Assert.Equal(Ink(level, Palette.Muted), Words(bar).Color);

        var step = new StepRow { HasProblem = true, Level = level };
        step.Show(null!, Simulation.Fresh().Texts);
        step.Restyle();
        var wash = level == NoticeLevel.Problem ? Palette.ProblemWash : level == NoticeLevel.Warning ? Palette.NoticeWash : Palette.None;
        Assert.Equal(wash, step.Background);
    }

    /// <summary>The banner writes a note in plain ink on the panel, with none of the grounds a warning or a problem has.</summary>
    [Fact]
    public void ABannerNoteHasNoWash()
    {
        var banner = new Banner();
        banner.Show(new Notice { Text = Said, Level = NoticeLevel.Info });
        banner.Restyle();

        Assert.Equal(Palette.Ink, Words(banner).Color);
        Assert.DoesNotContain(banner.Within(), element => element.Background == Palette.NoticeWash || element.Background == Palette.ProblemWash);
    }

    static Color Ink(NoticeLevel level, Color plain) =>
        level == NoticeLevel.Problem ? Palette.Problem : level == NoticeLevel.Warning ? Palette.Notice : plain;

    static Label Words(Element element) =>
        element.Within().OfType<Label>().Single(label => label.Text == Said);
}
