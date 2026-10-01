using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Developing;
using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Ui.Input;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Ui.Windows;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Values;
using Sprocket.Host.SpaceEngineers.Localization;
using VRage.Game.ModAPI;
using Run = Sprocket.Host.SpaceEngineers.Sequences.Run;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>
    /// The editor. All of it.
    /// </summary>
    /// <remarks>
    /// Navigation, page contents, slot filling and search live here rather than in the screens.
    /// What is drawn renders <see cref="Screen" /> and each page's view, and carries out the commands they hand it; it
    /// decides nothing about what the editor means or does, and nothing it draws has to be worked out twice.
    ///
    /// The wording is the one thing this does not own - see <see cref="Sentences.SequenceText" />, which
    /// decides word order, capitals and every other thing that is a language's business rather than
    /// automation's.
    /// </remarks>
    public sealed class SprocketEditor
    {
        /// <summary>The most lines a list in a question shows, the last of them counting the rest where there are more.</summary>
        const int Listed = 5;

        readonly GridWorld _world;
        readonly Automation _program;
        readonly AutomationRunner _runner;
        string _author = "";
        string _holder;
        Habits _habits = new Habits();
        readonly List<EditorPage> _stack = new List<EditorPage>();

        readonly Dictionary<int, int> _standIns = new Dictionary<int, int>();
        readonly EditorScreen _screen = new EditorScreen();
        readonly List<ChoiceEntry> _actionBar = new List<ChoiceEntry>();

        bool _structureDirty;
        bool _developerMode;

        /// <summary>An editor of <paramref name="program" />, saying what it says in <paramref name="texts" />.</summary>
        public SprocketEditor(GridWorld world, Automation program, AutomationRunner runner, Texts texts)
        {
            _world = world;
            _program = program;
            _runner = runner;
            Texts = texts;
            Ago = new Ago(texts);
            _stack.Add(new HomePage(this));
            Rebuild();
        }

        /// <summary>What to show right now.</summary>
        public EditorScreen Screen => _screen;

        /// <summary>What the editor says, in the player's language.</summary>
        public Texts Texts { get; }

        /// <summary>How long ago something happened, in <see cref="Texts" />.</summary>
        public Ago Ago { get; }

        /// <summary>Where the screen showing the editor takes the keyboard and the pointer, or null while nothing shows it.</summary>
        public InputRoot Input { get; set; }

        /// <summary>The windows open over the screen showing the editor, or null while nothing shows it.</summary>
        public WindowLayer Windows { get; set; }

        /// <summary>How many times the screen has been built from its page, which a screen standing still never adds to.</summary>
        public int Rebuilds { get; private set; }

        /// <summary>The box a blueprint's name is typed into to delete it, where one has been asked for.</summary>
        public Prompt Asking => _asking;

        /// <summary>What the person at the editor needs told above whatever page is showing, or null for nothing.</summary>
        public Notice Notice => _notice;

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
        /// <summary>
        /// Where running a sequence by hand, or stopping it, goes where it is not the runner here, or
        /// null: the sequence, the one of its system, and whether it stops.
        /// </summary>
        /// <remarks>
        /// Set on a copy a client is editing, whose runner runs nothing: the sequence runs where the
        /// Sprocket is kept, on the server.
        /// </remarks>
        public Action<Sequence, SystemInstance, bool> Elsewhere { get; set; }

        /// <summary>Where a button somebody has just picked goes to be written into its panel's slots: the panel, by entity id.</summary>
        /// <remarks>
        /// Set by the unit that writes the toolbars; on a copy a client is editing, by whoever lent it, since the toolbars are
        /// written where the Sprocket is kept.
        /// </remarks>
        public Action<long> Wire { get; set; }

        /// <summary>Who holds the Sprocket this editor shows, or null where this editor may change it.</summary>
        /// <remarks>
        /// Set on a copy lent to look at while somebody else edits it. Every command but the ones that
        /// move around the editor is refused where it is registered, so a control that would change
        /// something has no command and goes inert - the way a switched-on sequence locks itself.
        /// The screen is built again on the next tick, since what was registered before is not.
        /// </remarks>
        public string Holder
        {
            get { return _holder; }
            set
            {
                _holder = value;
                _structureDirty = true;
            }
        }

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
        /// <summary>One frame of a screen somebody is looking at.</summary>
        /// <remarks>
        /// Only while a screen is open: everything here is about what a screen shows. What runs the
        /// program, and what switches off a sequence that cannot work, is the runtime's and goes on
        /// whether or not anybody looks.
        /// </remarks>
        public void Tick(int tick)
        {
            if (_structureDirty)
            {
                Rebuild();
                return;
            }

            // Somebody is looking, so the construct is looked at for them: what a run would only find at the step that
            // needs it is shown before anything runs into it. See AutomationRunner.Look for what it costs.
            if (_looked.Due(tick))
                _runner.Look();

            Current.Refresh();

            // The heading's words are made of things that can change without anything saying so: the
            // grid's own name, and the name of a block inside a sentence. Nothing in the game's own
            // interface announces either, so they are read again on a cadence - a second, the same
            // cadence the rest of this class already keeps time by. A name changed here shows at once,
            // because renaming reads them back the moment it lands; somebody else's shows within a
            // second, which nobody can tell from at once.
            //
            // Its words only. The heading's shape was settled when the screen was made, and the target
            // has already built controls to match it.
            var retitled = Current.TakeRetitle();
            if (_worded.Due(tick) || retitled)
                Current.RefreshHeading(_screen.Heading);

            _screen.Subtitle = Current.Subtitle;
            _screen.SwitchIsOn = Current.SwitchIsOn;
            _screen.SwitchAbout = Current.SwitchAbout;
            _screen.Running = Current.IsRunning;

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
            _screen.Undo = canUndo ? _undo : null;
            _screen.UndoText = canUndo ? page.UndoText : "";

            var canRedo = page.CanRedo;
            _screen.Redo = canRedo ? _redo : null;
            _screen.RedoText = canRedo ? page.RedoText : "";

            // The button itself, its words and whether it can be pressed, because a picker comes to be finishable by a
            // choice that leaves the screen its shape.
            var primary = page.PrimaryText;
            var ready = primary.Length != 0 && page.CanPrimary;
            _screen.PrimaryText = primary;
            _screen.Primary = ready ? _primary : null;
            _screen.PrimaryAbout = primary.Length == 0 ? "" : ready ? page.PrimaryAbout : page.PrimaryWaits;
        }

        ICommand _undo;
        ICommand _redo;
        ICommand _primary;

        const int Second = 60;

        Interval _worded = new Interval(Second);
        Interval _looked = new Interval(Second);

        /// <summary>
        /// Puts a short question on the screen, to be answered before anything else.
        /// </summary>
        /// <remarks>
        /// For a question that is small enough to hang over the screen.
        /// </remarks>
        internal void Ask(string question)
        {
            _screen.Ask(question, false);
        }

        /// <summary>The same, <paramref name="question" /> a template whose named slots take their words from <paramref name="arguments" />.</summary>
        internal void Ask(string question, Arguments arguments)
        {
            _screen.Ask(question, arguments, false);
        }

        /// <summary>
        /// The same, for a question that belongs to the control somebody just pressed.
        /// </summary>
        /// <remarks>
        /// A menu rather than a dialog: it hangs under what was pressed, because the answers are
        /// about that thing and the row it is on has to stay in sight. A question about the screen -
        /// whether to switch a sequence on, whether to replace a program - belongs to no control
        /// and goes in the middle, which is <see cref="Ask(string)" />.
        /// </remarks>
        internal void AskHere(string question)
        {
            _screen.Ask(question, true);
        }

        /// <summary>The same, <paramref name="question" /> a template whose named slots take their words from <paramref name="arguments" />.</summary>
        internal void AskHere(string question, Arguments arguments)
        {
            _screen.Ask(question, arguments, true);
        }

        /// <summary>Adds a way to answer, in the order they are offered.</summary>
        /// <remarks>The answer that does what was asked about comes first and the one that does not second, as Yes and No stand in the game's own message boxes.</remarks>
        internal void Answer(string text, Action chosen)
        {
            Answer(text, chosen, false);
        }

        /// <summary>An answer that finishes the page on top, the way its own way on does.</summary>
        internal void AnswerPrimary(string text, bool byDefault)
        {
            Answer(text, delegate { Current.Primary(); }, byDefault);
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
            var command = Refuses(Cmd.Answer) ? null : new Relay(delegate { Reply(chosen); });
            _screen.Answer(new ChoiceEntry(text, false, command), byDefault);
        }

        /// <summary>Carries out an answer to the standing question, which takes the question away first.</summary>
        void Reply(Action chosen)
        {
            Unask();
            chosen();
            Settle();
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

            _screen.Unask();
        }


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
        /// Above the page, because a notice below the thing it is about is a footnote.
        /// </para>
        /// </remarks>
        void Said()
        {
            if (_holder != null && _program.Trouble.Length == 0)
            {
                Held();
                return;
            }

            if (_program.Trouble.Length == 0)
            {
                if (_notice == null)
                    return;

                // Told to go rather than dropped, and kept until it says it has gone: a banner taken away
                // between two frames is the jump this whole arrangement avoids.
                _notice.Leaving = true;

                if (_notice.Closed)
                    _notice = null;

                return;
            }

            if (_notice == null)
                _notice = new Notice();

            _notice.Leaving = false;
            _notice.Level = NoticeLevel.Problem;

            // One line, and the rest when they ask for it. The line says what is wrong; opening it
            // says what happened and what it will cost, which is what somebody needs to decide. Two
            // faults, because what somebody can do differs: a program from another version, and one
            // with an error in its text, which can be mended there.
            if (_program.Incompatible)
            {
                _notice.Text = Texts.SalvageDifferent;
                _notice.Detail = Texts.SalvageDifferentDetail;
            }
            else
            {
                _notice.Text = Texts.SalvageBroken;
                _notice.Detail = Texts.SalvageBrokenDetail;
            }

            // The reader's own words, kept because somebody has to be able to say what happened to
            // whoever mends it, and because a person who can read it is better off than one who cannot.
            _notice.Diagnostic = Template.Fill(Texts.SalvageTrouble, "trouble", _program.Trouble);

            _notice.Expanded = _open;
            _notice.Expands = Expands;

            // The mending, on the thing that reports the fault, so somebody who has read what it costs
            // does not have to go and find an edit to make. A text with an error in it is mended in the
            // text; one from another version can only be replaced, which asks the same question the
            // first edit does, because it throws away the same program.
            if (_program.Incompatible)
            {
                _notice.OfferText = Texts.SalvageReplace;
                _notice.Offers = Resolves;
            }
            else
            {
                _notice.OfferText = Texts.SalvageMend;
                _notice.Offers = Mends;
            }
        }

        /// <summary>
        /// Says, at the head of every page, who is editing the Sprocket this editor only shows.
        /// </summary>
        /// <remarks>
        /// For as long as the editor is open, so it never comes or goes while somebody is looking. Not
        /// a problem: nothing is wrong, the Sprocket is simply somebody else's for now.
        /// </remarks>
        void Held()
        {
            if (_notice == null)
                _notice = new Notice();

            _notice.Leaving = false;
            _notice.Level = NoticeLevel.Info;
            _notice.Text = Template.Fill(Texts.LeaseHolder, "holder", _holder);
            _notice.Detail = Texts.LeaseDetail;
            _notice.Diagnostic = "";
            _notice.Expanded = _open;
            _notice.Expands = Expands;
            _notice.OfferText = "";
            _notice.Offers = null;
        }

        Notice _notice;

        ICommand _expands;
        ICommand _resolves;
        ICommand _mends;

        ICommand Expands => _expands ?? (_expands = Act(Cmd.Expand, delegate
        {
            _open = !_open;
            if (_notice != null)
                _notice.Expanded = _open;
        }));

        ICommand Resolves => _resolves ?? (_resolves = Act(Cmd.Resolve, delegate { AskBeforeReplacing(delegate { }); }));

        ICommand Mends => _mends ?? (_mends = Act(Cmd.ShowScript, ShowScript));

        bool _open;

        bool _told;

        Prompt _asking;

        /// <summary>
        /// Whether a press of <paramref name="kind" /> would be the edit that replaces a program Sprocket could not read.
        /// </summary>
        /// <remarks>
        /// Only ever true on a block whose program failed to load, and only until it is answered once.
        /// A failed load leaves what the block holds untouched, so the first edit is the moment that
        /// stops being reversible, and it is the moment to say so.
        /// </remarks>
        bool Replacing(Cmd kind)
        {
            return !_program.Intact && !_told && !_screen.HasQuestion && Changes(kind);
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
                case Cmd.Mend:
                case Cmd.Choice:
                case Cmd.OpenSystem:
                case Cmd.OpenSequence:
                case Cmd.OpenTrigger:
                case Cmd.LoadSystem:
                case Cmd.ShowScript:
                case Cmd.RunNow:
                case Cmd.StopNow:
                case Cmd.TriggerTarget:
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
            Ask(Texts.SalvageQuestion);

            // Said here rather than at the save, because this press is the decision and everything
            // after it is ordinary editing. It also settles the banner: what it was warning about has
            // just stopped being true, and nothing else would have told it.
            Answer(Texts.SalvageReplace, delegate
            {
                Replace();
                go();
            });

            // The one Enter takes: a key pressed without reading must not throw a program away.
            Answer(Texts.SalvageKeep, delegate { }, true);
        }

        /// <summary>A command that carries <paramref name="action" /> out as a press of <paramref name="kind" />, or null where the editor refuses that kind of press.</summary>
        /// <remarks>
        /// The first edit of a program that could not be read asks first, a press while a question stands only puts it
        /// away, and the editor settles after.
        /// </remarks>
        public ICommand Act(Cmd kind, Action action)
        {
            return Refuses(kind) ? null : new Relay(delegate { Carry(kind, action); });
        }

        void Carry(Cmd kind, Action action)
        {
            if (Replacing(kind))
            {
                AskBeforeReplacing(delegate { Carry(kind, action); });
                return;
            }

            if (_screen.HasQuestion)
            {
                Unask();
                return;
            }

            action();
            Settle();
        }

        /// <summary>What an action's values ask for, one per value, asked of the blocks it acts on.</summary>
        internal List<Field> Fields(IList<long> entityIds, string actionId, Form form)
        {
            return Vocabulary.Fields(_world, entityIds, actionId, form);
        }

        /// <summary>
        /// What gives a name through <paramref name="rename" />, where <paramref name="accepts" /> takes it, or null where
        /// the editor refuses renaming.
        /// </summary>
        /// <remarks>
        /// The heading's words are read back the moment a name lands, because a rename that took a moment to appear would
        /// read as one that had not worked. A name refused is dropped here too, so a target that never asked still cannot
        /// get past it.
        /// </remarks>
        public Action<string> Renaming(Action<string> rename, Func<string, bool> accepts)
        {
            if (Refuses(Cmd.Rename))
                return null;

            Action<string> gives = null;
            gives = delegate(string name)
            {
                name = name ?? "";
                if (accepts != null && !accepts(name))
                    return;

                if (Replacing(Cmd.Rename))
                {
                    AskBeforeReplacing(delegate { gives(name); });
                    return;
                }

                rename(name);
                Current.RefreshHeading(_screen.Heading);
                Settle();
            };

            return gives;
        }

        /// <summary>How somebody is working in this editor; see <see cref="Shell.Habits" />.</summary>
        /// <remarks>Replaced on a client's copy by the ones its earlier copies of the same block used.</remarks>
        public Habits Habits
        {
            get { return _habits; }
            set { _habits = value ?? new Habits(); }
        }

        /// <summary>Whether the list of what to do shows the block's own vocabulary rather than Sprocket's.</summary>
        /// <remarks>
        /// Held here rather than on the picker so that somebody who works in the block's own words
        /// keeps working in them, rather than being put back a step at every sequence.
        /// </remarks>
        public bool ShowsTerminal
        {
            get { return Habits.ShowsTerminal; }
            set { Habits.ShowsTerminal = value; }
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

        /// <summary>Whether a command is refused because somebody else holds the Sprocket.</summary>
        /// <remarks>Moving around is all a copy lent to look at may do.</remarks>
        bool Refuses(Cmd kind)
        {
            if (Holder == null)
                return false;

            switch (kind)
            {
                case Cmd.Back:
                case Cmd.Refresh:
                case Cmd.Expand:
                case Cmd.OpenSystem:
                case Cmd.OpenSequence:
                case Cmd.PageOn:
                case Cmd.PageBack:
                case Cmd.ShowScript:
                    return false;
                default:
                    return true;
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

        /// <summary>Goes back a page, where the page lets it go, taking back what it was opened on where that was made and left untouched.</summary>
        /// <remarks>
        /// A page whose <see cref="EditorPage.Leaving" /> answers false stays where it is, and whatever it put on screen instead
        /// is then responsible for leaving: the question raised on the way out of a sequence pops from its own answers, and the
        /// one on the way out of a picker does the same. Pressing to add something and backing straight out adds nothing.
        /// </remarks>
        public void Leave()
        {
            var page = Current;
            if (!page.Leaving())
                return;

            Pop();
            if (page.IsMade && page.Untouched)
                page.Unmake();
        }

        /// <summary>The shape of the screen has changed and must be rebuilt.</summary>
        public void Invalidate()
        {
            _structureDirty = true;
        }

        readonly History<Sequence> _sequenceHistory = new History<Sequence>();

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
            _program.Edited();
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
            return ChangeText.Of(Texts, _sequenceHistory.Undoes(sequence));
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
            return ChangeText.Of(Texts, _sequenceHistory.Redoes(sequence));
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
            return ChangeText.Of(Texts, _systemHistory.Undoes(system));
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
            return ChangeText.Of(Texts, _systemHistory.Redoes(system));
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

            _program.Edited();
            _program.Rewire();
            Invalidate();
        }

        /// <summary>Asks <paramref name="question" /> about <paramref name="named" />, with <paramref name="warning" /> under it, and deletes only on the answer that says so.</summary>
        /// <remarks>
        /// Keeping is the first answer and the one Enter takes, so the press made without reading keeps what is there. The
        /// warning's own slots take their words from <paramref name="arguments" />, and it has none where that is null.
        /// </remarks>
        void Deleting(string question, string named, string warning, Arguments arguments, Action delete)
        {
            arguments = arguments ?? new Arguments();
            arguments.Set("named", named ?? "");

            Ask(question + "\n\n**{named}**\n\n" + warning, arguments);
            Answer(Texts.DeleteKeep, delegate { }, true);
            Answer(Texts.DeleteConfirm, delete);
        }

        /// <summary>
        /// A bulleted list of <paramref name="count" /> names for a question, each in its own slot of
        /// <paramref name="arguments" /> so it is said as it was written.
        /// </summary>
        /// <remarks>
        /// A construct can hold any number of anything, so past <see cref="Listed" /> lines the last one counts the
        /// rest, and only the names shown are asked of <paramref name="name" />.
        /// </remarks>
        string Bullets(int count, Func<int, string> name, Arguments arguments)
        {
            var shown = count <= Listed ? count : Listed - 1;
            var list = new System.Text.StringBuilder();

            for (var i = 0; i < shown; i++)
            {
                var slot = "item" + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                arguments.Set(slot, name(i));
                list.Append("\n- {").Append(slot).Append('}');
            }

            if (shown < count)
            {
                arguments.Set("more", Template.Fill(Texts.DialogMore, "count",
                    (count - shown).ToString(System.Globalization.CultureInfo.InvariantCulture)));
                list.Append("\n- {more}");
            }

            return list.ToString();
        }

        /// <summary>
        /// Puts the program <paramref name="text" /> says in place of the whole of this one, keeping what was switched off, and
        /// answers empty; or answers what is wrong with the text, and changes nothing.
        /// </summary>
        /// <remarks>
        /// Read into a program of its own first, because a text with an error in it is trusted in no part. There is no way
        /// back: it is which program the block has, not an edit to it. Runs under way end, as they do when a system goes.
        /// </remarks>
        public string Apply(string text)
        {
            var trial = new Automation();
            var trouble = ProgramText.Load(text, trial, _world.Values);
            if (trouble.Length > 0)
                return trouble;

            var switches = StateText.Save(_program, _runner);

            var systems = _program.Systems;
            for (var i = systems.Count - 1; i >= 0; i--)
                Drop(systems[i]);

            _program.Discard();
            ProgramText.Load(text, _program, _world.Values);
            StateText.Load(switches, _program, _runner);

            _told = true;
            _program.Kept();
            _program.Edited();
            _program.Rewire();
            Invalidate();
            return "";
        }

        void Drop(AutomationSystem system)
        {
            var sequences = system.Sequences;
            for (var i = sequences.Count - 1; i >= 0; i--)
                ForgetSequence(sequences[i]);

            _program.RemoveSystem(system);
            _systemHistory.Forget(system);
            _standIns.Remove(system.Id);

            // Nobody is left looking at something that no longer exists.
            for (var i = _stack.Count - 1; i > 0; i--)
            {
                if (_stack[i].IsAbout(system))
                    _stack.RemoveAt(i);
            }

            _structureDirty = true;
        }

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

            // A title that follows what is being chosen catches up with the command that chose it,
            // rather than a frame later - after a rebuild too, since that made its words from the page
            // as it stood before the choice was worded.
            if (Current.TakeRetitle())
                Current.RefreshHeading(_screen.Heading);

            Walking();
        }

        /// <summary>
        /// Rebuilds the screen from the page on top of the stack.
        /// </summary>
        /// <remarks>
        /// Every command the chrome shows is made afresh. The page keeps its own view, and with it whatever has the
        /// keyboard and wherever it was scrolled to.
        /// </remarks>
        void Rebuild()
        {
            Rebuilds++;

            // What is wrong is read by the page below, so an edit just made is checked first rather than on the next tick.
            _runner.Checking();

            var page = Current;

            // Rebuilt in place rather than swapped, because a target holds this list across a refresh and
            // compares what is in it to decide whether its own controls need making again.
            _screen.Heading.Clear();
            page.Heading(_screen.Heading);
            _screen.Reheaded();

            _screen.CanGoBack = _stack.Count > 1;
            _screen.Back = _screen.CanGoBack ? Act(Cmd.Back, Leave) : null;
            _screen.Refresh = page.CanRefresh ? Act(Cmd.Refresh, Look) : null;
            _screen.Library = page.Library;
            _screen.Flip = page.CanSwitch ? Act(Cmd.Switch, delegate { page.Switch(!page.SwitchIsOn); }) : null;
            _screen.SwitchAbout = page.SwitchAbout;
            _screen.Runs = page.Runs;
            _screen.Stops = page.Stops;
            _screen.Deletes = page.Deletes;
            _screen.DeleteAbout = page.DeleteAbout;

            // Made whether or not there is anything to walk back, because what there is to walk back changes without a
            // rebuild. So this decides whether the command may exist at all, and the tick decides whether it is offered.
            _screen.HasHistory = page.HasHistory;
            _undo = page.HasHistory ? Act(Cmd.Undo, page.Undo) : null;
            _redo = page.HasHistory ? Act(Cmd.Redo, page.Redo) : null;
            _primary = Act(Cmd.Primary, page.Primary);

            page.Build();

            // A page that went straight on to another as it was built is never shown: the one it went
            // on to is, built from the start.
            if (Current != page)
            {
                Rebuild();
                return;
            }
            Said();

            // Everything, not just what the cadence is due to say: what the page shows may have been made from nothing a
            // moment ago.
            page.Resay();
            page.Refresh();

            _screen.Subtitle = page.Subtitle;
            _screen.SwitchIsOn = page.SwitchIsOn;
            Walking();
            BuildActions();

            _structureDirty = false;
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
            // thing to learn before anything has been done, and navigation is what the screens
            // themselves offer, where it means something.
            //
            // The listing is the exception that proves it: a way to look at something, reachable from
            // anywhere, belonging to the screen rather than to what is on it.
            if (DeveloperMode)
            {
                _actionBar.Add(new ChoiceEntry(Texts.ScriptTitle, Current is ScriptPage,
                    Act(Cmd.ShowScript, ShowScript)));
            }

            _screen.ShowActions(_actionBar);
        }

        /// <summary>
        /// Opens the listing of what this construct's automation is saved as.
        /// </summary>
        /// <remarks>
        /// Offered in <see cref="DeveloperMode" />, and from the notice about a program that would not read. This
        /// does not check either: the check belongs where the way in is put on a screen, so there is one place to
        /// read rather than two that can disagree.
        /// </remarks>
        internal void ShowScript()
        {
            // Also the way to a program's text that would not read, which is mended there whether or not developer mode is on.
            // Offered from every screen including this one, where it is already lit. Pressing it there
            // means "I am looking at it", not "give me another one on top of the first".
            if (Current is ScriptPage)
                return;

            Push(new ScriptPage(this));
        }

        /// <summary>
        /// The blueprint library, where blueprints are imported and exported. Not built yet, and says so.
        /// </summary>
        /// <remarks>
        /// Present rather than hidden because a screen that grows an option later teaches itself twice.
        /// </remarks>
        internal void OfferLoading()
        {
            var page = new ChoicePage(this, Texts.SystemsLoad);
            page.EmptyText = Texts.SystemsLibraryUnbuilt;
            Push(page);
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
            // Where to unwind to once the questions are answered. The system's screen is
            // already underneath - the starters live on it now - so everything the questions
            // pushed is dropped and the finished sequence lands directly on it.
            var origin = _stack.Count;

            // A new sequence is what a trigger usually does, and one being made does nothing yet.
            var aim = new Aim(this, system, null);

            var moment = trigger as MomentTrigger;
            if (moment != null)
            {
                // Nothing picked yet: the sequence is being made, so there is nothing to resolve.
                var picker = new PickPage(this, "When something happens", true,
                    new List<long>(), moment.EventId, Form.Native,
                    delegate(PickAnswer answer)
                    {
                        // Nothing to record: the sequence does not exist yet, so there is no
                        // history for it and nothing to take back to - which is why the trigger
                        // losing the button here loses it outright, the way the bin on its row is
                        // the way back from making one at all.
                        MomentTrigger made = Starter(system, answer, null);
                        Unless(system, made, answer, null, delegate(Occupant taken)
                        {
                            if (taken != null)
                                Release(taken);

                            var answering = Answering(system, made, answer, null);
                            if (answering != null)
                            {
                                answering.Apply(system);
                                _program.Edited();
                                _program.Rewire();
                            }

                            Aimed(system, made, aim, origin, true, null);
                        }, delegate { });
                    },
                    true, aim: aim);

                Push(picker);
                return;
            }

            var every = trigger as EveryTrigger;
            if (every == null)
                return;

            var timer = new TimerPage(this, every.IntervalTicks, aim, delegate(int ticks)
            {
                every.IntervalTicks = ticks;
                Aimed(system, every, aim, origin, true, null);
            });

            Push(timer);
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
            sequence.Name = Name(sequence);

            Invalidate();
            PushMade(new SequencePage(this, sequence));
        }

        /// <summary>Opens <paramref name="page" /> on something just made and given a name of its own, with that name in hand to be given.</summary>
        /// <remarks>A made-up name, "Sequence 3", is one somebody means to replace, so the box is open for it before anything else is pressed.</remarks>
        internal void PushNamed(EditorPage page)
        {
            page.NameOnArrival = true;
            Push(page);
        }

        /// <summary>Opens <paramref name="page" /> on something just made, with its name in hand; left without anything done to it, it goes again.</summary>
        internal void PushMade(EditorPage page)
        {
            page.Made();
            PushNamed(page);
        }

        /// <summary>
        /// Does it, unless something already answers a button it is given; then asks first, and takes the button only where
        /// that is the answer.
        /// </summary>
        /// <remarks>
        /// A toolbar slot holds one thing. Two triggers on the same button and the same press are two claims on that one slot,
        /// and the one written last silently wins, so it is asked before it happens rather than found afterwards on a row that
        /// has gone quiet. Taking a button empties the trigger slot of the blockset that had it, which then needs another, or
        /// takes out the system's trigger that had it; either way the button is written, whatever its slot holds by then.
        /// <para>
        /// Only where the construct is what announces the moment. Two sequences both watching the inner door close is an
        /// ordinary way to build and costs nothing: Sprocket does that watching, and it can watch for two.
        /// </para>
        /// <para>
        /// Its own trigger never counts, so settling on what was already there is not a clash with itself.
        /// </para>
        /// </remarks>
        /// <param name="system">The logic system the trigger is in.</param>
        /// <param name="made">The trigger as it would be once done.</param>
        /// <param name="answer">What was picked, buttons and all.</param>
        /// <param name="except">The trigger being changed, which never clashes with itself; null for a new one.</param>
        /// <param name="then">What is done once it may be, given the trigger the button is taken from and the blockset it was in, or null where it takes none.</param>
        /// <param name="otherwise">What is done where the answer is to leave the button as it is.</param>
        void Unless(AutomationSystem system, MomentTrigger made, PickAnswer answer, Trigger except, Action<Occupant> then,
            Action otherwise)
        {
            if (!(made is RequiredTrigger))
            {
                then(null);
                return;
            }

            // A system's trigger is one variable, holding the first button picked, so no other is checked or written.
            IList<long> panels = system.IsBlueprint || answer.Targets.Count <= 1 ? answer.Targets : new[] { answer.Targets[0] };
            var holder = Holding(panels, Pressed(answer), except, StandIn(system), null);

            if (holder == null)
            {
                then(null);
                Wired(panels);
                return;
            }

            AskAbout(holder);
            Answer(Texts.ButtonTakeOver, delegate
            {
                then(holder.Trigger == null ? null : holder);
                Wired(panels);
            });
            Answer(Texts.ButtonLeaveAlone, otherwise, true);
        }

        /// <summary>
        /// The toolbar slot answering the picked trigger on each picked block: the trigger's own, or one of the slots a trigger
        /// that stands for many runs; slot 0 where what was picked is no trigger.
        /// </summary>
        internal List<ToolbarSlot> Pressed(PickAnswer answer)
        {
            var picked = ToolbarSlot.Of(answer.Chosen);
            var at = new List<ToolbarSlot>();
            for (var i = 0; i < answer.Targets.Count; i++)
            {
                GridEntity block = picked != null && picked.IsMany ? _world.FindEntity(answer.Targets[i]) : null;
                at.Add(picked == null ? new ToolbarSlot(1, 1, 0) : block != null ? Free(block, picked) : picked);
            }

            return at;
        }

        /// <summary>The first of the slots <paramref name="many" /> stands for on <paramref name="block" /> that holds nothing, or the first of them where every one holds something.</summary>
        /// <remarks>Each trigger written to a timer takes a slot of its own, so two sequences started by one timer both run when it goes off.</remarks>
        static ToolbarSlot Free(GridEntity block, ToolbarSlot many)
        {
            IMyToolbar toolbar = Toolbars.Of(block.Block, many.Toolbar);
            ToolbarSlot first = null;
            for (var page = 1; toolbar != null && page <= toolbar.PageCount; page++)
            {
                for (var slot = 1; slot <= toolbar.SlotCount; slot++)
                {
                    var one = new ToolbarSlot(many.Toolbar, page, slot);
                    if (!many.Covers(one))
                        continue;

                    if (first == null)
                        first = one;

                    MyToolbarItemInfo held;
                    if (!toolbar.TryGetItem(Toolbars.Index(toolbar, one), out held))
                        return one;
                }
            }

            return first ?? new ToolbarSlot(many.Toolbar, 1, 0);
        }

        /// <summary>
        /// Fills <paramref name="part" /> of <paramref name="chosen" /> with the blocks <paramref name="blocks" /> by
        /// <paramref name="fill" />, asking first where it is a trigger part and something else already answers one of the
        /// toolbar slots <paramref name="at" /> it is given.
        /// </summary>
        /// <remarks>
        /// One question at a time. Taking a button empties the trigger slot of the blockset that had it; nothing is taken until
        /// every question is answered, and leaving any of them alone leaves the part as it was.
        /// </remarks>
        internal void Filling(AutomationSystem system, Part part, SystemInstance chosen, IList<long> blocks, IList<ToolbarSlot> at,
            Action fill)
        {
            Filling(system, part, chosen, blocks, at, fill, new List<Occupant>());
        }

        void Filling(AutomationSystem system, Part part, SystemInstance chosen, IList<long> blocks, IList<ToolbarSlot> at,
            Action fill, List<Occupant> taking)
        {
            var trigger = part.IsTrigger && at != null ? TriggerOf(system, part.Id) : null;
            var holder = trigger == null ? null : Holding(blocks, at, trigger, chosen, taking);
            if (holder != null)
            {
                AskAbout(holder);
                Answer(Texts.ButtonTakeOver, delegate
                {
                    taking.Add(holder);
                    Filling(system, part, chosen, blocks, at, fill, taking);
                });
                Answer(Texts.ButtonLeaveAlone, delegate { }, true);
                return;
            }

            for (var i = 0; i < taking.Count; i++)
            {
                if (taking[i].Trigger != null)
                    Release(taking[i]);
            }

            fill();
            Wired(blocks);
        }

        /// <summary>The trigger that is the trigger part <paramref name="part" /> of <paramref name="system" />, or null.</summary>
        static RequiredTrigger TriggerOf(AutomationSystem system, string part)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i] as RequiredTrigger;
                if (trigger != null && trigger.Part == part)
                    return trigger;
            }

            return null;
        }

        /// <summary>What already answers a toolbar slot: a trigger of the program on one of its system, or, with no trigger, a slot the program did not write.</summary>
        sealed class Occupant
        {
            public GridEntity Panel;
            public ToolbarSlot At;
            public AutomationSystem System;
            public RequiredTrigger Trigger;
            public SystemInstance Instance;

            public bool Same(Occupant other)
            {
                return Panel.Id == other.Panel.Id && At.Same(other.At) && Trigger == other.Trigger && Instance == other.Instance;
            }
        }

        /// <summary>
        /// The first of <paramref name="panels" /> whose toolbar slot in <paramref name="at" /> something other than
        /// <paramref name="trigger" /> on <paramref name="instance" /> already answers, passing over what is in
        /// <paramref name="taking" />; or null where nothing does.
        /// </summary>
        /// <remarks>
        /// Compared as blocks and slots, through every one of every system: a toolbar slot belongs to a block, whichever part
        /// of whichever system names it. A trigger of the program comes first, since it can be named; then a slot holding what
        /// the program did not write, which is only read where no trigger has the slot.
        /// </remarks>
        Occupant Holding(IList<long> panels, IList<ToolbarSlot> at, Trigger trigger, SystemInstance instance, List<Occupant> taking)
        {
            for (var p = 0; p < panels.Count && p < at.Count; p++)
            {
                GridEntity panel = _world.FindEntity(panels[p]);
                var where = at[p];
                if (panel == null || where.Slot < 1)
                    continue;

                var named = false;
                for (var s = 0; s < _program.Systems.Count; s++)
                {
                    AutomationSystem each = _program.Systems[s];
                    var triggers = each.Triggers;
                    for (var i = 0; i < triggers.Count; i++)
                    {
                        var other = triggers[i] as RequiredTrigger;
                        if (other == null)
                            continue;

                        var instances = each.Uses;
                        for (var n = 0; n < instances.Count; n++)
                        {
                            var through = instances[n];
                            if ((other == trigger && (through == instance || !each.IsBlueprint))
                                || !Answers(each, through, other.Part, panel.Id, where))
                                continue;

                            named = true;
                            var holder = new Occupant { Panel = panel, At = where, System = each, Trigger = other, Instance = through };
                            if (!Taken(taking, holder))
                                return holder;
                        }
                    }
                }

                if (!named && _world.Foreign(panel.Block, where))
                {
                    var holder = new Occupant { Panel = panel, At = where };
                    if (!Taken(taking, holder))
                        return holder;
                }
            }

            return null;
        }

        static bool Taken(List<Occupant> taking, Occupant holder)
        {
            for (var i = 0; taking != null && i < taking.Count; i++)
            {
                if (taking[i].Same(holder))
                    return true;
            }

            return false;
        }

        /// <summary>Whether <paramref name="instance" /> of <paramref name="system" /> answers trigger <paramref name="trigger" /> with the slot <paramref name="where" /> on the block <paramref name="block" />.</summary>
        bool Answers(AutomationSystem system, SystemInstance instance, string trigger, long block, ToolbarSlot where)
        {
            var blocks = new List<string>();
            var positions = new List<ToolbarSlot>();
            _program.Answers(system, instance, trigger, blocks, positions);
            for (var i = 0; i < blocks.Count && i < positions.Count; i++)
            {
                GridEntity entity = _runner.Bindings.Entity(blocks[i]);
                if (entity != null && entity.Id == block && positions[i].Same(where))
                    return true;
            }

            return false;
        }

        /// <summary>Asks whether to take the button <paramref name="holder" /> has, saying what it does now.</summary>
        void AskAbout(Occupant holder)
        {
            var arguments = new Arguments();
            arguments.Set("block", holder.Panel.Name);

            if (holder.Trigger == null)
            {
                arguments.Set("button", holder.At.Slot.ToString(System.Globalization.CultureInfo.InvariantCulture));
                AskHere(Texts.ButtonElsewhere, arguments);
                return;
            }

            var sequence = holder.System.FindSequence(holder.Trigger.Runs);
            arguments.Set("sequence", sequence == null ? "" : sequence.HasName ? sequence.Name : SequenceText.DefaultName(Texts, sequence.Number));
            arguments.Set("blockset", holder.System.IsBlueprint && holder.Instance != null
                ? SystemSummary.DescribeInstance(Texts, _runner.Bindings, holder.System, holder.Instance)
                : holder.System.Name);
            AskHere(holder.Trigger.Acts != null ? Texts.ButtonActs : holder.Trigger.Stops ? Texts.ButtonStops : Texts.ButtonStarts,
                arguments);
        }

        /// <summary>
        /// Takes <paramref name="holder" />'s toolbar slot out of the trigger slot of the blockset that had it, which then needs
        /// another, or takes out a system's trigger, whose variable holds only that button.
        /// </summary>
        void Release(Occupant holder)
        {
            if (!holder.System.IsBlueprint)
            {
                holder.System.ReplaceTrigger(holder.Trigger, null);
                _program.Edited();
                _program.Rewire();
                return;
            }

            var kept = new List<string>();
            var keptAt = new List<ToolbarSlot>();
            Kept(holder, kept, keptAt);

            if (kept.Count == 0)
                holder.Instance.Clear(holder.Trigger.Part);
            else
                holder.Instance.Fill(holder.Trigger.Part, kept, keptAt);

            _program.Edited();
            _program.Rewire();
        }

        /// <summary>Does <paramref name="made" /> and takes <paramref name="holder" />'s button, as one edit.</summary>
        SystemEdit TakingOver(SystemEdit made, Occupant holder)
        {
            if (!holder.System.IsBlueprint)
                return new TakeOverTrigger(made, holder.System, holder.Trigger);

            var kept = new List<string>();
            var keptAt = new List<ToolbarSlot>();
            Kept(holder, kept, keptAt);
            return new TakeOver(made, holder.Instance, holder.Trigger.Part, kept, keptAt);
        }

        /// <summary>What answers <paramref name="holder" />'s trigger part in its blockset once its toolbar slot is taken out.</summary>
        void Kept(Occupant holder, List<string> kept, List<ToolbarSlot> keptAt)
        {
            var part = holder.Trigger.Part;
            var blocks = holder.Instance.FilledWith(part);
            var positions = holder.Instance.Positions(part);
            for (var i = 0; blocks != null && positions != null && i < blocks.Count && i < positions.Count; i++)
            {
                GridEntity entity = _runner.Bindings.Entity(blocks[i]);
                if (entity != null && entity.Id == holder.Panel.Id && positions[i].Same(holder.At))
                    continue;

                kept.Add(blocks[i]);
                keptAt.Add(positions[i]);
            }
        }

        /// <summary>Has every slot the program wants on <paramref name="panels" /> written, whatever it holds, as somebody has just picked them.</summary>
        void Wired(IList<long> panels)
        {
            if (Wire == null)
                return;

            for (var i = 0; i < panels.Count; i++)
                Wire(panels[i]);
        }

        /// <summary>
        /// What somebody has just described, as the trigger that makes it happen.
        /// </summary>
        /// <remarks>
        /// The one place the two arrangements are told apart, and nobody is asked: picking a button means the construct
        /// already announces it and Sprocket wires a slot, picking a door means Sprocket watches for it. See
        /// <see cref="Vocabulary.Starter" />. In a system blueprint a button names a trigger slot, new or, picked again for
        /// <paramref name="keeping" />, that trigger's own; the slot is taken and answered once the question of whether
        /// the buttons are free is answered - see <see cref="Answering" />. In a system it names the variable holding the
        /// first button picked, defined at that answer too.
        /// </remarks>
        internal MomentTrigger Starter(AutomationSystem system, PickAnswer answer, RequiredTrigger keeping)
        {
            MomentTrigger made = Vocabulary.Starter(
                Vocabulary.EventsOf(_world, answer.Targets), answer.Chosen, answer.All);

            if (!(made is RequiredTrigger))
            {
                var named = Named(system, answer.Targets);
                for (var i = 0; i < named.Count; i++)
                    made.Sources.Add(named[i]);

                return made;
            }

            if (keeping != null && system.IsBlueprint)
                return RequiredTrigger.Named(keeping.Part);

            var declared = new List<string>();
            var at = new List<ToolbarSlot>();
            var first = Picked(answer, declared, at);
            if (first == null)
                return made;

            if (!system.IsBlueprint)
                return RequiredTrigger.Named(TriggerName(declared[0], at[0]));

            if (StandIn(system) == null)
                return made;

            var called = declared.Count == 1 ? Called(first) : first.Kind.DisplayName;
            return RequiredTrigger.Named(system.Free(called));
        }

        /// <summary>
        /// What the variable holding slot <paramref name="at" /> on the block <paramref name="block" /> names is called: its
        /// own name where there is one, and the block's with the slot's number after it otherwise.
        /// </summary>
        string TriggerName(string block, ToolbarSlot at)
        {
            var held = _program.FindTriggerRef(block, at);
            return held != null
                ? held.Name
                : _program.Free(block + " " + at.Slot.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// The edit that answers the trigger part <paramref name="made" /> names in the blockset shown with the buttons
        /// <paramref name="answer" /> picked, taking the part where it is new, around <paramref name="then" />; null where
        /// <paramref name="made" /> names no part or nothing was picked.
        /// </summary>
        /// <remarks>
        /// Made once the question about the buttons is answered, so leaving them alone leaves no part behind. A system's
        /// trigger is a variable of the program's, defined here for the first button picked, and <paramref name="then" /> is
        /// all there is to do.
        /// </remarks>
        SystemEdit Answering(AutomationSystem system, MomentTrigger made, PickAnswer answer, SystemEdit then)
        {
            var required = made as RequiredTrigger;
            if (required == null)
                return null;

            var declared = new List<string>();
            var at = new List<ToolbarSlot>();
            var first = Picked(answer, declared, at);
            if (first == null)
                return null;

            if (!system.IsBlueprint)
            {
                _program.DeclareTrigger(declared[0], at[0], required.Part);
                return then;
            }

            var instance = StandIn(system);
            if (instance == null)
                return null;

            return new AnswerTrigger(then, system, instance, required.Part, Type(first.Kind), declared, at);
        }

        /// <summary>The blocks <paramref name="answer" /> picked, as the construct declares them, each with the toolbar slot its press is answered on; the first of them, or null.</summary>
        internal GridEntity Picked(PickAnswer answer, List<string> declared, List<ToolbarSlot> at)
        {
            var pressed = Pressed(answer);
            GridEntity first = null;
            for (var i = 0; i < answer.Targets.Count; i++)
            {
                GridEntity entity = _world.FindEntity(answer.Targets[i]);
                if (entity == null)
                    continue;

                if (first == null)
                    first = entity;

                declared.Add(_program.Declare(_world, entity).Name);
                at.Add(pressed[i]);
            }

            return first;
        }

        /// <summary>
        /// Carries out what a trigger was chosen to do, with the change to what starts it where there
        /// is one.
        /// </summary>
        /// <remarks>
        /// A trigger being changed that goes on doing what it did has only its start to change, and one
        /// that did nothing takes what the column opened on. What starts it goes in with what it does,
        /// as one edit.
        /// <para>
        /// A single action is chosen on the page after, in the same pick, and nothing is kept until it
        /// is. A trigger being made does not exist until then, so backing out leaves nothing behind.
        /// </para>
        /// </remarks>
        void Aimed(AutomationSystem system, Trigger trigger, Aim aim, int origin, bool fresh,
            SystemEdit start)
        {
            if (aim.Outcome == Outcome.One)
            {
                DoOneThing(system, trigger, origin, fresh, start, aim);
                return;
            }

            UnwindTo(origin);

            if (!fresh && !aim.Moved)
            {
                if (start != null)
                    Do(system, start);

                return;
            }

            if (aim.Outcome == Outcome.New)
            {
                RunNew(system, trigger, origin, fresh, start);
                return;
            }

            var sequence = system.FindSequence(aim.Sequence);
            if (sequence != null)
                Points(system, trigger, new Run(sequence.Number, aim.Stops), fresh, null, start);
            else if (start != null)
                Do(system, start);
        }

        /// <summary>
        /// Where every choice of what a trigger does lands.
        /// </summary>
        /// <remarks>
        /// A trigger being made is not yet in the system, and nothing has been done that could be
        /// taken back - the way back from making one is the bin on its row. A trigger being changed
        /// is already there, so this is an edit, and it is the system's to walk back, together with
        /// <paramref name="start" /> where what starts it changed too.
        /// </remarks>
        void Points(AutomationSystem system, Trigger trigger, Step does, bool fresh, Sequence made, SystemEdit start)
        {
            if (fresh)
            {
                trigger.Does = does;
                system.AddTrigger(trigger);
                _program.Rewire();
                Invalidate();
                return;
            }

            var point = new PointTrigger(_program, trigger, does, made);
            Do(system, start == null ? point : (SystemEdit)new Retrigger(start, point));
        }

        /// <summary>
        /// Makes the sequence this trigger runs, and opens it.
        /// </summary>
        /// <remarks>Made in a system switched off, as everything in one is, so a trigger with no steps behind it is never live.</remarks>
        void RunNew(AutomationSystem system, Trigger trigger, int origin, bool fresh, SystemEdit start)
        {
            // The picker goes with the answer. Left on the stack, backing out of the sequence would
            // land on the choice of what it does, for a trigger that already does something.
            UnwindTo(origin);

            var sequence = _program.AddSequence(system);
            sequence.Name = Name(sequence);

            Points(system, trigger, new Run(sequence.Number), fresh, sequence, start);

            PushNamed(new SequencePage(this, sequence));
        }

        /// <summary>
        /// Asks what one thing the trigger should do, as the next page of the pick what it does was
        /// chosen in, and makes it do exactly that.
        /// </summary>
        /// <remarks>
        /// What it holds is an act of its own. Where the block has an action of the game's own for it,
        /// the host writes that into the button and Sprocket is not part of the press - see
        /// <c>SprocketUnit</c>, which decides that by looking at the act rather than by being told.
        /// <para>
        /// A trigger that already does one thing opens on it, and finishing puts the act chosen in its
        /// place, together with <paramref name="start" />.
        /// </para>
        /// <para>
        /// The page is kept on <paramref name="aim" /> while the single action stays chosen, so going
        /// back to look at what starts the trigger and on again finds it as it was left. What finishing
        /// it does is set afresh every time, since what starts the trigger may have changed between.
        /// </para>
        /// </remarks>
        void DoOneThing(AutomationSystem system, Trigger trigger, int origin, bool fresh,
            SystemEdit start, Aim aim)
        {
            aim.Finish = delegate(PickAnswer answer)
            {
                var act = new Act(answer.Chosen);
                act.Form = answer.Form;
                var named = Named(system, answer.Targets);
                for (var i = 0; i < named.Count; i++)
                    act.Targets.Add(named[i]);

                for (var i = 0; i < answer.Values.Count; i++)
                    act.Values.Add(answer.Values[i]);

                UnwindTo(origin);
                Points(system, trigger, act, fresh, null, start);
            };

            var only = fresh ? null : trigger.Acts;
            if (only != null)
            {
                if (aim.Following == null)
                {
                    aim.Following = new PickPage(this, Texts.PickSingleTask, false, Blocks(system, only.Targets),
                        only.ActionId, only.Form, delegate(PickAnswer answer) { aim.Finish(answer); },
                        values: only.Values, opens: aim.Opens, after: aim.Pick);
                }

                aim.Following.Arrived(aim.Straight);
                aim.Straight = false;
                Push(aim.Following);
                return;
            }

            if (aim.Following == null)
            {
                aim.Following = new PickPage(this, Texts.PickSingleTask, false, new List<long>(), "", Form.Native,
                    delegate(PickAnswer answer) { aim.Finish(answer); }, opens: aim.Opens, after: aim.Pick);
            }

            aim.Following.Arrived(aim.Straight);
            aim.Straight = false;
            Push(aim.Following);
        }

        /// <summary>
        /// Makes <paramref name="system" /> a system blueprint, with a first instance answering its slots with what it named;
        /// that instance, or the first of a blueprint already.
        /// </summary>
        /// <remarks>
        /// A system names the program's variables, and a blueprint its own slots: so each block the system acts on becomes a
        /// slot, and each trigger it is started by a trigger slot, called what the block is called now. Every step and trigger
        /// that named the variable names the slot, and the first instance answers it with that variable.
        /// </remarks>
        public SystemInstance Blueprinted(AutomationSystem system)
        {
            if (system.IsBlueprint)
                return system.First;

            var first = system.AddInstance();
            system.IsBlueprint = true;

            var named = new List<string>();
            system.Acted(named);

            var slots = new Dictionary<string, string>();
            for (var i = 0; i < named.Count; i++)
            {
                // What starts a trigger is named too, and becomes a trigger slot below.
                if (_program.FindTriggerRef(named[i]) != null)
                    continue;

                GridEntity entity = _runner.Bindings.Entity(named[i]);
                var slot = system.Free(entity == null ? _program.Called(named[i], _world.Values) : Called(entity));
                var part = system.PartFor(slot);
                part.Picked = true;
                if (entity != null && Type(entity.Kind).Length > 0)
                    part.Expects.Add(Type(entity.Kind));

                first.Fill(slot, new[] { named[i] });
                slots[named[i]] = slot;
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var needed = triggers[i] as RequiredTrigger;
                var declared = needed == null ? null : _program.FindTriggerRef(needed.Part);
                if (declared == null || slots.ContainsKey(needed.Part))
                    continue;

                GridEntity panel = _runner.Bindings.Entity(declared.Block);
                var slot = system.Free(panel == null ? declared.Name : Called(panel));
                var part = system.PartFor(slot);
                part.Picked = true;
                part.IsTrigger = true;
                part.Optional = true;

                first.Fill(slot, new[] { declared.Block }, new[] { declared.At });

                slots[needed.Part] = slot;
            }

            for (var i = 0; i < triggers.Count; i++)
            {
                var moment = triggers[i] as MomentTrigger;
                if (moment != null)
                    Slotted(moment.Sources, slots);

                var own = triggers[i].Acts;
                if (own != null)
                    Slotted(own.Targets, slots);
            }

            var sequences = system.Sequences;
            for (var q = 0; q < sequences.Count; q++)
            {
                var steps = sequences[q].Steps;
                for (var s = 0; s < steps.Count; s++)
                {
                    var act = steps[s] as Act;
                    if (act != null)
                        Slotted(act.Targets, slots);
                }
            }

            _program.Edited();
            _program.Rewire();
            return first;
        }

        /// <summary>Names the slot for each variable in <paramref name="names" /> that <paramref name="slots" /> has one for.</summary>
        static void Slotted(IList<string> names, Dictionary<string, string> slots)
        {
            for (var i = 0; i < names.Count; i++)
            {
                string slot;
                if (slots.TryGetValue(names[i], out slot))
                    names[i] = slot;
            }
        }

        /// <summary>
        /// The names <paramref name="system" /> uses for these blocks: a system's variables, each block declared where the
        /// construct has not named it yet, or the slots a blueprint's stand-in fills with them, a slot taken for any block
        /// that fills none.
        /// </summary>
        /// <remarks>
        /// Where picking a block turns into naming one. What a blueprint's sentence stores is a slot, and which block fills
        /// it belongs to each instance - so the same sentence acts on the port airlock's door in one and the starboard's in
        /// another.
        /// </remarks>
        internal List<string> Named(AutomationSystem system, IList<long> blocks)
        {
            SystemInstance instance = system.IsBlueprint ? StandIn(system) : null;

            var named = new List<string>();
            for (var i = 0; i < blocks.Count; i++)
            {
                GridEntity entity = _world.FindEntity(blocks[i]);
                if (entity == null)
                    continue;

                var declared = _program.Declare(_world, entity).Name;
                named.Add(instance == null
                    ? declared
                    : system.Take(instance, declared, Called(entity), Type(entity.Kind)));
            }

            return named;
        }

        /// <summary>
        /// What these names in <paramref name="system" /> come to as its stand-in fills them: the
        /// blocks, as the construct declares them.
        /// </summary>
        /// <remarks>A name that is not a part, or a part the stand-in leaves empty, comes through as it is.</remarks>
        internal List<string> Seen(AutomationSystem system, IList<string> names)
        {
            return SystemInstance.Through(StandIn(system), names);
        }

        /// <summary>
        /// The instance of <paramref name="system" /> whose blocks the editor shows, and picks blocks
        /// through: the one last chosen, or the first.
        /// </summary>
        internal SystemInstance StandIn(AutomationSystem system)
        {
            if (system == null)
                return null;

            int number;
            SystemInstance chosen = _standIns.TryGetValue(system.Id, out number) ? system.FindInstance(number) : null;
            return chosen ?? system.First;
        }

        /// <summary>Adds another logic system made from <paramref name="system" />, named for it with the first number free from two.</summary>
        internal SystemInstance Another(AutomationSystem system)
        {
            for (var number = 2; ; number++)
            {
                var name = system.Name + " " + number;
                if (system.FindInstance(name) == null)
                    return Another(system, name);
            }
        }

        /// <summary>
        /// Adds another instance of a blueprint, called <paramref name="name" />, and shows the blueprint
        /// through it; or null where the name is empty or one of them already has it.
        /// </summary>
        /// <remarks>
        /// Shown through the new one because the next thing to do is say which blocks it uses, and every
        /// part of it is empty until somebody does.
        /// </remarks>
        internal SystemInstance Another(AutomationSystem system, string name)
        {
            name = Naming.Clean(name);
            if (name.Length == 0 || system.FindInstance(name) != null)
                return null;

            SystemInstance made = system.AddInstance();
            made.Name = name;
            Choose(system, made);
            return made;
        }

        /// <summary>Shows <paramref name="system" /> through <paramref name="instance" />'s blocks from now on.</summary>
        internal void Choose(AutomationSystem system, SystemInstance instance)
        {
            if (system == null || instance == null)
                return;

            _standIns[system.Id] = instance.Number;
            Invalidate();
        }

        /// <summary>What a slot taken for <paramref name="entity" /> is called at first: the block's own name, or its kind where it has none.</summary>
        static string Called(GridEntity entity)
        {
            var name = Naming.Clean(entity.Name);
            return name.Length > 0 ? name : entity.Kind.DisplayName;
        }

        /// <summary>The type a part filled by a block of <paramref name="kind" /> expects, with no subtype.</summary>
        static string Type(EntityKind kind)
        {
            var sort = BlockKinds.Sort(kind.Id);
            var slash = sort.IndexOf('/');
            return slash < 0 ? sort : sort.Substring(0, slash);
        }

        /// <summary>
        /// Takes a trigger out of a system.
        /// </summary>
        /// <remarks>
        /// No question asked, because the system's arrows are the answer to a deletion nobody meant -
        /// and a question in front of every one of them is how people learn to press through
        /// questions. A sequence deleted from its own screen still asks: that is work somebody can
        /// see, and its way back would be on a screen that went with it.
        /// </remarks>
        internal void Forget(AutomationSystem system, Trigger trigger)
        {
            Do(system, new ForgetTrigger(system, trigger));
        }

        /// <summary>
        /// Opens the picker a trigger was made in, on what starts it and what it does now, and puts
        /// the answer in its place.
        /// </summary>
        /// <remarks>
        /// Wherever the trigger's row was pressed - what starts it, or what it does - because the two
        /// are chosen side by side. A trigger that listens for a moment stays one: turning a repeat
        /// into a moment is deleting it and making the one that was meant.
        /// <para>
        /// Whatever somebody wrote about it is carried over, and what it does is changed only where a
        /// different choice was made.
        /// </para>
        /// <para>
        /// It opens on the column of the part pressed. A part of the single action a trigger does is
        /// on the page after, so the picker goes straight on to that page, where the action was made.
        /// </para>
        /// </remarks>
        internal void ChangeTrigger(AutomationSystem system, Trigger trigger, Opening opens, bool action)
        {
            // Back to the screen the row is on, which is where the answer lands.
            var origin = _stack.Count;
            var aim = new Aim(this, system, trigger);
            aim.Opens = action ? opens : Opening.Blocks;
            aim.Straight = action;

            var every = trigger as EveryTrigger;
            if (every != null)
            {
                var rate = every.IntervalTicks;
                var timer = new TimerPage(this, rate, aim, delegate(int ticks)
                {
                    Aimed(system, every, aim, origin, false,
                        ticks == rate ? null : new SetCadence(every, rate, ticks));
                });

                Push(timer);
                if (action)
                    timer.GoOn();

                return;
            }

            var moment = trigger as MomentTrigger;
            if (moment == null)
                return;

            var was = moment as WhenTrigger;

            var picker = new PickPage(this, "When something happens", true,
                Blocks(system, moment.Sources), Vocabulary.EventOf(_program, _runner.Bindings, system, moment, StandIn(system)), Form.Native,
                delegate(PickAnswer answer)
                {
                    var keeping = moment as RequiredTrigger;
                    MomentTrigger made = Starter(system, answer, keeping);
                    Unless(system, made, answer, moment, delegate(Occupant taken)
                    {
                        // A button picked again for a button trigger answers the same trigger part, so the trigger is kept:
                        // its toolbar slots name it.
                        if (keeping != null && made is RequiredTrigger && system.IsBlueprint)
                        {
                            var refill = Answering(system, made, answer, null);
                            if (refill != null)
                                Aimed(system, moment, aim, origin, false, taken == null ? refill : TakingOver(refill, taken));
                            else
                                Aimed(system, moment, aim, origin, false, null);

                            return;
                        }

                        var change = new ChangeTrigger(moment, made);
                        if (!change.Changes && taken == null)
                        {
                            Aimed(system, moment, aim, origin, false, null);
                            return;
                        }

                        // One edit either way, because it is one answer to one question.
                        var edit = Answering(system, made, answer, change) ?? change;
                        Aimed(system, made, aim, origin, false, taken == null ? edit : TakingOver(edit, taken));
                    }, delegate { UnwindTo(origin); });
                },
                true, was != null && was.RequiresAll, opens: action ? Opening.Nothing : opens, aim: aim);

            Push(picker);
            if (action)
                picker.GoOn();
        }

        /// <summary>
        /// What these names in <paramref name="system" /> point at, as blocks, for a control that shows
        /// blocks.
        /// </summary>
        /// <remarks>
        /// The model points at claims; a picker and a sentence show things on the ship. This is where the
        /// two meet. A reference that points at nothing is left out rather than stood in for by a zero:
        /// what to say about the gap is the control's, and it can already see that fewer came back than
        /// the sentence claims.
        /// </remarks>
        internal List<long> Blocks(AutomationSystem system, IList<string> names)
        {
            var seen = Seen(system, names);
            var found = new List<long>();
            for (var i = 0; i < seen.Count; i++)
            {
                GridEntity entity = _runner.Bindings.Entity(seen[i]);
                if (entity != null)
                    found.Add(entity.Id);
            }

            return found;
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
            if (Elsewhere != null)
            {
                Elsewhere(sequence, StandIn(sequence.System), true);
                return;
            }

            SequenceRunner state = _runner.Get(sequence, StandIn(sequence.System));
            if (state != null)
                state.Stop();
        }

        internal void RunNow(Sequence sequence)
        {
            if (Elsewhere != null)
            {
                Elsewhere(sequence, StandIn(sequence.System), false);
                return;
            }

            // By hand, which is what lets a parked sequence run at all: the switch keeps triggers
            // off it, and a person at the screen is not a trigger.
            _runner.Call(sequence, StandIn(sequence.System), true);
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
        /// rather than left to back. Back does mean keep - it always means return to where somebody
        /// came from - but a screen whose only row is the destructive one is a screen where a stray press
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
            Deleting(Texts.SequenceDeleteQuestion, SequenceText.Called(Texts, sequence),
                Texts.DeleteFinal, null, delegate { TakeOut(sequence); });
        }

        /// <summary>Asks before taking <paramref name="system" /> out of the program, with its sequences, its triggers and every one of it.</summary>
        /// <remarks>
        /// A blueprint only once its name is typed, in a box whose way on is dead until it is: every logic system made from
        /// it goes with it, which is too much to lose to a press that landed on the wrong answer.
        /// </remarks>
        internal void DeleteSystem(AutomationSystem system)
        {
            if (!system.IsBlueprint)
            {
                Deleting(Texts.DeleteSystem, system.Name, Texts.DeleteFinal, null, delegate { Drop(system); });
                return;
            }

            _asking = new Prompt(Texts.DeleteConfirm);
            _asking.Ask(Template.Fill(Texts.DeleteBlueprint, "name", system.Name), "", null,
                delegate(string typed) { return string.Equals(typed.Trim(), system.Name, StringComparison.OrdinalIgnoreCase); },
                delegate
                {
                    var drop = Act(Cmd.DeleteSystem, delegate { Drop(system); });
                    if (drop != null)
                        drop.Execute();
                },
                Windows, Input);
        }

        /// <summary>Asks before taking <paramref name="instance" />, one of the logic systems made from a blueprint, out of it.</summary>
        /// <remarks>The blueprint stays, the last one taken or not: it goes only from its own logic system screen, by name.</remarks>
        internal void DeleteInstance(AutomationSystem system, SystemInstance instance)
        {
            var named = SystemSummary.DescribeInstance(Texts, _runner.Bindings, system, instance);

            var arguments = new Arguments();
            arguments.Set("name", system.Name);

            Deleting(Texts.DeleteInstance, named, Texts.DeleteInstanceKept, arguments, delegate { TakeOut(system, instance); });
        }

        /// <summary>Switches <paramref name="system" /> on or off; a blueprint off only after asking, since that stops every logic system made from it.</summary>
        internal void SwitchSystem(AutomationSystem system, bool on)
        {
            if (on || !system.IsBlueprint || system.Instances.Count == 0)
            {
                system.Enabled = on;
                Invalidate();
                return;
            }

            var instances = system.Instances;
            var arguments = new Arguments();
            arguments.Set("name", system.Name);

            var listed = Bullets(instances.Count,
                delegate(int i) { return SystemSummary.DescribeInstance(Texts, _runner.Bindings, system, instances[i]); }, arguments);
            var running = SystemSummary.IsRunning(system, _runner, null);
            Ask(Texts.BlueprintSwitchOffQuestion + "\n\n" + Texts.BlueprintSwitchOffStops + listed
                + (running ? "\n\n" + Texts.BlueprintSwitchOffRunning : ""), arguments);

            // Keeping them running is the one Enter takes.
            Answer(Texts.BlueprintKeepRunning, delegate { }, true);
            Answer(Texts.BlueprintSwitchThemOff, delegate
            {
                system.Enabled = false;
                Invalidate();
            });
        }

        /// <summary>Switches <paramref name="instance" /> of <paramref name="system" /> on or off; on while the system is off, it asks to switch the system on as well.</summary>
        /// <remarks>Leaving it off is the answer Enter takes, since the key pressed without reading must not start a machine.</remarks>
        internal void SwitchInstance(AutomationSystem system, SystemInstance instance, bool on)
        {
            if (!on || system.Enabled)
            {
                instance.Enabled = on;
                return;
            }

            var arguments = new Arguments();
            arguments.Set("name", system.Name);

            AskHere(Texts.SystemOffQuestion, arguments);
            Answer(Texts.SystemSwitchItOn, delegate
            {
                instance.Enabled = true;
                SwitchSystem(system, true);
            });
            Answer(Texts.SystemLeaveItOff, delegate { }, true);
        }

        /// <summary>What pressing the switch of <paramref name="system" /> would do, said as a note on it.</summary>
        internal string SwitchAbout(AutomationSystem system)
        {
            if (!system.IsBlueprint)
                return StatusRow.Switching(Texts, true, system.Enabled);

            return system.Enabled ? Texts.RowSwitchBlueprintOff : Texts.RowSwitchBlueprintOn;
        }

        /// <summary>Takes <paramref name="sequence" /> out of the program without asking, and any page showing it.</summary>
        internal void TakeOut(Sequence sequence)
        {
            _program.RemoveSequence(sequence);
            ForgetSequence(sequence);
        }

        /// <summary>Takes <paramref name="system" /> out of the program without asking, with everything in it and any page showing it.</summary>
        internal void TakeOut(AutomationSystem system)
        {
            Drop(system);
        }

        /// <summary>Takes <paramref name="instance" /> out of <paramref name="system" /> without asking.</summary>
        /// <remarks>For a question already answered, or for one made a moment ago that was left without anything done to it.</remarks>
        internal void TakeOut(AutomationSystem system, SystemInstance instance)
        {
            system.RemoveInstance(instance);
            _standIns.Remove(system.Id);
            _program.Rewire();
            _structureDirty = true;
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

            PushMade(new SequencePage(this, copy));
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
                return SequenceText.DefaultName(Texts, 1);

            for (var number = 1; ; number++)
            {
                var wanted = SequenceText.DefaultName(Texts, number);

                var taken = false;
                for (var i = 0; i < owner.Sequences.Count && !taken; i++)
                    taken = owner.Sequences[i] != sequence && owner.Sequences[i].Name == wanted;

                if (!taken)
                    return wanted;
            }
        }
    }
}
