using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>One content sliding in from the right as another slides out to the left, moved as it is drawn and laid out once.</summary>
public class SlideTests
{
    /// <summary>Partway through, the leaving content is drawn that far to the left and the coming one the rest of the way to the right.</summary>
    [Fact]
    public void PartwayEachIsDrawnItsShareOfTheWayAcross()
    {
        var leaving = new Label { Text = "leaving" };
        var coming = new Label { Text = "coming" };
        var slide = new Slide { Leaving = leaving, Coming = coming, Placement = FixedSize.Size(200f, 40f), Progress = 0.25f };
        var easel = new Easel(new HStack { Children = { slide } }, 400f, 200f);
        var x = slide.Bounds.X * easel.Pixels;

        Assert.Equal(x - 50f * easel.Pixels, easel.Written.Single(run => run.Text == "leaving").Position.X, 2);
        Assert.Equal(x + 150f * easel.Pixels, easel.Written.Single(run => run.Text == "coming").Position.X, 2);
    }

    /// <summary>Travel sets how far apart the two are drawn in place of the slide's width.</summary>
    [Fact]
    public void TravelSetsHowFarApartTheyAre()
    {
        var leaving = new Label { Text = "leaving" };
        var coming = new Label { Text = "coming" };
        var slide = new Slide { Leaving = leaving, Coming = coming, Placement = FixedSize.Size(200f, 40f), Progress = 0.5f, Travel = 300f };
        var easel = new Easel(new HStack { Children = { slide } }, 400f, 200f);
        var x = slide.Bounds.X * easel.Pixels;

        Assert.Equal(x - 150f * easel.Pixels, easel.Written.Single(run => run.Text == "leaving").Position.X, 2);
        Assert.Equal(x + 150f * easel.Pixels, easel.Written.Single(run => run.Text == "coming").Position.X, 2);
    }

    /// <summary>Once done, only the coming content is drawn, in place, and nothing is laid out again on the way.</summary>
    [Fact]
    public void DoneOnlyTheComingContentIsDrawnInPlace()
    {
        var slide = new Slide { Leaving = new Label { Text = "leaving" }, Coming = new Label { Text = "coming" }, Progress = 0f };
        var easel = new Easel(new HStack { Children = { slide } }, 400f, 200f);
        var measured = easel.Measured;
        Assert.Equal(new[] { "leaving", "coming" }, easel.Written.Select(run => run.Text).ToArray());

        slide.Progress = 1f;
        easel.Draw(400f, 200f);

        Assert.Equal(new[] { "coming" }, easel.Written.Select(run => run.Text).ToArray());
        Assert.Equal(slide.Bounds.X * easel.Pixels, easel.Written.Single().Position.X, 2);
        Assert.Equal(measured, easel.Measured);
    }

    /// <summary>A press reaches the coming content only once it is in place.</summary>
    [Fact]
    public void APressReachesTheComingContentOnlyInPlace()
    {
        var coming = new Button { Placement = FixedSize.Size(200f, 40f), Command = new Relay(() => { }) };
        var slide = new Slide { Leaving = new Label { Text = "leaving" }, Coming = coming, Progress = 0.5f };
        _ = new Easel(new HStack { Children = { slide } }, 400f, 200f);
        var middle = new Vector2(slide.Bounds.X + 10f, slide.Bounds.Y + 10f);

        Assert.Null(slide.Under(middle));

        slide.Progress = 1f;
        Assert.Same(coming, slide.Under(middle));
    }
}
