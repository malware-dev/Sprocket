using System.Collections.Generic;
using Phosphor;
using Reactor.Ui;
using VRageMath;

namespace Reactor.Screens
{
    /// <summary>
    /// The stages of a pick, as columns, in the manner of a file browser's column view.
    /// </summary>
    /// <remarks>
    /// Columns are this target's decision and the other host's before it, not core's. Core said there are
    /// stages and that each narrows the next; with every stage still in view the blocks already picked stay
    /// beside what is happening to them, and changing an earlier answer is a glance to the left rather than
    /// a retreat through screens.
    /// <para>
    /// Built once and thereafter changed in place. Anything torn down and made again loses what somebody
    /// was doing with it - the caret in the box they were typing into above all, since typing is what
    /// changes the list underneath. Only a change in how many stages there are rebuilds anything.
    /// </para>
    /// </remarks>
    public sealed class PickView : ComponentView
    {
        public const float ColumnWidth = Sizes.Column;

        readonly PickComponent _component;
        readonly Stack _columns = Widgets.RowOf(Space.Item);
        readonly Scroll _strip = new Scroll();
        readonly List<StageView> _stages = new List<StageView>();

        int _revision = -1;
        int _built = -1;

        public PickView(PickComponent component)
        {
            _component = component;
            _strip.Horizontal = true;
            _strip.Content = _columns;
        }

        public override Element Root
        {
            get { return _strip; }
        }

        /// <summary>The strip scrolls sideways and every column scrolls its own rows.</summary>
        public override bool ScrollsItself
        {
            get { return true; }
        }

        public override void Update(int tick)
        {
            // Core says when it has changed something and says nothing the rest of the time, so an
            // ordinary tick costs one comparison.
            if (_revision == _component.Revision && _built == _component.Stages.Count)
                return;

            _revision = _component.Revision;

            if (_built != _component.Stages.Count)
            {
                _built = _component.Stages.Count;
                Rebuild();
            }

            for (int i = 0; i < _stages.Count && i < _component.Stages.Count; i++)
                _stages[i].Update(_component.Stages[i], Host);
        }

        public override void Collect(List<Hit> hits)
        {
            for (int i = 0; i < _stages.Count && i < _component.Stages.Count; i++)
                _stages[i].Collect(hits, _component.Stages[i], i, Host);
        }

        /// <summary>
        /// Moves the column being pointed at, or the whole strip when that column has nowhere to go.
        /// </summary>
        /// <remarks>
        /// The fallback is not a flourish. With several stages open the strip runs off the side, and a
        /// wheel that did nothing because this particular column happened to fit would read as the
        /// screen being broken rather than as the column being full.
        /// </remarks>
        public override bool ScrollBy(Vector2 point, float amount)
        {
            if (!EditorView.Contains(_strip.Bounds, point))
                return false;

            for (int i = 0; i < _stages.Count; i++)
            {
                if (_stages[i].ScrollBy(point, amount))
                    return true;
            }

            if (!_strip.CanScroll)
                return false;

            _strip.ScrollBy(amount);
            return true;
        }

        void Rebuild()
        {
            _stages.Clear();
            _columns.Children.Clear();

            for (int i = 0; i < _component.Stages.Count; i++)
            {
                var stage = new StageView(_component, i);
                _stages.Add(stage);
                _columns.Children.Add(stage.Root);
            }
        }
    }

    /// <summary>One column: a heading, whatever narrows it, and the things to choose from.</summary>
    public sealed class StageView
    {
        sealed class RowVisual
        {
            public Bar Line;
            public Mark Mark;
            public IconBox Icon;
            public Label Name;
            public Label Detail;
            public Label Badge;
        }

        sealed class KindVisual
        {
            public Bar Line;
            public Label Label;
            public Label Count;
        }

        /// <summary>
        /// How big a thing's picture is drawn.
        /// </summary>
        /// <remarks>
        /// Entirely this target's number. Core hands over a name and says nothing about size, which it
        /// should not: Space Engineers draws recognisable artwork for every block type and it earns the
        /// room, where a game whose things all look alike would want it smaller or absent.
        /// </remarks>
        const float IconSize = Sizes.Icon;

        readonly PickComponent _component;
        readonly int _index;

        readonly Sized _root = new Sized(PickView.ColumnWidth);
        readonly Dock _column = new Dock();
        readonly Stack _head = Widgets.Column(Space.Row);
        readonly Label _title = Widgets.Label("", Palette.Ink, Type.Body);
        readonly Label _subtitle = Widgets.Label("", Palette.Faint, Type.Tiny);
        readonly TextBox _narrow = Widgets.Box(Type.Small, Palette.Page);
        readonly Signpost _kindLine = new Signpost("", Symbols.Down, false);
        readonly Label _showing = Widgets.Label("", Palette.Faint, Type.Tiny);
        readonly Stack _options = Widgets.RowOf(Space.Region);
        readonly List<OptionRow> _optionRows = new List<OptionRow>();

        /// <summary>
        /// One of the two answers to "any of these, or all of them", drawn the way every other
        /// one-of-several choice on this screen is drawn.
        /// </summary>
        /// <remarks>
        /// This used to be the words with "(*) " or "( ) " stuck on the front of them. A round mark is
        /// what the rest of the editor uses to say only one of these may be chosen, and there is a glyph
        /// for it in the font we ship, so spelling it out of brackets and an asterisk was the one place on
        /// screen pretending to be a terminal.
        /// </remarks>
        sealed class OptionRow
        {
            public Stack Row;
            public Mark Mark;
            public Label Text;
        }

        readonly Scroll _scroll = new Scroll();
        readonly Stack _rows = Widgets.Column(Space.Line);
        readonly List<RowVisual> _visible = new List<RowVisual>();
        readonly Label _empty = Widgets.Label("", Palette.Muted, Type.Small);

        readonly Stack _kindList = Widgets.Column(Space.Hair * 2f);
        readonly List<KindVisual> _kindRows = new List<KindVisual>();

        bool _hasNarrow;
        bool _hasKinds;

        public StageView(PickComponent component, int index)
        {
            _component = component;
            _index = index;

            PickStage stage = component.Stages[index];

            _title.Wraps = false;
            _title.Trims = true;

            _narrow.Hint = "Search";
            _kindLine.Highlight = Palette.ControlHot;

            _empty.Padding = new Thickness(Space.Line, Space.Item, 0f, 0f);

            _kindList.Background = Palette.PanelHot;
            _kindList.Edge = Palette.Line;
            _kindList.Padding = new Thickness(Space.Item);

            _head.Children.Add(_title);
            _head.Children.Add(_subtitle);

            // Wherever core says a stage can be narrowed, and for the whole life of the stage. A box that
            // came and went with how much data happens to be present would leave the column with no shape
            // to learn, and the saving is one line.
            _hasNarrow = stage.CanNarrow;
            if (_hasNarrow)
                _head.Children.Add(_narrow);

            // One line rather than a row of chips. A large construct shows twenty or thirty kinds, and a
            // wall of them crowds out the box that would have answered the question in four keystrokes.
            _hasKinds = stage.Filters.Count > 1;
            if (_hasKinds)
                _head.Children.Add(_kindLine);

            if (stage.Filters.Count > 0)
                _head.Children.Add(_showing);

            // Choices about the picking itself sit above the rows they describe, on one line across
            // rather than stacked down: there are two of them, they are three words each, and a
            // column has width to spare where every line of height comes out of the list.
            _head.Children.Add(_options);

            _scroll.Content = _rows;

            _column.Background = Palette.Panel;
            _column.Edge = Palette.Line;
            _column.Padding = new Thickness(Space.Region);
            _column.Top = _head;
            _column.Fill = _scroll;

            _root.Content = _column;
        }

        public Element Root
        {
            get { return _root; }
        }

        /// <summary>Moves this column's rows, if the point is in it and there is anything out of sight.</summary>
        public bool ScrollBy(Vector2 point, float amount)
        {
            if (!EditorView.Contains(_root.Bounds, point) || !_scroll.CanScroll)
                return false;

            _scroll.ScrollBy(amount);
            return true;
        }

        public void Update(PickStage stage, IScreenHost host)
        {
            _title.Text = Widgets.Caps(stage.Title);
            _subtitle.Text = stage.Subtitle;

            // Put back only while nobody is typing into it. Whatever is in the box is the truth while it
            // is being typed, and writing core's answer over it would fight the caret every keystroke.
            if (_hasNarrow && !IsTyping(host))
                _narrow.Text = stage.NarrowText;

            if (_hasKinds)
                UpdateKinds(stage);

            if (stage.Filters.Count > 0)
            {
                int total = stage.Filters[0].Count;
                _showing.Text = stage.Entries.Count == total
                    ? total + " blocks"
                    : stage.Entries.Count + " of " + total;
            }

            UpdateOptions(stage);
            UpdateRows(stage);
        }

        public void Collect(List<Hit> hits, PickStage stage, int index, IScreenHost host)
        {
            if (_hasNarrow)
                hits.Add(Hit.Typing(_narrow.Bounds, stage.NarrowCommand, _narrow));

            if (_hasKinds)
            {
                Hit opener = Hit.Opens(_kindLine.Bounds, _kindList);
                opener.Anchor = _kindLine;
                opener.Hot = _kindLine;
                hits.Add(opener);
            }

            for (int i = 0; i < _optionRows.Count && i < stage.Options.Count; i++)
                hits.Add(Hit.On(_optionRows[i].Row, stage.Options[i].Command));

            for (int i = 0; i < _visible.Count && i < stage.Entries.Count; i++)
            {
                Hit row = Hit.On(_visible[i].Line, stage.Entries[i].Command);
                row.Confirms = !stage.AllowsMany;
                hits.Add(row);
            }

            // Only while the list is actually hanging over the screen. A closed list is not measured, so
            // its rows keep the rectangles they had when it was last open - and those rows are collected
            // last and therefore tested first, so leaving them in makes a press anywhere near where the
            // list used to be change the filter instead of doing what it looks like it does.
            if (!_hasKinds || host == null || !host.IsOverlay(_kindList))
                return;

            for (int i = 0; i < _kindRows.Count && i < stage.Filters.Count; i++)
                hits.Add(Hit.On(_kindRows[i].Line, stage.Filters[i].Command));
        }

        /// <summary>The list of kinds, for the screen to hang over everything when this line is pressed.</summary>
        public Element KindList
        {
            get { return _kindList; }
        }

        public Element KindLine
        {
            get { return _kindLine; }
        }

        bool IsTyping(IScreenHost host)
        {
            var view = host as EditorView;
            return view != null && view.Focus == _narrow;
        }

        void UpdateKinds(PickStage stage)
        {
            KindFilter current = stage.Filters[0];
            for (int i = 0; i < stage.Filters.Count; i++)
            {
                if (stage.Filters[i].IsCurrent)
                    current = stage.Filters[i];
            }

            _kindLine.Text = current.Label;

            while (_kindRows.Count < stage.Filters.Count)
            {
                var row = new KindVisual();
                row.Label = Widgets.Label("", Palette.Muted, Type.Small);
                row.Count = Widgets.Label("", Palette.Faint, Type.Tiny);
                row.Count.Wraps = false;

                row.Line = new Bar();
                row.Line.Spacing = Space.Region * 1.6f;
                row.Line.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY * 1.3f);

                // Down, not up. A row you can press is a control, and a control deepens under the
                // pointer in this game - which also means the words on the one row somebody is actually
                // reading get brighter against their ground rather than dimmer.
                row.Line.Highlight = Palette.ControlHot;
                row.Line.Fill = row.Label;
                row.Line.Right = row.Count;

                _kindRows.Add(row);
                _kindList.Children.Add(row.Line);
            }

            while (_kindRows.Count > stage.Filters.Count)
            {
                int last = _kindRows.Count - 1;
                _kindList.Children.Remove(_kindRows[last].Line);
                _kindRows.RemoveAt(last);
            }

            for (int i = 0; i < stage.Filters.Count; i++)
            {
                KindFilter filter = stage.Filters[i];

                _kindRows[i].Label.Text = filter.Label;
                _kindRows[i].Label.Color = filter.IsCurrent ? Palette.Ink : Palette.Muted;
                _kindRows[i].Count.Text = filter.Count.ToString();
            }
        }

        void UpdateOptions(PickStage stage)
        {
            while (_optionRows.Count < stage.Options.Count)
            {
                var option = new OptionRow();
                option.Mark = new Mark();
                option.Text = Widgets.Label("", Palette.Muted, Type.Small);

                option.Row = Widgets.RowOf(Space.Item);
                option.Row.Children.Add(option.Mark);
                option.Row.Children.Add(option.Text);
                option.Row.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY * 0.8f);
                option.Row.Highlight = Palette.ControlHot;

                _optionRows.Add(option);
                _options.Children.Add(option.Row);
            }

            while (_optionRows.Count > stage.Options.Count)
            {
                int last = _optionRows.Count - 1;
                _options.Children.Remove(_optionRows[last].Row);
                _optionRows.RemoveAt(last);
            }

            for (int i = 0; i < stage.Options.Count; i++)
            {
                ChoiceEntry option = stage.Options[i];
                OptionRow row = _optionRows[i];

                // Round, never square: exactly one of these holds at a time, and that is what the shape
                // says before anybody reads the words beside it.
                row.Mark.Many = false;
                row.Mark.Chosen = option.IsCurrent;
                row.Mark.Color = option.IsCurrent ? Palette.Live : Palette.Faint;

                row.Text.Text = option.Text;
                row.Text.Color = option.IsCurrent ? Palette.Ink : Palette.Muted;
            }
        }

        /// <summary>
        /// Matches the rows already drawn against what is on offer now.
        /// </summary>
        /// <remarks>
        /// By position, and updated rather than replaced. Typing into the search changes which things
        /// match, and rebuilding the list then would take the box being typed into out of the layout and
        /// the caret with it.
        /// </remarks>
        void UpdateRows(PickStage stage)
        {
            bool many = stage.AllowsMany;

            while (_visible.Count < stage.Entries.Count)
            {
                RowVisual row = BuildRow(stage.HasIcons);
                _visible.Add(row);
                _rows.Children.Add(row.Line);
            }

            while (_visible.Count > stage.Entries.Count)
            {
                int last = _visible.Count - 1;
                _rows.Children.Remove(_visible[last].Line);
                _visible.RemoveAt(last);
            }

            if (stage.Entries.Count == 0)
            {
                _empty.Text = stage.EmptyText;

                if (_empty.Parent != _rows)
                    _rows.Children.Add(_empty);
            }
            else if (_empty.Parent == _rows)
            {
                _rows.Children.Remove(_empty);
            }

            for (int i = 0; i < stage.Entries.Count; i++)
            {
                PickOption entry = stage.Entries[i];
                RowVisual row = _visible[i];

                // A tick for things you collect, a dot for things you settle on.
                row.Mark.Many = many;
                row.Mark.Chosen = entry.IsChosen;
                row.Mark.Color = entry.IsChosen ? Palette.Live : Palette.Faint;

                if (row.Icon != null)
                    row.Icon.Art = entry.Icon;

                row.Name.Text = entry.Text;

                string detail = Detail(entry);
                row.Detail.Text = detail;
                row.Detail.Collapsed = string.IsNullOrEmpty(detail);

                bool badged = string.IsNullOrEmpty(entry.Status) && entry.Badge > 0;

                // Out of the row rather than drawn in nothing. Held space would be right if this came and
                // went as somebody used the screen, but which rows carry a count is a property of the
                // things themselves and settled before the list is drawn.
                row.Badge.Collapsed = !badged;
                row.Badge.Text = badged ? entry.Badge.ToString() : "";
            }
        }

        RowVisual BuildRow(bool withIcon)
        {
            var row = new RowVisual();

            row.Mark = new Mark();

            Stack left = Widgets.RowOf(Space.Item);
            left.Children.Add(row.Mark);

            // Reserved for the whole stage or for none of it, so rows never jog sideways depending on
            // which kinds happen to be listed.
            if (withIcon)
            {
                row.Icon = new IconBox(IconSize);
                row.Icon.Background = Palette.SlotFill;

                left.Children.Add(row.Icon);
            }

            // The size the game writes a block list in, because that is what this is. It was a step
            // down for a while, which made the one thing on the screen somebody is here to read the
            // smallest name on it.
            row.Name = Widgets.Label("", Palette.Ink, Type.Body);
            row.Detail = Widgets.Label("", Palette.Faint, Type.Tiny);

            Stack text = Widgets.Column(Space.Line * 0.6f);
            text.Children.Add(row.Name);
            text.Children.Add(row.Detail);

            row.Badge = Widgets.Label("", Palette.Muted, Type.Tiny);
            row.Badge.Background = Palette.SlotFill;
            row.Badge.Wraps = false;
            row.Badge.Padding = new Thickness(Space.Item, Space.Hair * 2f);

            // A stack, not a bar. A bar spans whatever width it is offered, which is exactly right for a
            // header and exactly wrong here: as the right-hand end of a row it claimed the whole line and
            // left the name to wrap one word at a time.
            Stack right = Widgets.RowOf(Space.Row);
            right.Children.Add(row.Badge);

            row.Line = new Bar();
            row.Line.Spacing = Space.Item;
            row.Line.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY * 1.6f);
            row.Line.Highlight = Palette.ControlHot;
            row.Line.Left = left;
            row.Line.Fill = text;
            row.Line.Right = right;

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
}
