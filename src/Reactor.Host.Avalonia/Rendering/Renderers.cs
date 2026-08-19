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

    public override void Update()
    {
        _text.Text = _model.Text;
        var problem = _model.Level == NoticeLevel.Problem;
        _text.Foreground = problem ? Palette.Problem : Palette.Notice;
        _border.Background = problem ? Palette.ProblemWash : Palette.NoticeWash;
    }
}

/// <summary>
/// The ways a machine comes into being, and how this host asks for what they need.
/// </summary>
/// <remarks>
/// Core said a name is needed and gave somewhere to send it. Everything below - that it opens
/// under the button, that Escape abandons it, that Continue is dead until there is something
/// to continue with - is this host deciding what that should feel like. Another target may
/// use a modal, a terminal prompt, or a line at the top of the screen, and core will not know.
/// </remarks>
sealed class StartRenderer : ComponentRenderer
{
    /// <summary>
    /// Everything in here adds something, so everything in here gets a plus.
    /// </summary>
    /// <remarks>
    /// Core does not spell the plus into the labels. It says these are the ways to start
    /// something, which is already the whole of the claim - drawing that as a plus, here and
    /// on the "add" rows of a list, is this host being consistent with itself. A target with
    /// no room for it can leave it off and lose nothing.
    /// </remarks>
    public const string Plus = "+ ";

    readonly StartComponent _model;
    readonly StackPanel _root = Widgets.Stack();

    public StartRenderer(ReactorEditor editor, StartComponent model) : base(editor)
    {
        _model = model;

        // A line under the whole block, because what follows it is a different kind of thing:
        // above is what you can add, below is what is already there.
        _root.Children.Add(new Border
        {
            Height = 1,
            Background = Palette.Line,
            Margin = new Thickness(0, 8, 0, 8)
        });

        foreach (var option in model.Options)
            _root.Children.Insert(_root.Children.Count - 1, BuildOption(option));
    }

    Control BuildOption(StartOption option)
    {
        var button = Widgets.Row(Widgets.Label(Plus + option.Label, Palette.Ink));

        if (!option.Asks)
        {
            button.Click += (_, _) => Editor.Invoke(option.Command);
            return button;
        }

        // No example. The question sits directly above the box, and any example specific
        // enough to be useful reads as a requirement - somebody would wonder whether it has
        // to be an airlock.
        var box = new TextBox
        {
            Background = Palette.Page,
            BorderBrush = Palette.Line,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 5),
            FontSize = 13,
            MinWidth = 220
        };

        var confirm = new Button
        {
            Content = "Continue",
            Background = Palette.Accent,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Padding = new Thickness(14, 5),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(8, 0, 0, 0),
            IsEnabled = false
        };

        var row = new DockPanel();
        DockPanel.SetDock(confirm, Dock.Right);
        row.Children.Add(confirm);
        row.Children.Add(box);

        var inside = Widgets.Stack();
        inside.Children.Add(Widgets.Label(option.Question, Palette.Muted, 11));
        inside.Children.Add(row);

        // A flyout rather than a panel in the stack: it hangs over the screen instead of
        // shoving it down, it is anchored to the thing that opened it, and clicking away
        // dismisses it. None of which core knows or should.
        var flyout = new Flyout
        {
            Content = inside,
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            ShowMode = FlyoutShowMode.Transient
        };

        Action confirmed = () =>
        {
            var answer = (box.Text ?? "").Trim();
            if (answer.Length == 0)
                return;

            flyout.Hide();
            Editor.SetText(option.Command, answer);
        };

        box.TextChanged += (_, _) => confirm.IsEnabled = !string.IsNullOrWhiteSpace(box.Text);
        box.KeyDown += (_, e) =>
        {
            if (e.Key == Avalonia.Input.Key.Enter)
                confirmed();
            else if (e.Key == Avalonia.Input.Key.Escape)
                flyout.Hide();
        };
        confirm.Click += (_, _) => confirmed();

        flyout.Opened += (_, _) =>
        {
            box.Text = "";
            confirm.IsEnabled = false;
            box.Focus();
        };

        button.Flyout = flyout;
        return button;
    }

    public override Control Control => _root;

    public override void Update()
    {
    }
}

sealed class ChoiceListRenderer : ComponentRenderer
{
    int _seenRevision = -1;

    sealed class Row
    {
        public Ellipse? Blink;
        public int Ticks;
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

    /// <summary>
    /// Drives the rate indicators. An interval of one tick is lit the whole time,
    /// which is exactly what "every tick" means.
    /// </summary>
    public override void Update()
    {
        // Rebuilt here rather than from the list's own event, which fires while
        // core is still mid-update and would read half-written rows.
        if (_seenRevision != _model.Entries.Revision)
        {
            _seenRevision = _model.Entries.Revision;
            Build();
        }

        foreach (var row in _rows)
        {
            if (row.Blink == null)
                continue;

            // A short flash rather than half the cycle. At 50% duty a 600-tick rate
            // would sit lit for ten seconds and read as "on" rather than as "rarely";
            // a brief blip reads as rare at every speed, and one tick reads as solid.
            var flash = Math.Min(6, Math.Max(1, row.Ticks / 2));
            var on = row.Ticks <= 1 || Editor.CurrentTick % row.Ticks < flash;
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
            var row = new Row { Ticks = entry.IntervalTicks };
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

            if (entry.IntervalTicks > 0)
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

sealed class StatusListRenderer : ComponentRenderer
{
    int _seenRevision = -1;

    sealed class Row
    {
        public Ellipse Lit = null!;
        public TextBlock Summary = null!;
        public TextBlock Status = null!;
        public TextBlock LastRun = null!;
        public TextBlock Problem = null!;
    }

    StatusListComponent _model;
    readonly StackPanel _panel = Widgets.Stack();
    readonly List<Row> _rows = [];

    public StatusListRenderer(ReactorEditor editor, StatusListComponent model) : base(editor)
    {
        _model = model;
        _model.Entries.Changed += Build;
        Build();
    }

    public override Control Control => _panel;

    public override void Update()
    {
        // Rebuilt here rather than from the list's own event, which fires while
        // core is still mid-update and would read half-written rows.
        if (_seenRevision != _model.Entries.Revision)
        {
            _seenRevision = _model.Entries.Revision;
            Build();
        }

        for (var i = 0; i < _rows.Count && i < _model.Entries.Count; i++)
        {
            var entry = _model.Entries[i];
            var row = _rows[i];

            // The same plus the start block uses, for the same reason: this row adds
            // something. Core marks it as an action and leaves the drawing to us.
            row.Summary.Text = entry.IsAction ? StartRenderer.Plus + entry.Summary : entry.Summary;
            row.Status.Text = entry.Status;
            row.LastRun.Text = entry.LastRun;
            row.Lit.IsVisible = entry.JustRan;
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
            // A row that does something has no state to show, so it gets no light - only the
            // space where one would be, which keeps its text aligned with its neighbours.
            dot.IsVisible = !entry.IsAction;
            grid.Children.Add(dot);

            row.Summary = Widgets.Label("", entry.IsAction ? Palette.Muted : Palette.Ink);
            row.Status = Widgets.Label("", Palette.Faint, 11);
            row.LastRun = Widgets.Label("", Palette.Faint, 11);
            row.LastRun.Margin = new Thickness(8, 0, 0, 0);
            row.Problem = Widgets.Label("", Palette.Problem, 11);
            row.Problem.IsVisible = false;

            var line = Widgets.Stack(Orientation.Horizontal);
            line.Children.Add(row.Status);
            line.Children.Add(row.LastRun);

            var text = Widgets.Stack();
            text.Children.Add(row.Summary);
            text.Children.Add(line);
            text.Children.Add(row.Problem);
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            var command = entry.Command;

            // No command means there is nothing to press, so it is not drawn as something that
            // can be. A machine's own row is a heading over the ones of it, and a heading that
            // lights up under the pointer is a promise it cannot keep.
            if (command == 0)
            {
                _panel.Children.Add(new Border
                {
                    Child = grid,
                    Padding = new Thickness(12, 9),
                    Margin = new Thickness(entry.Depth > 0 ? 22 : 0, 0, 0, 4)
                });
                _rows.Add(row);
                continue;
            }

            var button = Widgets.Row(grid);

            // One level of nesting, ever: a machine, and the ones of it on this ship.
            if (entry.Depth > 0)
                button.Margin = new Thickness(22, 0, 0, 4);

            button.Click += (_, _) => Editor.Invoke(command);

            _panel.Children.Add(button);
            _rows.Add(row);
        }

        Update();
    }
}
