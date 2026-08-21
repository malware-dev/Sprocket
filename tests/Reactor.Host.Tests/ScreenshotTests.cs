using Avalonia;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Reactor.Editing;
using Reactor.Host.Features.Shell;
using Reactor.Host.Machine;
using Xunit;

namespace Reactor.Host.Tests;

/// <summary>
/// Renders the editor to PNGs so the design can be reviewed without opening a window
/// on somebody's desktop.
/// </summary>
public class ScreenshotTests
{
    const int Width = 820;
    const int Height = 680;

    [AvaloniaFact]
    public void Capture()
    {
        var window = new MainWindow(Simulation.Fresh());
        window.Show();

        var editor = window.Editor;

        // What a brand new Reactor opens on.
        Tick(window, 1);
        Shoot(window, "start");

        BuildAirlock(window);

        // Let the sequence take a baseline sample before anything happens: a signal
        // already true when a sequence appears is not an edge.
        Tick(window, 3);

        var button = window.Simulation.World.SimEntities.OfType<SimButton>().First();
        button.Press();
        Tick(window, 8);

        Shoot(window, "sequence");

        // The front door: the machines on this ship, and the ones of each that are fitted.
        editor.GoToRoot();
        Tick(window, 1);
        Shoot(window, "ship");

        // How often - labelled, blinking at the real rate, and warning about the
        // rates that cost the server dearly.
        editor.NewSystem("Airlock");
        editor.PressStart("Every...");
        Tick(window, 1);
        Shoot(window, "rates");
    }

    /// <summary>
    /// The airlock as a player would say it: secure the side you are leaving, move
    /// the air, then release the side you are going to. Nothing states when each step
    /// happens, because the list already does.
    /// </summary>
    static void BuildAirlock(MainWindow window)
    {
        var editor = window.Editor;

        // Straight down the sentence: the verb, then what it listens to, then what
        // happens. Answering the verb lands directly on the block picker, with no
        // stop at a half-empty sentence in between.
        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        Tick(window, 1);
        Shoot(window, "listen");

        editor.PressBlock("Airlock Button");
        Tick(window, 1);
        Shoot(window, "picked");

        editor.PressChoice("is pressed");
        Tick(window, 1);
        Shoot(window, "path");

        editor.PressPrimary();

        Step(editor, "Airlock Inner Door", "close");
        Step(editor, "Airlock Inner Door", "lock");
        Step(editor, "Airlock Vent", "depressurize");
        Step(editor, "Airlock Outer Door", "unlock");
        Step(editor, "Airlock Outer Door", "open");

        editor.Name("Let me out");
    }

    static void Step(ReactorEditor editor, string block, string action)
    {
        editor.PressAdd("do something");
        editor.PressBlock(block);
        editor.PressChoice(action);
        editor.PressPrimary();
    }

    static void Tick(MainWindow window, int ticks)
    {
        for (var i = 0; i < ticks; i++)
            window.Tick();
    }

    static void Shoot(MainWindow window, string name)
    {
        window.Measure(new Size(Width, Height));
        window.Arrange(new Rect(0, 0, Width, Height));
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();

        var frame = window.CaptureRenderedFrame();
        Assert.NotNull(frame);
        frame.Save(Path.Combine(Path.GetTempPath(), $"reactor-{name}.png"));
    }
}
