using Avalonia;
using Avalonia.Headless;
using Reactor.Sim.Shell;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Picking;
using Reactor.Screens.Sequences;

[assembly: AvaloniaTestApplication(typeof(Reactor.Screens.Tests.TestAppBuilder))]

namespace Reactor.Screens.Tests;

static class TestAppBuilder
{
    /// <summary>
    /// Headless, with real Skia drawing rather than the stub renderer, so text is measured the way it
    /// is measured when somebody is looking.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp() => AppBuilder
        .Configure<App>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
