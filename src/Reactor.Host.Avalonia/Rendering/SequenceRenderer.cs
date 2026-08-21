using Avalonia;
using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Transformation;
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
        public TextBlock Grip = null!;
        public int Position;
    }

    SequenceComponent _model;
    readonly StackPanel _root = Widgets.Stack();
    readonly List<StepVisual> _steps = [];
    readonly List<Button> _triggerSlots = [];

    /// <summary>The margin under each step, which the drag arithmetic has to agree with.</summary>
    const double RowGap = 3;

    /// <summary>Which step is being dragged, or -1.</summary>
    int _dragging = -1;
    double _grabbedAt;


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
    Button _redo = null!;
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

        // Both hold their place for the whole time the sentence is editable, and go quiet when
        // there is nothing to walk back. Hiding them would move Pause and Delete sideways every
        // time somebody made or undid a change, under the pointer they were about to use.
        //
        // Greying is right here and was wrong for the sentence: these are controls that will
        // become available, so "not just now" is exactly what grey means. The sentence is not
        // a control at all.
        Waiting(_undo, _model.UndoCommand, "Undo " + _model.UndoText, "Nothing to undo");
        Waiting(_redo, _model.RedoCommand, "Redo " + _model.RedoText, "Nothing to redo");

        for (var i = 0; i < _triggerSlots.Count && i < _model.Trigger.Slots.Count; i++)
            _triggerSlots[i].Content = _model.Trigger.Slots[i].Text;

        for (var i = 0; i < _steps.Count && i < _model.Steps.Count; i++)
        {
            var step = _model.Steps[i];

            // Left alone while somebody is dragging. This runs every tick, and it used to
            // repaint the row being dragged sixty times a second in the colour it is not - the
            // drag set one background, the tick set the other, and the result was a flicker
            // that looked like a rendering fault rather than two writers.
            if (_dragging < 0)
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
            ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto,Auto,Auto,Auto"),
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
        // Glyphs, not sentences. Naming what would be undone is worth having and it is not
        // worth the width of two labels that change length every time somebody edits - the row
        // would breathe in and out as they worked. It moves to the tooltip, which costs nothing
        // and is still there for anybody who wants to be sure before pressing.
        _undo = Arrow("↶");
        _undo.Click += (_, _) => Editor.Invoke(_model.UndoCommand);
        _undo.IsVisible = _model.Editable;
        Grid.SetColumn(_undo, 2);
        grid.Children.Add(_undo);

        _redo = Arrow("↷");
        _redo.Click += (_, _) => Editor.Invoke(_model.RedoCommand);
        _redo.IsVisible = _model.Editable;
        Grid.SetColumn(_redo, 3);
        grid.Children.Add(_redo);

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
        Grid.SetColumn(_enable, 4);
        grid.Children.Add(_enable);

        if (_model.Editable)
        {
            var delete = Widgets.Ghost("Delete");
            delete.Click += (_, _) => Editor.Invoke(_model.DeleteCommand);
            Grid.SetColumn(delete, 5);
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

        visual.Position = step.Position;

        // A grip, rather than the whole line being draggable. The line is full of buttons, so
        // "press anywhere that is not a chip" is a rule nobody can see and half a rule anyway -
        // it makes an accidental drag easy for somebody aiming just past a word. A handle says
        // where to grab, and says that grabbing is a thing you can do at all.
        var handle = Widgets.Stack(Orientation.Horizontal);

        visual.Grip = Widgets.Label("⋮⋮", Palette.Faint, 13);
        visual.Grip.Width = 18;
        visual.Grip.VerticalAlignment = VerticalAlignment.Center;
        visual.Grip.IsVisible = _model.Editable;

        // A transparent background, which is not decoration: without one a TextBlock is only
        // hit-testable where the glyph actually paints, so the grip answered on the dots and
        // nowhere else. Padding then makes it a target somebody can hit without aiming.
        visual.Grip.Background = Brushes.Transparent;
        visual.Grip.Padding = new Thickness(2, 6);
        handle.Children.Add(visual.Grip);

        var number = Widgets.Label(step.Number.ToString(), Palette.Faint);
        number.Width = 20;
        handle.Children.Add(number);
        grid.Children.Add(handle);

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
            tools.Children.Add(Tool("✕", step.DeleteCommand));
            Grid.SetColumn(tools, 2);
            grid.Children.Add(tools);
        }

        visual.Frame = new Border
        {
            Child = grid,
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(6, 5),
            Margin = new Thickness(0, 0, 0, RowGap)
        };

        if (_model.Editable)
        {
            visual.Frame.Transitions = Sliding();
            Draggable(visual, step);
        }

        _steps.Add(visual);
        return visual.Frame;
    }

    /// <summary>One of the pair, a fixed width so neither can shift the row.</summary>
    static Button Arrow(string glyph)
    {
        var button = Widgets.Ghost(glyph);
        button.FontSize = 15;
        button.Padding = new Thickness(6, 2);
        button.Width = 32;
        return button;
    }

    static void Waiting(Button button, int command, string available, string empty)
    {
        var ready = command != 0;
        button.IsEnabled = ready;
        button.Opacity = ready ? 1.0 : 0.35;
        ToolTip.SetTip(button, ready ? available : empty);
    }

    /// <summary>
    /// Lets a line be picked up and dropped where it belongs.
    /// </summary>
    /// <remarks>
    /// A sequence is a numbered list somebody reads as a procedure, and a procedure written in
    /// the wrong order is the ordinary case. Reaching for the arrows four times to move one
    /// line four places is the sort of thing that makes somebody give up and retype it.
    ///
    /// Core says only that a step's position is settable. Where the line is dropped is worked
    /// out here from where the rows actually are, and sent as one move - not as a run of
    /// single steps, which would fill the undo history with four entries for one act.
    ///
    /// The chips inside the row are buttons and handle their own presses, so a press that
    /// lands on one never starts a drag. Anywhere else in the row does.
    /// </remarks>
    void Draggable(StepVisual visual, SentenceStep step)
    {
        var grip = visual.Grip;
        var frame = visual.Frame;
        grip.Cursor = new Cursor(StandardCursorType.SizeAll);

        grip.PointerPressed += (_, e) =>
        {
            _dragging = visual.Position;
            _grabbedAt = e.GetPosition(_root).Y;

            // The line being carried tracks the pointer exactly, so it is the one row with no
            // easing on it. Everything else slides.
            frame.Transitions = null;
            frame.ZIndex = 1;
            frame.Background = Palette.SlotFill;

            e.Pointer.Capture(grip);
        };

        grip.PointerMoved += (_, e) =>
        {
            if (_dragging != visual.Position)
                return;

            var moved = e.GetPosition(_root).Y - _grabbedAt;
            Carry(frame, moved);
            MakeRoom(visual.Position,
                LandingFor(visual.Position, CarriedMiddle(visual.Position, moved)));
        };

        grip.PointerReleased += (_, e) =>
        {
            if (_dragging != visual.Position)
                return;

            var moved = e.GetPosition(_root).Y - _grabbedAt;
            var to = LandingFor(visual.Position, CarriedMiddle(visual.Position, moved));
            _dragging = -1;
            e.Pointer.Capture(null);

            if (to == visual.Position)
            {
                // Nothing to do, so the rows slide back to where they were.
                Settle();
                return;
            }

            // Everything stays exactly where the drag put it, and the carried line drops into
            // the gap that was being held for it. What is on screen is now the new order, so
            // the rebuild that follows changes nothing anybody can see.
            //
            // Putting it all back first was the obvious thing and it flashed: for one frame the
            // list rendered in its old order, because the model does not change until the
            // command is sent and the screen is not rebuilt until the next tick.
            Carry(frame, Landing(visual.Position, to) - _steps[visual.Position].Frame.Bounds.Top);
            Editor.SetNumber(step.MoveToCommand, to);
        };
    }

    static void Carry(Border frame, double y)
    {
        var builder = TransformOperations.CreateBuilder(1);
        builder.AppendTranslate(0, y);
        frame.RenderTransform = builder.Build();
    }

    /// <summary>
    /// Opens a gap where the line would land.
    /// </summary>
    /// <remarks>
    /// The rows between where it came from and where it is going step aside by exactly its
    /// height, so the space it would occupy is visibly empty rather than having to be imagined
    /// from a pointer position. Every one of them has a transition on it, so they slide out of
    /// the way and back as the pointer crosses them - a row that popped between two places
    /// would read as the list glitching rather than as the list responding.
    /// </remarks>
    void MakeRoom(int from, int to)
    {
        var height = _steps[from].Frame.Bounds.Height + RowGap;

        for (var i = 0; i < _steps.Count; i++)
        {
            if (i == from)
                continue;

            var shift = 0.0;
            if (from < to && i > from && i <= to)
                shift = -height;
            else if (from > to && i >= to && i < from)
                shift = height;

            Carry(_steps[i].Frame, shift);
        }
    }

    /// <summary>Puts everything back where the layout says it goes.</summary>
    void Settle()
    {
        for (var i = 0; i < _steps.Count; i++)
        {
            var frame = _steps[i].Frame;
            frame.RenderTransform = null;
            frame.ZIndex = 0;
            frame.Background = null;
            frame.Transitions = Sliding();
        }
    }

    /// <summary>
    /// Short and eased, because this is feedback rather than decoration.
    /// </summary>
    /// <remarks>
    /// A fresh list per control: Avalonia mutates a control's transition collection, so sharing
    /// one between rows makes them fight over it.
    /// </remarks>
    static Transitions Sliding()
    {
        return new Transitions
        {
            new TransformOperationsTransition
            {
                Property = Visual.RenderTransformProperty,
                Duration = TimeSpan.FromMilliseconds(120),
                Easing = new CubicEaseOut()
            }
        };
    }

    /// <summary>
    /// Where the line being carried would land, judged by where the line is.
    /// </summary>
    /// <remarks>
    /// By the line, not by the pointer. Somebody grabs a row wherever the grip happens to be
    /// under their finger and then aims with the *row* - it is the thing they can see moving.
    /// Judging by the pointer put the gap a grab-offset away from the row hovering over it, so
    /// the line landed somewhere other than where the space had been made for it. The two were
    /// never in disagreement in the arithmetic; they disagreed on screen, which is the only
    /// place it matters.
    ///
    /// Counting how many *other* rows the carried row has passed also gives the answer
    /// directly, with no correcting for the row being lifted out: the rows it has passed are
    /// exactly the ones that end up above it.
    ///
    /// Measured against where the rows were laid out rather than where they have slid to. The
    /// sliding is a picture of this answer, and reading it back off the picture would chase
    /// its own tail.
    /// </remarks>
    int LandingFor(int from, double carried)
    {
        var index = 0;
        for (var i = 0; i < _steps.Count; i++)
        {
            if (i == from)
                continue;

            var bounds = _steps[i].Frame.Bounds;
            if (carried > bounds.Top + bounds.Height / 2)
                index++;
        }
        return index;
    }

    /// <summary>
    /// Where the top of the carried row belongs once it has landed.
    /// </summary>
    /// <remarks>
    /// Added up from the rows that will be above it rather than worked out from an offset,
    /// because the rows are not all the same height - a wait is a shorter line than an action.
    /// </remarks>
    double Landing(int from, int to)
    {
        var top = _steps[0].Frame.Bounds.Top;
        var placed = 0;

        for (var i = 0; i < _steps.Count && placed < to; i++)
        {
            if (i == from)
                continue;

            top += _steps[i].Frame.Bounds.Height + RowGap;
            placed++;
        }
        return top;
    }

    /// <summary>The middle of the carried row, where the drag has put it.</summary>
    double CarriedMiddle(int from, double moved)
    {
        var bounds = _steps[from].Frame.Bounds;
        return bounds.Top + bounds.Height / 2 + moved;
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
