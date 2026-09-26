using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>The banner: slid in over a set number of the game's frames, opened for the rest, and slid out before it goes.</summary>
public class BannerTests
{
    const float Wide = 800f;
    const float High = 400f;

    const int Frames = 12;

    /// <summary>A notice arrives by sliding in, part of the way after one frame and the whole way after a slide's worth.</summary>
    [Fact]
    public void ANoticeSlidesIn()
    {
        var banner = new Banner();
        var notice = Said();

        banner.Show(notice, 0);
        Assert.InRange(banner.Openness, 0.01f, 0.99f);

        for (var tick = 1; tick <= Frames; tick++)
            banner.Show(notice, tick);

        Assert.Equal(1f, banner.Openness);
    }

    /// <summary>The slide takes the same time however the frames are handed over, one at a time or all at once.</summary>
    [Fact]
    public void TheSlideKeepsTimeWithTheGame()
    {
        var banner = new Banner();
        var notice = Said();

        banner.Show(notice, 0);
        banner.Show(notice, Frames);

        Assert.Equal(1f, banner.Openness);
    }

    /// <summary>A notice leaving slides out, and says it is closed only once it is out of sight.</summary>
    [Fact]
    public void ALeavingNoticeSlidesOutBeforeItCloses()
    {
        var banner = new Banner();
        var notice = Said();
        banner.Show(notice, 0);
        banner.Show(notice, Frames);

        notice.Leaving = true;
        banner.Show(notice, Frames + 1);
        Assert.False(notice.Closed);
        Assert.True(banner.Openness > 0f);

        banner.Show(notice, Frames * 3);
        Assert.True(banner.Shut);
        Assert.True(notice.Closed);
    }

    /// <summary>With nothing to say and nothing left to slide, it leaves the layout altogether; while it slides, it stays.</summary>
    [Fact]
    public void ItLeavesTheLayoutOnlyOnceShut()
    {
        var banner = new Banner();
        var notice = Said();
        banner.Show(notice, 0);
        banner.Show(notice, Frames);

        banner.Show(null, Frames + 1);
        Assert.False(banner.Collapsed);

        banner.Show(null, Frames * 3);
        Assert.True(banner.Collapsed);
    }

    /// <summary>Opened, it is taller: the rest of what it says slides down under its line.</summary>
    [Fact]
    public void OpeningItShowsTheRest()
    {
        var banner = new Banner();
        var notice = Said();
        notice.Detail = "What happened, in more words than fit on the line.";
        notice.Expands = new Relay(delegate { notice.Expanded = !notice.Expanded; });

        var easel = new Easel(new VStack { Children = { banner } }, Wide, High);
        banner.Show(notice, 0);
        banner.Show(notice, Frames);
        easel.Draw(Wide, High);
        var shut = banner.DesiredSize.Y;

        notice.Expanded = true;
        banner.Show(notice, Frames + 1);
        banner.Show(notice, Frames * 3);
        easel.Draw(Wide, High);

        Assert.True(banner.DesiredSize.Y > shut);
    }

    /// <summary>Once it has arrived, showing the same notice again measures nothing.</summary>
    [Fact]
    public void SettledItMeasuresNothing()
    {
        var banner = new Banner();
        var notice = Said();
        var easel = new Easel(new VStack { Children = { banner } }, Wide, High);
        banner.Show(notice, 0);
        banner.Show(notice, Frames);
        easel.Draw(Wide, High);
        var settled = easel.Measured;

        for (var tick = Frames + 1; tick < Frames * 4; tick++)
        {
            banner.Show(notice, tick);
            easel.Draw(Wide, High);
        }

        Assert.Equal(settled, easel.Measured);
    }

    static Notice Said() => new Notice { Text = "Something is wrong.", Level = NoticeLevel.Problem };
}
