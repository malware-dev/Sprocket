using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;
using Reactor.Editing;
using Reactor.Ui;

namespace Reactor.Host.Rendering;

/// <summary>
/// The sequence itself: the sentence, its steps, and its live state.
/// </summary>
/// <remarks>
/// The status here is not instrumentation bolted on afterwards - it is the reason
/// this is more than an editor. Watching a sequence arm, fire, sit on step 3 and skip a
/// re-trigger is what saves the user from having to run the program in their head.
/// </remarks>
sealed class SequenceRenderer : ComponentRenderer
{
    sealed class StepVisual
    {
        public Border Frame = null!;
        public List<Button> Slots = [];
    }

    SequenceComponent _model;
    readonly StackPanel _root = Widgets.Stack();
    readonly List<StepVisual> _steps = [];
    readonly List<Button> _triggerSlots = [];

    Ellipse _ranLight = null!;
    TextBlock _status = null!;
    TextBlock _lastRun = null!;
    TextBlock _skipped = null!;
    Border _skippedBadge = null!;
    TextBlock _problem = null!;
    Border _problemBanner = null!;
    Border _liveBadge = null!;
    Button _enable = null!;

    public SequenceRenderer(ReactorEditor editor, SequenceComponent model) : base(editor)
    {
        _model = model;
        Build();
    }

    public override Control Control => _root;

    public override void Update()
    {
        _status.Text = _model.Status;
        _lastRun.Text = _model.LastRun;
        _ranLight.IsVisible = _model.JustRan;

        _skippedBadge.IsVisible = _model.SkippedCount > 0;
        _skipped.Text = $"skipped {_model.SkippedCount}× while busy";

        var hasProblem = !string.IsNullOrEmpty(_model.Problem);
        _problemBanner.IsVisible = hasProblem;
        _problem.Text = _model.Problem;

        _liveBadge.IsVisible = _model.Trigger.IsLiveNow;
        _enable.Content = _model.Enabled ? "on" : "off";
        _enable.Foreground = _model.Enabled ? Palette.Live : Palette.Faint;

        for (var i = 0; i < _triggerSlots.Count && i < _model.Trigger.Slots.Count; i++)
            _triggerSlots[i].Content = _model.Trigger.Slots[i].Text;

        for (var i = 0; i < _steps.Count && i < _model.Steps.Count; i++)
        {
            var step = _model.Steps[i];
            _steps[i].Frame.Background = step.IsCurrent ? Palette.LiveWash : Brushes.Transparent;

            for (var s = 0; s < _steps[i].Slots.Count && s < step.Slots.Count; s++)
                _steps[i].Slots[s].Content = step.Slots[s].Text;
        }
    }

    bool SameShape(SequenceComponent other)
    {
        if (other.Steps.Count != _model.Steps.Count)
            return false;
        if (other.Trigger.Slots.Count != _model.Trigger.Slots.Count)
            return false;

        for (var i = 0; i < other.Steps.Count; i++)
        {
            if (other.Steps[i].Slots.Count != _model.Steps[i].Slots.Count)
                return false;
        }
        return true;
    }

    void Build()
    {
        _root.Children.Clear();
        _steps.Clear();
        _triggerSlots.Clear();

        _root.Children.Add(BuildHeader());
        _root.Children.Add(BuildProblem());
        _root.Children.Add(BuildTrigger());

        foreach (var step in _model.Steps)
            _root.Children.Add(BuildStep(step));

        _root.Children.Add(BuildAdders());

        Update();
    }

    Control BuildHeader()
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto"),
            Margin = new Thickness(0, 0, 0, 10)
        };

        _status = Widgets.Label("", Palette.Muted, 12);
        _lastRun = Widgets.Label("", Palette.Faint, 12);
        _lastRun.Margin = new Thickness(8, 0, 0, 0);

        var state = Widgets.Stack(Orientation.Horizontal);
        state.Children.Add(_status);
        state.Children.Add(_lastRun);
        Grid.SetColumn(state, 0);
        grid.Children.Add(state);

        _skipped = Widgets.Label("", Palette.Notice, 11);
        _skippedBadge = new Border
        {
            Child = _skipped,
            Background = Palette.NoticeWash,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 2),
            Margin = new Thickness(10, 0, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false
        };
        Grid.SetColumn(_skippedBadge, 1);
        grid.Children.Add(_skippedBadge);

        // Standing in for whatever would normally set this off, so an airlock can be
        // tried without walking to the button.
        var run = Widgets.Ghost("Run now");
        run.Click += (_, _) => Editor.Invoke(_model.RunCommand);
        Grid.SetColumn(run, 2);
        grid.Children.Add(run);

        _enable = Widgets.Ghost("on");
        _enable.Click += (_, _) => Editor.SetToggle(_model.ToggleCommand, !_model.Enabled);
        Grid.SetColumn(_enable, 3);
        grid.Children.Add(_enable);

        var delete = Widgets.Ghost("Delete");
        delete.Click += (_, _) => Editor.Invoke(_model.DeleteCommand);
        Grid.SetColumn(delete, 4);
        grid.Children.Add(delete);

        return grid;
    }

    Control BuildProblem()
    {
        _problem = Widgets.Label("", Palette.Problem, 12);
        _problemBanner = Widgets.Wash(_problem, Palette.ProblemWash);
        _problemBanner.Margin = new Thickness(0, 0, 0, 10);
        _problemBanner.IsVisible = false;
        return _problemBanner;
    }

    Control BuildTrigger()
    {
        var wrap = new WrapPanel { Margin = new Thickness(0, 0, 0, 8) };

        var dot = Widgets.Dot(out _ranLight);
        dot.Margin = new Thickness(0, 0, 10, 4);
        wrap.Children.Add(dot);

        var lead = Widgets.Label(_model.Trigger.Lead, Palette.Muted, 15);
        lead.Margin = new Thickness(0, 0, 6, 4);
        wrap.Children.Add(lead);

        foreach (var slot in _model.Trigger.Slots)
        {
            var command = slot.Command;
            var button = Widgets.Slot(slot.Text);
            if (slot.IsEmpty)
                button.FontStyle = FontStyle.Italic;
            button.Click += (_, _) => Editor.Invoke(command);
            wrap.Children.Add(button);
            _triggerSlots.Add(button);
        }

        // Idle while the signal reads true is not a contradiction - it is the
        // difference between a moment and a situation, shown rather than explained.
        _liveBadge = new Border
        {
            Child = Widgets.Label("true right now", Palette.Live, 11),
            Background = Palette.LiveWash,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(8, 2),
            Margin = new Thickness(0, 0, 6, 4),
            VerticalAlignment = VerticalAlignment.Center,
            IsVisible = false
        };
        wrap.Children.Add(_liveBadge);


        return wrap;
    }

    Control BuildStep(SentenceStep step)
    {
        var visual = new StepVisual();
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto") };

        var number = Widgets.Label(step.Number.ToString(), Palette.Faint);
        number.Width = 20;
        grid.Children.Add(number);

        var wrap = new WrapPanel();
        if (!string.IsNullOrEmpty(step.Lead))
        {
            var lead = Widgets.Label(step.Lead, Palette.Muted);
            lead.Margin = new Thickness(0, 0, 6, 4);
            wrap.Children.Add(lead);
        }

        foreach (var slot in step.Slots)
        {
            var command = slot.Command;
            var button = Widgets.Slot(slot.Text);
            if (slot.IsEmpty)
                button.FontStyle = FontStyle.Italic;
            button.Click += (_, _) => Editor.Invoke(command);
            wrap.Children.Add(button);
            visual.Slots.Add(button);
        }
        Grid.SetColumn(wrap, 1);
        grid.Children.Add(wrap);

        var tools = Widgets.Stack(Orientation.Horizontal);
        tools.Children.Add(Tool("↑", step.MoveUpCommand));
        tools.Children.Add(Tool("↓", step.MoveDownCommand));
        tools.Children.Add(Tool("✕", step.DeleteCommand));
        Grid.SetColumn(tools, 2);
        grid.Children.Add(tools);

        visual.Frame = new Border
        {
            Child = grid,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 5),
            Margin = new Thickness(0, 0, 0, 3)
        };

        _steps.Add(visual);
        return visual.Frame;
    }

    Button Tool(string glyph, int command)
    {
        var button = Widgets.Ghost(glyph);
        button.Click += (_, _) => Editor.Invoke(command);
        return button;
    }

    Control BuildAdders()
    {
        var row = Widgets.Stack(Orientation.Horizontal);
        row.Margin = new Thickness(20, 6, 0, 0);

        foreach (var option in _model.AddOptions)
        {
            var command = option.Command;
            var button = Widgets.Ghost("+ " + option.Text);
            button.Click += (_, _) => Editor.Invoke(command);
            row.Children.Add(button);
        }

        return row;
    }
}
