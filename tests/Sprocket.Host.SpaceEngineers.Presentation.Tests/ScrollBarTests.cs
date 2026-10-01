using Lumel;
using Lumel.Input;
using Lumel.Layout;
using VRageMath;
using Xunit;
using Lumel.Viewing;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>A scroll: as much of its content as fits, moved by the wheel and by a thumb on a bar that takes the pointer itself.</summary>
public class ScrollBarTests
{
    const float Wide = 400f;
    const float High = 200f;
    const float Bar = 12f;

    static readonly Color Thumb = new Color(95, 114, 120);
    static readonly Color Held = new Color(133, 161, 169);

    static readonly ScrollViewStyle Barred = new ScrollViewStyle
    {
        Base = { BarWidth = Bar, Thumb = Thumb, LeastThumb = 32f },
        Held = { Thumb = Held }
    };

    /// <summary>Scrolling moves the content up, and never past either end of it.</summary>
    [Fact]
    public void ScrollingStaysInsideTheContent()
    {
        var (scroll, content, easel, _) = Laid(1000f);

        scroll.ScrollBy(300f);
        easel.Draw(Wide, High);
        Assert.Equal(-300f, content.Bounds.Y);

        scroll.ScrollBy(5000f);
        easel.Draw(Wide, High);
        Assert.Equal(High - 1000f, content.Bounds.Y);

        scroll.ScrollBy(-5000f);
        easel.Draw(Wide, High);
        Assert.Equal(0f, content.Bounds.Y);
    }

    /// <summary>The content is as wide as the room the bar leaves it.</summary>
    [Fact]
    public void TheBarTakesItsWidthFromTheContent()
    {
        var (_, content, _, _) = Laid(1000f);

        Assert.Equal(Wide - Bar, content.Bounds.Width);
    }

    /// <summary>Dragging the thumb moves the content in proportion, and the thumb takes its held color while it is held.</summary>
    [Fact]
    public void DraggingTheThumbMovesTheContent()
    {
        var (scroll, content, easel, input) = Laid(1000f);
        var top = new Vector2(Wide - Bar / 2f, 10f);

        input.PointerDown(scroll.Under(top), top, 1, false, false);
        Assert.True(scroll.IsHeld);
        Assert.Equal(Held, scroll.Thumb);

        input.PointerMove(top + new Vector2(0f, 40f));
        easel.Draw(Wide, High);

        // The thumb is 40 of the 200 and travels 160 while the content travels 800: five to one.
        Assert.Equal(-200f, content.Bounds.Y, 1);

        input.PointerUp(top + new Vector2(0f, 40f));
        Assert.False(scroll.IsHeld);
        Assert.Equal(Thumb, scroll.Thumb);
    }

    /// <summary>A press on the track away from the thumb puts the thumb there first.</summary>
    [Fact]
    public void APressOnTheTrackPutsTheThumbThere()
    {
        var (scroll, content, easel, input) = Laid(1000f);
        var bottom = new Vector2(Wide - Bar / 2f, High - 1f);

        input.PointerDown(scroll.Under(bottom), bottom, 1, false, false);
        input.PointerUp(bottom);
        easel.Draw(Wide, High);

        Assert.Equal(High - 1000f, content.Bounds.Y);
    }

    /// <summary>Only the bar is the scroll's own to press; everywhere else a press reaches the content or nothing.</summary>
    [Fact]
    public void OnlyTheBarIsPressed()
    {
        var (scroll, _, _, _) = Laid(1000f);

        Assert.Same(scroll, scroll.Under(new Vector2(Wide - Bar / 2f, 100f)));
        Assert.Null(scroll.Under(new Vector2(100f, 100f)));
    }

    /// <summary>Content that fits draws no bar, and a press where the bar would be reaches nothing.</summary>
    [Fact]
    public void ContentThatFitsHasNoBar()
    {
        var (scroll, _, _, _) = Laid(100f);

        Assert.False(scroll.CanScroll);
        Assert.Null(scroll.Under(new Vector2(Wide - Bar / 2f, 50f)));
    }

    static (ScrollView, Element, Easel, InputRoot) Laid(float tall)
    {
        var content = new VStack { Children = { new VStack { Placement = Fixed.High(tall) } } };
        var scroll = new ScrollView { Style = Barred, Content = content, Placement = Fixed.Size(Wide, High) };
        var root = new VStack { Children = { scroll } };
        var easel = new Easel(root, Wide, High);
        return (scroll, content, easel, new InputRoot(root));
    }
}
