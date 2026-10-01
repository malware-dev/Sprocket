using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRage.Input;
using VRageMath;
using Xunit;
using Lumel.Images;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Where the pointer and keyboard enter a tree: commands, focus, and the order Tab takes.</summary>
public class InputRootTests
{
    const float Wide = 400f;
    const float High = 200f;

    /// <summary>A press released on a button carries out its command.</summary>
    [Fact]
    public void AReleaseOnAButtonCarriesOutItsCommand()
    {
        var count = 0;
        var button = new Button { Text = "Done", Command = new Relay(() => count++) };
        var input = Laid(new VStack { Children = { button } });

        input.PointerDown(Middle(button), 1, false, false);
        input.PointerUp(Middle(button));

        Assert.Equal(1, count);
    }

    /// <summary>A press dragged off a button before it is released carries out nothing.</summary>
    [Fact]
    public void AReleaseOffTheButtonCarriesOutNothing()
    {
        var count = 0;
        var button = new Button { Text = "Done", Command = new Relay(() => count++) };
        var input = Laid(new VStack { Children = { button } });

        input.PointerDown(Middle(button), 1, false, false);
        input.PointerUp(new Vector2(Wide - 1f, High - 1f));

        Assert.Equal(0, count);
    }

    /// <summary>A button whose command cannot be carried out is disabled, and a press on it does nothing.</summary>
    [Fact]
    public void ACommandThatCannotRunDisablesItsButton()
    {
        var count = 0;
        var command = new Relay(() => count++) { CanExecute = false };
        var button = new Button { Text = "Done", Command = command };
        var input = Laid(new VStack { Children = { button } });

        Assert.False(button.IsEnabled);

        input.PointerDown(Middle(button), 1, false, false);
        input.PointerUp(Middle(button));
        Assert.Equal(0, count);

        command.CanExecute = true;
        Assert.True(button.IsEnabled);
    }

    /// <summary>Pressing a box gives it the keyboard; pressing nothing takes the keyboard back.</summary>
    [Fact]
    public void PressingABoxFocusesItAndPressingNothingLetsGo()
    {
        var box = new TextBox { Placement = Fixed.Wide(200f) };
        var input = Laid(new VStack { Children = { box } });

        input.PointerDown(Middle(box), 1, false, false);
        Assert.Same(box, input.Focused);
        Assert.True(box.IsFocused);

        input.PointerDown(new Vector2(Wide - 1f, High - 1f), 1, false, false);
        Assert.Null(input.Focused);
        Assert.False(box.IsFocused);
    }

    /// <summary>Tab moves the keyboard through the focusable elements in the order the tree is built in, round again, and back with shift.</summary>
    [Fact]
    public void TabMovesTheFocusInTreeOrder()
    {
        var first = new TextBox { Placement = Fixed.Wide(200f) };
        var second = new TextBox { Placement = Fixed.Wide(200f) };
        var third = new TextBox { Placement = Fixed.Wide(200f) };
        var input = Laid(new VStack { Children = { first, new HStack { Children = { second } }, third } });

        input.Key(MyKeys.Tab, false, false);
        Assert.Same(first, input.Focused);

        input.Key(MyKeys.Tab, false, false);
        Assert.Same(second, input.Focused);

        input.Key(MyKeys.Tab, false, false);
        input.Key(MyKeys.Tab, false, false);
        Assert.Same(first, input.Focused);

        input.Key(MyKeys.Tab, true, false);
        Assert.Same(third, input.Focused);
    }

    /// <summary>Moving the keyboard says so, and giving it to what already has it says nothing.</summary>
    [Fact]
    public void MovingTheFocusSaysSo()
    {
        var box = new TextBox { Placement = Fixed.Wide(200f) };
        var input = Laid(new VStack { Children = { box } });
        var moves = 0;
        input.FocusChanged += () => moves++;

        input.Focus(box);
        input.Focus(box);
        input.Focus(null);

        Assert.Equal(2, moves);
    }

    /// <summary>Escape lets the keyboard go.</summary>
    [Fact]
    public void EscapeLetsTheFocusGo()
    {
        var box = new TextBox { Placement = Fixed.Wide(200f) };
        var input = Laid(new VStack { Children = { box } });

        input.Focus(box);
        input.Key(MyKeys.Escape, false, false);

        Assert.Null(input.Focused);
    }

    /// <summary>Typed characters go to what holds the keyboard.</summary>
    [Fact]
    public void TypingGoesToTheFocusedBox()
    {
        var box = new TextBox { Placement = Fixed.Wide(200f) };
        var input = Laid(new VStack { Children = { box } });

        input.Focus(box);
        input.Text('h');
        input.Text('i');

        Assert.Equal("hi", box.Text);
    }

    /// <summary>Something inside a control that only has something to say is what a tip is read from, and a press on it reaches the control.</summary>
    [Fact]
    public void ATipInsideAControlLetsThePressThrough()
    {
        var light = new Glyph { Symbol = "o", Tip = new Tip("Idle") };
        var row = new HStack { Command = new Relay(() => { }), Children = { light, new Label { Text = "Airlock" } } };
        Laid(new VStack { Children = { row } });

        var at = Middle(light);

        Assert.Same(row, row.Under(at));
        Assert.Same(light, row.SayingAt(at));
    }

    /// <summary>A transparent self passes a press to its container; transparent children pass it to the container that holds them.</summary>
    [Theory]
    [InlineData(InputTransparency.None, "button")]
    [InlineData(InputTransparency.Self, "row")]
    public void TransparencyOfTheElementItself(InputTransparency transparency, string reached)
    {
        var button = new Button { Text = "Go", Command = new Relay(() => { }), InputTransparency = transparency };
        var row = new HStack { Command = new Relay(() => { }), Children = { button } };
        Laid(new VStack { Children = { row } });

        Assert.Same(reached == "button" ? button : row, row.Under(Middle(button)));
    }

    /// <summary>A container with transparent children takes the press itself; one transparent through and through lets it past entirely.</summary>
    [Theory]
    [InlineData(InputTransparency.Children, "row")]
    [InlineData(InputTransparency.All, "nothing")]
    public void TransparencyOfWhatIsInside(InputTransparency transparency, string reached)
    {
        var button = new Button { Text = "Go", Command = new Relay(() => { }) };
        var row = new HStack { Command = new Relay(() => { }), InputTransparency = transparency, Children = { button } };
        var root = new VStack { Children = { row } };
        Laid(root);

        Assert.Same(reached == "row" ? row : null, root.Under(Middle(button)));
    }

    /// <summary>Tab passes over a transparent box, and over every box inside a container whose children are transparent.</summary>
    [Fact]
    public void TabPassesOverWhatIsTransparent()
    {
        var first = new TextBox { Placement = Fixed.Wide(200f), InputTransparency = InputTransparency.Self };
        var inner = new TextBox { Placement = Fixed.Wide(200f) };
        var last = new TextBox { Placement = Fixed.Wide(200f) };
        var input = Laid(new VStack
        {
            Children = { first, new HStack { InputTransparency = InputTransparency.Children, Children = { inner } }, last }
        });

        input.Key(MyKeys.Tab, false, false);
        Assert.Same(last, input.Focused);

        input.Key(MyKeys.Tab, false, false);
        Assert.Same(last, input.Focused);
    }

    static InputRoot Laid(Element root)
    {
        _ = new Easel(root, Wide, High);
        return new InputRoot(root);
    }

    static Vector2 Middle(Element element)
    {
        return new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
    }
}

