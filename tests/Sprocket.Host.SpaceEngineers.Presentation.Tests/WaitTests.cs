using Sprocket.Host.SpaceEngineers.Sequences;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// How long a step waits is picked from a few lengths, or typed where none of them will do.
/// </summary>
/// <remarks>
/// Typed in the list itself, below the lengths it offers, rather than on another screen. What is typed
/// is ticks, the only unit a program writes a wait in.
/// </remarks>
public class WaitTests
{
    [Fact]
    public void ALengthNobodyOfferedCanBeTyped()
    {
        var screen = WithASequence();
        screen.Editor.PressAdd("wait…");
        screen.Run(2);

        var box = screen.Editor.TypedBox()!;
        box.Insert("90");
        screen.Run(1);
        box.Submit!.Execute();
        screen.Run(2);

        var wait = Assert.IsType<Wait>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal(90, wait.Ticks);
    }

    /// <summary>Only digits go in, so Enter never takes something it cannot use.</summary>
    [Fact]
    public void TheBoxTakesDigitsAndNothingElse()
    {
        var screen = WithASequence();
        screen.Editor.PressAdd("wait…");
        screen.Run(2);

        var box = screen.Editor.TypedBox()!;
        box.Insert("12");
        box.Insert("s");
        box.Insert("-");
        Assert.Equal("12", box.Text);

        box.Backspace();
        box.Backspace();
        Assert.Equal("", box.Text);
    }

    /// <summary>And nothing at all is a length of nothing, which Enter does not take.</summary>
    [Fact]
    public void NothingTypedTakesNothing()
    {
        var screen = WithASequence();
        screen.Editor.PressAdd("wait…");
        screen.Run(2);

        screen.Run(1);
        Assert.False(screen.Editor.TypedBox()!.Submit!.CanExecute, "an empty box offers to take nothing");
        screen.Editor.TypedBox()!.Submit!.Execute();
        screen.Run(2);

        Assert.Empty(screen.Editor.Program.Sequences[0].Steps);
    }

    static Screen WithASequence()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewButtonSystem("Airlock");
        return screen.Run(2);
    }
}
