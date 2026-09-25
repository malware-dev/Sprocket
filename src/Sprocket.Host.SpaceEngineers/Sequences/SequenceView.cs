using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;

namespace Sprocket.Host.SpaceEngineers.Sequences
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

        // The one mark anybody already knows, and the note on it says what it promises: started as
        // its trigger would start it, rather than tried out.
        readonly Glyph _run = Widgets.Symbol(Symbols.Play, Palette.Muted);
        // Governing, because switching it off stops the whole of what the screen is showing.
        readonly Switch _enable = new Switch { Style = Unconverted.Governing, IsSet = true };
        // The same bin the step rows use. Deleting a sequence and deleting a step out of one are the
        // same act at two sizes, and drawing them as a word and a picture said they were different kinds
        // of thing. What separates them is where they sit: this one is up in the tools row, apart from
        // the steps, beside the switch that governs the whole sequence.
        readonly Glyph _delete = Widgets.Symbol(Symbols.Delete, Palette.Muted);

        readonly Label _locked = Widgets.Wash("", Palette.Muted, Palette.SlotFill);
        /// <summary>
        /// What is wrong, where the state would otherwise be.
        /// </summary>
        /// <remarks>
        /// The same line rather than a line of its own: a banner appearing pushes the whole sentence
        /// down, and a card that changes height as a machine runs is a card nobody can read.
        /// <para>
        /// Its own label rather than the state's words in problem colours, because the two are not
        /// the same thing and only one of them is ever true. What a sequence is doing is not news;
        /// what is stopping it is, and it is said in the colour of news.
        /// </para>
        /// </remarks>
        readonly Label _problem = Widgets.Label("", Palette.Problem, Lettering.Small);

        readonly Dot _ranLight = new Dot();

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
            _problem.Wraps = false;
            _lastRun.Wraps = false;
            _skipped.Wraps = false;
            // Deleting the whole sequence, so the same red warning the per-step crosses use.
            _delete.Highlight = Palette.ProblemWash;

            // Both hold their place for the whole time the sentence is editable and go quiet when there is
            // nothing to walk back. Hiding them would move Run and Delete sideways every time somebody
            // made or undid a change, under the pointer they were about to use.
            _undoSlot = new Sized(48f);
            _undoSlot.Content = _undo;

            _redoSlot = new Sized(48f);
            _redoSlot.Content = _redo;

            // The light leads the state, because it is the same fact said faster: "Waiting" with a
            // green flash is what a sequence firing looks like from across the room.
            Stack state = Widgets.RowOf(Space.Item);
            state.Children.Add(_ranLight);
            state.Children.Add(_status);
            state.Children.Add(_problem);
            state.Children.Add(_lastRun);

            // The switch is last, and that is the whole point of the order. This row is anchored to the
            // right edge and lays out leftwards from it, so a control's position depends only on what
            // stands to its right - which means the last one cannot be moved by anything. Undo, redo and
            // Delete all come and go with whether the sequence is running; the switch is the one control
            // that is always here, and it is also the one being pressed when the others leave. Putting it
            // anywhere but last meant pressing it slid it out from under the pointer that pressed it.
            Stack tools = Widgets.RowOf(Space.Item);
            tools.Children.Add(_run);
            tools.Children.Add(_undoSlot);
            tools.Children.Add(_redoSlot);
            tools.Children.Add(_delete);
            tools.Children.Add(_enable);

            _header.Spacing = Space.Region;
            _header.Left = state;
            _header.Fill = _skipped;
            _header.Right = tools;

            _adders.Padding = new Thickness(32f, Space.Item, 0f, 0f);
        }

        /// <summary>
        /// Told rather than asked. The sentence announces its own values, repeats what its steps say,
        /// and its lists say when they change shape.
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

            // One of the two, never both and never neither, so the row is the same height whatever
            // is happening.
            bool wrong = !string.IsNullOrEmpty(_component.Problem);
            _status.Collapsed = wrong;
            _problem.Collapsed = !wrong;

            _status.Text = _component.Status;
            _lastRun.Text = _component.LastRun;

            _ranLight.Background = _component.JustRan ? Palette.Live : Palette.Dim;

            bool skipping = _component.SkippedCount > 0 || _component.RefusedCount > 0;
            _skipped.Collapsed = !skipping;
            if (skipping)
                _skipped.Text = Skips(_component.SkippedCount, _component.RefusedCount);

            // Absent rather than blank. A banner with nothing to say still holds a line, and a strip of
            // empty chrome above the sentence is worse than the jump of one appearing when it has news.
            Banner(_locked, _component.Editable ? "" : _component.LockedText, 1);
            // The same line either way; what changes is how loud it is. A warning in problem colours
            // would have somebody hunting for a broken block that is not there.
            if (wrong)
            {
                _problem.Text = _component.Problem;
                _problem.Color = _component.ProblemIsWarning ? Palette.Notice : Palette.Problem;
            }

            _enable.IsOn = _component.Enabled;

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

            // One control, two acts, and never both at once: a sequence that is going is one to
            // stop and one that is not is one to start. The same button in the same place, because
            // a second glyph appearing beside it would move everything else in the row.
            //
            // Absent rather than dead while it may not run, like everything else on this screen
            // that cannot answer: the switch beside it is what brings it back.
            bool going = _component.Running;
            int command = going ? _component.StopCommand : _component.RunCommand;

            _run.Symbol = going ? Symbols.Stop : Symbols.Play;
            _run.Collapsed = command == 0;
            Widgets.Waiting(_run, command, going ? SequenceText.Stops() : SequenceText.Starts());

            Widgets.Waiting(_undo, _component.UndoCommand, _undoSays);
            Widgets.Waiting(_redo, _component.RedoCommand, _redoSays);
            Widgets.Waiting(_delete, _component.DeleteCommand, "Delete this sequence");

            // Says the act, where the switch itself says the state. The two together are the whole reason
            // a state switch is safe for an audience who have never programmed: the control cannot be read
            // the wrong way round, and the note says what pressing it would do.
            //
            // The one control here that a running sequence keeps, because stopping it is the act.
            _enable.Press = Deed.Switches(_component.ToggleCommand, !_component.Enabled);
            _enable.Tip = _component.ToggleCommand == 0 ? null : _component.Enabled
                ? "Switch off"
                : "Switch on";

            for (int i = 0; i < _adderRows.Count && i < _component.AddOptions.Count; i++)
                _adderRows[i].Press = Deed.Sends(_component.AddOptions[i].Command);

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

            BuildSteps();
            _root.Children.Add(_steps);

            if (_component.Editable)
            {
                BuildAdders();
                _root.Children.Add(_adders);
            }

            // Gone rather than greyed, so what is on screen while it runs is a view of a system
            // working. Walking a change back is not something a running sequence can offer at all,
            // so the two arrows go rather than sitting there dimmed as if they might yet answer.
            // Safe to take away only because the switch stands to the right of them - see the order
            // they were added in.
            //
            // The bin stays. Throwing a sequence away is not editing it, and wanting rid of one
            // while it runs is exactly when somebody wants rid of it.
            _undoSlot.Collapsed = !_component.Editable;
            _redoSlot.Collapsed = !_component.Editable;
        }

        void BuildSteps()
        {
            _steps.Children.Clear();
            _stepRows.Clear();

            for (int i = 0; i < _component.Steps.Count; i++)
            {
                SentenceStep step = _component.Steps[i];

                var row = new StepRow();

                // A grip rather than the whole line, because the line is full of chips and "press
                // anywhere that is not one" is a rule nobody can see. A handle says where to grab, and
                // says that grabbing is a thing that can be done at all.
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
                // Which step it is takes no saying - the pointer is on its line. What the note is for
                // is the difference between this and the bin in the tools above, which takes the lot.
                row.Delete.Tip = "Delete this step";

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

    }
}
