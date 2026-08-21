using System;
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
/// Draws the stages of a pick as columns, in the manner of a file browser's column view.
/// </summary>
/// <remarks>
/// Columns are this host's decision, not core's. Core said there are stages and that each
/// narrows the next; on a wide screen that reads best with every stage still visible, so the
/// blocks you picked stay in view while you choose what happens to them and changing an
/// earlier answer is a glance to the left rather than a retreat through screens. A narrower
/// target might ask one stage at a time and be right to.
///
/// Built once and thereafter changed in place. Every control here outlives the answers it is
/// showing, because anything that is torn down and made again loses what somebody was doing
/// with it - the caret in the box they were typing into, the row their pointer was on, the
/// second half of a double-click. Rows are matched up by position and updated; only a change
/// in how many stages there are rebuilds anything.
/// </remarks>
sealed class ColumnPickRenderer : ComponentRenderer
{
    const double ColumnWidth = 250;

    /// <summary>
    /// How big a thing's picture is drawn here.
    /// </summary>
    /// <remarks>
    /// Entirely this host's number. Core hands over a name and says nothing about size, which
    /// it should not: what a picture is worth depends on the game. Space Engineers draws
    /// recognisable artwork for every block type and it earns the room; a game whose things
    /// all look alike would want it smaller, or absent.
    /// </remarks>
    const double IconSize = 34;

    sealed class RowVisual
    {
        public Button Button = null!;
        public TextBlock Mark = null!;
        public Border? Icon;
        public TextBlock? IconGlyph;
        public TextBlock Name = null!;
        public TextBlock Detail = null!;
        public Ellipse Light = null!;
        public Border Badge = null!;
        public TextBlock BadgeText = null!;
        public int Command;
    }

    sealed class KindVisual
    {
        public Button Button = null!;
        public TextBlock Label = null!;
        public TextBlock Count = null!;
        public int Command;
    }

    sealed class StageVisual
    {
        public TextBlock Subtitle = null!;
        public TextBox? Narrow;
        public bool Echo;
        public Button? Kinds;
        public TextBlock? Showing;
        public StackPanel Options = null!;
        public List<Button> OptionButtons = [];
        public StackPanel KindList = null!;
        public List<KindVisual> KindRows = [];
        public int Index;
        public StackPanel Rows = null!;
        public TextBlock Empty = null!;
        public List<RowVisual> Visible = [];
    }

    readonly PickComponent _model;
    readonly StackPanel _columns = Widgets.Stack(Orientation.Horizontal);
    readonly ScrollViewer _root;
    readonly List<StageVisual> _stages = [];

    int _seenRevision = -1;
    int _builtStages = -1;

    public ColumnPickRenderer(ReactorEditor editor, PickComponent model) : base(editor)
    {
        _model = model;
        _root = new ScrollViewer
        {
            Content = _columns,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        Update();
    }

    public override Control Control => _root;

    /// <summary>The columns scroll sideways, and each of them scrolls on its own.</summary>
    public override bool ScrollsItself => true;

    public override void Update()
    {
        // Core says when it has changed something, and says nothing the rest of the time. An
        // ordinary tick therefore costs one relation, which is the whole point of the number.
        if (_seenRevision == _model.Revision)
            return;

        _seenRevision = _model.Revision;

        // Columns are made only when there is a different number of them. Everything else -
        // which rows there are, what they say, which is marked - is changed in place.
        if (_builtStages != _model.Stages.Count)
        {
            _builtStages = _model.Stages.Count;
            BuildColumns();
        }

        for (var s = 0; s < _stages.Count && s < _model.Stages.Count; s++)
            Sync(_stages[s], _model.Stages[s]);
    }

    void BuildColumns()
    {
        _columns.Children.Clear();
        _stages.Clear();

        for (var i = 0; i < _model.Stages.Count; i++)
            _columns.Children.Add(BuildColumn(i));
    }

    Control BuildColumn(int index)
    {
        var stage = _model.Stages[index];
        var visual = new StageVisual { Index = index };

        // A dock rather than a stack, so the rows get whatever height is left over and their
        // scroller is actually bounded by the column. Stacked, every child is measured against
        // infinite height, the scroller grows to fit its content and there is nothing left for
        // it to scroll - a list longer than the screen simply runs off the bottom.
        var stack = new DockPanel { LastChildFill = true };
        Action<Control> top = child =>
        {
            DockPanel.SetDock(child, Dock.Top);
            stack.Children.Add(child);
        };

        top(Widgets.Label(stage.Title, Palette.Ink, 13, true));

        visual.Subtitle = Widgets.Label(stage.Subtitle, Palette.Faint, 11);
        top(visual.Subtitle);

        // Wherever core says a stage can be narrowed, and for the whole life of the stage. A
        // box that comes and goes with how much data happens to be present means the screen has
        // no shape to learn, and the saving is one line.
        if (stage.CanNarrow)
        {
            var box = new TextBox
            {
                Background = Palette.Page,
                BorderBrush = Palette.Line,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 5),
                FontSize = 12,
                Margin = new Thickness(0, 8, 0, 0),
                Watermark = "Search…"
            };

            // Asked for afresh every keystroke, never remembered. Core re-issues its command
            // ids on every rebuild, so an id captured when this box was made is dead the moment
            // anything changes - and typing would go quietly nowhere while the list stayed
            // filtered on whatever core last heard.
            box.TextChanged += (_, _) =>
            {
                if (visual.Echo)
                    return;

                var stages = _model.Stages;
                if (visual.Index >= stages.Count)
                    return;

                Editor.SetText(stages[visual.Index].NarrowCommand, box.Text ?? "");
            };

            visual.Narrow = box;
            top(box);
        }

        if (stage.Filters.Count > 1)
        {
            visual.KindList = Widgets.Stack();
            visual.Kinds = Widgets.Ghost("");
            visual.Kinds.Margin = new Thickness(0, 6, 0, 0);
            visual.Kinds.Flyout = new Flyout
            {
                Content = new ScrollViewer
                {
                    Content = visual.KindList,
                    MaxHeight = 320,
                    MinWidth = 200,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
                },
                Placement = PlacementMode.BottomEdgeAlignedLeft,
                ShowMode = FlyoutShowMode.Transient
            };
            top(visual.Kinds);
        }

        if (stage.Filters.Count > 0)
        {
            visual.Showing = Widgets.Label("", Palette.Faint, 11);
            visual.Showing.Margin = new Thickness(2, 6, 0, 0);
            top(visual.Showing);
        }

        // Choices about the picking itself sit above the rows they describe.
        visual.Options = Widgets.Stack();
        top(visual.Options);

        visual.Empty = Widgets.Label(stage.EmptyText, Palette.Muted, 12);
        visual.Empty.Margin = new Thickness(2, 8, 0, 0);

        visual.Rows = Widgets.Stack();
        var scroll = new ScrollViewer
        {
            Content = visual.Rows,
            Margin = new Thickness(0, 8, 0, 0),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        top(visual.Empty);
        stack.Children.Add(scroll);

        _stages.Add(visual);

        return new Border
        {
            Child = stack,
            Width = ColumnWidth,
            Background = Palette.Panel,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 8, 0)
        };
    }

    void Sync(StageVisual visual, PickStage stage)
    {
        visual.Subtitle.Text = stage.Subtitle;

        if (visual.Narrow != null)
        {
            var text = stage.NarrowText ?? "";
            if (visual.Narrow.Text != text)
            {
                // Put back only when it genuinely differs, and never reported as a keystroke:
                // Avalonia raises the change after the assignment returns, so our own echo would
                // otherwise arrive looking like typing and start the whole thing again.
                visual.Echo = true;
                visual.Narrow.Text = text;
                visual.Echo = false;
            }
        }

        SyncKinds(visual, stage);
        SyncOptions(visual, stage);

        if (visual.Showing != null && stage.Filters.Count > 0)
        {
            var total = stage.Filters[0].Count;
            visual.Showing.Text = stage.Entries.Count == total
                ? total + " blocks"
                : stage.Entries.Count + " of " + total;
        }

        visual.Empty.IsVisible = stage.Entries.Count == 0;
        SyncRows(visual, stage);
    }

    /// <summary>
    /// One line for narrowing by kind, opening onto what is actually here.
    /// </summary>
    /// <remarks>
    /// Not a row of chips. A large construct shows twenty or thirty kinds and a wall of them
    /// crowds out the box that would have answered the question in four keystrokes. One line
    /// costs nothing when it is not wanted, and opens onto the whole list with counts when it
    /// is - which is also the answer for somebody who does not know the game calls it an air
    /// vent, because they can read the word rather than having to type it.
    /// </remarks>
    void SyncKinds(StageVisual visual, PickStage stage)
    {
        if (visual.Kinds == null)
            return;

        var current = stage.Filters[0];
        for (var i = 0; i < stage.Filters.Count; i++)
        {
            if (stage.Filters[i].IsCurrent)
                current = stage.Filters[i];
        }

        visual.Kinds.Content = current.Label + " ▾";

        while (visual.KindRows.Count < stage.Filters.Count)
        {
            var row = BuildKind();
            visual.KindRows.Add(row);
            visual.KindList.Children.Add(row.Button);
        }

        while (visual.KindRows.Count > stage.Filters.Count)
        {
            var last = visual.KindRows.Count - 1;
            visual.KindList.Children.Remove(visual.KindRows[last].Button);
            visual.KindRows.RemoveAt(last);
        }

        for (var i = 0; i < stage.Filters.Count; i++)
        {
            var filter = stage.Filters[i];
            var row = visual.KindRows[i];

            row.Command = filter.Command;
            row.Label.Text = filter.Label;
            row.Label.Foreground = filter.IsCurrent ? Palette.Ink : Palette.Muted;
            row.Count.Text = filter.Count.ToString();
        }
    }

    KindVisual BuildKind()
    {
        var row = new KindVisual();
        var line = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };

        row.Label = Widgets.Label("", Palette.Muted, 12);
        line.Children.Add(row.Label);

        row.Count = Widgets.Label("", Palette.Faint, 11);
        row.Count.Margin = new Thickness(16, 0, 0, 0);
        Grid.SetColumn(row.Count, 1);
        line.Children.Add(row.Count);

        row.Button = Widgets.Row(line);
        row.Button.Padding = new Thickness(8, 5);
        row.Button.Background = Brushes.Transparent;
        row.Button.Click += (_, _) => Editor.Invoke(row.Command);
        return row;
    }

    void SyncOptions(StageVisual visual, PickStage stage)
    {
        while (visual.OptionButtons.Count < stage.Options.Count)
        {
            var index = visual.OptionButtons.Count;
            var button = Widgets.Ghost("");
            button.HorizontalAlignment = HorizontalAlignment.Stretch;
            button.HorizontalContentAlignment = HorizontalAlignment.Left;
            button.Click += (_, _) =>
            {
                var stages = _model.Stages;
                if (visual.Index < stages.Count && index < stages[visual.Index].Options.Count)
                    Editor.Invoke(stages[visual.Index].Options[index].Command);
            };
            visual.OptionButtons.Add(button);
            visual.Options.Children.Add(button);
        }

        while (visual.OptionButtons.Count > stage.Options.Count)
        {
            var last = visual.OptionButtons.Count - 1;
            visual.Options.Children.Remove(visual.OptionButtons[last]);
            visual.OptionButtons.RemoveAt(last);
        }

        for (var i = 0; i < stage.Options.Count; i++)
        {
            var option = stage.Options[i];
            var button = visual.OptionButtons[i];

            button.Content = (option.IsCurrent ? "● " : "○ ") + option.Text;
            button.Foreground = option.IsCurrent ? Palette.Ink : Palette.Muted;
        }
    }

    /// <summary>
    /// Matches the rows already drawn against what is on offer now.
    /// </summary>
    /// <remarks>
    /// By position, and updated rather than replaced. Typing into the search changes which
    /// things match, and rebuilding the list then would take the box being typed into out of
    /// the layout and the caret with it.
    /// </remarks>
    void SyncRows(StageVisual visual, PickStage stage)
    {
        var single = !stage.AllowsMany;

        while (visual.Visible.Count < stage.Entries.Count)
        {
            var row = BuildRow(single, stage.HasIcons);
            visual.Visible.Add(row);
            visual.Rows.Children.Add(row.Button);
        }

        while (visual.Visible.Count > stage.Entries.Count)
        {
            var last = visual.Visible.Count - 1;
            visual.Rows.Children.Remove(visual.Visible[last].Button);
            visual.Visible.RemoveAt(last);
        }

        for (var i = 0; i < stage.Entries.Count; i++)
        {
            var entry = stage.Entries[i];
            var row = visual.Visible[i];

            row.Command = entry.Command;

            row.Mark.Text = entry.IsChosen
                ? (stage.AllowsMany ? "✓" : "●")
                : (stage.AllowsMany ? "○" : "·");
            row.Mark.Foreground = entry.IsChosen ? Palette.Live : Palette.Faint;

            // Standing in for artwork we do not have. A host with real pictures resolves the
            // name; this one takes its first letter and admits what it is.
            if (row.IconGlyph != null)
            {
                var icon = entry.Icon ?? "";
                row.IconGlyph.Text = icon.Length == 0 ? "" : icon.Substring(0, 1).ToUpperInvariant();
            }

            row.Name.Text = entry.Text;

            var detail = Detail(entry);
            row.Detail.Text = detail;
            row.Detail.IsVisible = !string.IsNullOrEmpty(detail);

            row.Light.IsVisible = !string.IsNullOrEmpty(entry.Status) && entry.IsActive;

            row.Badge.IsVisible = string.IsNullOrEmpty(entry.Status) && entry.Badge > 0;
            row.BadgeText.Text = entry.Badge.ToString();
        }
    }

    RowVisual BuildRow(bool single, bool withIcon)
    {
        var row = new RowVisual();
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*,Auto") };

        // A tick for things you collect, a dot for things you settle on.
        row.Mark = Widgets.Label("", Palette.Faint, 13, true);
        row.Mark.Margin = new Thickness(0, 0, 8, 0);
        grid.Children.Add(row.Mark);

        // The game has artwork for every kind of thing and this is where it goes. We have
        // none, so a letter stands in - enough to prove the picture reaches the row and to
        // show what the column costs. Reserved for the whole stage or for none of it, so rows
        // never jog sideways depending on which kinds happen to be listed.
        if (withIcon)
        {
            row.IconGlyph = Widgets.Label("", Palette.Muted, 15, true);
            row.IconGlyph.HorizontalAlignment = HorizontalAlignment.Center;
            row.IconGlyph.VerticalAlignment = VerticalAlignment.Center;
            row.Icon = new Border
            {
                Child = row.IconGlyph,
                Width = IconSize,
                Height = IconSize,
                CornerRadius = new CornerRadius(4),
                Background = Palette.SlotFill,
                Margin = new Thickness(0, 0, 8, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(row.Icon, 1);
            grid.Children.Add(row.Icon);
        }

        row.Name = Widgets.Label("", Palette.Ink, 12);
        row.Detail = Widgets.Label("", Palette.Faint, 11);

        var text = Widgets.Stack();
        text.Children.Add(row.Name);
        text.Children.Add(row.Detail);
        Grid.SetColumn(text, 2);
        grid.Children.Add(text);

        row.Light = new Ellipse
        {
            Width = 7,
            Height = 7,
            Fill = Palette.Live,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(row.Light, 3);
        grid.Children.Add(row.Light);

        row.BadgeText = Widgets.Label("", Palette.Muted, 11);
        row.Badge = new Border
        {
            Child = row.BadgeText,
            Background = Palette.SlotFill,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(6, 1),
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(row.Badge, 3);
        grid.Children.Add(row.Badge);

        row.Button = Widgets.Row(grid);
        row.Button.Padding = new Thickness(8, 6);
        row.Button.Background = Brushes.Transparent;
        row.Button.Click += (_, _) => Editor.Invoke(row.Command);

        // Taking a choice and moving on in one gesture, where one thing is being chosen. Where
        // several can be, the second click means un-choosing, and moving on from that would be
        // taking away what somebody just did.
        if (single)
        {
            row.Button.DoubleTapped += (_, _) =>
            {
                if (Editor.Screen.HasPrimary)
                    Editor.Invoke(Editor.Screen.PrimaryCommand);
            };
        }

        return row;
    }

    static string Detail(PickOption entry)
    {
        if (string.IsNullOrEmpty(entry.Secondary))
            return entry.Status ?? "";
        if (string.IsNullOrEmpty(entry.Status))
            return entry.Secondary;
        return entry.Secondary + "   " + entry.Status;
    }
}
