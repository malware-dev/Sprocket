using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Reactor.Host.Features.Shell;
using Reactor.Host.System;
using Reactor.Ui;
using Xunit;

namespace Reactor.Host.Tests;

/// <summary>
/// Runs the clock the way the app does, rather than a tick or two per action.
/// </summary>
/// <remarks>
/// The other tests drive the editor and tick once; the real host ticks sixty times a
/// second between actions. Anything that throws on the second pass through a screen
/// is invisible to them and fatal in the app, because a throwing timer stops running.
/// </remarks>
public class RepeatedTickTests
{
    [AvaloniaFact]
    public void TickingRepeatedlyOnEveryScreenDoesNotThrow()
    {
        var window = new MainWindow(Simulation.Fresh());
        window.Show();
        var editor = window.Editor;

        Run(window, 60);

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        Run(window, 60);

        editor.PressBlock("Airlock Button");
        Run(window, 60);

        editor.PressChoice("is pressed");
        Run(window, 60);

        editor.PressPrimary();
        Run(window, 60);

        editor.PressAdd("do something");
        Run(window, 60);

        editor.Back();
        Run(window, 60);

        editor.GoToRoot();
        Run(window, 60);

        editor.GoToRoot();
        Run(window, 60);

        Assert.True(window.IsVisible);
    }

    /// <summary>
    /// A screen that nothing is happening on must keep the very components it had.
    /// </summary>
    /// <remarks>
    /// Replacing them makes the host throw away every control and build new ones,
    /// which loses whatever the pointer was over and whatever was being typed into.
    /// Sixty times a second that reads as the whole screen flickering under the mouse.
    ///
    /// It went unnoticed because the loop ran through the dispatcher: the host pushed a
    /// value back into a search box, Avalonia raised the change afterwards rather than
    /// during, core took the echo for a keystroke and rebuilt, which pushed the value
    /// again. Nothing showed up without pumping the dispatcher, which is why
    /// <see cref="Run"/> now does.
    /// </remarks>
    [AvaloniaFact]
    public void ScreensDoNotRebuildThemselvesWhileNothingHappens()
    {
        var window = new MainWindow(Simulation.Fresh());
        window.Show();
        var editor = window.Editor;

        AssertStill(window, "the sequence list");

        editor.NewSystem("Airlock");
        editor.PressStart("When…");
        AssertStill(window, "picking what happens");

        editor.PressBlock("Airlock Button");
        AssertStill(window, "picking what happens, with a block picked");

        editor.PressChoice("is pressed");
        editor.PressPrimary();
        AssertStill(window, "the sequence");

        editor.PressAdd("do something");
        AssertStill(window, "picking what to do");

        editor.Back();
        editor.GoToRoot();
        AssertStill(window, "the ship");
    }

    static void AssertStill(MainWindow window, string what)
    {
        Run(window, 5);

        var before = new List<EditorComponent>(window.Editor.Screen.Components);
        Run(window, 60);
        var after = window.Editor.Screen.Components;

        var same = before.Count == after.Count;
        for (var i = 0; same && i < before.Count; i++)
            same = ReferenceEquals(before[i], after[i]);

        Assert.True(same,
            $"{what} replaced its components while nothing was happening, so the host " +
            "rebuilds every control sixty times a second and the pointer never keeps " +
            "hold of anything.");
    }

    static void Run(MainWindow window, int ticks)
    {
        for (var i = 0; i < ticks; i++)
        {
            window.Tick();
            window.Measure(new Size(820, 680));
            window.Arrange(new Rect(0, 0, 820, 680));

            // The app runs on a dispatcher, and some of what the host does to a control
            // comes back through it a beat later. Without this the test never sees it.
            Dispatcher.UIThread.RunJobs();
        }
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }
}
