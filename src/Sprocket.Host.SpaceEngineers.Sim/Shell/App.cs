using System.Windows;

namespace Sprocket.Sim.Shell
{
    /// <summary>
    /// Built in code rather than in markup. There is one window and it hosts one canvas, so a XAML tree
    /// would be a file to keep in step for no benefit.
    /// </summary>
    public sealed class App : Application
    {
        /// <inheritdoc />
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnMainWindowClose;
            MainWindow = new MainWindow();
            MainWindow.Show();
        }
    }
}
