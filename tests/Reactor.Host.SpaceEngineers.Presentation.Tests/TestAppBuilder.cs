using Avalonia;
using Avalonia.Headless;
using Reactor.Sim.Shell;

[assembly: AvaloniaTestApplication(typeof(Reactor.Host.SpaceEngineers.Presentation.Tests.TestAppBuilder))]

namespace Reactor.Host.SpaceEngineers.Presentation.Tests;

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
