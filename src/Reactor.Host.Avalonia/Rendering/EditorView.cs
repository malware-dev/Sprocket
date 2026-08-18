using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Reactor.Editing;
using Reactor.Ui;

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
    // A Border, not a ContentControl: a bare ContentControl has no template of its
    // own here and silently presents nothing, which renders every screen empty while
    // the header and footer carry on updating as if all were well.
    readonly Border _body = new();
    readonly StackPanel _actions = Widgets.Stack(Orientation.Horizontal);
    readonly TextBlock _title;
    readonly TextBlock _subtitle;
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

    readonly Dictionary<EditorComponent, ComponentRenderer> _byComponent = [];
    readonly List<EditorComponent> _shown = [];

    public EditorView(ReactorEditor editor)
    {
        _editor = editor;

        _title = Widgets.Label("", Palette.Ink, 16, true);
        _subtitle = Widgets.Label("", Palette.Faint, 11);

        _back = Widgets.Ghost("‹ Back");
        _back.Margin = new Thickness(0, 0, 10, 0);
        _back.Click += (_, _) => _editor.Invoke(_editor.Screen.BackCommand);

        Content = BuildLayout();
        Sync();
    }

    /// <summary>Brings the visuals in line with the editor. Called every tick.</summary>
    public void Sync()
    {
        var screen = _editor.Screen;

        if (!ShowingSameComponents(screen))
            Rebuild();

        _title.Text = screen.Title;
        _subtitle.Text = screen.Subtitle;
        _back.IsVisible = screen.CanGoBack;

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

        var headerGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        headerGrid.Children.Add(_back);
        Grid.SetColumn(titles, 1);
        headerGrid.Children.Add(titles);

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

        _body.Margin = new Thickness(12);

        var root = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(header);
        root.Children.Add(footer);
        root.Children.Add(_body);
        return root;
    }

    /// <summary>
    /// Whether the screen is still made of the very same components as last time.
    /// </summary>
    /// <remarks>
    /// Identity, not equality. Core hands back the same component instances whenever
    /// it can, so this is false only when the screen genuinely changed shape - which
    /// is what lets controls, and the focus and caret in them, survive everything
    /// else.
    /// </remarks>
    bool ShowingSameComponents(EditorScreen screen)
    {
        if (screen.Components.Count != _shown.Count)
            return false;

        for (var i = 0; i < _shown.Count; i++)
        {
            if (!ReferenceEquals(screen.Components[i], _shown[i]))
                return false;
        }
        return true;
    }

    void Rebuild()
    {
        var screen = _editor.Screen;

        _shown.Clear();
        _renderers.Clear();

        foreach (var component in screen.Components)
        {
            ComponentRenderer renderer;
            if (!_byComponent.TryGetValue(component, out renderer))
            {
                renderer = ComponentRenderer.Create(component, _editor);
                _byComponent[component] = renderer;
            }

            _shown.Add(component);
            _renderers.Add(renderer);
        }

        // Anything no longer on screen is dropped, so its renderer and its event
        // subscriptions go with it.
        if (_byComponent.Count > _shown.Count)
        {
            var stale = new List<EditorComponent>();
            foreach (var pair in _byComponent)
            {
                if (!_shown.Contains(pair.Key))
                    stale.Add(pair.Key);
            }
            foreach (var component in stale)
                _byComponent.Remove(component);
        }

        _body.Child = Arrange(screen, _renderers);

        RebuildActions();
    }

    /// <summary>
    /// Stacks the fixed parts and gives the rest of the height to whichever component
    /// asked for it.
    /// </summary>
    /// <remarks>
    /// Without this the whole screen scrolls, which carries the search box and the
    /// filters off the top along with the rows they filter. A screen with nothing
    /// claiming the space just scrolls as a whole, which is right for a form.
    /// </remarks>
    static Control Arrange(EditorScreen screen, List<ComponentRenderer> renderers)
    {
        // Renderers survive a rebuild, so their controls are still sitting in the
        // layout that is about to be thrown away. Avalonia refuses to re-parent a
        // control that still has one.
        foreach (var renderer in renderers)
            Detach(renderer.Control);

        var filler = -1;
        for (var i = 0; i < screen.Components.Count && i < renderers.Count; i++)
        {
            if (screen.Components[i].Fills)
            {
                filler = i;
                break;
            }
        }

        if (filler < 0)
        {
            var stack = Widgets.Stack();
            foreach (var renderer in renderers)
                stack.Children.Add(renderer.Control);

            return new ScrollViewer
            {
                Content = stack,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            };
        }

        var panel = new DockPanel();

        for (var i = 0; i < filler; i++)
        {
            DockPanel.SetDock(renderers[i].Control, Dock.Top);
            panel.Children.Add(renderers[i].Control);
        }

        // Anything after the filler is pinned to the bottom, added in reverse so it
        // still reads in the order core listed it.
        for (var i = renderers.Count - 1; i > filler; i--)
        {
            DockPanel.SetDock(renderers[i].Control, Dock.Bottom);
            panel.Children.Add(renderers[i].Control);
        }

        var content = renderers[filler].Control;
        panel.Children.Add(renderers[filler].ScrollsItself
            ? content
            : new ScrollViewer
            {
                Content = content,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
            });

        return panel;
    }

    static void Detach(Control control)
    {
        switch (control.Parent)
        {
            case Panel panel:
                panel.Children.Remove(control);
                break;
            case ContentControl holder when ReferenceEquals(holder.Content, control):
                holder.Content = null;
                break;
            case Decorator decorator when ReferenceEquals(decorator.Child, control):
                decorator.Child = null;
                break;
        }
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
