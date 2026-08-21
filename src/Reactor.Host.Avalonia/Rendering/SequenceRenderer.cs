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
    TextBlock _enableText = null!;
    Border _lockedBanner = null!;
    Button _undo = null!;
    TextBlock _locked = null!;

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
        // Says what pressing it does, not what state it is in - a switch labelled with its
        // own state makes somebody work out which way round it is before they dare press it.
        _enableText.Text = _model.Enabled ? "❚❚  Pause" : "▶  Run";
        _enable.Background = _model.Enabled ? Palette.Notice : Palette.Live;

        _lockedBanner.IsVisible = !_model.Editable;
        _locked.Text = _model.LockedText;

        _undo.IsVisible = _model.UndoCommand != 0;
        _undo.Content = "↶ Undo " + _model.UndoText;

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
        _root.Children.Add(BuildLocked());
        _root.Children.Add(BuildProblem());
        _root.Children.Add(BuildTrigger());

        foreach (var step in _model.Steps)
            _root.Children.Add(BuildStep(step));

        if (_model.Editable)
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

        // The switch this whole design hangs on, so it is the loudest thing in the row rather
        // than a word in the corner. Off is not a disabled state here - it is the state you
        // work in, and it has to look like somewhere you would willingly be.
        _undo = Widgets.Ghost("");
        _undo.Click += (_, _) => Editor.Invoke(_model.UndoCommand);
        Grid.SetColumn(_undo, 2);
        grid.Children.Add(_undo);

        _enableText = Widgets.Label("", Brushes.White, 12, true);
        _enable = new Button
        {
            Content = _enableText,
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(14, 5),
            Margin = new Thickness(8, 0, 0, 0)
        };
        _enable.Click += (_, _) => Editor.SetToggle(_model.ToggleCommand, !_model.Enabled);
        Grid.SetColumn(_enable, 3);
        grid.Children.Add(_enable);

        if (_model.Editable)
        {
            var delete = Widgets.Ghost("Delete");
            delete.Click += (_, _) => Editor.Invoke(_model.DeleteCommand);
            Grid.SetColumn(delete, 4);
            grid.Children.Add(delete);
        }

        return grid;
    }

    /// <summary>
    /// How to get from watching it to changing it.
    /// </summary>
    /// <remarks>
    /// Not a warning about disabled controls - there are none, they are simply absent. This
    /// is the one thing the watching view has to say that it could not say by showing: that
    /// there is a way through to editing, and what it is. Without it, somebody looking for the
    /// add button concludes it is gone rather than waiting.
    /// </remarks>
    Control BuildLocked()
    {
        _locked = Widgets.Label("", Palette.Muted, 12);
        _lockedBanner = Widgets.Wash(_locked, Palette.SlotFill);
        _lockedBanner.Margin = new Thickness(0, 0, 0, 10);
        _lockedBanner.IsVisible = false;
        return _lockedBanner;
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
            if (_model.Editable)
                button.Click += (_, _) => Editor.Invoke(command);
            else
                Reading(button);
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
            if (_model.Editable)
                button.Click += (_, _) => Editor.Invoke(command);
            else
                Reading(button);
            wrap.Children.Add(button);
            visual.Slots.Add(button);
        }
        Grid.SetColumn(wrap, 1);
        grid.Children.Add(wrap);

        // Gone rather than greyed. What is on screen while it runs is a view of a machine
        // working, not an editor with its hands tied.
        if (_model.Editable)
        {
            var tools = Widgets.Stack(Orientation.Horizontal);
            tools.Children.Add(Tool("↑", step.MoveUpCommand));
            tools.Children.Add(Tool("↓", step.MoveDownCommand));
            tools.Children.Add(Tool("✕", step.DeleteCommand));
            Grid.SetColumn(tools, 2);
            grid.Children.Add(tools);
        }

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

    /// <summary>
    /// Turns a slot from something you press into something you read.
    /// </summary>
    /// <remarks>
    /// Deliberately not disabled. Disabling greys it, and grey says "broken, or not yours to
    /// touch" - neither of which is true. A running sequence is not a crippled editor, it is
    /// the thing being watched, and its words are the most important thing on the screen. So
    /// the chip goes and the sentence stays at full contrast: no fill, no underline, and no
    /// response to the pointer, because there is nothing there to press.
    /// </remarks>
    static void Reading(Button slot)
    {
        slot.IsHitTestVisible = false;
        slot.Background = Brushes.Transparent;
        slot.BorderThickness = new Thickness(0);
        slot.Foreground = Palette.Ink;
        slot.Padding = new Thickness(0, 4, 6, 4);
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
