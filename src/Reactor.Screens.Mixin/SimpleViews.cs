using System.Collections.Generic;
using Phosphor;
using Reactor.Ui;
using VRageMath;

namespace Reactor.Screens
{
    /// <summary>What a component view can ask of the screen it is drawn on.</summary>
    public interface IScreenHost
    {
        /// <summary>Hangs <paramref name="content" /> over the screen, under <paramref name="anchor" />.</summary>
        void ShowOverlay(Element content, Element anchor);

        /// <summary>Takes away whatever is hanging over the screen.</summary>
        void HideOverlay();

        /// <summary>Whether <paramref name="content" /> is the thing currently hanging over the screen.</summary>
        bool IsOverlay(Element content);

        /// <summary>Sends a number to core. Reordering is the one thing a view answers with a position.</summary>
        void SetNumber(int command, int value);

        /// <summary>Sends a switch's new state to core.</summary>
        void SetToggle(int command, bool on);
    }

    /// <summary>One component's elements, kept between ticks so values can be replaced rather than rebuilt.</summary>
    public abstract class ComponentView
    {
        /// <summary>What to put in the layout.</summary>
        public abstract Element Root { get; }

        /// <summary>
        /// Copies this tick's values onto the elements.
        /// </summary>
        /// <param name="tick">
        /// The editor's own count. Passed rather than fetched, because an indicator that blinks at a rate
        /// needs to know what time it is and core is the only thing that knows what a tick means here.
        /// </param>
        public abstract void Update(int tick);

        /// <summary>Adds whatever in here can be pressed. Called after the layout, never before.</summary>
        public virtual void Collect(List<Hit> hits)
        {
        }

        /// <summary>
        /// Offers this view the chance to take hold of something at <paramref name="point" />.
        /// </summary>
        /// <remarks>
        /// Asked before an ordinary press, so a grab does not also count as a click on whatever was under
        /// it. A view that takes hold is then given every move until the button comes back up.
        /// </remarks>
        public virtual bool TryGrab(Vector2 point)
        {
            return false;
        }

        /// <summary>Where the thing being carried has got to.</summary>
        public virtual void Carry(Vector2 point)
        {
        }

        /// <summary>Lets go, and puts the result wherever it belongs.</summary>
        public virtual void Drop()
        {
        }

        /// <summary>
        /// Offers a turn of the wheel at <paramref name="point" />, and answers whether it was used.
        /// </summary>
        /// <remarks>
        /// Only a view that scrolls itself has anything to do here, and it is handed the point because
        /// it may well have several regions that scroll - a picker has one per column. Everything else
        /// sits inside the screen's own scroller and is moved by it.
        /// </remarks>
        public virtual bool ScrollBy(Vector2 point, float amount)
        {
            return false;
        }

        /// <summary>
        /// Whether this manages its own scrolling and must not be wrapped in a scroller.
        /// </summary>
        /// <remarks>
        /// True for anything that scrolls sideways or scrolls several regions independently. Wrapping one
        /// of those in a vertical scroller gives two scrollers fighting over the same gesture.
        /// </remarks>
        public virtual bool ScrollsItself
        {
            get { return false; }
        }

        /// <summary>
        /// The screen this view is on, for the few things a view cannot do from inside the layout.
        /// </summary>
        /// <remarks>
        /// Opening something over the top is the case. A view draws where it was arranged, so anything
        /// that has to hang over its neighbours has to be handed to whoever draws last.
        /// </remarks>
        public IScreenHost Host { get; internal set; }

        public static ComponentView For(EditorComponent component)
        {
            var heading = component as HeadingComponent;
            if (heading != null)
                return new HeadingView(heading);

            var notice = component as NoticeComponent;
            if (notice != null)
                return new NoticeView(notice);

            var start = component as StartComponent;
            if (start != null)
                return new StartView(start);

            var choices = component as ChoiceListComponent;
            if (choices != null)
                return new ChoiceListView(choices);

            var status = component as StatusListComponent;
            if (status != null)
                return new StatusListView(status);

            var pick = component as PickComponent;
            if (pick != null)
                return new PickView(pick);

            var sequence = component as SequenceComponent;
            if (sequence != null)
                return new SequenceView(sequence);

            // Everything else, drawn loudly. A component this target has not learned to draw must not be
            // silently absent: a screen missing its picker looks exactly like a screen with nothing to
            // pick, and the second is a thing that legitimately happens.
            return new UnsupportedView(component);
        }
    }

    public sealed class HeadingView : ComponentView
    {
        readonly HeadingComponent _component;
        readonly Label _label = Widgets.Label("", Palette.Muted, Type.Small);

        public HeadingView(HeadingComponent component)
        {
            _component = component;
            _label.Padding = new Thickness(Space.Line, Space.Item, 0f, Space.Row);
        }

        public override Element Root
        {
            get { return _label; }
        }

        public override void Update(int tick)
        {
            _label.Text = _component.Text;
        }
    }

    public sealed class NoticeView : ComponentView
    {
        readonly NoticeComponent _component;
        readonly Label _label = Widgets.Wash("", Palette.Problem, Palette.ProblemWash);

        public NoticeView(NoticeComponent component)
        {
            _component = component;
        }

        public override Element Root
        {
            get { return _label; }
        }

        public override void Update(int tick)
        {
            bool problem = _component.Level == NoticeLevel.Problem;

            _label.Text = _component.Text;
            _label.Color = problem ? Palette.Problem : Palette.Notice;
            _label.Background = problem ? Palette.ProblemWash : Palette.NoticeWash;
        }
    }

    /// <summary>
    /// The ways to start something new.
    /// </summary>
    /// <remarks>
    /// Everything in here adds something, so everything in here gets a plus. Core does not spell that
    /// into the labels - it says these are the ways to start something, which is already the whole of the
    /// claim - and drawing it as a plus is this target agreeing with the other one.
    /// <para>
    /// A line closes the block off, because what follows is a different kind of thing: above is what can
    /// be added, below is what is already there.
    /// </para>
    /// </remarks>
    public sealed class StartView : ComponentView
    {
        public const string Plus = "+ ";

        readonly StartComponent _component;
        readonly Stack _stack = Widgets.Column(Space.Row);
        readonly List<Label> _rows = new List<Label>();
        readonly Rule _line = Widgets.Line();

        int _count = -1;

        public StartView(StartComponent component)
        {
            _component = component;
        }

        public override Element Root
        {
            get { return _stack; }
        }

        public override void Update(int tick)
        {
            if (_count == _component.Options.Count)
                return;

            _count = _component.Options.Count;
            _rows.Clear();

            for (int i = 0; i < _component.Options.Count; i++)
            {
                StartOption option = _component.Options[i];

                // An ellipsis on the ones that will ask something, which is the convention every desktop
                // already uses for "this opens a question rather than doing it".
                _rows.Add(Widgets.Row(Plus + option.Label + (option.Asks ? "..." : ""), Palette.Ink));
            }

            _line.Padding = new Thickness(0f, Space.Item);

            _stack.Children.Clear();
            for (int i = 0; i < _rows.Count; i++)
                _stack.Children.Add(_rows[i]);

            _stack.Children.Add(_line);
        }

        public override void Collect(List<Hit> hits)
        {
            for (int i = 0; i < _rows.Count && i < _component.Options.Count; i++)
            {
                StartOption option = _component.Options[i];

                // The row is what the question hangs under, so it is handed over with the hit. Nothing
                // here opens anything: the row does not know a popup exists, only that pressing it asks.
                hits.Add(new Hit(_rows[i].Bounds, option.Command, option.Question, _rows[i]));
            }
        }
    }

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
                row.Line.Background = entry.IsCurrent ? Palette.PanelHot : Palette.Panel;

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
                hits.Add(new Hit(_rows[i].Line.Bounds, _component.Entries[i].Command));
        }

        void Rebuild()
        {
            _rows.Clear();
            _stack.Children.Clear();

            if (_component.Entries.Count == 0)
            {
                Label empty = Widgets.Label(_component.EmptyText, Palette.Muted, Type.Small);
                empty.Padding = new Thickness(Space.Row, Space.Row, 0f, 0f);
                _stack.Children.Add(empty);
                return;
            }

            for (int i = 0; i < _component.Entries.Count; i++)
            {
                ChoiceEntry entry = _component.Entries[i];

                var row = new Row();
                row.Ticks = entry.IntervalTicks;
                row.Text = Widgets.Label("", Palette.Ink, Type.Body);

                // Told as well as shown, and told before the choice rather than after it.
                row.Warning = Widgets.Label("", Palette.Notice, Type.Tiny);
                row.Current = Widgets.Label("", Palette.Live, Type.Small);

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

    /// <summary>Named things that are doing something, one line each.</summary>
    public sealed class StatusListView : ComponentView
    {
        readonly StatusListComponent _component;
        readonly Stack _stack = Widgets.Column(Space.Row);
        readonly List<StatusRow> _rows = new List<StatusRow>();

        int _revision = -1;

        public StatusListView(StatusListComponent component)
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
                _rows[i].Update(_component.Entries[i]);
        }

        public override void Collect(List<Hit> hits)
        {
            for (int i = 0; i < _rows.Count && i < _component.Entries.Count; i++)
            {
                StatusEntry entry = _component.Entries[i];
                _rows[i].Collect(hits, entry);
            }
        }

        void Rebuild()
        {
            _rows.Clear();
            _stack.Children.Clear();

            if (_component.Entries.Count == 0)
            {
                Label empty = Widgets.Label(_component.EmptyText, Palette.Muted, Type.Small);
                empty.Padding = new Thickness(Space.Row, Space.Row, 0f, 0f);
                _stack.Children.Add(empty);
                return;
            }

            for (int i = 0; i < _component.Entries.Count; i++)
            {
                var row = new StatusRow();
                _rows.Add(row);
                _stack.Children.Add(row.Root);
            }
        }
    }

    /// <summary>
    /// One live thing, on one line: what it is, what it is doing, when it last did anything.
    /// </summary>
    /// <remarks>
    /// Indented by <c>Depth</c>, which core promises is never more than one level, so a fixed indent is
    /// right rather than lazy - there is no tree here to reinvent.
    /// </remarks>
    public sealed class StatusRow
    {
        /// <summary>
        /// How far a nested row steps in, on both sides.
        /// </summary>
        /// <remarks>
        /// Both, not just the left. The left edge already says "this belongs to the row above"; the right
        /// edge did not, so a switch on a nested row landed in the same column as the switch governing it,
        /// and nothing but reading the labels told them apart.
        /// </remarks>
        const float Inset = 35f;

        readonly Bar _line = new Bar();
        readonly Dot _light = new Dot();
        readonly Label _summary = Widgets.Label("", Palette.Ink, Type.Body);
        readonly Label _status = Widgets.Label("", Palette.Faint, Type.Tiny);
        readonly Label _lastRun = Widgets.Label("", Palette.Faint, Type.Tiny);
        readonly Label _problem = Widgets.Label("", Palette.Problem, Type.Tiny);
        readonly Switch _switch = new Switch(Type.Small);
        readonly Stack _text = Widgets.Column(Space.Line * 0.6f);
        readonly Stack _detail = Widgets.RowOf(Space.Item);

        public StatusRow()
        {
            _detail.Children.Add(_status);
            _detail.Children.Add(_lastRun);

            _text.Children.Add(_summary);
            _text.Children.Add(_detail);
            _text.Children.Add(_problem);

            _line.Spacing = Space.PadX;
            _line.Background = Palette.Panel;
            _line.Left = _light;
            _line.Fill = _text;
            _line.Right = _switch;
        }

        public Element Root
        {
            get { return _line; }
        }

        public void Update(StatusEntry entry)
        {
            float side = Space.PadX + (entry.Depth > 0 ? Inset : 0f);
            _line.Padding = new Thickness(side, Space.PadY, side, Space.PadY);

            // The same plus the start block uses, for the same reason: this row adds something. Core marks
            // it as an action and leaves the drawing to us.
            _summary.Text = entry.IsAction ? StartView.Plus + entry.Summary : entry.Summary;
            _summary.Color = entry.IsAction ? Palette.Muted : Palette.Ink;

            // A row that does something has no state to show, so it gets no light - only the space where
            // one would be, which keeps its text lined up with its neighbours.
            _light.Background = entry.IsAction
                ? Palette.None
                : (entry.JustRan ? Palette.Live : Palette.Dim);

            _status.Text = entry.Status;
            _lastRun.Text = entry.LastRun;

            // Out of the layout when there is nothing to say, not merely blank. A row that always
            // reserved both of these would be three lines tall to show one, and a list of them would be
            // a column of mostly empty panel.
            _detail.Collapsed = string.IsNullOrEmpty(entry.Status) && string.IsNullOrEmpty(entry.LastRun);

            // Its own line rather than a replacement for the status. What went wrong and what it is doing
            // are two different readings, and losing one to show the other is how a diagnosis screen lies.
            _problem.Text = entry.Problem;
            _problem.Collapsed = string.IsNullOrEmpty(entry.Problem);

            // Says what state it is in, the way the game's own on/off control does. The light on the
            // left of this row says whether the thing is doing something right now; this says whether it
            // has been left switched on, which is not the same question and is the one a switch answers.
            if (entry.CanSwitch)
                _switch.Show(entry.SwitchIsOn, entry.SwitchGovernsAll);
            else
                _switch.Hide();
        }

        public void Collect(List<Hit> hits, StatusEntry entry)
        {
            // No command means there is nothing to press. A system's own row is a heading over the ones of
            // it, and a heading that answers to a press is a promise it cannot keep.
            if (entry.Command != 0)
                hits.Add(new Hit(_line.Bounds, entry.Command));

            // Added after the row, so the switch wins where the two overlap.
            if (entry.CanSwitch)
                hits.Add(Hit.Switch(_switch.Bounds, entry.SwitchCommand, !entry.SwitchIsOn));
        }
    }

    /// <summary>A component with no renderer yet, drawn so that it cannot be mistaken for absence.</summary>
    public sealed class UnsupportedView : ComponentView
    {
        readonly Label _label;

        public UnsupportedView(EditorComponent component)
        {
            _label = Widgets.Wash(
                component.GetType().Name + " is not drawn on this target yet",
                Palette.Ink, new Color(220, 60, 190));
        }

        public override Element Root
        {
            get { return _label; }
        }

        public override void Update(int tick)
        {
        }
    }
}
