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
        var window = new MainWindow();
        window.Show();

        var editor = window.Editor;

        // What a brand new Reactor opens on.
        Beat(window, 1);
        Shoot(window, "start");

        BuildAirlock(window);

        // Let the sequence take a baseline sample before anything happens: a signal
        // already true when a sequence appears is not an edge.
        Beat(window, 3);

        var button = window.Simulation.World.SimDevices.OfType<SimButton>().First();
        button.Press();
        Beat(window, 8);

        Shoot(window, "sequence");

        editor.PressAction("Ship");
        Beat(window, 1);
        Shoot(window, "ship");

        editor.PressBlock("Airlock Outer Door");
        Beat(window, 1);
        Shoot(window, "block");

        editor.PressAction("Sequences");
        Beat(window, 1);
        Shoot(window, "sequences");

        // How often — labelled, blinking at the real rate, and warning about the
        // rates that cost the server dearly.
        editor.PressAction("Start something new");
        editor.PressChoice("Every so often");
        Beat(window, 1);
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
        editor.PressChoice("When something happens");
        Beat(window, 1);
        Shoot(window, "listen");

        editor.PressBlock("Airlock Button");
        Beat(window, 1);
        Shoot(window, "picked");

        editor.PressPrimary();
        editor.PressChoice("is pressed");

        Step(editor, 0, "Airlock Inner Door", "close");
        Step(editor, 1, "Airlock Inner Door", "lock");
        Step(editor, 2, "Airlock Vent", "pump the air out");
        Step(editor, 3, "Airlock Outer Door", "unlock");
        Step(editor, 4, "Airlock Outer Door", "open");
    }

    static void Step(ReactorEditor editor, int index, string block, string action)
    {
        editor.PressAdd("do something");

        editor.PressStepSlot(index, 1);
        editor.PressBlock(block);
        editor.PressPrimary();

        editor.PressStepSlot(index, 0);
        editor.PressChoice(action);
    }

    static void Beat(MainWindow window, int beats)
    {
        for (var i = 0; i < beats; i++)
            window.Beat();
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
