using Sprocket.Host.SpaceEngineers.Features.Selector;
using Sprocket.Host.SpaceEngineers.Features.Sequences.Edits;
using Sprocket.Host.SpaceEngineers.Features.States;
using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Parts;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Run = Sprocket.Host.SpaceEngineers.Framework.Model.Run;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>The screen of one sequence: what it is doing, its steps, and the ways to add a step.</summary>
    public sealed partial class SequencePage : EditorPage
    {
        const int JustRanTicks = 10;

        readonly Sequence _sequence;
        readonly List<object> _keys = new List<object>();
        readonly List<int> _shown = new List<int>();
        readonly HashSet<string> _termed = new HashSet<string>();
        Dictionary<Step, object> _ends = new Dictionary<Step, object>();

        // The row the last catch or finally ends at, kept so it is not made again.
        readonly object _handlersEnd = new object();

        // Whether a run of the sequence is in its catch or finally, which locks it.
        bool _cleaning;

        readonly LastRunText _account;
        readonly StepEditing _stepwork;
        readonly WaitCoverage _ties;
        int _wiring = -1;
        string _madeName;
        int _madeEdits;
        KeyedChildren<object, StepRow> _rows;
        LengthQuestion _length;
        AbortQuestion _abort;
        ValuesQuestion _values;

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

        /// <summary>The form an abort's message and level are being given in, or null while none is open.</summary>
        public FieldSet Aborting => _abort != null && _abort.IsOpen ? _abort.Form : null;

        /// <summary>The panel a step's values are being given in, or null while none is open.</summary>
        public ValuesQuestion Giving => _values != null && _values.IsOpen ? _values : null;

        /// <summary>What the sequence is called: its name, or its number where it has none.</summary>
        public override string Title => SequenceText.Called(Editor.Texts, _sequence);

        /// <inheritdoc />
        public override bool Edits => true;

        /// <summary>Whether the screen has a switch, which is its logic system's.</summary>
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

        /// <summary>The command that runs the sequence by hand, or null while it or the installation shown is switched off.</summary>
        public override ICommand Runs => RunLocked == null ? Editor.Act(CommandKind.RunNow, () => Editor.RunNow(_sequence)) : null;

        /// <inheritdoc />
        public override string RunLocked => SequenceText.RunLocked(Editor.Texts, _sequence, Editor.StandIn(_sequence.System));

        /// <inheritdoc />
        public override ICommand Stops => Editor.Act(CommandKind.StopNow, () => Editor.StopNow(_sequence));

        /// <summary>The command that deletes the sequence, or null while it cannot be changed.</summary>
        public override ICommand Deletes => Editable ? Editor.Act(CommandKind.DeleteSequence, () => Editor.DeleteSequence(_sequence)) : null;

        /// <inheritdoc />
        public override string DeleteLocked => Editable ? null : LockedText;

        /// <inheritdoc />
        public override string DeleteAbout => Editor.Texts.Codex.SequenceDelete;

        /// <summary>Always true: the screen walks back the changes to the sequence's logic system.</summary>
        public override bool HasHistory => true;

        /// <summary>Whether a change to the sequence's logic system can be taken back from this screen, while nobody else holds the editor.</summary>
        public override bool CanUndo => Editor.Holder == null && Editor.CanUndo(_sequence.System, _sequence);

        /// <inheritdoc />
        public override string UndoText => Editor.UndoLabel(_sequence.System, _sequence);

        /// <inheritdoc />
        public override bool CanRedo => Editor.Holder == null && Editor.CanRedo(_sequence.System, _sequence);

        /// <inheritdoc />
        public override string RedoText => Editor.RedoLabel(_sequence.System, _sequence);

        /// <summary>Whether the sequence just made is unchanged since it was made.</summary>
        public override bool Untouched => _sequence.Name == _madeName && Editor.Program.Edits == _madeEdits;

        /// <summary>The sequence's name, which renaming overtypes.</summary>
        public override string CustomName => _sequence.Name ?? "";

        /// <inheritdoc />
        public override bool CanRename => true;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.Codex.SequenceNameAbout;

        /// <summary>Always true: a sequence must have a name.</summary>
        public override bool NameRequired => true;

        /// <summary>
        /// Whether the sequence may be changed now: not while its system is switched on, not while somebody else holds the editor,
        /// and not while a run of it is in its catch or finally.
        /// </summary>
        public bool Editable => !SwitchIsOn && Editor.Holder == null && !_cleaning;

        /// <summary>Why nothing can be changed, said where the ways to add a step would be; empty while it can.</summary>
        public string LockedText => Editable
            ? ""
            : Editor.Holder != null
                ? Template.Fill(Editor.Texts.SequenceHeld, "holder", Editor.Holder)
                : _cleaning
                    ? Editor.Texts.SequenceCleaning
                    : Template.Fill(Editor.Texts.SystemLocked, "name", _sequence.System.Name);

        /// <summary>The line saying what the sequence is doing.</summary>
        public SequenceBar Bar => _bar;

        /// <summary>The steps, one row each, in order.</summary>
        public Element.Collection Rows => _steps.Children;

        /// <inheritdoc />
        public override void Rename(string name)
        {
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
        public override void Heading(List<HeadingPart> parts)
        {
            var does = new HeadingPart("");
            does.Mark = Symbols.Sequence;
            does.MarkAbout = Editor.Texts.Codex.TermSequence;
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
        /// <remarks>Called every tick the page is showing; faults are worked out once a second.</remarks>
        public override void Refresh()
        {
            var reshaped = _wiring != Editor.Program.Wiring;
            Show(reshaped);
        }

        /// <summary>Keeps the rows by step, and fills the view.</summary>
        protected override void ViewCreated()
        {
            _rows = new KeyedChildren<object, StepRow>(_steps.Children, delegate { return Row(); });
            _steps.FnAllowedDropIndexes = Landings;
            _steps.Reordered += moved =>
            {
                var move = Editor.Act(CommandKind.MoveStep, () => Move(moved.From, moved.To));
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

            var recount = Revised() || reshaped;

            if (reshaped)
                Arrange(texts);

            var state = Editor.Runner.Get(_sequence);
            var tick = Editor.CurrentTick;

            // Looked at on every refresh, as the bar's status is; walks the steps only while a run is under way, and lays the
            // page out again only as a catch or finally starts or ends.
            var cleaning = state != null && state.Cleaning;
            if (cleaning != _cleaning)
            {
                _cleaning = cleaning;
                Arrange(texts);
            }

            var status = SequenceSummary.Status(_sequence, state);
            _bar.Status = status == SequenceStatus.Stopped ? _account.CalledOff(texts, state) : SequenceText.Status(texts, status);
            _bar.JustRan = state != null && state.LastFiredTick >= 0 && tick - state.LastFiredTick < JustRanTicks;
            _bar.LastRun = Editor.Ago.Said(SequenceSummary.TicksSinceRun(state, tick));
            _bar.Skipped = _account.Skips(texts, state?.SkippedCount ?? 0, state?.RefusedCount ?? 0, state?.HeldBy);

            if (recount)
                Mark();

            _ties.Light(_keys, _rows, _steps.IsDragging);
            var glow = _ties.Glow(tick);

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

            if (_length != null)
                _length.Hang();

            if (_abort != null)
                _abort.Hang();

            if (_values != null)
                _values.Hang();
        }

        /// <summary>The chip of the first part of <paramref name="step" />'s row that is a <typeparamref name="TSlot" />, or null where its row shows none.</summary>
        Element Chip<TSlot>(Step step) where TSlot : SentenceSlot
        {
            return Chip(step, slot => slot is TSlot);
        }

        /// <summary>The chip of the first part of <paramref name="step" />'s row saying values that are <paramref name="holds" /> to it, or null where its row shows none.</summary>
        Element Chip(Step step, ArgumentRole holds)
        {
            return Chip(step, slot => (slot as ArgumentsSlot)?.Holds == holds);
        }

        /// <summary>The chip of the first part of <paramref name="step" />'s row that <paramref name="wanted" /> takes, or null where its row shows none.</summary>
        Element Chip(Step step, Func<SentenceSlot, bool> wanted)
        {
            var row = _rows == null || !_keys.Contains(step) ? null : _rows[step];
            if (row == null)
                return null;

            foreach (var slot in row.Slots)
            {
                if (wanted(slot))
                    return row.ChipOf(slot);
            }

            return null;
        }

        /// <summary>Puts a row in place for each step with the parts of its sentence, and offers what may be done now.</summary>
        /// <remarks>Walks the steps' blocks for their names, so it runs only when the program changes.</remarks>
        void Arrange(Texts texts)
        {
            _wiring = Editor.Program.Wiring;

            _account.Forget();

            var editable = Editable;
            _add.Show(new AddOption(texts.SequenceStep, null, Last(texts)));
            _locked.IsVisible = !editable;
            _locked.Text = LockedText;

            var locked = editable
                ? ""
                : Editor.Holder != null
                    ? Template.Fill(texts.SequenceHeld, "holder", Editor.Holder)
                    : _cleaning
                        ? texts.SequenceCleaning
                        : texts.Codex.SystemTurnOffToEdit;

            var ends = new Dictionary<Step, object>();
            _keys.Clear();
            _shown.Clear();
            for (var i = 0; i < _sequence.Steps.Count; i++)
            {
                var step = _sequence.Steps[i];
                if (editable && (step is Else || step is EndIf || step is Handler))
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

            // The last catch or finally ends at its own way to add a step, as a branch does.
            if (editable && _sequence.HandlersAt < _sequence.Steps.Count)
            {
                _keys.Add(_handlersEnd);
                _shown.Add(_sequence.Steps.Count);
            }

            _ends = ends;
            _rows.Show(_keys);

            var depth = 0;
            var number = 0;
            var offTo = -1;
            _termed.Clear();
            for (var i = 0; i < _keys.Count; i++)
            {
                var row = _rows[_keys[i]];
                var index = _shown[i];
                var step = _keys[i] as Step;
                if (step == null)
                {
                    BranchEnd(row, texts, index, depth, index <= offTo, locked);
                    row.IsVisible = editable;
                    continue;
                }

                var branch = step is Else;
                var closes = step is EndIf;
                var handler = step is Handler;

                if ((branch || closes) && depth > 0)
                    depth--;

                // A catch or finally stands at the sequence's own level, its steps under it.
                if (handler)
                    depth = 0;

                if (step is If && step.IsOff && index > offTo)
                    offTo = _sequence.EndOf(index);

                row.Depth = depth;
                row.Number = closes ? 0 : ++number;
                row.Kind = Kind(step);
                row.LeadTerm = Term(row.Kind);
                row.DraggedWithCount = step is If ? Within(i) : Kind(step) == StepKind.OtherwiseIf ? Branch(i) : 0;
                row.Slots.Clear();
                Word(row, step);
                row.Deletes = closes ? null : Changes(CommandKind.DeleteStep, () => _stepwork.Remove(index));
                row.Copies = closes || handler || branch && Kind(step) != StepKind.OtherwiseIf
                    ? null
                    : Changes(CommandKind.DuplicateStep, () => _stepwork.Duplicate(index));
                row.IsOff = step.IsOff || index <= offTo;
                row.Pauses = branch || closes || handler ? null : Changes(CommandKind.SwitchStep, () => _stepwork.SwitchStep(index));
                row.DragHandle.CanDrag = editable && !closes && !handler && (!branch || Kind(step) == StepKind.OtherwiseIf);
                Branches(row, texts, step, index);
                row.Locked = locked;

                // The end of an if holds nothing but its ways to add, which only an editable sequence offers.
                row.IsVisible = !closes || editable;

                if (step is If || branch || handler)
                    depth++;
            }

            _ties.Cover(_keys, _rows);
        }

        /// <summary>Makes <paramref name="row" /> the end of a branch, holding the ways to add a step at the line <paramref name="at" />.</summary>
        void BranchEnd(StepRow row, Texts texts, int at, int depth, bool off, string locked)
        {
            row.Depth = depth;
            row.Number = 0;
            row.Kind = StepKind.Adds;
            row.LeadTerm = null;
            row.DraggedWithCount = 0;
            row.Slots.Clear();
            row.Deletes = null;
            row.Copies = null;
            row.IsOff = off;
            row.Pauses = null;
            row.DragHandle.CanDrag = false;
            row.Locked = locked;
            row.Adds.Clear();
            row.Adds.Add(new AddOption(texts.SequenceStep, null, Kinds(texts, at)));
        }

        /// <summary>Every kind of step there is, each added at the line <paramref name="at" />, in the order a menu offers them.</summary>
        List<AddOption> Kinds(Texts texts, int at)
        {
            var kinds = new List<AddOption>
            {
                new AddOption(texts.SequenceDoSomething, texts.Codex.SequenceDoSomethingAbout, Changes(CommandKind.AddAction, () => _stepwork.AddAction(at))),
                new AddOption(SentenceText.WaitWord, texts.Codex.TermDelay, Changes(CommandKind.AddWait, () => AddWait(at)))
            };

            kinds.Add(new AddOption(SentenceText.AwaitWords, texts.Codex.SequenceAwaitAbout,
                Changes(CommandKind.AddAwait, () => Editor.Do(_sequence, new AddStep(ChangeKind.AddStep, at, new Await())))));

            kinds.Add(new AddOption(SentenceText.AbortWord, texts.Codex.SequenceAbortAbout,
                Changes(CommandKind.AddAbort, () => Editor.Do(_sequence, new AddStep(ChangeKind.AddStep, at, new Abort())))));

            kinds.Add(new AddOption(SentenceText.RunWord, texts.Codex.TermRunAndStop, Changes(CommandKind.AddRun, () => _stepwork.AddRun(false, at))));
            kinds.Add(new AddOption(SentenceText.StopWord, texts.Codex.TermRunAndStop, Changes(CommandKind.AddStop, () => _stepwork.AddRun(true, at))));
            kinds.Add(new AddOption(SentenceText.SetStateWords, texts.Codex.SequenceSetStateAbout, Changes(CommandKind.AddSetState, () => _stepwork.AddSetState(at))));
            kinds.Add(new AddOption(SentenceText.IfWord, texts.Codex.TermIf, Changes(CommandKind.AddIf, () => _stepwork.AddIf(at))));
            return kinds;
        }

        /// <summary>
        /// What the sequence's last way to add offers: every kind of step, added at its end, while it has no catch or finally;
        /// and each of those it has not got yet.
        /// </summary>
        List<AddOption> Last(Texts texts)
        {
            var options = _sequence.HandlersAt < _sequence.Steps.Count ? new List<AddOption>() : Kinds(texts, _sequence.Steps.Count);
            if (_sequence.CatchAt < 0)
            {
                // A catch comes before a finally.
                var at = _sequence.FinallyAt;
                options.Add(new AddOption(SentenceText.CatchWords, texts.Codex.TermCatch,
                    Changes(CommandKind.AddHandler, () => Editor.Do(_sequence, new AddStep(ChangeKind.AddStep, at < 0 ? _sequence.Steps.Count : at, new Catch())))));
            }

            if (_sequence.FinallyAt < 0)
            {
                options.Add(new AddOption(SentenceText.FinallyWord, texts.Codex.TermFinally,
                    Changes(CommandKind.AddHandler, () => Editor.Do(_sequence, new AddStep(ChangeKind.AddStep, _sequence.Steps.Count, new Finally())))));
            }

            return options;
        }

        /// <summary>Gives the row closing an if, or an otherwise, its plus for adding a branch or a test.</summary>
        void Branches(StepRow row, Texts texts, Step step, int index)
        {
            row.Adds.Clear();
            if (step is EndIf)
            {
                var ends = _sequence.Otherwise(_sequence.OpenOf(index)) < 0;
                row.Adds.Add(new AddOption(texts.SequenceBranch, texts.Codex.SequenceBranchAbout, new[]
                {
                    new AddOption(SentenceText.OtherwiseIfWords, texts.Codex.SequenceOtherwiseIfAbout, Changes(CommandKind.AddBranch, () => _stepwork.AddBranch(index))),
                    new AddOption(SentenceText.OtherwiseWord, texts.Codex.SequenceOtherwiseAbout,
                        ends ? Changes(CommandKind.AddBranch, () => _stepwork.AddLast(index)) : null)
                }));
            }
            else if (Kind(step) == StepKind.Otherwise)
            {
                row.Adds.Add(new AddOption(SentenceText.IfWord, texts.Codex.SequenceOtherwiseTestAbout, new[]
                {
                    new AddOption(SentenceText.IfWord, texts.Codex.SequenceOtherwiseTestAbout, Changes(CommandKind.AddBranch, () => _stepwork.GiveTest(step)))
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
        void Mark()
        {
            var through = Editor.StandIn(_sequence.System);
            var fault = SequenceSummary.Fault(Editor.Runner.Get(_sequence, through));

            var said = fault.Exists ? SequenceText.Problem(Editor.Texts, fault, _sequence) : "";
            var level = SequenceText.Level(fault);

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
            return SwitchIsOn || _cleaning ? null : Editor.Act(kind, action);
        }

        /// <summary>Gives a step's row the parts of its sentence, each pressed to open the question it was answered with.</summary>
        void Word(StepRow row, Step step)
        {
            var branch = step as Branch;
            if (branch != null)
            {
                var compared = branch.Test as Comparison;
                if (compared != null)
                    Tested(row, step, compared);

                var kept = branch.Test as StateTest;
                if (kept != null)
                    Tested(row, step, kept);

                return;
            }

            var set = step as SetState;
            if (set != null)
            {
                Setting(row, set);
                return;
            }

            var wait = step as Wait;
            if (wait != null)
            {
                row.Slots.Add(new DurationSlot(wait.Length, wait.Setting, Changes(CommandKind.StepSlot, () => Open(wait, false))));
                return;
            }

            var abort = step as Abort;
            if (abort != null)
            {
                row.Slots.Add(ArgumentsSlot.Aborting(abort.Message, abort.Setting, abort.Level, Changes(CommandKind.StepSlot, () => Open(abort))));
                return;
            }

            var run = step as Run;
            if (run != null)
            {
                var called = _sequence.System?.FindSequence(run.Number);
                row.Slots.Add(new SequenceSlot(run.Number, called == null ? "" : called.Name,
                    Changes(CommandKind.StepSlot, () => _stepwork.Rerun(run))));
                return;
            }

            var act = step as Act;
            if (act == null)
                return;

            var chosen = act.Names(Editor.SlotBlocks.Actions(_sequence.System, act.Targets));

            var verb = act.Sets ? SentenceText.Switching(chosen, act.Arguments) : "";
            if (verb.Length != 0)
            {
                row.Kind = StepKind.Enable;
                row.Slots.Add(ArgumentsSlot.Switching(verb, Changes(CommandKind.StepSlot, () => _stepwork.Switch(act))));
            }

            row.Slots.Add(new ActionSlot(chosen, Changes(CommandKind.StepSlot, () => _stepwork.EditStep(act, OpeningColumn.What))));
            var blocks = Editor.Blocks(_sequence.System, act.Targets);
            var entities = new EntitiesSlot(blocks, act.Targets, true, MarkText.OfSlots(_sequence.System, act.Targets),
                Changes(CommandKind.StepSlot, () => _stepwork.EditStep(act, OpeningColumn.Blocks)))
            {
                Screens = Editor.SlotBlocks.Screens(_sequence.System, act.Targets, blocks)
            };
            if (verb.Length != 0 || chosen == null || chosen.Wants == 0)
            {
                row.Slots.Add(entities);
                return;
            }

            var fields = new List<Field>();
            chosen.Fields(fields);
            var holds = act.Sets ? ArgumentRole.NewValue : ArgumentRole.Options;
            ArgumentsSlot.Lay(row.Slots, entities, chosen.Join, act.Arguments, fields, Changes(CommandKind.StepSlot, () => Give(act, fields, holds)), holds,
                names => new EntitiesSlot(Editor.Blocks(_sequence.System, names), names, true, MarkText.OfSlots(_sequence.System, names),
                    Changes(CommandKind.StepSlot, () => _stepwork.EditStep(act, OpeningColumn.Target))));
        }

        /// <summary>Gives the row of an if, or of a branch that tests, the parts of its test.</summary>
        void Tested(StepRow row, Step step, Comparison test)
        {
            var blocks = Editor.Blocks(_sequence.System, test.Targets);
            var value = test.Names(Editor.SlotBlocks.Actions(_sequence.System, test.Targets));

            // A line naming slots says how it reads them whatever they hold; one naming blocks, only where it names several.
            var slotted = _sequence.System.IsBlueprint;
            var shape = TestShape.Of(test.Quantifier, value == null || !value.IsStatus, slotted || blocks.Count > 1, slotted, ComparisonQuestion.Join(value, test.Arguments));
            var numeric = value != null && (value.Holds == ValueKind.Integer || value.Holds == ValueKind.Real);
            var said = shape.Reading;
            var reading = shape.IsRead
                ? new ArgumentsSlot(new[] { Literal.Name(said) }, new[] { Field.Among("reading", new[] { said }, new[] { said }) },
                    Changes(CommandKind.StepSlot, () => _stepwork.Quantify(step, test, numeric)), ArgumentRole.Read)
                : null;
            var named = shape.NamesValue
                ? new ActionSlot(value, Changes(CommandKind.StepSlot, () => _stepwork.EditTest(step, test, OpeningColumn.What)))
                : null;
            var targets = new EntitiesSlot(blocks, test.Targets, shape.JoinsAll, MarkText.OfSlots(_sequence.System, test.Targets),
                Changes(CommandKind.StepSlot, () => _stepwork.EditTest(step, test, OpeningColumn.Blocks)))
            {
                Screens = Editor.SlotBlocks.Screens(_sequence.System, test.Targets, blocks)
            };
            var parameters = ComparisonQuestion.Parameters(value);
            var taking = !ComparisonQuestion.SaysAny(parameters, test.Arguments) ? null : new ArgumentsSlot(test.Arguments, parameters,
                Changes(CommandKind.StepSlot, () => Compare(step, test, parameters, ArgumentRole.ReadWith)), ArgumentRole.ReadWith);
            shape.Lay(row.Slots, reading, named, targets, taking);

            if (value == null)
                return;

            var fields = ComparisonQuestion.Fields(value, Editor.SlotBlocks.Events(_sequence.System, test.Targets), test.Arguments);
            var plural = shape.IsPlural;
            row.Slots.Add(new ArgumentsSlot(new[] { ComparisonQuestion.Of(test.Relation) }, fields.GetRange(0, 1),
                Changes(CommandKind.StepSlot, () => _stepwork.Relate(step, test, fields[0], plural)), ArgumentRole.Compared, plural));
            var checks = fields.GetRange(1, fields.Count - 1);
            row.Slots.Add(new ArgumentsSlot(new[] { test.Value }, checks,
                Changes(CommandKind.StepSlot, () => Compare(step, test, checks, ArgumentRole.Checked)), ArgumentRole.Checked));
        }

        /// <summary>Gives the row of a step setting one of the system's states its parts: the state, and what it is set to.</summary>
        void Setting(StepRow row, SetState set)
        {
            var opens = Changes(CommandKind.StepSlot, () => _stepwork.ChangeSetState(set));
            row.Slots.Add(new SystemStateSlot(set.State, opens));
            row.Slots.Add(StateValueQuestion.Part(_sequence.System, set.State, set.Value, set.From,
                Changes(CommandKind.StepSlot, () => _stepwork.SetStateTo(set)), ArgumentRole.NewValue));
        }

        /// <summary>Gives the row of an if, or of a branch, that tests one of the system's states the parts of its test.</summary>
        void Tested(StepRow row, Step step, StateTest test)
        {
            var opens = Changes(CommandKind.StepSlot, () => _stepwork.EditTest(step, test, OpeningColumn.Blocks));
            row.Slots.Add(new SystemStateSlot(test.State, opens));
            row.Slots.Add(new ArgumentsSlot(new[] { ComparisonQuestion.Of(test.Relation) }, ComparisonQuestion.StateFields(),
                Changes(CommandKind.StepSlot, () => _stepwork.RelateState(step, test)), ArgumentRole.Compared));
            row.Slots.Add(StateValueQuestion.Part(_sequence.System, test.State, test.Value, test.Other,
                Changes(CommandKind.StepSlot, () => _stepwork.CompareState(step, test)), ArgumentRole.Checked));
        }

        /// <summary>A row for a step, whose opening words explain their term with the codex's page on it.</summary>
        StepRow Row()
        {
            var row = new StepRow();
            row.Explain(Editor.Codex);
            return row;
        }

        /// <summary>The codex page the words a step of <paramref name="kind" /> opens with are the term of, where no row above on the screen links that page; null otherwise.</summary>
        string Term(StepKind kind)
        {
            string page;
            switch (kind)
            {
                case StepKind.If:
                    page = CodexPages.If;
                    break;
                case StepKind.OtherwiseIf:
                case StepKind.Otherwise:
                    page = CodexPages.Branch;
                    break;
                case StepKind.Wait:
                    page = CodexPages.Delay;
                    break;
                case StepKind.Set:
                    page = CodexPages.Set;
                    break;
                case StepKind.Await:
                    page = CodexPages.Await;
                    break;
                case StepKind.Abort:
                    page = CodexPages.Abort;
                    break;
                case StepKind.Catch:
                    page = CodexPages.Catch;
                    break;
                case StepKind.Finally:
                    page = CodexPages.Finally;
                    break;
                case StepKind.Run:
                case StepKind.Stop:
                    page = CodexPages.RunAndStop;
                    break;
                default:
                    return null;
            }

            return _termed.Add(page) ? page : null;
        }

        static StepKind Kind(Step step)
        {
            if (step is Catch)
                return StepKind.Catch;

            if (step is Finally)
                return StepKind.Finally;

            if (step is Wait)
                return StepKind.Wait;

            if (step is Await)
                return StepKind.Await;

            if (step is Abort)
                return StepKind.Abort;

            var branch = step as Branch;
            if (branch != null)
                return step is If ? StepKind.If : branch.Test == null ? StepKind.Otherwise : StepKind.OtherwiseIf;

            if (step is EndIf)
                return StepKind.End;

            var act = step as Act;
            if (act != null && act.Sets || step is SetState)
                return StepKind.Set;

            var run = step as Run;
            if (run != null)
                return run.Stops ? StepKind.Stop : StepKind.Run;

            return StepKind.Do;
        }

        /// <summary>Adds a wait of a second at <paramref name="at" /> and opens the panel its length is given in.</summary>
        void AddWait(int at)
        {
            var wait = new Wait(Editor.NewLength);
            Editor.Do(_sequence, new AddStep(ChangeKind.AddWait, at, wait));
            Open(wait, true);
        }

        /// <summary>Opens the panel the length of <paramref name="wait" /> is given in, hung from its chip.</summary>
        void Open(Wait wait, bool added)
        {
            if (_length != null)
                _length.Finish(false);

            _length = new LengthQuestion(Editor, wait.Length, false, () => Chip<DurationSlot>(wait),
                (length, cancelled) => _stepwork.Closed(wait, length, added, cancelled));
            _length.Hang();
        }

        /// <summary>
        /// Opens the panel the values <paramref name="act" /> gives, asked for by <paramref name="fields" />, are given in, hung
        /// from the part of its row saying them, which are <paramref name="holds" /> to it; the selector where a value is chosen
        /// on a screen of its own.
        /// </summary>
        void Give(Act act, List<Field> fields, ArgumentRole holds)
        {
            if (ValuesQuestion.Chooses(fields))
            {
                _stepwork.EditStep(act, OpeningColumn.Arguments);
                return;
            }

            Action<string, bool> answered = (said, cancelled) => _stepwork.Closed(act, FieldSet.Read(said, fields), cancelled);
            _stepwork.Repaired(act, delegate
            {
                if (ValuesQuestion.Offer(Editor, fields, act.Arguments, said => answered(said, false)))
                    return;

                if (_values != null)
                    _values.Finish(true);

                _values = new ValuesQuestion(Editor, fields, act.Arguments, () => Chip(act, holds), answered);
                _values.Hang();
            });
        }

        /// <summary>
        /// Opens the panel the values <paramref name="test" />, the test of <paramref name="step" />, takes are given in, asked
        /// for by <paramref name="fields" />: what it compares with where they are <paramref name="holds" />
        /// <see cref="ArgumentRole.Checked" />, what it reads with otherwise; hung from the part saying them, and changing the test
        /// as one edit as it closes. The selector where a value is chosen on a screen of its own.
        /// </summary>
        void Compare(Step step, Comparison test, List<Field> fields, ArgumentRole holds)
        {
            if (ValuesQuestion.Chooses(fields))
            {
                _stepwork.EditTest(step, test, OpeningColumn.Arguments);
                return;
            }

            var checks = holds == ArgumentRole.Checked;
            var given = checks ? new[] { test.Value } : test.Arguments;
            Action<string, bool> answered = (said, cancelled) =>
            {
                if (cancelled)
                    return;

                var now = test.Copy();
                ComparisonQuestion.Give(now.Arguments, FieldSet.Read(said, fields), checks, value => now.Value = value);
                _stepwork.Retest(step, test, now);
            };
            if (ValuesQuestion.Offer(Editor, fields, given, said => answered(said, false)))
                return;

            if (_values != null)
                _values.Finish(true);

            _values = new ValuesQuestion(Editor, fields, given, () => Chip(step, holds), answered);
            _values.Hang();
        }

        /// <summary>Opens the panel what <paramref name="abort" /> says is given in, hung from the part of its row saying it.</summary>
        void Open(Abort abort)
        {
            if (_abort != null)
                _abort.Finish(false);

            _abort = new AbortQuestion(Editor, abort, () => Chip<ArgumentsSlot>(abort),
                (message, level, cancelled) => _stepwork.Closed(abort, message, level, cancelled));
            _abort.Hang();
        }

        /// <summary>The places the row at <paramref name="from" /> may land, counted among the rows it does not carry.</summary>
        IList<int> Landings(int from)
        {
            if (from < 0 || from >= _keys.Count)
                return null;

            if (!(_keys[from] is Else))
                return DropPlaces(from);

            var open = _sequence.OpenOf(_shown[from]);
            var branches = _sequence.Branches(open);
            var otherwise = _sequence.Otherwise(open);
            var end = _sequence.EndOf(open);
            var span = _rows[_keys[from]].DraggedWithCount + 1;
            var allowed = new List<int>();
            for (var r = 0; r < _keys.Count; r++)
            {
                if (r >= from && r < from + span || !(_keys[r] is Step))
                    continue;

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
        List<int> DropPlaces(int from)
        {
            var span = _rows[_keys[from]].DraggedWithCount + 1;
            var gaps = new List<int>();
            var above = 0;
            object last = null;
            for (var r = 0; r <= _keys.Count; r++)
            {
                if (r < _keys.Count && r >= from && r < from + span)
                    continue;

                if (last == null || last is Step)
                    gaps.Add(above);

                if (r == _keys.Count)
                    break;

                last = _keys[r];
                above++;
            }

            return gaps;
        }

        /// <summary>Moves the dragged branch row at <paramref name="from" /> to the place in its if it was dropped.</summary>
        void Reorder(int from, int to)
        {
            var line = _shown[from];
            var open = _sequence.OpenOf(line);
            var last = from + _rows[_keys[from]].DraggedWithCount;
            var others = _shown.Count - (last - from + 1);
            if (open < 0 || to < 0 || to > others)
                return;

            var before = to == others ? _sequence.Steps.Count : _shown[to < from ? to : to + last - from + 1];

            var branches = _sequence.Branches(open);
            var target = before == _sequence.EndOf(open) ? branches.Count : branches.IndexOf(before);
            if (target < 0)
                return;

            var source = branches.IndexOf(line);
            if (target > source)
                target--;

            var limit = _sequence.Otherwise(open) >= 0 ? branches.Count - 2 : branches.Count - 1;
            if (target > limit || target == source)
                return;

            Editor.Do(_sequence, new MoveBranch(open, source, target));
        }

        /// <summary>Moves the dragged row, with the rows it carries, to the place among the other rows given by <paramref name="to" />.</summary>
        void Move(int from, int to)
        {
            if (from < 0 || from >= _shown.Count || to == from)
                return;

            if (_keys[from] is Else)
            {
                Reorder(from, to);
                return;
            }

            var last = from + _rows[_keys[from]].DraggedWithCount;
            var others = _shown.Count - (last - from + 1);
            if (last >= _shown.Count || to < 0 || to > others)
                return;

            var start = _shown[from];
            var count = _shown[last] + 1 - start;

            var before = to == others ? _sequence.Steps.Count : _shown[to < from ? to : to + last - from + 1];
            var landing = before > start ? before - count : before;

            Editor.Do(_sequence, new MoveStep(start, landing, count));
        }
    }
}
