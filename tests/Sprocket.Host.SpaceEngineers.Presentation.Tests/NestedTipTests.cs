using Lumel;
using Lumel.Layout;
using Lumel.Windows;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// A codex tip that nests: resting on a link inside it opens the link's page in a second tip beside it, the first staying
/// open; moving back onto the first closes the one beside it, leaving them all closes all, and no more than three stand at once.
/// </summary>
public class NestedTipTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 1600f, 800f);

    const float Wide = 300f;

    const int Delay = 10;

    const int Grace = 5;

    /// <summary>Resting on a link for the delay opens its page in a tip of its own beside the first, which stays open.</summary>
    [Fact]
    public void ALinkRestedOnOpensItsPageBeside()
    {
        var (tip, pages) = Opened();

        Hover(tip, Link(tip), 0);
        Hover(tip, Link(tip), Delay - 1);
        Assert.Null(tip.Beside);

        Hover(tip, Link(tip), Delay);
        Assert.True(tip.IsOpen);
        Assert.NotNull(tip.Beside);
        Assert.Same(pages["b"], tip.Beside!.Page);

        tip.Draw(new Recording(Area), Area);
        Assert.True(tip.Beside.Bounds.X >= tip.Bounds.X + tip.Bounds.Width - 1f || tip.Beside.Bounds.X + tip.Beside.Bounds.Width <= tip.Bounds.X + 1f,
            "the tip beside stood over the first");
    }

    /// <summary>The pointer crossing onto the tip beside keeps both open, and a press there closes neither.</summary>
    [Fact]
    public void ThePointerOnTheTipBesideKeepsBoth()
    {
        var tip = Beside();

        var on = On(tip.Beside!);
        Hover(tip, on, Delay + 1);
        Hover(tip, on, Delay + 1 + Grace * 3);
        Assert.True(tip.Covers(on));
        tip.Dismiss(on);

        Assert.True(tip.IsOpen);
        Assert.NotNull(tip.Beside);
    }

    /// <summary>Moving back onto the first tip, off its links, closes the one beside it once the grace is over.</summary>
    [Fact]
    public void BackOnTheFirstTheTipBesideCloses()
    {
        var tip = Beside();

        var plain = Plain(tip);
        Hover(tip, plain, Delay + 1);
        Assert.NotNull(tip.Beside);

        Hover(tip, plain, Delay + 1 + Grace);
        Assert.True(tip.IsOpen);
        Assert.Null(tip.Beside);
    }

    /// <summary>Leaving every tip closes them all once the grace is over.</summary>
    [Fact]
    public void LeavingThemAllClosesAll()
    {
        var tip = Beside();

        var away = new Vector2(Area.Width - 5f, Area.Height - 5f);
        Hover(tip, away, Delay + 1);
        Hover(tip, away, Delay + 1 + Grace);

        Assert.False(tip.IsOpen);
        Assert.Null(tip.Beside);
    }

    /// <summary>No more than three tips stand side by side: the third's links open nothing.</summary>
    [Fact]
    public void NoMoreThanThreeStandAtOnce()
    {
        var tip = Beside();
        var second = tip.Beside!;

        var now = Delay + 1;
        Hover(tip, Link(second), now);
        Hover(tip, Link(second), now + Delay);
        var third = second.Beside;
        Assert.NotNull(third);

        now += Delay + 1;
        Hover(tip, Link(third), now);
        Hover(tip, Link(third), now + Delay);
        Assert.Null(third.Beside);
    }

    /// <summary>A tip that does not nest shows a link's page in itself, and opens nothing beside it.</summary>
    [Fact]
    public void ATipThatDoesNotNestOpensNothingBeside()
    {
        var (tip, _) = Opened(false);

        Hover(tip, Link(tip), 0);
        Hover(tip, Link(tip), Delay * 2);

        Assert.Null(tip.Beside);
    }

    /// <summary>The first tip opened with a link on its last line, drawn, and a second tip open beside it on that link's page.</summary>
    static CodexTip Beside()
    {
        var (tip, _) = Opened();
        Hover(tip, Link(tip), 0);
        Hover(tip, Link(tip), Delay);
        tip.Draw(new Recording(Area), Area);
        return tip;
    }

    /// <summary>A tip open and drawn on an element at the left of a wide screen, its page plain words and then a link, each page it leads to the same.</summary>
    static (CodexTip Tip, Dictionary<string, Tip> Pages) Opened(bool nests = true)
    {
        var anchor = new VStack { Placement = FixedSize.Size(100f, 40f), Align = Align.Left | Align.Top };
        var root = new VStack { Children = { new VStack { Padding = new Thickness(40f, 100f, 0f, 0f), Children = { anchor } } } };
        new WindowLayer().Covers(root);
        root.Measure(new Recording(Area), new Vector2(Area.Width, Area.Height));
        root.Arrange(Area);

        var pages = new Dictionary<string, Tip>
        {
            ["b"] = new Tip("Words of b.\n\n[c](c)"),
            ["c"] = new Tip("Words of c.\n\n[d](d)"),
            ["d"] = new Tip("Words of d.\n\n[e](e)"),
            ["e"] = new Tip("Words of e.")
        };
        var tip = new CodexTip
        {
            Placement = FixedSize.Wide(Wide),
            Delay = Delay,
            Grace = Grace,
            Pages = id => pages.TryGetValue(id, out var page) ? page : null,
            Nests = nests
        };
        tip.Open(anchor, new Tip("Plain words first.\n\n[b](b)"));
        tip.Draw(new Recording(Area), Area);
        return (tip, pages);
    }

    /// <summary>Rests the pointer at <paramref name="point" /> at <paramref name="now" />, on nothing the screen has to say: the tip covers what lies there.</summary>
    static void Hover(CodexTip tip, Vector2 point, int now)
    {
        tip.Hover(null, null, point, now);
        tip.Draw(new Recording(Area), Area);
    }

    /// <summary>A point on the link at the start of the last line of <paramref name="tip" />.</summary>
    static Vector2 Link(CodexTip tip) => new Vector2(tip.Bounds.X + 4f, tip.Bounds.Y + tip.Bounds.Height - 4f);

    /// <summary>A point on the plain words at the start of the first line of <paramref name="tip" />.</summary>
    static Vector2 Plain(CodexTip tip) => new Vector2(tip.Bounds.X + 4f, tip.Bounds.Y + 4f);

    static Vector2 On(Element element) => new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
}
