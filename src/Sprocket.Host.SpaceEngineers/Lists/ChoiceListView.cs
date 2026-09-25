using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;
using TextBox = Sprocket.Host.SpaceEngineers.Ui.Legacy.TextBox;

namespace Sprocket.Host.SpaceEngineers.Lists
{
    /// <summary>
    /// A short list of named options.
    /// </summary>
    /// <remarks>
    /// An option standing for a repeat rate gets a light that blinks at that rate, because core supplies
    /// the interval and a speed that can be watched is easier to judge than one that has to be imagined. The flash
    /// is short rather than half the cycle: at even duty a ten-second rate would sit lit for five and
    /// read as "on" rather than as "rarely".
    /// </remarks>
    public sealed class ChoiceListView : ComponentView
    {
        sealed class Row
        {
            public Bar Line;
            public Dot Blink;
            public Label Text;
            public Label Warning;
            public Label Current;
            public TextBox Box;
            public Label Take;
            public bool Taking;
            public int Ticks;
        }

        readonly ChoiceListComponent _component;
        readonly Stack _stack = Widgets.Column(Space.Row);
        readonly List<Row> _rows = new List<Row>();

        int _revision = -1;

        /// <summary>What is said while there is nothing to choose from, held so it can be said again.</summary>
        Label _empty;

        public ChoiceListView(ChoiceListComponent component)
        {
            _component = component;
        }

        /// <summary>
        /// Told rather than asked. The list says when it changes, a choice's values are written once
        /// when the page builds them, and the light that blinks is not a value at all.
        /// </summary>
        protected override bool Stale
        {
            get { return false; }
        }

        public override Element Root
        {
            get { return _stack; }
        }

        protected override void Refresh()
        {
            if (_empty != null)
                _empty.Text = _component.EmptyText;

            if (_revision != _component.Entries.Revision)
            {
                _revision = _component.Entries.Revision;
                Rebuild();
            }

            for (int i = 0; i < _rows.Count && i < _component.Entries.Count; i++)
            {
                ChoiceEntry entry = _component.Entries[i];
                Row row = _rows[i];

                if (row.Box != null)
                {
                    Typed(row, entry);
                    continue;
                }

                row.Line.Press = Deed.Sends(entry.Command);
                row.Text.Text = entry.Text;
                row.Warning.Text = entry.Warning;
                row.Warning.Collapsed = string.IsNullOrEmpty(entry.Warning);
                row.Current.Text = entry.IsCurrent ? "*" : "";
                // The one already chosen is drawn in the highlight tone at rest, so hovering it has to go
                // a step further again or it would look like nothing happened.
                row.Line.Background = entry.IsCurrent ? Palette.PanelHot : Palette.Panel;
                row.Line.Highlight = entry.IsCurrent ? Palette.Line : Palette.PanelHot;
            }
        }

        /// <summary>
        /// Blinks the light on a row that stands for something that repeats, at that thing's own rate.
        /// </summary>
        /// <remarks>
        /// The one thing on this list that changes without anybody doing anything, so the one thing
        /// here that belongs on every tick.
        /// </remarks>
        protected override void Moved(int tick)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];

                // The box is typed into without the list hearing about each key, so whether there is
                // anything to take is read off it here: one length, on a row that has a box, and the
                // button dressed again only when that answer changes.
                if (row.Box != null)
                {
                    if (row.Taking != row.Box.Text.Length > 0)
                        Dress(row);

                    continue;
                }

                if (row.Blink == null)
                    continue;

                int flash = row.Ticks / 2;
                if (flash < 1)
                    flash = 1;
                if (flash > 6)
                    flash = 6;

                row.Blink.IsLit = row.Ticks <= 1 || tick % row.Ticks < flash;
            }
        }

        /// <summary>A row that is typed into: its box takes the keyboard, and Enter in it takes what is there.</summary>
        void Typed(Row row, ChoiceEntry entry)
        {
            row.Box.Hint = entry.Hint;

            // Put back only while nobody is typing into it, the way a search box is: whatever is in the
            // box is the truth while it is being typed.
            var view = Host as EditorView;
            if (view == null || view.Focus != row.Box)
                row.Box.Text = entry.Typed;

            row.Box.Press = Deed.Types(entry.TextCommand, row.Box, entry.Command);
            row.Take.Text = entry.Text;
            row.Take.Press = Deed.Sends(entry.Command);
            Dress(row);
        }

        /// <summary>The button lit while the box holds something to take, and a dim well while it holds nothing.</summary>
        static void Dress(Row row)
        {
            row.Taking = row.Box.Text.Length > 0;

            if (row.Taking)
                Widgets.Go(row.Take);
            else
                Widgets.Waiting(row.Take);
        }

        void Rebuild()
        {
            _rows.Clear();
            _stack.Children.Clear();
            _empty = null;

            if (_component.Entries.Count == 0)
            {
                _empty = Widgets.Label(_component.EmptyText, Palette.Muted, Lettering.Small);
                Label empty = _empty;
                empty.Padding = new Thickness(Space.Row, Space.Row, 0f, 0f);
                _stack.Children.Add(empty);
                return;
            }

            for (int i = 0; i < _component.Entries.Count; i++)
            {
                ChoiceEntry entry = _component.Entries[i];

                var row = new Row();

                if (entry.TextCommand != 0)
                {
                    row.Box = Widgets.Box(Lettering.Body, Palette.Page);
                    row.Take = Widgets.Label("", Palette.Ink, Lettering.Small);
                    row.Take.Wraps = false;
                    row.Line = new Bar();
                    row.Line.Spacing = Space.PadX;
                    row.Line.Padding = new Thickness(Space.PadX, Space.PadY);
                    row.Line.Background = Palette.Panel;
                    row.Line.Fill = row.Box;
                    row.Line.Right = row.Take;

                    _rows.Add(row);
                    _stack.Children.Add(row.Line);
                    continue;
                }

                row.Ticks = entry.IntervalTicks;
                row.Text = Widgets.Label("", Palette.Ink, Lettering.Body);

                // Told as well as shown, and told before the choice rather than after it.
                row.Warning = Widgets.Label("", Palette.Notice, Lettering.Tiny);
                row.Current = Widgets.Label("", Palette.Live, Lettering.Small);

                Stack text = Widgets.Column(Space.Line * 0.6f);
                text.Children.Add(row.Text);
                text.Children.Add(row.Warning);

                row.Line = new Bar();
                row.Line.Spacing = Space.PadX;
                row.Line.Padding = new Thickness(Space.PadX, Space.PadY);
                row.Line.Background = Palette.Panel;
                row.Line.Fill = text;
                row.Line.Right = row.Current;

                if (entry.IntervalTicks > 0)
                {
                    row.Blink = new Dot();
                    row.Line.Left = row.Blink;
                }

                _rows.Add(row);
                _stack.Children.Add(row.Line);
            }
        }
    }
}
