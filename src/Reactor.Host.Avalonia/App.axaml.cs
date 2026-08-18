using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Reactor.Host.Features.Shell;

namespace Reactor.Host;

public partial class App : Application
{
    /// <summary>
    /// One tick per frame, roughly matching the game's 60 simulation steps a second.
    /// </summary>
    static readonly TimeSpan TickInterval = TimeSpan.FromMilliseconds(16);

    DispatcherTimer? _clock;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = new MainWindow();
            desktop.MainWindow = window;

            _clock = new DispatcherTimer { Interval = TickInterval };
            _clock.Tick += (_, _) => window.Tick();
            _clock.Start();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
