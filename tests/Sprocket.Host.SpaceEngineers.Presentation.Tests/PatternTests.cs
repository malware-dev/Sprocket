using Lumel;
using Lumel.Drawing;
using Lumel.Layout;
using Lumel.Styling;
using VRage.Utils;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A pattern: one picture laid edge to edge across an element's box over its background, its last tiles cut to fit, and gone with the style that laid it.</summary>
public class PatternTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 1000f, 800f);

    static readonly MyStringId Stripes = MyStringId.GetOrCompute("Stripes");

    /// <summary>A box of 2.5 tiles across and one down takes three sprites, the last one half as wide, none past the box.</summary>
    [Fact]
    public void APatternTilesItsBoxAndCutsTheLastTile()
    {
        var graphics = new Recording(Area);
        new Pattern(Stripes, 64, 64f, Color.White).Draw(graphics, new RectangleF(10f, 20f, 160f, 40f), new Vector2(10f, 20f));

        var tiles = graphics.Pieces.Where(piece => piece.Material == Stripes).Select(piece => piece.Destination).ToList();
        Assert.Equal(3, tiles.Count);
        Assert.Equal(new[] { 10f, 74f, 138f }, tiles.Select(t => t.X));
        Assert.Equal(32f, tiles[2].Width, 1);
        Assert.All(tiles, t => Assert.Equal(40f, t.Height, 1));
    }

    /// <summary>Two boxes laid from one origin carry one grid: the lower one's tiles are cut where the grid crosses it, so its stripes run on from the upper one's.</summary>
    [Fact]
    public void BoxesLaidFromOneOriginShareItsGrid()
    {
        var graphics = new Recording(Area);
        var pattern = new Pattern(Stripes, 64, 64f, Color.White);
        pattern.Draw(graphics, new RectangleF(0f, 0f, 64f, 40f), Vector2.Zero);
        pattern.Draw(graphics, new RectangleF(0f, 40f, 64f, 40f), Vector2.Zero);

        var lower = graphics.Pieces.Where(piece => piece.Material == Stripes).Select(piece => piece.Destination).Skip(1).ToList();
        Assert.Equal(new[] { 40f, 64f }, lower.Select(t => t.Y));
        Assert.Equal(new[] { 24f, 16f }, lower.Select(t => t.Height));
    }

    /// <summary>On a text panel, which draws every picture whole, the pattern lays whole tiles inside a clip of its box rather than drawing part of one.</summary>
    [Fact]
    public void OnAPanelThePatternClipsWholeTiles()
    {
        var graphics = new Recording(Area) { DrawsParts = false };
        new Pattern(Stripes, 64, 64f, Color.White).Draw(graphics, new RectangleF(10f, 20f, 160f, 40f), new Vector2(10f, 20f));

        var tiles = graphics.Pieces.Where(piece => piece.Material == Stripes).Select(piece => piece.Destination).ToList();
        Assert.Equal(3, tiles.Count);
        Assert.All(tiles, t => Assert.Equal(64f, t.Width, 1));
        Assert.Equal(1, graphics.Clips);
    }

    /// <summary>A switched-off logic system's row on the home screen lies on the warning's hazard stripes, and once switched on on none.</summary>
    [Fact]
    public void ASwitchedOffLogicSystemLiesOnStripes()
    {
        var screen = Screen.On(Sim.Machine.Rig.Demo());
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);
        editor.GoToRoot();
        screen.Run(70);

        var row = editor.RootRows()[0];
        Assert.False(editor.Program.Systems[0].Enabled);
        Assert.Same(Features.Look.Styles.NoticeStripes, row.Pattern);

        row.IsHot = true;
        Assert.Same(Features.Look.Styles.NoticeStripesHot, row.Pattern);
        Assert.Equal(Features.Look.Palette.NoticeWashHot, row.Background);
        row.IsHot = false;
        Assert.Same(Features.Look.Styles.NoticeStripes, row.Pattern);

        editor.SwitchSystem(editor.Program.Systems[0], true);
        screen.Run(70);
        Assert.Null(editor.RootRows()[0].Pattern);
    }

    /// <summary>A switched-off logic system's screen has the warning's stripes across its heading, and once switched on none.</summary>
    [Fact]
    public void ASwitchedOffLogicSystemsHeadingLiesOnStripes()
    {
        var screen = Screen.On(Sim.Machine.Rig.Demo());
        var editor = screen.Editor;
        editor.NewButtonSystem("Airlock");
        screen.Run(2);

        Assert.False(editor.Program.Systems[0].Enabled);
        Assert.Contains(screen.Drawings, d => !d.IsText && d.Ink == Features.Look.Palette.NoticeStripe && d.Where.Y < 100);

        editor.SwitchOn();
        screen.Run(2);
        Assert.DoesNotContain(screen.Drawings, d => !d.IsText && d.Ink == Features.Look.Palette.NoticeStripe && d.Where.Y < 100);
    }

    /// <summary>A style laying a pattern draws it over the element's background, and a style laying none takes it away.</summary>
    [Fact]
    public void AStyleLaysItsPatternAndTheNextTakesItAway()
    {
        var pattern = new Pattern(Stripes, 64, 64f, Color.White);
        var striped = new ElementStyle { Base = { Background = Color.Black, Pattern = pattern } };
        var plain = new ElementStyle { Base = { Background = Color.Gray } };
        var element = new VStack { Style = striped, Placement = FixedSize.Size(100f, 40f), Align = Align.Left | Align.Top };
        var root = new VStack { Children = { element } };
        root.Measure(new Recording(Area), new Vector2(Area.Width, Area.Height));
        root.Arrange(Area);

        var graphics = new Recording(Area);
        root.Draw(graphics);
        Assert.Same(pattern, element.Pattern);
        Assert.Contains(graphics.Pieces, piece => piece.Material == Stripes);

        element.Style = plain;
        graphics = new Recording(Area);
        root.Draw(graphics);
        Assert.Null(element.Pattern);
        Assert.DoesNotContain(graphics.Pieces, piece => piece.Material == Stripes);
    }
}
