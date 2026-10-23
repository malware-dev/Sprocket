using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A codex tip: opened by resting on an element, standing edge to edge with it on the side with room, kept while the pointer
/// crosses onto it, gone between pages through its links, and closed by leaving, by a press elsewhere or by a window opening.
/// </summary>
public class CodexTipTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 1000f, 800f);

    const float Wide = 300f;

    static readonly string Long = string.Join(" ", Enumerable.Repeat("words", 800));

    /// <summary>The tip opens once the pointer has rested on its element for the delay, at what the element says.</summary>
    [Fact]
    public void TheTipOpensAfterTheDelay()
    {
        var (_, anchor, tip) = Stage(100f, 100f);
        tip.Delay = 10;
        var page = new Tip("Words.");

        tip.Hover(anchor, page, On(anchor), 0);
        tip.Hover(anchor, page, On(anchor), 9);
        Assert.False(tip.IsOpen, "the tip opened before the delay");

        tip.Hover(anchor, page, On(anchor), 10);
        Assert.True(tip.IsOpen);
        Assert.Same(anchor, tip.Anchor);
        Assert.Same(page, tip.Page);
    }

    /// <summary>With room to the right, the tip stands there, its left edge on the element's right edge and their tops level.</summary>
    [Fact]
    public void TheTipStandsToTheRightWhereThereIsRoom()
    {
        var (_, anchor, tip) = Opened(100f, 100f);

        Assert.Equal(TipSide.Right, tip.Side);
        Assert.Equal(anchor.Bounds.X + anchor.Bounds.Width, tip.Bounds.X, 1);
        Assert.Equal(anchor.Bounds.Y, tip.Bounds.Y, 1);
        Assert.Equal(Wide, tip.Bounds.Width, 1);
    }

    /// <summary>Without room to the right the tip stands to the left; without room either side, over; and without room over, under.</summary>
    [Theory]
    [InlineData(800f, 100f, 100f, TipSide.Left)]
    [InlineData(0f, 600f, 1000f, TipSide.Above)]
    [InlineData(0f, 0f, 1000f, TipSide.Below)]
    public void TheTipTakesTheFirstSideWithRoom(float x, float y, float width, TipSide side)
    {
        var (_, anchor, tip) = Opened(x, y, width);

        Assert.Equal(side, tip.Side);
        switch (side)
        {
            case TipSide.Left:
                Assert.Equal(anchor.Bounds.X, tip.Bounds.X + tip.Bounds.Width, 1);
                break;
            case TipSide.Above:
                Assert.Equal(anchor.Bounds.Y, tip.Bounds.Y + tip.Bounds.Height, 1);
                break;
            default:
                Assert.Equal(anchor.Bounds.Y + anchor.Bounds.Height, tip.Bounds.Y, 1);
                break;
        }
    }

    /// <summary>The side the tip took when it opened is kept while its pages change, though the page shown later would fit elsewhere.</summary>
    [Fact]
    public void TheSideIsKeptWhileThePagesChange()
    {
        var (_, _, tip) = Opened(0f, 600f, 1000f);
        Assert.Equal(TipSide.Above, tip.Side);

        tip.Go(new Tip(Long));
        tip.Draw(new Recording(Area), Area);

        Assert.Equal(TipSide.Above, tip.Side);
    }

    /// <summary>The tip never grows taller than its cap, however long its page: it is cut off there rather than scrolled.</summary>
    [Fact]
    public void ATipIsNeverTallerThanItsCap()
    {
        var (_, anchor, tip) = Stage(100f, 100f);
        tip.MaxHeight = 120f;

        tip.Open(anchor, new Tip(Long));
        tip.Draw(new Recording(Area), Area);

        Assert.Equal(120f, tip.Bounds.Height, 1);
    }

    /// <summary>
    /// Off both the element and the tip, the tip waits out its grace before closing, and while it waits no other element's tip
    /// takes its place; reaching the tip in time keeps it.
    /// </summary>
    [Fact]
    public void ThePointerHasTimeToCrossOntoTheTip()
    {
        var (anchor, tip, other) = Pair();
        tip.Grace = 15;
        var page = tip.Page;
        var between = new Vector2(tip.Bounds.X + tip.Bounds.Width + 50f, tip.Bounds.Y + tip.Bounds.Height + 50f);

        tip.Hover(null, null, between, 100);
        tip.Hover(other, new Tip("Other."), On(other), 110);
        Assert.Same(anchor, tip.Anchor);

        tip.Hover(null, null, On(tip), 114);
        tip.Hover(null, null, On(tip), 500);
        Assert.Same(page, tip.Page);
        Assert.True(tip.IsOpen, "the tip closed with the pointer on it");
    }

    /// <summary>An element the pointer reaches during the grace opens its tip once it has rested its delay from when it was reached, not from when the grace ran out.</summary>
    [Fact]
    public void AnElementReachedDuringTheGraceRestsFromWhenItWasReached()
    {
        var (_, tip, other) = Pair();
        tip.Grace = 15;
        tip.Delay = 10;
        var page = new Tip("Other.");

        tip.Hover(other, page, On(other), 100);
        tip.Hover(other, page, On(other), 114);
        Assert.NotSame(other, tip.Anchor);

        tip.Hover(other, page, On(other), 115);
        Assert.Same(other, tip.Anchor);
        Assert.Same(page, tip.Page);
    }

    /// <summary>Once the pointer has been off both the element and the tip for the grace, the tip closes.</summary>
    [Fact]
    public void TheTipClosesWhenThePointerHasLeftBoth()
    {
        var (_, tip, _) = Pair();
        tip.Grace = 15;
        var away = new Vector2(990f, 790f);

        tip.Hover(null, null, away, 100);
        tip.Hover(null, null, away, 114);
        Assert.True(tip.IsOpen, "the tip closed before its grace was out");

        tip.Hover(null, null, away, 115);
        Assert.False(tip.IsOpen);
    }

    /// <summary>
    /// A press outside the tip closes it, and its element shows it again only once the pointer has rested on something else; a
    /// press inside it leaves it open.
    /// </summary>
    [Fact]
    public void APressOutsideClosesTheTipAndOneInsideDoesNot()
    {
        var (anchor, tip, other) = Pair();
        var page = tip.Page!;

        tip.Dismiss(On(tip));
        Assert.True(tip.IsOpen, "a press on the tip closed it");

        tip.Dismiss(On(anchor));
        Assert.False(tip.IsOpen, "a press beside the tip left it open");

        tip.Hover(anchor, page, On(anchor), 1);
        Assert.False(tip.IsOpen, "the tip came back over what was just pressed");

        tip.Hover(other, null, On(other), 2);
        tip.Hover(anchor, page, On(anchor), 3);
        Assert.True(tip.IsOpen, "the tip did not come back once the pointer had rested elsewhere");
    }

    /// <summary>Kept by a key, the tip without the key is an ordinary tooltip: the pointer reaches through it, and leaving the element closes it at once.</summary>
    [Fact]
    public void WithoutItsKeyATipIsAnOrdinaryTooltip()
    {
        var (_, tip, _) = Pair();
        tip.KeptByKey = true;
        tip.KeepKey = () => false;
        tip.Grace = 15;

        Assert.False(tip.Covers(On(tip)), "the pointer cannot reach through the tip");
        tip.Hover(null, null, On(tip), 100);
        Assert.False(tip.IsOpen, "the tip stayed with the pointer off its element");
    }

    /// <summary>Held, the key lets the pointer move onto the tip, which it then reaches and which stays.</summary>
    [Fact]
    public void HoldingTheKeyLetsThePointerOntoTheTip()
    {
        var (anchor, tip, _) = Pair();
        tip.KeptByKey = true;
        tip.KeepKey = () => true;
        tip.Grace = 15;

        tip.Hover(anchor, tip.Page, On(anchor), 100);
        tip.Hover(null, null, On(tip), 101);

        Assert.True(tip.IsOpen);
        Assert.True(tip.Covers(On(tip)));
    }

    /// <summary>Letting the key go closes the tip, unless the pointer is on the tip's own element, where it stays as an ordinary tooltip.</summary>
    [Fact]
    public void LettingTheKeyGoClosesTheTipAwayFromItsElement()
    {
        var (anchor, tip, _) = Pair();
        var held = true;
        tip.KeptByKey = true;
        // ReSharper disable once AccessToModifiedClosure
        // The key is pressed and let go by the test setting what the stand-in answers.
        tip.KeepKey = () => held;

        tip.Hover(anchor, tip.Page, On(anchor), 100);
        held = false;
        tip.Hover(anchor, tip.Page, On(anchor), 101);
        Assert.True(tip.IsOpen, "letting go on the element closed the tip");

        held = true;
        tip.Hover(null, null, On(tip), 102);
        held = false;
        tip.Hover(null, null, On(tip), 103);
        Assert.False(tip.IsOpen, "letting go on the tip left it open");
    }

    /// <summary>Anything opening on the layer the tip gives way to closes the tip, so nothing that opens is covered by it.</summary>
    [Fact]
    public void AWindowOpeningClosesTheTip()
    {
        var (_, tip, _) = Pair();
        var layer = new WindowLayer();
        tip.GiveWayTo(layer);

        layer.Open(new Window { Placement = FixedSize.Wide(100f) });

        Assert.False(tip.IsOpen);
    }

    /// <summary>A link leads to the page it names, back and forward step through what was read, and going on drops what was stepped back from.</summary>
    [Fact]
    public void LinksMoveThroughPagesWithBackAndForward()
    {
        var (_, anchor, tip) = Stage(100f, 100f);
        var pages = new Dictionary<string, Tip> { ["b"] = new Tip("Page b."), ["c"] = new Tip("Page c.") };
        tip.Pages = id => pages.TryGetValue(id, out var page) ? page : null;
        var first = new Tip("See [b](b).");
        tip.Open(anchor, first);
        Assert.False(tip.CanGoBack || tip.CanGoForward, "a fresh tip has somewhere to go");

        Assert.True(tip.Follow("b"));
        Assert.Same(pages["b"], tip.Page);
        Assert.True(tip.CanGoBack);

        tip.GoBack();
        Assert.Same(first, tip.Page);
        Assert.True(tip.CanGoForward);

        tip.Follow("c");
        Assert.Same(pages["c"], tip.Page);
        Assert.False(tip.CanGoForward, "going on kept what was stepped back from");
    }

    /// <summary>A page is its document whole, a heading it opens with included, with no title row and no row of steps above it.</summary>
    [Fact]
    public void AFreshTipIsItsPageAlone()
    {
        var (_, anchor, tip) = Stage(100f, 100f);
        tip.BackSymbol = "<";
        tip.ForwardSymbol = ">";
        var page = new Tip("# A heading\n\nA few words about it.");

        tip.Open(anchor, page);
        tip.Draw(new Recording(Area), Area);

        Assert.Equal(Alone(page), tip.Bounds.Height, 1);
    }

    /// <summary>The row of steps back and forward shows above the page once a link was followed, and stays while there is a page to step forward to.</summary>
    [Fact]
    public void TheStepsShowOnceALinkWasFollowed()
    {
        var (_, anchor, tip) = Stage(100f, 100f);
        tip.BackSymbol = "<";
        tip.ForwardSymbol = ">";
        var first = new Tip("See [b](b).");
        var second = new Tip("Page b.");
        tip.Pages = id => id == "b" ? second : null;
        tip.Open(anchor, first);

        tip.Follow("b");
        tip.Draw(new Recording(Area), Area);
        Assert.True(tip.Bounds.Height > Alone(second) + 1f, "the steps did not show after a link was followed");

        tip.GoBack();
        tip.Draw(new Recording(Area), Area);
        Assert.True(tip.Bounds.Height > Alone(first) + 1f, "the steps went with a page still to step forward to");
    }

    /// <summary>A link to a page there is none of goes nowhere: the page shown stays.</summary>
    [Fact]
    public void ALinkToAMissingPageFails()
    {
        var (_, anchor, tip) = Stage(100f, 100f);
        tip.Pages = _ => null;
        var first = new Tip("See [nowhere](nowhere).");
        tip.Open(anchor, first);

        Assert.False(tip.Follow("nowhere"));
        Assert.Same(first, tip.Page);
        Assert.False(tip.CanGoBack);
    }

    /// <summary>The pages read on one element are gone once another element's tip opens: each tip starts on its own page.</summary>
    [Fact]
    public void ANewElementStartsFresh()
    {
        var (_, tip, other) = Pair();
        tip.Pages = _ => new Tip("Elsewhere.");
        tip.Follow("anywhere");
        tip.Close();

        var page = new Tip("Other.");
        tip.Hover(other, page, On(other), 1);

        Assert.Same(other, tip.Anchor);
        Assert.Same(page, tip.Page);
        Assert.False(tip.CanGoBack);
    }

    /// <summary>A tip whose element is taken off the screen closes.</summary>
    [Fact]
    public void ATipClosesWhenItsElementLeavesTheScreen()
    {
        var (root, anchor, tip) = Opened(100f, 100f);

        root.Children.Remove(anchor.Parent!);
        tip.Hover(null, null, new Vector2(990f, 790f), 1);

        Assert.False(tip.IsOpen);
    }

    /// <summary>A screen covered by a layer holding one element at <paramref name="x" />, <paramref name="y" />, laid out, with a closed tip.</summary>
    static (VStack Root, Element Anchor, CodexTip Tip) Stage(float x, float y, float width = 100f)
    {
        var anchor = new VStack { Placement = FixedSize.Size(width, 40f), Align = Align.Left | Align.Top };
        var root = new VStack { Children = { new VStack { Padding = new Thickness(x, y, 0f, 0f), Children = { anchor } } } };
        new WindowLayer().Covers(root);
        root.Measure(new Recording(Area), new Vector2(Area.Width, Area.Height));
        root.Arrange(Area);

        var tip = new CodexTip { Placement = FixedSize.Wide(Wide) };
        return (root, anchor, tip);
    }

    /// <summary>The <see cref="Stage" />, with the tip open on its element and drawn.</summary>
    static (VStack Root, Element Anchor, CodexTip Tip) Opened(float x, float y, float width = 100f)
    {
        var (root, anchor, tip) = Stage(x, y, width);
        tip.Open(anchor, new Tip("A few words about it."));
        tip.Draw(new Recording(Area), Area);
        return (root, anchor, tip);
    }

    /// <summary>Two elements apart on one screen, the tip open and drawn on the first.</summary>
    static (Element Anchor, CodexTip Tip, Element Other) Pair()
    {
        var anchor = new VStack { Placement = FixedSize.Size(100f, 40f), Align = Align.Left | Align.Top };
        var other = new VStack { Placement = FixedSize.Size(100f, 40f), Align = Align.Left | Align.Top };
        var root = new VStack
        {
            Children =
            {
                new VStack { Padding = new Thickness(100f, 100f, 0f, 0f), Children = { anchor } },
                new VStack { Padding = new Thickness(100f, 400f, 0f, 0f), Children = { other } }
            }
        };
        new WindowLayer().Covers(root);
        root.Measure(new Recording(Area), new Vector2(Area.Width, Area.Height));
        root.Arrange(Area);

        var tip = new CodexTip { Placement = FixedSize.Wide(Wide) };
        tip.Open(anchor, new Tip("A few words about it."));
        tip.Draw(new Recording(Area), Area);
        return (anchor, tip, other);
    }

    /// <summary>How tall <paramref name="page" /> stands as rich text alone, at the tip's width.</summary>
    static float Alone(Tip page)
    {
        var text = new RichText { Document = page.Document };
        text.Measure(new Recording(Area), new Vector2(Wide, Area.Height));
        return text.DesiredSize.Y;
    }

    static Vector2 On(Element element) => new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
}
