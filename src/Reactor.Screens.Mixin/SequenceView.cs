using System.Collections.Generic;
using Phosphor;
using Reactor.Ui;
using VRageMath;

namespace Reactor.Screens
{
    /// <summary>
    /// A sequence: the sentence, its steps, and what it is doing right now.
    /// </summary>
    /// <remarks>
    /// The live state here is not instrumentation bolted on afterwards, it is the reason this is more than
    /// an editor. Watching a sequence arm, fire, sit on step three and skip a re-trigger is what saves
    /// somebody from having to run the program in their head.
    /// <para>
    /// While it is running there is no editor to speak of: the grips, the delete marks and the adders are
    /// gone rather than greyed, and the slots read as words instead of chips. What is on screen then is a
    /// view of a system working, not an editor with its hands tied.
    /// </para>
    /// </remarks>
    public sealed class SequenceView : ComponentView
    {
        sealed class StepRow
        {
            public Bar Line;
            public Grip Grip;
            public Label Number;
            public SentenceView Sentence;
            public Glyph Delete;
            public int Position;

            /// <summary>Where this row is being drawn relative to where the layout put it.</summary>
            public float Shift;

            /// <summary>Where it is heading. The two differ only while it is on its way.</summary>
            public float Wanted;
        }

        /// <summary>The gap under each step, which the drag arithmetic has to agree with.</summary>
        const float RowGap = Space.Row * 0.8f;

        readonly SequenceComponent _component;
        readonly Stack _root = Widgets.Column(Space.Item);

        readonly Bar _header = new Bar();
        readonly Label _status = Widgets.Label("", Palette.Muted, Type.Small);
        readonly Label _lastRun = Widgets.Label("", Palette.Faint, Type.Small);
        readonly Label _skipped = Widgets.Wash("", Palette.Notice, Palette.NoticeWash);
        readonly Glyph _undo = Widgets.Chevron(0f, Palette.Muted);
        readonly Glyph _redo = Widgets.Chevron(Symbols.TurnedRight, Palette.Muted);

        readonly Sized _undoSlot;
        readonly Sized _redoSlot;
        readonly Switch _enable = new Switch(Type.Small);
        readonly Label _delete = Widgets.Ghost(Widgets.Caps("Delete"));

        readonly Label _locked = Widgets.Wash("", Palette.Muted, Palette.SlotFill);
        readonly Label _problem = Widgets.Wash("", Palette.Problem, Palette.ProblemWash);

        readonly Wrap _trigger = new Wrap();
        readonly Dot _ranLight = new Dot();
        readonly Label _triggerLead = Widgets.Label("", Palette.Muted, Type.Body);
        readonly Label _liveBadge = Widgets.Wash("true right now", Palette.Live, Palette.LiveWash);
        readonly List<Element> _triggerSlots = new List<Element>();

        readonly Stack _steps = Widgets.Column(Space.Row * 0.8f);
        readonly List<StepRow> _stepRows = new List<StepRow>();
        readonly Stack _adders = Widgets.RowOf(Space.Item);
        readonly List<Label> _adderRows = new List<Label>();

        bool _builtEditable;
        bool _built;

        /// <summary>The step being carried, or -1.</summary>
        int _carrying = -1;

        float _grabbedAt;
        float _moved;

        public SequenceView(SequenceComponent component)
        {
            _component = component;

            _status.Wraps = false;
            _lastRun.Wraps = false;
            _skipped.Wraps = false;
            _liveBadge.Wraps = false;
            _delete.Wraps = false;

            // Both hold their place for the whole time the sentence is editable and go quiet when there is
            // nothing to walk back. Hiding them would move Run and Delete sideways every time somebody
            // made or undid a change, under the pointer they were about to use.
            _undoSlot = new Sized(48f);
            _undoSlot.Content = _undo;

            _redoSlot = new Sized(48f);
            _redoSlot.Content = _redo;

            Stack state = Widgets.RowOf(Space.Item);
            state.Children.Add(_status);
            state.Children.Add(_lastRun);

            Stack tools = Widgets.RowOf(Space.Item);
            tools.Children.Add(_undoSlot);
            tools.Children.Add(_redoSlot);
            tools.Children.Add(_enable);
            tools.Children.Add(_delete);

            _header.Spacing = Space.Region;
            _header.Left = state;
            _header.Fill = _skipped;
            _header.Right = tools;

            _trigger.ItemSpacing = Space.Item;
            _trigger.LineSpacing = Space.Row;

            _adders.Padding = new Thickness(32f, Space.Item, 0f, 0f);
        }

        public override Element Root
        {
            get { return _root; }
        }

        public override void Update(int tick)
        {
            if (!_built || _builtEditable != _component.Editable || !SameShape())
                Build();

            _status.Text = _component.Status;
            _lastRun.Text = _component.LastRun;
            _ranLight.Background = _component.JustRan ? Palette.Live : Palette.Dim;

            bool skipping = _component.SkippedCount > 0;
            _skipped.Collapsed = !skipping;
            if (skipping)
                _skipped.Text = "skipped " + _component.SkippedCount + "x while busy";

            // Absent rather than blank. A banner with nothing to say still holds a line, and a strip of
            // empty chrome above the sentence is worse than the jump of one appearing when it has news.
            Banner(_locked, _component.Editable ? "" : _component.LockedText, 1);
            Banner(_problem, _component.Problem, 2);

            // Idle while the signal reads true is not a contradiction. It is the difference between a
            // moment and a situation, shown rather than explained.
            _liveBadge.Collapsed = !_component.Trigger.IsLiveNow;

            // Governs, because a sequence's switch is the only one on this screen and switching it off
            // stops the whole of what the screen is showing.
            _enable.Show(_component.Enabled, true);

            Waiting(_undo, _component.UndoCommand);
            Waiting(_redo, _component.RedoCommand);

            _triggerLead.Text = _component.Trigger.Lead;
            Fill(_triggerSlots, _component.Trigger.Slots);

            for (int i = 0; i < _stepRows.Count && i < _component.Steps.Count; i++)
            {
                SentenceStep step = _component.Steps[i];
                StepRow row = _stepRows[i];

                row.Number.Text = step.Number.ToString();
                row.Sentence.Update(step.Lead, step.Slots);

                // Left alone while it is being carried. This runs every tick, and it would otherwise
                // repaint the row being dragged in the colour it is not, sixty times a second.
                if (_carrying < 0)
                    row.Line.Background = step.IsCurrent ? Palette.LiveWash : Palette.None;
            }

            Slide();
        }

        public override void Collect(List<Hit> hits)
        {
            if (_component.Editable)
            {
                if (_component.UndoCommand != 0)
                    hits.Add(new Hit(_undo.Bounds, _component.UndoCommand));

                if (_component.RedoCommand != 0)
                    hits.Add(new Hit(_redo.Bounds, _component.RedoCommand));

                hits.Add(new Hit(_delete.Bounds, _component.DeleteCommand));

                for (int i = 0; i < _triggerSlots.Count && i < _component.Trigger.Slots.Count; i++)
                    hits.Add(new Hit(_triggerSlots[i].Bounds, _component.Trigger.Slots[i].Command));

                for (int i = 0; i < _stepRows.Count && i < _component.Steps.Count; i++)
                {
                    SentenceStep step = _component.Steps[i];
                    StepRow row = _stepRows[i];

                    row.Sentence.Collect(hits, step.Slots);
                    hits.Add(new Hit(row.Delete.Bounds, step.DeleteCommand));
                }

                for (int i = 0; i < _adderRows.Count && i < _component.AddOptions.Count; i++)
                    hits.Add(new Hit(_adderRows[i].Bounds, _component.AddOptions[i].Command));
            }

            hits.Add(Hit.Switch(_enable.Bounds, _component.ToggleCommand, !_component.Enabled));
        }

        public override bool TryGrab(Vector2 point)
        {
            if (!_component.Editable)
                return false;

            for (int i = 0; i < _stepRows.Count; i++)
            {
                StepRow row = _stepRows[i];

                if (!EditorView.Contains(row.Grip.Bounds, point))
                    continue;

                _carrying = i;
                _grabbedAt = point.Y;
                _moved = 0f;

                // The carried line is the one row with no easing on it: it tracks the pointer exactly,
                // and everything else slides out of its way.
                row.Shift = 0f;
                row.Wanted = 0f;
                row.Line.Background = Palette.SlotFill;
                return true;
            }

            return false;
        }

        public override void Carry(Vector2 point)
        {
            if (_carrying < 0)
                return;

            _moved = point.Y - _grabbedAt;
            MakeRoom(LandingFor(_moved));
        }

        public override void Drop()
        {
            if (_carrying < 0)
                return;

            int from = _carrying;
            int to = LandingFor(_moved);

            _carrying = -1;
            _moved = 0f;

            for (int i = 0; i < _stepRows.Count; i++)
                _stepRows[i].Wanted = 0f;

            if (to == from || from >= _component.Steps.Count)
                return;

            // Everything stays where the drag put it and the carried line drops into the gap being held
            // for it, so the rebuild that follows changes nothing anybody can see. Putting it all back
            // first was the obvious thing and it flashed: the model does not change until the command is
            // sent, so for one frame the list would render in its old order.
            _stepRows[from].Shift = Landing(from, to) - _stepRows[from].Line.Bounds.Y + _stepRows[from].Shift;
            _stepRows[from].Wanted = _stepRows[from].Shift;

            Host.SetNumber(_component.Steps[from].MoveToCommand, to);
        }

        /// <summary>
        /// Opens a gap where the line would land.
        /// </summary>
        /// <remarks>
        /// The rows between where it came from and where it is going step aside by exactly its height, so
        /// the space it would occupy is visibly empty rather than having to be imagined from a pointer
        /// position. They slide rather than jump, because a row that appeared in a new place would read as
        /// the list glitching rather than as the list answering.
        /// </remarks>
        void MakeRoom(int to)
        {
            if (_carrying < 0 || _carrying >= _stepRows.Count)
                return;

            float height = _stepRows[_carrying].Line.DesiredSize.Y + RowGap;

            for (int i = 0; i < _stepRows.Count; i++)
            {
                if (i == _carrying)
                    continue;

                float shift = 0f;

                if (_carrying < to && i > _carrying && i <= to)
                    shift = -height;
                else if (_carrying > to && i >= to && i < _carrying)
                    shift = height;

                _stepRows[i].Wanted = shift;
            }
        }

        /// <summary>
        /// Where the line being carried would land, judged by where the line is.
        /// </summary>
        /// <remarks>
        /// By the line, not by the pointer. Somebody grabs a row wherever the grip happens to be under
        /// their finger and then aims with the row, because that is the thing they can see moving. Judging
        /// by the pointer puts the gap a grab-offset away from the row hovering over it.
        /// <para>
        /// Counting how many other rows the carried one has passed gives the answer directly, with no
        /// correcting for the row being lifted out: the ones it has passed are exactly the ones that end
        /// up above it. Measured against where the rows were laid out rather than where they have slid to,
        /// since the sliding is a picture of this answer and reading it back would chase its own tail.
        /// </para>
        /// </remarks>
        int LandingFor(float moved)
        {
            if (_carrying < 0)
                return 0;

            RectangleF carried = _stepRows[_carrying].Line.Bounds;
            float middle = carried.Y - _stepRows[_carrying].Shift + carried.Height * 0.5f + moved;

            int index = 0;
            for (int i = 0; i < _stepRows.Count; i++)
            {
                if (i == _carrying)
                    continue;

                RectangleF bounds = _stepRows[i].Line.Bounds;
                float top = bounds.Y - _stepRows[i].Shift;

                if (middle > top + bounds.Height * 0.5f)
                    index++;
            }

            return index;
        }

        /// <summary>
        /// Where the top of the carried row belongs once it has landed.
        /// </summary>
        /// <remarks>
        /// Added up from the rows that will be above it rather than worked out from an offset, because the
        /// rows are not all the same height: a wait is a shorter line than an action.
        /// </remarks>
        float Landing(int from, int to)
        {
            float top = _stepRows[0].Line.Bounds.Y - _stepRows[0].Shift;
            int placed = 0;

            for (int i = 0; i < _stepRows.Count && placed < to; i++)
            {
                if (i == from)
                    continue;

                top += _stepRows[i].Line.DesiredSize.Y + RowGap;
                placed++;
            }

            return top;
        }

        /// <summary>
        /// Moves every row a little closer to where it should be.
        /// </summary>
        /// <remarks>
        /// There is no animation clock here, so the sliding is done a frame at a time from the tick the
        /// view is handed anyway. Short and eased-out, because this is feedback rather than decoration.
        /// </remarks>
        void Slide()
        {
            for (int i = 0; i < _stepRows.Count; i++)
            {
                StepRow row = _stepRows[i];

                float target = i == _carrying ? _moved : row.Wanted;

                if (i == _carrying)
                    row.Shift = target;
                else
                    row.Shift += (target - row.Shift) * 0.35f;

                if (row.Shift < 0.5f && row.Shift > -0.5f && target == 0f)
                    row.Shift = 0f;

                row.Line.Offset = new Vector2(0f, row.Shift);
            }
        }

        /// <summary>Whether the sentence still has the same number of parts in the same places.</summary>
        bool SameShape()
        {
            if (_stepRows.Count != _component.Steps.Count)
                return false;

            if (_triggerSlots.Count != _component.Trigger.Slots.Count)
                return false;

            for (int i = 0; i < _stepRows.Count; i++)
            {
                if (!_stepRows[i].Sentence.Fits(_component.Steps[i].Slots.Count))
                    return false;
            }

            return true;
        }

        void Build()
        {
            _built = true;
            _builtEditable = _component.Editable;

            _root.Children.Clear();
            _root.Children.Add(_header);

            BuildTrigger();
            _root.Children.Add(_trigger);

            BuildSteps();
            _root.Children.Add(_steps);

            if (_component.Editable)
            {
                BuildAdders();
                _root.Children.Add(_adders);
            }

            // Gone rather than greyed, so what is on screen while it runs is a view of a system working.
            // Walking a change back is not something a running sequence can offer at all, so the pair go
            // with the rest of the editor rather than sitting there dimmed as if they might yet answer.
            _delete.Collapsed = !_component.Editable;
            _undoSlot.Collapsed = !_component.Editable;
            _redoSlot.Collapsed = !_component.Editable;
        }

        void BuildTrigger()
        {
            _trigger.Children.Clear();
            _triggerSlots.Clear();

            _trigger.Children.Add(_ranLight);
            _trigger.Children.Add(_triggerLead);

            for (int i = 0; i < _component.Trigger.Slots.Count; i++)
            {
                Element slot = NewSlot();
                _triggerSlots.Add(slot);
                _trigger.Children.Add(slot);
            }

            _trigger.Children.Add(_liveBadge);
        }

        void BuildSteps()
        {
            _steps.Children.Clear();
            _stepRows.Clear();

            for (int i = 0; i < _component.Steps.Count; i++)
            {
                SentenceStep step = _component.Steps[i];

                var row = new StepRow();
                row.Position = step.Position;

                // A grip rather than the whole line, because the line is full of chips and "press
                // anywhere that is not one" is a rule nobody can see. A handle says where to grab, and
                // says that grabbing is a thing you can do at all.
                row.Grip = new Grip(Palette.Faint);
                row.Grip.Collapsed = !_component.Editable;

                row.Number = Widgets.Label("", Palette.Faint, Type.Small);
                row.Number.Wraps = false;

                var grip = new Sized(26f);
                grip.Content = row.Grip;
                grip.Collapsed = !_component.Editable;

                var number = new Sized(29f);
                number.Content = row.Number;

                Stack handle = Widgets.RowOf(Space.Line);
                handle.Children.Add(grip);
                handle.Children.Add(number);

                row.Sentence = new SentenceView(_component.Editable, step.Slots.Count);
                row.Delete = Widgets.Symbol(Symbols.Cross, Palette.Faint);
                row.Delete.Collapsed = !_component.Editable;

                row.Line = new Bar();
                row.Line.Spacing = Space.Item;
                row.Line.Padding = new Thickness(Space.Item, Space.ChipY * 1.3f);
                row.Line.Left = handle;
                row.Line.Fill = row.Sentence.Root;
                row.Line.Right = row.Delete;

                _stepRows.Add(row);
                _steps.Children.Add(row.Line);
            }
        }

        void BuildAdders()
        {
            _adders.Children.Clear();
            _adderRows.Clear();

            for (int i = 0; i < _component.AddOptions.Count; i++)
            {
                Label adder = Widgets.Ghost("+ " + _component.AddOptions[i].Text);
                _adderRows.Add(adder);
                _adders.Children.Add(adder);
            }
        }

        Element NewSlot()
        {
            if (!_component.Editable)
            {
                // Reading, not editing: a slot becomes a word in a sentence rather than something to press.
                Label word = Widgets.Label("", Palette.Ink, Type.Body);
                word.Wraps = false;
                return word;
            }

            return new Slot();
        }

        void Fill(List<Element> slots, List<SentenceSlot> model)
        {
            for (int i = 0; i < slots.Count && i < model.Count; i++)
                SentenceView.Apply(slots[i], model[i]);
        }

        /// <summary>Shows a banner when it has something to say, and takes it out of the layout when not.</summary>
        void Banner(Label banner, string text, int at)
        {
            bool wanted = !string.IsNullOrEmpty(text);
            bool present = banner.Parent == _root;

            if (wanted)
                banner.Text = text;

            if (wanted == present)
                return;

            if (!wanted)
            {
                _root.Children.Remove(banner);
                return;
            }

            // Put back in its own place rather than on the end, so the two banners always read in the
            // same order however they came and went.
            var order = new List<Element>();
            for (int i = 0; i < _root.Children.Count; i++)
                order.Add(_root.Children[i]);

            int index = at <= 1 ? 1 : Index(order, _locked) + 1;
            if (index > order.Count)
                index = order.Count;

            order.Insert(index, banner);

            _root.Children.Clear();
            for (int i = 0; i < order.Count; i++)
                _root.Children.Add(order[i]);
        }

        static int Index(List<Element> order, Element element)
        {
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i] == element)
                    return i;
            }

            return 0;
        }

        /// <summary>Quiet when there is nothing to walk back, and in place either way.</summary>
        static void Waiting(Glyph button, int command)
        {
            button.Tint = command != 0 ? Palette.Muted : Palette.Dim;
        }
    }

    /// <summary>One sentence: a lead-in, then the slots that complete it, wrapping when it has to.</summary>
    public sealed class SentenceView
    {
        readonly Wrap _wrap = new Wrap();
        readonly Label _lead;
        readonly List<Element> _slots = new List<Element>();

        public SentenceView(bool editable, int slots)
        {
            _wrap.ItemSpacing = Space.Item;
            _wrap.LineSpacing = Space.Row;

            _lead = Widgets.Label("", Palette.Muted, Type.Body);
            _lead.Wraps = false;
            _wrap.Children.Add(_lead);

            for (int i = 0; i < slots; i++)
            {
                Element slot;

                if (editable)
                {
                    slot = new Slot();
                }
                else
                {
                    Label word = Widgets.Label("", Palette.Ink, Type.Body);
                    word.Wraps = false;
                    slot = word;
                }

                _slots.Add(slot);
                _wrap.Children.Add(slot);
            }
        }

        public Element Root
        {
            get { return _wrap; }
        }

        public bool Fits(int slots)
        {
            return _slots.Count == slots;
        }

        public void Update(string lead, List<SentenceSlot> slots)
        {
            _lead.Text = lead;

            for (int i = 0; i < _slots.Count && i < slots.Count; i++)
                Apply(_slots[i], slots[i]);
        }

        public void Collect(List<Hit> hits, List<SentenceSlot> slots)
        {
            for (int i = 0; i < _slots.Count && i < slots.Count; i++)
                hits.Add(new Hit(_slots[i].Bounds, slots[i].Command));
        }

        /// <summary>
        /// Puts a slot's words on it, and says whether it is still waiting to be filled.
        /// </summary>
        /// <remarks>
        /// The other host leans an unfilled slot into italics. There is no italic here - the game ships
        /// one weight - so the colour carries it alone, which is why the empty ones are the warning colour
        /// rather than merely a paler ink.
        /// </remarks>
        public static void Apply(Element element, SentenceSlot model)
        {
            var slot = element as Slot;
            if (slot != null)
            {
                slot.Text = model.Text;
                slot.Color = model.IsEmpty ? Palette.Notice : Palette.Ink;
                slot.Underline = model.IsEmpty ? Palette.Notice : Palette.SlotEdge;
                return;
            }

            var word = element as Label;
            if (word != null)
                word.Text = model.Text;
        }
    }
}
