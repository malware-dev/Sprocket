using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Blueprints;
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

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>The editor of a construct's automation: the pages open, the screen built from the one on top, and every edit made from them.</summary>
    /// <remarks>
    /// What is drawn renders <see cref="Screen" /> and each page's view, and carries out the commands they hand back, so
    /// nothing it draws has to be worked out twice.
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
        readonly Gate _gate;
        readonly Bulletin _bulletin;
        readonly Dates _dates;
        readonly Claims _claims;
        readonly Picks _picks;
        readonly Versions _versions;
        readonly Starts _starts;
        readonly Holdings _holdings;
        readonly EditorScreen _screen = new EditorScreen();
        readonly List<ChoiceEntry> _actionBar = new List<ChoiceEntry>();

        bool _structureDirty;
        bool _developerMode;
        bool _toldPasting;
        Shelf _shelf;

        /// <summary>An editor of <paramref name="program" />, saying what it says in <paramref name="texts" />.</summary>
        public SprocketEditor(GridWorld world, Automation program, AutomationRunner runner, Texts texts)
        {
            _gate = new Gate(this);
            _bulletin = new Bulletin(this, _gate);
            _world = world;
            _program = program;
            _runner = runner;
            Texts = texts;
            Ago = new Ago(texts);
            Codex = new Terms(texts);

            _dates = new Dates(_program);
            _claims = new Claims(this);
            _picks = new Picks(this);
            _versions = new Versions(this, _dates);
            _starts = new Starts(this);
            _holdings = new Holdings(this, _dates);

            _stack.Add(new HomePage(this));
            Rebuild();
        }

        /// <summary>What to show right now.</summary>
        public EditorScreen Screen => _screen;

        /// <summary>Who already claims the toolbar slots somebody picks.</summary>
        public Claims Claims => _claims;

        /// <summary>How picked blocks become the slots of a blueprint.</summary>
        public Picks Picks => _picks;

        /// <summary>The choice between two versions of a logic system.</summary>
        public Versions Versions => _versions;

        /// <summary>How a trigger is made, changed or copied.</summary>
        public Starts Starts => _starts;

        /// <summary>Moving logic systems between the construct and the library.</summary>
        public Holdings Holdings => _holdings;

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
        public Notice Notice => _bulletin.Notice;

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
        /// <remarks>
        /// Set by the host, which knows who is at the screen: in the game, the player's name until they choose one of their
        /// own.
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

        /// <summary>The ticks the runner has counted.</summary>
        public int CurrentTick => _runner.CurrentTick;

        /// <summary>Brings the screen up to date for frame <paramref name="tick" />, which the host counts.</summary>
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

        /// <summary>Puts the short question <paramref name="question" /> on the screen, to be answered before anything else, its named slots taking their words from <paramref name="arguments" />.</summary>
        public void Ask(string question, Arguments arguments)
        {
            _screen.Ask(question, arguments, false);
        }

        /// <summary>Puts the question <paramref name="question" /> under the control somebody just pressed, to be answered before anything else.</summary>
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

        /// <summary>Puts the question <paramref name="question" /> under the control somebody just pressed, to be answered before anything else, its named slots taking their words from <paramref name="arguments" />.</summary>
        public void AskHere(string question, Arguments arguments)
        {
            _screen.Ask(question, arguments, true);
        }

        /// <summary>Adds the answer <paramref name="text" /> to the standing question, carrying out <paramref name="chosen" />, after the answers already offered.</summary>
        /// <remarks>The answer that does what was asked about comes first and the one that does not second, as Yes and No stand in the game's own message boxes.</remarks>
        public void Answer(string text, Action chosen)
        {
            Answer(text, chosen, false);
        }

        /// <summary>An answer that finishes the page on top, the way its own way on does.</summary>
        public void AnswerPrimary(string text, bool byDefault)
        {
            Answer(text, () => Current.Primary(), byDefault);
        }

        /// <summary>Adds the answer <paramref name="text" /> to the standing question, carrying out <paramref name="chosen" />, after the answers already offered; the one Enter takes where <paramref name="byDefault" />.</summary>
        /// <remarks>
        /// Said here rather than inferred from the order, because the order is what reads best and
        /// the default is what is safe, and those are not the same question.
        /// </remarks>
        public void Answer(string text, Action chosen, bool byDefault)
        {
            var command = _gate.Refuses(Cmd.Answer) ? null : new Relay(() => Reply(chosen));
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


        /// <summary>A command that carries <paramref name="action" /> out as a press of <paramref name="kind" />, or null where the editor refuses that kind of press.</summary>
        /// <remarks>
        /// The first edit of a program that could not be read asks first, a press while a question stands only puts it
        /// away, and the editor settles after.
        /// </remarks>
        public ICommand Act(Cmd kind, Action action)
        {
            return _gate.Refuses(kind) ? null : new Relay(() => Carry(kind, action));
        }

        void Carry(Cmd kind, Action action)
        {
            if (_gate.Replacing(kind))
            {
                _gate.AskBeforeReplacing(() => Carry(kind, action));
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
            if (_gate.Refuses(Cmd.Rename))
                return null;

            Action<string> gives = null;
            gives = name =>
            {
                name = name ?? "";
                if (accepts != null && !accepts(name))
                    return;

                if (_gate.Replacing(Cmd.Rename))
                {
                    _gate.AskBeforeReplacing(() => gives(name));
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

        /// <summary>How somebody is working in this editor, as against what they are building in it: which list of what to do they read, and where they keep the codex.</summary>
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

        /// <summary>Has the screen built again on the next tick, its shape having changed.</summary>
        public void Invalidate()
        {
            _structureDirty = true;
        }


        /// <summary>Dates each logic system whose words have changed since they were last read with the time now, or reads every one where <paramref name="surely" />.</summary>
        public void Stamp(bool surely = false)
        {
            _dates.Stamp(surely);
        }

        readonly History<AutomationSystem> _systemHistory = new History<AutomationSystem>();

        readonly History<Automation> _constructHistory = new History<Automation>();

        readonly List<object> _also = new List<object>();

        /// <summary>Makes a change to a sequence and remembers how to take it back.</summary>
        /// <remarks>The only way anything changes a sequence.</remarks>
        public void Do(Sequence sequence, SequenceEdit edit)
        {
            var system = sequence.System;
            Record(system, new OnSequence(sequence, _picks.Slotted(system, edit)));
        }

        /// <summary>Makes a change to a system and remembers how to take it back.</summary>
        /// <remarks>The only way anything changes a trigger.</remarks>
        public void Do(AutomationSystem system, SystemEdit edit)
        {
            Record(system, _picks.Slotted(system, edit));
        }

        /// <summary>Makes <paramref name="edit" /> to <paramref name="system" /> as the next step of its history.</summary>
        void Record(AutomationSystem system, Edit<AutomationSystem> edit)
        {
            if (!_systemHistory.Did(system, edit))
                return;

            Settled(edit);
        }

        /// <summary>Makes a change to the program as a whole, a logic system or an installation made or taken out, and remembers how to take it back.</summary>
        /// <remarks>Walked back from the home screen.</remarks>
        public void Do(ProgramEdit edit)
        {
            if (!_constructHistory.Did(_program, edit))
                return;

            Settled(edit);
        }

        /// <summary>Has the program edited and rewired after <paramref name="edit" />, and the screen built again where it changes its shape.</summary>
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

            return names.Count == 0 ? label : $"{label} {Template.Fill(Texts.UndoAlso, "screens", string.Join(", ", names.ToArray()))}";
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

            Reach(page => page is LogicSystemPage && page.IsAbout(system), system, null);
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

        /// <summary>What walking forward again the last change to the program as a whole that was walked back redoes, as the redo arrow names it.</summary>
        public string RedoLabelConstruct()
        {
            return ChangeText.Of(Texts, _constructHistory.Redoes(_program, null));
        }

        /// <summary>Walks forward again the last change to the program as a whole that was walked back.</summary>
        public void RedoConstruct()
        {
            Walked(_constructHistory.Redo(_program, null));
        }

        /// <summary>Has the program edited and rewired and the screen built again after a step back or forward made <paramref name="edit" />.</summary>
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
                _dates.Know(standing);

            _program.Edited();
            _program.Rewire();
            Invalidate();
        }

        /// <summary>Asks <paramref name="question" /> about <paramref name="named" />, with <paramref name="warning" /> under it, and deletes only on the answer that says so.</summary>
        /// <remarks>
        /// Keeping is the first answer and the one Enter takes, so the press made without reading keeps what is there. The
        /// warning's own slots take their words from <paramref name="arguments" />, and it has none where that is null.
        /// </remarks>
        public void Deleting(string question, string named, string warning, Arguments arguments, Action delete)
        {
            arguments = arguments ?? new Arguments();
            arguments.Set("named", named ?? "");

            Ask($"{question}\n\n**{{named}}**{(string.IsNullOrEmpty(warning) ? "" : $"\n\n{warning}")}", arguments);
            Answer(Texts.DeleteKeep, () => { }, true);
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
                var slot = $"item{i.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
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
            var trouble = Reader.Load(text, trial, _world.Values);
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
            string warning;
            Reader.Load(text, _program, _world.Values, out warning, _world);
            StateText.Load(switches, _program, _runner);

            _gate.Told = true;
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
        public void Gone(AutomationSystem system)
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
            _screen.Help = page.Help != null ? Act(Cmd.OpenCodex, () => Helped(page)) : null;
            _screen.Flip = page.CanSwitch ? Act(Cmd.Switch, () => page.Switch(!page.SwitchIsOn)) : null;
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
            _bulletin.Say();

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
        public void ShowScript()
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

        /// <summary>Adds a sequence with nothing starting it yet to <paramref name="system" />, and opens it to be named.</summary>
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
        public void PushNamed(EditorPage page)
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
                _picks.Blueprinted(system);
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
            foreach (var blockName in seen)
            {
                GridEntity entity = _runner.Bindings.Entity(blockName);
                if (entity != null)
                    found.Add(entity.Id);
            }

            return found;
        }

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
        /// <remarks>
        /// What starts a sequence is usually a door or a button somewhere else on the ship, so this saves going and pressing
        /// it. It is the same call a trigger makes, on the installation the editor shows.
        /// <para>
        /// A sequence already running is not started twice. The runner counts that as a skip, the
        /// same as a button pressed twice, and the row says so.
        /// </para>
        /// </remarks>
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
                foreach (var doomed in systems)
                    DeleteSystem(doomed);

                _program.PausedFrom.Clear();
                UnwindTo(1);
            });
            Answer(Texts.ButtonLeaveAlone, () => { }, true);
        }

        /// <summary>Takes <paramref name="system" /> out of the program, with its sequences, its triggers and its installations, as a step home's undo takes back.</summary>
        /// <remarks>Without asking: undo brings it back, switched off, with its own way back.</remarks>
        public void DeleteSystem(AutomationSystem system)
        {
            Do(new TakeSystemOut(system, Change.DeleteSystem));
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
            // A version waiting for a choice asks for it instead.
            if (on && _versions.Rival(system) != null)
            {
                _versions.AskChoose(system);
                return;
            }

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
                i => SystemSummary.DescribeInstance(Texts, _runner.Bindings, system, instances[i]), arguments);
            var running = SystemSummary.IsRunning(system, _runner, null);
            Ask($"{Texts.BlueprintSwitchOffQuestion}\n\n{Texts.BlueprintSwitchOffStops}{listed}{(running ? $"\n\n{Texts.BlueprintSwitchOffRunning}" : "")}", arguments);

            // Keeping them running is the one Enter takes.
            Answer(Texts.BlueprintKeepRunning, () => { }, true);
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

        /// <summary>Adds a copy of <paramref name="sequence" /> to its logic system, and opens it for whatever is to be different.</summary>
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

        /// <summary>
        /// Names a new sequence by the first free number, counting on from the sequences its system already has.
        /// </summary>
        /// <remarks>
        /// So the number says which one it is: beside "Cycle" a new one is "Sequence 2", never "Sequence 1".
        /// <para>
        /// Stored from here on, and never re-derived, so renaming one cannot renumber another.
        /// </para>
        /// </remarks>
        public string Name(Sequence sequence)
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
