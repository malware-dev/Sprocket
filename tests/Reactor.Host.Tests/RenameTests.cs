using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Reactor.Host.Features.Shell;
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
        var window = new MainWindow();
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
