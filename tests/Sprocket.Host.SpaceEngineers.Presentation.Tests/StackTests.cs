using System.Globalization;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Sim.Simulation;
using Xunit;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>How HStack and VStack lay out their children, and that they draw what the containers they replace drew.</summary>
public class StackTests
{
    const float Wide = 400f;
    const float High = 200f;

    /// <summary>A VStack draws exactly what a Stack running down drew.</summary>
    [Fact]
    public void VStackDrawsWhatAStackDownDrew()
    {
        var old = new Stack { Spacing = 4f };
        Fill(old.Children);

        var fresh = new VStack { Spacing = 4f };
        Fill(fresh.Children);

        Assert.Equal(Drawn(old), Drawn(fresh));
    }

    /// <summary>An HStack draws exactly what a Stack running across drew.</summary>
    [Fact]
    public void AnHStackDrawsWhatAStackAcrossDrew()
    {
        var old = new Stack { Spacing = 4f, Horizontal = true };
        Fill(old.Children);

        var fresh = new HStack { Spacing = 4f };
        Fill(fresh.Children);

        Assert.Equal(Drawn(old), Drawn(fresh));
    }

    /// <summary>An HStack whose middle grows draws what a Bar drew, in its own order.</summary>
    [Fact]
    public void AnHStackWithAGrowingMiddleDrawsWhatABarDrew()
    {
        var bar = new Bar
        {
            Spacing = 5f,
            Left = new Label { Text = "Left" },
            Fill = new Label { Text = "The middle", Background = Palette(1) },
            Right = new Label { Text = "Right" }
        };

        var stack = new HStack
        {
            Spacing = 5f,
            Children =
            {
                new Label { Text = "Left" },
                new Label { Text = "The middle", Background = Palette(1), Placement = Flex.Grow },
                new Label { Text = "Right" }
            }
        };

        var fromBar = Drawn(bar);
        var fromStack = Drawn(stack);
        fromBar.Sort();
        fromStack.Sort();

        Assert.Equal(fromBar, fromStack);
    }

    /// <summary>Children that grow share the room left by weight.</summary>
    [Fact]
    public void GrowingChildrenShareTheRoomLeftByWeight()
    {
        Label one, three;
        var stack = new HStack
        {
            Children =
            {
                new Label { Placement = Fixed.Wide(40f) },
                new Label { Placement = Flex.By(1f) }.As(out one),
                new Label { Placement = Flex.By(3f) }.As(out three)
            }
        };

        _ = new Easel(stack, Wide, High);

        Assert.Equal(90f, one.Bounds.Width, 3);
        Assert.Equal(270f, three.Bounds.Width, 3);
        Assert.Equal(130f, three.Bounds.X, 3);
    }

    /// <summary>A fixed width holds a child to it, whatever the child asks for.</summary>
    [Fact]
    public void AFixedWidthHoldsAChildToIt()
    {
        Label held;
        var stack = new HStack
        {
            Children = { new Label { Text = "Far too long for the room", Placement = Fixed.Wide(30f) }.As(out held) }
        };

        _ = new Easel(stack, Wide, High);

        Assert.Equal(30f, held.Bounds.Width, 3);
    }

    /// <summary>Changing a child's placement lays the stack out again.</summary>
    [Fact]
    public void ChangingAPlacementLaysTheStackOutAgain()
    {
        Label moved;
        var stack = new HStack { Children = { new Label { Text = "Name" }.As(out moved) } };
        var easel = new Easel(stack, Wide, High);

        moved.Placement = Fixed.Wide(50f);
        easel.Draw(Wide, High);

        Assert.Equal(50f, moved.Bounds.Width, 3);
    }

    /// <summary>A stack refuses a child placed in a way it cannot honour, as the child joins.</summary>
    [Fact]
    public void AStackRefusesAPlacementItCannotHonour()
    {
        var stack = new VStack();

        Assert.Throws<InvalidOperationException>(() => stack.Children.Add(new Label { Placement = new Elsewhere() }));
        Assert.Equal(0, stack.Children.Count);
    }

    /// <summary>A stack refuses a placement it cannot honour when a child it holds changes to it, and keeps the old one.</summary>
    [Fact]
    public void AStackRefusesAChangeToAPlacementItCannotHonour()
    {
        Label child;
        _ = new VStack { Children = { new Label { Placement = Flex.Grow }.As(out child) } };

        Assert.Throws<InvalidOperationException>(() => child.Placement = new Elsewhere());
        Assert.Same(Flex.Grow, child.Placement);
    }

    /// <summary>A stack measures no text on a frame where nothing changed.</summary>
    [Fact]
    public void AnIdleStackMeasuresNoText()
    {
        var stack = new HStack
        {
            Children =
            {
                new Label { Text = "Left" },
                new Label { Text = "Middle", Placement = Flex.Grow },
                new Label { Text = "Right" }
            }
        };

        var easel = new Easel(stack, Wide, High);
        var settled = easel.Measured;
        easel.Draw(Wide, High);
        easel.Draw(Wide, High);

        Assert.Equal(settled, easel.Measured);
    }

    /// <summary>Children of every kind the stacks meet: labels, padding, a background, and one collapsed.</summary>
    static void Fill(ElementCollection children)
    {
        children.Add(new Label { Text = "First", Background = Palette(0) });
        children.Add(new Label { Text = "Collapsed", Collapsed = true });
        children.Add(new Label { Text = "Padded", Padding = new Thickness(6f, 3f), Background = Palette(1) });
        children.Add(new Label { Text = "Last of three, and longer" });
    }

    static VRageMath.Color Palette(int which)
    {
        return which == 0 ? new VRageMath.Color(10, 20, 30) : new VRageMath.Color(40, 50, 60);
    }

    /// <summary>Every call a frame of <paramref name="root" /> makes, written out so two frames can be compared.</summary>
    static List<string> Drawn(Element root)
    {
        var easel = new Easel(root, Wide, High);
        return easel.Commands.Select(Written).ToList();
    }

    static string Written(DrawCommand command)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0} {1} {2} {3} {4} {5}",
            command.Kind, command.Destination, command.Position, command.Text, command.Color, command.Clip);
    }

    /// <summary>A placement no stack understands.</summary>
    sealed class Elsewhere : IPlacement
    {
        public event Action? Changed
        {
            add { }
            remove { }
        }
    }
}
