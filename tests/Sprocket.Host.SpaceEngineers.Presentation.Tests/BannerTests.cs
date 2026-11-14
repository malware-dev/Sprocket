using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel.Layout;
using Lumel.Input;
using Lumel.Viewing;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The banner: slid in over a set number of the game's frames, opened for the rest, and slid out before it goes.</summary>
public class BannerTests
{
    const float Wide = 800f;
    const float High = 400f;

    const int Frames = Reveal.OpenTicks;

    /// <summary>A notice arrives by sliding in, part of the way after one frame and the whole way after a slide's worth.</summary>
    [Fact]
    public void ANoticeSlidesIn()
    {
        var banner = new Banner();
        var easel = Easel(banner);
        var notice = Said();

        At(easel, banner, notice, 0);
        At(easel, banner, notice, 1);
        Assert.InRange(banner.Progress, 0.01f, 0.99f);

        for (var tick = 2; tick <= Frames; tick++)
            At(easel, banner, notice, tick);

        Assert.Equal(1f, banner.Progress);
    }

    /// <summary>The slide takes the same time however the frames are handed over, one at a time or all at once.</summary>
    [Fact]
    public void TheSlideKeepsTimeWithTheGame()
    {
        var banner = new Banner();
        var easel = Easel(banner);
        var notice = Said();

        At(easel, banner, notice, 0);
        At(easel, banner, notice, Frames);

        Assert.Equal(1f, banner.Progress);
    }

    /// <summary>A notice leaving slides out, and says it is closed only once it is out of sight.</summary>
    [Fact]
    public void ALeavingNoticeSlidesOutBeforeItCloses()
    {
        var banner = new Banner();
        var easel = Easel(banner);
        var notice = Said();
        At(easel, banner, notice, 0);
        At(easel, banner, notice, Frames);

        notice.Leaving = true;
        At(easel, banner, notice, Frames + 1);
        At(easel, banner, notice, Frames + 2);
        Assert.False(notice.Closed);
        Assert.True(banner.Progress > 0f);

        At(easel, banner, notice, Frames * 3);
        banner.Show(notice);
        Assert.True(banner.IsClosed);
        Assert.True(notice.Closed);
    }

    /// <summary>With nothing to say and nothing left to slide, it leaves the layout altogether; while it slides, it stays.</summary>
    [Fact]
    public void ItLeavesTheLayoutOnlyOnceShut()
    {
        var banner = new Banner();
        var easel = Easel(banner);
        var notice = Said();
        At(easel, banner, notice, 0);
        At(easel, banner, notice, Frames);

        At(easel, banner, null, Frames + 1);
        Assert.True(banner.IsVisible);

        At(easel, banner, null, Frames * 3);
        banner.Show(null);
        Assert.False(banner.IsVisible);
    }

    /// <summary>Opened, it is taller: the rest of what it says slides down under its line.</summary>
    [Fact]
    public void OpeningItShowsTheRest()
    {
        var banner = new Banner();
        var notice = Said();
        notice.Detail = "What happened, in more words than fit on the line.";
        notice.Expands = new RelayCommand(() => notice.Expanded = !notice.Expanded);

        var easel = Easel(banner);
        At(easel, banner, notice, 0);
        At(easel, banner, notice, Frames);
        var shut = banner.DesiredSize.Y;

        notice.Expanded = true;
        At(easel, banner, notice, Frames + 1);
        At(easel, banner, notice, Frames * 3);
        easel.Draw(Wide, High);

        Assert.True(banner.DesiredSize.Y > shut);
    }

    /// <summary>Once it has arrived, showing the same notice again measures nothing.</summary>
    [Fact]
    public void SettledItMeasuresNothing()
    {
        var banner = new Banner();
        var notice = Said();
        var easel = Easel(banner);
        At(easel, banner, notice, 0);
        At(easel, banner, notice, Frames);
        At(easel, banner, notice, Frames + 1);
        var settled = easel.Measured;

        for (var tick = Frames + 2; tick < Frames * 4; tick++)
            At(easel, banner, notice, tick);

        Assert.Equal(settled, easel.Measured);
    }

    static Easel Easel(Banner banner) => new Easel(new VStack { Children = { banner } }, Wide, High);

    // The editor's order each frame: the banner is shown what the editor says, then drawn at the frame's tick.
    static void At(Easel easel, Banner banner, Notice? notice, int tick)
    {
        easel.Now = tick;
        banner.Show(notice);
        easel.Draw(Wide, High);
    }

    static Notice Said() => new Notice { Text = "Something is wrong.", Level = NoticeLevel.Problem };
}
