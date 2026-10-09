using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A viewbox: its content scaled to fill the room as nearly as it can without spilling out, text flowing anew at each scale.</summary>
public class ViewboxTests
{
    const float Wide = 400f;
    const float High = 400f;

    static readonly string Long = string.Join("\n\n", Enumerable.Repeat("The airlock cycles when the inner door shuts.", 12));

    /// <summary>A document too tall for its room shrinks until it fits, close to the room's height.</summary>
    [Fact]
    public void ATallDocumentShrinksToItsRoom()
    {
        var box = Boxed(Long, 120f, _ => { });

        Assert.True(box.Scale < 1f, $"scaled by {box.Scale}");
        Assert.True(box.DesiredSize.Y <= 120f + 0.01f, $"{box.DesiredSize.Y} is taller than the room");
        Assert.True(box.DesiredSize.Y > 120f * 0.9f, $"{box.DesiredSize.Y} shrank further than it had to");
    }

    /// <summary>A short document grows until it meets the room on one side.</summary>
    [Fact]
    public void AShortDocumentGrowsToFillItsRoom()
    {
        var box = Boxed("Airlock", 300f, _ => { });

        Assert.True(box.Scale > 1f, $"scaled by {box.Scale}");
        Assert.True(box.DesiredSize.X > Wide * 0.9f || box.DesiredSize.Y > 300f * 0.9f, $"{box.DesiredSize} meets neither side");
        Assert.True(box.DesiredSize.X <= Wide + 0.01f && box.DesiredSize.Y <= 300f + 0.01f, $"{box.DesiredSize} spills out");
    }

    /// <summary>Text keeps the lines it was written with: a paragraph wider than the room is one line scaled to its width, never wrapped.</summary>
    [Fact]
    public void AParagraphKeepsItsLine()
    {
        var text = new RichText { Document = Markdown.Parse("The airlock cycles when the inner door shuts and the outer door is sealed.\nNext"), FontSize = 20f };
        var box = new Viewbox { Content = text, Placement = Fixed.Size(Wide, 300f) };
        var easel = new Easel(new VStack { Children = { box } }, Wide, High);

        var first = easel.Written.Where(w => w.Text.Trim().Length > 0 && w.Text.Trim() != "Next").Select(w => w.Position.Y).Distinct();
        Assert.Single(first);
        Assert.True(box.Scale < 1f, $"scaled by {box.Scale}");
        Assert.Equal(Wide, box.DesiredSize.X, 1);
    }

    /// <summary>A table with a starred column, given all the room it could want, is as wide as its cells rather than taking all of it, so it grows to the room like anything else.</summary>
    [Fact]
    public void AStarredTableGrowsLikeAnythingElse()
    {
        var box = Boxed("| Tank | Fill |\n|:--*|--:|\n| Oxygen | 80% |", 300f, _ => { });

        Assert.True(box.Scale > 1f, $"scaled by {box.Scale}");
        Assert.Equal(Wide, box.DesiredSize.X, 1);
    }

    /// <summary>A centered part stays centered in the room, padding and all, where the room's height is what limits the scale.</summary>
    [Fact]
    public void ACenteredPartStaysCentered()
    {
        var text = new RichText { Document = Markdown.Parse("::: center\nAirlock\n:::\n\nThe outer door"), FontSize = 20f };
        var box = new Viewbox { Content = text, Padding = new Thickness(25f), Placement = Fixed.Size(Wide, 120f) };
        var easel = new Easel(new VStack { Children = { box } }, Wide, High);

        var word = Assert.Single(easel.Written, w => w.Text.Trim() == "Airlock");
        var drawnWide = easel.WidthOf("Airlock") * 20f / easel.LineHeight * box.Scale;
        var middle = (box.ContentBounds.X + box.ContentBounds.Width * 0.5f) * easel.Pixels;

        var widest = easel.WidthOf("The outer door") * 20f / easel.LineHeight * box.Scale;
        Assert.True(widest < box.ContentBounds.Width * easel.Pixels - 20f, "the height did not limit the scale");
        Assert.Equal(middle, word.Position.X + drawnWide * 0.5f, 2f);
    }

    /// <summary>A viewbox that only shrinks leaves a document that fits at its own size.</summary>
    [Fact]
    public void OneThatOnlyShrinksKeepsAShortDocumentsSize()
    {
        var box = Boxed("Airlock", 300f, b => b.Grows = false);

        Assert.Equal(1f, box.Scale);
        Assert.Equal(20f, box.DesiredSize.Y);
    }

    /// <summary>A viewbox that only grows leaves a document too tall for it at its own size.</summary>
    [Fact]
    public void OneThatOnlyGrowsKeepsATallDocumentsSize()
    {
        var box = Boxed(Long, 120f, b => b.Shrinks = false);

        Assert.Equal(1f, box.Scale);
        Assert.True(box.DesiredSize.Y > 120f, "the document shrank");
    }

    /// <summary>Content whose size does not change with its width is scaled exactly to meet the nearer side.</summary>
    [Fact]
    public void FixedContentMeetsTheNearerSide()
    {
        var label = new Label { Text = "Airlock", FontSize = 20f };
        var box = new Viewbox { Content = label, Placement = Fixed.Size(Wide, 300f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        Assert.Equal(Wide, box.DesiredSize.X, 2);
        Assert.Equal(20f * box.Scale, box.DesiredSize.Y, 2);
    }

    /// <summary>A point drawn over a link reaches the text and finds the link, however much the viewbox has scaled it.</summary>
    [Fact]
    public void APointReachesTheLinkDrawnUnderIt()
    {
        var text = new RichText { Document = Markdown.Parse("[codex](#codex) and more"), FontSize = 20f, Follow = _ => { } };
        var box = new Viewbox { Content = text, Placement = Fixed.Size(Wide, 300f) };
        _ = new Easel(new VStack { Children = { box } }, Wide, High);

        var origin = new Vector2(box.ContentBounds.X, box.ContentBounds.Y);
        var laid = new Vector2(text.ContentBounds.X + 4f, text.ContentBounds.Y + 10f);
        var drawn = (laid - origin) * box.Scale + origin;

        Assert.True(box.Scale > 1f, $"scaled by {box.Scale}");
        Assert.Same(text, box.Under(drawn));
        Assert.Equal("#codex", text.LinkAt(text.Local(drawn))?.Target);
    }

    /// <summary>The scaled text is drawn across the room, its words grown with it.</summary>
    [Fact]
    public void TheScaledTextIsDrawnAcrossTheRoom()
    {
        var text = new RichText { Document = Markdown.Parse("Airlock"), FontSize = 20f };
        var box = new Viewbox { Content = text, Placement = Fixed.Size(Wide, 300f) };
        var easel = new Easel(new VStack { Children = { box } }, Wide, High);

        var word = Assert.Single(easel.Written, command => command.Text.Trim() == "Airlock");
        Assert.True(box.Scale > 1f, $"scaled by {box.Scale}");
        var drawnWide = easel.WidthOf("Airlock") * 20f / easel.LineHeight * box.Scale;
        Assert.Equal(Wide * easel.Pixels, drawnWide, Wide * easel.Pixels * 0.02f);
        Assert.Equal(box.ContentBounds.X * easel.Pixels, word.Position.X, 1f);
    }

    static Viewbox Boxed(string markdown, float high, Action<Viewbox> set)
    {
        var text = new RichText { Document = Markdown.Parse(markdown), FontSize = 20f, BlockSpacing = 8f };
        var box = new Viewbox { Content = text, Placement = Fixed.Size(Wide, high) };
        set(box);

        _ = new Easel(new VStack { Children = { box } }, Wide, High);
        return box;
    }
}
