using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Developing;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Words;

namespace Sprocket.Host.SpaceEngineers.Editing
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
    public sealed class SprocketEditor
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
        readonly List<ChoiceEntry> _actionBar = new List<ChoiceEntry>();

        bool _structureDirty;
        bool _developerMode;

        public SprocketEditor(GridWorld world, Automation program, AutomationRunner runner)
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
        /// <para>
        /// <b>Held to the same rule as any other name, because that is what it becomes.</b> It is written
        /// into a save as a namespace and read back out of one, so the length and the line it has to fit
        /// on are the same length and the same line. What a host hands over is its own business.
        /// </para>
        /// </remarks>
        public string Author
        {
            get { return _author; }
            set { _author = Naming.Clean(value); }
        }

        /// <summary>
        /// Whether to offer tools for working on Sprocket itself.
        /// </summary>
        /// <remarks>
        /// Set by the host, which is the only thing that knows where a player writes their settings
        /// down. Off is the answer for everybody who is here to play rather than to build this.
        /// <para>
        /// <b>Rebuilds what is on screen.</b> Pages decide what to offer from this while they are being
        /// built, and the first one is built before a host has had a chance to say - so a flag that
        /// merely changed a field would be read as no by the only screen that ever asks.
        /// </para>
        /// </remarks>
        public bool DeveloperMode
        {
            get { return _developerMode; }
            set
            {
                if (_developerMode == value)
                    return;

                _developerMode = value;
                Invalidate();
            }
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

            Walking();
        }

        /// <summary>The screen's own arrows, in the words of whatever they would take back.</summary>
        /// <remarks>
        /// Read every tick rather than only when the screen is rebuilt, because an edit made from a
        /// row changes what there is to walk back without changing the screen's shape.
        /// </remarks>
        void Walking()
        {
            var page = Current;

            var canUndo = page.CanUndo;
            _screen.UndoCommand = canUndo ? _undoCommand : 0;
            _screen.UndoText = canUndo ? page.UndoText : "";

            var canRedo = page.CanRedo;
            _screen.RedoCommand = canRedo ? _redoCommand : 0;
            _screen.RedoText = canRedo ? page.RedoText : "";
        }

        int _undoCommand;
        int _redoCommand;

        /// <summary>
        /// Switches off anything that cannot work.
        /// </summary>
        /// <remarks>
        /// Sprocket switching off somebody's automation, which it does nowhere else, and the reason it
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
        /// somebody's decision every time - Sprocket starting a machine on its own judgement is the one
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

                if (fault.Parks)
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
            _screen.QuestionIsHere = false;
            _screen.Default = 0;
            _screen.Finishing = 0;
            _screen.Asked++;
        }

        /// <summary>
        /// The same, for a question that belongs to the control somebody just pressed.
        /// </summary>
        /// <remarks>
        /// A menu rather than a dialog: it hangs under what was pressed, because the answers are
        /// about that thing and the row it is on has to stay in sight. A question about the screen -
        /// whether to switch a sequence on, whether to replace a program - belongs to no control
        /// and goes in the middle, which is <see cref="Ask" />.
        /// </remarks>
        internal void AskHere(string question)
        {
            Release();
            _screen.Question = question;
            _screen.QuestionIsHere = true;
            _screen.Default = 0;
            _screen.Finishing = 0;
            _screen.Asked++;
        }

        /// <summary>Adds a way to answer, in the order they are offered.</summary>
        internal void Answer(string text, Action chosen)
        {
            Answer(text, chosen, false);
        }

        /// <summary>
        /// An answer that means "finish this page", the way its own button would.
        /// </summary>
        /// <remarks>
        /// Not just an answer that happens to call <c>Primary</c>. Finishing can need a question of
        /// its own - values an action will not work without - and that one is a target's to open, so
        /// the answer has to be pointed at the same place the button is rather than at half of it.
        /// </remarks>
        internal void AnswerPrimary(string text, bool byDefault)
        {
            Answer(text, delegate { Current.Primary(); }, byDefault);
            _screen.Finishing = _screen.Answers[_screen.Answers.Count - 1].Command;
        }

        /// <summary>
        /// The same, saying whether this is the one Enter takes.
        /// </summary>
        /// <remarks>
        /// Said here rather than inferred from the order, because the order is what reads best and
        /// the default is what is safe, and those are not the same question.
        /// </remarks>
        internal void Answer(string text, Action chosen, bool byDefault)
        {
            _answering.Add(new Answering(text, chosen));

            int command = Register(Cmd.Answer, _screen.Answers.Count, chosen);
            _screen.Answers.Add(new ChoiceEntry(text, false, command));

            if (byDefault)
                _screen.Default = command;
        }

        /// <summary>
        /// What the standing question's answers do, apart from the numbers they are offered under.
        /// </summary>
        /// <remarks>
        /// A question can be raised by something that changed the screen - "what should this do" is
        /// asked with the picker already unwound - and a rebuild hands back every command number the
        /// screen was using. Without this the answers would still be on screen, pointing at numbers
        /// nothing knows any more, which is a press that silently does nothing.
        /// </remarks>
        readonly List<Answering> _answering = new List<Answering>();

        struct Answering
        {
            public readonly string Text;
            public readonly Action Chosen;

            public Answering(string text, Action chosen)
            {
                Text = text;
                Chosen = chosen;
            }
        }

        /// <summary>Offers the standing question's answers again, under this screen's numbers.</summary>
        void Reask()
        {
            if (!_screen.HasQuestion || _answering.Count == 0)
                return;

            _screen.Answers.Clear();

            for (var i = 0; i < _answering.Count; i++)
            {
                _screen.Answers.Add(new ChoiceEntry(_answering[i].Text, false,
                    Register(Cmd.Answer, i, _answering[i].Chosen)));
            }
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

        void Released()
        {
            _answering.Clear();
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
            // says what happened and what it will cost, which is what somebody needs to decide. Two
            // faults, because what is on the screen differs: nothing of a program from another
            // version, and whatever did read of a damaged one.
            if (_program.Incompatible)
            {
                _notice.Text = "This block holds a program from a different version of Sprocket.";

                _notice.Detail = "This version cannot read it, so this screen starts empty. The block "
                    + "still holds it until you change something here - and that replaces it for good.";
            }
            else
            {
                _notice.Text = "This block holds a program Sprocket could only partly read.";

                _notice.Detail = "What could be read is shown here. The block still holds the whole of "
                    + "it until you change something here - and that replaces it with what you see.";
            }

            // The reader's own words, kept because somebody has to be able to say what happened to
            // whoever mends it, and because a person who can read it is better off than one who cannot.
            _notice.Diagnostic = "Sprocket read it as: " + _program.Trouble;

            _notice.Expanded = _open;
            _notice.ExpandCommand = Register(Cmd.Expand, delegate { _open = !_open; Invalidate(); });

            // The mending, on the thing that reports the fault, so somebody who has read what it costs
            // does not have to go and find an edit to make. It asks the same question the first edit
            // does, because it throws away the same program.
            // Replaced rather than reached into. This banner is deliberately kept between builds, and
            // the command it carries is issued again on every one of them - so writing the id into the
            // entry that is already there would change what the button does while saying nothing, and
            // the view showing it is told rather than asked.
            _mending.Clear();
            _mending.Add(new ChoiceEntry("Replace it", false,
                Register(Cmd.Resolve, delegate { AskBeforeReplacing(delegate { }); })));
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
        /// Whether this command would be the edit that replaces a program Sprocket could not read.
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
                case Cmd.OpenTrigger:
                case Cmd.LoadSystem:
                case Cmd.ShowScript:
                case Cmd.RunNow:
                case Cmd.StopNow:
                case Cmd.TriggerTarget:
                case Cmd.ClearTarget:
                case Cmd.StartWhen:
                case Cmd.StartEvery:
                case Cmd.AddAction:
                case Cmd.AddWait:
                case Cmd.StepSlot:
                case Cmd.PickEntity:
                case Cmd.PickChoice:
                case Cmd.NarrowEntities:
                case Cmd.NarrowChoices:
                case Cmd.NarrowKinds:
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
            Ask("Replace the program this block holds? The one Sprocket could not read will be gone.");

            // The safe answer first, so the reflex press lands on it, and the one Enter takes for
            // the same reason: a key pressed without reading must not throw a program away.
            Answer("Leave it alone", delegate { }, true);
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
            Released();
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

        /// <summary>What an action's values ask for, one per value, asked of the blocks it acts on.</summary>
        internal List<Field> Fields(IList<long> entityIds, string actionId)
        {
            return Vocabulary.Fields(_world, entityIds, actionId);
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
        /// <summary>
        /// The kind of block a picker is narrowed to, kept per place a picker opens from.
        /// </summary>
        /// <remarks>
        /// Somebody wiring a row of lights opens the picker once per step, and a filter that reset
        /// itself every time would be set again at every one of them. Held here because a page is
        /// built afresh each time one opens and this has to outlive that.
        /// <para>
        /// <b>One per place and not one in total.</b> What starts a sequence and what it does are
        /// different questions asked of different blocks - a button and a sensor start things, and
        /// lights and doors are what gets done to - so a filter set while answering one has no
        /// business narrowing the other.
        /// </para>
        /// <para>
        /// Not saved with the program. It is how somebody is working right now rather than part of
        /// what they are building, and a world reopened a week later should not still be hiding
        /// blocks for a reason nobody remembers.
        /// </para>
        /// </remarks>
        public string KindFor(bool forEvents)
        {
            return forEvents ? _whenKind : _doingKind;
        }

        public void RememberKind(bool forEvents, string kind)
        {
            if (forEvents)
                _whenKind = kind ?? "";
            else
                _doingKind = kind ?? "";
        }

        string _whenKind = "";
        string _doingKind = "";

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
            return RegisterText(kind, which, 0, action);
        }

        /// <summary>The same, for one part of a numbered thing - one slot of a sentence.</summary>
        internal int RegisterText(Cmd kind, int which, int part, Action<string> action)
        {
            var id = CommandId.Of(kind, which, part);
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
        /// nowhere else to get it. A page that answers false stays where it is, and whatever it put
        /// on screen instead is then responsible for leaving - the question raised on the way out of
        /// a sequence pops from its own answers, and the one on the way out of a picker does the
        /// same.
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

        /// <summary>What has been done to each sequence, so it can be taken back.</summary>
        readonly History<Sequence> _sequenceHistory = new History<Sequence>();

        /// <summary>
        /// And one for its system, because a trigger is not a sequence's to hold.
        /// </summary>
        /// <remarks>
        /// Two stacks rather than one, so undo takes back the last thing done to the thing being
        /// looked at. Somebody on a system's screen who has just repointed a button did not do the
        /// last thing to any sequence in it, and a sequence's arrows offering to take that back
        /// would be a control acting on something not on the screen.
        /// </remarks>
        readonly History<AutomationSystem> _systemHistory = new History<AutomationSystem>();

        /// <summary>Makes a change to a sequence and remembers how to take it back.</summary>
        /// <remarks>The only way anything changes a sequence.</remarks>
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
            if (!_sequenceHistory.Did(sequence, edit, coalesce))
                return;

            Settled(edit);
        }

        /// <summary>Makes a change to a system and remembers how to take it back.</summary>
        /// <remarks>The only way anything changes a trigger.</remarks>
        internal void Do(AutomationSystem system, SystemEdit edit)
        {
            if (!_systemHistory.Did(system, edit, false))
                return;

            Settled(edit);
        }

        /// <summary>
        /// What every edit costs whatever level it was at.
        /// </summary>
        /// <remarks>
        /// Rewiring because any deliberate change may have moved what a trigger points at, and
        /// whatever arranges triggers outside the program has no other way to know. Undoing one
        /// pays the same, which it did not before triggers could be walked back: a slot left
        /// pointing at the arrangement that was taken away is a button that does nothing.
        /// </remarks>
        void Settled<T>(Edit<T> edit)
        {
            _program.Rewire();

            if (edit.ChangesShape)
                Invalidate();
        }

        internal bool CanUndo(Sequence sequence)
        {
            return _sequenceHistory.CanUndo(sequence);
        }

        internal string UndoLabel(Sequence sequence)
        {
            return _sequenceHistory.UndoLabel(sequence);
        }

        internal void Undo(Sequence sequence)
        {
            Walked(_sequenceHistory.Undo(sequence));
        }

        internal bool CanRedo(Sequence sequence)
        {
            return _sequenceHistory.CanRedo(sequence);
        }

        internal string RedoLabel(Sequence sequence)
        {
            return _sequenceHistory.RedoLabel(sequence);
        }

        internal void Redo(Sequence sequence)
        {
            Walked(_sequenceHistory.Redo(sequence));
        }

        internal bool CanUndo(AutomationSystem system)
        {
            return _systemHistory.CanUndo(system);
        }

        internal string UndoLabel(AutomationSystem system)
        {
            return _systemHistory.UndoLabel(system);
        }

        internal void Undo(AutomationSystem system)
        {
            Walked(_systemHistory.Undo(system));
        }

        internal bool CanRedo(AutomationSystem system)
        {
            return _systemHistory.CanRedo(system);
        }

        internal string RedoLabel(AutomationSystem system)
        {
            return _systemHistory.RedoLabel(system);
        }

        internal void Redo(AutomationSystem system)
        {
            Walked(_systemHistory.Redo(system));
        }

        /// <summary>Whatever a step back or forward changed, once it has changed it.</summary>
        /// <remarks>
        /// Always a rebuild, unlike making the edit in the first place: somebody walking back is
        /// looking for what they had, and a screen that only re-worded itself would leave a step
        /// they have just put back missing from the list.
        /// </remarks>
        void Walked<T>(Edit<T> edit)
        {
            if (edit == null)
                return;

            _program.Rewire();
            Invalidate();
        }

        /// <summary>
        /// Forgets a sequence: its way back, and any page showing it, so deleting one never
        /// leaves somebody staring at something that no longer exists.
        /// </summary>
        internal void ForgetSequence(Sequence sequence)
        {
            _sequenceHistory.Forget(sequence);

            for (var i = _stack.Count - 1; i > 0; i--)
            {
                var page = _stack[i] as SequencePage;
                if (page != null && page.SequenceId == sequence.Id)
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

            // Registered whether or not there is anything to walk back, because issuing a command
            // needs the numbering only a rebuild does - and what there is to walk back changes
            // without one. So this decides whether the command may exist at all, and the tick
            // decides whether it is offered.
            _screen.HasHistory = page.HasHistory;
            _undoCommand = page.HasHistory ? Register(Cmd.Undo, page.Undo) : 0;
            _redoCommand = page.HasHistory ? Register(Cmd.Redo, page.Redo) : 0;
            _screen.PrimaryText = page.PrimaryText;
            _screen.PrimaryQuestion = page.PrimaryQuestion;
            _screen.PrimaryFields.Reset(page.PrimaryFields);
            _screen.PrimaryFilled.Reset(page.PrimaryFilled);
            _screen.PrimaryCommand = !_screen.HasPrimary
                ? 0
                : _screen.PrimaryAsks
                    ? RegisterText(Cmd.Primary, page.Primary)
                    : Register(Cmd.Primary, page.Primary);

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
            Walking();
            BuildActions();
            Reask();

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
        /// On a server somebody else may have re-pointed a slot since, and nothing tells Sprocket, so
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

            // Empty for everybody who is here to play. A permanent strip of tabs along the bottom is a
            // thing to learn before you have done anything, and the ones it used to hold were only ever
            // navigation - which the screens themselves can offer, where they mean something.
            //
            // The listing is the exception that proves it: a way to look at something, reachable from
            // wherever you happen to be, belonging to the screen rather than to what is on it.
            if (DeveloperMode)
                _actionBar.Add(new ChoiceEntry(ScriptPage.Called, Current is ScriptPage,
                    Register(Cmd.ShowScript, ShowScript)));

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
        /// <summary>
        /// Opens the listing of what this construct's automation is saved as.
        /// </summary>
        /// <remarks>
        /// A developer tool. Nothing offers it unless <see cref="DeveloperMode" /> says so, and this
        /// does not check again: the check belongs where the way in is put on a screen, so there is one
        /// place to read rather than two that can disagree.
        /// </remarks>
        internal void ShowScript()
        {
            // Offered from every screen including this one, where it is already lit. Pressing it there
            // means "I am looking at it", not "give me another one on top of the first".
            if (Current is ScriptPage)
                return;

            Push(new ScriptPage(this));
        }

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
                        // history for it and nothing to take back to - which is why the trigger
                        // losing the button here loses it outright, the way the bin on its row is
                        // the way back from making one at all.
                        MomentTrigger made = Starter(answer);
                        Unless(made, null, delegate(MomentTrigger taken)
                        {
                            if (taken != null)
                                taken.Sources.Clear();

                            FinishTrigger(made, origin);
                        });
                    },
                    true));
                return;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
                Push(RatePage(every, delegate { FinishTrigger(every, origin); }));
        }

        /// <summary>
        /// A sequence made on its own, with nothing starting it yet.
        /// </summary>
        /// <remarks>
        /// The other way round from the wizard, and both are ordinary. Somebody who knows what the
        /// machine should do writes it and says what starts it afterwards; somebody who knows which
        /// button they want starts there. A sequence made this way is what the copy button makes
        /// too, which is why the way to say what starts it is already on its screen.
        /// </remarks>
        internal void NewSequence(AutomationSystem system)
        {
            var sequence = _program.AddSequence(system);
            sequence.Enabled = false;
            sequence.Name = Name(sequence);

            Invalidate();
            Push(new SequencePage(this, sequence));
        }

        /// <summary>
        /// What somebody has just described, as the trigger that makes it happen.
        /// </summary>
        /// <remarks>
        /// The one place the two arrangements are told apart, and nobody is asked: picking a button
        /// means the construct already announces it and Sprocket wires a slot, picking a door means
        /// Sprocket watches for it. See <see cref="Vocabulary.Starter" />.
        /// </remarks>
        /// <summary>
        /// Does it, unless something on the construct is already answering that moment.
        /// </summary>
        /// <remarks>
        /// A toolbar slot holds one thing. Two triggers on the same button and the same press are
        /// two claims on that one slot, which is a state the construct cannot be put into: asked to
        /// arrange both, Sprocket arranges neither, and the button is left answering nothing at all.
        /// So it is asked before it happens rather than found afterwards on a row that has quietly
        /// gone quiet - and the answer decides which trigger keeps the button.
        /// <para>
        /// Only where the construct is what announces the moment. Two sequences both watching the
        /// inner door close is an ordinary way to build and costs nothing: Sprocket does that
        /// watching, and it can watch for two.
        /// </para>
        /// <para>
        /// Its own trigger never counts, so settling on what was already there is not a clash with
        /// itself.
        /// </para>
        /// </remarks>
        void Unless(MomentTrigger made, Trigger except, Action<MomentTrigger> then)
        {
            var taken = Holding(made, except) as MomentTrigger;
            if (taken == null)
            {
                then(null);
                return;
            }

            AskHere(SequenceText.Taken(_runner.Bindings, taken));
            Answer("Leave it alone", delegate { });
            Answer("Take it over", delegate { then(taken); });
        }

        /// <summary>What already answers that moment, or null.</summary>
        Trigger Holding(MomentTrigger made, Trigger except)
        {
            if (!(made is RequiredTrigger))
                return null;

            for (var s = 0; s < _program.Systems.Count; s++)
            {
                var triggers = _program.Systems[s].Triggers;
                for (var i = 0; i < triggers.Count; i++)
                {
                    var moment = triggers[i] as RequiredTrigger;
                    if (moment == null || triggers[i] == except)
                        continue;

                    if (moment.EventId == made.EventId && Same(moment.Sources, made.Sources))
                        return moment;
                }
            }

            return null;
        }

        /// <summary>Whether two triggers name the same blocks, in whatever order they were picked.</summary>
        static bool Same(IList<string> left, IList<string> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var i = 0; i < left.Count; i++)
            {
                var found = false;
                for (var q = 0; q < right.Count && !found; q++)
                    found = left[i] == right[q];

                if (!found)
                    return false;
            }

            return true;
        }

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
        /// Asks what the trigger should do, now that it is known what starts it.
        /// </summary>
        /// <remarks>
        /// Nothing exists until one of these is answered, so backing out of the questions leaves
        /// nothing behind. The order is the teaching: a trigger does one thing, and the way to have
        /// several things happen is the first row on the list rather than a rule to explain.
        /// </remarks>
        void FinishTrigger(Trigger trigger, int origin)
        {
            var system = _pendingSystem;
            _pendingSystem = null;

            UnwindTo(origin);
            AskWhatItDoes(system, trigger, origin, true);
        }

        /// <summary>
        /// The same question, for a trigger that already exists.
        /// </summary>
        /// <remarks>
        /// One question and one set of answers, because "what should this do" is one thought whether
        /// the trigger is being made or mended - and a second list that read differently would be two
        /// things to learn about one act.
        /// <para>
        /// Hung over the screen rather than shown as a page of its own. Four short answers is not a
        /// screenful, and a whole screen for them put the row somebody was working on out of sight
        /// behind the question about it. Dismissing it is a third outcome: the trigger is left as it
        /// was, which for one being made means there is nothing to leave behind.
        /// </para>
        /// </remarks>
        void AskWhatItDoes(AutomationSystem system, Trigger trigger, int origin, bool fresh)
        {
            AskHere("What should it do?");

            Answer("Run a new sequence", delegate { RunNew(system, trigger, origin, fresh); });

            if (Standing(system))
            {
                Answer("Run an existing sequence",
                    delegate { PickSequence(system, trigger, false, origin, fresh); });

                Answer("Stop a sequence",
                    delegate { PickSequence(system, trigger, true, origin, fresh); });
            }

            Answer("Do something", delegate { DoOneThing(system, trigger, origin, fresh); });
        }

        /// <summary>
        /// Where every answer to that question lands.
        /// </summary>
        /// <remarks>
        /// A trigger being made is not yet in the system, and nothing has been done that could be
        /// taken back - the way back from making one is the bin on its row. A trigger being changed
        /// is already there, so this is an edit, and it is the system's to walk back.
        /// </remarks>
        void Points(AutomationSystem system, Trigger trigger, Sequence sequence, bool stops,
            bool fresh, Sequence made)
        {
            if (fresh)
            {
                trigger.Runs = sequence.Number;
                trigger.Stops = stops;
                system.AddTrigger(trigger);
                _program.Rewire();
                Invalidate();
                return;
            }

            Do(system, new PointTrigger(_program, trigger, sequence.Number, stops, made));
        }

        /// <summary>Whether this system has a sequence that stands on its own and can be named.</summary>
        static bool Standing(AutomationSystem system)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (sequences[i].HasName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Makes the sequence this trigger runs, and opens it.
        /// </summary>
        /// <remarks>
        /// Parked until somebody says otherwise. Enabled by default, a sequence with a trigger and no
        /// steps would already be live, and would get progressively more so as each step was added -
        /// a system half-doing something nobody has finished describing. It is also the only way the
        /// switch can gate editing without a new sequence being unbuildable the moment it exists.
        /// </remarks>
        void RunNew(AutomationSystem system, Trigger trigger, int origin, bool fresh)
        {
            // The question goes with the answer. Left on the stack, backing out of the sequence would
            // land on "what should it do?" for a trigger that already does something.
            UnwindTo(origin);

            var sequence = _program.AddSequence(system);
            sequence.Enabled = false;
            sequence.Name = Name(sequence);

            Points(system, trigger, sequence, false, fresh, sequence);

            Push(new SequencePage(this, sequence));
        }

        /// <summary>
        /// Points the trigger at a sequence that already exists, to run or to stop.
        /// </summary>
        /// <remarks>
        /// Only the ones that stand on their own. A sequence nobody named is the one thing its own
        /// trigger does and is not a thing to point a second one at - it has no name to offer, and
        /// pointing at it would be sharing something the person never made separately.
        /// </remarks>
        void PickSequence(AutomationSystem system, Trigger trigger, bool stops, int origin,
            bool fresh)
        {
            var page = new ChoicePage(this, stops ? "Stop which sequence?" : "Run which sequence?");

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (!sequences[i].HasName)
                    continue;

                var chosen = sequences[i];
                page.Add(chosen.Name, false, delegate
                {
                    UnwindTo(origin);
                    Points(system, trigger, chosen, stops, fresh, null);
                });
            }

            Push(page);
        }

        /// <summary>
        /// Asks what one thing the trigger should do, and makes it do exactly that.
        /// </summary>
        /// <remarks>
        /// What it holds is a sequence of one act that nobody named, which is what "one thing" is in
        /// the model. Where the block has an action of the game's own for it, the host writes that
        /// into the button and Sprocket is not part of the press - see <c>SprocketUnit</c>, which
        /// decides that by looking at the sequence rather than by being told.
        /// </remarks>
        void DoOneThing(AutomationSystem system, Trigger trigger, int origin, bool fresh)
        {
            Push(new PickPage(this, "Do something", false, new List<long>(), "",
                delegate(PickAnswer answer)
                {
                    var act = new Act(answer.Chosen);
                    act.Sets = answer.Sets;
                    for (var i = 0; i < answer.Targets.Count; i++)
                    {
                        GridEntity entity = _world.FindEntity(answer.Targets[i]);
                        if (entity != null)
                            act.Targets.Add(_program.Declare(_world, entity).Name);
                    }

                    var sequence = _program.AddSequence(system);
                    sequence.Steps.Add(act);
                    sequence.Steps.Add(new Await());

                    UnwindTo(origin);
                    Points(system, trigger, sequence, false, fresh, sequence);
                }));
        }

        /// <summary>
        /// Takes a trigger out of a system, and the sequence it was the only reason for.
        /// </summary>
        /// <remarks>
        /// No question asked, because the system's arrows are the answer to a deletion nobody meant -
        /// and a question in front of every one of them is how people learn to press through
        /// questions. A sequence deleted from its own screen still asks: that is work somebody can
        /// see, and its way back would be on a screen that went with it.
        /// </remarks>
        internal void Forget(AutomationSystem system, Trigger trigger)
        {
            Sequence sequence = system.FindSequence(trigger.Runs);
            var only = sequence != null && !sequence.HasName && !Started(system, sequence, trigger)
                ? sequence
                : null;

            if (only != null)
                ForgetSequence(only);

            Do(system, new ForgetTrigger(_program, trigger, only));
        }

        /// <summary>
        /// Asks what a trigger should do, which is what an empty slot is waiting to be told.
        /// </summary>
        /// <remarks>
        /// The whole question - run a new sequence, run an existing one, stop one, do something -
        /// because an empty slot is one nobody has answered yet rather than one to be changed.
        /// </remarks>
        internal void ChangeTarget(AutomationSystem system, Trigger trigger)
        {
            AskWhatItDoes(system, trigger, _stack.Count, false);
        }

        /// <summary>
        /// Empties what a trigger does, so it is waiting to be told again.
        /// </summary>
        /// <remarks>
        /// What the cross on that part of the row does, and it does exactly that: the slot goes
        /// empty, the trigger stops doing anything, and pressing the empty slot asks the whole
        /// question. A cross that opened a question instead would be a mark saying one thing and
        /// doing another.
        /// <para>
        /// A sequence nobody named goes with it, because it existed only to be the thing this
        /// trigger did - and comes back if this is walked back, which is the same machinery that
        /// carries one when a trigger is pointed somewhere else.
        /// </para>
        /// </remarks>
        internal void ClearTarget(AutomationSystem system, Trigger trigger)
        {
            Do(system, new PointTrigger(_program, trigger, 0, false, null));
        }

        /// <summary>
        /// Asks which sequence a trigger is about, keeping what it does with it.
        /// </summary>
        /// <remarks>
        /// One that stops a sequence goes on stopping the one it is pointed at next. Changing that is
        /// the whole question, which is what the cross on the slot asks.
        /// </remarks>
        internal void PointTriggerAt(AutomationSystem system, Trigger trigger)
        {
            PickSequence(system, trigger, trigger.Stops, _stack.Count, false);
        }

        /// <summary>
        /// Changes the one act a trigger does, in the picker it was chosen in.
        /// </summary>
        /// <remarks>
        /// The act belongs to a sequence nobody named - what the trigger does rather than a thing in
        /// its own right - so it has no screen to be changed on, and the way back is the system's
        /// rather than that sequence's own unreachable history.
        /// </remarks>
        internal void ChangeDoing(AutomationSystem system, Sequence sequence, Act act)
        {
            var origin = _stack.Count;
            var index = sequence.Steps.IndexOf(act);

            // Handed what the step already holds, so the question that finishes this opens on those
            // values rather than empty. Changing which blocks a step acts on is not a reason to type
            // its values again.
            Push(new PickPage(this, "Do something", false, Blocks(act.Targets), act.ActionId,
                delegate(PickAnswer answer)
                {
                    UnwindTo(origin);

                    var named = new List<string>();
                    for (var i = 0; i < answer.Targets.Count; i++)
                    {
                        GridEntity entity = _world.FindEntity(answer.Targets[i]);
                        if (entity != null)
                            named.Add(_program.Declare(_world, entity).Name);
                    }

                    Do(system, new Rewrite(sequence, new ChangeStep(index, act, named, answer.Chosen,
                        answer.Sets)));
                }, values: act.Values));
        }

        /// <summary>
        /// Asks again what starts a trigger, and puts the answer in its place.
        /// </summary>
        /// <remarks>
        /// Straight to the question it was made with rather than to the top of the wizard. A trigger
        /// that listens for a moment stays one: what somebody came to change is which block, or which
        /// moment of it. Turning a repeat into a moment is deleting it and making the one you meant.
        /// <para>
        /// Everything the trigger is for is carried over, because this question was only ever about
        /// what starts it - the sequence it runs, whether it stops that sequence instead, and
        /// whatever somebody wrote about it.
        /// </para>
        /// </remarks>
        internal void ChangeTrigger(AutomationSystem system, Trigger trigger)
        {
            // Back to the screen the row is on, which is where the answer lands.
            var origin = _stack.Count;

            var every = trigger as EveryTrigger;
            if (every != null)
            {
                // The page the wizard uses, so a rate reads the same wherever it is met. It sets the
                // interval itself, which is why the edit is handed what it was before that.
                var rate = every.IntervalTicks;
                Push(RatePage(every, delegate
                {
                    var chosen = every.IntervalTicks;
                    every.IntervalTicks = rate;

                    UnwindTo(origin);
                    Do(system, new SetCadence(every, rate, chosen));
                }));
                return;
            }

            var moment = trigger as MomentTrigger;
            if (moment == null)
                return;

            var was = moment as WhenTrigger;

            Push(new PickPage(this, "When something happens", true,
                Blocks(moment.Sources), moment.EventId,
                delegate(PickAnswer answer)
                {
                    UnwindTo(origin);

                    MomentTrigger made = Starter(answer);
                    Unless(made, moment, delegate(MomentTrigger taken)
                    {
                        var change = new ChangeTrigger(moment, made);

                        // One edit either way, because it is one answer to one question.
                        Do(system, taken == null
                            ? (SystemEdit)change
                            : new TakeOver(change, taken));
                    });
                },
                true, was != null && was.RequiresAll));
        }

        /// <summary>What these declarations point at, as blocks, for a picker that shows blocks.</summary>
        /// <remarks>
        /// A reference that points at nothing is left out rather than stood in for by a zero: the
        /// picker shows what is on the ship, and what is gone is not.
        /// </remarks>
        List<long> Blocks(IList<string> names)
        {
            var found = new List<long>();
            for (var i = 0; i < names.Count; i++)
            {
                GridEntity entity = _runner.Bindings.Entity(names[i]);
                if (entity != null)
                    found.Add(entity.Id);
            }

            return found;
        }

        /// <summary>
        /// Whether anything but that trigger still starts or stops the sequence.
        /// </summary>
        /// <remarks>
        /// Asked while the one being deleted is still in the list, because what is being decided is
        /// whether to delete it - so it cannot be allowed to answer for itself.
        /// </remarks>
        static bool Started(AutomationSystem system, Sequence sequence, Trigger apart)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (triggers[i] != apart && triggers[i].Runs == sequence.Number)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Runs a sequence now, as whatever starts it would.
        /// </summary>
        /// <remarks>
        /// What starts a sequence is usually a door or a button somewhere else on the ship, so
        /// watching one work meant going and pressing it. This is the same call a trigger makes, on
        /// the one of its system there is - which is the only one until more than one can be made.
        /// <para>
        /// A sequence already running is not started twice. The runner counts that as a skip, the
        /// same as a button pressed twice, and the row says so.
        /// </para>
        /// </remarks>
        /// <summary>
        /// Ends a run that is going, by hand.
        /// </summary>
        /// <remarks>
        /// The way out of a wait that will not end on its own: a room that will not fill leaves a
        /// sequence holding for as long as the world stays that way, and somebody watching it has
        /// to be able to call it off without switching the whole sequence off.
        /// <para>
        /// It stops the run and nothing else. What the sequence already did stands - Sprocket never
        /// moves anything back that nobody asked it to move - so an airlock stopped halfway is left
        /// where it is, which is the honest result and the one a person can see.
        /// </para>
        /// </remarks>
        internal void StopNow(Sequence sequence)
        {
            SequenceRunner state = _runner.Get(sequence, Only(sequence));
            if (state != null)
                state.Stop();
        }

        internal void RunNow(Sequence sequence)
        {
            // By hand, which is what lets a parked sequence run at all: the switch keeps triggers
            // off it, and a person at the screen is not a trigger.
            _runner.Call(sequence, Only(sequence), true);
        }

        /// <summary>The one of the system this screen is about, which is the first of them.</summary>
        static SystemInstance Only(Sequence sequence)
        {
            AutomationSystem system = sequence.System;
            return system == null || system.Instances.Count == 0 ? null : system.Instances[0];
        }

        /// <summary>
        /// Asks before throwing a sequence away, and throws it away if they say so.
        /// </summary>
        /// <remarks>
        /// The one place in the editor where a mistake cannot be walked back: every other act is
        /// live and reversible by doing the opposite, and this one is not reversible at all. So it
        /// asks, where deleting a trigger does not - a trigger is one line and three presses to make
        /// again, and a sequence is work.
        /// <para>
        /// The safe answer is listed first, so the reflex press lands on it, and it is spelt out
        /// rather than left to back. Back does mean keep - it always means return to where you came
        /// from - but a screen whose only row is the destructive one is a screen where a stray press
        /// destroys something.
        /// </para>
        /// <para>
        /// Here rather than on the sequence's own screen, because it is asked for from two places:
        /// that screen, and the row for it in the list on its system's. Whatever is showing the
        /// sequence goes when it does, which is what <see cref="ForgetSequence" /> is for.
        /// </para>
        /// </remarks>
        internal void DeleteSequence(Sequence sequence)
        {
            var page = new ChoicePage(this, "Delete this sequence?");
            page.Caption = SequenceText.Doing(_runner.Bindings, sequence);

            page.Add("Keep it", false, delegate { Pop(); });

            page.Add("Delete it", false, delegate
            {
                // The question goes first. Forgetting the sequence drops any page showing it, but
                // not this one on top of it, which would leave somebody standing on a question about
                // something that no longer exists.
                Pop();
                _program.RemoveSequence(sequence);
                ForgetSequence(sequence);
            });

            page.WarnOnLast("This cannot be undone.");

            Push(page);
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
