using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Sequences;
using Lumel;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using VRageMath;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Fields;
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
    public sealed partial class SequencePage : EditorPage
    {
        const int JustRanTicks = 10;
        // How long the ring around what is tied to what is under the pointer takes to brighten and fade again: a second.
        const int PulseTicks = 60;

        readonly Sequence _sequence;
        readonly List<object> _keys = new List<object>();
        readonly List<int> _shown = new List<int>();
        Dictionary<Step, object> _ends = new Dictionary<Step, object>();

        int _wiring = -1;
        int _busy;
        int _off;
        string _skips = "";
        string _calledOff;
        Stopper _stoppedBy;
        int _stoppedAt;
        int _stoppedFrom;
        string _madeName;
        int _madeEdits;
        Keyed<object, StepRow> _rows;
        readonly List<int> _waiter = new List<int>();
        readonly List<int> _covered = new List<int>();
        readonly List<bool> _lasting = new List<bool>();
        int _hot = -1;
        bool _hotWait;
        LengthQuestion _length;

        /// <summary>The page of <paramref name="sequence" />, in the editor <paramref name="editor" />.</summary>
        public SequencePage(SprocketEditor editor, Sequence sequence) : base(editor)
        {
            _sequence = sequence;
        }

        /// <summary>The id of the sequence the page shows.</summary>
        public int SequenceId => _sequence.Id;

        /// <summary>The panel a wait's length is being given in, or null while none is open.</summary>
        public DurationPanel Lengths => _length != null && _length.IsOpen ? _length.Panel : null;

        /// <summary>A named sequence is called by its name, and an unnamed one by its number.</summary>
        /// <remarks>
        /// No subtitle. What the sequence is doing belongs beside the switch that changes it, and what starts it is a row
        /// on its system's screen: a sequence three triggers start has no one line to say so.
        /// </remarks>
        public override string Title => SequenceText.Called(Editor.Texts, _sequence);

        /// <inheritdoc />
        public override bool Edits => true;

        /// <inheritdoc />
        public override string Help => Terms.Sequence;

        /// <summary>The switch is its system's, which is also what keeps the sequence from being changed while it may run.</summary>
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

        /// <summary>Runs it by hand, whatever the switch says.</summary>
        /// <remarks>
        /// Somebody working on a system switches it off so its triggers leave it alone, which is exactly when they want to
        /// see a sequence work.
        /// </remarks>
        public override ICommand Runs => Editor.Act(Cmd.RunNow, () => Editor.RunNow(_sequence));

        /// <inheritdoc />
        public override ICommand Stops => Editor.Act(Cmd.StopNow, () => Editor.StopNow(_sequence));

        /// <summary>Deletes the sequence, as a step its logic system's undo takes back; like every edit, while it cannot run.</summary>
        public override ICommand Deletes => Editable ? Editor.Act(Cmd.DeleteSequence, () => Editor.DeleteSequence(_sequence)) : null;

        /// <inheritdoc />
        public override string DeleteLocked => Editable ? null : LockedText;

        /// <inheritdoc />
        public override string DeleteAbout => Editor.Texts.SequenceDelete;

        /// <summary>The way back of the logic system the sequence is in: the last thing done to it, on any of its screens.</summary>
        public override bool HasHistory => true;

        /// <summary>A step is walked back under the rule it was made under, which each step says for itself.</summary>
        public override bool CanUndo => Editor.Holder == null && Editor.CanUndo(_sequence.System, _sequence);

        /// <inheritdoc />
        public override string UndoText => Editor.UndoLabel(_sequence.System, _sequence);

        /// <inheritdoc />
        public override bool CanRedo => Editor.Holder == null && Editor.CanRedo(_sequence.System, _sequence);

        /// <inheritdoc />
        public override string RedoText => Editor.RedoLabel(_sequence.System, _sequence);

        /// <summary>The sequence just made is as it was made: its name, and nothing done in the program since.</summary>
        public override bool Untouched => _sequence.Name == _madeName && Editor.Program.Edits == _madeEdits;

        /// <summary>A sequence arrives already named, "Sequence 3", the way a block does, so renaming is always overtyping.</summary>
        public override string CustomName => _sequence.Name ?? "";

        /// <inheritdoc />
        public override bool CanRename => true;

        /// <inheritdoc />
        public override string NameAbout => Editor.Texts.SequenceNameAbout;

        /// <summary>A sequence has to be called something, because the name is what identifies it.</summary>
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
            var wanted = Naming.Clean(name);
            if (wanted.Length == 0)
                return;

            Editor.Do(_sequence, new Rename(_sequence.Name, wanted));
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

        /// <summary>One part: what this sequence is called, the name where there is one and what it does where there is not.</summary>
        /// <remarks>What starts it is not here: a sequence may be started from several places, and its system already lists them.</remarks>
        public override void Heading(List<HeadingPart> parts)
        {
            var does = new HeadingPart("");
            does.Mark = Symbols.Sequence;
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
            _rows = new Keyed<object, StepRow>(_steps.Children, delegate { return new StepRow(); });
            _steps.Landings = Landings;
            _steps.Moves = (from, to) =>
            {
                var move = Editor.Act(Cmd.MoveStep, () => Move(from, to));
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
            _bar.Status = status == SequenceStatus.Stopped ? CalledOff(texts, state) : SequenceText.Status(texts, status);
            _bar.JustRan = state != null && state.LastFiredTick >= 0 && tick - state.LastFiredTick < JustRanTicks;
            _bar.LastRun = Editor.Ago.Said(SequenceSummary.TicksSinceRun(state, tick));
            _bar.Skipped = Skips(texts, state?.SkippedCount ?? 0, state?.RefusedCount ?? 0);

            if (recount)
                Mark();

            // The ring around what is tied to what is under the pointer pulses while there is one.
            Light();
            var glow = _hot < 0 ? Palette.None : Pulse(tick);

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
            _busy = -1;

            // Everything that changes what the sequence does waits until its system is switched off: the commands are not
            // made, so there is nothing to press by accident, and the page keeps its shape either way.
            var editable = Editable;
            _add.Show(new Adder(texts.SequenceStep, texts.SequenceStepAbout, Kinds(texts, _sequence.Steps.Count)));
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
                row.Deletes = closes ? null : Changes(Cmd.DeleteStep, () => Remove(index));
                row.Copies = closes || branch && Kind(step) != StepKind.OtherwiseIf
                    ? null
                    : Changes(Cmd.DuplicateStep, () => Duplicate(index));
                row.IsOff = step.IsOff || index <= offTo;
                row.Pauses = branch || closes ? null : Changes(Cmd.SwitchStep, () => SwitchStep(index));
                row.Grip.CanCarry = editable && !closes && (!branch || Kind(step) == StepKind.OtherwiseIf);
                Branches(row, texts, step, index);
                row.Locked = locked;

                if (step is If || branch)
                    depth++;
            }

            Covers();
        }

        /// <summary>
        /// Works out which row waits for each step that takes time, and from which row each wait covers: every step that takes time
        /// since the wait before, down to the wait.
        /// </summary>
        /// <remarks>
        /// A step that takes time with no wait below it is covered by none, which is how the sequence leaves it to finish on its own.
        /// A paused step is read as the run reads it, as though it were not there: a paused wait covers nothing and leaves its steps
        /// to the next wait, and a paused step that takes time is not waited for.
        /// </remarks>
        void Covers()
        {
            _waiter.Clear();
            _covered.Clear();
            _lasting.Clear();
            for (var i = 0; i < _keys.Count; i++)
            {
                _waiter.Add(-1);
                _covered.Add(-1);
                var step = _keys[i] as Step;
                _lasting.Add(step != null && Lasting(step) && !_rows[_keys[i]].IsOff);
            }

            _hot = -1;
            var open = -1;
            for (var i = 0; i < _keys.Count; i++)
            {
                if (!(_keys[i] is Step))
                    continue;

                if (_lasting[i] && open < 0)
                    open = i;

                // A paused wait is read as though it were not there, its steps going to the next.
                if (!(_keys[i] is Await) || _rows[_keys[i]].IsOff || open < 0)
                    continue;

                _covered[i] = open;
                for (var j = open; j <= i; j++)
                {
                    if (_lasting[j] || j == i)
                        _waiter[j] = i;
                }

                open = -1;
            }
        }

        /// <summary>
        /// Lights what is tied to what is under the pointer, its own clock with it: on a wait, the clock marks of the steps it covers;
        /// on a step, the clock of the wait covering it.
        /// </summary>
        /// <remarks>Asked every tick, as the pointer gives no word when it moves; it lights anything only when what is under it is another.</remarks>
        void Light()
        {
            var hot = -1;
            var onWait = false;

            // Nothing is lit while a step is carried: the pointer passing over rows on its way is not pointing at them.
            for (var i = 0; !_steps.IsCarrying && i < _keys.Count && i < _waiter.Count; i++)
            {
                var row = _rows[_keys[i]];
                if (row.Kind == StepKind.Await && row.IsHot)
                {
                    hot = i;
                    onWait = true;
                    break;
                }

                if (hot < 0 && row.Hot)
                    hot = i;
            }

            if (hot == _hot && onWait == _hotWait)
                return;

            _hot = hot;
            _hotWait = onWait;

            var waiter = hot < 0 ? -1 : onWait ? _covered[hot] >= 0 ? hot : -1 : _waiter[hot];
            for (var i = 0; i < _waiter.Count; i++)
            {
                var row = _rows[_keys[i]];
                var tied = waiter >= 0 && _waiter[i] == waiter && _lasting[i];
                row.MarksLit = onWait ? tied : waiter >= 0 && i == hot;
                row.ClockLit = i == waiter;
            }

            Join();
        }

        /// <summary>Runs the spine joining lit lines from the first of them to the last.</summary>
        void Join()
        {
            var first = -1;
            var last = -1;
            for (var i = 0; i < _waiter.Count; i++)
            {
                var row = _rows[_keys[i]];
                if (!row.MarksLit && !row.ClockLit)
                    continue;

                if (first < 0)
                    first = i;

                last = i;
            }

            for (var i = 0; i < _waiter.Count; i++)
            {
                var row = _rows[_keys[i]];
                row.Joins = first >= 0 && i >= first && i <= last;
                row.JoinFirst = i == first;
                row.JoinLast = i == last;
            }
        }

        /// <summary>The ring's color at <paramref name="tick" />, brightening and fading once a second.</summary>
        static Color Pulse(int tick)
        {
            var phase = tick % PulseTicks * MathHelper.TwoPi / PulseTicks;
            return Palette.Link * (0.6f - 0.4f * (float)Math.Cos(phase));
        }

        /// <summary>Whether <paramref name="step" /> starts something that takes time: running another sequence, or an action that lasts.</summary>
        bool Lasting(Step step)
        {
            var run = step as Run;
            if (run != null)
                return !run.Stops;

            var act = step as Act;
            if (act == null || act.Sets)
                return false;

            var chosen = act.Names(Vocabulary.ActionsFor(Editor.Runner.Bindings, Editor.Seen(_sequence.System, act.Targets)));
            return chosen != null && chosen.Lasts;
        }

        /// <summary>Whether a wait follows the step at <paramref name="index" />.</summary>
        bool Waited(int index)
        {
            return index + 1 < _sequence.Steps.Count && _sequence.Steps[index + 1] is Await;
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
            row.Adds.Add(new Adder(texts.SequenceStep, texts.SequenceStepAbout, Kinds(texts, at)));
        }

        /// <summary>Every kind of step there is, each added at the line <paramref name="at" />, in the order a menu offers them.</summary>
        /// <remarks>Made when the page is arranged, which is when the program changes, so the line each adds at is current.</remarks>
        List<Adder> Kinds(Texts texts, int at)
        {
            var kinds = new List<Adder>
            {
                new Adder(texts.SequenceDoSomething, texts.SequenceDoSomethingAbout, Changes(Cmd.AddAction, () => AddAction(at))),
                new Adder(SentenceText.WaitWord, texts.SequenceWaitAbout, Changes(Cmd.AddWait, () => AddWait(at)))
            };

            // Beside the delay.
            kinds.Add(new Adder(SentenceText.AwaitWords, texts.SequenceAwaitAbout,
                Changes(Cmd.AddAwait, () => Editor.Do(_sequence, new AddStep(Change.AddStep, at, new Await())))));

            kinds.Add(new Adder(SentenceText.RunWord, texts.SequenceRunAbout, Changes(Cmd.AddRun, () => AddRun(false, at))));
            kinds.Add(new Adder(SentenceText.StopWord, texts.SequenceStopAbout, Changes(Cmd.AddStop, () => AddRun(true, at))));
            kinds.Add(new Adder(SentenceText.IfWord, texts.SequenceIfAbout, Changes(Cmd.AddIf, () => AddIf(at))));
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
                row.Adds.Add(new Adder(texts.SequenceBranch, texts.SequenceBranchAbout, new[]
                {
                    new Adder(SentenceText.OtherwiseIfWords, texts.SequenceOtherwiseIfAbout, Changes(Cmd.AddBranch, () => AddBranch(index))),
                    new Adder(SentenceText.OtherwiseWord, texts.SequenceOtherwiseAbout,
                        ends ? Changes(Cmd.AddBranch, () => AddLast(index)) : null)
                }));
            }
            else if (Kind(step) == StepKind.Otherwise)
            {
                row.Adds.Add(new Adder(SentenceText.IfWord, texts.SequenceOtherwiseTestAbout, new[]
                {
                    new Adder(SentenceText.IfWord, texts.SequenceOtherwiseTestAbout, Changes(Cmd.AddBranch, () => GiveTest(step)))
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

        /// <summary>What stopped the last run and where, in one line.</summary>
        /// <remarks>Asked every tick while it is the status, so the line is joined again only when the stop it says is another.</remarks>
        string CalledOff(Texts texts, SequenceRunner state)
        {
            if (state.StoppedBy == _stoppedBy && state.StoppedAt == _stoppedAt && state.StoppedFrom == _stoppedFrom
                && _calledOff != null)
            {
                return _calledOff;
            }

            _stoppedBy = state.StoppedBy;
            _stoppedAt = state.StoppedAt;
            _stoppedFrom = state.StoppedFrom;
            _calledOff = SequenceText.CalledOff(texts, _sequence, state);
            return _calledOff;
        }

        /// <summary>What was asked for and did not happen, and why not, in one line when both happened.</summary>
        /// <remarks>Asked every tick, so the line is joined again only when a count has changed.</remarks>
        string Skips(Texts texts, int busy, int off)
        {
            if (busy == _busy && off == _off)
                return _skips;

            _busy = busy;
            _off = off;

            var missed = busy.ToString(CultureInfo.InvariantCulture);
            var refused = off.ToString(CultureInfo.InvariantCulture);
            if (busy == 0 && off == 0)
                _skips = "";
            else if (busy == 0)
                _skips = off == 1 ? texts.SequenceSkippedOffOne : Template.Fill(texts.SequenceSkippedOff, "off", refused);
            else if (off == 0)
                _skips = busy == 1 ? texts.SequenceSkippedBusyOne : Template.Fill(texts.SequenceSkippedBusy, "busy", missed);
            else
                _skips = busy == 1
                    ? Template.Fill(texts.SequenceSkippedBothOne, "off", refused)
                    : Template.Fill(Template.Fill(texts.SequenceSkippedBoth, "busy", missed), "off", refused);

            return _skips;
        }

        /// <summary>A command made only while the sequence may be changed, and null while it may not.</summary>
        ICommand Changes(Cmd kind, Action action)
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
                row.Slots.Add(new DurationSlot(wait.Length, Changes(Cmd.StepSlot, () => Open(wait, false))));
                return;
            }

            var run = step as Run;
            if (run != null)
            {
                // Opened on the list that made it, to run or stop another.
                var called = _sequence.System?.FindSequence(run.Number);
                row.Slots.Add(new SequenceSlot(run.Number, called == null ? "" : called.Name,
                    Changes(Cmd.StepSlot, () => ChangeRun(run))));
                return;
            }

            var act = step as Act;
            if (act == null)
                return;

            // Shown as written. A step whose blocks do not offer what it does is a fault, said on the sequence, and the
            // gap in its sentence is where it is picked again.
            var chosen = act.Names(Vocabulary.ActionsFor(Editor.Runner.Bindings, Editor.Seen(_sequence.System, act.Targets)));

            row.Slots.Add(new ActionSlot(chosen, Changes(Cmd.StepSlot, () => EditStep(act, Opening.What))));
            row.Slots.Add(new EntitiesSlot(Editor.Blocks(_sequence.System, act.Targets), true,
                Changes(Cmd.StepSlot, () => EditStep(act, Opening.Blocks))));

            // Last, because the blocks are what somebody scanning a list of steps is looking for and the values are the
            // detail. Pressing it opens the step where its values are given, with the caret already in them.
            if (chosen == null || chosen.Wants == 0)
                return;

            var fields = new List<Field>();
            chosen.Fields(fields);
            var given = new ArgumentsSlot(act.Arguments, fields, Changes(Cmd.StepSlot, () => EditStep(act, Opening.Arguments)),
                act.Sets ? Given.NewValue : Given.Options);
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
                    Changes(Cmd.StepSlot, () => Quantify(step, test, numeric)), Given.Read));
            }

            // A value on some blocks, as a set step and the script say it; a status is the blocks themselves.
            if (value == null || !value.IsStatus)
                row.Slots.Add(new ActionSlot(value, Changes(Cmd.StepSlot, () => EditTest(step, test, Opening.What))));

            row.Slots.Add(new EntitiesSlot(blocks, test.Quantifier != Quantifier.Any,
                Changes(Cmd.StepSlot, () => EditTest(step, test, Opening.Blocks))));

            if (value == null)
                return;

            // The comparison is a part of its own, chosen from a menu hanging under it, and the value another, chosen with the
            // picker as every other part is.
            var fields = Comparing.Fields(value, Vocabulary.EventsFor(bindings, seen));
            var plural = test.Quantifier == Quantifier.All && blocks.Count > 1;
            row.Slots.Add(new ArgumentsSlot(new[] { Comparing.Of(test.Relation) }, fields.GetRange(0, 1),
                Changes(Cmd.StepSlot, () => Relate(step, test, fields[0], plural)), Given.Compared, plural));
            row.Slots.Add(new ArgumentsSlot(new[] { test.Value }, fields.GetRange(1, fields.Count - 1),
                Changes(Cmd.StepSlot, () => EditTest(step, test, Opening.Arguments)), Given.Checked));
        }

        /// <summary>Offers the comparisons <paramref name="relation" /> takes in a menu under the part pressed, the one <paramref name="test" /> makes marked, and changes the test to the one chosen as one edit.</summary>
        void Relate(Step step, Comparison test, Field relation, bool plural)
        {
            Editor.AskHere("");
            for (var i = 0; i < relation.Words.Count; i++)
            {
                var related = Comparing.Read(Literal.Name(relation.Means[i]));
                var said = plural ? SentenceText.Plural(relation.Words[i]) : relation.Words[i];
                Editor.Answer(said, delegate
                {
                    if (related == test.Relation)
                        return;

                    var now = test.Copy();
                    now.Relation = related;
                    Editor.Do(_sequence, new SetTest(_sequence.Steps.IndexOf(step), test, now));
                }, related == test.Relation);
            }
        }

        /// <summary>
        /// Offers the ways <paramref name="test" /> can read its blocks in a menu under the part pressed, in the sentence's own
        /// words, the one it reads them by marked, and changes it to the one chosen as one edit; a sum and an average only of
        /// what is <paramref name="numeric" />.
        /// </summary>
        void Quantify(Step step, Comparison test, bool numeric)
        {
            Editor.AskHere("");
            for (var i = 0; i < SentenceText.Readings.Count; i++)
            {
                var reading = SentenceText.Readings[i];
                if (numeric || reading == Quantifier.Any || reading == Quantifier.All)
                    Quantifying(step, test, reading);
            }
        }

        /// <summary>One answer of <see cref="Quantify" />: reading the blocks by <paramref name="quantifier" />.</summary>
        void Quantifying(Step step, Comparison test, Quantifier quantifier)
        {
            Editor.Answer(SentenceText.Quantified(quantifier), () =>
            {
                if (quantifier == test.Quantifier)
                    return;

                var now = test.Copy();
                now.Quantifier = quantifier;
                Editor.Do(_sequence, new SetTest(_sequence.Steps.IndexOf(step), test, now));
            }, quantifier == test.Quantifier);
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

        /// <summary>Opens a step's picker on the column <paramref name="opens" /> says, the part that was pressed.</summary>
        void EditStep(Act step, Opening opens)
        {
            var index = _sequence.Steps.IndexOf(step);

            // Handed what the step already holds, so the question that finishes this opens on those values rather than
            // empty: changing which blocks a step acts on is not a reason to type its values again.
            Editor.Push(new PickPage(Editor, "Do something", false,
                Editor.Blocks(_sequence.System, step.Targets), step.ActionId, step.Form,
                answer =>
                {
                    var named = Named(answer, step.Targets);
                    var lasts = Vocabulary.Lasts(Editor.World, answer.Targets, answer.Chosen, answer.Form);
                    Editor.Do(_sequence,
                        new ChangeStep(index, step, named, answer.Chosen,
                            answer.Form, Vocabulary.Provider(Editor.World, answer.Targets, answer.Chosen, answer.Form),
                            answer.Arguments, Awaited(index, step, lasts)));
                    Editor.Pop();
                }, arguments: step.Arguments, opens: opens, facet: Editor.Facet(_sequence.System, step.Targets)));
        }

        /// <summary>
        /// The names the step answered by <paramref name="answer" /> acts on, in place of <paramref name="replacing" /> where it
        /// replaces a list: a slot of the facet chosen, where what it does is a member of a type the host provides.
        /// </summary>
        List<string> Named(PickAnswer answer, IList<string> replacing)
        {
            var type = answer.Facet.Length == 0 ? null : Vocabulary.TypeOf(Editor.World, answer.Targets, answer.Chosen);
            return type == null
                ? Editor.Named(_sequence.System, answer.Targets, replacing)
                : Editor.Named(_sequence.System, type, answer.Facet, answer.Targets, replacing);
        }

        /// <summary>
        /// Whether the step at <paramref name="index" />, changed to something that <paramref name="lasts" /> or not, is followed by a
        /// wait: the one it has, kept, and one for a step that comes to take time as a new one would have.
        /// </summary>
        bool Awaited(int index, Act step, bool lasts)
        {
            return Waited(index) || lasts && !Lasting(step);
        }

        /// <summary>Asks what to affect, then what to do to it, and adds the step only once both are answered.</summary>
        /// <remarks>
        /// Blocks first, because somebody knows which block they mean but may not know what verbs exist: pointing at a
        /// door and being told what a door can do is discovery, and every verb on the ship in one list is a wall.
        /// </remarks>
        void AddAction(int at)
        {
            var origin = Editor.Depth;

            Editor.Push(new PickPage(Editor, "Do something", false,
                new List<long>(), "", Nature.Native,
                answer =>
                {
                    var step = new Act(answer.Chosen);
                    step.Form = answer.Form;
                    step.Provider = Vocabulary.Provider(Editor.World, answer.Targets, answer.Chosen, answer.Form);
                    var named = Named(answer, null);
                    foreach (var target in named)
                        step.Targets.Add(target);

                    for (var i = 0; i < answer.Arguments.Count; i++)
                        step.Arguments.Add(answer.Arguments[i]);

                    // With a wait for it after it where what it does takes time to finish, because a numbered list is
                    // read as one thing after another; one edit, so one press takes the pair back.
                    Editor.Do(_sequence, Vocabulary.Lasts(Editor.World, answer.Targets, answer.Chosen, answer.Form)
                        ? new AddStep(Change.AddStep, at, step, new Await())
                        : new AddStep(Change.AddStep, at, step));
                    Editor.UnwindTo(origin);
                }));
        }

        /// <summary>Asks what to check, and adds an if that checks it, with nothing in it yet, at the line <paramref name="at" />.</summary>
        void AddIf(int at)
        {
            var origin = Editor.Depth;
            Editor.Push(TestPage(null, Opening.Blocks, test =>
            {
                Editor.Do(_sequence, new AddStep(Change.AddIf, at, new If(test), new EndIf()));
                Editor.UnwindTo(origin);
            }));
        }

        /// <summary>Asks what to check, and adds an otherwise if checking it to the if closed at <paramref name="end" />: last, or above its otherwise, which comes last.</summary>
        void AddBranch(int end)
        {
            var origin = Editor.Depth;
            Editor.Push(TestPage(null, Opening.Blocks, test =>
            {
                var last = _sequence.Otherwise(_sequence.OpenOf(end));
                Editor.Do(_sequence, new AddStep(Change.AddBranch, last < 0 ? end : last, new Else(test)));
                Editor.UnwindTo(origin);
            }));
        }

        /// <summary>Asks what to check, and makes the otherwise <paramref name="step" /> an otherwise if checking it, where it stands.</summary>
        void GiveTest(Step step)
        {
            var origin = Editor.Depth;
            Editor.Push(TestPage(null, Opening.Blocks, test =>
            {
                Editor.Do(_sequence, new SetTest(_sequence.Steps.IndexOf(step), null, test));
                Editor.UnwindTo(origin);
            }));
        }

        /// <summary>Adds an otherwise to the if closed at <paramref name="end" />, as its last branch.</summary>
        void AddLast(int end)
        {
            Editor.Do(_sequence, new AddStep(Change.AddBranch, end, new Else(null)));
        }

        /// <summary>Opens the test of <paramref name="step" /> on the column <paramref name="opens" /> says, to be changed.</summary>
        void EditTest(Step step, Comparison test, Opening opens)
        {
            Editor.Push(TestPage(test, opens, now =>
            {
                Editor.Do(_sequence, new SetTest(_sequence.Steps.IndexOf(step), test, now));
                Editor.Pop();
            }));
        }

        /// <summary>The picker a test is made and changed with, opened on <paramref name="test" /> where there is one; <paramref name="chosen" /> is given the test made.</summary>
        PickPage TestPage(Comparison test, Opening opens, Action<Comparison> chosen)
        {
            // A sameness on a status opens on its state, chosen as it is listed; anything else on its value.
            var value = test?.Names(Vocabulary.ActionsFor(Editor.Runner.Bindings, Editor.Seen(_sequence.System, test.Targets)));
            var stated = value != null && value.IsStatus && (test.Relation == Relation.Is || test.Relation == Relation.IsNot);
            return new PickPage(Editor, "Check something", false,
                test == null ? new List<long>() : Editor.Blocks(_sequence.System, test.Targets),
                test == null ? "" : stated ? Comparing.Id(test.MemberId, test.Value.Said) : test.MemberId,
                stated ? Nature.Native : Nature.Value,
                answer => chosen(Test(answer, test)),
                true, test?.Quantifier ?? Quantifier.Any,
                test == null ? null : stated ? new[] { Comparing.Of(test.Relation) } : new[] { Comparing.Of(test.Relation), test.Value },
                opens, forTests: true, facet: test == null ? "" : Editor.Facet(_sequence.System, test.Targets));
        }

        /// <summary>The test a pick for one comes to, changing <paramref name="was" /> where there was one.</summary>
        /// <remarks>
        /// The picker asks for the blocks, the value, how it is compared and with what. A sum and a function's arguments are
        /// kept from <paramref name="was" /> where the same value is picked again, so changing the blocks of a test written in
        /// the script changes nothing else.
        /// </remarks>
        Comparison Test(PickAnswer answer, Comparison was)
        {
            var test = new Comparison();
            test.Quantifier = answer.Quantifier;
            var named = Named(answer, was?.Targets);
            foreach (var target in named)
                test.Targets.Add(target);

            // Read off the blocks picked, since a slot made for them is not filled until the edit is done.
            string id, state;
            var stated = Comparing.Split(answer.Chosen, out id, out state);
            var value = Valued(Vocabulary.ActionsOf(Editor.World, answer.Targets), id);
            var given = answer.Arguments;
            test.MemberId = id;
            test.Provider = value != null && value.Being != null ? BlockKinds.Own : "";
            test.Relation = given != null && given.Count > 0 ? Comparing.Read(given[0]) : Relation.Is;
            test.Value = stated ? Stated(state) : given != null && given.Count > 1 ? given[1] : Literal.Of(true);
            if (was == null || was.MemberId != test.MemberId || was.Provider != test.Provider)
                return test;

            for (var i = 0; i < was.Arguments.Count; i++)
                test.Arguments.Add(was.Arguments[i]);

            return test;
        }

        /// <summary>The state a choice checking a status names, as the program holds it: on or off for a switch, a name otherwise.</summary>
        static Literal Stated(string state)
        {
            if (state == Literal.Of(true).Said)
                return Literal.Of(true);

            return state == Literal.Of(false).Said ? Literal.Of(false) : Literal.Name(state);
        }

        /// <summary>The value called <paramref name="id" /> among <paramref name="actions" />, or null where there is none.</summary>
        static EntityAction Valued(IList<EntityAction> actions, string id)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == id && actions[i].Form == Nature.Value)
                    return actions[i];
            }

            return null;
        }

        /// <summary>Adds a wait of a second at <paramref name="at" />, in the player's unit, and opens the panel its length is given in.</summary>
        void AddWait(int at)
        {
            var wait = new Wait(Editor.NewLength);
            Editor.Do(_sequence, new AddStep(Change.AddWait, at, wait));
            Open(wait, true);
        }

        /// <summary>Asks which other sequence of the system to run, or to stop where <paramref name="stops" /> says so, and adds the step that does.</summary>
        /// <remarks>
        /// Running one comes with a wait for it after it, one edit, because a numbered list is read as one thing after another;
        /// stopping one is done at once.
        /// </remarks>
        void AddRun(bool stops, int at)
        {
            var origin = Editor.Depth;
            Editor.Push(RunPage(stops, 0, number =>
            {
                Editor.Do(_sequence, stops
                    ? new AddStep(Change.AddStep, at, new Run(number, true))
                    : new AddStep(Change.AddStep, at, new Run(number), new Await()));
                Editor.UnwindTo(origin);
            }));
        }

        /// <summary>Asks again which sequence <paramref name="step" /> runs or stops, on the list that made it.</summary>
        void ChangeRun(Run step)
        {
            var origin = Editor.Depth;
            Editor.Push(RunPage(step.Stops, step.Number, number =>
            {
                Editor.Do(_sequence, new SetRun(_sequence.Steps.IndexOf(step), step.Number, number));
                Editor.UnwindTo(origin);
            }));
        }

        /// <summary>The system's other sequences, to run or to stop, with <paramref name="current" /> marked where it is one; <paramref name="chosen" /> is given the one chosen.</summary>
        ChoicePage RunPage(bool stops, int current, Action<int> chosen)
        {
            var texts = Editor.Texts;
            var page = new ChoicePage(Editor, stops ? texts.SequenceStopQuestion : texts.SequenceRunQuestion)
            {
                EmptyText = stops ? texts.SequenceStopNone : texts.SequenceRunNone
            };

            var sequences = _sequence.System.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var other = sequences[i];
                if (other == _sequence)
                    continue;

                page.Add(SequenceText.Called(texts, other), other.Number == current, () => chosen(other.Number));
                page.MarkLast(Symbols.Sequence);
            }

            return page;
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
                (length, cancelled) => Closed(wait, length, added, cancelled));
            _length.Hang();
        }

        /// <summary>Gives <paramref name="wait" /> the <paramref name="length" /> its panel closed on, or leaves it as it was where it was <paramref name="cancelled" />.</summary>
        void Closed(Wait wait, Duration? length, bool added, bool cancelled)
        {
            var index = _sequence.Steps.IndexOf(wait);
            if (index < 0)
                return;

            if (added && cancelled)
            {
                Editor.Withdraw(_sequence);
                return;
            }

            if (cancelled || !length.HasValue || length.Value == wait.Length)
                return;

            if (added && Editor.Withdraw(_sequence))
                Editor.Do(_sequence, new AddStep(Change.AddWait, index, new Wait(length.Value)));
            else
                Editor.Do(_sequence, new SetWait(index, wait.Length, length.Value));
        }

        /// <summary>Deletes the row's step; an if, or a branch of one, asks first whether the steps in it go too.</summary>
        void Remove(int index)
        {
            var step = _sequence.Steps[index];
            if (!(step is Branch))
            {
                RemoveStep(index);
                return;
            }

            var whole = step is If;
            var texts = Editor.Texts;
            Editor.AskHere(whole ? texts.SequenceRemoveIf : texts.SequenceRemoveBranch);
            Editor.Answer(texts.SequenceKeepSteps, () => Unwrap(index, whole));
            Editor.Answer(texts.SequenceDeleteSteps, () => Cut(index, whole));
            Editor.Answer(texts.ButtonLeaveAlone, () => { }, true);
        }

        /// <summary>Puts a copy of the row's step right after it: whatever a drag would carry, an if with every step in it, a branch with its own.</summary>
        void Duplicate(int index)
        {
            var steps = _sequence.Steps;
            int end;
            if (steps[index] is If)
                end = _sequence.EndOf(index) + 1;
            else if (steps[index] is Else)
                end = _sequence.BranchAfter(index);
            else
                end = index + 1;

            var copies = new List<Step>();
            for (var i = index; i < end; i++)
                copies.Add(Automation.Copied(steps[i]));

            Editor.Do(_sequence, new AddStep(Change.CopyStep, end, copies.ToArray()));
        }

        void RemoveStep(int index)
        {
            Editor.Do(_sequence, new RemoveStep(index, _sequence.Steps[index]));
        }

        /// <summary>
        /// Takes out the if at <paramref name="index" /> where <paramref name="whole" /> says so, its own lines only, or the branch
        /// line there, keeping every step that was in it where it stands.
        /// </summary>
        /// <remarks>A branch's steps join the branch before it.</remarks>
        void Unwrap(int index, bool whole)
        {
            var lines = whole ? _sequence.Branches(index) : new List<int> { index };
            if (whole)
                lines.Add(_sequence.EndOf(index));

            var steps = new List<Step>();
            foreach (var line in lines)
                steps.Add(_sequence.Steps[line]);

            Editor.Do(_sequence, new Unwrap(whole ? Change.Unwrap : Change.RemoveBranch, lines, steps));
        }

        /// <summary>Deletes the if at <paramref name="index" /> where <paramref name="whole" /> says so, or the branch there, with every step in it.</summary>
        void Cut(int index, bool whole)
        {
            var end = whole ? _sequence.EndOf(index) + 1 : _sequence.BranchAfter(index);

            var going = new List<Step>();
            for (var i = index; i < end && i < _sequence.Steps.Count; i++)
                going.Add(_sequence.Steps[i]);

            Editor.Do(_sequence, new RemoveStep(whole ? Change.RemoveIf : Change.RemoveBranch, index, going.ToArray()));
        }

        /// <summary>Pauses the row's step, or lets it go again.</summary>
        void SwitchStep(int index)
        {
            Editor.Do(_sequence, new SwitchStep(index, 1, !_sequence.Steps[index].IsOff));
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
