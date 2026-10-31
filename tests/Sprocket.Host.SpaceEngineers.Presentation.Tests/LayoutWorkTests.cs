using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What the layout does on a frame where nothing has changed, and on one where only a size has.
/// </summary>
/// <remarks>
/// Measuring text is the dearest thing the layout does and the one no screenshot can show: a layout
/// that re-measures every label every frame draws exactly the right picture. So it is counted.
/// </remarks>
public class LayoutWorkTests
{
    const string Older = "sprocket 1\nblock|entity1|MyObjectBuilder_ButtonPanel/ButtonPanelLarge|i:21,t:B\n";

    [Fact]
    public void AnIdleSecondMeasuresNoText()
    {
        var screen = Screen.Fresh("60", "130", "type", "Airlock", "enter");
        screen.Run(Styles.TipDelay + 5);

        var settled = screen.Measured;
        screen.Run(60);

        Assert.Equal(settled, screen.Measured);
    }

    /// <summary>A box being typed into measures no text while nothing is typed.</summary>
    [Fact]
    public void AnIdleBoxBeingTypedIntoMeasuresNoText()
    {
        var screen = Screen.Fresh("240", "70", "type", "Air");
        screen.Run(5);

        var settled = screen.Measured;
        screen.Run(60);

        Assert.Equal(settled, screen.Measured);
    }

    /// <summary>A number kept against the right of its box measures no text while idle.</summary>
    [Fact]
    public void AnIdleTrailingNumberMeasuresNoText()
    {
        var screen = Screen.Fresh("60", "130", "type", "Airlock", "enter", "60", "161", "200", "351", "560", "333",
            "1353", "311", "70", "193", "200", "476", "800", "676", "1315", "206", "type", "300", "1365", "260");
        screen.Run(120);

        var settled = screen.Measured;
        screen.Run(60);

        Assert.Equal(settled, screen.Measured);
    }

    /// <summary>
    /// A banner sliding in measures no text while it moves.
    /// </summary>
    /// <remarks>
    /// The height of the reveal changes every frame of the slide, which invalidates the measure of
    /// everything between it and the root. What must not follow is the labels being measured again:
    /// their width, scale and font are what they were, and only the room they are given has moved.
    /// </remarks>
    [Fact]
    public void ABannerSlidingInMeasuresNoText()
    {
        var screen = Screen.Fresh();
        screen.Scene.Load(Older);

        // Two frames for the banner's own words to be measured once.
        screen.Run(2);

        var settled = screen.Measured;
        screen.Run(6);

        Assert.Equal(settled, screen.Measured);
    }

    /// <summary>
    /// A banner with nothing left to say slides out and is gone.
    /// </summary>
    /// <remarks>
    /// Drawn rather than driven, because finishing the slide is the drawing's business: the editor
    /// keeps a departing banner in the list until whatever is showing it says it has finished going,
    /// and this is the half that says so.
    /// </remarks>
    [Fact]
    public void ABannerWithNothingLeftToSayGoesAway()
    {
        var screen = Screen.Fresh();
        screen.Scene.Load(Older);
        screen.Run(14);

        Assert.NotNull(Said(screen));

        // Through the question, the way a person resolves it.
        screen.Editor.NewSystem("Airlock");
        screen.Editor.Screen.Answers.First(a => a.Text == "Replace it").Command!.Execute();
        screen.Run(20);

        // Finished going, and nothing of it on the screen. The component may sit in the list at
        // no height until the next rebuild, which costs an early-out and draws nothing.
        Assert.True(Said(screen).Closed);
        Assert.DoesNotContain("cannot read", screen.Dump);
    }

    static Notice Said(Screen screen) =>
        screen.Editor.Notice;
}
