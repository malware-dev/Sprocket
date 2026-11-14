using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel;
using Lumel.Controls;
using Xunit;

namespace Sprocket.Host.SpaceEngineers.Presentation.Tests;

/// <summary>Everything a player can press says what pressing it does where the pointer rests on it, unless it is of a kind whose words already say it.</summary>
public class PressableTipTests
{
    /// <summary>Everything shown that a press would run carries a tip, on each screen of the reference suite, or is of a kind that needs none.</summary>
    [Theory]
    [MemberData(nameof(ControlSoundTests.Screens), MemberType = typeof(ControlSoundTests))]
    public void EverythingPressableHasATipOrSaysItAll(string script)
    {
        var screen = Screen.Fresh(script.Length == 0 ? [] : script.Split(' '));

        var untold = screen.Scene.View.Within()
            .Where(e => e.Command != null && e.Command.CanExecute && Shown(e) && string.IsNullOrEmpty(e.Tip?.Text) && !SaysItAll(e))
            .Select(e => e.GetType().Name + " \"" + ((e as Button)?.Text ?? "") + "\"")
            .ToList();

        Assert.True(untold.Count == 0, "pressable with no tip: " + string.Join(", ", untold));
    }

    /// <summary>
    /// Whether <paramref name="element" /> is of a kind whose words say all a press does, so it needs no tip: a plus with words
    /// says what it adds, the window's primary button finishes what is on the screen, a selector row's check box picks the row
    /// named beside it, and a field's choice words are the choice.
    /// </summary>
    /// <remarks>A mark with no words is never one of these: nothing on it says what it does.</remarks>
    static bool SaysItAll(Element element)
    {
        return element is PlusMenu plus && !string.IsNullOrEmpty(plus.Text)
            || element is Button primary && primary.Style == Styles.Primary
            || element is CheckBox && element.Parent is PickRow
            || element is Button && Within<WordsField>(element);
    }

    static bool Within<T>(Element element)
    {
        for (var at = element.Parent; at != null; at = at.Parent)
        {
            if (at is T)
                return true;
        }

        return false;
    }

    static bool Shown(Element element)
    {
        for (var at = element; at != null; at = at.Parent)
        {
            if (!at.IsVisible)
                return false;
        }

        return true;
    }
}
