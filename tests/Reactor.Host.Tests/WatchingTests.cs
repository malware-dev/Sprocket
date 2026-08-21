using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Reactor.Host.Features.Shell;
using Reactor.Host.System;
using Xunit;

namespace Reactor.Host.Tests;

/// <summary>
/// A running sequence is drawn as a thing being watched, not as an editor with its hands tied.
/// </summary>
/// <remarks>
/// Core withholding the commands is tested on its own side. This is the other half, and it is
/// the half that rots quietly: nothing else notices if a renderer starts drawing arrows and
/// crosses that cannot do anything, and the first attempt at this greyed them instead of
/// removing them, which read as broken rather than as a readout.
/// </remarks>
public class WatchingTests
{
    [AvaloniaFact]
    public void TheEditingAffordancesAreAbsentWhileItRuns()
    {
        var window = new MainWindow(Simulation.Fresh());
        window.Show();
        var editor = window.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("turn on");
        editor.PressPrimary();
        Run(window);

        // Parked, which is how a new sequence starts: the whole toolkit is there.
        Assert.True(HasGrip(window), "no drag grip while parked");
        Assert.True(Shows(window, "✕"), "no remove button while parked");
        Assert.True(Shows(window, "+ do something"), "no add button while parked");
        Assert.True(Shows(window, "Delete"), "no delete while parked");

        editor.SwitchOn();
        Run(window);

        // Running: gone, not greyed.
        Assert.False(HasGrip(window), "drag grip still drawn while running");
        Assert.False(Shows(window, "✕"), "remove button still drawn while running");
        Assert.False(Shows(window, "+ do something"), "add button still drawn while running");
        Assert.False(Shows(window, "Delete"), "delete still drawn while running");

        // Nothing visible is left behind wearing the disabled look, which was the first wrong
        // answer to this: grey says "broken, or not yours to touch", and neither is true of a
        // system that is simply running.
        Assert.DoesNotContain(
            window.GetVisualDescendants().OfType<Button>().Where(b => b.IsVisible),
            b => !b.IsEnabled);

        editor.SetToggle(editor.Sentence().ToggleCommand, false);
        Run(window);

        // Back to parked, and the toolkit returns rather than staying gone.
        Assert.True(HasGrip(window), "drag grip did not come back");
        Assert.True(Shows(window, "+ do something"), "add button did not come back");
    }

    /// <summary>
    /// The sentence stays readable while it runs, because reading it is the point.
    /// </summary>
    [AvaloniaFact]
    public void TheSentenceIsStillFullyLegibleWhileItRuns()
    {
        var window = new MainWindow(Simulation.Fresh());
        window.Show();
        var editor = window.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
        editor.SwitchOn();
        Run(window);

        var words = window.GetVisualDescendants().OfType<Button>()
            .First(b => b.Content is "Airlock Button");

        // Not dimmed and not disabled - just not pressable, because there is nothing there
        // to press.
        Assert.True(words.IsEnabled);
        Assert.Equal(1.0, words.Opacity);
        Assert.False(words.IsHitTestVisible);
    }

    /// <summary>
    /// The undo pair holds its place whether or not there is anything to walk back.
    /// </summary>
    /// <remarks>
    /// Hiding them would move Pause and Delete sideways every time somebody made or undid a
    /// change - under the pointer they were about to use.
    /// </remarks>
    [AvaloniaFact]
    public void TheUndoPairHoldsItsPlaceWithNothingToUndo()
    {
        var window = new MainWindow(Simulation.Fresh());
        window.Show();
        var editor = window.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        editor.PressChoice("is pressed");
        editor.PressPrimary();
        Run(window);

        var undo = Arrow(window, "↶");
        var redo = Arrow(window, "↷");
        var placed = new[] { undo.Bounds, redo.Bounds };

        // Freshly built: nothing to walk back either way, but both are there and quiet.
        Assert.True(undo.IsVisible);
        Assert.True(redo.IsVisible);
        Assert.False(undo.IsEnabled);
        Assert.False(redo.IsEnabled);

        editor.PressAdd("do something");
        editor.PressBlock("Airlock Light");
        editor.PressChoice("turn on");
        editor.PressPrimary();
        Run(window);

        undo = Arrow(window, "↶");
        redo = Arrow(window, "↷");
        Assert.True(undo.IsEnabled);
        Assert.False(redo.IsEnabled);
        Assert.Equal(placed[0], undo.Bounds);
        Assert.Equal(placed[1], redo.Bounds);

        editor.Invoke(editor.Sentence().UndoCommand);
        Run(window);

        undo = Arrow(window, "↶");
        redo = Arrow(window, "↷");
        Assert.False(undo.IsEnabled);
        Assert.True(redo.IsEnabled);
        Assert.Equal(placed[0], undo.Bounds);
        Assert.Equal(placed[1], redo.Bounds);
    }

    static Button Arrow(MainWindow window, string glyph) =>
        window.GetVisualDescendants().OfType<Button>().First(b => Equals(b.Content, glyph));

    static bool HasGrip(MainWindow window) =>
        window.GetVisualDescendants().OfType<TextBlock>().Any(t => t.IsVisible && t.Text == "⋮⋮");

    static bool Shows(MainWindow window, string content) =>
        window.GetVisualDescendants().OfType<Button>()
            .Any(b => b.IsVisible && Equals(b.Content, content));

    static void Run(MainWindow window)
    {
        for (var i = 0; i < 5; i++)
        {
            window.Tick();
            window.Measure(new Size(820, 680));
            window.Arrange(new Rect(0, 0, 820, 680));
            Dispatcher.UIThread.RunJobs();
        }
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }
}
