using System;
using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.Runtime;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// The editor. All of it.
    /// </summary>
    /// <remarks>
    /// Navigation, page contents, slot filling, search, the wording of every
    /// sentence and every status line live here rather than in a target. A target
    /// renders <see cref="Screen"/> and sends back command ids; it decides nothing
    /// about what the editor means or does.
    ///
    /// That split is the whole architecture. Anything a target has to re-derive is a
    /// thing that will drift between targets, and the second implementation is where
    /// you find out.
    /// </remarks>
    public sealed class ReactorEditor
    {
        readonly IWorld _world;
        readonly Automation _program;
        readonly AutomationRunner _runner;
        readonly List<EditorPage> _stack = new List<EditorPage>();
        readonly Dictionary<int, Action> _actions = new Dictionary<int, Action>();
        readonly Dictionary<int, Action<string>> _textActions = new Dictionary<int, Action<string>>();
        readonly Dictionary<int, Action<bool>> _toggleActions = new Dictionary<int, Action<bool>>();
        readonly Dictionary<int, Action<int>> _numberActions = new Dictionary<int, Action<int>>();
        readonly EditorScreen _screen = new EditorScreen();
        readonly List<EditorComponent> _gathered = new List<EditorComponent>();
        readonly List<ChoiceEntry> _actionBar = new List<ChoiceEntry>();

        bool _structureDirty;

        public ReactorEditor(IWorld world, Automation program, AutomationRunner runner)
        {
            _world = world;
            _program = program;
            _runner = runner;
            _stack.Add(new SystemsPage(this));
            Rebuild();
        }

        /// <summary>What to show right now.</summary>
        public EditorScreen Screen { get { return _screen; } }

        /// <summary>Ticks elapsed. The only honest unit of time available.</summary>
        public int CurrentTick { get { return _runner.CurrentTick; } }

        /// <summary>
        /// Advances everything to the given frame and brings the screen up to date.
        /// </summary>
        /// <remarks>
        /// The clock belongs to the target, and so does the frame number: Space
        /// Engineers passes its own counter, a desktop host passes one it keeps, a
        /// test passes whatever it likes. Nothing in core counts time for itself.
        /// </remarks>
        public void Tick(int tick)
        {
            _runner.Tick(tick);

            if (_structureDirty)
            {
                Rebuild();
                return;
            }

            Current.Refresh();

            // All three are read every tick rather than only when the screen is
            // rebuilt, so naming something shows up in the heading straight away.
            _screen.Title = Current.Title;
            _screen.Subtitle = Current.Subtitle;
            _screen.CustomName = Current.CustomName;
            _screen.SwitchIsOn = Current.SwitchIsOn;
        }

        /// <summary>Acts on a button, a row, or anything else that was pressed.</summary>
        public void Invoke(int command)
        {
            Action action;
            if (!_actions.TryGetValue(command, out action))
                return;

            action();
            Settle();
        }

        /// <summary>Reports new text for a search box or any other field.</summary>
        public void SetText(int command, string value)
        {
            Action<string> action;
            if (!_textActions.TryGetValue(command, out action))
                return;

            action(value ?? "");
            Settle();
        }

        /// <summary>
        /// Reports a number somebody chose - a position to move something to, most of all.
        /// </summary>
        /// <remarks>
        /// The third shape of answer, beside text and a toggle. Without it core could only
        /// offer a step "up" and "down", which is a widget rather than a capability: a target
        /// that wanted dragging had to synthesise a run of single moves and one with a number
        /// field could not express itself at all.
        ///
        /// Out of range is ignored rather than refused. A drop past the end of a list is an
        /// ordinary thing for a pointer to do.
        /// </remarks>
        public void SetNumber(int command, int value)
        {
            Action<int> action;
            if (!_numberActions.TryGetValue(command, out action))
                return;

            action(value);
            Settle();
        }

        /// <summary>Reports a toggle being flipped.</summary>
        public void SetToggle(int command, bool on)
        {
            Action<bool> action;
            if (!_toggleActions.TryGetValue(command, out action))
                return;

            action(on);
            Settle();
        }

        internal IWorld World { get { return _world; } }

        internal Automation Program { get { return _program; } }

        internal AutomationRunner Runner { get { return _runner; } }

        internal EditorPage Current { get { return _stack[_stack.Count - 1]; } }

        /// <summary>How deep the stack is, for a run of questions to return to.</summary>
        internal int Depth { get { return _stack.Count; } }

        /// <summary>
        /// Drops every page above the given depth, so answering the last question
        /// lands back where the run started rather than walking out through it.
        /// </summary>
        internal void UnwindTo(int depth)
        {
            while (_stack.Count > depth && _stack.Count > 1)
                _stack.RemoveAt(_stack.Count - 1);

            _structureDirty = true;
        }

        /// <summary>
        /// Gives a command an id that says what it is.
        /// </summary>
        /// <remarks>
        /// The same control gets the same id on every rebuild, by construction. That is the
        /// whole point: an id used to be a position in this rebuild's registration order, so
        /// anything holding one went stale the moment a page changed shape, silently, because
        /// <see cref="Invoke"/> ignores what it does not recognise.
        ///
        /// Registering the same identity twice is a mistake and says so. Under the old counter
        /// the equivalent - two controls that ought to be distinguishable ending up
        /// indistinguishable - could not be detected at all.
        /// </remarks>
        internal int Register(Cmd kind, Action action)
        {
            return Register(kind, 0, 0, action);
        }

        internal int Register(Cmd kind, int which, Action action)
        {
            return Register(kind, which, 0, action);
        }

        internal int Register(Cmd kind, int which, int part, Action action)
        {
            var id = CommandId.Of(kind, which, part);
            Claim(id, kind);
            _actions.Add(id, action);
            return id;
        }

        internal int RegisterText(Cmd kind, Action<string> action)
        {
            return RegisterText(kind, 0, action);
        }

        internal int RegisterText(Cmd kind, int which, Action<string> action)
        {
            var id = CommandId.Of(kind, which);
            Claim(id, kind);
            _textActions.Add(id, action);
            return id;
        }

        internal int RegisterNumber(Cmd kind, int which, Action<int> action)
        {
            var id = CommandId.Of(kind, which);
            Claim(id, kind);
            _numberActions.Add(id, action);
            return id;
        }

        internal int RegisterToggle(Cmd kind, Action<bool> action)
        {
            return RegisterToggle(kind, 0, action);
        }

        internal int RegisterToggle(Cmd kind, int which, Action<bool> action)
        {
            return RegisterToggle(kind, which, 0, action);
        }

        internal int RegisterToggle(Cmd kind, int which, int part, Action<bool> action)
        {
            var id = CommandId.Of(kind, which, part);
            Claim(id, kind);
            _toggleActions.Add(id, action);
            return id;
        }

        /// <summary>
        /// Refuses to let two commands share one identity.
        /// </summary>
        /// <remarks>
        /// Loud, and on purpose. A page that hands the same id to two controls has one of them
        /// doing the other's job, and the symptom - a button that does the wrong thing - would
        /// be blamed on anything but the numbering.
        /// </remarks>
        void Claim(int id, Cmd kind)
        {
            if (_actions.ContainsKey(id) || _textActions.ContainsKey(id)
                || _toggleActions.ContainsKey(id) || _numberActions.ContainsKey(id))
            {
                throw new InvalidOperationException(
                    "Two commands were registered as " + kind + " with the same number. "
                    + "A command's identity has to be unique on its screen.");
            }
        }

        internal void Push(EditorPage page)
        {
            _stack.Add(page);
            _structureDirty = true;
        }

        internal void Pop()
        {
            if (_stack.Count <= 1)
                return;

            _stack.RemoveAt(_stack.Count - 1);
            _structureDirty = true;
        }

        internal void Reset(EditorPage page)
        {
            _stack.Clear();
            _stack.Add(page);
            _structureDirty = true;
        }

        /// <summary>The shape of the screen has changed and must be rebuilt.</summary>
        internal void Invalidate()
        {
            _structureDirty = true;
        }

        /// <summary>
        /// Drops any page showing the given sequence, so deleting one never leaves the
        /// user staring at something that no longer exists.
        /// </summary>
        /// <summary>
        /// What has been done to each sequence, so it can be taken back.
        /// </summary>
        /// <remarks>
        /// Here rather than on the sequence itself, because it is a fact about somebody's
        /// editing session and not about the system. A sequence that is exported, shared or
        /// saved should carry what it is, never how its author got there.
        ///
        /// Kept per sequence and across navigation, so backing out to look at something and
        /// coming back does not silently throw the safety net away.
        /// </remarks>
        readonly Dictionary<int, List<SequenceEdit>> _history =
            new Dictionary<int, List<SequenceEdit>>();

        /// <summary>
        /// What has been taken back and could be put back again.
        /// </summary>
        /// <remarks>
        /// Emptied by the next edit, which is the standard behaviour and the right one: once
        /// somebody has walked back three steps and then done something different, the three
        /// they walked back are a road not taken. Offering to redo into a sequence that has
        /// moved on since would put back a step at an index that means something else now.
        /// </remarks>
        readonly Dictionary<int, List<SequenceEdit>> _redo =
            new Dictionary<int, List<SequenceEdit>>();

        /// <summary>
        /// How far back one sequence can be walked.
        /// </summary>
        /// <remarks>
        /// Deep enough that nobody reaches the end of it while fixing a mistake, shallow
        /// enough that a long session cannot grow without bound. Reactor runs inside a game.
        /// </remarks>
        const int HistoryDepth = 50;

        /// <summary>
        /// Makes a change and remembers how to take it back.
        /// </summary>
        /// <remarks>
        /// The only way anything changes a sequence. Applying and recording are one call so
        /// they cannot come apart - an edit that is applied without being recorded is a hole
        /// in the way back, and one recorded without being applied is worse.
        /// </remarks>
        internal void Do(Sequence sequence, SequenceEdit edit)
        {
            Do(sequence, edit, false);
        }

        /// <summary>
        /// The same, optionally folding a run of the same kind of change into one.
        /// </summary>
        /// <remarks>
        /// Typing a name arrives one letter at a time. Fifty entries all saying "renaming"
        /// would be a stack somebody has to press through rather than a way back.
        /// </remarks>
        internal void Do(Sequence sequence, SequenceEdit edit, bool coalesce)
        {
            if (!edit.Changes)
                return;

            edit.Apply(sequence);

            // A new edit is a new branch, so whatever was walked back is gone.
            _redo.Remove(sequence.Id);

            var stack = HistoryFor(sequence);
            if (!coalesce || stack.Count == 0 || stack[stack.Count - 1].Label != edit.Label)
            {
                stack.Add(edit);

                if (stack.Count > HistoryDepth)
                    stack.RemoveAt(0);
            }

            if (edit.ChangesShape)
                Invalidate();
        }

        internal bool CanUndo(Sequence sequence)
        {
            List<SequenceEdit> stack;
            return _history.TryGetValue(sequence.Id, out stack) && stack.Count > 0;
        }

        /// <summary>What undoing would undo, in the words it happened in.</summary>
        internal string UndoLabel(Sequence sequence)
        {
            List<SequenceEdit> stack;
            if (!_history.TryGetValue(sequence.Id, out stack) || stack.Count == 0)
                return "";

            return stack[stack.Count - 1].Label;
        }

        internal void Undo(Sequence sequence)
        {
            List<SequenceEdit> stack;
            if (!_history.TryGetValue(sequence.Id, out stack) || stack.Count == 0)
                return;

            var last = stack.Count - 1;
            var edit = stack[last];
            stack.RemoveAt(last);

            edit.Undo(sequence);
            Waiting(sequence).Add(edit);

            Invalidate();
        }

        internal bool CanRedo(Sequence sequence)
        {
            List<SequenceEdit> stack;
            return _redo.TryGetValue(sequence.Id, out stack) && stack.Count > 0;
        }

        /// <summary>What redoing would put back, in the words it happened in.</summary>
        internal string RedoLabel(Sequence sequence)
        {
            List<SequenceEdit> stack;
            if (!_redo.TryGetValue(sequence.Id, out stack) || stack.Count == 0)
                return "";

            return stack[stack.Count - 1].Label;
        }

        /// <summary>
        /// Puts back the last thing undone.
        /// </summary>
        /// <remarks>
        /// The same <c>Apply</c> the original edit ran, not a third code path. An edit that
        /// could be applied, reversed, and then not applied again the same way would be a
        /// stranger bug than either half alone.
        /// </remarks>
        internal void Redo(Sequence sequence)
        {
            List<SequenceEdit> stack;
            if (!_redo.TryGetValue(sequence.Id, out stack) || stack.Count == 0)
                return;

            var last = stack.Count - 1;
            var edit = stack[last];
            stack.RemoveAt(last);

            edit.Apply(sequence);
            HistoryFor(sequence).Add(edit);

            Invalidate();
        }

        List<SequenceEdit> Waiting(Sequence sequence)
        {
            List<SequenceEdit> stack;
            if (!_redo.TryGetValue(sequence.Id, out stack))
            {
                stack = new List<SequenceEdit>();
                _redo[sequence.Id] = stack;
            }
            return stack;
        }

        List<SequenceEdit> HistoryFor(Sequence sequence)
        {
            List<SequenceEdit> stack;
            if (!_history.TryGetValue(sequence.Id, out stack))
            {
                stack = new List<SequenceEdit>();
                _history[sequence.Id] = stack;
            }
            return stack;
        }

        void ForgetHistory(int sequenceId)
        {
            _history.Remove(sequenceId);
            _redo.Remove(sequenceId);
        }

        internal void ForgetSequence(int sequenceId)
        {
            ForgetHistory(sequenceId);

            for (var i = _stack.Count - 1; i > 0; i--)
            {
                var page = _stack[i] as SequencePage;
                if (page != null && page.SequenceId == sequenceId)
                    _stack.RemoveAt(i);
            }
            _structureDirty = true;
        }

        internal bool IsRootPage<T>() where T : EditorPage
        {
            return _stack.Count > 0 && _stack[0] is T;
        }

        void Settle()
        {
            if (_structureDirty)
                Rebuild();
        }

        /// <summary>
        /// Rebuilds the screen from the page on top of the stack.
        /// </summary>
        /// <remarks>
        /// The component list is gathered first and published in one go. Clearing and
        /// re-adding would announce a half-built screen to anything watching, and a
        /// target would draw it.
        ///
        /// Pages hand back the same component instances wherever they can, so a
        /// target that keys its controls on identity keeps them - along with focus,
        /// carets and scroll positions - even though this ran.
        /// </remarks>
        void Rebuild()
        {
            _actions.Clear();
            _textActions.Clear();
            _toggleActions.Clear();
            _numberActions.Clear();

            var page = Current;
            _screen.Title = page.Title;
            _screen.CanGoBack = _stack.Count > 1;
            _screen.BackCommand = _screen.CanGoBack ? Register(Cmd.Back, Pop) : 0;
            _screen.RenameCommand = page.CanRename ? RegisterText(Cmd.Rename, page.Rename) : 0;
            _screen.RenamePrompt = page.RenamePrompt;
            _screen.SwitchCommand = page.CanSwitch ? RegisterToggle(Cmd.Switch, page.Switch) : 0;
            _screen.PrimaryText = page.PrimaryText;
            _screen.PrimaryCommand = _screen.HasPrimary ? Register(Cmd.Primary, page.Primary) : 0;

            _gathered.Clear();
            page.Build(_gathered);
            page.Refresh();

            if (!SameComponents(_gathered))
                _screen.Components.Reset(_gathered);

            _screen.Subtitle = page.Subtitle;
            _screen.CustomName = page.CustomName;
            _screen.SwitchIsOn = page.SwitchIsOn;
            BuildActions();

            _structureDirty = false;
        }

        bool SameComponents(List<EditorComponent> gathered)
        {
            if (gathered.Count != _screen.Components.Count)
                return false;

            for (var i = 0; i < gathered.Count; i++)
            {
                if (!ReferenceEquals(gathered[i], _screen.Components[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Rebuilds the screen-level actions, replacing the previous set outright.
        /// </summary>
        /// <remarks>
        /// Replaced rather than appended, because their command ids are re-issued on
        /// every rebuild. A leftover entry would still look right and point at an id
        /// that no longer exists, so pressing it would silently do nothing.
        /// </remarks>
        void BuildActions()
        {
            _actionBar.Clear();

            // Deliberately empty. A permanent strip of tabs along the bottom is a thing to
            // learn before you have done anything, and two of the three were only ever
            // navigation - which the screens themselves can offer, where they mean something.
            _screen.Actions.Reset(_actionBar);
        }

        void GoToSystems()
        {
            Reset(new SystemsPage(this));
        }

        /// <summary>
        /// Asks what should start it, before anything else.
        /// </summary>
        /// <remarks>
        /// The opening question is the verb, not the thing. Someone who arrives
        /// knowing what they want thinks "when the button is pressed, open the door"
        /// - so "When" is the first word they should meet, and the blocks come after
        /// it in the order the sentence reads.
        ///
        /// Browsing the ship is still there, but as a way to look things up rather
        /// than as the only door in.
        /// </remarks>
        /// <summary>
        /// Loading a system somebody else made. Not built yet, and says so.
        /// </summary>
        /// <remarks>
        /// Present rather than hidden because it is half of what the front door offers, and
        /// a screen that grows an option later teaches itself twice. What it needs is the
        /// configurator, which needs roles.
        /// </remarks>
        /// <summary>
        /// Another one of a system already on the ship. Not built yet, and says so.
        /// </summary>
        /// <remarks>
        /// It opens the same configurator loading does, because they are the same act: point a
        /// system at the blocks it should use here. Neither exists until roles do.
        /// </remarks>
        internal void OfferAnother(AutomationSystem system)
        {
            var page = new ChoicePage(this, "Add another");
            page.EmptyText = "Not built yet. This would ask which of your " + _world.Noun.Many
                + " this one uses, "
                + "and everything it does would stay the same as the others.";
            Push(page);
        }

        internal void OfferLoading()
        {
            var page = new ChoicePage(this, "Load system");
            page.EmptyText = "Not built yet. A system somebody sent you would be set up here, "
                + "asking which of your " + _world.Noun.Many + " fill each part of it.";
            Push(page);
        }

        /// <summary>
        /// The ways a sequence can begin, offered on the system's own screen.
        /// </summary>
        /// <remarks>
        /// These used to be a screen of their own, asked the moment a system was made. It
        /// bought nothing: a question with two answers and no context, standing between
        /// somebody and the thing they came to build, and it left the system's own screen off
        /// the way back. They belong at the top of the system, in the same place and the same
        /// shape as the ship's own "new system" - it is the same question either way, which is
        /// what would you like to add here.
        ///
        /// The catalogue lives here rather than on the page so a third way to start something
        /// is one line in one place.
        ///
        /// Each label is the first word of the sentence it makes: "When..." becomes "When the
        /// airlock button is pressed", "Every..." becomes "Every 30 seconds". The ellipsis is
        /// the promise that the rest gets filled in. They read as answers to a question -
        /// "when something happens", "every so often" - only because they were once exactly
        /// that, on a screen headed "What should start it?" that no longer exists.
        /// </remarks>
        internal void AddStarters(StartComponent start, AutomationSystem system)
        {
            start.Add("When...",
                Register(Cmd.StartWhen, delegate { StartSequence(system, new WhenTrigger("")); }));

            start.Add("Every...", Register(Cmd.StartEvery,
                delegate { StartSequence(system, new EveryTrigger(Cadences.Default)); }));
        }

        /// <summary>
        /// Asks the questions a new sequence needs, one screen at a time.
        /// </summary>
        /// <remarks>
        /// Answering "when something happens" and then being handed a half-empty
        /// sentence to poke at is a stall, so the next question is asked straight
        /// away: what should it listen to, then what should it do.
        ///
        /// Going back retreats one question, all the way out to where the user
        /// started. It is never repurposed as "finished" - moving on is its own
        /// action, so somebody who genuinely wants to retreat still has somewhere to
        /// press.
        /// </remarks>
        internal void StartSequence(Trigger trigger)
        {
            StartSequence(null, trigger);
        }

        /// <summary>
        /// The same, for a sequence that belongs to a system somebody is already looking at.
        /// </summary>
        internal void StartSequence(AutomationSystem system, Trigger trigger)
        {
            _pendingSystem = system;

            // Where to unwind to once the questions are answered. The system's screen is
            // already underneath - the starters live on it now - so everything the questions
            // pushed is dropped and the finished sequence lands directly on it.
            var origin = _stack.Count;

            var when = trigger as WhenTrigger;
            if (when != null)
            {
                Push(new PickPage(this, "When something happens", true,
                    when.Sources, when.EventId,
                    delegate(PickAnswer answer)
                    {
                        // Nothing to record: the sequence does not exist yet, so there is no
                        // history for it and nothing to take back to.
                        when.Sources.Clear();
                        for (var i = 0; i < answer.Targets.Count; i++)
                            when.Sources.Add(answer.Targets[i]);

                        when.EventId = answer.Chosen;
                        when.RequiresAll = answer.All;
                        FinishSequence(when, origin);
                    },
                    true, when.RequiresAll));
                return;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                Push(RatePage(every, delegate { FinishSequence(every, origin); }));
        }

        /// <summary>The repeat rates, shared by the wizard and by editing a sequence.</summary>
        internal ChoicePage RatePage(EveryTrigger every, Action chosen)
        {
            var page = new ChoicePage(this, "How often?");
            var cadences = Cadences.All;

            for (var i = 0; i < cadences.Count; i++)
            {
                var ticks = cadences[i].Ticks;
                var label = ticks == 1 ? "every tick" : cadences[i].Name + " - every " + ticks + " ticks";

                page.Add(label, ticks == every.IntervalTicks, ticks, delegate
                {
                    every.IntervalTicks = ticks;
                    chosen();
                });
                page.WarnOnLast(cadences[i].Warning);
            }

            return page;
        }

        /// <summary>
        /// Adds the sequence and opens it, dropping the questions behind it.
        /// </summary>
        /// <remarks>
        /// The sequence does not exist until here, so backing out of the questions
        /// leaves nothing behind. Creating it up front and tidying up afterwards
        /// would mean an abandoned run littered the list with an empty sequence.
        /// </remarks>
        void FinishSequence(Trigger trigger, int origin)
        {
            var system = _pendingSystem;
            _pendingSystem = null;

            UnwindTo(origin);

            // Parked until somebody says otherwise. A sequence with a trigger and no steps is
            // already live under the old default, and it becomes progressively more live as
            // each step is added - a system half-doing something nobody has finished
            // describing. It is also the only way the switch could gate editing without a new
            // sequence being unbuildable the moment it exists.
            var sequence = _program.AddSequence(system, trigger);
            sequence.Enabled = false;

            Push(new SequencePage(this, sequence));
        }

        /// <summary>
        /// The system a sequence being built belongs to, or null to make a new one.
        /// </summary>
        /// <remarks>
        /// Held across the run of questions rather than passed through them, because every
        /// question in between is about the sequence and none of them cares which system it
        /// is for. Cleared when the sequence is made, so an abandoned run leaves nothing.
        /// </remarks>
        AutomationSystem _pendingSystem;
    }
}
