using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Reactor.Host.Machine;
using Reactor.Host.Rendering;

namespace Reactor.Host.Features.Shell;

/// <summary>
/// The prototype window: Reactor, plus the controls that stand in for the world.
/// </summary>
/// <remarks>
/// The simulate strip along the bottom is scaffolding, not part of the editor.
/// Tripping a sensor or draining a tank is something the world does; in Space
/// Engineers a ship actually flies up to the sensor, so none of it exists in core.
/// </remarks>
public class MainWindow : Window
{
    readonly Simulation _simulation = new();
    readonly EditorView _view;

    public MainWindow()
    {
        Title = "Reactor";
        Width = 820;
        Height = 680;
        MinWidth = 460;
        MinHeight = 420;
        Background = Palette.Page;

        _view = new EditorView(_simulation.Editor);

        var root = new DockPanel();
        var strip = BuildSimulateStrip();
        DockPanel.SetDock(strip, Dock.Bottom);
        root.Children.Add(strip);
        root.Children.Add(_view);
        Content = root;
    }

    /// <summary>The editor being rendered. Exposed so tests can drive it.</summary>
    internal Reactor.Editing.ReactorEditor Editor => _simulation.Editor;

    /// <summary>The simulated ship, so tests can make things happen to it.</summary>
    internal Simulation Simulation => _simulation;

    /// <summary>Advances the whole prototype by one tick.</summary>
    public void Tick()
    {
        _simulation.Tick();
        _view.Sync();
    }

    Control BuildSimulateStrip()
    {
        var row = Widgets.Stack(Orientation.Horizontal);
        row.Children.Add(Widgets.Label("Simulate:", Palette.Faint, 11));

        foreach (var device in _simulation.Pokeable())
        {
            switch (device)
            {
                case SimButton button:
                {
                    var press = Widgets.Ghost($"press {button.Name}");
                    press.Click += (_, _) => button.Press();
                    row.Children.Add(press);
                    break;
                }
                case SimSensor sensor:
                {
                    var trip = Widgets.Ghost($"trip {sensor.Name}");
                    var on = false;
                    trip.Click += (_, _) =>
                    {
                        on = !on;
                        sensor.SetDetecting(on);
                        trip.Foreground = on ? Palette.Live : Palette.Muted;
                    };
                    row.Children.Add(trip);
                    break;
                }
                case SimTank tank:
                {
                    var drain = Widgets.Ghost($"drain {tank.Name}");
                    drain.Click += (_, _) =>
                    {
                        tank.Draining = !tank.Draining;
                        drain.Foreground = tank.Draining ? Palette.Live : Palette.Muted;
                    };
                    row.Children.Add(drain);
                    break;
                }
            }
        }

        return new Border
        {
            Child = row,
            Background = Palette.Chrome,
            Padding = new Thickness(12, 6),
            BorderBrush = Palette.Line,
            BorderThickness = new Thickness(0, 1, 0, 0)
        };
    }
}
