using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Screen;
using VRageMath;
using Reactor.Host.SpaceEngineers.Layout;
using Reactor.Host.SpaceEngineers.Controls;
using Reactor.Host.SpaceEngineers.Look;
using Reactor.Host.SpaceEngineers.Words;

namespace Reactor.Host.SpaceEngineers.Sequences
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

            /// <summary>What the number label was last written from.</summary>
            /// <remarks>
            /// Formatting an int allocates, and this runs every frame - so the number is compared
            /// rather than the string it would make. Zero is never a step number; they start at one.
            /// </remarks>
            public int Numbered;

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
        readonly Label _status = Widgets.Label("", Palette.Muted, Lettering.Small);
        readonly Label _lastRun = Widgets.Label("", Palette.Faint, Lettering.Small);
        readonly Label _skipped = Widgets.Wash("", Palette.Notice, Palette.NoticeWash);
        // Not chevrons. An arrow that turns back on itself is what undo is; a chevron is where a press
        // goes, and Back already owns that shape on the same screen.
        readonly Glyph _undo = Widgets.Symbol(Symbols.Undo, Palette.Muted);
        readonly Glyph _redo = Widgets.Symbol(Symbols.Redo, Palette.Muted);

        readonly Sized _undoSlot;
        readonly Sized _redoSlot;
        readonly Switch _enable = new Switch(Lettering.Small);
        // The same bin the step rows use. Deleting a sequence and deleting a step out of one are the
        // same act at two sizes, and drawing them as a word and a picture said they were different kinds
        // of thing. What separates them is where they sit: this one is up in the tools row, apart from
        // the steps, beside the switch that governs the whole sequence.
        readonly Glyph _delete = Widgets.Symbol(Symbols.Delete, Palette.Muted);

        readonly Label _locked = Widgets.Wash("", Palette.Muted, Palette.SlotFill);
        readonly Label _problem = Widgets.Wash("", Palette.Problem, Palette.ProblemWash);

        readonly Wrap _trigger = new Wrap();
        readonly Dot _ranLight = new Dot();
        readonly Label _triggerLead = Widgets.Label("", Palette.Muted, Lettering.Body);
        readonly Label _liveBadge = Widgets.Wash("true right now", Palette.Live, Palette.LiveWash);
        readonly List<Element> _triggerSlots = new List<Element>();

        /// <summary>What each trigger slot was last given, so an unchanged one is not worded again.</summary>
        readonly List<SentenceSlot> _shownTrigger = new List<SentenceSlot>();

        /// <summary>What the two arrows were last told to say, and the words they were made from.</summary>
        string _undone;
        string _undoSays;
        string _redone;
        string _redoSays;

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
            // Deleting the whole sequence, so the same red warning the per-step crosses use.
            _delete.Highlight = Palette.ProblemWash;

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

            // The switch is last, and that is the whole point of the order. This row is anchored to the
            // right edge and lays out leftwards from it, so a control's position depends only on what
            // stands to its right - which means the last one cannot be moved by anything. Undo, redo and
            // Delete all come and go with whether the sequence is running; the switch is the one control
            // that is always here, and it is also the one being pressed when the others leave. Putting it
            // anywhere but last meant pressing it slid it out from under the pointer that pressed it.
            Stack tools = Widgets.RowOf(Space.Item);
            tools.Children.Add(_undoSlot);
            tools.Children.Add(_redoSlot);
            tools.Children.Add(_delete);
            tools.Children.Add(_enable);

            _header.Spacing = Space.Region;
            _header.Left = state;
            _header.Fill = _skipped;
            _header.Right = tools;

            _trigger.ItemSpacing = Space.Item;
            _trigger.LineSpacing = Space.Row;
            _trigger.Padding = new Thickness(Space.Item, Space.ChipY * 1.3f);

            _adders.Padding = new Thickness(32f, Space.Item, 0f, 0f);
        }

        /// <summary>
        /// Told rather than asked. The sentence announces its own values, repeats what its steps and
        /// its trigger say, and its lists say when they change shape.
        /// </summary>
        protected override bool Stale
        {
            get { return false; }
        }

        public override Element Root
        {
            get { return _root; }
        }

        protected override void Refresh()
        {
            if (!_built || _builtEditable != _component.Editable || !SameShape())
                Build();

            _status.Text = _component.Status;
            _lastRun.Text = _component.LastRun;

            // The same wash and the same red light a broken row in a list gets, so arriving here
            // from one arrives at something already recognised. Core says which line; this says so
            // loudly, because being led two screens deep and then having to hunt is worse than not
            // being led at all.
            bool triggerWrong = _component.Trigger.HasProblem;
            _trigger.Background = triggerWrong ? Palette.ProblemWash : Palette.None;

            // Broken outranks recently-run. A green light on a line that cannot fire is the screen
            // contradicting itself, and the light is the first thing the eye lands on.
            _ranLight.Background = triggerWrong
                ? Palette.Problem
                : (_component.JustRan ? Palette.Live : Palette.Dim);

            bool skipping = _component.SkippedCount > 0 || _component.RefusedCount > 0;
            _skipped.Collapsed = !skipping;
            if (skipping)
                _skipped.Text = Skips(_component.SkippedCount, _component.RefusedCount);

            // Absent rather than blank. A banner with nothing to say still holds a line, and a strip of
            // empty chrome above the sentence is worse than the jump of one appearing when it has news.
            Banner(_locked, _component.Editable ? "" : _component.LockedText, 1);
            // The banner is the same line either way; what changes is how loud it is. A warning in
            // problem colours would have somebody hunting for a broken block that is not there.
            _problem.Color = _component.ProblemIsWarning ? Palette.Notice : Palette.Problem;
            _problem.Background = _component.ProblemIsWarning ? Palette.NoticeWash : Palette.ProblemWash;
            Banner(_problem, _component.Problem, 2);

            // Idle while the signal reads true is not a contradiction. It is the difference between a
            // moment and a situation, shown rather than explained.
            _liveBadge.Collapsed = !_component.Trigger.IsLiveNow;

            // Governs, because a sequence's switch is the only one on this screen and switching it off
            // stops the whole of what the screen is showing.
            _enable.Show(_component.Enabled, true);

            // Nothing on this screen needs asking whether the sequence can be edited. Core hands a
            // running one a command of zero for every act it cannot do - see SequencePage.Change - and a
            // control with no command is not a control, so the whole screen locks itself.
            //
            // The verb is this screen's word and the thing it acts on is core's, which already publishes
            // it: UndoText exists because "Undo" alone asks somebody to remember what they just did, and
            // somebody who has made a mistake is worst at exactly that.
            //
            // Built only when those words change. Joining two strings is cheap and this runs sixty times
            // a second, for a note that is read when the pointer happens to rest on the arrow.
            if (!ReferenceEquals(_undone, _component.UndoText))
            {
                _undone = _component.UndoText;
                _undoSays = "Undo " + _component.UndoText;
            }

            if (!ReferenceEquals(_redone, _component.RedoText))
            {
                _redone = _component.RedoText;
                _redoSays = "Redo " + _component.RedoText;
            }

            Waiting(_undo, _component.UndoCommand, _undoSays);
            Waiting(_redo, _component.RedoCommand, _redoSays);
            Waiting(_delete, _component.DeleteCommand, "Delete this sequence");

            // Says the act, where the switch itself says the state. The two together are the whole reason
            // a state switch is safe for an audience who have never programmed: the control cannot be read
            // the wrong way round, and the note says what pressing it would do.
            //
            // The one control here that a running sequence keeps, because stopping it is the act.
            _enable.Press = Deed.Switches(_component.ToggleCommand, !_component.Enabled);
            _enable.Tip = _component.ToggleCommand == 0 ? null : _component.Enabled
                ? "Switch this sequence off"
                : "Switch this sequence on";

            for (int i = 0; i < _adderRows.Count && i < _component.AddOptions.Count; i++)
                _adderRows[i].Press = Deed.Sends(_component.AddOptions[i].Command);

            _triggerLead.Text = SentenceText.Lead(_component.Trigger);

            // Only when it changes. Wording a slot builds strings, and this runs on every tick - see
            // SentenceView, which remembers the same way for the same reason.
            for (int i = 0; i < _triggerSlots.Count && i < _component.Trigger.Slots.Count; i++)
            {
                SentenceSlot slot = _component.Trigger.Slots[i];
                _triggerSlots[i].Press = Deed.Sends(slot.Command);

                if (ReferenceEquals(_shownTrigger[i], slot))
                    continue;

                _shownTrigger[i] = slot;
                SentenceView.Apply(_triggerSlots[i], slot, SentenceText.InTrigger(Host.World, slot));
            }

            for (int i = 0; i < _stepRows.Count && i < _component.Steps.Count; i++)
            {
                SentenceStep step = _component.Steps[i];
                StepRow row = _stepRows[i];

                if (row.Numbered != step.Number)
                {
                    row.Numbered = step.Number;
                    row.Number.Text = step.Number.ToString();
                }
                row.Sentence.Update(Host.World, step);
                row.Delete.Press = Deed.Sends(step.DeleteCommand);

                // Left alone while it is being carried. This runs every tick, and it would otherwise
                // repaint the row being dragged in the colour it is not, sixty times a second.
                //
                // Broken outranks being the current step, which the two can only be at once for the
                // instant between a block going away and the run abandoning the step that named it.
                if (_carrying < 0)
                    row.Line.Background = step.HasProblem
                        ? Palette.ProblemWash
                        : (step.IsCurrent ? Palette.LiveWash : Palette.None);
            }
        }

        /// <summary>A row being carried, and the ones getting out of its way.</summary>
        protected override void Moved(int tick)
        {
            Slide();
        }

        public override bool TryGrab(Vector2 point)
        {
            if (!_component.Editable)
                return false;

            for (int i = 0; i < _stepRows.Count; i++)
            {
                StepRow row = _stepRows[i];

                if (!row.Grip.Reaches(point))
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
            // Walking a change back is not something a running sequence can offer at all, and neither is
            // deleting it, so all three go rather than sitting there dimmed as if they might yet answer.
            // Safe to take away only because the switch stands to the right of them - see the order they
            // were added in.
            _delete.Collapsed = !_component.Editable;
            _undoSlot.Collapsed = !_component.Editable;
            _redoSlot.Collapsed = !_component.Editable;
        }

        void BuildTrigger()
        {
            _trigger.Children.Clear();
            _triggerSlots.Clear();
            _shownTrigger.Clear();

            _trigger.Children.Add(_ranLight);
            _trigger.Children.Add(_triggerLead);

            for (int i = 0; i < _component.Trigger.Slots.Count; i++)
            {
                Element slot = NewSlot();
                _triggerSlots.Add(slot);
                _shownTrigger.Add(null);
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

                row.Number = Widgets.Label("", Palette.Faint, Lettering.Small);
                row.Number.Wraps = false;

                var grip = new Sized(26f);
                grip.Content = row.Grip;
                grip.Collapsed = !_component.Editable;

                var number = new Sized(34f);
                number.Content = row.Number;

                Stack handle = Widgets.RowOf(Space.Line);
                handle.Children.Add(grip);
                handle.Children.Add(number);

                row.Sentence = new SentenceView(_component.Editable, step.Slots.Count);
                // Washed red rather than the ordinary panel, because this is the one control on the row
                // that destroys something. It is the only place on these screens where hovering says
                // "careful" rather than "go on".
                row.Delete = Widgets.Symbol(Symbols.Delete, Palette.Faint);
                row.Delete.Highlight = Palette.ProblemWash;
                row.Delete.Collapsed = !_component.Editable;

                // Said here rather than per tick: a row's position is settled when the row is made,
                // and the alternative is building this string sixty times a second for every step.
                row.Delete.Tip = "Delete step " + row.Position;

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
                Label word = Widgets.Label("", Palette.Ink, Lettering.Body);
                word.Wraps = false;
                return word;
            }

            return new Slot();
        }

        /// <summary>Shows a banner when it has something to say, and takes it out of the layout when not.</summary>
        /// <summary>What was asked for and did not happen, and why not.</summary>
        /// <remarks>
        /// Both reasons in one line when both happened, because they are one piece of news - this was
        /// asked for and did not run - and a second banner for the rarer half would cost a line of the
        /// screen on every sequence that has ever been busy.
        /// </remarks>
        static string Skips(int busy, int off)
        {
            if (busy == 0)
                return "skipped " + off + "x while off";

            if (off == 0)
                return "skipped " + busy + "x while busy";

            return "skipped " + busy + "x while busy, " + off + "x while off";
        }

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

        /// <summary>
        /// Dresses one of the header's glyph buttons, and says what it would do.
        /// </summary>
        /// <remarks>
        /// It keeps its place whether or not there is anything to do - hiding it would move the next one
        /// sideways under the pointer somebody was about to use - so the only thing left to say "there is
        /// nothing here" is that it is dim, does not light up, and says nothing.
        /// <para>
        /// One call, because the dimming, the light, the press and the note are four statements of a
        /// single fact, and four statements drift. A dim control that still answered the pointer would be
        /// one somebody reaches for twice.
        /// </para>
        /// </remarks>
        static void Waiting(Glyph button, int command, string says)
        {
            bool ready = command != 0;
            button.Tint = ready ? Palette.Muted : Palette.Dim;
            button.Highlight = ready ? Palette.Panel : Palette.None;
            button.Press = Deed.Sends(command);
            button.Tip = ready ? says : null;
        }
    }
}
