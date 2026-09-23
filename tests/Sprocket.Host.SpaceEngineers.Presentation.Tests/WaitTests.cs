using Sprocket.Host.SpaceEngineers.Lists;
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
        screen.Editor.PressAdd("wait");
        screen.Run(2);

        ChoiceEntry typed = Typed(screen);
        screen.Editor.SetText(typed.TextCommand, "90");
        screen.Editor.Invoke(typed.Command);
        screen.Run(2);

        var wait = Assert.IsType<Wait>(Assert.Single(screen.Editor.Program.Sequences[0].Steps));
        Assert.Equal(90, wait.Ticks);
    }

    /// <summary>Only digits go in, so Enter never takes something it cannot use.</summary>
    [Fact]
    public void TheBoxTakesDigitsAndNothingElse()
    {
        var screen = WithASequence();
        screen.Editor.PressAdd("wait");
        screen.Run(2);

        ChoiceEntry typed = Typed(screen);
        Assert.True(screen.Editor.Accepts(typed.TextCommand, "120"));
        Assert.False(screen.Editor.Accepts(typed.TextCommand, "2s"));
        Assert.False(screen.Editor.Accepts(typed.TextCommand, "-5"));
        Assert.True(screen.Editor.Accepts(typed.TextCommand, ""));
    }

    /// <summary>And nothing at all is a length of nothing, which Enter does not take.</summary>
    [Fact]
    public void NothingTypedTakesNothing()
    {
        var screen = WithASequence();
        screen.Editor.PressAdd("wait");
        screen.Run(2);

        screen.Editor.Invoke(Typed(screen).Command);
        screen.Run(2);

        Assert.Empty(screen.Editor.Program.Sequences[0].Steps);
    }

    static ChoiceEntry Typed(Screen screen)
    {
        var list = screen.Editor.Screen.Components.OfType<ChoiceListComponent>().Single();
        return list.Entries.Single(entry => entry.TextCommand != 0);
    }

    static Screen WithASequence()
    {
        var screen = Screen.Fresh();
        screen.Editor.NewSystem("Airlock");
        screen.Editor.PressStart("When…");
        screen.Editor.PressBlock("Airlock Button");
        screen.Editor.PressChoice("is pressed");
        screen.Editor.PressPrimary();
        return screen.Run(2);
    }
}
