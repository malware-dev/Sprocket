using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Reactor.Host.Features.Shell;
using Reactor.Host.System;
using Reactor.Ui;
using Xunit;

[assembly: AvaloniaTestApplication(typeof(Reactor.Host.Tests.TestAppBuilder))]

namespace Reactor.Host.Tests;

static class TestAppBuilder
{
    /// <summary>
    /// Headless, but with real Skia drawing rather than the stub renderer, so frames
    /// can be captured and looked at.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}

/// <summary>
/// Walks the renderer through every component, with no visible window involved.
/// </summary>
/// <remarks>
/// The editor's own behavior is covered in Reactor.Core.Tests. What is left to check
/// here is this target's half of the bargain: that every component actually has a
/// renderer, and that nothing throws when it is built and laid out.
/// </remarks>
public class WindowSmokeTests
{
    [AvaloniaFact]
    public void EveryComponentRendersAndLaysOut()
    {
        var window = new MainWindow(Simulation.Fresh());
        window.Show();
        window.Tick();
        Layout(window);

        var editor = window.Editor;
        var seen = new HashSet<string>();
        Record(seen, editor);

        // The empty root offers the starters straight away, and answering one lands
        // directly on the path: blocks, then what happens to them.
        editor.NewSystem("Airlock");
        window.Tick();
        Layout(window);
        Record(seen, editor);

        editor.PressStart("When...");
        window.Tick();
        Layout(window);
        Record(seen, editor);

        editor.PressBlock("Approach Sensor");
        window.Tick();
        Layout(window);
        Record(seen, editor);

        editor.PressChoice("detects something");
        editor.PressPrimary();
        window.Tick();
        Layout(window);
        Record(seen, editor);

        editor.PressAdd("do something");
        editor.PressBlock("Landing Lights");
        editor.PressChoice("turn on");
        editor.PressPrimary();

        // The front door: the systems on this ship
        editor.GoToRoot();
        window.Tick();
        Layout(window);
        Record(seen, editor);

        // A question with a fixed set of answers. The starters used to be one of these and are
        // not any more, so the walk reaches for the loader instead.
        editor.PressStart("Load system");
        window.Tick();
        Layout(window);
        Record(seen, editor);

        Assert.Contains(nameof(PickComponent), seen);
        Assert.Contains(nameof(ChoiceListComponent), seen);
        Assert.Contains(nameof(StartComponent), seen);
        Assert.Contains(nameof(SequenceComponent), seen);
        Assert.Contains(nameof(StatusListComponent), seen);
        Assert.True(window.IsVisible);

        // Heading and Notice are not asserted: nothing produces one at the moment. They are
        // kept because R39's reported problems and M's diagnosis both want them, unlike the
        // block list, which the column path replaced outright and which is gone.
    }

    static void Record(HashSet<string> seen, Reactor.Editing.ReactorEditor editor)
    {
        foreach (var component in editor.Screen.Components)
            seen.Add(component.GetType().Name);
    }

    static void Layout(MainWindow window)
    {
        window.Measure(new Size(820, 680));
        window.Arrange(new Rect(0, 0, 820, 680));
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }
}
