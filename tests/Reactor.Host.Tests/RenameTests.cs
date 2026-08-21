using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Reactor.Host.Features.Shell;
using Reactor.Host.System;
using Xunit;

namespace Reactor.Host.Tests;

/// <summary>
/// The heading turns into the box it is edited in, and holds its place while it does.
/// </summary>
public class RenameTests
{
    [AvaloniaFact]
    public void PressingAnywhereOnTheHeadingEditsItInPlace()
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

        var header = Header(window);
        var before = header.Bounds.Height;

        // Pressed near the left edge of the heading, well away from the pencil: the whole
        // heading is the target, not the icon.
        var heading = window.GetVisualDescendants().OfType<Button>()
            .First(b => b.Content is Grid g && g.Children.OfType<TextBlock>().Any(t => t.FontSize > 14));
        var at = heading.TranslatePoint(new Point(8, heading.Bounds.Height / 2), window)!.Value;

        window.MouseDown(at, MouseButton.Left);
        window.MouseUp(at, MouseButton.Left);
        Run(window);

        var box = window.GetVisualDescendants().OfType<TextBox>().First(t => t.FontSize > 14);
        Assert.True(box.IsVisible, "the heading did not turn into a box");
        Assert.False(heading.IsVisible, "the heading is still showing behind the box");

        // The one thing an in-place swap can get wrong.
        Assert.Equal(before, header.Bounds.Height);

        box.Text = "Let me out";
        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        Run(window);

        Assert.Equal("Let me out", editor.Screen.CustomName);
        Assert.Equal("Let me out", editor.Screen.Title);
        Assert.True(heading.IsVisible);
        Assert.False(box.IsVisible);
        Assert.Equal(before, header.Bounds.Height);
    }

    /// <summary>
    /// The heading and Back line up, and the header does not resize as a screen changes.
    /// </summary>
    /// <remarks>
    /// Back used to be centred against the title and subtitle together, which put it half a
    /// line below the title on every screen with no subtitle. The subtitle's line stays
    /// reserved either way: a picker's empties the moment the pick is complete, and a header
    /// that shrank when it did would move the screen under somebody mid-task.
    /// </remarks>
    [AvaloniaFact]
    public void TheHeadingLinesUpWithBackAndTheHeaderHoldsItsHeight()
    {
        var window = new MainWindow(Simulation.Fresh());
        window.Show();
        var editor = window.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        editor.PressBlock("Airlock Button");
        Run(window);

        // Mid-pick: this screen has a subtitle.
        var tall = Header(window).Bounds.Height;
        Assert.NotEqual("", editor.Screen.Subtitle);

        editor.PressChoice("is pressed");
        Run(window);

        // Complete: the subtitle emptied, and the header must not have moved.
        Assert.Equal("", editor.Screen.Subtitle);
        Assert.Equal(tall, Header(window).Bounds.Height);

        editor.PressPrimary();
        Run(window);

        // A sequence never has one at all, and the title still lines up with Back.
        Assert.Equal(tall, Header(window).Bounds.Height);
        Assert.Equal(Middle(window, Back(window)), Middle(window, Title(window)), 1);
    }

    static double Middle(MainWindow window, Visual visual) =>
        visual.TranslatePoint(new Point(0, visual.Bounds.Height / 2), window)!.Value.Y;

    static TextBlock Title(MainWindow window) =>
        window.GetVisualDescendants().OfType<TextBlock>().First(t => t.FontSize > 14);

    static Button Back(MainWindow window) =>
        window.GetVisualDescendants().OfType<Button>().First(b => b.Content is "‹ Back");

    static Border Header(MainWindow window) =>
        window.GetVisualDescendants().OfType<Border>()
            .First(b => b.Child is Grid g && g.Children.OfType<Button>().Any());

    static void Run(MainWindow window)
    {
        for (var i = 0; i < 5; i++)
        {
            window.Tick();
            window.Measure(new Size(820, 480));
            window.Arrange(new Rect(0, 0, 820, 480));
            Dispatcher.UIThread.RunJobs();
        }
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
    }
}
