using Lumel.Sim;
using Sprocket.Sim.Measuring;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>What the frame's text checks find: a run cut by the side of its clip, and two runs drawn over each other.</summary>
public class TextFitCheckTests
{
    /// <summary>A run that ends past the right of its clip is cut, and one that fits is not.</summary>
    [Fact]
    public void ARunPastItsClipIsCut()
    {
        var commands = new List<DrawCommand>
        {
            new DrawCommand { Kind = DrawKind.SetClip, Clip = new Rectangle(0, 0, 100, 40) },
            Run("fits", 0, 0),
            Run("runs past the side", 0, 20)
        };

        var cut = TextFit.Cut(commands, new TenPixels(), 1000, 1000);

        Assert.Equal(new[] { "\"runs past the side\" runs 80 px past its clip" }, cut);
    }

    /// <summary>A run scrolled out of its clip, starting outside it, is not cut.</summary>
    [Fact]
    public void ARunStartingOutsideItsClipIsNotCut()
    {
        var commands = new List<DrawCommand>
        {
            new DrawCommand { Kind = DrawKind.SetClip, Clip = new Rectangle(0, 0, 100, 40) },
            Run("scrolled away to the right", 200, 0)
        };

        Assert.Empty(TextFit.Cut(commands, new TenPixels(), 1000, 1000));
    }

    /// <summary>Two runs drawn over each other are found, and two side by side are not.</summary>
    [Fact]
    public void RunsOverEachOtherAreFound()
    {
        var commands = new List<DrawCommand> { Run("left", 0, 0), Run("beside", 40, 0), Run("over it", 50, 5) };

        Assert.Equal(new[] { "\"beside\" over \"over it\"" }, TextFit.Overlapping(commands, new TenPixels(), 1000, 1000));
    }

    /// <summary>A run painted over by a solid box drawn after it is hidden, so text drawn on that box is over nothing.</summary>
    [Fact]
    public void ARunPaintedOverIsHidden()
    {
        var commands = new List<DrawCommand>
        {
            Run("under the page", 0, 0),
            new DrawCommand { Kind = DrawKind.Sprite, Destination = new Rectangle(0, 0, 500, 100), Color = Color.Black },
            Run("on the page", 0, 0)
        };

        Assert.Empty(TextFit.Overlapping(commands, new TenPixels(), 1000, 1000));
    }

    static DrawCommand Run(string text, float x, float y)
    {
        return new DrawCommand { Kind = DrawKind.Text, Text = text, Font = "White", Scale = 1f, Position = new Vector2(x, y) };
    }

    /// <summary>A face whose every letter is ten pixels wide and whose line is ten pixels tall.</summary>
    sealed class TenPixels : IFontMetrics
    {
        public Vector2 MeasurePixels(string? font, string? text, float scale) => new Vector2((text?.Length ?? 0) * 10f, 10f);

        public float LineHeightPixels(string? font, float scale) => 10f;

        public int LineUnits(string? font) => 10;
    }
}
