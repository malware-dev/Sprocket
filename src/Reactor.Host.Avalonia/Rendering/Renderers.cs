using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Reactor.Editing;
using Reactor.Ui;

namespace Reactor.Host.Rendering;

sealed class HeadingRenderer : ComponentRenderer
{
    HeadingComponent _model;
    readonly TextBlock _text;

    public HeadingRenderer(ReactorEditor editor, HeadingComponent model) : base(editor)
    {
        _model = model;
        _text = Widgets.Label(model.Text, Palette.Muted, 12);
        _text.Margin = new Thickness(2, 8, 0, 4);
    }

    public override Control Control => _text;

    public override bool TryBind(EditorComponent component)
    {
        if (component is not HeadingComponent heading)
            return false;
        _model = heading;
        return true;
    }

    public override void Update() => _text.Text = _model.Text;
}

sealed class NoticeRenderer : ComponentRenderer
{
    NoticeComponent _model;
    readonly TextBlock _text;
    readonly Border _border;

    public NoticeRenderer(ReactorEditor editor, NoticeComponent model) : base(editor)
    {
        _model = model;
        _text = Widgets.Label(model.Text, Palette.Problem, 12);
        _border = Widgets.Wash(_text, Palette.ProblemWash);
        _border.Margin = new Thickness(0, 0, 0, 8);
        Update();
    }

    public override Control Control => _border;

    public override bool TryBind(EditorComponent component)
    {
        if (component is not NoticeComponent notice)
            return false;
        _model = notice;
        return true;
    }

    public override void Update()
    {
        _text.Text = _model.Text;
        var problem = _model.Level == NoticeLevel.Problem;
        _text.Foreground = problem ? Palette.Problem : Palette.Notice;
        _border.Background = problem ? Palette.ProblemWash : Palette.NoticeWash;
    }
}

/// <summary>
/// Search box plus kind filters.
/// </summary>
/// <remarks>
/// The text box deliberately survives structural rebuilds. Typing changes what the
/// list contains, which is a structural change - recreating the box on every
/// keystroke would throw away focus and the caret.
/// </remarks>
sealed class BlockSearchRenderer : ComponentRenderer
{
    BlockSearchComponent _model;
    readonly TextBox _box;
    readonly WrapPanel _filters;
    readonly DockPanel _root;
    bool _echo;

    public BlockSearchRenderer(ReactorEditor editor, BlockSearchComponent model) : base(editor)
    {
        _model = model;

        _box = new TextBox
        {
            Text = model.Text,
            Watermark = "Search…",
            Background = Palette.Panel,
            BorderBrush = Palette.Line,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10, 7),
            Margin = new Thickness(0, 0, 0, 8)
        };
        _box.TextChanged += (_, _) =>
        {
            if (_echo)
                return;
            Editor.SetText(_model.Command, _box.Text ?? "");
        };

        _filters = new WrapPanel();
        _root = new DockPanel();
        DockPanel.SetDock(_box, Dock.Top);
        DockPanel.SetDock(_filters, Dock.Top);
        _root.Children.Add(_box);
        _root.Children.Add(_filters);

        BuildFilters();
    }

    public override Control Control => _root;

    public override bool TryBind(EditorComponent component)
    {
        if (component is not BlockSearchComponent search)
            return false;

        _model = search;

        if (_box.Text != search.Text)
        {
            _echo = true;
            _box.Text = search.Text;
            _echo = false;
        }

        BuildFilters();
        return true;
    }

    public override void Update() { }

    void BuildFilters()
    {
        _filters.Children.Clear();
        foreach (var filter in _model.Filters)
        {
            var command = filter.Command;
            var chip = Widgets.Chip(filter.Label, filter.IsCurrent);
            chip.Click += (_, _) => Editor.Invoke(command);
            _filters.Children.Add(chip);
        }
    }
}

sealed class BlockListRenderer : ComponentRenderer
{
    sealed class Row
    {
        public Ellipse Lit = null!;
        public TextBlock Name = null!;
        public TextBlock Detail = null!;
        public TextBlock Count = null!;
        public Border Badge = null!;
    }

    BlockListComponent _model;
    readonly StackPanel _panel = Widgets.Stack();
    readonly List<Row> _rows = [];
    readonly TextBlock _empty;

    public BlockListRenderer(ReactorEditor editor, BlockListComponent model) : base(editor)
    {
        _model = model;
        _empty = Widgets.Label("", Palette.Muted, 12);
        _empty.Margin = new Thickness(4, 8, 0, 0);
        Build();
    }

    public override Control Control => _panel;

    public override bool TryBind(EditorComponent component)
    {
        if (component is not BlockListComponent list || list.Entries.Count != _model.Entries.Count)
            return false;

        _model = list;
        Build();
        return true;
    }

    public override void Update()
    {
        for (var i = 0; i < _rows.Count && i < _model.Entries.Count; i++)
        {
            var entry = _model.Entries[i];
            var row = _rows[i];

            row.Name.Text = entry.Name;
            row.Detail.Text = string.IsNullOrEmpty(entry.Status)
                ? entry.Kind
                : $"{entry.Kind}   {entry.Status}";
            row.Lit.IsVisible = entry.IsActive;
            row.Badge.IsVisible = entry.SequenceCount > 0;
            row.Count.Text = entry.SequenceCount.ToString();
        }
    }

    void Build()
    {
        _panel.Children.Clear();
        _rows.Clear();

        if (_model.Entries.Count == 0)
        {
            _empty.Text = _model.EmptyText ?? "";
            _panel.Children.Add(_empty);
            return;
        }

        var choosing = _model.Mode == BlockListMode.Choose;

        foreach (var entry in _model.Entries)
        {
            var row = new Row();
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto")
            };

            if (choosing)
            {
                // A glyph rather than a checkbox: the command already toggles, and
                // one press path is easier to reason about than two.
                var mark = Widgets.Label(entry.IsChosen ? "✓" : "○",
                    entry.IsChosen ? Palette.Live : Palette.Faint, 14, true);
                mark.Margin = new Thickness(0, 0, 10, 0);
                Grid.SetColumn(mark, 0);
                grid.Children.Add(mark);
                row.Lit = new Ellipse { IsVisible = false };
            }
            else
            {
                var dot = Widgets.Dot(out var lit);
                row.Lit = lit;
                Grid.SetColumn(dot, 0);
                grid.Children.Add(dot);
            }

            row.Name = Widgets.Label(entry.Name, Palette.Ink);
            row.Detail = Widgets.Label(entry.Kind, Palette.Faint, 11);

            var text = Widgets.Stack();
            text.Children.Add(row.Name);
            text.Children.Add(row.Detail);
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            row.Count = Widgets.Label("", Palette.Muted, 11);
            row.Badge = new Border
            {
                Child = row.Count,
                Background = Palette.SlotFill,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(7, 1),
                VerticalAlignment = VerticalAlignment.Center,
                IsVisible = false
            };
            Grid.SetColumn(row.Badge, 2);
            grid.Children.Add(row.Badge);

            var command = entry.Command;
            var button = Widgets.Row(grid);
            button.Click += (_, _) => Editor.Invoke(command);

            _panel.Children.Add(button);
            _rows.Add(row);
        }

        Update();
    }
}

sealed class ChoiceListRenderer : ComponentRenderer
{
    sealed class Row
    {
        public Ellipse? Blink;
        public int Beats;
    }

    ChoiceListComponent _model;
    readonly StackPanel _panel = Widgets.Stack();
    readonly List<Row> _rows = [];

    public ChoiceListRenderer(ReactorEditor editor, ChoiceListComponent model) : base(editor)
    {
        _model = model;
        Build();
    }

    public override Control Control => _panel;

    public override bool TryBind(EditorComponent component)
    {
        if (component is not ChoiceListComponent list || list.Entries.Count != _model.Entries.Count)
            return false;

        _model = list;
        Build();
        return true;
    }

    /// <summary>
    /// Drives the rate indicators. An interval of one beat is lit the whole time,
    /// which is exactly what "every beat" means.
    /// </summary>
    public override void Update()
    {
        foreach (var row in _rows)
        {
            if (row.Blink == null)
                continue;

            // A short flash rather than half the cycle. At 50% duty a 600-beat rate
            // would sit lit for ten seconds and read as "on" rather than as "rarely";
            // a brief blip reads as rare at every speed, and one beat reads as solid.
            var flash = Math.Min(6, Math.Max(1, row.Beats / 2));
            var on = row.Beats <= 1 || Editor.Beat % row.Beats < flash;
            row.Blink.Fill = on ? Palette.Live : Palette.Dim;
        }
    }

    void Build()
    {
        _panel.Children.Clear();
        _rows.Clear();

        if (_model.Entries.Count == 0)
        {
            var empty = Widgets.Label(_model.EmptyText, Palette.Muted, 12);
            empty.Margin = new Thickness(4, 4, 0, 0);
            _panel.Children.Add(empty);
            return;
        }

        foreach (var entry in _model.Entries)
        {
            var row = new Row { Beats = entry.BlinkBeats };
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

            if (entry.BlinkBeats > 0)
            {
                row.Blink = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Palette.Dim,
                    Margin = new Thickness(0, 0, 12, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                grid.Children.Add(row.Blink);
            }

            var text = Widgets.Stack();
            text.Children.Add(Widgets.Label(entry.Text, Palette.Ink));

            // Told as well as shown, and told before the choice rather than after it.
            if (!string.IsNullOrEmpty(entry.Warning))
                text.Children.Add(Widgets.Label(entry.Warning, Palette.Notice, 11));

            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            if (entry.IsCurrent)
            {
                var tick = Widgets.Label("✓", Palette.Live);
                Grid.SetColumn(tick, 2);
                grid.Children.Add(tick);
            }

            var command = entry.Command;
            var button = Widgets.Row(grid);
            button.Click += (_, _) => Editor.Invoke(command);

            _panel.Children.Add(button);
            _rows.Add(row);
        }

        Update();
    }
}

sealed class SequenceListRenderer : ComponentRenderer
{
    sealed class Row
    {
        public Ellipse Lit = null!;
        public TextBlock Summary = null!;
        public TextBlock Status = null!;
        public TextBlock Problem = null!;
    }

    SequenceListComponent _model;
    readonly StackPanel _panel = Widgets.Stack();
    readonly List<Row> _rows = [];

    public SequenceListRenderer(ReactorEditor editor, SequenceListComponent model) : base(editor)
    {
        _model = model;
        Build();
    }

    public override Control Control => _panel;

    public override bool TryBind(EditorComponent component)
    {
        if (component is not SequenceListComponent list || list.Entries.Count != _model.Entries.Count)
            return false;

        _model = list;
        Build();
        return true;
    }

    public override void Update()
    {
        for (var i = 0; i < _rows.Count && i < _model.Entries.Count; i++)
        {
            var entry = _model.Entries[i];
            var row = _rows[i];

            row.Summary.Text = entry.Summary;
            row.Status.Text = entry.Status;
            row.Lit.IsVisible = entry.Pulse;
            row.Problem.Text = entry.Problem;
            row.Problem.IsVisible = !string.IsNullOrEmpty(entry.Problem);
        }
    }

    void Build()
    {
        _panel.Children.Clear();
        _rows.Clear();

        if (_model.Entries.Count == 0)
        {
            var empty = Widgets.Label(_model.EmptyText, Palette.Muted, 12);
            empty.Margin = new Thickness(4, 4, 0, 0);
            _panel.Children.Add(empty);
            return;
        }

        foreach (var entry in _model.Entries)
        {
            var row = new Row();
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };

            var dot = Widgets.Dot(out var lit);
            row.Lit = lit;
            grid.Children.Add(dot);

            row.Summary = Widgets.Label("", Palette.Ink);
            row.Status = Widgets.Label("", Palette.Faint, 11);
            row.Problem = Widgets.Label("", Palette.Problem, 11);
            row.Problem.IsVisible = false;

            var text = Widgets.Stack();
            text.Children.Add(row.Summary);
            text.Children.Add(row.Status);
            text.Children.Add(row.Problem);
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            var command = entry.Command;
            var button = Widgets.Row(grid);
            button.Click += (_, _) => Editor.Invoke(command);

            _panel.Children.Add(button);
            _rows.Add(row);
        }

        Update();
    }
}
