using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using VRageMath;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Windows over a screen: placed by the layer, drawn over what is beneath, keeping what is on them to themselves, and a modal one keeping all input.</summary>
public class WindowLayerTests
{
    static readonly RectangleF Area = new RectangleF(0f, 0f, 1000f, 800f);

    /// <summary>A window stands in the middle of the area, as wide as its placement says.</summary>
    [Fact]
    public void AWindowStandsInTheMiddle()
    {
        var (layer, window, _) = Opened(modal: false);

        Assert.Equal(300f, window.Bounds.Width);
        Assert.Equal(Area.Width / 2f, window.Bounds.X + window.Bounds.Width / 2f, 1);
        Assert.Equal(Area.Height / 2f, window.Bounds.Y + window.Bounds.Height / 2f, 1);
        Assert.Equal(1, layer.Count);
    }

    /// <summary>A press on what a window holds reaches it, and the screen beneath is told to stay out of it.</summary>
    [Fact]
    public void APressOnAWindowReachesWhatItHolds()
    {
        var (layer, _, button) = Opened(modal: false);

        var hit = layer.Under(Middle(button), out var stopped);

        Assert.Same(button, hit);
        Assert.True(stopped);
    }

    /// <summary>Beside a modeless window, a press is left for the screen beneath; beside a modal one it reaches nothing at all.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public void AModalWindowKeepsPressesFromTheScreen(bool modal, bool kept)
    {
        var (layer, _, _) = Opened(modal);

        var hit = layer.Under(new Vector2(5f, 5f), out var stopped);

        Assert.Null(hit);
        Assert.Equal(kept, stopped);
        Assert.Equal(modal, layer.Modal != null);
    }

    /// <summary>A modal window's backdrop is painted over the whole area before the window is drawn.</summary>
    [Fact]
    public void AModalWindowPaintsItsBackdropFirst()
    {
        var graphics = new Recording(Area);
        var layer = new WindowLayer();
        layer.Open(new Dialog { Backdrop = Color.Black, Background = Color.White, Content = new Button { Text = "Go" } });

        layer.Draw(graphics, Area);

        Assert.Equal(Area, graphics.Fills[0]);
        Assert.True(graphics.Fills.Count > 1, "the window itself was not drawn");
    }

    /// <summary>Closing a window takes it away, and the screen has the input back.</summary>
    [Fact]
    public void ClosingGivesTheInputBack()
    {
        var (layer, window, _) = Opened(modal: true);

        layer.Close(window);
        layer.Under(new Vector2(5f, 5f), out var stopped);

        Assert.False(stopped);
        Assert.Equal(0, layer.Count);
    }

    /// <summary>A long list hanging under something near the top stops at the foot of the area and scrolls, rather than running off it.</summary>
    [Fact]
    public void ALongListHangingOpenStopsAtTheEdgeAndScrolls()
    {
        var (layer, popup, scroll, rows) = Hanging(73, 100f);

        Assert.True(popup.Bounds.Y >= 140f, "the list is not under what it hangs from");
        Assert.True(popup.Bounds.Y + popup.Bounds.Height <= Area.Y + Area.Height, "the list runs off the area");
        Assert.True(scroll.CanScroll, "a list longer than its room does not scroll");
        Assert.Equal(1, layer.Count);
        Assert.True(rows[72].Bounds.Y > popup.Bounds.Y + popup.Bounds.Height, "the last word is on show without scrolling");
    }

    /// <summary>A short list is as tall as its words, with nothing to scroll.</summary>
    [Fact]
    public void AShortListIsAsTallAsItsWords()
    {
        var (_, popup, scroll, rows) = Hanging(3, 100f);

        Assert.False(scroll.CanScroll);
        Assert.Equal(rows[2].Bounds.Y + rows[2].Bounds.Height, popup.Bounds.Y + popup.Bounds.Height, 1);
    }

    /// <summary>Asked to show one of its words, a list scrolls just far enough to bring it into view.</summary>
    [Fact]
    public void AListBringsAWordIntoView()
    {
        var (layer, popup, scroll, rows) = Hanging(73, 100f);

        scroll.BringIntoView(rows[60]);
        layer.Draw(new Recording(Area), Area);

        Assert.True(rows[60].Bounds.Y >= popup.Bounds.Y, "the word is above what is on show");
        Assert.True(rows[60].Bounds.Y + rows[60].Bounds.Height <= popup.Bounds.Y + popup.Bounds.Height,
            "the word is below what is on show");
    }

    /// <summary>A list opened on something near the foot hangs over it instead, in the room above.</summary>
    [Fact]
    public void AListNearTheFootHangsAboveInTheRoomThere()
    {
        var (_, popup, scroll, _) = Hanging(73, 700f);

        Assert.True(popup.Bounds.Y + popup.Bounds.Height <= 700f, "the list is not over what it hangs from");
        Assert.True(popup.Bounds.Y >= Area.Y, "the list runs off the top");
        Assert.True(scroll.CanScroll);
    }

    static (WindowLayer, Popup, ScrollView, List<Button>) Hanging(int count, float at)
    {
        var anchor = new Button { Text = "Anchor" };
        anchor.Measure(new Recording(Area), new Vector2(200f, 40f));
        anchor.Arrange(new RectangleF(0f, at, 200f, 40f));

        var rows = new List<Button>();
        var stack = new VStack();
        for (var i = 0; i < count; i++)
        {
            var row = new Button { Text = "Word " + i, Command = new Relay(() => { }) };
            rows.Add(row);
            stack.Children.Add(row);
        }

        var scroll = new ScrollView { BarWidth = 12f, Thumb = Color.White, FitsContent = true, Content = stack };
        var popup = new Popup { Anchor = anchor, Content = scroll };
        var layer = new WindowLayer();
        layer.Open(popup);
        layer.Draw(new Recording(Area), Area);
        return (layer, popup, scroll, rows);
    }

    static (WindowLayer, Window, Button) Opened(bool modal)
    {
        var button = new Button { Text = "Go", Command = new Relay(() => { }) };
        var window = new Window { IsModal = modal, Placement = Fixed.Wide(300f), Content = new VStack { Children = { button } } };
        var layer = new WindowLayer();
        layer.Open(window);
        layer.Draw(new Recording(Area), Area);
        return (layer, window, button);
    }

    /// <summary>Over a window holding nothing that takes input, a press, a resting pointer and a wheel all stop at the window; beside it, none do.</summary>
    [Fact]
    public void AWindowKeepsWhatIsOnItToItself()
    {
        var layer = new WindowLayer();
        var window = new FloatingWindow { Placement = Fixed.Size(200f, 100f), Content = new Label { Text = "Words" } };
        layer.Open(window);
        layer.Draw(new Recording(Area), Area);

        var on = new Vector2(window.Bounds.X + 20f, window.Bounds.Y + 20f);
        var beside = new Vector2(window.Bounds.X - 20f, window.Bounds.Y - 20f);

        layer.Under(on, out var pressed);
        layer.SayingAt(on, out var said);
        layer.WheelAt(on, out var turned);
        Assert.True(pressed, "a press went through");
        Assert.True(said, "the pointer's note came from beneath");
        Assert.True(turned, "the wheel went through");

        layer.Under(beside, out pressed);
        layer.SayingAt(beside, out said);
        layer.WheelAt(beside, out turned);
        Assert.False(pressed || said || turned, "something beside the window stopped at it");
    }

    static Vector2 Middle(Element element) =>
        new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
}
