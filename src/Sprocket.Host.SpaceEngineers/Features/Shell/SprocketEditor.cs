using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Developing;
using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.Features.Library;
using Sprocket.Host.SpaceEngineers.Features.Ownership;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;
using Lumel.Input;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Lumel.Text;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Sentences;
using Sprocket.Host.SpaceEngineers.Features.Fields;
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
        PlayerMemory _viewer = new PlayerMemory();
        readonly List<EditorPage> _stack = new List<EditorPage>();

        readonly Dictionary<int, int> _standIns = new Dictionary<int, int>();
        readonly EditorScreen _screen = new EditorScreen();
        readonly List<ChoiceEntry> _actionBar = new List<ChoiceEntry>();

        bool _structureDirty;
        bool _developerMode;
        bool _toldPasting;
        Shelf _shelf;

        /// <summary>An editor of <paramref name="program" />, saying what it says in <paramref name="texts" />.</summary>
        public SprocketEditor(GridWorld world, Automation program, AutomationRunner runner, Texts texts)
        {
            _world = world;
            _program = program;
            _runner = runner;
            Texts = texts;
            Ago = new Ago(texts);
            Codex = new Terms(texts);

            // What each system says as the editor finds it, so only what changes from here on is dated.
            var systems = _program.Systems;
            for (var i = 0; i < systems.Count; i++)
                _said[systems[i].Identity] = ProgramText.Said(systems[i]);

            _saidEdits = _program.Edits;
            _program.Reshaped += delegate { _reshaped = true; };

            _stack.Add(new HomePage(this));
            Rebuild();
        }

        /// <summary>What to show right now.</summary>
        public EditorScreen Screen => _screen;

        /// <summary>The name the game knows a player by, from their identity, set by whatever shows the editor; null where nothing can say.</summary>
        public Func<long, string> PlayerName { get; set; }

        /// <summary>The blocks the automation could not reach before its construct changed hands and can now, set by whatever keeps it.</summary>
        public IList<string> Returned { get; set; } = new List<string>();

        /// <summary>What the editor says, in the player's language.</summary>
        public Texts Texts { get; }

        /// <summary>The codex, in <see cref="Texts" />.</summary>
        public Terms Codex { get; }

        /// <summary>The codex's window, made the first time the codex is opened, or null before then.</summary>
        public CodexWindow CodexWindow { get; private set; }

        /// <summary>How long ago something happened, in <see cref="Texts" />.</summary>
        public Ago Ago { get; }

        /// <summary>Where the screen showing the editor takes the keyboard and the pointer, or null while nothing shows it.</summary>
        public InputRoot Input { get; set; }

        /// <summary>The windows open over the screen showing the editor, or null while nothing shows it.</summary>
        public WindowLayer Windows { get; set; }

        /// <summary>How many times the screen has been built from its page, which a screen standing still never adds to.</summary>
        public int Rebuilds { get; private set; }

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

        /// <summary>Where the buttons picked and not yet written are let go of, as a pick is backed out of.</summary>
        /// <remarks>Set beside <see cref="Wire" />, and like it, on a copy a client is editing it asks the server.</remarks>
        public Action Unwire { get; set; }

        /// <summary>Whether the player at the screen may change the world's settings now, or null where nobody may.</summary>
        public Func<bool> Administers { get; set; }

        /// <summary>The settings the world runs by, or null where nothing gives them.</summary>
        public Func<Settings> WorldSettings { get; set; }

        /// <summary>Where a change of the world's settings is made, or null where nobody may change them.</summary>
        public Action<Settings> ChangeSettings { get; set; }

        /// <summary>Whether the admin's button belongs on the screen now: the player may change the world's settings.</summary>
        public bool MayAdminister => Administers != null && WorldSettings != null && ChangeSettings != null && Administers();

        /// <summary>Where a change of the unit the player gives lengths of time in is told on, or null where <see cref="Viewer" /> is the world's own.</summary>
        /// <remarks>Set on a copy a client is editing, which tells the server.</remarks>
        public Action Preferred { get; set; }

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

        /// <summary>Who is editing, by player name: the namespace a logic system copied in is made by; empty where nobody is named.</summary>
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
            Stamp();

            if (_structureDirty)
            {
                Rebuild();
                return;
            }

            Current.Refresh();

            // Again after the page, which writes a box the keyboard has just left into the program.
            Stamp();

            // The heading's words include the grid's own name, which the game's interface announces no
            // change to, so they are read again on a cadence - a second, the same cadence the rest of this
            // class already keeps time by. A block renamed is announced, and builds the screen again. A
            // name changed here shows at once, because renaming reads it back the moment it lands;
            // somebody else's shows within a second, which nobody can tell from at once.
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

        /// <summary>
        /// Puts a short question on the screen, to be answered before anything else.
        /// </summary>
        /// <remarks>
        /// For a question that is small enough to hang over the screen.
        /// </remarks>
        public void Ask(string question)
        {
            _screen.Ask(question, false);
        }

        /// <summary>The same, <paramref name="question" /> a template whose named slots take their words from <paramref name="arguments" />.</summary>
        public void Ask(string question, Arguments arguments)
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
        public void AskHere(string question)
        {
            _screen.Ask(question, true);
        }

        /// <summary>The same, <paramref name="question" /> a template whose named slots take their words from <paramref name="arguments" />.</summary>
        void AskHere(string question, Arguments arguments)
        {
            _screen.Ask(question, arguments, true);
        }

        /// <summary>Adds a way to answer, in the order they are offered.</summary>
        /// <remarks>The answer that does what was asked about comes first and the one that does not second, as Yes and No stand in the game's own message boxes.</remarks>
        public void Answer(string text, Action chosen)
        {
            Answer(text, chosen, false);
        }

        /// <summary>An answer that finishes the page on top, the way its own way on does.</summary>
        public void AnswerPrimary(string text, bool byDefault)
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
        public void Answer(string text, Action chosen, bool byDefault)
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

            if (_program.Trouble.Length == 0 && _program.Paused && !(Current is PausedPage))
            {
                Paused();
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

        /// <summary>Says, on every screen but the resume screen, that the automation is paused, with the way back to that screen.</summary>
        void Paused()
        {
            if (_notice == null)
                _notice = new Notice();

            _notice.Leaving = false;
            _notice.Level = NoticeLevel.Warning;
            _notice.Text = Template.Fill(Texts.PausedBanner, "construct", _world.Name);
            _notice.Detail = "";
            _notice.Diagnostic = "";
            _notice.Expanded = false;
            _notice.Expands = null;
            _notice.OfferText = Texts.PausedReview;
            _notice.Offers = Reviews;
        }

        Notice _notice;

        ICommand _reviews;

        ICommand Reviews => _reviews ?? (_reviews = Act(Cmd.ReviewPaused, Review));

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
        /// <para>
        /// The same line undo draws: what is shown, switches, and the library's files change no program here.
        /// </para>
        /// </remarks>
        static bool Changes(Cmd kind)
        {
            switch (kind)
            {
                case Cmd.None:
                case Cmd.Back:
                case Cmd.Answer:
                case Cmd.Expand:
                case Cmd.Resolve:
                case Cmd.Mend:
                case Cmd.Choice:
                case Cmd.OpenSystem:
                case Cmd.OpenSequence:
                case Cmd.OpenTrigger:
                case Cmd.LoadSystem:
                case Cmd.ShowScript:
                case Cmd.OpenCodex:
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
                case Cmd.ReadBlocks:
                case Cmd.TriggerStops:
                case Cmd.TriggerDoes:
                case Cmd.PageOn:
                case Cmd.PageBack:
                case Cmd.ShowOrganized:
                case Cmd.ShowTerminal:
                case Cmd.HiddenBlocks:
                case Cmd.ChooseInstance:
                case Cmd.OpenSlot:
                case Cmd.EnableSystem:
                case Cmd.EnableInstance:
                case Cmd.Shelve:
                case Cmd.Unshelve:
                case Cmd.ForgetBackup:
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
        public List<Field> Fields(IList<long> entityIds, string actionId, Nature form)
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

        /// <summary>The world's backups, which the library lists; null on a machine that does not keep them.</summary>
        public Backups Backups { get; set; }

        /// <summary>The player's library, which keeps blueprints for every world; null on a machine that keeps none.</summary>
        /// <remarks>Each logic system that links to no blueprint is linked, as the library is given, to the one in it of its identity.</remarks>
        public Shelf Shelf
        {
            get { return _shelf; }
            set
            {
                _shelf = value;
                for (var i = 0; value != null && i < _program.Systems.Count; i++)
                    value.Link(_program.Systems[i]);
            }
        }

        /// <summary>How the player likes the screens to look in every world; null on a machine that keeps none.</summary>
        public Preferences Preferences { get; set; }

        /// <summary>How somebody is working in this editor; see <see cref="Shell.Habits" />.</summary>
        /// <remarks>Replaced on a client's copy by the ones its earlier copies of the same block used.</remarks>
        public Habits Habits
        {
            get { return _habits; }
            set { _habits = value ?? new Habits(); }
        }

        /// <summary>The settings in this world of the player at the screen.</summary>
        public PlayerMemory Viewer
        {
            get { return _viewer; }
            set { _viewer = value ?? new PlayerMemory(); }
        }

        /// <summary>The length a new wait or timer starts at: a second, in the unit the player gives lengths in.</summary>
        public Duration NewLength => _viewer.Ticks ? Duration.OfTicks(Duration.PerSecond) : Duration.OfSeconds(1);

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

        /// <summary>The automation being edited.</summary>
        public Automation Program => _program;

        /// <summary>What runs the automation's sequences.</summary>
        public AutomationRunner Runner => _runner;

        /// <summary>The page on top: the screen showing, or the question asked over it.</summary>
        public EditorPage Current => _stack[_stack.Count - 1];

        /// <summary>The screen the questions on top of the stack are asked for: the topmost page that is not one.</summary>
        public EditorPage Opener
        {
            get
            {
                for (var i = _stack.Count - 1; i > 0; i--)
                {
                    if (!_stack[i].Asks)
                        return _stack[i];
                }

                return _stack[0];
            }
        }

        /// <summary>How deep the stack is, for a run of questions to return to.</summary>
        public int Depth => _stack.Count;

        /// <summary>
        /// Drops every page above the given depth, so answering the last question
        /// lands back where the run started rather than walking out through it.
        /// </summary>
        public void UnwindTo(int depth)
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
                case Cmd.Expand:
                case Cmd.OpenSystem:
                case Cmd.OpenSequence:
                case Cmd.OpenSlot:
                case Cmd.PageOn:
                case Cmd.PageBack:
                case Cmd.ShowScript:
                case Cmd.OpenCodex:
                case Cmd.Shelve:
                case Cmd.Unshelve:
                case Cmd.ForgetBackup:
                    return false;
                default:
                    return true;
            }
        }


        /// <summary>Shows <paramref name="page" /> over the current one.</summary>
        public void Push(EditorPage page)
        {
            _stack.Add(page);
            _structureDirty = true;
        }

        /// <summary>Goes back to the page under the current one, where there is one.</summary>
        public void Pop()
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
            // A pick backed out of leaves its buttons unwritten; going on with it picks them again.
            if (Unwire != null)
                Unwire();

            var page = Current;
            if (!page.Leaving())
                return;

            Pop();
            if (page.IsMade && page.Untouched)
                page.Unmake();
        }

        /// <summary>Goes back a page as Done, where the page lets it go, keeping what it was opened on whether or not anything was done to it.</summary>
        /// <remarks>Done says the thing is wanted, so only <see cref="Leave" />, which is Back, takes back something made and left untouched.</remarks>
        public void Finish()
        {
            if (Current.Leaving())
                Pop();
        }

        /// <summary>The shape of the screen has changed and must be rebuilt.</summary>
        public void Invalidate()
        {
            _structureDirty = true;
        }


        /// <summary>What the picks in a blueprint since the last edit do to its slots beyond the names they gave, in order, for the edit made next to carry.</summary>
        readonly List<Reslot> _picked = new List<Reslot>();

        /// <summary>The names those picks gave, which count as named until their edit is made.</summary>
        readonly List<List<string>> _pickedNames = new List<List<string>>();

        /// <summary>What adding a trigger has settled while its blocks were picked, made already, for the step that adds it to carry.</summary>
        readonly List<SystemEdit> _starting = new List<SystemEdit>();

        /// <summary>The copies of button triggers whose start nobody has picked yet, whose lines say why they have a slot of their own.</summary>
        readonly List<Trigger> _copies = new List<Trigger>();

        /// <summary>The trigger the one being added takes the place of, or null; and where it stood.</summary>
        Trigger _replaced;

        int _replacedAt;

        readonly Dictionary<Guid, string> _said = new Dictionary<Guid, string>();

        readonly List<Guid> _gone = new List<Guid>();

        int _saidEdits;

        bool _reshaped;

        /// <summary>
        /// Dates each system whose words have changed since they were last read with the time now; a few comparisons where
        /// nothing has, unless <paramref name="surely" /> asks for every system to be read.
        /// </summary>
        /// <remarks>
        /// Asked on every tick, where the program saying it was edited, reshaped or given another system is what makes it
        /// read anything; and surely at the end of every command and before the program or a blueprint is written out, so a
        /// change made by a way that says nothing is dated by then. A system's words are read without its date, so
        /// switching it, which is not something it says, dates nothing. Known by identity, so a program read again over this
        /// one is compared with what it was. A system met for the first time is dated only where it has no date: one just
        /// made.
        /// </remarks>
        public void Stamp(bool surely = false)
        {
            if (!surely && !_reshaped && _program.Edits == _saidEdits && _program.Systems.Count == _said.Count)
                return;

            _reshaped = false;
            _saidEdits = _program.Edits;

            var now = DateTime.UtcNow;
            var systems = _program.Systems;
            for (var i = 0; i < systems.Count; i++)
            {
                var said = ProgramText.Said(systems[i]);
                string was;
                if (_said.TryGetValue(systems[i].Identity, out was) ? was != said : !systems[i].Changed.HasValue)
                    systems[i].Changed = now;

                _said[systems[i].Identity] = said;
            }

            if (_said.Count == systems.Count)
                return;

            _gone.Clear();
            foreach (var identity in _said.Keys)
            {
                if (_program.FindSystem(identity) == null)
                    _gone.Add(identity);
            }

            for (var i = 0; i < _gone.Count; i++)
                _said.Remove(_gone[i]);
        }

        readonly History<AutomationSystem> _systemHistory = new History<AutomationSystem>();

        readonly History<Automation> _constructHistory = new History<Automation>();

        readonly List<object> _also = new List<object>();

        /// <summary>Makes a change to a sequence and remembers how to take it back.</summary>
        /// <remarks>The only way anything changes a sequence.</remarks>
        public void Do(Sequence sequence, SequenceEdit edit)
        {
            var system = sequence.System;
            Record(system, new OnSequence(sequence, Slotted(system, edit)));
        }

        /// <summary>Makes a change to a system and remembers how to take it back.</summary>
        /// <remarks>The only way anything changes a trigger.</remarks>
        public void Do(AutomationSystem system, SystemEdit edit)
        {
            Record(system, Slotted(system, edit));
        }

        /// <summary>Makes <paramref name="edit" /> to <paramref name="system" /> as the next step of its history.</summary>
        void Record(AutomationSystem system, Edit<AutomationSystem> edit)
        {
            if (!_systemHistory.Did(system, edit))
                return;

            Settled(edit);
        }

        /// <summary>
        /// <paramref name="edit" />, together with what it does to the slots of <paramref name="system" /> where that is a
        /// blueprint: what the pick before it worked out, or what the lists come to once it is made.
        /// </summary>
        Edit<T> Slotted<T>(AutomationSystem system, Edit<T> edit)
        {
            var picked = Unpicked(system);
            var shown = system != null && system.IsBlueprint ? StandIn(system) : null;
            if (shown == null)
                return edit;

            return new Reslotted<T>(edit, picked, delegate { return Slots(system, shown, null).Settle(); });
        }

        /// <summary>Makes a change to the program as a whole, a logic system or an installation made or taken out, and remembers how to take it back.</summary>
        /// <remarks>Walked back from the home screen.</remarks>
        public void Do(ProgramEdit edit)
        {
            if (!_constructHistory.Did(_program, edit))
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

        /// <summary>
        /// Whether there is a change to <paramref name="system" /> on its screen <paramref name="screen" /> to take back now - a
        /// sequence, a slot, an installation or the logic system itself - and not waiting for a switch.
        /// </summary>
        public bool CanUndo(AutomationSystem system, object screen)
        {
            return _systemHistory.CanUndo(system, screen);
        }

        /// <summary>What taking back the last change on <paramref name="screen" /> would undo, in words, with the other screens it takes back changes on.</summary>
        public string UndoLabel(AutomationSystem system, object screen)
        {
            return Label(_systemHistory.Undoes(system, screen), system, screen, true);
        }

        /// <summary>Takes back the last change on <paramref name="screen" />, with every later change it takes along.</summary>
        public void Undo(AutomationSystem system, object screen)
        {
            Walked(_systemHistory.Undo(system, screen));
            Stand(system);
        }

        /// <summary>Whether there is a change on <paramref name="screen" /> taken back to make again.</summary>
        public bool CanRedo(AutomationSystem system, object screen)
        {
            return _systemHistory.CanRedo(system, screen);
        }

        /// <summary>What making the last change taken back on <paramref name="screen" /> again would redo, in words, with the other screens it makes changes on.</summary>
        public string RedoLabel(AutomationSystem system, object screen)
        {
            return Label(_systemHistory.Redoes(system, screen), system, screen, false);
        }

        /// <summary>Makes the last change taken back on <paramref name="screen" /> again, with whatever taken back it leans on.</summary>
        public void Redo(AutomationSystem system, object screen)
        {
            Walked(_systemHistory.Redo(system, screen));
            Stand(system);
        }

        /// <summary>
        /// <paramref name="change" /> in words, and where walking it takes changes on other screens along, back where
        /// <paramref name="back" /> or forward, what those screens are called.
        /// </summary>
        string Label(Change change, AutomationSystem system, object screen, bool back)
        {
            var label = ChangeText.Of(Texts, change);
            _also.Clear();
            _systemHistory.Also(system, screen, back, _also);
            if (_also.Count == 0)
                return label;

            var names = new List<string>();
            for (var i = 0; i < _also.Count; i++)
            {
                var name = ScreenName(_also[i]);
                if (name.Length > 0 && !names.Contains(name))
                    names.Add(name);
            }

            return names.Count == 0 ? label : label + " " + Template.Fill(Texts.UndoAlso, "screens", string.Join(", ", names.ToArray()));
        }

        /// <summary>What the screen of <paramref name="screen" /> is called: the sequence's, slot's, installation's or logic system's name.</summary>
        string ScreenName(object screen)
        {
            var sequence = screen as Sequence;
            if (sequence != null)
                return SequenceText.Called(Texts, sequence);

            var part = screen as Part;
            if (part != null)
                return part.Id;

            var instance = screen as SystemInstance;
            if (instance != null)
                return instance.Name ?? "";

            var system = screen as AutomationSystem;
            return system != null ? system.Name ?? "" : "";
        }

        /// <summary>Goes back to the logic system's screen where an undo or redo on a sequence's screen took that sequence away.</summary>
        void Stand(AutomationSystem system)
        {
            var open = Current as SequencePage;
            if (open == null || !open.IsAbout(system))
                return;

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (sequences[i].Id == open.SequenceId)
                    return;
            }

            Reach(delegate(EditorPage page) { return page is LogicSystemPage && page.IsAbout(system); }, system, null);
        }

        /// <summary>
        /// Shows the page <paramref name="fits" /> picks out: the one open, one further down the way back gone back to, or one
        /// <paramref name="make" /> makes over the logic system's screen.
        /// </summary>
        void Reach(Func<EditorPage, bool> fits, AutomationSystem system, Func<EditorPage> make)
        {
            if (fits(Current))
                return;

            for (var i = _stack.Count - 2; i >= 0; i--)
            {
                if (!fits(_stack[i]))
                    continue;

                _stack.RemoveRange(i + 1, _stack.Count - i - 1);
                _structureDirty = true;
                return;
            }

            // Opened over the logic system's screen, so Back leads to it.
            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                if (!(_stack[i] is LogicSystemPage) || !_stack[i].IsAbout(system))
                    continue;

                _stack.RemoveRange(i + 1, _stack.Count - i - 1);
                break;
            }

            if (!(Current is LogicSystemPage) || !Current.IsAbout(system))
                Push(new LogicSystemPage(this, system));

            if (make != null)
                Push(make());

            _structureDirty = true;
        }

        /// <summary>Whether there is a change to the program as a whole to walk back.</summary>
        public bool CanUndoConstruct()
        {
            return _constructHistory.CanUndo(_program, null);
        }

        /// <summary>What walking back the last change to the program as a whole undoes, as the undo arrow names it.</summary>
        public string UndoLabelConstruct()
        {
            return ChangeText.Of(Texts, _constructHistory.Undoes(_program, null));
        }

        /// <summary>Walks back the last change to the program as a whole.</summary>
        public void UndoConstruct()
        {
            Walked(_constructHistory.Undo(_program, null));
        }

        /// <summary>Whether there is a change to the program as a whole to walk forward again.</summary>
        public bool CanRedoConstruct()
        {
            return _constructHistory.CanRedo(_program, null);
        }

        /// <summary>What walking forward again does, as the redo arrow names it.</summary>
        public string RedoLabelConstruct()
        {
            return ChangeText.Of(Texts, _constructHistory.Redoes(_program, null));
        }

        /// <summary>Walks forward again the last change to the program as a whole that was walked back.</summary>
        public void RedoConstruct()
        {
            Walked(_constructHistory.Redo(_program, null));
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

            // A logic system swapped back or forth comes with its own date, so what is said of it is what it says now, and
            // nothing reads it as changed since.
            var replaced = edit as ReplaceSystem;
            var standing = replaced == null ? null : _program.FindSystem(replaced.Identity);
            if (standing != null)
                _said[standing.Identity] = ProgramText.Said(standing);

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

            Ask(question + "\n\n**{named}**" + (string.IsNullOrEmpty(warning) ? "" : "\n\n" + warning), arguments);
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

            // Another program, so every way back starts again from here.
            _systemHistory.Clear();
            _constructHistory.Clear();

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
            _program.RemoveSystem(system);
            Gone(system);
            _systemHistory.Forget(system);
        }

        /// <summary>Lets go of every page and choice the editor kept about <paramref name="system" />, which has just been taken out of the program.</summary>
        /// <remarks>Its history is kept, so a logic system put back by undo comes back with its own way back.</remarks>
        void Gone(AutomationSystem system)
        {
            var sequences = system.Sequences;
            for (var i = sequences.Count - 1; i >= 0; i--)
                ForgetSequence(sequences[i]);

            _standIns.Remove(system.Id);

            // Nobody is left looking at something that no longer exists.
            for (var i = _stack.Count - 1; i > 0; i--)
            {
                if (_stack[i].IsAbout(system))
                    _stack.RemoveAt(i);
            }

            _structureDirty = true;
        }

        void ForgetSequence(Sequence sequence)
        {
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
            // Every system read, since a command is somebody's press and some of what they change, a name for one, says
            // nothing to the counts the tick goes by.
            Stamp(true);
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
            _screen.Library = page.Library;
            _screen.Help = page.Help != null ? Act(Cmd.OpenCodex, delegate { Helped(page); }) : null;
            _screen.Flip = page.CanSwitch ? Act(Cmd.Switch, delegate { page.Switch(!page.SwitchIsOn); }) : null;
            _screen.SwitchAbout = page.SwitchAbout;

            // Asked as the screen is built: what locks a switch is another page's switch, so it cannot change while this page shows.
            _screen.SwitchLocked = page.SwitchLocked;
            _screen.Runs = page.Runs;
            _screen.Stops = page.Stops;
            _screen.Deletes = page.Deletes;
            _screen.DeleteAbout = page.DeleteAbout;
            _screen.DeleteLocked = page.DeleteLocked;

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

        /// <summary>Says, under the box pasted into, where text from outside the game comes in; the first paste while this editor lives, and no other.</summary>
        /// <remarks>
        /// The game gives a mod no clipboard, and nothing can tell whether the player copied something outside the game, so the
        /// paste itself is the one moment there is to say so.
        /// </remarks>
        public void Pasted()
        {
            if (_toldPasting || Input == null || Input.Focused == null)
                return;

            _toldPasting = true;
            new Popup
            {
                Style = Styles.Menu,
                Anchor = Input.Focused,
                Gap = 4f,
                Placement = Fixed.Wide(360f),
                Content = new Label { Style = Styles.TableTalk, Wraps = true, Text = Texts.WritingPasteNote }
            }.Open();
        }

        /// <summary>Whether the codex's window is open beside the screen.</summary>
        public bool CodexOpen => CodexWindow != null && Windows != null && Windows.IsOpen(CodexWindow);

        /// <summary>What the question mark in a screen's heading does: opens the codex at the screen's page, or closes it where it is open.</summary>
        void Helped(EditorPage page)
        {
            if (CodexOpen)
            {
                Windows.Close(CodexWindow);
                Invalidate();
                return;
            }

            page.ShowHelp();
            Invalidate();
        }

        /// <summary>Opens the codex's window at <paramref name="term" />, beside whatever screen is showing.</summary>
        public void OpenCodex(Term term)
        {
            var open = Act(Cmd.OpenCodex, delegate
            {
                if (CodexWindow == null)
                    CodexWindow = new CodexWindow(this);

                CodexWindow.Go(term);
                if (Windows != null)
                    Windows.Open(CodexWindow);
            });

            if (open != null)
                open.Execute();
        }

        /// <summary>Has every word the editor shows read again in the language now spoken, the codex's included.</summary>
        /// <remarks>An open codex window is made again at the page it showed, since its words were set when it was made.</remarks>
        public void Reword()
        {
            Codex.Reword();

            var codex = CodexWindow;
            if (codex != null)
            {
                CodexWindow = null;
                if (Windows != null && Windows.IsOpen(codex))
                {
                    Windows.Close(codex);
                    if (codex.Term != null)
                        OpenCodex(codex.Term);
                }
            }

            Invalidate();
        }

        /// <summary>
        /// Opens the listing of what this construct's automation is saved as.
        /// </summary>
        /// <remarks>
        /// Offered in <see cref="DeveloperMode" />, and from the notice about a program that would not read. This
        /// does not check either: the check belongs where the way in is put on a screen, so there is one place to
        /// read rather than two that can disagree.
        /// </remarks>
        void ShowScript()
        {
            // Also the way to a program's text that would not read, which is mended there whether or not developer mode is on.
            // Offered from every screen including this one, where it is already lit. Pressing it there
            // means "I am looking at it", not "give me another one on top of the first".
            if (Current is ScriptPage)
                return;

            Push(new ScriptPage(this));
        }

        /// <summary>Opens the library: the world's backups, and the blueprints.</summary>
        public void OpenLibrary()
        {
            Push(new LibraryPage(this, Guid.Empty));
        }

        /// <summary>Opens the library with the blueprint <paramref name="identity" /> chosen.</summary>
        public void OpenLibrary(Guid identity)
        {
            Push(new LibraryPage(this, identity));
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
        public void StartSequence(AutomationSystem system, Trigger trigger)
        {
            _starting.Clear();
            _replaced = null;

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
                    new List<long>(), moment.EventId, Nature.Native,
                    delegate(PickAnswer answer)
                    {
                        // Nothing to record: the sequence does not exist yet, so there is no
                        // history for it and nothing to take back to - which is why the trigger
                        // losing the button here loses it outright, the way the bin on its row is
                        // the way back from making one at all.
                        MomentTrigger made = Starter(system, answer, null, null);
                        Unless(system, made, answer, null, delegate(Occupant taken)
                        {
                            // Settled now, so the questions that follow see it, and recorded, so adding the trigger takes it
                            // all back as one step.
                            if (Shares(taken, system))
                            {
                                made = SignalTrigger.Named(taken.Trigger.Part);
                                _replaced = taken.Trigger;
                                _replacedAt = system.IndexOfTrigger(taken.Trigger);
                                system.ReplaceTrigger(taken.Trigger, null);
                            }
                            else if (taken != null)
                            {
                                Starting(system, TakingOver(null, taken));
                            }

                            var answering = Answering(system, made, answer, null);
                            if (answering != null)
                                Starting(system, answering);

                            _program.Edited();
                            _program.Rewire();

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

            var timer = new TimerPage(this, every.Interval, aim, delegate(Duration interval)
            {
                every.Interval = interval;
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
        public void NewSequence(AutomationSystem system)
        {
            var sequence = _program.AddSequence(system);
            sequence.Name = Name(sequence);
            Do(system, new MakeSequence(_program, sequence, Change.NewSequence));

            Invalidate();
            PushMade(new SequencePage(this, sequence));
        }

        /// <summary>Opens <paramref name="page" /> on something just made and given a name of its own, with that name in hand to be given.</summary>
        /// <remarks>A made-up name, "Sequence 3", is one somebody means to replace, so the box is open for it before anything else is pressed.</remarks>
        void PushNamed(EditorPage page)
        {
            page.NameOnArrival = true;
            Push(page);
        }

        /// <summary>Opens <paramref name="page" /> on something just made, with its name in hand; left without anything done to it, it goes again.</summary>
        public void PushMade(EditorPage page)
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
        /// has gone quiet. Taking a button from another installation empties the signal slot that had it there, which then needs
        /// another; taking it from a trigger in the same installation takes that trigger out and its slot over; either way the
        /// button is written, whatever its slot holds by then.
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
            var signal = made as SignalTrigger;
            if (signal == null || signal.IsWatched)
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
        public List<ToolbarSlot> Pressed(PickAnswer answer)
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
        /// <paramref name="fill" />, asking first where it is a signal part and something else already answers one of the
        /// toolbar slots <paramref name="at" /> it is given.
        /// </summary>
        /// <remarks>
        /// One question at a time. Taking a button empties the signal slot of the blockset that had it; nothing is taken until
        /// every question is answered, and leaving any of them alone leaves the part as it was.
        /// </remarks>
        public void Filling(AutomationSystem system, Part part, SystemInstance chosen, IList<long> blocks, IList<ToolbarSlot> at,
            Func<SystemEdit> fill, Action done)
        {
            Filling(system, part, chosen, blocks, at, fill, done, new List<Occupant>());
        }

        void Filling(AutomationSystem system, Part part, SystemInstance chosen, IList<long> blocks, IList<ToolbarSlot> at,
            Func<SystemEdit> fill, Action done, List<Occupant> taking)
        {
            var trigger = part.IsSignal && at != null ? TriggerOf(system, part.Id) : null;
            var holder = trigger == null ? null : Holding(blocks, at, trigger, chosen, taking);
            if (holder != null)
            {
                AskAbout(holder);
                Answer(Texts.ButtonTakeOver, delegate
                {
                    taking.Add(holder);
                    Filling(system, part, chosen, blocks, at, fill, done, taking);
                });
                Answer(Texts.ButtonLeaveAlone, delegate { }, true);
                return;
            }

            // One step: the fill, and every button it takes from somewhere else.
            var edit = fill();
            for (var i = 0; i < taking.Count; i++)
            {
                if (taking[i].Trigger != null)
                    edit = TakingOver(edit, taking[i]);
            }

            Do(system, edit);
            Wired(blocks);
            done();
        }

        /// <summary>The trigger that is the signal part <paramref name="part" /> of <paramref name="system" />, or null.</summary>
        static SignalTrigger TriggerOf(AutomationSystem system, string part)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var trigger = triggers[i] as SignalTrigger;
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
            public SignalTrigger Trigger;
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
                        var other = triggers[i] as SignalTrigger;
                        if (other == null)
                            continue;

                        var instances = each.Uses;
                        for (var n = 0; n < instances.Count; n++)
                        {
                            var through = instances[n];
                            if ((other == trigger && (through == instance || !each.IsBlueprint))
                                || !Answers(each, through, other, panel.Id, where))
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
        bool Answers(AutomationSystem system, SystemInstance instance, SignalTrigger trigger, long block, ToolbarSlot where)
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
                ? SystemSummary.DescribeInstanceOf(Texts, _runner.Bindings, holder.System, holder.Instance)
                : holder.System.Name);
            AskHere(holder.Trigger.Acts != null ? Texts.ButtonActs : holder.Trigger.Stops ? Texts.ButtonStops : Texts.ButtonStarts,
                arguments);
        }

        /// <summary>Whether <paramref name="taken" /> is another trigger in the installation of <paramref name="system" /> being edited.</summary>
        /// <remarks>
        /// Then the button stays in the slot it is in: the trigger that had it gives that slot up and goes, and the trigger taking
        /// the button takes the slot. Emptying the slot is for another installation, which goes on needing a button of its own.
        /// </remarks>
        bool Shares(Occupant taken, AutomationSystem system)
        {
            return taken != null && taken.Trigger != null && system.IsBlueprint && taken.System == system && taken.Instance == StandIn(system);
        }

        /// <summary>Makes <paramref name="edit" /> now, and keeps it for the step that adds the trigger being made.</summary>
        void Starting(AutomationSystem system, SystemEdit edit)
        {
            edit.Apply(system);
            _starting.Add(edit);
        }

        /// <summary>Does <paramref name="made" />, where there is anything, and takes <paramref name="holder" />'s button, as one edit.</summary>
        SystemEdit TakingOver(SystemEdit made, Occupant holder)
        {
            if (!holder.System.IsBlueprint)
                return new TakeOverTrigger(made, holder.System, holder.Trigger);

            var kept = new List<string>();
            var keptAt = new List<ToolbarSlot>();
            Kept(holder, kept, keptAt);
            return new TakeOver(made, holder.Instance, holder.Trigger.Part, kept, keptAt);
        }

        /// <summary>What answers <paramref name="holder" />'s signal part in its blockset once its toolbar slot is taken out.</summary>
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
        /// <see cref="Vocabulary.Starter" />. In a system blueprint a button names a signal slot, new or, picked again for
        /// <paramref name="keeping" />, that trigger's own; the slot is taken and answered once the question of whether
        /// the buttons are free is answered - see <see cref="Answering" />. In a system it names the variable holding the
        /// first button picked, defined at that answer too. Blocks picked again for a trigger watching <paramref name="replacing" />
        /// keep its slot where they are one block of the kind it expects, as a step's do.
        /// </remarks>
        public MomentTrigger Starter(AutomationSystem system, PickAnswer answer, SignalTrigger keeping, IList<string> replacing)
        {
            // A number on the blocks starts it as it comes to compare as asked.
            if (answer.Form == Nature.Value)
            {
                var given = answer.Arguments;
                var when = new WhenTrigger(answer.Chosen)
                {
                    Provider = Vocabulary.Provider(_world, answer.Targets, answer.Chosen, Nature.Value),
                    Quantifier = answer.Quantifier,
                    Relation = given.Count > 0 ? Comparing.Read(given[0]) : Relation.Is,
                    Value = given.Count > 1 ? given[1] : Literal.Of(0)
                };

                var sources = Named(system, answer.Targets, replacing);
                for (var i = 0; i < sources.Count; i++)
                    when.Sources.Add(sources[i]);

                return when;
            }

            MomentTrigger made = Vocabulary.Starter(
                Vocabulary.EventsOf(_world, answer.Targets), answer.Chosen, answer.Quantifier == Quantifier.All);

            var signal = made as SignalTrigger;
            if (signal == null || signal.IsWatched)
            {
                var named = Named(system, answer.Targets, replacing);
                for (var i = 0; i < named.Count; i++)
                    made.Sources.Add(named[i]);

                return made;
            }

            if (keeping != null && system.IsBlueprint)
                return SignalTrigger.Named(keeping.Part);

            var declared = new List<string>();
            var at = new List<ToolbarSlot>();
            var first = Picked(answer, declared, at);
            if (first == null)
                return made;

            if (!system.IsBlueprint)
                return SignalTrigger.Named(SignalName(declared[0], at[0]));

            if (StandIn(system) == null)
                return made;

            var called = declared.Count == 1 ? Called(first) : first.Kind.DisplayName;
            return SignalTrigger.Named(system.Free(called));
        }

        /// <summary>
        /// What the variable holding slot <paramref name="at" /> on the block <paramref name="block" /> names is called: its
        /// own name where there is one, and the block's with the slot's number after it otherwise.
        /// </summary>
        string SignalName(string block, ToolbarSlot at)
        {
            var held = _program.FindSignalRefOnly(block, at);
            return held != null
                ? held.Name
                : _program.Free(block + " " + at.Slot.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// The edit that answers the signal part <paramref name="made" /> names in the blockset shown with the buttons
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
            var required = made as SignalTrigger;
            if (required == null || required.IsWatched)
                return null;

            var declared = new List<string>();
            var at = new List<ToolbarSlot>();
            var first = Picked(answer, declared, at);
            if (first == null)
                return null;

            if (!system.IsBlueprint)
            {
                _program.DeclareSignal(declared[0], at[0], required.Part);
                return then;
            }

            var instance = StandIn(system);
            if (instance == null)
                return null;

            return new AnswerSignal(then, system, instance, required.Part, Part.Nearest(new[] { first.Kind.Lineage }), declared, at);
        }

        /// <summary>The blocks <paramref name="answer" /> picked, as the construct declares them, each with the toolbar slot its press is answered on; the first of them, or null.</summary>
        public GridEntity Picked(PickAnswer answer, List<string> declared, List<ToolbarSlot> at)
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
                Do(system, new AddTrigger(_program, trigger, made, _starting, _replaced, _replacedAt));
                _starting.Clear();
                _replaced = null;
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
                act.Provider = Vocabulary.Provider(_world, answer.Targets, answer.Chosen, answer.Form);
                var named = Named(system, answer.Targets, fresh || trigger.Acts == null ? null : trigger.Acts.Targets);
                for (var i = 0; i < named.Count; i++)
                    act.Targets.Add(named[i]);

                for (var i = 0; i < answer.Arguments.Count; i++)
                    act.Arguments.Add(answer.Arguments[i]);

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
                        arguments: only.Arguments, opens: aim.Opens, after: aim.Pick);
                }

                aim.Following.Arrived(aim.Straight);
                aim.Straight = false;
                Push(aim.Following);
                return;
            }

            if (aim.Following == null)
            {
                aim.Following = new PickPage(this, Texts.PickSingleTask, false, new List<long>(), "", Nature.Native,
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
        /// A system names the program's variables, and a blueprint its own slots: so each trigger it is started by becomes a
        /// signal slot, called what the block is called now, and the blocks its steps and triggers name become slots by the
        /// sets they name, as <see cref="Slotting" /> works them out. The first instance answers them with those variables.
        /// </remarks>
        public SystemInstance Blueprinted(AutomationSystem system)
        {
            if (system.IsBlueprint)
                return system.First;

            var name = SystemSummary.NewInstanceName(Texts, system);
            var first = system.AddInstance();
            first.Name = name;
            system.IsBlueprint = true;

            var slots = new Dictionary<string, string>();
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var needed = triggers[i] as SignalTrigger;
                var declared = needed == null ? null : _program.FindSignalRef(needed.Part);
                if (declared == null || slots.ContainsKey(needed.Part))
                    continue;

                GridEntity panel = _runner.Bindings.Entity(declared.Name);
                var slot = system.Free(panel == null ? declared.Name : Called(panel));
                var part = system.PartFor(slot);
                part.IsSignal = true;

                var at = new ToolbarSlot[declared.Blocks.Count];
                for (var n = 0; n < at.Length; n++)
                    at[n] = declared.At;

                first.Fill(slot, declared.Blocks, at);

                slots[needed.Part] = slot;
            }

            for (var i = 0; i < triggers.Count; i++)
            {
                var moment = triggers[i] as MomentTrigger;
                if (moment != null)
                    Slotted(moment.Sources, slots);
            }

            // The blocks the steps and triggers name become slots as any picks do, the smallest sets first.
            Slots(system, first, null).Settle().Apply();

            _program.Edited();
            _program.Rewire();
            return first;
        }

        /// <summary>Names the signal slot for each variable in <paramref name="names" /> that <paramref name="slots" /> has one for.</summary>
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
        /// construct has not named it yet, or the slots a blueprint's stand-in fills with them.
        /// </summary>
        /// <remarks>
        /// Where picking a block turns into naming one. What a blueprint's sentence stores is a slot, and which block fills
        /// it belongs to each instance - so the same sentence acts on the port airlock's door in one and the starboard's in
        /// another.
        /// </remarks>
        public List<string> Named(AutomationSystem system, IList<long> blocks)
        {
            return Named(system, blocks, null);
        }

        /// <summary>The same, for blocks picked in place of the names <paramref name="replacing" />, the list itself, where they replace one.</summary>
        /// <remarks>
        /// In a blueprint the slots are worked out by <see cref="Slotting" />, and what that changes beyond the names given
        /// back is kept for the edit made next, which carries it so taking the edit back takes it back too.
        /// </remarks>
        public List<string> Named(AutomationSystem system, IList<long> blocks, IList<string> replacing)
        {
            var declared = new List<string>();
            var met = new Dictionary<string, GridEntity>();
            for (var i = 0; i < blocks.Count; i++)
            {
                GridEntity entity = _world.FindEntity(blocks[i]);
                if (entity == null)
                    continue;

                var name = _program.Declare(_world, entity).Name;
                declared.Add(name);
                met[name] = entity;
            }

            SystemInstance shown = system.IsBlueprint ? StandIn(system) : null;
            if (shown == null)
                return declared;

            // Worked out as though the picks before it in this command were made, and undone again, since their edits come later.
            if (_picked.Count > 0 && _picked[0].System != system)
                Unpicked(null);

            for (var i = 0; i < _picked.Count; i++)
                _picked[i].Apply();

            Reslot reslot;
            var named = Slots(system, shown, met).Pick(declared, replacing, _pickedNames, out reslot);

            for (var i = _picked.Count - 1; i >= 0; i--)
                _picked[i].Undo();

            _picked.Add(reslot);
            _pickedNames.Add(new List<string>(named));
            return named;
        }

        /// <summary>Whether the picks since the last edit change any slot of <paramref name="system" />.</summary>
        bool Reslots(AutomationSystem system)
        {
            for (var i = 0; i < _picked.Count; i++)
            {
                if (_picked[i].System == system && !_picked[i].IsEmpty)
                    return true;
            }

            return false;
        }

        /// <summary>The changes the picks since the last edit worked out for <paramref name="system" />, in order, forgetting them; none where they were for another.</summary>
        List<Reslot> Unpicked(AutomationSystem system)
        {
            var picked = new List<Reslot>();
            if (_picked.Count > 0 && _picked[0].System == system)
                picked.AddRange(_picked);

            _picked.Clear();
            _pickedNames.Clear();
            return picked;
        }

        /// <summary>Works out the slots of <paramref name="system" /> as <paramref name="shown" /> fills them, knowing the blocks just picked as <paramref name="met" />.</summary>
        Slotting Slots(AutomationSystem system, SystemInstance shown, Dictionary<string, GridEntity> met)
        {
            return new Slotting(system, shown,
                delegate(string name)
                {
                    GridEntity entity = Known(name, met);
                    return entity != null ? Called(entity) : _program.Called(name, _world.Values);
                },
                delegate(string name)
                {
                    GridEntity entity = Known(name, met);
                    return entity != null ? entity.Kind.Lineage : new string[0];
                },
                delegate(int number) { return Template.Fill(Texts.SlotNumbered, "number", number.ToString(System.Globalization.CultureInfo.InvariantCulture)); });
        }

        /// <summary>The block the variable <paramref name="name" /> holds: one just picked, or one the construct has bound.</summary>
        GridEntity Known(string name, Dictionary<string, GridEntity> met)
        {
            GridEntity entity;
            if (met != null && met.TryGetValue(name, out entity))
                return entity;

            return _runner.Bindings.Entity(name);
        }

        /// <summary>
        /// What these names in <paramref name="system" /> come to as its stand-in fills them: the
        /// blocks, as the construct declares them.
        /// </summary>
        /// <remarks>A name that is not a part, or a part the stand-in leaves empty, comes through as it is.</remarks>
        public List<string> Seen(AutomationSystem system, IList<string> names)
        {
            return SystemInstance.Through(StandIn(system), names);
        }

        /// <summary>
        /// The instance of <paramref name="system" /> whose blocks the editor shows, and picks blocks
        /// through: the one last chosen, or the first.
        /// </summary>
        public SystemInstance StandIn(AutomationSystem system)
        {
            if (system == null)
                return null;

            int number;
            SystemInstance chosen = _standIns.TryGetValue(system.Id, out number) ? system.FindInstance(number) : null;
            return chosen ?? system.First;
        }

        /// <summary>Gives <paramref name="system" /> slots and a first installation where it has none: a logic system keeps at least one.</summary>
        /// <remarks>The script still reads a system without slots, or a system blueprint with no installation, from an older save or the script page.</remarks>
        public void Installed(AutomationSystem system)
        {
            if (system == null)
                return;

            if (!system.IsBlueprint)
            {
                Blueprinted(system);
                return;
            }

            if (system.Instances.Count > 0)
                return;

            var name = SystemSummary.NewInstanceName(Texts, system);
            system.AddInstance().Name = name;
            _program.Edited();
            _program.Rewire();
        }

        /// <summary>Adds another installation of <paramref name="system" />, named by the first number free, and shows the system through it.</summary>
        public SystemInstance Another(AutomationSystem system)
        {
            var name = SystemSummary.NewInstanceName(Texts, system);
            SystemInstance made = system.AddInstance();
            made.Name = name;
            Do(new MakeInstallation(system, made));
            Choose(system, made);
            return made;
        }

        /// <summary>Shows <paramref name="system" /> through <paramref name="instance" />'s blocks from now on.</summary>
        public void Choose(AutomationSystem system, SystemInstance instance)
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

        /// <summary>
        /// Takes a trigger out of a system.
        /// </summary>
        /// <remarks>
        /// No question asked, because the system's arrows are the answer to a deletion nobody meant -
        /// and a question in front of every one of them is how people learn to press through
        /// questions. A sequence deleted from its own screen still asks: that is work somebody can
        /// see, and its way back would be on a screen that went with it.
        /// </remarks>
        public void Forget(AutomationSystem system, Trigger trigger)
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
        public void ChangeTrigger(AutomationSystem system, Trigger trigger, Opening opens, bool action)
        {
            // Back to the screen the row is on, which is where the answer lands.
            var origin = _stack.Count;
            var aim = new Aim(this, system, trigger);
            aim.Opens = action ? opens : Opening.Blocks;
            aim.Straight = action;

            var every = trigger as EveryTrigger;
            if (every != null)
            {
                var rate = every.Interval;
                var timer = new TimerPage(this, rate, aim, delegate(Duration interval)
                {
                    Aimed(system, every, aim, origin, false, interval == rate ? null : new SetCadence(every, rate, interval));
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

            // A when comparing a number opens on that number and how it compares; anything else on its moment.
            var compares = was != null && Vocabulary.Compares(_runner.Bindings, was, SystemInstance.Through(StandIn(system), was.Sources));
            var picker = new PickPage(this, "When something happens", true,
                Blocks(system, moment.Sources), compares ? was.EventId : Vocabulary.EventOf(_program, _runner.Bindings, system, moment, StandIn(system)),
                compares ? Nature.Value : Nature.Native,
                delegate(PickAnswer answer)
                {
                    // The first pick on a copy is the answer its line was asking for.
                    _copies.Remove(moment);

                    var keeping = moment as SignalTrigger;
                    if (keeping != null && keeping.IsWritten)
                        keeping = null;

                    MomentTrigger made = Starter(system, answer, keeping, moment.Sources);
                    Unless(system, made, answer, moment, delegate(Occupant taken)
                    {
                        if (Shares(taken, system))
                        {
                            var taking = SignalTrigger.Named(taken.Trigger.Part);
                            var moved = new ChangeTrigger(moment, taking);
                            Aimed(system, taking, aim, origin, false,
                                new TakeOverTrigger(Answering(system, taking, answer, moved) ?? moved, system, taken.Trigger));
                            return;
                        }

                        // A button picked again for a button trigger answers the same signal part, so the trigger is kept:
                        // its toolbar slots name it.
                        var answered = made as SignalTrigger;
                        if (keeping != null && answered != null && !answered.IsWritten && system.IsBlueprint)
                        {
                            var refill = Answering(system, made, answer, null);
                            if (refill != null)
                                Aimed(system, moment, aim, origin, false, taken == null ? refill : TakingOver(refill, taken));
                            else
                                Aimed(system, moment, aim, origin, false, null);

                            return;
                        }

                        // A trigger left as it was by a block traded within its slot still changes that slot.
                        var change = new ChangeTrigger(moment, made);
                        if (!change.Changes && taken == null && !Reslots(system))
                        {
                            Aimed(system, moment, aim, origin, false, null);
                            return;
                        }

                        // One edit either way, because it is one answer to one question.
                        var edit = Answering(system, made, answer, change) ?? change;
                        Aimed(system, made, aim, origin, false, taken == null ? edit : TakingOver(edit, taken));
                    }, delegate { UnwindTo(origin); });
                },
                true, was == null ? Quantifier.Any : was.Quantifier, compares ? new[] { Comparing.Of(was.Relation), was.Value } : null,
                opens: action ? Opening.Nothing : opens, aim: aim);

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
        public List<long> Blocks(AutomationSystem system, IList<string> names)
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
        public void StopNow(Sequence sequence)
        {
            if (Elsewhere != null)
            {
                Elsewhere(sequence, StandIn(sequence.System), true);
                return;
            }

            SequenceRunner state = _runner.Get(sequence, StandIn(sequence.System));
            if (state != null)
                state.Interrupt(Stopper.Hand, 0);
        }

        /// <summary>Runs <paramref name="sequence" /> by hand, at once, where it is or on the construct <see cref="Elsewhere" /> runs it on.</summary>
        public void RunNow(Sequence sequence)
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

        /// <summary>Takes a sequence out of its logic system, with the triggers that run it, as a step its undo takes back.</summary>
        /// <remarks>
        /// Without asking, as deleting a trigger is: undo brings it back, and a question in front of a delete that can be taken
        /// back is one people learn to press through. From the sequence's own screen or its row on its logic system's; whatever
        /// is showing it goes when it does, which is what <see cref="ForgetSequence" /> is for.
        /// </remarks>
        public void DeleteSequence(Sequence sequence)
        {
            Do(sequence.System, new TakeSequenceOut(_program, sequence));
            ForgetSequence(sequence);
        }

        /// <summary>Remembers that the player gives lengths of time in <paramref name="seconds" />, or in ticks.</summary>
        public void Prefer(bool seconds)
        {
            if (_viewer.Ticks == !seconds)
                return;

            _viewer.Ticks = !seconds;
            if (Preferred != null)
                Preferred();
        }

        /// <summary>Shows the editor as it opens: on the resume screen while the automation is paused.</summary>
        public void Opened()
        {
            // Looked at, worded and counted at once, however soon after it last closed, so an opened screen always starts from
            // what is there. While it stays open the runner looks again whenever the blocks or the program change.
            _runner.Look();
            _worded.Restart();
            Current.Resay();

            if (_program.Paused)
                Review();
        }

        /// <summary>Shows the resume screen over the home screen.</summary>
        public void Review()
        {
            UnwindTo(1);
            Push(new PausedPage(this));
        }

        /// <summary>Starts paused automation again as it was, and goes on to the home screen.</summary>
        public void Resume()
        {
            _program.PausedFrom.Clear();
            UnwindTo(1);
        }

        /// <summary>Keeps the automation paused and goes on to the home screen, whose banner leads back.</summary>
        public void LeavePaused()
        {
            UnwindTo(1);
        }

        /// <summary>Asks before taking every logic system out of paused automation; each one comes back with undo on the home screen.</summary>
        public void RemoveAll()
        {
            var systems = new List<AutomationSystem>(_program.Systems);
            var question = Template.Fill(systems.Count == 1 ? Texts.PausedRemoveOneQuestion : Texts.PausedRemoveQuestion, "construct",
                _world.Name);
            Ask(Template.Fill(question, "count", systems.Count.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Answer(Texts.PausedRemoveAll, delegate
            {
                for (var i = 0; i < systems.Count; i++)
                    DeleteSystem(systems[i]);

                _program.PausedFrom.Clear();
                UnwindTo(1);
            });
            Answer(Texts.ButtonLeaveAlone, delegate { }, true);
        }

        /// <summary>Takes <paramref name="system" /> out of the program, with its sequences, its triggers and its installations, as a step home's undo takes back.</summary>
        /// <remarks>Without asking: undo brings it back, switched off, with its own way back.</remarks>
        public void DeleteSystem(AutomationSystem system)
        {
            Do(new TakeSystemOut(system, Change.DeleteSystem));
            Gone(system);
        }

        /// <summary>Puts the blueprint <paramref name="system" /> in the library as it is now.</summary>
        public void Shelve(AutomationSystem system)
        {
            Stamp(true);
            Shelf.Keep(system);
            Invalidate();
        }

        /// <summary>Asks before deleting <paramref name="volume" /> from the library.</summary>
        public void Unshelve(Volume volume)
        {
            Deleting(Texts.LibraryForgetQuestion, volume.State == VolumeState.Read ? volume.Name : volume.File, Texts.DeleteFinal, null, delegate
            {
                Shelf.Forget(volume);
                Invalidate();
            });
        }

        /// <summary>Asks before forgetting <paramref name="backup" />, a logic system kept of a construct that lost its automation.</summary>
        public void ForgetBackup(Backup backup)
        {
            Deleting(Texts.LibraryForgetBackupQuestion, backup.Name, Texts.DeleteFinal, null, delegate
            {
                Backups.Forget(backup);
                Invalidate();
            });
        }

        /// <summary>
        /// Puts the blueprint <paramref name="volume" /> from the library on the construct as a logic system of its own, linked
        /// to it, with its first installation, answering what was wrong, or empty.
        /// </summary>
        /// <remarks>Refused where a logic system made from it is here already: another of the same is another installation of that one.</remarks>
        public string PutOn(Volume volume)
        {
            if (_program.MadeFrom(volume.Identity) != null)
                return Texts.LibraryOnAlready;

            AutomationSystem system;
            var trouble = ProgramText.Lone(volume.Text, _world.Values, out system);
            if (trouble.Length > 0)
                return trouble;

            system.Identity = Guid.NewGuid();
            system.Origin = volume.Identity;
            system.OriginChanged = volume.Changed;
            return PutOn(ProgramText.Linked(system), system.Identity);
        }

        /// <summary>
        /// Puts the logic system <paramref name="backed" />, as a backup keeps it, back on the construct with its own identity
        /// and its link, with its first installation, answering what was wrong with it, or empty.
        /// </summary>
        /// <remarks>Refused where a logic system of its identity is here already.</remarks>
        public string PutOn(AutomationSystem backed)
        {
            if (Shelf != null)
                Shelf.Link(backed);

            return PutOn(ProgramText.Linked(backed), backed.Identity);
        }

        string PutOn(string text, Guid identity)
        {
            var trouble = ProgramText.AddBlueprint(text, _program, _world.Values);
            if (trouble.Length > 0)
                return trouble;

            var made = _program.FindSystem(identity);
            Installed(made);
            Do(new MakeSystem(made, Change.PutOnSystem));
            Invalidate();
            return "";
        }

        /// <summary>
        /// Puts a copy of the logic system written as <paramref name="text" /> on the construct, as a logic system of its own with
        /// its first installation, answering it; or null, with what was wrong in <paramref name="trouble" />.
        /// </summary>
        /// <remarks>
        /// A blueprint of its own because it has an identity of its own: changing either never touches the other. It is named
        /// with the first number no blueprint here or in the library has, and is by whoever made the copy.
        /// </remarks>
        public AutomationSystem Copy(string text, out string trouble)
        {
            var trial = new Automation();
            trouble = ProgramText.Load(text, trial, _world.Values);
            if (trouble.Length > 0)
                return null;

            if (trial.Systems.Count != 1)
            {
                trouble = "expected one system blueprint and nothing else.";
                return null;
            }

            var copy = trial.Systems[0];
            copy.Identity = Guid.NewGuid();
            copy.Changed = DateTime.UtcNow;
            copy.Name = Numbered(copy.Name);
            if (_author.Length > 0)
                copy.Namespace = _author;

            trouble = ProgramText.AddBlueprint(ProgramText.Blueprint(copy), _program, _world.Values);
            if (trouble.Length > 0)
                return null;

            var made = _program.FindSystem(copy.Identity);
            Installed(made);
            Do(new MakeSystem(made, Change.CopyBlueprint));
            Invalidate();
            return made;
        }

        /// <summary><paramref name="name" /> with the first number from 2 that no system here and no blueprint in the library is called.</summary>
        string Numbered(string name)
        {
            for (var number = 2; ; number++)
            {
                // The number first, so a name with braces in it is never read as a slot.
                var wanted = Template.Fill(Template.Fill(Texts.LibraryCopyName, "number",
                    number.ToString(System.Globalization.CultureInfo.InvariantCulture)), "name", name);

                var taken = false;
                for (var i = 0; i < _program.Systems.Count && !taken; i++)
                    taken = _program.Systems[i].Name == wanted;

                for (var i = 0; Shelf != null && i < Shelf.All.Count && !taken; i++)
                    taken = Shelf.All[i].Name == wanted;

                if (!taken)
                    return wanted;
            }
        }

        /// <summary>Whether the library keeps the blueprint the logic system <paramref name="system" /> is linked to.</summary>
        public bool Stored(AutomationSystem system)
        {
            return Shelf != null && system.HasOrigin && Shelf.Find(system.Origin) != null;
        }

        /// <summary>Asks before taking the logic system <paramref name="system" /> off the construct; keeping it is the answer Enter takes.</summary>
        /// <remarks>
        /// Nothing is lost either way, but a press that landed on the wrong minus should not cost the work of putting it back.
        /// Where the logic system was changed after its blueprint in the library, a second question asks whether the blueprint
        /// is brought up to date; otherwise the blueprint is left as it is, unasked.
        /// </remarks>
        public void AskTakeOff(AutomationSystem system)
        {
            Stamp(true);

            var arguments = new Arguments();
            arguments.Set("named", system.Name);

            if (!ChangedSince(system))
            {
                Ask(Texts.LibraryTakeOffQuestion + "\n\n**{named}**\n\n" + Texts.LibraryTakeOffKept, arguments);
                Answer(Texts.LibraryTakeOffKeep, delegate { }, true);
                Answer(Texts.LibraryTakeOffConfirm, delegate { TakeOff(system, false); });
                return;
            }

            Ask(Texts.LibraryTakeOffQuestion + "\n\n**{named}**\n\n" + Texts.LibraryTakeOffGoes, arguments);
            Answer(Texts.LibraryTakeOffKeep, delegate { }, true);
            Answer(Texts.LibraryTakeOffConfirm, delegate { AskTakeOffBlueprint(system); });
        }

        /// <summary>
        /// Asks whether taking off the logic system <paramref name="system" /> brings its blueprint in the library up to date;
        /// leaving the blueprint is the answer Enter takes, and backing out takes nothing off.
        /// </summary>
        void AskTakeOffBlueprint(AutomationSystem system)
        {
            var arguments = new Arguments();
            arguments.Set("named", system.Name);

            Ask(Texts.LibraryTakeOffChanged, arguments);
            Answer(Texts.LibraryTakeOffUpdate, delegate { TakeOff(system, true); });
            Answer(Texts.LibraryTakeOffLeave, delegate { TakeOff(system, false); }, true);
        }

        /// <summary>Asks before putting the logic system <paramref name="system" /> in place of its blueprint in the library; keeping the blueprint is the answer Enter takes.</summary>
        public void AskUpdate(AutomationSystem system)
        {
            Stamp(true);

            var arguments = new Arguments();
            arguments.Set("named", system.Name);

            Ask(Texts.LibraryUpdateQuestion + "\n\n**{named}**\n\n" + Texts.LibraryUpdateReplaced, arguments);
            Answer(Texts.LibraryUpdateKeep, delegate { }, true);
            Answer(Texts.LibraryUpdateConfirm, delegate { Shelve(system); });
        }

        /// <summary>
        /// Puts the logic system <paramref name="backed" />, as a backup keeps it, in the library: where the blueprint it is
        /// linked to is there, after asking whether it updates that blueprint or is kept as a new one; leaving both is the
        /// answer Enter takes.
        /// </summary>
        public void ShelveBackup(AutomationSystem backed)
        {
            if (!Stored(backed))
            {
                Shelve(backed);
                return;
            }

            Stamp(true);

            var arguments = new Arguments();
            arguments.Set("named", backed.Name);

            Ask(Texts.LibraryKeepBackupQuestion + "\n\n**{named}**\n\n" + Texts.LibraryKeepBackupLinked, arguments);
            Answer(Texts.LibraryKeepBackupLeave, delegate { }, true);
            Answer(Texts.LibraryKeepBackupUpdate, delegate { Shelve(backed); });
            Answer(Texts.LibraryKeepBackupNew, delegate
            {
                backed.Origin = Guid.Empty;
                backed.OriginChanged = null;
                Shelve(backed);
            });
        }

        /// <summary>Asks before putting the blueprint of <paramref name="system" /> in the library in its place; keeping it is the answer Enter takes.</summary>
        public void AskRenew(AutomationSystem system)
        {
            Stamp(true);

            var arguments = new Arguments();
            arguments.Set("named", system.Name);

            Ask(Texts.LibraryRenewQuestion + "\n\n**{named}**\n\n" + Texts.LibraryRenewReplaced, arguments);
            Answer(Texts.LibraryRenewKeep, delegate { }, true);
            Answer(Texts.LibraryRenewConfirm, delegate { Renew(system, Change.UpdateFromLibrary); });
        }

        /// <summary>Asks before putting the blueprint of <paramref name="system" /> in the library back in its place, losing what changed since; keeping it is the answer Enter takes.</summary>
        public void AskReset(AutomationSystem system)
        {
            Stamp(true);

            var arguments = new Arguments();
            arguments.Set("named", system.Name);

            Ask(Texts.LibraryResetQuestion + "\n\n**{named}**\n\n" + Texts.LibraryResetReplaced, arguments);
            Answer(Texts.LibraryRenewKeep, delegate { }, true);
            Answer(Texts.LibraryResetConfirm, delegate { Renew(system, Change.ResetToBlueprint); });
        }

        /// <summary>
        /// Puts the blueprint <paramref name="system" /> is linked to in its place, its installations keeping their blocks in
        /// every slot the blueprint still uses, offered back as <paramref name="change" />, answering what was wrong, or empty.
        /// </summary>
        /// <remarks>
        /// It keeps its own identity, so its buttons go on naming it, and is linked to the blueprint as it now is. Its runs end,
        /// as they do when a logic system goes, and each installation stays switched as it was. The whole program as it would
        /// stand is read first, because a text with an error in it is trusted in no part.
        /// </remarks>
        public string Renew(AutomationSystem system, Change change)
        {
            var volume = Stored(system) ? Shelf.Find(system.Origin) : null;
            if (volume == null)
                return "";

            AutomationSystem copy;
            var trouble = ProgramText.Lone(volume.Text, _world.Values, out copy);
            if (trouble.Length > 0)
                return trouble;

            copy.Identity = system.Identity;
            copy.Origin = volume.Identity;
            copy.OriginChanged = volume.Changed;

            var text = ProgramText.Replacing(_program, _world.Values, copy);
            trouble = ProgramText.Load(text, new Automation(), _world.Values);
            if (trouble.Length > 0)
                return trouble;

            Stamp(true);

            // Read in beside the rest of the program, then set aside, so that the edit is the swap of the two alone.
            var at = 0;
            while (at < _program.Systems.Count && _program.Systems[at] != system)
                at++;

            _program.RemoveSystem(system);
            trouble = ProgramText.ReadOne(text, system.Identity, _program);
            var fresh = _program.FindSystem(system.Identity);
            if (fresh != null)
                _program.RemoveSystem(fresh);

            _program.PutBack(system, at);
            if (trouble.Length > 0 || fresh == null)
                return trouble;

            fresh.Enabled = system.Enabled;
            var instances = fresh.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                var was = system.FindInstance(instances[i].Number);
                if (was != null)
                    instances[i].Enabled = was.Enabled;
            }

            // Said as it is, so its date stays the library's and nothing reads it as changed since.
            _said[fresh.Identity] = ProgramText.Said(fresh);

            Do(new ReplaceSystem(system, fresh, change));
            Gone(system);
            return "";
        }

        /// <summary>Whether the logic system <paramref name="system" /> was changed after the version of its blueprint in the library it is linked to.</summary>
        public bool ChangedSince(AutomationSystem system)
        {
            return Stored(system) && system.Changed.HasValue
                && (!system.OriginChanged.HasValue || system.Changed.Value > system.OriginChanged.Value);
        }

        /// <summary>
        /// Takes the logic system <paramref name="system" />, whose blueprint is in the library, off the construct with its
        /// installations, bringing the blueprint up to date first where <paramref name="update" /> says so.
        /// </summary>
        /// <remarks>One with no blueprint in the library is deleted instead, by <see cref="DeleteSystem" />: taking it off would lose it.</remarks>
        public void TakeOff(AutomationSystem system, bool update)
        {
            if (!Stored(system))
                return;

            Stamp(true);
            if (update && !Shelf.Holds(system))
                Shelf.Keep(system);

            Do(new TakeSystemOut(system, Change.TakeOffSystem));
            Gone(system);
        }

        /// <summary>Takes <paramref name="instance" />, one of the installations of <paramref name="system" />, out of it, as a step home's undo takes back.</summary>
        /// <remarks>
        /// Without asking: undo brings it back, switched off, with its blocks. A logic system keeps at least one installation, so
        /// its only one is never taken out: the logic system goes instead.
        /// </remarks>
        public void DeleteInstance(AutomationSystem system, SystemInstance instance)
        {
            if (system.Instances.Count <= 1)
                return;

            Do(new TakeInstallationOut(system, instance));
            _standIns.Remove(system.Id);
        }

        /// <summary>Switches <paramref name="system" /> on or off; a blueprint off only after asking, since that stops every logic system made from it.</summary>
        public void SwitchSystem(AutomationSystem system, bool on)
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

        /// <summary>Switches <paramref name="instance" /> of <paramref name="system" /> on or off, while the system is on.</summary>
        /// <remarks>
        /// An installation keeps its own setting while its blueprint is off, so the blueprint switched back on starts exactly
        /// the ones that were on; its switch cannot be pressed meanwhile.
        /// </remarks>
        public void SwitchInstance(AutomationSystem system, SystemInstance instance, bool on)
        {
            if (!system.Enabled)
                return;

            // Switched on, it forgets why it was switched off, which an open screen says at once.
            instance.Enabled = on;
            _runner.Revise();
        }

        /// <summary>What pressing the switch of <paramref name="system" /> would do, said as a note on it.</summary>
        public string SwitchAbout(AutomationSystem system)
        {
            if (!system.IsBlueprint)
                return StatusRow.Switching(Texts, true, system.Enabled);

            return system.Enabled ? Texts.RowSwitchBlueprintOff : Texts.RowSwitchBlueprintOn;
        }

        /// <summary>Takes back the last change to <paramref name="sequence" /> as though it was never made, where it is the last change to its system.</summary>
        /// <remarks>For a step added a moment ago and left at once: leaving it is a cancel, not a step to walk.</remarks>
        public bool Withdraw(Sequence sequence)
        {
            var system = sequence.System;
            var last = system == null ? null : _systemHistory.Last(system) as OnSequence;
            if (last == null || last.Sequence != sequence || !_systemHistory.Withdraw(system, last))
                return false;

            Settled(last);
            return true;
        }

        /// <summary>Takes <paramref name="sequence" /> out of the program without asking, and any page showing it.</summary>
        /// <remarks>For one made a moment ago and left untouched: the step that made it is withdrawn rather than walked back.</remarks>
        public void TakeOut(Sequence sequence)
        {
            var system = sequence.System;
            var last = system == null ? null : _systemHistory.Last(system);
            var slotted = last as Reslotted<AutomationSystem>;
            var made = (slotted != null ? slotted.Edit : last) as MakeSequence;
            if (made == null || made.Sequence != sequence || !_systemHistory.Withdraw(system, last))
                _program.RemoveSequence(sequence);

            ForgetSequence(sequence);
            _program.Rewire();
        }

        /// <summary>Takes <paramref name="system" /> out of the program without asking, with everything in it and any page showing it.</summary>
        /// <remarks>For one made a moment ago and left untouched: the step that made it is withdrawn rather than walked back.</remarks>
        public void TakeOut(AutomationSystem system)
        {
            var made = _constructHistory.Last(_program) as MakeSystem;
            if (made == null || made.System != system || !_constructHistory.Withdraw(_program, made))
            {
                Drop(system);
                return;
            }

            Gone(system);
            _systemHistory.Forget(system);
            _program.Rewire();
        }

        /// <summary>Takes <paramref name="instance" /> out of <paramref name="system" /> without asking.</summary>
        /// <remarks>For one made a moment ago that was left without anything done to it: the step that made it is withdrawn.</remarks>
        public void TakeOut(AutomationSystem system, SystemInstance instance)
        {
            var made = _constructHistory.Last(_program) as MakeInstallation;
            if (made == null || made.Instance != instance || !_constructHistory.Withdraw(_program, made))
                system.RemoveInstance(instance);

            _standIns.Remove(system.Id);
            _program.Rewire();
            _structureDirty = true;
        }

        /// <summary>
        /// Another sequence saying what this one says, opened for whatever is to be different.
        /// </summary>
        /// <remarks>
        /// Named like any other new sequence rather than after the one it came from: a script may run a
        /// sequence by its name, so a copied name would leave that name meaning two sequences.
        /// </remarks>
        public void Duplicate(Sequence sequence)
        {
            var copy = _program.Duplicate(sequence);
            copy.Name = Name(copy);
            Do(copy.System, new MakeSequence(_program, copy, Change.CopySequence));

            PushMade(new SequencePage(this, copy));
        }

        /// <summary>Adds a copy of <paramref name="trigger" /> to <paramref name="system" />, after its last trigger, doing what it does.</summary>
        /// <remarks>
        /// A button starts only one trigger, so a button trigger's copy starts from a new, empty signal slot of the kinds the
        /// original's expects, and its line says so until the first pick on it. Every other trigger is copied whole.
        /// </remarks>
        public void CopyTrigger(AutomationSystem system, Trigger trigger)
        {
            var signal = trigger as SignalTrigger;
            var shown = system.IsBlueprint ? StandIn(system) : null;
            if (signal == null || signal.IsWritten || shown == null)
            {
                Do(system, new AddTrigger(_program, Automation.Copied(trigger, null), null, new List<SystemEdit>(), null, -1,
                    Change.CopyTrigger));

                return;
            }

            var part = new Part(FreeSlotName(system));
            part.IsSignal = true;
            part.Expects = Expected(system, signal);

            var reslot = new Reslot(system, shown);
            reslot.Make(part, new List<string>());
            _picked.Add(reslot);
            _pickedNames.Add(new List<string> { part.Id });

            var copy = Automation.Copied(trigger, part.Id);
            Do(system, new AddTrigger(_program, copy, null, new List<SystemEdit>(), null, -1, Change.CopyTrigger));
            _copies.Add(copy);
        }

        /// <summary>Whether <paramref name="trigger" /> is a copy of a button trigger nobody has picked a start for yet, whose line says why it has a slot of its own.</summary>
        public bool Copied(Trigger trigger)
        {
            return _copies.Contains(trigger);
        }

        /// <summary>The first slot name of the numbered kind that <paramref name="system" /> does not use, counting on from how many slots it has.</summary>
        string FreeSlotName(AutomationSystem system)
        {
            for (var number = system.Parts.Count + 1;; number++)
            {
                var name = Template.Fill(Texts.SlotNumbered, "number", number.ToString(System.Globalization.CultureInfo.InvariantCulture));
                if (system.FindPart(name) == null)
                    return name;
            }
        }

        /// <summary>The nearest type every slot <paramref name="trigger" /> starts from expects, or empty for any block.</summary>
        string Expected(AutomationSystem system, MomentTrigger trigger)
        {
            var lineages = new List<IList<string>>();
            for (var i = 0; i < trigger.Sources.Count; i++)
            {
                var part = system.FindPart(trigger.Sources[i]);
                if (part == null)
                    continue;

                if (part.Expects.Length == 0)
                    return "";

                lineages.Add(Lineage(part.Expects));
            }

            return Part.Nearest(lineages);
        }

        /// <summary>
        /// The type <paramref name="type" /> and each type the game builds it on, nearest first, as a kind on the construct has
        /// them; the type alone where no block on the construct is of it.
        /// </summary>
        IList<string> Lineage(string type)
        {
            var entities = _world.Entities;
            for (var i = 0; i < entities.Count; i++)
            {
                var lineage = entities[i].Kind.Lineage;
                var at = lineage.IndexOf(type);
                if (at >= 0)
                {
                    var from = new List<string>();
                    for (var k = at; k < lineage.Count; k++)
                        from.Add(lineage[k]);

                    return from;
                }
            }

            return new List<string> { type };
        }

        /// <summary>
        /// Names a new sequence by the first free number, counting on from the sequences its system already has.
        /// </summary>
        /// <remarks>
        /// So the number says which one it is: beside "Cycle" a new one is "Sequence 2", never "Sequence 1".
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

            // The sequence being named is already among them.
            for (var number = owner.Sequences.Count; ; number++)
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
