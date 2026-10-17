using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Localization;
using Run = Sprocket.Host.SpaceEngineers.Sequences.Run;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>The screen of one sequence: what it is doing, its steps, and the ways to add a step.</summary>
    /// <remarks>
    /// The live state here is the reason this is more than an editor: watching a sequence fire, sit on step three and
    /// skip a re-trigger saves somebody from running the program in their head. Locked, it is the same page with
    /// nothing to press: every grip, part, bin and arrow stays where it stands and goes quiet, and the line the adders
    /// stand on says why instead. The elements it is drawn with are declared in <c>SequencePage.View.cs</c>.
    /// </remarks>
    public sealed partial class SequencePage : EditorPage
    {
        const int JustRanTicks = 10;

        readonly Sequence _sequence;
        readonly List<object> _keys = new List<object>();
        readonly List<int> _shown = new List<int>();
        Dictionary<Step, object> _ends = new Dictionary<Step, object>();

        readonly LastRunText _account;
        readonly StepEditing _stepwork;
        readonly WaitCoverage _ties;
        int _wiring = -1;
        string _madeName;
        int _madeEdits;
        KeyedChildren<object, StepRow> _rows;
        LengthQuestion _length;

        /// <summary>The page of <paramref name="sequence" />, in the editor <paramref name="editor" />.</summary>
        public SequencePage(SprocketEditor editor, Sequence sequence) : base(editor)
        {
            _sequence = sequence;
            _account = new LastRunText(sequence);
            _stepwork = new StepEditing(editor, sequence);
            _ties = new WaitCoverage(_stepwork.Lasting);
        }

        /// <summary>The id of the sequence the page shows.</summary>
        public int SequenceId => _sequence.Id;

        /// <summary>The panel a wait's length is being given in, or null while none is open.</summary>
        public DurationPanel Lengths => _length != null && _length.IsOpen ? _length.Panel : null;

        /// <summary>What the sequence is called: its name, or its number where it has none.</summary>
        /// <remarks>
        /// No subtitle. What the sequence is doing belongs beside the switch that changes it, and what starts it is a row
        /// on its system's screen: a sequence three triggers start has no one line to say so.
        /// </remarks>
        public override string Title => SequenceText.Called(Editor.Texts, _sequence);

        /// <inheritdoc />
        public override bool Edits => true;

        /// <inheritdoc />
        public override string Help => Terms.Sequence;

        /// <summary>Whether the screen has a switch: the logic system's, which also keeps the sequence from being changed while it may run.</summary>
        public override bool CanSwitch => _sequence.System != null;

        /// <inheritdoc />
        public override bool SwitchIsOn => _sequence.System != null && _sequence.System.Enabled;

        /// <inheritdoc />
        public override string SwitchAbout => _sequence.System == null ? null : Editor.SwitchAbout(_sequence.System);

        /// <inheritdoc />
        public override bool IsRunning
        {
            get
            {
                var state = Editor.Runner.Get(_sequence);
                return state != null && state.IsRunning;
            }
        }

        /// <summary>The command that runs the sequence by hand, whatever the switch says.</summary>
        /// <remarks>
        /// Somebody working on a system switches it off so its triggers leave it alone, which is exactly when they want to
        /// see a sequence work.
        /// </remarks>
        public override ICommand Runs => Editor.Act(CommandKind.RunNow, () => Editor.RunNow(_sequence));

        /// <inheritdoc />
        public override ICommand Stops => Editor.Act(CommandKind.StopNow, () => Editor.StopNow(_sequence));

        /// <summary>Deletes the sequence, as a step its logic system's undo takes back; like every edit, while it cannot run.</summary>
        public override ICommand Deletes => Editable ? Editor.Act(CommandKind.DeleteSequence, () => Editor.DeleteSequence(_sequence)) : null;

        /// <inheritdoc />
        public override string DeleteLocked => Editable ? null : LockedText;

        /// <inheritdoc />
        public override string DeleteAbout => Editor.Texts.SequenceDelete;

        /// <summary>Always true: the screen walks back the changes to the sequence's logic system, the last thing done to it on any of its screens.</summary>
        public override bool HasHistory => true;

        /// <summary>Whether a change to the sequence's logic system can be taken back from this screen, while nobody else holds the editor.</summary>
        public override bool CanUndo => Editor.Holder == null && Editor.CanUndo(_sequence.System, _sequence);

        /// <inheritdoc />
        public override string UndoText => Editor.UndoLabel(_sequence.System, _sequence);

        /// <inheritdoc />
        public override bool CanRedo => Editor.Holder == null && Editor.CanRedo(_sequence.System, _sequence);

        /// <inheritdoc />
        public override string RedoText => Editor.RedoLabel(_sequence.System, _sequence);

        /// <summary>The sequence just made is as it was made: its name, and nothing done in the program since.</summary>
        public override bool Untouched => _sequence.Name == _madeName && Editor.Program.Edits == _madeEdits;

        /// <summary>The name renaming overtypes: a sequence arrives already named, "Sequence 3", the way a block does.</summary>
        public override string CustomName => _sequence.Name ?? "";

        /// <inheritdoc />
        public override bool CanRename => true;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.SequenceNameAbout;

        /// <summary>Always true: a sequence has to be called something, as its name is what identifies it.</summary>
        public override bool NameRequired => true;

        /// <summary>Whether the sequence may be changed now: not while its system is switched on, and not while somebody else holds the editor.</summary>
        public bool Editable => !SwitchIsOn && Editor.Holder == null;

        /// <summary>Why nothing can be changed, said where the ways to add a step would be; empty while it can.</summary>
        public string LockedText => Editable
            ? ""
            : Editor.Holder != null
                ? Template.Fill(Editor.Texts.SequenceHeld, "holder", Editor.Holder)
                : Template.Fill(Editor.Texts.SystemLocked, "name", _sequence.System.Name);

        /// <summary>The line saying what the sequence is doing.</summary>
        public SequenceBar Bar => _bar;

        /// <summary>The steps, one row each, in order.</summary>
        public Element.Collection Rows => _steps.Children;

        /// <inheritdoc />
        public override void Rename(string name)
        {
            // Refused here as well as wherever it was typed, and cut to length in the one place that owns what a name
            // may be. Folded into one edit, because a name arrives a letter at a time.
            var wanted = NameRules.Clean(name);
            if (wanted.Length == 0)
                return;

            Editor.Do(_sequence, new RenameSequence(_sequence.Name, wanted));
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
            _madeEdits = Editor.Program.Edits;
        }

        /// <inheritdoc />
        public override void Switch(bool on)
        {
            if (_sequence.System != null)
                Editor.SwitchSystem(_sequence.System, on);
        }

        /// <inheritdoc />
        public override void Undo()
        {
            Editor.Undo(_sequence.System, _sequence);
        }

        /// <inheritdoc />
        public override void Redo()
        {
            Editor.Redo(_sequence.System, _sequence);
        }

        /// <summary>Puts the screen's heading in <paramref name="parts" />: one part, what the sequence is called.</summary>
        /// <remarks>What starts it is not here: a sequence may be started from several places, and its system already lists them.</remarks>
        public override void Heading(List<HeadingPart> parts)
        {
            var does = new HeadingPart("");
            does.Mark = Symbols.Sequence;
            does.MarkAbout = Editor.Texts.HeadingSequence;
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
            parts[0].Text = SequenceText.Called(Editor.Texts, _sequence);
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

        /// <summary>Keeps the rows by step, and fills the view.</summary>
        protected override void ViewCreated()
        {
            _rows = new KeyedChildren<object, StepRow>(_steps.Children, delegate { return new StepRow(); });
            _steps.Landings = Landings;
            _steps.Moves = (from, to) =>
            {
                var move = Editor.Act(CommandKind.MoveStep, () => Move(from, to));
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
            var recount = Revised() || reshaped;

            if (reshaped)
                Arrange(texts);

            var state = Editor.Runner.Get(_sequence);
            var tick = Editor.CurrentTick;

            var status = SequenceSummary.Status(_sequence, state);
            _bar.Status = status == SequenceStatus.Stopped ? _account.CalledOff(texts, state) : SequenceText.Status(texts, status);
            _bar.JustRan = state != null && state.LastFiredTick >= 0 && tick - state.LastFiredTick < JustRanTicks;
            _bar.LastRun = Editor.Ago.Said(SequenceSummary.TicksSinceRun(state, tick));
            _bar.Skipped = _account.Skips(texts, state?.SkippedCount ?? 0, state?.RefusedCount ?? 0);

            if (recount)
                Mark();

            // The ring around what is tied to what is under the pointer pulses while there is one.
            _ties.Light(_keys, _rows, _steps.IsCarrying);
            var glow = _ties.Glow(tick);

            // A row is lit while the sequence is anywhere inside it, so a step stays lit while the wait after it is what
            // is being waited on.
            for (var i = 0; i < _keys.Count; i++)
            {
                var row = _rows[_keys[i]];
                var from = _shown[i];
                var to = from + 1;
                row.IsCurrent = _keys[i] is Step && state != null && state.IsRunning && state.CurrentStep >= from
                                && state.CurrentStep < to;
                row.Glow = glow;
                row.Show(Editor.World, texts);
            }

            _bar.Show(texts);
            _steps.Slide();

            if (_length != null)
                _length.Hang();
        }

        /// <summary>The chip of <paramref name="wait" />'s length, or null where its row does not show one.</summary>
        Element LengthChip(Wait wait)
        {
            var row = _rows == null || !_keys.Contains(wait) ? null : _rows[wait];
            if (row == null)
                return null;

            foreach (var slot in row.Slots)
            {
                if (slot is DurationSlot)
                    return row.ChipOf(slot);
            }

            return null;
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
            _account.Forget();

            // Everything that changes what the sequence does waits until its system is switched off: the commands are not
            // made, so there is nothing to press by accident, and the page keeps its shape either way.
            var editable = Editable;
            _add.Show(new AddOption(texts.SequenceStep, texts.SequenceStepAbout, Kinds(texts, _sequence.Steps.Count)));
            _locked.IsVisible = !editable;
            _locked.Text = LockedText;

            var locked = editable
                ? ""
                : Editor.Holder != null
                    ? Template.Fill(texts.SequenceHeld, "holder", Editor.Holder)
                    : texts.SystemTurnOffToEdit;

            // While it can be changed, every branch ends in a row of its own holding the ways to add to it, standing where the
            // line after the branch does. Kept by that line, so the row is the same row from one change to the next.
            var ends = new Dictionary<Step, object>();
            _keys.Clear();
            _shown.Clear();
            for (var i = 0; i < _sequence.Steps.Count; i++)
            {
                var step = _sequence.Steps[i];
                if (editable && (step is Else || step is EndIf))
                {
                    object end;
                    if (!_ends.TryGetValue(step, out end))
                        end = new object();

                    ends[step] = end;
                    _keys.Add(end);
                    _shown.Add(i);
                }

                _keys.Add(step);
                _shown.Add(i);
            }

            _ends = ends;
            _rows.Show(_keys);

            // Every line is counted but the ones that hold only ways to add: the end of a branch and the row closing an if. A
            // branch's line stands where its if does, and the steps in it one further in.
            var depth = 0;
            var number = 0;
            var offTo = -1;
            for (var i = 0; i < _keys.Count; i++)
            {
                var row = _rows[_keys[i]];
                var index = _shown[i];
                var step = _keys[i] as Step;
                if (step == null)
                {
                    BranchEnd(row, texts, index, depth, index <= offTo, locked);
                    continue;
                }

                var branch = step is Else;
                var closes = step is EndIf;

                if ((branch || closes) && depth > 0)
                    depth--;

                // Passed over whole while its if is switched off, so drawn as passed over, whatever each says.
                if (step is If && step.IsOff && index > offTo)
                    offTo = _sequence.EndOf(index);

                row.Depth = depth;
                row.Number = closes ? 0 : ++number;
                row.Kind = Kind(step);
                row.Carries = step is If ? Within(i) : Kind(step) == StepKind.OtherwiseIf ? Branch(i) : 0;
                row.Slots.Clear();
                Word(row, step);
                row.Deletes = closes ? null : Changes(CommandKind.DeleteStep, () => _stepwork.Remove(index));
                row.Copies = closes || branch && Kind(step) != StepKind.OtherwiseIf
                    ? null
                    : Changes(CommandKind.DuplicateStep, () => _stepwork.Duplicate(index));
                row.IsOff = step.IsOff || index <= offTo;
                row.Pauses = branch || closes ? null : Changes(CommandKind.SwitchStep, () => _stepwork.SwitchStep(index));
                row.Grip.CanCarry = editable && !closes && (!branch || Kind(step) == StepKind.OtherwiseIf);
                Branches(row, texts, step, index);
                row.Locked = locked;

                if (step is If || branch)
                    depth++;
            }

            _ties.Cover(_keys, _rows);
        }

        /// <summary>Makes <paramref name="row" /> the end of a branch: the ways to add a step to it, at the line <paramref name="at" /> the branch ends before.</summary>
        void BranchEnd(StepRow row, Texts texts, int at, int depth, bool off, string locked)
        {
            row.Depth = depth;
            row.Number = 0;
            row.Kind = StepKind.Adds;
            row.Carries = 0;
            row.Slots.Clear();
            row.Deletes = null;
            row.Copies = null;
            row.IsOff = off;
            row.Pauses = null;
            row.Grip.CanCarry = false;
            row.Locked = locked;
            row.Adds.Clear();
            row.Adds.Add(new AddOption(texts.SequenceStep, texts.SequenceStepAbout, Kinds(texts, at)));
        }

        /// <summary>Every kind of step there is, each added at the line <paramref name="at" />, in the order a menu offers them.</summary>
        /// <remarks>Made when the page is arranged, which is when the program changes, so the line each adds at is current.</remarks>
        List<AddOption> Kinds(Texts texts, int at)
        {
            var kinds = new List<AddOption>
            {
                new AddOption(texts.SequenceDoSomething, texts.SequenceDoSomethingAbout, Changes(CommandKind.AddAction, () => _stepwork.AddAction(at))),
                new AddOption(SentenceText.WaitWord, texts.SequenceWaitAbout, Changes(CommandKind.AddWait, () => AddWait(at)))
            };

            // Beside the delay.
            kinds.Add(new AddOption(SentenceText.AwaitWords, texts.SequenceAwaitAbout,
                Changes(CommandKind.AddAwait, () => Editor.Do(_sequence, new AddStep(ChangeKind.AddStep, at, new Await())))));

            kinds.Add(new AddOption(SentenceText.RunWord, texts.SequenceRunAbout, Changes(CommandKind.AddRun, () => _stepwork.AddRun(false, at))));
            kinds.Add(new AddOption(SentenceText.StopWord, texts.SequenceStopAbout, Changes(CommandKind.AddStop, () => _stepwork.AddRun(true, at))));
            kinds.Add(new AddOption(SentenceText.IfWord, texts.SequenceIfAbout, Changes(CommandKind.AddIf, () => _stepwork.AddIf(at))));
            return kinds;
        }

        /// <summary>
        /// Gives the row closing an if its plus for a branch, offering an otherwise only while it has none, and an otherwise the
        /// way to make it an otherwise if where it stands.
        /// </summary>
        void Branches(StepRow row, Texts texts, Step step, int index)
        {
            row.Adds.Clear();
            if (step is EndIf)
            {
                var ends = _sequence.Otherwise(_sequence.OpenOf(index)) < 0;
                row.Adds.Add(new AddOption(texts.SequenceBranch, texts.SequenceBranchAbout, new[]
                {
                    new AddOption(SentenceText.OtherwiseIfWords, texts.SequenceOtherwiseIfAbout, Changes(CommandKind.AddBranch, () => _stepwork.AddBranch(index))),
                    new AddOption(SentenceText.OtherwiseWord, texts.SequenceOtherwiseAbout,
                        ends ? Changes(CommandKind.AddBranch, () => _stepwork.AddLast(index)) : null)
                }));
            }
            else if (Kind(step) == StepKind.Otherwise)
            {
                row.Adds.Add(new AddOption(SentenceText.IfWord, texts.SequenceOtherwiseTestAbout, new[]
                {
                    new AddOption(SentenceText.IfWord, texts.SequenceOtherwiseTestAbout, Changes(CommandKind.AddBranch, () => _stepwork.GiveTest(step)))
                }));
            }
        }

        /// <summary>How many rows after the one at <paramref name="row" />, which opens a branch, are the rest of the branch, its end among them.</summary>
        int Branch(int row)
        {
            var next = _sequence.BranchAfter(_shown[row]);
            var count = 0;
            for (var i = row + 1; i < _shown.Count && (_shown[i] < next || !(_keys[i] is Step)); i++)
                count++;

            return count;
        }

        /// <summary>How many rows after the one at <paramref name="row" />, which opens an if, are the rest of that if.</summary>
        int Within(int row)
        {
            var end = _sequence.EndOf(_shown[row]);
            var count = 0;
            for (var i = row + 1; i < _shown.Count && _shown[i] <= end; i++)
                count++;

            return count;
        }

        /// <summary>Says what is wrong once, on the bar, and marks the row it is wrong on.</summary>
        /// <remarks>
        /// Two statements that do not repeat each other: the words say what, the mark says where. A fault in what starts
        /// it has no row here to mark, because what starts it is not on this page; the words still say it.
        /// </remarks>
        void Mark()
        {
            var through = Editor.StandIn(_sequence.System);
            var fault = SequenceSummary.Fault(Editor.Runner.Get(_sequence, through));

            var said = fault.Exists ? SequenceText.Problem(Editor.Texts, fault, _sequence) : "";
            var level = fault.IsWarning ? NoticeLevel.Warning : NoticeLevel.Problem;

            // Said under the row of the line at fault; on the bar only where no row is.
            var at = -1;
            for (var i = 0; i < _keys.Count; i++)
            {
                var from = _shown[i];
                if (fault.Exists && at < 0 && _keys[i] is Step && fault.Step >= from && fault.Step < from + 1)
                    at = i;
            }

            _bar.Problem = at < 0 ? said : "";
            _bar.Level = level;
            for (var i = 0; i < _keys.Count; i++)
            {
                var row = _rows[_keys[i]];
                row.HasProblem = i == at;
                row.Problem = i == at ? said : "";
                row.Level = level;
            }
        }

        /// <summary>A command made only while the sequence may be changed, and null while it may not.</summary>
        ICommand Changes(CommandKind kind, Action action)
        {
            return SwitchIsOn ? null : Editor.Act(kind, action);
        }

        /// <summary>Gives a step's row the parts of its sentence, each pressed to open the question it was answered with.</summary>
        void Word(StepRow row, Step step)
        {
            var branch = step as Branch;
            if (branch != null)
            {
                if (branch.Test != null)
                    Tested(row, step, branch.Test);

                return;
            }

            var wait = step as Wait;
            if (wait != null)
            {
                row.Slots.Add(new DurationSlot(wait.Length, Changes(CommandKind.StepSlot, () => Open(wait, false))));
                return;
            }

            var run = step as Run;
            if (run != null)
            {
                // Opened on the list that made it, to run or stop another.
                var called = _sequence.System?.FindSequence(run.Number);
                row.Slots.Add(new SequenceSlot(run.Number, called == null ? "" : called.Name,
                    Changes(CommandKind.StepSlot, () => _stepwork.ChangeRun(run))));
                return;
            }

            var act = step as Act;
            if (act == null)
                return;

            // Shown as written. A step whose blocks do not offer what it does is a fault, said on the sequence, and the
            // gap in its sentence is where it is picked again.
            var chosen = act.Names(Vocabulary.ActionsFor(Editor.Runner.Bindings, Editor.Seen(_sequence.System, act.Targets)));

            // A state set on or off reads as enabled or disabled, its verb first and pressed to change it.
            var verb = act.Sets ? SentenceText.Switching(chosen, act.Arguments) : "";
            if (verb.Length != 0)
            {
                row.Kind = StepKind.Enable;
                row.Slots.Add(ArgumentsSlot.Switching(verb, Changes(CommandKind.StepSlot, () => _stepwork.EditStep(act, OpeningColumn.Arguments))));
            }

            row.Slots.Add(new ActionSlot(chosen, Changes(CommandKind.StepSlot, () => _stepwork.EditStep(act, OpeningColumn.What))));
            row.Slots.Add(new EntitiesSlot(Editor.Blocks(_sequence.System, act.Targets), true,
                Changes(CommandKind.StepSlot, () => _stepwork.EditStep(act, OpeningColumn.Blocks))));
            if (verb.Length != 0)
                return;

            // Last, because the blocks are what somebody scanning a list of steps is looking for and the values are the
            // detail. Pressing it opens the step where its values are given, with the caret already in them.
            if (chosen == null || chosen.Wants == 0)
                return;

            var fields = new List<Field>();
            chosen.Fields(fields);
            var given = new ArgumentsSlot(act.Arguments, fields, Changes(CommandKind.StepSlot, () => _stepwork.EditStep(act, OpeningColumn.Arguments)),
                act.Sets ? ArgumentRole.NewValue : ArgumentRole.Options);
            if (!given.Quiet)
                row.Slots.Add(given);
        }

        /// <summary>
        /// Gives the row of an if, or of a branch that tests, the parts of its test: the blocks, the value unless it is their
        /// status, and how it is compared with what.
        /// </summary>
        void Tested(StepRow row, Step step, Comparison test)
        {
            var blocks = Editor.Blocks(_sequence.System, test.Targets);
            var seen = Editor.Seen(_sequence.System, test.Targets);
            var bindings = Editor.Runner.Bindings;
            var value = test.Names(Vocabulary.ActionsFor(bindings, seen));

            // How several blocks are read is said first, and chosen from a menu under it as the comparison is; one block is read
            // the same way whichever it is.
            if (blocks.Count > 1)
            {
                var said = SentenceText.Quantified(test.Quantifier);
                var numeric = value != null && (value.Holds == ValueKind.Integer || value.Holds == ValueKind.Real);
                row.Slots.Add(new ArgumentsSlot(new[] { Literal.Name(said) }, new[] { Field.Among("reading", new[] { said }, new[] { said }) },
                    Changes(CommandKind.StepSlot, () => _stepwork.Quantify(step, test, numeric)), ArgumentRole.Read));
            }

            // A value on some blocks, as a set step and the script say it; a status is the blocks themselves.
            if (value == null || !value.IsStatus)
                row.Slots.Add(new ActionSlot(value, Changes(CommandKind.StepSlot, () => _stepwork.EditTest(step, test, OpeningColumn.What))));

            row.Slots.Add(new EntitiesSlot(blocks, test.Quantifier != Quantifier.Any,
                Changes(CommandKind.StepSlot, () => _stepwork.EditTest(step, test, OpeningColumn.Blocks))));

            if (value == null)
                return;

            // The comparison is a part of its own, chosen from a menu hanging under it, and the value another, chosen with the
            // picker as every other part is.
            var fields = ComparisonQuestion.Fields(value, Vocabulary.EventsFor(bindings, seen));
            var plural = test.Quantifier == Quantifier.All && blocks.Count > 1;
            row.Slots.Add(new ArgumentsSlot(new[] { ComparisonQuestion.Of(test.Relation) }, fields.GetRange(0, 1),
                Changes(CommandKind.StepSlot, () => _stepwork.Relate(step, test, fields[0], plural)), ArgumentRole.Compared, plural));
            row.Slots.Add(new ArgumentsSlot(new[] { test.Value }, fields.GetRange(1, fields.Count - 1),
                Changes(CommandKind.StepSlot, () => _stepwork.EditTest(step, test, OpeningColumn.Arguments)), ArgumentRole.Checked));
        }

        static StepKind Kind(Step step)
        {
            if (step is Wait)
                return StepKind.Wait;

            if (step is Await)
                return StepKind.Await;

            var branch = step as Branch;
            if (branch != null)
                return step is If ? StepKind.If : branch.Test == null ? StepKind.Otherwise : StepKind.OtherwiseIf;

            if (step is EndIf)
                return StepKind.End;

            var act = step as Act;
            if (act != null && act.Sets)
                return StepKind.Set;

            var run = step as Run;
            if (run != null)
                return run.Stops ? StepKind.Stop : StepKind.Run;

            return StepKind.Do;
        }

        /// <summary>Adds a wait of a second at <paramref name="at" />, in the player's unit, and opens the panel its length is given in.</summary>
        void AddWait(int at)
        {
            var wait = new Wait(Editor.NewLength);
            Editor.Do(_sequence, new AddStep(ChangeKind.AddWait, at, wait));
            Open(wait, true);
        }

        /// <summary>Opens the panel the length of <paramref name="wait" /> is given in, hung from its chip once the chip is on a screen.</summary>
        /// <remarks>
        /// The length is changed as the panel closes, as one change: a press anywhere else or Enter keeps what it says, and
        /// Escape leaves the wait as it was. A wait <paramref name="added" /> a moment ago is added again with its length,
        /// so adding it and giving it a length are one change, and Escape takes it away.
        /// </remarks>
        void Open(Wait wait, bool added)
        {
            if (_length != null)
                _length.Finish(false);

            _length = new LengthQuestion(Editor, wait.Length, false, () => LengthChip(wait),
                (length, cancelled) => _stepwork.Closed(wait, length, added, cancelled));
            _length.Hang();
        }

        /// <summary>
        /// Where the row at <paramref name="from" /> may land, counted among the rows it does not carry: for an otherwise if, right
        /// above each line opening a branch of its if, the if's own included, up to the otherwise or the end; for the rest, every
        /// gap but the one just under the end of a branch.
        /// </summary>
        IList<int> Landings(int from)
        {
            if (from < 0 || from >= _keys.Count)
                return null;

            if (!(_keys[from] is Else))
                return Gaps(from);

            var open = _sequence.OpenOf(_shown[from]);
            var branches = _sequence.Branches(open);
            var otherwise = _sequence.Otherwise(open);
            var end = _sequence.EndOf(open);
            var span = _rows[_keys[from]].Carries + 1;
            var allowed = new List<int>();
            for (var r = 0; r < _keys.Count; r++)
            {
                if (r >= from && r < from + span || !(_keys[r] is Step))
                    continue;

                // The line opening a branch, up to the otherwise, or the row closing the if where it has none.
                var line = _shown[r];
                if (!branches.Contains(line) && (line != end || otherwise >= 0))
                    continue;

                allowed.Add(r < from ? r : r - span);
                if (line == otherwise)
                    break;
            }

            return allowed;
        }

        /// <summary>Every gap between the rows the row at <paramref name="from" /> does not carry, but the one just under the end of a branch.</summary>
        /// <remarks>
        /// The end of a branch stands where the line after it does, so the gap under it is the same place as the gap above it:
        /// a row dropped there would land at the end of the branch, and opening both would show two places that are one.
        /// </remarks>
        List<int> Gaps(int from)
        {
            var span = _rows[_keys[from]].Carries + 1;
            var gaps = new List<int>();
            var above = 0;
            object last = null;
            for (var r = 0; r <= _keys.Count; r++)
            {
                if (r < _keys.Count && r >= from && r < from + span)
                    continue;

                // An end row is the one key that is no step.
                if (last == null || last is Step)
                    gaps.Add(above);

                if (r == _keys.Count)
                    break;

                last = _keys[r];
                above++;
            }

            return gaps;
        }

        /// <summary>
        /// Moves the branch whose row somebody dragged to the place in its if they dropped it: above the if, it becomes the if,
        /// and the if an otherwise if. Dropped anywhere else, or below the otherwise, it goes back where it was.
        /// </summary>
        void Reorder(int from, int to)
        {
            var line = _shown[from];
            var open = _sequence.OpenOf(line);
            var last = from + _rows[_keys[from]].Carries;
            var others = _shown.Count - (last - from + 1);
            if (open < 0 || to < 0 || to > others)
                return;

            // The line the branch would land in front of; the row it lands above may be the end of the branch before, which
            // stands where the line opening the next one does.
            var before = to == others ? _sequence.Steps.Count : _shown[to < from ? to : to + last - from + 1];

            var branches = _sequence.Branches(open);
            var target = before == _sequence.EndOf(open) ? branches.Count : branches.IndexOf(before);
            if (target < 0)
                return;

            // Counted with the branch itself taken out, and never past an otherwise, which comes last.
            var source = branches.IndexOf(line);
            if (target > source)
                target--;

            var limit = _sequence.Otherwise(open) >= 0 ? branches.Count - 2 : branches.Count - 1;
            if (target > limit || target == source)
                return;

            Editor.Do(_sequence, new MoveBranch(open, source, target));
        }

        /// <summary>
        /// Moves the row somebody dragged, with the rows it carries, to where they dropped it: <paramref name="to" /> is how many of
        /// the rows it did not carry are above it now.
        /// </summary>
        /// <remarks>Where it lands in the sequence says which branch of which if it is in, as it says which step it follows.</remarks>
        void Move(int from, int to)
        {
            if (from < 0 || from >= _shown.Count || to == from)
                return;

            if (_keys[from] is Else)
            {
                Reorder(from, to);
                return;
            }

            var last = from + _rows[_keys[from]].Carries;
            var others = _shown.Count - (last - from + 1);
            if (last >= _shown.Count || to < 0 || to > others)
                return;

            var start = _shown[from];
            var count = _shown[last] + 1 - start;

            // In front of the line the row it lands above starts on, less the run itself where it is moving down.
            var before = to == others ? _sequence.Steps.Count : _shown[to < from ? to : to + last - from + 1];
            var landing = before > start ? before - count : before;

            Editor.Do(_sequence, new MoveStep(start, landing, count));
        }
    }
}
