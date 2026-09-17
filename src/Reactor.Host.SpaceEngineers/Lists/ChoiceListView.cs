using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Screen;
using Reactor.Host.SpaceEngineers.Layout;
using Reactor.Host.SpaceEngineers.Controls;
using Reactor.Host.SpaceEngineers.Look;

namespace Reactor.Host.SpaceEngineers.Lists
{
    /// <summary>
    /// A short list of named options.
    /// </summary>
    /// <remarks>
    /// An option standing for a repeat rate gets a light that blinks at that rate, because core supplies
    /// the interval and a speed you can watch is easier to judge than one you have to imagine. The flash
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
