using System;
using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Host.SpaceEngineers.Runtime;
using Reactor.Host.SpaceEngineers.Lists;
using Reactor.Host.SpaceEngineers.Screen;
using Reactor.Host.SpaceEngineers.World;
using Reactor.Host.SpaceEngineers.Words;

namespace Reactor.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// The editor. All of it.
    /// </summary>
    /// <remarks>
    /// Navigation, page contents, slot filling and search live here rather than in the screens.
    /// What is drawn renders <see cref="Screen" /> and sends back command ids; it decides nothing
    /// about what the editor means or does, and nothing it draws has to be worked out twice.
    ///
    /// The wording is the one thing this does not own - see <see cref="Words.SequenceText" />, which
    /// decides word order, capitals and every other thing that is a language's business rather than
    /// automation's.
    /// </remarks>
    public sealed class ReactorEditor
    {
        readonly GridWorld _world;
        readonly Automation _program;
        readonly AutomationRunner _runner;
        string _author = "";
        readonly List<EditorPage> _stack = new List<EditorPage>();
        readonly Dictionary<int, Action> _actions = new Dictionary<int, Action>();
        readonly Dictionary<int, Action<string>> _textActions = new Dictionary<int, Action<string>>();
        readonly Dictionary<int, Func<string, bool>> _textChecks = new Dictionary<int, Func<string, bool>>();
        readonly Dictionary<int, Action<bool>> _toggleActions = new Dictionary<int, Action<bool>>();
        readonly Dictionary<int, Action<int>> _numberActions = new Dictionary<int, Action<int>>();
        readonly EditorScreen _screen = new EditorScreen();
        readonly List<EditorComponent> _gathered = new List<EditorComponent>();
        // Deliberately never added to - see where it is published. The analyser is right that it is
        // never updated; that is the point.
        // ReSharper disable once CollectionNeverUpdated.Local
        readonly List<ChoiceEntry> _actionBar = new List<ChoiceEntry>();

        bool _structureDirty;

        public ReactorEditor(GridWorld world, Automation program, AutomationRunner runner)
        {
            _world = world;
            _program = program;
            _runner = runner;
            _stack.Add(new SystemsPage(this));
            Rebuild();
        }

        /// <summary>What to show right now.</summary>
        public EditorScreen Screen => _screen;

        /// <summary>
        /// Who the systems made here are by: the namespace a new system gets.
        /// </summary>
        /// <remarks>
        /// Set by the host, which knows who is at the screen - in the game, the player's name until they
        /// choose one of their own. Empty makes systems with no namespace, which is only ever a host that has
        /// not said.
        /// </remarks>
        public string Author
        {
            get { return _author; }
            set { _author = value ?? ""; }
        }

        /// <summary>Ticks elapsed. The only honest unit of time available.</summary>
        public int CurrentTick => _runner.CurrentTick;

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

            // Once a second, and whether or not anybody has a screen open, because a sequence that
            // cannot work is not made safer by nobody looking at it.
            if (_parked.Due(tick))
                ParkBroken();

            if (_structureDirty)
            {
                Rebuild();
                return;
            }

            Current.Refresh();

            // The heading's words are made of things that can change without anything saying so: the
            // grid's own name, and the name of a block inside a sentence. Nothing in the game's own
            // interface announces either, so they are read again on a cadence - a second, the same
            // cadence the rest of this class already keeps time by. A name changed here shows at once,
            // because SetText reads them back the moment it lands; somebody else's shows within a
            // second, which nobody can tell from at once.
            //
            // Its words only. The heading's shape was settled when the screen was made, and the target
            // has already built controls to match it.
            if (_worded.Due(tick))
                Current.RefreshHeading(_screen.Heading);

            _screen.Subtitle = Current.Subtitle;
            _screen.SwitchIsOn = Current.SwitchIsOn;
        }

        /// <summary>
        /// Switches off anything that cannot work.
        /// </summary>
        /// <remarks>
        /// Reactor switching off somebody's automation, which it does nowhere else, and the reason it
        /// is allowed here is that there is nothing to switch off: the thing is already not running.
        /// A sequence nothing can start never fires, and one whose blocks are gone can only get part
        /// way through - which is worse than not starting, because it leaves a machine half-way.
        /// <para>
        /// It also removes a step. A sequence may only be edited while it is switched off, so finding
        /// a broken one meant switching it off yourself before you could mend it - an errand on top of
        /// a problem you did not ask for.
        /// </para>
        /// <para>
        /// <b>Off, and never back on.</b> Mending it leaves it parked, and switching a sequence on is
        /// somebody's decision every time - Reactor starting a machine on its own judgement is the one
        /// direction that can hurt. Leaving the editor with it off is where the offer to switch it on
        /// belongs.
        /// </para>
        /// </remarks>
        void ParkBroken()
        {
            var sequences = _program.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                Sequence sequence = sequences[i];
                if (!sequence.Enabled)
                    continue;

                // Faults only. A sequence nothing starts yet is not broken, and parking it would make
                // the step somebody is about to point at it fail for a second reason.
                var fault = SequenceSummary.Fault(_world, _runner.Bindings, sequence,
                    _runner.Get(sequence));

                if (fault.Exists && !fault.IsWarning)
                    sequence.Enabled = false;
            }
        }

        /// <summary>Ticks in a second, at the rate the game runs at.</summary>
        const int Second = 60;

        /// <summary>A second behind the start, so the first tick looks rather than waiting.</summary>
        /// <summary>Switching off what cannot work, which is worth doing whether or not anybody looks.</summary>
        Interval _parked = new Interval(Second);

        /// <summary>Reading the words in the heading again, which nothing announces.</summary>
        Interval _worded = new Interval(Second);

        /// <summary>
        /// Puts a short question on the screen, to be answered before anything else.
        /// </summary>
        /// <remarks>
        /// For a question that is small enough to hang over the screen. A choice that needs a caption,
        /// a warning and room to think is a page - see the one that asks before deleting a sequence.
        /// </remarks>
        internal void Ask(string question)
        {
            Release();
            _screen.Question = question;
        }

        /// <summary>Adds a way to answer, in the order they are offered.</summary>
        internal void Answer(string text, Action chosen)
        {
            _screen.Answers.Add(new ChoiceEntry(text, false, Register(Cmd.Answer, _screen.Answers.Count, chosen)));
        }

        /// <summary>
        /// Takes the question away without answering it.
        /// </summary>
        /// <remarks>
        /// A third outcome, and not one of the answers: somebody who dismisses "switch this on?" has
        /// said they did not mean to leave at all, which is different from saying leave it off. So
        /// whatever raised the question does not happen either.
        /// </remarks>
        public void Unask()
        {
            if (!_screen.HasQuestion)
                return;

            _screen.Question = "";
            Release();
        }

        /// <summary>
        /// Gives back the command numbers the last set of answers was using.
        /// </summary>
        /// <remarks>
        /// A command number is claimed for the life of a screen and refused twice, which is what
        /// catches a page registering the same control twice over. A question outlives none of that:
        /// it can be raised, dismissed and raised again without the screen ever being rebuilt, so its
        /// numbers have to be handed back rather than held.
        /// </remarks>
        /// <summary>
        /// Says it on every screen where the block holds a program that would not read.
        /// </summary>
        /// <remarks>
        /// <b>Every screen, not the front one.</b> What it says is that this is not what the block
        /// holds, and that is as true three pages deep as it is on arrival - somebody who walked
        /// straight into a system and started building is exactly the person it is for.
        /// <para>
        /// Two things to say, and which one depends on whether they have decided yet. Until the edit
        /// that replaces it, the block still holds the old program and this is a warning about what is
        /// in front of them. Afterwards it is the answer to "what happened to the old one".
        /// </para>
        /// <para>
        /// First in the list, because a notice below the thing it is about is a footnote.
        /// </para>
        /// </remarks>
        void Said(List<EditorComponent> components)
        {
            if (_program.Trouble.Length == 0)
            {
                if (_notice == null)
                    return;

                // Told to go rather than dropped, and kept in the list until it says it has gone: a
                // banner taken away between two frames is the jump this whole arrangement avoids.
                _notice.Leaving = true;

                if (_notice.Closed)
                {
                    _notice = null;
                    return;
                }

                components.Insert(0, _notice);
                return;
            }

            if (_notice == null)
                _notice = new NoticeComponent("", NoticeLevel.Problem);

            _notice.Leaving = false;
            _notice.Level = NoticeLevel.Problem;

            // One line, and the rest when they ask for it. The line says what is wrong; opening it
            // says what happened and what it will cost, which is what somebody needs to decide.
            _notice.Text = "This block holds a program Reactor cannot read.";

            _notice.Detail = "It was written by a different version of Reactor, or it is damaged. "
                + "None of it could be used, which is why this screen is empty. The block still holds "
                + "it until you change something here - and that replaces it for good.";

            // The reader's own words, kept because somebody has to be able to say what happened to
            // whoever mends it, and because a person who can read it is better off than one who cannot.
            _notice.Diagnostic = "Reactor read it as: " + _program.Trouble;

            _notice.Expanded = _open;
            _notice.ExpandCommand = Register(Cmd.Expand, delegate { _open = !_open; Invalidate(); });

            // The mending, on the thing that reports the fault. Somebody who has opened the banner and
            // read what it costs should not then have to go and find an edit to make: the press that
            // says "yes, replace it" is the same decision the question asks for, and this is the same
            // answer given a step earlier.
            // Replaced rather than reached into. This banner is deliberately kept between builds, and
            // the command it carries is issued again on every one of them - so writing the id into the
            // entry that is already there would change what the button does while saying nothing, and
            // the view showing it is told rather than asked.
            _mending.Clear();
            _mending.Add(new ChoiceEntry("Replace it", false, Register(Cmd.Resolve, Replace)));
            _notice.Actions.Reset(_mending);

            components.Insert(0, _notice);
        }

        /// <summary>
        /// The one banner, kept between builds.
        /// </summary>
        /// <remarks>
        /// Held rather than made on every build, because a fresh one every tick is a fresh view every
        /// tick: the slide would start over before a frame of it had been seen, and the openness it was
        /// part way through would be thrown away with it.
        /// </remarks>
        NoticeComponent _notice;

        /// <summary>Held so that saying what mends a fault allocates nothing on a build.</summary>
        readonly List<ChoiceEntry> _mending = new List<ChoiceEntry>();

        /// <summary>Whether the banner is open, which is the person's choice and outlives a rebuild.</summary>
        bool _open;

        /// <summary>Whether somebody has been told what their first edit would cost, and said go on.</summary>
        bool _told;

        /// <summary>
        /// Whether this command would be the edit that replaces a program Reactor could not read.
        /// </summary>
        /// <remarks>
        /// Only ever true on a block whose program failed to load, and only until it is answered once.
        /// A failed load leaves what the block holds untouched, so the first edit is the moment that
        /// stops being reversible, and it is the moment to say so.
        /// </remarks>
        bool Replacing(int command)
        {
            return !_program.Intact && !_told && !_screen.HasQuestion && Changes(CommandId.Kind(command));
        }

        /// <summary>
        /// Whether a command changes the program rather than where somebody is looking.
        /// </summary>
        /// <remarks>
        /// <b>Anything not listed here changes something.</b> The list is the navigating half because
        /// that is the half that can be enumerated safely: a command added later and forgotten about is
        /// then treated as an edit, which costs one question, where the other default would cost
        /// somebody the only copy of what their block held.
        /// <para>
        /// Picking is navigation. Choosing a block or narrowing a list changes what the picker is
        /// showing and nothing in the program; the trip ends at <see cref="Cmd.Primary" />, which is
        /// where the edit is, and that is not listed.
        /// </para>
        /// </remarks>
        static bool Changes(Cmd kind)
        {
            switch (kind)
            {
                case Cmd.None:
                case Cmd.Back:
                case Cmd.Answer:
                case Cmd.Refresh:
                case Cmd.Expand:
                case Cmd.Resolve:
                case Cmd.Choice:
                case Cmd.OpenSystem:
                case Cmd.OpenSequence:
                case Cmd.StartWhen:
                case Cmd.StartEvery:
                case Cmd.AddAction:
                case Cmd.AddWait:
                case Cmd.EditCadence:
                case Cmd.TriggerSlot:
                case Cmd.StepSlot:
                case Cmd.PickEntity:
                case Cmd.PickChoice:
                case Cmd.NarrowEntities:
                case Cmd.NarrowChoices:
                case Cmd.AnyKind:
                case Cmd.OneKind:
                case Cmd.RequireAny:
                case Cmd.RequireAll:
                    return false;

                default:
                    return true;
            }
        }

        /// <summary>
        /// Takes the decision the banner and the question both ask for.
        /// </summary>
        /// <remarks>
        /// One place, two ways in: the question the first edit raises, and the operation on the banner.
        /// Both mean "the program on this block is not coming back", and after it every edit saves like
        /// any other - so the warning has nothing left to say and leaves.
        /// </remarks>
        void Replace()
        {
            _told = true;
            _program.Kept();
            Invalidate();
        }

        /// <summary>
        /// Says what the first edit costs, and does it only if they say so.
        /// </summary>
        /// <remarks>
        /// The screen has been saying all along that what it shows is not what the block holds. This is
        /// the other half: the press that makes it so, held until somebody has been told that the one
        /// that would not read goes with it. There is no undo for that - it is not an edit to a program,
        /// it is which program the block has.
        /// </remarks>
        void AskBeforeReplacing(Action go)
        {
            Ask("Replace the program this block holds? The one Reactor could not read will be gone.");

            // The safe answer first, so the reflex press lands on it.
            Answer("Leave it alone", delegate { });
            // Said here rather than at the save, because this press is the decision and everything
            // after it is ordinary editing. It also settles the banner: what it was warning about has
            // just stopped being true, and nothing else would have told it.
            Answer("Replace it", delegate
            {
                Replace();
                go();
            });
        }

        bool IsAnswer(int command)
        {
            foreach (ChoiceEntry answer in _screen.Answers)
            {
                if (answer.Command == command)
                    return true;
            }

            return false;
        }

        void Release()
        {
            foreach (ChoiceEntry answer in _screen.Answers)
                _actions.Remove(answer.Command);

            _screen.Answers.Clear();
        }

        /// <summary>Acts on a button, a row, or anything else that was pressed.</summary>
        public void Invoke(int command)
        {
            Action action;
            if (!_actions.TryGetValue(command, out action))
                return;

            if (Replacing(command))
            {
                AskBeforeReplacing(delegate { Invoke(command); });
                return;
            }

            // While a question stands, only its own answers are honoured. Anything else dismisses it
            // and goes no further - the press that puts a question away is not also a press on
            // whatever was behind it, and back in particular must not raise the same question again.
            if (_screen.HasQuestion && !IsAnswer(command))
            {
                Unask();
                return;
            }

            Unask();

            action();
            Settle();
        }

        /// <summary>Reports new text for a search box or any other field, unless the field refuses it.</summary>
        public void SetText(int command, string value)
        {
            Action<string> action;
            if (!_textActions.TryGetValue(command, out action) || !Accepts(command, value))
                return;

            if (Replacing(command))
            {
                AskBeforeReplacing(delegate { SetText(command, value); });
                return;
            }

            action(value ?? "");

            // What was just typed may be a name the heading is showing, and a rename that took a
            // moment to appear would read as one that had not worked.
            Current.RefreshHeading(_screen.Heading);
            Settle();
        }

        /// <summary>
        /// Whether a field would take this text, asked before it is sent.
        /// </summary>
        /// <remarks>
        /// So a target can say a name is taken while it is still being typed, instead of sending it and
        /// having nothing happen. The same check guards <see cref="SetText" />, so a target that never asks
        /// still cannot get past it. Only a field with a rule refuses anything; an empty required name is
        /// the target's to catch, through <see cref="HeadingPart.NameRequired" />.
        /// </remarks>
        public bool Accepts(int command, string value)
        {
            Func<string, bool> check;
            return !_textChecks.TryGetValue(command, out check) || check(value ?? "");
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

            if (Replacing(command))
            {
                AskBeforeReplacing(delegate { SetNumber(command, value); });
                return;
            }

            action(value);
            Settle();
        }

        /// <summary>Reports a toggle being flipped.</summary>
        public void SetToggle(int command, bool on)
        {
            Action<bool> action;
            if (!_toggleActions.TryGetValue(command, out action))
                return;

            if (Replacing(command))
            {
                AskBeforeReplacing(delegate { SetToggle(command, on); });
                return;
            }

            action(on);
            Settle();
        }

        /// <summary>
        /// What is being edited, for a target that has words to put on what core names by id.
        /// </summary>
        /// <remarks>
        /// A sentence says which blocks it is about and never what they are called, because a name is
        /// read off here and what to say about one that is gone is the target's to phrase. So the
        /// target needs this to render one.
        /// </remarks>
        public GridWorld World => _world;

        internal Automation Program => _program;

        internal AutomationRunner Runner => _runner;

        internal EditorPage Current => _stack[_stack.Count - 1];

        /// <summary>How deep the stack is, for a run of questions to return to.</summary>
        internal int Depth => _stack.Count;

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
        /// The same control gets the same id on every rebuild, by construction. An id derived from
        /// registration order would instead go stale the moment a page changed shape, and silently,
        /// because <see cref="Invoke"/> ignores what it does not recognise.
        /// <para>
        /// Registering the same identity twice is a mistake and says so, which is what stops two
        /// controls that ought to be distinguishable ending up indistinguishable.
        /// </para>
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

        /// <summary>A text command that refuses whatever <paramref name="accepts" /> says no to.</summary>
        internal int RegisterText(Cmd kind, Action<string> action, Func<string, bool> accepts)
        {
            var id = RegisterText(kind, 0, action);
            _textChecks.Add(id, accepts);
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

        /// <summary>
        /// Goes back, unless the page has something to say about it first.
        /// </summary>
        /// <remarks>
        /// Back is the one control on every screen, so a page that wants a word on the way out has
        /// nowhere else to get it. It may only ask a question, never refuse - a person pressing back
        /// must always end up somewhere other than where they were, or the control has lied to them.
        /// </remarks>
        void Leave()
        {
            if (Current.Leaving())
                Pop();
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

            // Any deliberate change may have moved what a trigger points at, and whatever arranges
            // triggers outside the program has no other way to know.
            _program.Rewire();

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
            _textChecks.Clear();
            _toggleActions.Clear();
            _numberActions.Clear();

            var page = Current;

            // Rebuilt in place rather than swapped, because a target holds this list across a refresh and
            // compares what is in it to decide whether its own controls need making again.
            _screen.Heading.Clear();
            page.Heading(_screen.Heading);
            _screen.Renamed();

            _screen.CanGoBack = _stack.Count > 1;
            _screen.BackCommand = _screen.CanGoBack ? Register(Cmd.Back, Leave) : 0;
            _screen.RefreshCommand = Register(Cmd.Refresh, Look);
            _screen.SwitchCommand = page.CanSwitch ? RegisterToggle(Cmd.Switch, page.Switch) : 0;
            _screen.PrimaryText = page.PrimaryText;
            _screen.PrimaryCommand = _screen.HasPrimary ? Register(Cmd.Primary, page.Primary) : 0;

            _gathered.Clear();
            page.Build(_gathered);
            Said(_gathered);

            // Everything, not just what the cadence is due to say: the components above were made from
            // nothing a moment ago and hold nothing yet.
            page.Resay();
            page.Refresh();

            if (!SameComponents(_gathered))
                _screen.Components.Reset(_gathered);

            _screen.Subtitle = page.Subtitle;
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
        /// Everything the host learns by looking, read again.
        /// </summary>
        /// <remarks>
        /// What a screen says about which buttons are wired is a reading, taken when the screen opened.
        /// On a server somebody else may have re-pointed a slot since, and nothing tells Reactor, so
        /// this is how a person says "look again".
        /// </remarks>
        void Look()
        {
            World.LookAgain();
            Invalidate();
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

        /// <summary>
        /// Another instance of a system. Not built yet, and says so.
        /// </summary>
        /// <remarks>
        /// It opens the same configurator loading does, because they are the same act: point a
        /// system at the blocks it should use here. Neither exists until block slots do.
        /// </remarks>
        internal void OfferAnother(AutomationSystem system)
        {
            var page = new ChoicePage(this, "Add another");
            page.EmptyText = "Not built yet. This would ask which of your blocks this one uses, "
                + "and everything it does would stay the same as the others.";
            Push(page);
        }

        /// <summary>
        /// Loading a system somebody else made. Not built yet, and says so.
        /// </summary>
        /// <remarks>
        /// Present rather than hidden because it is half of what the front door offers, and a
        /// screen that grows an option later teaches itself twice. What it needs is the
        /// configurator, which needs block slots.
        /// </remarks>
        internal void OfferLoading()
        {
            var page = new ChoicePage(this, "Load system");
            page.EmptyText = "Not built yet. A system somebody sent you would be set up here, "
                + "asking which of your blocks fill each part of it.";
            Push(page);
        }

        /// <summary>
        /// The ways a sequence can begin, offered on the system's own screen.
        /// </summary>
        /// <remarks>
        /// At the top of the system, in the same place and the same shape as the construct's own
        /// "new system", because it is the same question either way: what would you like to add here.
        /// <para>
        /// The catalogue lives here rather than on the page, so a third way to start something is one
        /// line in one place.
        /// </para>
        /// <para>
        /// Each label is the first word of the sentence it makes: "When…" becomes "When the airlock
        /// button is pressed", "Every…" becomes "Every 30 seconds". The ellipsis is the promise that
        /// the rest gets filled in.
        /// </para>
        /// </remarks>
        internal void AddStarters(StartComponent start, AutomationSystem system)
        {
            start.Add("When…",
                Register(Cmd.StartWhen, delegate { StartSequence(system, new WhenTrigger("")); }));

            start.Add("Every…", Register(Cmd.StartEvery,
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
        internal void StartSequence(AutomationSystem system, Trigger trigger)
        {
            _pendingSystem = system;

            // Where to unwind to once the questions are answered. The system's screen is
            // already underneath - the starters live on it now - so everything the questions
            // pushed is dropped and the finished sequence lands directly on it.
            var origin = _stack.Count;

            var moment = trigger as MomentTrigger;
            if (moment != null)
            {
                // Nothing picked yet: the sequence is being made, so there is nothing to resolve.
                Push(new PickPage(this, "When something happens", true,
                    new List<long>(), moment.EventId,
                    delegate(PickAnswer answer)
                    {
                        // Nothing to record: the sequence does not exist yet, so there is no
                        // history for it and nothing to take back to.
                        FinishSequence(Starter(answer), origin);
                    },
                    true));
                return;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                Push(RatePage(every, delegate { FinishSequence(every, origin); }));
        }

        /// <summary>
        /// Asks what should start a sequence that nothing starts yet.
        /// </summary>
        /// <remarks>
        /// The same two questions the starters on a system's screen ask, and then the same pages, so
        /// choosing a trigger reads identically whether the sequence is being made or mended. The
        /// difference is only where the answer goes: there it makes a sequence, here it is an edit on
        /// one that exists, which is why this one can be undone.
        /// <para>
        /// A sequence gets here by being copied, by being sent in a program somebody wrote by hand, or
        /// by having had its trigger's block ground off. Before this there was no way back from any of
        /// them: the line said what was missing and nothing on the screen could fill it in.
        /// </para>
        /// </remarks>
        internal void AskWhatStarts(Sequence sequence)
        {
            // Where to come back to. Both branches push at least one more page, and the answer lands
            // on the sequence rather than on the question that was just answered.
            var origin = _stack.Count;

            var page = new ChoicePage(this, "What starts this?");

            page.Add("When something happens", false, delegate
            {
                Push(new PickPage(this, "When something happens", true,
                    new List<long>(), "",
                    delegate(PickAnswer answer)
                    {
                        UnwindTo(origin);
                        Do(sequence, new GiveTrigger(Starter(answer)));
                    },
                    true));
            });

            page.Add("Every so often", false, delegate
            {
                var every = new EveryTrigger(Cadences.Default);
                Push(RatePage(every, delegate
                {
                    UnwindTo(origin);
                    Do(sequence, new GiveTrigger(every));
                }));
            });

            Push(page);
        }

        /// <summary>
        /// What somebody has just described, as the trigger that makes it happen.
        /// </summary>
        /// <remarks>
        /// The one place the two arrangements are told apart, and nobody is asked: picking a button
        /// means the construct already announces it and Reactor wires a slot, picking a door means
        /// Reactor watches for it. See <see cref="Vocabulary.Starter" />.
        /// </remarks>
        internal MomentTrigger Starter(PickAnswer answer)
        {
            MomentTrigger made = Vocabulary.Starter(
                Vocabulary.EventsOf(_world, answer.Targets), answer.Chosen, answer.All);

            for (var i = 0; i < answer.Targets.Count; i++)
            {
                GridEntity entity = _world.FindEntity(answer.Targets[i]);
                if (entity != null)
                    made.Sources.Add(_program.Declare(_world, entity).Name);
            }

            return made;
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

            // Parked until somebody says otherwise. Enabled by default, a sequence with a trigger and
            // no steps would already be live, and would get progressively more so as each step was
            // added - a system half-doing something nobody has finished describing. It is also the only
            // way the switch can gate editing without a new sequence being unbuildable the moment it
            // exists.
            var sequence = _program.AddSequence(system, trigger);
            sequence.Enabled = false;
            sequence.Name = Name(sequence);

            Push(new SequencePage(this, sequence));
        }

        /// <summary>
        /// Another sequence saying what this one says, opened for whatever is to be different.
        /// </summary>
        /// <remarks>
        /// Named like any other new sequence rather than after the one it came from: a name is what
        /// makes a sequence callable, so a copied name would either collide or quietly take over what
        /// called the original.
        /// </remarks>
        internal void Duplicate(Sequence sequence)
        {
            var copy = _program.Duplicate(sequence);
            copy.Name = Name(copy);

            Push(new SequencePage(this, copy));
        }

        /// <summary>
        /// Names a new sequence the way the game names a new block.
        /// </summary>
        /// <remarks>
        /// The first free number in the system rather than a running count, so deleting the second of
        /// three and adding another gives "Sequence 2" back.
        /// <para>
        /// Stored from here on, and never re-derived, so renaming one cannot renumber another.
        /// </para>
        /// </remarks>
        string Name(Sequence sequence)
        {
            AutomationSystem owner = null;
            for (var i = 0; i < _program.Systems.Count && owner == null; i++)
            {
                var sequences = _program.Systems[i].Sequences;
                for (var q = 0; q < sequences.Count; q++)
                {
                    if (sequences[q] != sequence)
                        continue;

                    owner = _program.Systems[i];
                    break;
                }
            }

            if (owner == null)
                return SequenceText.DefaultName(1);

            for (var number = 1; ; number++)
            {
                var wanted = SequenceText.DefaultName(number);

                var taken = false;
                for (var i = 0; i < owner.Sequences.Count && !taken; i++)
                    taken = owner.Sequences[i] != sequence && owner.Sequences[i].Name == wanted;

                if (!taken)
                    return wanted;
            }
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
