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
        new Pattern(Stripes, 64, 64f, Color.White).Draw(graphics, new RectangleF(10f, 20f, 160f, 40f));

        var tiles = graphics.Pieces.Where(piece => piece.Material == Stripes).Select(piece => piece.Destination).ToList();
        Assert.Equal(3, tiles.Count);
        Assert.Equal(new[] { 10f, 74f, 138f }, tiles.Select(t => t.X));
        Assert.Equal(32f, tiles[2].Width, 1);
        Assert.All(tiles, t => Assert.Equal(40f, t.Height, 1));
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

        editor.SwitchSystem(editor.Program.Systems[0], true);
        screen.Run(70);
        Assert.Null(editor.RootRows()[0].Pattern);
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
