using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRage.Input;
using VRageMath;
using Xunit;

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
        var command = new Counted();
        var button = new Button { Text = "Done", Command = command };
        var input = Laid(new VStack { Children = { button } });

        input.PointerDown(Middle(button), 1);
        input.PointerUp(Middle(button));

        Assert.Equal(1, command.Count);
    }

    /// <summary>A press dragged off a button before it is released carries out nothing.</summary>
    [Fact]
    public void AReleaseOffTheButtonCarriesOutNothing()
    {
        var command = new Counted();
        var button = new Button { Text = "Done", Command = command };
        var input = Laid(new VStack { Children = { button } });

        input.PointerDown(Middle(button), 1);
        input.PointerUp(new Vector2(Wide - 1f, High - 1f));

        Assert.Equal(0, command.Count);
    }

    /// <summary>A button whose command cannot be carried out is disabled, and a press on it does nothing.</summary>
    [Fact]
    public void ACommandThatCannotRunDisablesItsButton()
    {
        var command = new Counted { CanExecute = false };
        var button = new Button { Text = "Done", Command = command };
        var input = Laid(new VStack { Children = { button } });

        Assert.False(button.IsEnabled);

        input.PointerDown(Middle(button), 1);
        input.PointerUp(Middle(button));
        Assert.Equal(0, command.Count);

        command.Allow();
        Assert.True(button.IsEnabled);
    }

    /// <summary>Pressing a box gives it the keyboard; pressing nothing takes the keyboard back.</summary>
    [Fact]
    public void PressingABoxFocusesItAndPressingNothingLetsGo()
    {
        var box = new TextBox { Placement = Fixed.Wide(200f) };
        var input = Laid(new VStack { Children = { box } });

        input.PointerDown(Middle(box), 1);
        Assert.Same(box, input.Focused);
        Assert.True(box.IsFocused);

        input.PointerDown(new Vector2(Wide - 1f, High - 1f), 1);
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

    static InputRoot Laid(Element root)
    {
        _ = new Easel(root, Wide, High);
        return new InputRoot(root);
    }

    static Vector2 Middle(Element element)
    {
        return new Vector2(element.Bounds.X + element.Bounds.Width / 2f, element.Bounds.Y + element.Bounds.Height / 2f);
    }

    /// <summary>A command that counts how often it was carried out.</summary>
    sealed class Counted : ICommand
    {
        public int Count { get; private set; }

        public bool CanExecute { get; set; } = true;

        public event Action? CanExecuteChanged;

        public void Execute()
        {
            Count++;
        }

        /// <summary>Lets the command run, and says so.</summary>
        public void Allow()
        {
            CanExecute = true;
            CanExecuteChanged?.Invoke();
        }
    }
}
