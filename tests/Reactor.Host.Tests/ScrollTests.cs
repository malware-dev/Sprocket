using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Reactor.Host.Features.Shell;
using Xunit;

namespace Reactor.Host.Tests;

/// <summary>
/// A list taller than the column it sits in has to be reachable by scrolling.
/// </summary>
/// <remarks>
/// The column stacked its parts, and a stack measures every child against infinite height.
/// The rows scroller therefore grew to fit whatever was in it and had nothing left to
/// scroll, so a long block list simply ran off the bottom of the screen with no way down.
/// </remarks>
public class ScrollTests
{
    [AvaloniaFact]
    public void TheBlocksListScrollsWhenItIsTallerThanItsColumn()
    {
        var window = new MainWindow();
        window.Show();
        var editor = window.Editor;

        editor.NewSystem("Airlock");
        editor.PressStart("When...");
        Run(window);

        var scroller = window.GetVisualDescendants().OfType<ScrollViewer>()
            .First(s => s.Content is StackPanel rows && rows.Children.OfType<Button>().Any());

        Assert.True(scroller.Extent.Height > scroller.Viewport.Height,
            $"extent {scroller.Extent.Height:0} viewport {scroller.Viewport.Height:0}");
    }

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
