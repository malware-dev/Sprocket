using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Values;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Localization;
using Run = Sprocket.Host.SpaceEngineers.Sequences.Run;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>One sequence: what it is doing, its steps, and the ways to add another.</summary>
    /// <remarks>
    /// The live state here is the reason this is more than an editor: watching a sequence fire, sit on step three and
    /// skip a re-trigger saves somebody from running the program in their head. Locked, it is the same page with
    /// nothing to press: every grip, part, bin and arrow stays where it stands and goes quiet, and the line the adders
    /// stand on says why instead. The elements it is drawn with are declared in <c>SequencePage.View.cs</c>, and
    /// nothing else is; this half is what it knows, what it says and what it does.
    /// </remarks>
    sealed partial class SequencePage : EditorPage
    {
        const int JustRanTicks = 10;

        static readonly int[] WaitPresets = { 60, 120, 300, 600, 1800, 3600 };

        readonly Sequence _sequence;
        readonly List<Step> _keys = new List<Step>();
        readonly List<int> _shown = new List<int>();

        int _quiet;
        int _wiring = -1;
        int _busy;
        int _off;
        string _skips = "";
        string _madeName;
        bool _madeOn;
        Keyed<Step, StepRow> _rows;

        /// <summary>The page of <paramref name="sequence" />, in the editor <paramref name="editor" />.</summary>
        public SequencePage(SprocketEditor editor, Sequence sequence) : base(editor)
        {
            _sequence = sequence;

            // Here rather than when the screen is built, which happens again every time the sentence changes shape,
            // which is exactly when something has just been edited.
            _quiet = editor.Program.Wiring;
        }

        /// <summary>The id of the sequence the page shows.</summary>
        public int SequenceId => _sequence.Id;

        /// <summary>A named sequence is called by its name, and an unnamed one reads as its own sentence.</summary>
        /// <remarks>
        /// No subtitle. What the sequence is doing belongs beside the switch that changes it, and what starts it is a row
        /// on its system's screen: a sequence three triggers start has no one line to say so.
        /// </remarks>
        public override string Title => SequenceText.Doing(Editor.Runner.Bindings, _sequence, Editor.StandIn(_sequence.System));

        /// <summary>Whether anything starts the sequence is read from the toolbars.</summary>
        public override bool CanRefresh => true;

        /// <summary>The sequence has a switch of its own, whatever it is doing, because stopping it is the act.</summary>
        public override bool CanSwitch => true;

        /// <inheritdoc />
        public override bool SwitchIsOn => _sequence.Enabled;

        /// <summary>The switch parks this sequence and nothing else in its system.</summary>
        public override bool SwitchGovernsAll => false;

        /// <inheritdoc />
        public override bool IsRunning
        {
            get
            {
                var state = Editor.Runner.Get(_sequence);
                return state != null && state.IsRunning;
            }
        }

        /// <summary>Runs it by hand, whatever the switch says.</summary>
        /// <remarks>
        /// Somebody working on a sequence parks it so its triggers leave it alone, which is exactly when they want to see it
        /// work.
        /// </remarks>
        public override ICommand Runs => Editor.Act(Cmd.RunNow, delegate { Editor.RunNow(_sequence); });

        /// <inheritdoc />
        public override ICommand Stops => Editor.Act(Cmd.StopNow, delegate { Editor.StopNow(_sequence); });

        /// <summary>Throws the whole sequence away, which is not editing it and so is offered while it may run.</summary>
        public override ICommand Deletes => Editor.Act(Cmd.DeleteSequence, delegate { Editor.DeleteSequence(_sequence); });

        /// <inheritdoc />
        public override string DeleteAbout => Editor.Texts.SequenceDelete;

        /// <summary>The sequence's own way back, for what is done to it on this page.</summary>
        public override bool HasHistory => true;

        /// <summary>Nothing is walked back while the sequence may run, as nothing else about it changes then.</summary>
        public override bool CanUndo => !Running && Editor.CanUndo(_sequence);

        /// <inheritdoc />
        public override string UndoText => Editor.UndoLabel(_sequence);

        /// <inheritdoc />
        public override bool CanRedo => !Running && Editor.CanRedo(_sequence);

        /// <inheritdoc />
        public override string RedoText => Editor.RedoLabel(_sequence);

        /// <summary>The sequence just made is as it was made: its name and switch, and nothing done to it that could be walked back.</summary>
        /// <remarks>Everything that changes what a sequence does is an edit in its history, so an empty history is nothing done.</remarks>
        public override bool Untouched => _sequence.Name == _madeName && _sequence.Enabled == _madeOn
            && !Editor.CanUndo(_sequence) && !Editor.CanRedo(_sequence);

        /// <summary>A sequence arrives already named, "Sequence 3", the way a block does, so renaming is always overtyping.</summary>
        public override string CustomName => _sequence.Name ?? "";

        /// <inheritdoc />
        public override bool CanRename => true;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.SequenceNameAbout;

        /// <summary>A sequence has to be called something, because the name is what identifies it.</summary>
        public override bool NameRequired => true;

        /// <summary>Whether the sequence may be changed now: not while it may run, and not while somebody else holds the editor.</summary>
        public bool Editable => !Running && Editor.Holder == null;

        /// <summary>Why nothing can be changed, said where the ways to add a step would be; empty while it can.</summary>
        public string LockedText => Editable
            ? ""
            : Editor.Holder != null
                ? Template.Fill(Editor.Texts.SequenceHeld, "holder", Editor.Holder)
                : Editor.Texts.SequenceLocked;

        /// <summary>The line saying what the sequence is doing.</summary>
        public SequenceBar Bar => _bar;

        /// <summary>The steps, one row each, in order.</summary>
        public Element.Collection Rows => _steps.Children;

        /// <summary>Whether it may run on the one of its system being edited, which is what locks it.</summary>
        bool Running => _sequence.CanRun(Editor.StandIn(_sequence.System));

        /// <inheritdoc />
        public override void Rename(string name)
        {
            // Refused here as well as wherever it was typed, and cut to length in the one place that owns what a name
            // may be. Folded into one edit, because a name arrives a letter at a time.
            var wanted = Naming.Clean(name);
            if (wanted.Length == 0)
                return;

            Editor.Do(_sequence, new Rename(_sequence.Name, wanted), true);
        }

        /// <inheritdoc />
        public override bool IsAbout(AutomationSystem system)
        {
            return system == _sequence.System;
        }

        /// <inheritdoc />
        public override void Unmake()
        {
            Editor.TakeOut(_sequence);
        }

        /// <summary>Notes the sequence just made, as it was made.</summary>
        protected override void Remember()
        {
            _madeName = _sequence.Name;
            _madeOn = _sequence.Enabled;
        }

        /// <inheritdoc />
        public override void Switch(bool on)
        {
            SetEnabled(on);
        }

        /// <inheritdoc />
        public override void Undo()
        {
            Editor.Undo(_sequence);
        }

        /// <inheritdoc />
        public override void Redo()
        {
            Editor.Redo(_sequence);
        }

        /// <summary>One part: what this sequence is called, the name where there is one and what it does where there is not.</summary>
        /// <remarks>What starts it is not here: a sequence may be started from several places, and its system already lists them.</remarks>
        public override void Heading(List<HeadingPart> parts)
        {
            var does = new HeadingPart("");
            does.About = NameAbout;
            does.NameRequired = NameRequired;
            does.Renames = Editor.Renaming(Rename, null);
            does.NamedOnArrival = NameOnArrival;
            NameOnArrival = false;

            parts.Add(does);
            RefreshHeading(parts);
        }

        /// <inheritdoc />
        public override void RefreshHeading(List<HeadingPart> parts)
        {
            if (parts.Count < 1)
                return;

            parts[0].Name = CustomName;
            parts[0].Text = SequenceText.Doing(Editor.Runner.Bindings, _sequence, Editor.StandIn(_sequence.System));
        }

        /// <summary>Brings the view up to date, parts and all.</summary>
        public override void Build()
        {
            Show(true);
        }

        /// <summary>Brings the view up to what the sequence is doing, and to its steps where they have changed.</summary>
        /// <remarks>
        /// Every tick the page is showing. What it is doing and which step it is on are read off its state, which costs
        /// nothing; what is wrong with it is worked out once a second.
        /// </remarks>
        public override void Refresh()
        {
            var reshaped = _wiring != Editor.Program.Wiring;
            Show(reshaped);
        }

        /// <summary>Offers to switch a finished sequence on, on the way out.</summary>
        /// <remarks>
        /// A sequence is parked while it is built, so a half-written rule never fires, and the cost is somebody finishing,
        /// walking away and concluding it does not work. Leaving is the moment that costs one press to put right. Only when
        /// something was changed, only once per change, and never for a sequence with no steps or one Sprocket parked
        /// because it cannot run: a warning is not that, since nothing starting it yet is exactly the state somebody walks
        /// away from thinking they have finished. Dismissing the question is not answering it, so pressing back asks again.
        /// </remarks>
        public override bool Leaving()
        {
            if (Editor.Program.Wiring == _quiet || _sequence.Enabled || _sequence.Steps.Count == 0)
                return true;

            var through = Editor.StandIn(_sequence.System);
            var fault = SequenceSummary.Fault(Editor.World, Editor.Runner.Bindings, _sequence,
                Editor.Runner.Get(_sequence, through), through);

            if (fault.Exists && !fault.IsWarning)
                return true;

            Editor.Ask(Editor.Texts.SequenceSwitchOnQuestion);

            Editor.Answer(Editor.Texts.SequenceSwitchItOn, delegate
            {
                _quiet = Editor.Program.Wiring;
                SetEnabled(true);
                Editor.Pop();
            });

            // The one Enter takes: leaving it parked is what pressing back already meant, and the key that gets pressed
            // without reading must not switch something on.
            Editor.Answer(Editor.Texts.SequenceLeaveItOff, delegate
            {
                _quiet = Editor.Program.Wiring;
                Editor.Pop();
            }, true);

            return false;
        }

        /// <summary>Keeps the rows by step, and fills the view.</summary>
        protected override void ViewCreated()
        {
            _rows = new Keyed<Step, StepRow>(_steps.Children, delegate { return new StepRow(); });
            _steps.Moves = delegate(int from, int to)
            {
                var move = Editor.Act(Cmd.MoveStep, delegate { Move(from, to); });
                if (move != null)
                    move.Execute();
            };

            Show(true);
        }

        void Show(bool reshaped)
        {
            if (_rows == null)
                return;

            var texts = Editor.Texts;

            // Counted when the rows are new as well, so a broken step is marked from the first frame.
            var recount = SecondPassed() || reshaped;

            if (reshaped)
                Arrange(texts);

            var state = Editor.Runner.Get(_sequence);
            var tick = Editor.CurrentTick;

            _bar.Status = SequenceText.Status(SequenceSummary.Status(_sequence, state));
            _bar.JustRan = state != null && state.LastFiredTick >= 0 && tick - state.LastFiredTick < JustRanTicks;
            _bar.LastRun = TimeText.Ago(SequenceSummary.TicksSinceRun(state, tick));
            _bar.Skipped = Skips(texts, state == null ? 0 : state.SkippedCount, state == null ? 0 : state.RefusedCount);

            if (recount)
                Mark();

            // A row is lit while the sequence is anywhere inside it, so a step stays lit while the wait after it is what
            // is being waited on.
            for (var i = 0; i < _keys.Count; i++)
            {
                var row = _rows[_keys[i]];
                var from = _shown[i];
                var to = from + Spanned(from);
                row.IsCurrent = state != null && state.IsRunning && state.CurrentStep >= from && state.CurrentStep < to;
                row.Show(Editor.World, texts);
            }

            _bar.Show(texts);
            _steps.Slide();
        }

        /// <summary>Puts a row in place for each step with the parts of its sentence, and offers what may be done now.</summary>
        /// <remarks>
        /// Walks the steps' blocks for their names, which is why it runs only when the program changes. A wait for a step
        /// to finish is not a row: each row stands for a step and any wait after it, moved and deleted as one.
        /// </remarks>
        void Arrange(Texts texts)
        {
            _wiring = Editor.Program.Wiring;

            // Joined again after anything that builds the page, a change of language among them.
            _busy = -1;

            // Everything that changes what the sequence does waits until it is switched off: the commands are not made,
            // so there is nothing to press by accident, and the page keeps its shape either way.
            var editable = Editable;
            _doSomething.Text = StatusRow.Plus + texts.SequenceDoSomething;
            _doSomething.Command = Changes(Cmd.AddAction, AddAction);
            _doSomething.IsVisible = editable;
            _wait.Text = StatusRow.Plus + texts.SequenceWait;
            _wait.Command = Changes(Cmd.AddWait, AddWait);
            _wait.IsVisible = editable;
            _locked.IsVisible = !editable;
            _locked.Text = LockedText;

            _keys.Clear();
            _shown.Clear();
            for (var i = 0; i < _sequence.Steps.Count; i++)
            {
                if (_sequence.Steps[i] is Await)
                    continue;

                _keys.Add(_sequence.Steps[i]);
                _shown.Add(i);
            }

            _rows.Show(_keys);

            for (var i = 0; i < _keys.Count; i++)
            {
                var row = _rows[_keys[i]];
                var index = _shown[i];

                row.Number = i + 1;
                row.Kind = Kind(_keys[i]);
                row.Slots.Clear();
                Word(row, _keys[i]);
                row.Deletes = Changes(Cmd.DeleteStep, delegate { RemoveStep(index); });
                row.Grip.CanCarry = editable;
            }
        }

        /// <summary>Says what is wrong once, on the bar, and marks the row it is wrong on.</summary>
        /// <remarks>
        /// Two statements that do not repeat each other: the words say what, the mark says where. A fault in what starts
        /// it has no row here to mark, because what starts it is not on this page; the words still say it.
        /// </remarks>
        void Mark()
        {
            var through = Editor.StandIn(_sequence.System);
            var fault = SequenceSummary.Fault(Editor.World, Editor.Runner.Bindings, _sequence,
                Editor.Runner.Get(_sequence, through), through);

            _bar.Problem = fault.Exists ? SequenceText.Problem(fault) : "";
            _bar.ProblemIsWarning = fault.IsWarning;

            for (var i = 0; i < _keys.Count; i++)
                _rows[_keys[i]].HasProblem = fault.Exists && fault.Step == _shown[i];
        }

        /// <summary>What was asked for and did not happen, and why not, in one line when both happened.</summary>
        /// <remarks>Asked every tick, so the line is joined again only when a count has changed.</remarks>
        string Skips(Texts texts, int busy, int off)
        {
            if (busy == _busy && off == _off)
                return _skips;

            _busy = busy;
            _off = off;

            if (busy == 0 && off == 0)
                _skips = "";
            else if (busy == 0)
                _skips = Template.Fill(texts.SequenceSkippedOff, "off", off.ToString(CultureInfo.InvariantCulture));
            else if (off == 0)
                _skips = Template.Fill(texts.SequenceSkippedBusy, "busy", busy.ToString(CultureInfo.InvariantCulture));
            else
                _skips = Template.Fill(Template.Fill(texts.SequenceSkippedBoth, "busy", busy.ToString(CultureInfo.InvariantCulture)),
                    "off", off.ToString(CultureInfo.InvariantCulture));

            return _skips;
        }

        /// <summary>A command made only while the sequence may be changed, and null while it may not.</summary>
        ICommand Changes(Cmd kind, Action action)
        {
            return Running ? null : Editor.Act(kind, action);
        }

        /// <summary>Gives a step's row the parts of its sentence, each pressed to open the question it was answered with.</summary>
        void Word(StepRow row, Step step)
        {
            var wait = step as Wait;
            if (wait != null)
            {
                row.Slots.Add(new DurationSlot(wait.Ticks, Changes(Cmd.StepSlot, delegate { EditWait(wait); })));
                return;
            }

            var run = step as Run;
            if (run != null)
            {
                // Read only: nothing in the editor makes one of these yet, and a part that opens nothing must still show
                // what the step does.
                var called = _sequence.System == null ? null : _sequence.System.FindSequence(run.Number);
                row.Slots.Add(new SequenceSlot(run.Number, called == null ? "" : called.Name, null));
                return;
            }

            var act = step as Act;
            if (act == null)
                return;

            // Shown as written. A step whose blocks do not offer what it does is a fault, said on the sequence, and the
            // gap in its sentence is where it is picked again.
            var chosen = act.Names(Vocabulary.ActionsFor(Editor.Runner.Bindings, Editor.Seen(_sequence.System, act.Targets)));

            row.Slots.Add(new ActionSlot(chosen, Changes(Cmd.StepSlot, delegate { EditStep(act, Opening.What); })));
            row.Slots.Add(new EntitiesSlot(Editor.Blocks(_sequence.System, act.Targets), true,
                Changes(Cmd.StepSlot, delegate { EditStep(act, Opening.Blocks); })));

            // Last, because the blocks are what somebody scanning a list of steps is looking for and the values are the
            // detail. Pressing it opens the step where its values are given, with the caret already in them.
            if (chosen == null || chosen.Wants == 0)
                return;

            var fields = new List<Field>();
            chosen.Fields(fields);
            row.Slots.Add(new ValuesSlot(act.Values, fields, Changes(Cmd.StepSlot, delegate { EditStep(act, Opening.Values); })));
        }

        static StepKind Kind(Step step)
        {
            if (step is Wait)
                return StepKind.Wait;

            var act = step as Act;
            if (act != null && act.Sets)
                return StepKind.Set;

            return step is Run ? StepKind.Run : StepKind.Do;
        }

        /// <summary>How many of the sequence's lines a row stands for: the step, and any wait for it after it.</summary>
        int Spanned(int index)
        {
            return index + 1 < _sequence.Steps.Count && _sequence.Steps[index + 1] is Await ? 2 : 1;
        }

        /// <summary>Opens a step's picker on the column <paramref name="opens" /> says, the part that was pressed.</summary>
        void EditStep(Act step, Opening opens)
        {
            var index = _sequence.Steps.IndexOf(step);

            // Handed what the step already holds, so the question that finishes this opens on those values rather than
            // empty: changing which blocks a step acts on is not a reason to type its values again.
            Editor.Push(new PickPage(Editor, "Do something", false,
                Editor.Blocks(_sequence.System, step.Targets), step.ActionId, step.Form,
                delegate(PickAnswer answer)
                {
                    Editor.Do(_sequence,
                        new ChangeStep(index, step, Editor.Named(_sequence.System, answer.Targets), answer.Chosen,
                            answer.Form, answer.Values,
                            Vocabulary.Lasts(Editor.World, answer.Targets, answer.Chosen, answer.Form)));
                    Editor.Pop();
                }, values: step.Values, opens: opens));
        }

        /// <summary>Asks what to affect, then what to do to it, and adds the step only once both are answered.</summary>
        /// <remarks>
        /// Blocks first, because somebody knows which block they mean but may not know what verbs exist: pointing at a
        /// door and being told what a door can do is discovery, and every verb on the ship in one list is a wall.
        /// </remarks>
        void AddAction()
        {
            var origin = Editor.Depth;

            Editor.Push(new PickPage(Editor, "Do something", false,
                new List<long>(), "", Form.Native,
                delegate(PickAnswer answer)
                {
                    var step = new Act(answer.Chosen);
                    step.Form = answer.Form;
                    var named = Editor.Named(_sequence.System, answer.Targets);
                    for (var i = 0; i < named.Count; i++)
                        step.Targets.Add(named[i]);

                    for (var i = 0; i < answer.Values.Count; i++)
                        step.Values.Add(answer.Values[i]);

                    // With a wait for it after it where what it does takes time to finish, because a numbered list is
                    // read as one thing after another; one edit, so one press takes the pair back.
                    Editor.Do(_sequence, Vocabulary.Lasts(Editor.World, answer.Targets, answer.Chosen, answer.Form)
                        ? new AddStep("adding a step", _sequence.Steps.Count, step, new Await())
                        : new AddStep("adding a step", _sequence.Steps.Count, step));
                    Editor.UnwindTo(origin);
                }));
        }

        /// <summary>Adds a timed wait, for the things that have no finishing to wait on.</summary>
        void AddWait()
        {
            var origin = Editor.Depth;
            Editor.Push(WaitPage(delegate(int ticks)
            {
                Editor.Do(_sequence, new AddStep("adding a wait", _sequence.Steps.Count, new Wait(ticks)));
                Editor.UnwindTo(origin);
            }));
        }

        ChoicePage WaitPage(Action<int> chosen)
        {
            var page = new ChoicePage(Editor, Editor.Texts.SequenceWaitQuestion);
            for (var i = 0; i < WaitPresets.Length; i++)
            {
                var ticks = WaitPresets[i];
                page.Add(TimeText.WaitChoice(ticks), false, delegate { chosen(ticks); });
            }

            page.AddTyped(TimeText.OtherWait, TimeText.UseLength, WholeTicks, delegate(string said)
            {
                int ticks;
                if (int.TryParse(said, NumberStyles.None, CultureInfo.InvariantCulture, out ticks) && ticks > 0)
                    chosen(ticks);
            });

            return page;
        }

        /// <summary>Whether what is typed can still become a number of ticks: digits only, few enough to fit, or nothing.</summary>
        static bool WholeTicks(string said)
        {
            if (string.IsNullOrEmpty(said))
                return true;

            if (said.Length > 9)
                return false;

            for (var i = 0; i < said.Length; i++)
            {
                if (said[i] < '0' || said[i] > '9')
                    return false;
            }

            return true;
        }

        void EditWait(Wait step)
        {
            var origin = Editor.Depth;
            Editor.Push(WaitPage(delegate(int ticks)
            {
                Editor.Do(_sequence, new SetWait(_sequence.Steps.IndexOf(step), step.Ticks, ticks));
                Editor.UnwindTo(origin);
            }));
        }

        void RemoveStep(int index)
        {
            var going = new List<Step>();
            for (var i = 0; i < Spanned(index); i++)
                going.Add(_sequence.Steps[index + i]);

            Editor.Do(_sequence, new RemoveStep(index, going.ToArray()));
        }

        /// <summary>Moves the row somebody dragged to where they dropped it; both ends are rows, since a row is what was taken hold of.</summary>
        void Move(int from, int to)
        {
            if (from < 0 || from >= _shown.Count || to < 0 || to >= _shown.Count || from == to)
                return;

            var start = _shown[from];
            var count = Spanned(start);

            // Where the run lands: the line the row it is going to starts on, less the run itself where it is moving down.
            var landing = _shown[to];
            if (to > from)
                landing = landing + Spanned(landing) - count;

            Editor.Do(_sequence, new MoveStep(start, landing, count));
        }

        /// <summary>Running or parked, which undo does not cover: a press of undo must never arm a system behind somebody's back.</summary>
        void SetEnabled(bool on)
        {
            _sequence.Enabled = on;
            Editor.Invalidate();
        }
    }
}
