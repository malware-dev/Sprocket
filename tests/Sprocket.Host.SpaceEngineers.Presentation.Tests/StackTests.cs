using Lumel;
using Lumel.Layout;
using Lumel.Text;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>How HStack and VStack lay out their children.</summary>
public class StackTests
{
    const float Wide = 400f;
    const float High = 200f;

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

    /// <summary>A child taking only spare room asks for none, so its stack keeps its children's size, and pushes what follows to the far end of a wider one.</summary>
    [Fact]
    public void ASpareChildTakesOnlyTheRoomLeftWhenArranged()
    {
        HStack row;
        Label last;
        var column = new VStack
        {
            Children =
            {
                new HStack { Placement = Fixed.Wide(300f) },
                new HStack
                {
                    Children =
                    {
                        new Label { Placement = Fixed.Wide(40f) },
                        new HStack { Placement = Flex.Spare },
                        new Label { Placement = Fixed.Wide(60f) }.As(out last)
                    }
                }.As(out row)
            }
        };

        _ = new Easel(new HStack { Children = { column } }, Wide, High);

        Assert.Equal(100f, row.DesiredSize.X, 3);
        Assert.Equal(300f, column.DesiredSize.X, 3);
        Assert.Equal(240f, last.Bounds.X - column.Bounds.X, 3);
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
