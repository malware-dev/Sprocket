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
    readonly Button _switch;
    readonly TextBlock _switchText;
    readonly Button _heading;
    readonly TextBlock _renameIcon;
    readonly TextBox _renameBox;

    bool _renaming;

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

        // The same control the sentence carries, at the scale of a whole system, and drawn
        // the same way on purpose - it is the same act. Labelled with what pressing it does
        // rather than what state it is in, so nobody has to work out which way round it is
        // before daring to press.
        _switchText = Widgets.Label("", Brushes.White, 12, true);
        _switch = new Button
        {
            Content = _switchText,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(14, 5),
            Margin = new Thickness(0, 0, 8, 0)
        };
        _switch.Click += (_, _) =>
            _editor.SetToggle(_editor.Screen.SwitchCommand, !_editor.Screen.SwitchIsOn);

        _back = Widgets.Ghost("‹ Back");
        _back.Margin = new Thickness(0, 0, 10, 0);
        _back.Click += (_, _) => _editor.Invoke(_editor.Screen.BackCommand);

        // Editing happens where the name already is. A popup would put a second copy of the
        // heading over the top of the first and ask which one is real; the heading is right
        // there and already says the name, so it turns into the box rather than summoning one.
        // Same footprint either way - the header must not change height when it does.
        _renameBox = new TextBox
        {
            Background = Brushes.Transparent,
            BorderBrush = Palette.Accent,
            BorderThickness = new Thickness(0, 0, 0, 1),
            CornerRadius = new CornerRadius(0),
            Padding = new Thickness(6, 2),
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = Palette.Ink,
            MinWidth = 240,
            // The theme gives a text box a comfortable minimum that a heading does not have.
            // Left alone it stands six pixels taller than the title it replaces and the whole
            // header steps down as you click it.
            MinHeight = 0,
            IsVisible = false,
            // Empty is a real answer, not a missing one: it hands the heading back to whatever
            // the thing says about itself. So the box says what empty means rather than
            // nagging for something to be typed.
            Watermark = "no name"
        };

        // The pencil says the heading can be renamed; the heading itself is what you press.
        // An icon is a small target and a title is a large one, and somebody who has worked
        // out that the name is editable should not then have to aim.
        _heading = new Button
        {
            Background = Brushes.Transparent,
            // An invisible line exactly where the box will draw a real one, so swapping them
            // moves nothing.
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(6, 2),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left,
            HorizontalContentAlignment = HorizontalAlignment.Left
        };

        _renameIcon = Widgets.Label("✎", Palette.Faint, 13);
        _renameIcon.Margin = new Thickness(10, 0, 0, 0);
        _renameIcon.VerticalAlignment = VerticalAlignment.Center;

        _heading.Click += (_, _) => StartRenaming();

        // Enter and clicking away both mean the same thing - the name is what the box says -
        // because there is no separate confirm to press any more. Escape is the way out that
        // changes nothing.
        _renameBox.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
                StopRenaming(true);
            else if (e.Key == Avalonia.Input.Key.Escape)
                StopRenaming(false);
        };
        _renameBox.LostFocus += (_, _) => StopRenaming(true);

        Content = BuildLayout();
        Sync();
    }

    /// <summary>Brings the visuals in line with the editor. Called every tick.</summary>
    public void Sync()
    {
        var screen = _editor.Screen;

        if (!ShowingSameComponents(screen))
            Rebuild();

        _title.Text = Heading(screen);
        _subtitle.Text = screen.Subtitle;
        _back.IsVisible = screen.CanGoBack;

        // Inert rather than disabled on a screen that cannot be renamed: disabling would grey
        // out the title, and the title is the heading, not a control that happens to be off.
        _switch.IsVisible = screen.HasSwitch;
        _switchText.Text = screen.SwitchIsOn ? "❚❚  Pause" : "▶  Run";
        _switch.Background = screen.SwitchIsOn ? Palette.Notice : Palette.Live;

        _renameIcon.IsVisible = screen.CanRename;
        _heading.IsHitTestVisible = screen.CanRename;

        // Whatever is being typed is the truth while it is being typed. Writing the screen's
        // name back into the box on every tick would fight the caret for it.
        if (_renaming && !screen.CanRename)
            StopRenaming(false);

        _primary.IsVisible = screen.HasPrimary;
        _primary.Content = screen.PrimaryText;

        foreach (var renderer in _renderers)
            renderer.Update();
    }

    /// <summary>
    /// The whole heading as one line, and the first part of it that can be renamed.
    /// </summary>
    /// <remarks>
    /// <b>A stopgap, and a knowing one.</b> Core publishes a heading in parts because each part answers
    /// for itself whether it can be renamed - a sequence's trigger cannot and what it then does can - and
    /// a host that draws them properly gives each part its own control. This one does not yet: it flattens
    /// them into a line and offers the rename of whichever part comes first.
    /// <para>
    /// Which is exactly why core does not offer a joined-up string of its own. Flattening is a decision a
    /// target makes, and it should have to write it down where somebody can see it costing something.
    /// </para>
    /// </remarks>
    static string Heading(EditorScreen screen)
    {
        var joined = "";
        for (var i = 0; i < screen.Heading.Count; i++)
            joined += i > 0 ? " » " + screen.Heading[i].Text : screen.Heading[i].Text;

        return joined;
    }

    static HeadingPart? Nameable(EditorScreen screen)
    {
        for (var i = 0; i < screen.Heading.Count; i++)
        {
            if (screen.Heading[i].CanRename)
                return screen.Heading[i];
        }

        return null;
    }

    /// <summary>Turns the heading into the box, holding its place exactly.</summary>
    void StartRenaming()
    {
        if (_renaming || !_editor.Screen.CanRename)
            return;

        _renaming = true;
        _renameBox.Text = Nameable(_editor.Screen)?.Name ?? "";
        _heading.IsVisible = false;
        _renameBox.IsVisible = true;
        _renameBox.SelectAll();
        _renameBox.Focus();
    }

    /// <summary>
    /// Puts the heading back, and sends the name on only if that is what was meant.
    /// </summary>
    /// <remarks>
    /// The guard is not paranoia: hiding a focused box raises its own lost-focus, which lands
    /// straight back in here and would send the name a second time - and after Escape, would
    /// send the very edit that was just abandoned.
    /// </remarks>
    void StopRenaming(bool keep)
    {
        if (!_renaming)
            return;

        _renaming = false;
        var text = (_renameBox.Text ?? "").Trim();

        _renameBox.IsVisible = false;
        _heading.IsVisible = true;

        // Read at the moment of use. Command ids are re-issued on every rebuild, so one
        // captured when the box opened is already stale by the time typing has finished.
        var command = Nameable(_editor.Screen)?.RenameCommand ?? 0;
        if (keep && command != 0)
            _editor.SetText(command, text);
    }

    Control BuildLayout()
    {
        // The pencil is pinned to the right and the title takes whatever is left, ellipsised
        // if it has to be. Sized to the title instead, a long name pushed the pencil off the
        // end of the header and the screen simply stopped offering a rename - which is the
        // worst way to lose a control, because nothing says it was ever there.
        var headingGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        _title.TextTrimming = TextTrimming.CharacterEllipsis;
        headingGrid.Children.Add(_title);
        Grid.SetColumn(_renameIcon, 1);
        headingGrid.Children.Add(_renameIcon);

        _heading.Content = headingGrid;
        _heading.HorizontalAlignment = HorizontalAlignment.Stretch;

        // The two share a cell so the one that is showing decides the height and neither can
        // shift the other's position when they swap.
        var swap = new Grid();
        swap.Children.Add(_heading);
        swap.Children.Add(_renameBox);

        // Back sits on the title's row rather than beside the pair of them, so the two line up
        // whether or not this screen has a subtitle. Centring it against the block put it half
        // a line below the title on every screen that has none, and reserving the subtitle's
        // line is not optional: a picker's subtitle empties as you finish picking, and a header
        // that resized when it did would be the screen moving under somebody's hands.
        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto")
        };

        _back.VerticalAlignment = VerticalAlignment.Center;
        headerGrid.Children.Add(_back);

        Grid.SetColumn(swap, 1);
        headerGrid.Children.Add(swap);

        var right = Widgets.Stack(Orientation.Horizontal);
        right.VerticalAlignment = VerticalAlignment.Center;
        right.Children.Add(_switch);
        Grid.SetColumn(right, 2);
        headerGrid.Children.Add(right);

        // Indented to the heading button's own padding, so the title and the line under it
        // start at the same place instead of six pixels apart.
        _subtitle.Margin = new Thickness(6, 0, 0, 0);
        Grid.SetRow(_subtitle, 1);
        Grid.SetColumn(_subtitle, 1);
        headerGrid.Children.Add(_subtitle);

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
