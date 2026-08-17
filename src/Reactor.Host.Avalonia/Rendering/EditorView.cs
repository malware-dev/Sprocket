using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Reactor.Editing;

namespace Reactor.Host.Rendering;

/// <summary>
/// Renders an <see cref="ReactorEditor"/> on this target.
/// </summary>
/// <remarks>
/// The whole of this host's editor. It draws the screen chrome, owns one renderer
/// per component, and sends command ids back. It decides nothing about what the
/// editor does - a second target reimplements this file and nothing else.
/// </remarks>
sealed class EditorView : UserControl
{
    readonly ReactorEditor _editor;
    readonly List<ComponentRenderer> _renderers = [];
    readonly StackPanel _components = Widgets.Stack();
    readonly StackPanel _actions = Widgets.Stack(Orientation.Horizontal);
    readonly TextBlock _title;
    readonly TextBlock _subtitle;
    readonly TextBlock _beat;
    readonly Button _back;

    readonly Button _primary = new()
    {
        Background = Palette.Accent,
        Foreground = Brushes.White,
        BorderThickness = new Thickness(0),
        Padding = new Thickness(16, 5),
        CornerRadius = new CornerRadius(4),
        IsVisible = false
    };

    int _renderedVersion = -1;

    public EditorView(ReactorEditor editor)
    {
        _editor = editor;

        _title = Widgets.Label("", Palette.Ink, 16, true);
        _subtitle = Widgets.Label("", Palette.Faint, 11);
        _beat = Widgets.Label("", Palette.Faint, 11);
        _beat.FontFamily = new FontFamily("monospace");

        _back = Widgets.Ghost("‹ Back");
        _back.Margin = new Thickness(0, 0, 10, 0);
        _back.Click += (_, _) => _editor.Invoke(_editor.Screen.BackCommand);

        Content = BuildLayout();
        Sync();
    }

    /// <summary>Brings the visuals in line with the editor. Called every beat.</summary>
    public void Sync()
    {
        var screen = _editor.Screen;

        if (screen.StructureVersion != _renderedVersion)
        {
            Rebuild();
            _renderedVersion = screen.StructureVersion;
        }

        _title.Text = screen.Title;
        _subtitle.Text = screen.Subtitle;
        _back.IsVisible = screen.CanGoBack;
        _beat.Text = $"beat {_editor.Beat}";

        _primary.IsVisible = screen.HasPrimary;
        _primary.Content = screen.PrimaryText;

        foreach (var renderer in _renderers)
            renderer.Update();
    }

    Control BuildLayout()
    {
        var titles = Widgets.Stack();
        titles.VerticalAlignment = VerticalAlignment.Center;
        titles.Children.Add(_title);
        titles.Children.Add(_subtitle);

        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };
        headerGrid.Children.Add(_back);
        Grid.SetColumn(titles, 1);
        headerGrid.Children.Add(titles);
        Grid.SetColumn(_beat, 2);
        headerGrid.Children.Add(_beat);

        var header = new Border
        {
            Child = headerGrid,
            Background = Palette.Chrome,
            Padding = new Thickness(12, 9),
            BorderBrush = Palette.Line,
            BorderThickness = new Thickness(0, 0, 0, 1)
        };

        // Moving on sits apart from the navigation, on the side the eye ends up.
        _primary.Click += (_, _) => _editor.Invoke(_editor.Screen.PrimaryCommand);
        Grid.SetColumn(_primary, 1);

        var footerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        footerGrid.Children.Add(_actions);
        footerGrid.Children.Add(_primary);

        var footer = new Border
        {
            Child = footerGrid,
            Background = Palette.Chrome,
            Padding = new Thickness(10, 7),
            BorderBrush = Palette.Line,
            BorderThickness = new Thickness(0, 1, 0, 0)
        };

        var scroll = new ScrollViewer
        {
            Content = _components,
            Margin = new Thickness(12),
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };

        var root = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(header);
        root.Children.Add(footer);
        root.Children.Add(scroll);
        return root;
    }

    void Rebuild()
    {
        var screen = _editor.Screen;
        var reused = new List<ComponentRenderer>();

        for (var i = 0; i < screen.Components.Count; i++)
        {
            var component = screen.Components[i];

            // Reuse the renderer sitting in the same slot when it can take the new
            // component. That is what lets a search box keep focus and a caret while
            // typing changes the list beneath it.
            if (i < _renderers.Count && _renderers[i].TryBind(component))
            {
                reused.Add(_renderers[i]);
                continue;
            }

            reused.Add(ComponentRenderer.Create(component, _editor));
        }

        _components.Children.Clear();
        _renderers.Clear();

        foreach (var renderer in reused)
        {
            _renderers.Add(renderer);
            _components.Children.Add(renderer.Control);
        }

        RebuildActions();
    }

    void RebuildActions()
    {
        _actions.Children.Clear();

        foreach (var action in _editor.Screen.Actions)
        {
            var command = action.Command;
            var button = Widgets.Ghost(action.Text);
            button.Margin = new Thickness(0, 0, 8, 0);

            if (action.IsCurrent)
                button.Foreground = Palette.Ink;

            button.Click += (_, _) => _editor.Invoke(command);
            _actions.Children.Add(button);
        }
    }
}
