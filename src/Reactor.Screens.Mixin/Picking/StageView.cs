using System.Collections.Generic;
using Reactor.Ui;
using VRageMath;
using Phosphor.Ui;
using Reactor.Screens.Controls;
using Reactor.Screens.Host;
using Reactor.Screens.Lists;
using Reactor.Screens.Look;
using Reactor.Screens.Sequences;

namespace Reactor.Screens.Picking
{
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
        readonly Label _title = Widgets.Label("", Palette.Ink, Lettering.Body);
        readonly Label _subtitle = Widgets.Label("", Palette.Faint, Lettering.Tiny);
        readonly TextBox _narrow = Widgets.Box(Lettering.Small, Palette.Page);
        readonly Signpost _kindLine = new Signpost("", Symbols.Down, false);
        readonly Label _showing = Widgets.Label("", Palette.Faint, Lettering.Tiny);
        readonly Stack _options = Widgets.RowOf(Space.Region);
        readonly List<OptionRow> _optionRows = new List<OptionRow>();

        /// <summary>
        /// One of the two answers to "any of these, or all of them", drawn the way every other
        /// one-of-several choice on this screen is drawn.
        /// </summary>
        /// <remarks>
        /// A round mark, which is what the rest of the editor uses to say only one of these may be
        /// chosen, and there is a glyph for it in the font this ships. Spelling it out of brackets and an
        /// asterisk would be the one place on screen pretending to be a terminal.
        /// </remarks>
        sealed class OptionRow
        {
            public Stack Row;
            public Mark Mark;
            public Label Text;
        }

        readonly Scroll _scroll = new Scroll();
        readonly Stack _rows = Widgets.Column(Space.Line);

        /// <summary>Where the pointer was when the thumb last moved. Only meaningful while it is held.</summary>
        float _held;
        readonly List<RowVisual> _visible = new List<RowVisual>();
        readonly Label _empty = Widgets.Label("", Palette.Muted, Lettering.Small);

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
            _kindLine.Highlight = Palette.PanelHot;

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

            Widgets.Barred(_scroll);
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

        /// <summary>Takes hold of this column's thumb, if that is what was pressed.</summary>
        /// <remarks>
        /// A press anywhere on the bar counts, not only on the thumb itself: the empty part jumps the
        /// thumb to the pointer and then carries on as an ordinary drag, so overshooting the thumb by a
        /// few units does something useful instead of nothing.
        /// </remarks>
        public bool TryGrabBar(Vector2 point)
        {
            if (!_scroll.HasBar || !EditorView.Contains(_scroll.Track, point))
                return false;

            if (!EditorView.Contains(_scroll.Thumb, point))
                _scroll.ThumbTo(point.Y);

            _scroll.BarHeld = true;
            _held = point.Y;
            return true;
        }

        public void CarryBar(Vector2 point)
        {
            if (!_scroll.BarHeld)
                return;

            _scroll.DragBy(point.Y - _held);
            _held = point.Y;
        }

        public void DropBar()
        {
            _scroll.BarHeld = false;
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
            // last and therefore tested first, so leaving them in would make a press anywhere near where
            // the list was last open change the filter instead of doing what it looks like it does.
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
                row.Label = Widgets.Label("", Palette.Muted, Lettering.Small);
                row.Count = Widgets.Label("", Palette.Faint, Lettering.Tiny);
                row.Count.Wraps = false;

                row.Line = new Bar();
                row.Line.Spacing = Space.Region * 1.6f;
                row.Line.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY * 1.3f);

                // The list itself is already the highlight tone, so its rows have to go a step past it.
                row.Line.Highlight = Palette.Line;
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
                option.Text = Widgets.Label("", Palette.Muted, Lettering.Small);

                option.Row = Widgets.RowOf(Space.Item);
                option.Row.Children.Add(option.Mark);
                option.Row.Children.Add(option.Text);
                option.Row.Padding = new Thickness(Space.ChipX * 0.8f, Space.ChipY * 0.8f);
                option.Row.Highlight = Palette.PanelHot;

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

            // The size the game writes a block list in, because that is what this is. A step down would
            // make the one thing on the screen somebody is here to read the smallest name on it.
            row.Name = Widgets.Label("", Palette.Ink, Lettering.Body);
            row.Detail = Widgets.Label("", Palette.Faint, Lettering.Tiny);

            Stack text = Widgets.Column(Space.Line * 0.6f);
            text.Children.Add(row.Name);
            text.Children.Add(row.Detail);

            row.Badge = Widgets.Label("", Palette.Muted, Lettering.Tiny);
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
            row.Line.Highlight = Palette.PanelHot;
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
