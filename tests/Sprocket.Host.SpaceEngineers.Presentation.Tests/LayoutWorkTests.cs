using Sprocket.Host.SpaceEngineers.Screen;
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
    /// <summary>What his test world held: the format from before the script language.</summary>
    const string Older = "sprocket 1\nblock|entity1|MyObjectBuilder_ButtonPanel/ButtonPanelLarge|i:21,t:B\n";

    [Fact]
    public void AnIdleSecondMeasuresNoText()
    {
        var screen = Screen.Fresh("60", "130", "type", "Airlock", "enter");
        screen.Run(5);

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
        screen.Editor.Invoke(screen.Editor.Screen.Answers.First(a => a.Text == "Replace it").Command);
        screen.Run(20);

        // Finished going, and nothing of it on the screen. The component may sit in the list at
        // no height until the next rebuild, which costs an early-out and draws nothing.
        Assert.True(Said(screen).Closed);
        Assert.DoesNotContain("cannot read", screen.Dump);
    }

    static NoticeComponent Said(Screen screen) =>
        screen.Editor.Screen.Components.OfType<NoticeComponent>().FirstOrDefault();
}
