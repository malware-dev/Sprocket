using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Reactor.Editing;
using Reactor.Ui;

namespace Reactor.Host.Rendering;

/// <summary>
/// Draws a path as columns, in the manner of a file browser's column view.
/// </summary>
/// <remarks>
/// The whole point is that every stage stays on screen: the blocks you picked remain
/// visible while you choose what happens to them, and changing your mind about an
/// earlier stage is a glance to the left rather than a retreat through screens.
///
/// Each column scrolls on its own, because the lists are wildly different lengths -
/// several hundred blocks beside four verbs - and one shared scrollbar would be wrong
/// for whichever of them it was not tuned for. The row of columns scrolls sideways,
/// so a path longer than the window stays usable.
/// </remarks>
sealed class PathRenderer : ComponentRenderer
{
    const double PaneWidth = 250;

    sealed class PaneVisual
    {
        public TextBox? Search;
        public TextBlock Subtitle = null!;
        public WrapPanel Filters = null!;
        public StackPanel Options = null!;
        public StackPanel Rows = null!;
        public List<Ellipse?> Lights = [];
        public List<TextBlock> Details = [];
    }

    PathComponent _model;
    readonly StackPanel _columns = Widgets.Stack(Orientation.Horizontal);
    readonly ScrollViewer _root;
    readonly List<PaneVisual> _panes = [];

    public PathRenderer(ReactorEditor editor, PathComponent model) : base(editor)
    {
        _model = model;
        _root = new ScrollViewer
        {
            Content = _columns,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Build();
    }

    public override Control Control => _root;

    /// <summary>The columns scroll sideways, and each of them scrolls on its own.</summary>
    public override bool ScrollsItself => true;

    public override void Update()
    {
        for (var p = 0; p < _panes.Count && p < _model.Panes.Count; p++)
        {
            var pane = _model.Panes[p];
            var visual = _panes[p];

            for (var i = 0; i < visual.Lights.Count && i < pane.Entries.Count; i++)
            {
                var entry = pane.Entries[i];
                if (visual.Lights[i] != null)
                    visual.Lights[i]!.IsVisible = entry.IsActive;

                visual.Details[i].Text = Detail(entry);
            }
        }
    }

    static string Detail(PathEntry entry)
    {
        if (string.IsNullOrEmpty(entry.Secondary))
            return entry.Status ?? "";
        if (string.IsNullOrEmpty(entry.Status))
            return entry.Secondary;
        return entry.Secondary + "   " + entry.Status;
    }

    void Build()
    {
        // Search boxes survive a rebuild so typing is not interrupted; everything else
        // is cheap enough to make afresh.
        var kept = new List<TextBox?>();
        foreach (var visual in _panes)
            kept.Add(visual.Search);

        _columns.Children.Clear();
        _panes.Clear();

        for (var i = 0; i < _model.Panes.Count; i++)
            _columns.Children.Add(BuildPane(i, i < kept.Count ? kept[i] : null));

        Update();
    }

    Control BuildPane(int index, TextBox? existingSearch)
    {
        var pane = _model.Panes[index];
        var visual = new PaneVisual();
        var stack = Widgets.Stack();

        var title = Widgets.Label(pane.Title, Palette.Ink, 13, true);
        visual.Subtitle = Widgets.Label(pane.Subtitle, Palette.Faint, 11);
        stack.Children.Add(title);
        stack.Children.Add(visual.Subtitle);

        if (pane.SearchCommand != 0)
        {
            var command = pane.SearchCommand;
            var box = existingSearch ?? new TextBox
            {
                Background = Palette.Page,
                BorderBrush = Palette.Line,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 5),
                FontSize = 12
            };

            box.Watermark = "Search…";
            if (box.Text != pane.SearchText)
                box.Text = pane.SearchText;

            // The box outlives the panes, so what it is compared against is read from
            // whichever pane holds this column now, not the one that built it. A flag
            // set around the assignment above would be down again by the time Avalonia
            // raises the change, and our own echo would read as a keystroke.
            if (existingSearch == null)
                box.TextChanged += (_, _) =>
                {
                    var text = box.Text ?? "";
                    if (index >= _model.Panes.Count || text == (_model.Panes[index].SearchText ?? ""))
                        return;

                    Editor.SetText(command, text);
                };

            box.Margin = new Thickness(0, 8, 0, 0);
            visual.Search = box;
            stack.Children.Add(box);
        }

        visual.Filters = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
        foreach (var filter in pane.Filters)
        {
            var command = filter.Command;
            var chip = Widgets.Chip(filter.Label, filter.IsCurrent);
            chip.Click += (_, _) => Editor.Invoke(command);
            visual.Filters.Children.Add(chip);
        }
        stack.Children.Add(visual.Filters);

        // Choices about the selection itself sit above the rows they describe.
        visual.Options = Widgets.Stack();
        foreach (var option in pane.Options)
        {
            var command = option.Command;
            var button = Widgets.Ghost((option.IsCurrent ? "● " : "○ ") + option.Text);
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.HorizontalContentAlignment = HorizontalAlignment.Left;
            if (option.IsCurrent)
                button.Foreground = Palette.Ink;
            button.Click += (_, _) => Editor.Invoke(command);
            visual.Options.Children.Add(button);
        }
        stack.Children.Add(visual.Options);

        visual.Rows = Widgets.Stack();
        if (pane.Entries.Count == 0)
        {
            var empty = Widgets.Label(pane.EmptyText, Palette.Muted, 12);
            empty.Margin = new Thickness(2, 8, 0, 0);
            visual.Rows.Children.Add(empty);
        }

        foreach (var entry in pane.Entries)
            visual.Rows.Children.Add(BuildRow(pane, entry, visual));

        var scroll = new ScrollViewer
        {
            Content = visual.Rows,
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        stack.Children.Add(scroll);
        DockPanel.SetDock(scroll, Dock.Bottom);

        _panes.Add(visual);

        return new Border
        {
            Child = stack,
            Width = PaneWidth,
            Background = Palette.Panel,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 8, 0)
        };
    }

    Control BuildRow(PathPane pane, PathEntry entry, PaneVisual visual)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

        // A tick for things you collect, a dot for things you settle on.
        var mark = Widgets.Label(
            entry.IsChosen ? (pane.AllowsMany ? "✓" : "●") : (pane.AllowsMany ? "○" : "·"),
            entry.IsChosen ? Palette.Live : Palette.Faint, 13, true);
        mark.Margin = new Thickness(0, 0, 8, 0);
        grid.Children.Add(mark);

        var text = Widgets.Stack();
        text.Children.Add(Widgets.Label(entry.Text, Palette.Ink, 12));

        var detail = Widgets.Label(Detail(entry), Palette.Faint, 11);
        if (!string.IsNullOrEmpty(entry.Secondary) || !string.IsNullOrEmpty(entry.Status))
            text.Children.Add(detail);

        Grid.SetColumn(text, 1);
        grid.Children.Add(text);

        Ellipse? light = null;
        if (!string.IsNullOrEmpty(entry.Status))
        {
            light = new Ellipse
            {
                Width = 7,
                Height = 7,
                Fill = Palette.Live,
                IsVisible = entry.IsActive,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(light, 2);
            grid.Children.Add(light);
        }
        else if (entry.Badge > 0)
        {
            var badge = new Border
            {
                Child = Widgets.Label(entry.Badge.ToString(), Palette.Muted, 11),
                Background = Palette.SlotFill,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(6, 1),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(badge, 2);
            grid.Children.Add(badge);
        }

        visual.Lights.Add(light);
        visual.Details.Add(detail);

        var command = entry.Command;
        var button = Widgets.Row(grid);
        button.Padding = new Thickness(8, 6);
        button.Background = Brushes.Transparent;
        button.Click += (_, _) => Editor.Invoke(command);
        return button;
    }
}
