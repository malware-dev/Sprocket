using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Core.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Look;

namespace Reactor.Host.SpaceEngineers.Presentation.Lists
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

        public ChoiceListView(ChoiceListComponent component)
        {
            _component = component;
        }

        public override Element Root
        {
            get { return _stack; }
        }

        public override void Update(int tick)
        {
            if (_revision != _component.Entries.Revision)
            {
                _revision = _component.Entries.Revision;
                Rebuild();
            }

            for (int i = 0; i < _rows.Count && i < _component.Entries.Count; i++)
            {
                ChoiceEntry entry = _component.Entries[i];
                Row row = _rows[i];

                row.Text.Text = entry.Text;
                row.Warning.Text = entry.Warning;
                row.Warning.Collapsed = string.IsNullOrEmpty(entry.Warning);
                row.Current.Text = entry.IsCurrent ? "*" : "";
                // The one already chosen is drawn in the highlight tone at rest, so hovering it has to go
                // a step further again or it would look like nothing happened.
                row.Line.Background = entry.IsCurrent ? Palette.PanelHot : Palette.Panel;
                row.Line.Highlight = entry.IsCurrent ? Palette.Line : Palette.PanelHot;

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

        public override void Collect(List<Hit> hits)
        {
            for (int i = 0; i < _rows.Count && i < _component.Entries.Count; i++)
                hits.Add(Hit.On(_rows[i].Line, _component.Entries[i].Command));
        }

        void Rebuild()
        {
            _rows.Clear();
            _stack.Children.Clear();

            if (_component.Entries.Count == 0)
            {
                Label empty = Widgets.Label(_component.EmptyText, Palette.Muted, Lettering.Small);
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
