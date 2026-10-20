using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Layout;
using Lumel.Text;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Pages under a strip of headers: only the page of the current tab is laid out and drawn, and its header takes no press.</summary>
public class TabViewTests
{
    static TabView Two(out Label first, out Label second)
    {
        return new TabView
        {
            Tabs =
            {
                new Tab { Header = "One", Content = new Label { Text = "first page" }.As(out first) },
                new Tab { Header = "Two", Content = new Label { Text = "second page" }.As(out second) }
            }
        };
    }

    /// <summary>The first tab's page shows, and only it: the other is out of the view, so it is neither laid out nor drawn.</summary>
    [Fact]
    public void OnlyTheCurrentPageIsInTheView()
    {
        var tabs = Two(out var first, out var second);
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);

        var written = easel.Written.Select(run => run.Text).ToArray();
        Assert.Contains("first page", written);
        Assert.DoesNotContain("second page", written);
        Assert.Same(tabs, first.Parent);
        Assert.Null(second.Parent);
    }

    /// <summary>Pressing another header shows its page in place of the one there, says so, and leaves the pressed header with no press of its own.</summary>
    [Fact]
    public void PressingAHeaderShowsItsPage()
    {
        var tabs = Two(out _, out _);
        var switched = -1;
        tabs.Switched += at => switched = at;
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);
        Assert.Null(tabs.Tabs[0].Command);

        tabs.Tabs[1].Command!.Execute();
        easel.Draw(400f, 200f);

        var written = easel.Written.Select(run => run.Text).ToArray();
        Assert.Equal(1, switched);
        Assert.Contains("second page", written);
        Assert.DoesNotContain("first page", written);
        Assert.True(tabs.Tabs[1].IsCurrent);
        Assert.False(tabs.Tabs[0].IsCurrent);
        Assert.Null(tabs.Tabs[1].Command);
        Assert.NotNull(tabs.Tabs[0].Command);
    }

    /// <summary>A marker hidden when the header was laid out is given its room once it is shown, on the header of the page shown as on any other.</summary>
    [Fact]
    public void AMarkerShownLaterIsGivenItsRoom()
    {
        var mark = new Glyph { Symbol = "!", IsVisible = false };
        var tabs = new TabView { Tabs = { new Tab { Header = "Sequences", Marker = mark, Content = new Label { Text = "page" } } } };
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);
        Assert.True(tabs.Tabs[0].IsCurrent);

        mark.IsVisible = true;
        easel.Draw(400f, 200f);

        var words = easel.Written.Single(run => run.Text == "Sequences");
        Assert.True(mark.Bounds.Width > 0f, "the marker was given no room");
        Assert.True(mark.Bounds.X * easel.Pixels >= words.Position.X + easel.WidthOf("Sequences") - 0.5f);
    }

    /// <summary>A header's marker is drawn after its words, and leaves the header's words alone while it is hidden.</summary>
    [Fact]
    public void AMarkerIsDrawnAfterTheWords()
    {
        var mark = new Glyph { Symbol = "!" };
        var tabs = new TabView { Tabs = { new Tab { Header = "Sequences", Marker = mark, Content = new Label { Text = "page" } } } };
        var easel = new Easel(new VStack { Children = { tabs } }, 400f, 200f);

        var words = easel.Written.Single(run => run.Text == "Sequences");
        Assert.True(mark.Bounds.X * easel.Pixels >= words.Position.X + easel.WidthOf("Sequences") - 0.5f);

        mark.IsVisible = false;
        easel.Draw(400f, 200f);
        Assert.Contains(easel.Written, run => run.Text == "Sequences");
        Assert.DoesNotContain(easel.Written, run => run.Text == "!");
    }
}
