using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Screen;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Look;

namespace Sprocket.Host.SpaceEngineers.Picking
{
    /// <summary>One column: a heading, whatever narrows it, and the things to choose from.</summary>
    public sealed class StageView
    {
        sealed class RowVisual
        {
            public Bar Line;
            public Stack Left;
            public Mark Mark;
            public IconBox Icon;
            public Label Name;
            public Label Detail;
        }

        sealed class KindVisual
        {
            public Bar Line;
            public Label Label;
            public Label Count;

            /// <summary>What the count was last written from. Minus one because no filter holds it.</summary>
            public int Counted = -1;
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

        readonly Sized _root = new Sized(PickView.ColumnWidth);
        readonly Dock _column = new Dock();
        readonly Stack _head = Widgets.Column(Space.Row);
        readonly Label _title = Widgets.Label("", Palette.Ink, Lettering.Body);
        readonly Label _subtitle = Widgets.Label("", Palette.Faint, Lettering.Tiny);
        readonly TextBox _narrow = Widgets.Box(Lettering.Small, Palette.Page);
        readonly Signpost _kindLine = new Signpost("", Symbols.Down);

        /// <summary>Narrows the kinds themselves, which on a varied construct are as many as the blocks.</summary>
        readonly TextBox _kindNarrow = Widgets.Box(Lettering.Small, Palette.Page);

        /// <summary>The way back to all of them, beside the line rather than inside the list.</summary>
        readonly Stack _kindHead = Widgets.RowOf(Space.Row);

        readonly Glyph _kindClear = Widgets.Centred(Symbols.Close, Palette.Faint);

        /// <summary>The way to empty each search box, inside it at the right.</summary>
        readonly Glyph _narrowClear = Widgets.Centred(Symbols.Close, Palette.Faint);
        readonly Glyph _kindNarrowClear = Widgets.Centred(Symbols.Close, Palette.Faint);
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

        /// <summary>The room inside a row somebody can press.</summary>
        static readonly Thickness Pad = new Thickness(Space.ChipX * 0.8f, Space.ChipY * 1.6f);

        /// <summary>The same for a heading, with nothing below it.</summary>
        static readonly Thickness HeadingPad =
            new Thickness(Space.ChipX * 0.8f, Space.ChipY * 1.6f, Space.ChipX * 0.8f, 0f);

        readonly Scroll _scroll = new Scroll();
        readonly Stack _rows = Widgets.Column(Space.Line);

        /// <summary>
        /// What scrolls: the rows in Sprocket's own words, and under them the well and its heading.
        /// </summary>
        readonly Stack _list = Widgets.Column(Space.Line);

        /// <summary>
        /// The recessed well holding what the game declared in its own words.
        /// </summary>
        /// <remarks>
        /// Filled like the search box, so the depth is already part of the editor's vocabulary. Nothing
        /// above it is badged: it reads as lifted because the rest is sunk.
        /// </remarks>
        readonly Stack _well = Widgets.Column(Space.Line);

        /// <summary>The well, held clear of the scroll bar, which the list's content otherwise runs up to.</summary>
        readonly Stack _wellRoom = Widgets.Column(0f);
        readonly Label _wellTitle = Widgets.Label("", Palette.Faint, Lettering.Body);
        readonly List<RowVisual> _sunk = new List<RowVisual>();

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
            PickStage stage = component.Stages[index];

            _title.Wraps = false;
            _title.Trims = true;

            _narrow.Hint = "Search";
            Widgets.Row(_kindLine);

            _empty.Padding = new Thickness(Space.Line, Space.Item, 0f, 0f);

            _kindList.Background = Palette.PanelHot;
            _kindList.Edge = Palette.Line;
            _kindList.Padding = new Thickness(Space.Item);

            // Inside the list rather than beside the line, because it narrows what the list holds and
            // belongs where that is. It is also the first thing under the pointer once the list opens,
            // which is where somebody who knows what they want is already heading.
            _kindNarrow.Hint = "Search";
            _kindNarrow.Clear = _kindNarrowClear;
            _kindList.Children.Add(_kindNarrow);

            // A step past the list's own tone, the same as its rows. The list is already drawn in the
            // highlight colour, so a control inside it that lit with that colour would not light.
            _kindNarrowClear.Highlight = Palette.Line;
            _narrowClear.Highlight = Palette.PanelHot;

            _narrow.Clear = _narrowClear;

            // The way back to all of them, on the line rather than in the list: clearing a filter is
            // something to do without first opening the thing that set it.
            _kindClear.Padding = new Thickness(Space.Row, 0f);
            _kindClear.Highlight = Palette.PanelHot;
            _kindClear.Tip = "Show every kind again";

            // Next to the word it clears and not at the end of the row. A bar puts its right-hand
            // part against the far edge, which left the cross four hundred units from the filter it
            // was offering to undo - close enough to the column's edge to read as part of the frame.
            _kindHead.Children.Add(_kindLine);
            _kindHead.Children.Add(_kindClear);

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
                _head.Children.Add(_kindHead);

            if (stage.Filters.Count > 0)
                _head.Children.Add(_showing);

            // Choices about the picking itself sit above the rows they describe, on one line across
            // rather than stacked down: there are two of them, they are three words each, and a
            // column has width to spare where every line of height comes out of the list.
            _head.Children.Add(_options);

            _wellTitle.Wraps = false;

            // Apart from Sprocket's own entries above it by a little more than they are from each
            // other, so it reads as the start of something else.
            _wellTitle.Padding = new Thickness(0f, Space.Row, 0f, 0f);
            _well.Background = Palette.Page;
            _well.Edge = Palette.Line;
            _well.EdgeThickness = Space.Hair;
            _well.Padding = new Thickness(Space.Item);
            _wellRoom.Padding = new Thickness(0f, 0f, Space.Row, 0f);
            _wellRoom.Children.Add(_well);

            _list.Children.Add(_rows);
            _list.Children.Add(_wellTitle);
            _list.Children.Add(_wellRoom);

            Widgets.Barred(_scroll);
            _scroll.Content = _list;

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
            if (!_root.Reaches(point) || !_scroll.CanScroll)
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
            if (!_scroll.OnBar(point))
                return false;

            if (!_scroll.OnThumb(point))
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

            // The box takes the keyboard rather than sending anything at once, and then every keystroke
            // goes to the same command.
            if (_hasNarrow)
            {
                _narrow.Press = Deed.Types(stage.NarrowCommand, _narrow);
                Clearing(_narrowClear, _narrow, stage.NarrowCommand);
            }

            if (_hasKinds)
            {
                UpdateKinds(stage);

                // The list hangs off this line, and is reachable only while it is hanging there - a
                // closed one is not in the layout at all, so its rows cannot be pressed through it.
                _kindLine.Press = Deed.Opens(_kindList, _kindNarrow);

                // Put back only while nobody is typing into it, the same rule the box over the blocks
                // keeps: what is in the box is the truth while it is being typed.
                if (!IsTypingKinds(host))
                    _kindNarrow.Text = stage.KindText;

                _kindNarrow.Press = Deed.Types(stage.KindCommand, _kindNarrow);
                Clearing(_kindNarrowClear, _kindNarrow, stage.KindCommand);

                // Nothing to clear while nothing is narrowed, and a way out that does nothing is a
                // control somebody presses twice before believing it.
                _kindClear.Collapsed = !stage.IsNarrowed;
                _kindClear.Press = stage.IsNarrowed
                    ? Deed.Sends(stage.ClearCommand)
                    : Deed.Nothing;
            }

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

        /// <summary>The list of kinds, for the screen to hang over everything when this line is pressed.</summary>
        public Element KindList
        {
            get { return _kindList; }
        }

        public Element KindLine
        {
            get { return _kindHead; }
        }

        /// <summary>
        /// Arms the cross beside a box, or takes it away where the box is empty.
        /// </summary>
        /// <remarks>
        /// Nothing to throw away while nothing is written, and a way out that does nothing is a
        /// control somebody presses twice before believing it.
        /// </remarks>
        static void Clearing(Glyph clear, TextBox box, int command)
        {
            bool anything = !string.IsNullOrEmpty(box.Text);

            // Collapsed rather than taken out: the room for it is held by the box whether or not it
            // is showing, so nothing moves when the first character is typed.
            clear.Collapsed = !anything;
            clear.Tip = "Clear what is typed";
            clear.Press = anything ? Deed.Empties(command, box) : Deed.Nothing;
        }

        bool IsTyping(IScreenHost host)
        {
            var view = host as EditorView;
            return view != null && view.Focus == _narrow;
        }

        bool IsTypingKinds(IScreenHost host)
        {
            var view = host as EditorView;
            return view != null && view.Focus == _kindNarrow;
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

                // As wide as what is written in it. The list this sits in is a menu, and a menu is
                // as wide as its longest word rather than as wide as whatever offered it room.
                row.Line.Tight = true;

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

                _kindRows[i].Line.Press = Deed.Sends(filter.Command);
                _kindRows[i].Label.Text = filter.Label;
                _kindRows[i].Label.Color = filter.IsCurrent ? Palette.Ink : Palette.Muted;

                if (_kindRows[i].Counted != filter.Count)
                {
                    _kindRows[i].Counted = filter.Count;
                    _kindRows[i].Count.Text = filter.Count.ToString();
                }
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
                Widgets.Row(option.Row);

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

                row.Row.Press = Deed.Sends(option.Command);

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

            int above = 0;
            int below = 0;
            for (int i = 0; i < stage.Entries.Count; i++)
            {
                if (stage.Entries[i].InWell)
                    below++;
                else
                    above++;
            }

            Fit(_visible, _rows, above, stage.HasIcons);
            Fit(_sunk, _well, below, stage.HasIcons);

            _wellRoom.Collapsed = below == 0;
            _wellTitle.Text = Widgets.Caps(stage.WellTitle ?? "");
            _wellTitle.Collapsed = below == 0 || string.IsNullOrEmpty(stage.WellTitle);

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

            // Nothing above the well is no room above it either.
            _rows.Collapsed = above == 0 && stage.Entries.Count > 0;

            int lifted = 0;
            int sunk = 0;
            for (int i = 0; i < stage.Entries.Count; i++)
            {
                PickOption entry = stage.Entries[i];
                RowVisual row = entry.InWell ? _sunk[sunk++] : _visible[lifted++];

                // A heading is words and nothing else: no mark, because there is nothing to choose,
                // and no press, because a row that answers the pointer and then does nothing is a
                // row somebody presses twice before believing it.
                row.Line.Press = entry.IsHeading ? Deed.Nothing : Moving(entry, many);

                // A heading starts where the marks do and sits against the first thing it names: the
                // mark's column goes, and the gap after it with it, and so does the room below.
                row.Left.Collapsed = entry.IsHeading;
                row.Line.Padding = entry.IsHeading ? HeadingPad : Pad;
                row.Line.Highlight = entry.IsHeading ? Palette.None : Palette.PanelHot;
                row.Mark.Collapsed = entry.IsHeading;

                // The box collects one at a time and says so whatever is held down, which is what it
                // is for. Only where several are being collected: where one is being settled on, a
                // dot is a picture of the state and there is nothing to collect.
                row.Mark.Press = entry.IsHeading || !many
                    ? Deed.Nothing
                    : Deed.Picks(entry.Command, Held.Also);

                row.Mark.Highlight = entry.IsHeading || !many ? Palette.None : Palette.PanelHot;

                // A tick for things you collect, a dot for things you settle on.
                row.Mark.Many = many;
                row.Mark.Chosen = entry.IsChosen;
                row.Mark.Color = entry.IsChosen ? Palette.Live : Palette.Faint;

                if (row.Icon != null)
                    row.Icon.Art = entry.Icon;

                row.Name.Text = entry.IsHeading ? Widgets.Caps(entry.Text) : entry.Text;
                row.Name.Color = entry.IsHeading ? Palette.Faint : Palette.Ink;

                string detail = DetailOf(entry);
                row.Detail.Text = detail;
                row.Detail.Collapsed = string.IsNullOrEmpty(detail);
            }
        }

        /// <summary>Makes a pool of rows as long as it needs to be, adding and taking away at its end.</summary>
        void Fit(List<RowVisual> pool, Stack into, int count, bool withIcon)
        {
            while (pool.Count < count)
            {
                RowVisual row = BuildRow(withIcon);
                pool.Add(row);
                into.Children.Add(row.Line);
            }

            while (pool.Count > count)
            {
                int last = pool.Count - 1;
                into.Children.Remove(pool[last].Line);
                pool.RemoveAt(last);
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

            // A stack, not a bar. A bar spans whatever width it is offered, which is exactly right for a
            // header and exactly wrong here: as the right-hand end of a row it claimed the whole line and
            // left the name to wrap one word at a time.
            Stack right = Widgets.RowOf(Space.Row);

            row.Line = new Bar();
            row.Line.Spacing = Space.Item;
            row.Line.Padding = Pad;
            Widgets.Row(row.Line);
            row.Line.Left = left;
            row.Left = left;
            row.Line.Fill = text;
            row.Line.Right = right;

            return row;
        }

        /// <summary>Taking a choice, and moving on with it where only one is being chosen.</summary>
        /// <remarks>
        /// Where several are being collected the row asks what was held down: plainly it is the whole
        /// answer, and with something held it joins what is there. Where one is being settled on there
        /// is nothing to qualify, so it is the plain send it always was.
        /// </remarks>
        static Deed Moving(PickOption entry, bool many)
        {
            if (many)
                return Deed.Picks(entry.Command);

            Deed deed = Deed.Sends(entry.Command);
            deed.Confirms = !many;
            return deed;
        }

        static string DetailOf(PickOption entry)
        {
            if (string.IsNullOrEmpty(entry.Secondary))
                return entry.Status ?? "";

            if (string.IsNullOrEmpty(entry.Status))
                return entry.Secondary;

            return entry.Secondary + "   " + entry.Status;
        }
    }
}
