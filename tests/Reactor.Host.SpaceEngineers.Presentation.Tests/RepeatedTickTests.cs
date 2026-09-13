using Avalonia.Headless.XUnit;
using Reactor.Ui;
using Xunit;

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

/// <summary>
/// What a screen does when nothing is happening to it, sixty times a second.
/// </summary>
/// <remarks>
/// Every one of these faults is invisible to a test that draws once and fatal in the game, where a
/// throwing tick stops the whole thing running and a rebuilding screen flickers under the pointer.
/// </remarks>
public class RepeatedTickTests
{
    [AvaloniaFact]
    public void TickingRepeatedlyOnEveryScreenDoesNotThrow()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        screen.Run(60);

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        screen.Run(60);

        editor.PressBlock("Airlock Button");
        screen.Run(60);

        editor.PressChoice("is pressed");
        screen.Run(60);

        editor.PressPrimary();
        screen.Run(60);

        editor.PressAdd("do something");
        screen.Run(60);

        editor.Back();
        screen.Run(60);

        editor.GoToRoot();
        screen.Run(60);

        editor.GoToRoot();
        screen.Run(60);

        Assert.NotEqual("", screen.Ended);
    }

    /// <summary>
    /// A screen that nothing is happening on must keep the very components it had.
    /// </summary>
    /// <remarks>
    /// Replacing them makes a target throw away everything it built and build it again, which loses
    /// whatever the pointer was over and whatever was being typed into. Sixty times a second that
    /// reads as the whole screen flickering under the mouse.
    /// </remarks>
    [AvaloniaFact]
    public void ScreensDoNotRebuildThemselvesWhileNothingHappens()
    {
        var screen = Screen.Fresh();
        var editor = screen.Editor;

        AssertStill(screen, "the sequence list");

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        AssertStill(screen, "picking what happens");

        editor.PressBlock("Airlock Button");
        AssertStill(screen, "picking what happens, with a block picked");

        editor.PressChoice("is pressed");
        editor.PressPrimary();
        AssertStill(screen, "the sequence");

        editor.PressAdd("do something");
        AssertStill(screen, "picking what to do");

        editor.Back();
        editor.GoToRoot();
        AssertStill(screen, "the construct");
    }

    static void AssertStill(Screen screen, string what)
    {
        screen.Run(5);

        var before = new List<EditorComponent>(screen.Editor.Screen.Components);
        screen.Run(60);
        var after = screen.Editor.Screen.Components;

        var same = before.Count == after.Count;
        for (var i = 0; same && i < before.Count; i++)
            same = ReferenceEquals(before[i], after[i]);

        Assert.True(same,
            $"{what} replaced its components while nothing was happening, so a target rebuilds "
            + "everything sixty times a second and the pointer never keeps hold of anything.");
    }
}
