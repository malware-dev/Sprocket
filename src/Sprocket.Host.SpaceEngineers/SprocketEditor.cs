using Sprocket.Host.SpaceEngineers.Features.Codex;
using Sprocket.Host.SpaceEngineers.Features.Developing;
using Sprocket.Host.SpaceEngineers.Features.Home;
using Sprocket.Host.SpaceEngineers.Features.Installations.Edits;
using Sprocket.Host.SpaceEngineers.Features.Library;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Features.LogicSystems.Edits;
using Sprocket.Host.SpaceEngineers.Features.Selector.Toolbar;
using Sprocket.Host.SpaceEngineers.Features.Sequences;
using Sprocket.Host.SpaceEngineers.Features.Sequences.Edits;
using Sprocket.Host.SpaceEngineers.Features.Sharing;
using Sprocket.Host.SpaceEngineers.Features.Slots;
using Sprocket.Host.SpaceEngineers.Features.Slots.Edits;
using Sprocket.Host.SpaceEngineers.Features.Triggers;
using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Writing;
using System;
using System.Collections.Generic;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers
{
    /// <summary>The editor of a construct's automation: the pages open, the screen built from the one on top, and every edit made from them.</summary>
    public sealed class SprocketEditor
    {
        const int Listed = 5;

        readonly GridWorld _world;
        readonly Automation _program;
        readonly AutomationRunner _runner;
        string _author = "";
        string _holder;
        EditorHabits _habits = new EditorHabits();
        PlayerMemory _viewer = new PlayerMemory();
        readonly List<EditorPage> _stack = new List<EditorPage>();

        readonly Dictionary<int, int> _standIns = new Dictionary<int, int>();
        readonly PressFilter _gate;
        readonly EditorNotices _bulletin;
        readonly ChangeDates _dates;
        readonly SlotClaims _claims;
        readonly SlotsFromPicks _picks;
        readonly SlotBlocks _slotBlocks;
        readonly VersionChoice _versions;
        readonly TriggerQuestions _starts;
        readonly LibraryTransfers _holdings;
        readonly EditorScreen _screen = new EditorScreen();
        readonly List<ChoiceEntry> _actionBar = new List<ChoiceEntry>();

        bool _structureDirty;
        bool _developerMode;
        bool _toldPasting;
        LibraryShelf _shelf;

        /// <summary>An editor of <paramref name="program" />, saying what it says in <paramref name="texts" />.</summary>
        public SprocketEditor(GridWorld world, Automation program, AutomationRunner runner, Texts texts)
        {
            _gate = new PressFilter(this);
            _bulletin = new EditorNotices(this, _gate);
            _world = world;
            _program = program;
            _runner = runner;
            Texts = texts;
            _history = new EditHistory(program, runner, texts);
            Ago = new AgoText(texts);
            Codex = new CodexPages(texts);
            Guides = new RelayCommand(() => Act(CommandKind.OpenGuide, OpenGuide)?.Execute());

            _dates = new ChangeDates(_program);
            _claims = new SlotClaims(this);
            _picks = new SlotsFromPicks(this);
            _slotBlocks = new SlotBlocks(this);
            _versions = new VersionChoice(this, _dates);
            _starts = new TriggerQuestions(this);
            _holdings = new LibraryTransfers(this, _dates);

            _stack.Add(new HomePage(this));
            Rebuild();
        }

        /// <summary>What to show right now.</summary>
        public EditorScreen Screen => _screen;

        /// <summary>Who already claims the toolbar slots somebody picks.</summary>
        public SlotClaims Claims => _claims;

        /// <summary>How picked blocks become the slots of a blueprint.</summary>
        public SlotsFromPicks Picks => _picks;

        /// <summary>The blocks that stand for the slots a logic system names, as the editor shows it, and what they offer.</summary>
        public SlotBlocks SlotBlocks => _slotBlocks;

        /// <summary>The choice between two versions of a logic system.</summary>
        public VersionChoice Versions => _versions;

        /// <summary>How a trigger is made, changed or copied.</summary>
        public TriggerQuestions Starts => _starts;

        /// <summary>Moving logic systems between the construct and the library.</summary>
        public LibraryTransfers Holdings => _holdings;

        /// <summary>The name the game knows a player by, from their identity, set by whatever shows the editor; null where nothing can say.</summary>
        public Func<long, string> PlayerName { get; set; }

        /// <summary>The blocks the automation could not reach before its construct changed hands and can now, set by whatever keeps it.</summary>
        public IList<string> Returned { get; set; } = new List<string>();

        /// <summary>What the editor says, in the player's language.</summary>
        public Texts Texts { get; }

        /// <summary>The codex's pages, in <see cref="Texts" />, found by the names links give them.</summary>
        public CodexPages Codex { get; }

        /// <summary>The guide to rich text's window, made the first time it is opened, or null before then.</summary>
        public GuideWindow Guide { get; private set; }

        /// <summary>What every button for the guide to rich text does: opens it, as a press the editor may refuse.</summary>
        public ICommand Guides { get; }

        /// <summary>How long ago something happened, in <see cref="Texts" />.</summary>
        public AgoText Ago { get; }

        /// <summary>Where the screen showing the editor takes the keyboard and the pointer, or null while nothing shows it.</summary>
        public InputRoot Input { get; set; }

        /// <summary>The windows open over the screen showing the editor, or null while nothing shows it.</summary>
        public WindowLayer Windows { get; set; }

        /// <summary>How the screen showing the editor gives a name in place, or null while nothing shows it.</summary>
        public Action<NamePlate, Naming> Renamer { get; set; }

        /// <summary>Begins giving the name <paramref name="naming" /> holds in <paramref name="plate" />, in place, on the screen showing the editor.</summary>
        public void Rename(NamePlate plate, Naming naming)
        {
            Renamer?.Invoke(plate, naming);
        }

        /// <summary>How many times the screen has been built from its page, which a screen standing still never adds to.</summary>
        public int Rebuilds { get; private set; }

        /// <summary>What the person at the editor needs told above whatever page is showing, or null for nothing.</summary>
        public Notice Notice => _bulletin.Notice;

        /// <summary>
        /// Where running a sequence by hand, or stopping it, goes where it is not the runner here, or
        /// null: the sequence, the one of its system, and whether it stops.
        /// </summary>
        /// <remarks>Set on a client's copy, whose runner runs nothing: the sequence runs on the server.</remarks>
        public Action<Sequence, SystemInstance, bool> Elsewhere { get; set; }

        /// <summary>
        /// Where a state's value changed by hand goes as well where it is not the runner here, or null: the logic system, the
        /// installation, the state and its new value.
        /// </summary>
        /// <remarks>Set on a client's copy: the copy shows the value at once, and the server keeps it.</remarks>
        public Action<AutomationSystem, SystemInstance, State, Literal> HeldElsewhere { get; set; }

        /// <summary>Where a button somebody has just picked goes to be written into its panel's slots: the panel, by entity id.</summary>
        /// <remarks>On a client's copy, set by whoever lent it, since the toolbars are written where the Sprocket is kept.</remarks>
        public Action<long> Wire { get; set; }

        /// <summary>Where paused automation resumed here is resumed for real, or null where this is the automation itself.</summary>
        /// <remarks>Set on a client's copy, which holds the pause only as the server lent it: the server resumes it.</remarks>
        public Action Resumes { get; set; }

        /// <summary>Where the buttons picked and not yet written are let go of, as a pick is backed out of.</summary>
        /// <remarks>On a client's copy it asks the server.</remarks>
        public Action Unwire { get; set; }

        /// <summary>Whether the player at the screen may change the world's settings now, or null where nobody may.</summary>
        public Func<bool> Administers { get; set; }

        /// <summary>The settings the world runs by, or null where nothing gives them.</summary>
        public Func<Settings> WorldSettings { get; set; }

        /// <summary>Where a change of the world's settings is made, or null where nobody may change them.</summary>
        public Action<Settings> ChangeSettings { get; set; }

        /// <summary>Whether the admin's button belongs on the screen now: the player may change the world's settings.</summary>
        public bool MayAdminister => Administers != null && WorldSettings != null && ChangeSettings != null && Administers();

        /// <summary>What this player knows of blueprints shared in this world, or null in single player, where nobody is there to share with.</summary>
        public ShareBoard Sharing { get; set; }

        /// <summary>The id of the block the editor was opened from, or 0 where nothing says.</summary>
        public long OpenedFrom { get; set; }

        /// <summary>
        /// Whether <paramref name="system" /> is listed from the block the editor was opened on: always, unless it is kept to
        /// another block that is surely there and it is working.
        /// </summary>
        public bool Lists(AutomationSystem system)
        {
            var kept = SystemSummary.KeptTo(Runner, system);
            return kept == 0 || OpenedFrom == 0 || kept == OpenedFrom;
        }

        /// <summary>Where a change of the unit the player gives lengths of time in is told on, or null where <see cref="Viewer" /> is the world's own.</summary>
        /// <remarks>Set on a copy a client is editing, which tells the server.</remarks>
        public Action Preferred { get; set; }

        /// <summary>Who holds the Sprocket this editor shows, or null where this editor may change it.</summary>
        /// <remarks>
        /// While it is set, every command but those that move around the editor is refused. Setting it builds the screen
        /// again on the next tick.
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
        /// <remarks>Cleaned to the rule for any name, since it is written into saves as a namespace.</remarks>
        public string Author
        {
            get { return _author; }
            set { _author = NameRules.Clean(value); }
        }

        /// <summary>
        /// Whether to offer tools for working on Sprocket itself.
        /// </summary>
        /// <remarks>A change rebuilds the screen.</remarks>
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

        /// <summary>Whether the screens show tips, and terms look like terms, as the player at the screen chose; on unless told otherwise.</summary>
        /// <remarks>A change rebuilds the screen.</remarks>
        public bool TipsShown
        {
            get { return Codex.Shown; }
            set
            {
                if (Codex.Shown == value)
                    return;

                Codex.Shown = value;
                Invalidate();
            }
        }

        /// <summary>Keeps the player's choice of whether tips are shown where the host keeps it, or null for a host that keeps none.</summary>
        public Action<bool> KeepTips { get; set; }

        /// <summary>Turns tips off where they are shown and on where they are not, and keeps the choice.</summary>
        public void ToggleTips()
        {
            TipsShown = !TipsShown;
            KeepTips?.Invoke(TipsShown);
        }

        /// <summary>The ticks the runner has counted.</summary>
        public int CurrentTick => _runner.CurrentTick;

        /// <summary>Brings the screen up to date for frame <paramref name="tick" />, which the host counts.</summary>
        /// <remarks>Called only while a screen is open.</remarks>
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

            // The game announces no change to the grid's own name, so the heading's words are read again once a second.
            var retitled = Current.TakeRetitle();
            if (_worded.Due(tick) || retitled)
                Current.RefreshHeading(_screen.Heading);

            _screen.Subtitle = Current.Subtitle;
            _screen.SwitchIsOn = Current.SwitchIsOn;
            _screen.SwitchAbout = Current.SwitchAbout;
            _screen.Running = Current.IsRunning;
            _screen.Standing = Current.Standing;

            Walking();
        }

        /// <summary>The screen's own arrows, in the words of whatever they would take back.</summary>
        /// <remarks>Runs every tick: an edit changes what there is to walk back without a rebuild.</remarks>
        void Walking()
        {
            var page = Current;

            var canUndo = page.CanUndo;
            _screen.Undo = canUndo ? _undo : null;
            _screen.UndoText = canUndo ? page.UndoText : "";

            var canRedo = page.CanRedo;
            _screen.Redo = canRedo ? _redo : null;
            _screen.RedoText = canRedo ? page.RedoText : "";

            // A picker becomes finishable by a choice that leaves the screen its shape.
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
        /// <remarks>The answer that does what was asked comes first, as Yes stands before No in the game's own message boxes.</remarks>
        public void Answer(string text, Action chosen)
        {
            Answer(text, chosen, false);
        }

        /// <summary>Adds an answer to the standing question that finishes the page on top, as its primary button does.</summary>
        public void AnswerPrimary(string text, bool byDefault)
        {
            Answer(text, () => Current.Primary(), byDefault);
        }

        /// <summary>Adds the answer <paramref name="text" /> to the standing question, carrying out <paramref name="chosen" />, after the answers already offered; the one Enter takes where <paramref name="byDefault" />.</summary>
        public void Answer(string text, Action chosen, bool byDefault)
        {
            var command = _gate.Refuses(CommandKind.Answer) ? null : new RelayCommand(() => Reply(chosen));
            _screen.Answer(new ChoiceEntry(text, false, command), byDefault);
        }

        /// <summary>Adds a line to the standing question's menu, between the answers offered so far and those offered after it.</summary>
        public void AnswerRule()
        {
            _screen.Answer(ChoiceEntry.Rule(), false);
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
        public void Unask()
        {
            if (!_screen.HasQuestion)
                return;

            _screen.Unask();
        }


        /// <summary>A command that carries <paramref name="action" /> out as a press of <paramref name="kind" />, or null where the editor refuses that kind of press.</summary>
        public ICommand Act(CommandKind kind, Action action)
        {
            return _gate.Refuses(kind) ? null : new RelayCommand(() => Carry(kind, action));
        }

        void Carry(CommandKind kind, Action action)
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
        public List<Field> Fields(IList<long> entityIds, string actionId, ReachKind form)
        {
            return Vocabulary.Fields(_world, entityIds, actionId, form);
        }

        /// <summary>
        /// What gives a name through <paramref name="rename" />, where <paramref name="accepts" /> takes it, or null where
        /// the editor refuses renaming.
        /// </summary>
        public Action<string> Renaming(Action<string> rename, Func<string, bool> accepts)
        {
            if (_gate.Refuses(CommandKind.Rename))
                return null;

            Action<string> gives = null;
            gives = name =>
            {
                name = name ?? "";
                if (accepts != null && !accepts(name))
                    return;

                if (_gate.Replacing(CommandKind.Rename))
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
        /// <remarks>Setting it links each logic system that links to no blueprint to the library's one of its identity.</remarks>
        public LibraryShelf Shelf
        {
            get { return _shelf; }
            set
            {
                _shelf = value;
                for (var i = 0; value != null && i < _program.Systems.Count; i++)
                    value.Link(_program.Systems[i]);
            }
        }

        /// <summary>How the player at the screen stands, shown at the editor's foot: the game's own reading unless a host gives another.</summary>
        public IVitals Vitals { get; set; } = new CharacterVitals();

        /// <summary>How somebody is working in this editor, as against what they are building in it: which list of what to do they read, and where they keep the codex.</summary>
        /// <remarks>Replaced on a client's copy by the ones its earlier copies of the same block used.</remarks>
        public EditorHabits Habits
        {
            get { return _habits; }
            set { _habits = value ?? new EditorHabits(); }
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
        public bool ShowsTerminal
        {
            get { return Habits.ShowsTerminal; }
            set { Habits.ShowsTerminal = value; }
        }

        /// <summary>The construct being edited, where the names of the blocks it holds are read.</summary>
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

        /// <summary>Drops every page above <paramref name="depth" />.</summary>
        public void UnwindTo(int depth)
        {
            while (_stack.Count > depth && _stack.Count > 1)
                _stack.RemoveAt(_stack.Count - 1);

            _structureDirty = true;
        }


        /// <summary>Shows <paramref name="page" /> over the current one.</summary>
        public void Push(EditorPage page)
        {
            page.Opened = ++_opened;
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
        /// <remarks>A page whose <see cref="EditorPage.Leaving" /> answers false stays, and must then leave by its own means.</remarks>
        public void Leave()
        {
            if (Unwire != null)
                Unwire();

            var page = Current;
            if (page.Leaving())
                Left(page);
        }

        /// <summary>Goes back from <paramref name="page" />, taking back what it was opened on where that was made and left untouched.</summary>
        public void Left(EditorPage page)
        {
            Pop();
            if (page.IsMade && page.Untouched)
                page.Unmake();
        }

        /// <summary>Goes back a page as Done, where the page lets it go, keeping what it was opened on whether or not anything was done to it.</summary>
        public void Finish()
        {
            Keep(Current);
            if (Current.Leaving())
                Pop();
        }

        /// <summary>Has the screen built again on the next tick, its shape having changed.</summary>
        public void Invalidate()
        {
            _structureDirty = true;
        }

        /// <summary>Has the screen built again on the next tick, and the arrows' words put together again, a block having been renamed in the game.</summary>
        public void Renamed()
        {
            _history.Renamed();
            _structureDirty = true;
        }

        /// <summary>Dates each logic system whose words have changed since they were last read with the time now, or reads every one where <paramref name="surely" />.</summary>
        public void Stamp(bool surely = false)
        {
            _dates.Stamp(surely);
        }

        readonly EditHistory _history;

        // Counts the pages opened, each one's number telling the changes it made from the others'.
        int _opened;

        /// <summary>Makes a change to a sequence and remembers how to take it back.</summary>
        /// <remarks>Every change to a sequence goes through here.</remarks>
        public void Do(Sequence sequence, SequenceEdit edit)
        {
            var system = sequence.System;
            Record(system, new OnSequence(sequence, _picks.Slotted(system, edit)));
        }

        /// <summary>Makes a change to a system and remembers how to take it back.</summary>
        /// <remarks>Every change to a trigger goes through here.</remarks>
        public void Do(AutomationSystem system, SystemEdit edit)
        {
            Record(system, _picks.Slotted(system, edit));
        }

        /// <summary>Makes <paramref name="edit" /> to <paramref name="system" /> as the next step of its history.</summary>
        void Record(AutomationSystem system, Edit<AutomationSystem> edit)
        {
            if (_history.Did(system, edit, Maker()))
                Settled(edit);
        }

        /// <summary>Makes a change to the program as a whole, a logic system or an installation made or taken out, and remembers how to take it back.</summary>
        public void Do(ProgramEdit edit)
        {
            if (_history.Did(edit, Maker()))
                Settled(edit);
        }

        /// <summary>The number of the page a change made now is made on: the nearest page with a history, which answers for it on leaving.</summary>
        /// <remarks>A picker or a writing page opened from it hands its result to that page.</remarks>
        int Maker()
        {
            for (var i = _stack.Count - 1; i >= 0; i--)
            {
                if (_stack[i].HasHistory)
                    return _stack[i].Opened;
            }

            return 0;
        }

        /// <summary>Whether a change made on <paramref name="page" /> still stands, to keep or discard on leaving it.</summary>
        public bool Changed(EditorPage page)
        {
            return _history.MadeBy(page.Opened);
        }

        /// <summary>Keeps every change made on <paramref name="page" />, so leaving it asks nothing about them.</summary>
        public void Keep(EditorPage page)
        {
            _history.Disown(page.Opened);
        }

        /// <summary>Takes back every change made on <paramref name="page" />, leaving those made on the pages opened from it.</summary>
        public void Discard(EditorPage page)
        {
            var systems = new List<Edit<AutomationSystem>>();
            var construct = new List<Edit<Automation>>();
            _history.Discard(page.Opened, systems, construct);

            foreach (var edit in systems)
                Walked(edit);

            foreach (var edit in construct)
                Walked(edit);
        }

        /// <summary>Has the program edited and rewired after <paramref name="edit" />, and the screen built again where it changes its shape.</summary>
        void Settled<T>(Edit<T> edit)
        {
            // Whatever the change brought in, a sequence made, a blueprint put on, a program merged, is named as it arrives.
            GivenNames.Give(_program, Texts);
            _program.Edited();
            _program.Rewire();

            if (edit.ChangesShape)
                Invalidate();
        }

        /// <summary>Whether there is a change to <paramref name="system" /> on its screen <paramref name="screen" /> to take back now.</summary>
        public bool CanUndo(AutomationSystem system, object screen)
        {
            return _history.CanUndo(system, screen);
        }

        /// <summary>What taking back the last change on <paramref name="screen" /> would undo, in words, with the other screens it takes back changes on.</summary>
        public string UndoLabel(AutomationSystem system, object screen)
        {
            return _history.UndoLabel(system, screen, StandIn(system));
        }

        /// <summary>Takes back the last change on <paramref name="screen" />, with every later change it takes along.</summary>
        public void Undo(AutomationSystem system, object screen)
        {
            Walked(_history.Undo(system, screen, StandIn(system)));
            Stand(system);
        }

        /// <summary>Whether there is a change on <paramref name="screen" /> taken back to make again.</summary>
        public bool CanRedo(AutomationSystem system, object screen)
        {
            return _history.CanRedo(system, screen);
        }

        /// <summary>What making the last change taken back on <paramref name="screen" /> again would redo, in words, with the other screens it makes changes on.</summary>
        public string RedoLabel(AutomationSystem system, object screen)
        {
            return _history.RedoLabel(system, screen, StandIn(system));
        }

        /// <summary>Makes the last change taken back on <paramref name="screen" /> again, with whatever taken back it leans on.</summary>
        public void Redo(AutomationSystem system, object screen)
        {
            Walked(_history.Redo(system, screen));
            Stand(system);
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

        /// <summary>How many times the undo and redo arrows' words have been put together.</summary>
        public int Worded => _history.Worded;

        /// <summary>Whether there is a change to the program as a whole to walk back.</summary>
        public bool CanUndoConstruct()
        {
            return _history.CanUndoConstruct();
        }

        /// <summary>What walking back the last change to the program as a whole undoes, as the undo arrow names it.</summary>
        public string UndoLabelConstruct()
        {
            return _history.UndoLabelConstruct();
        }

        /// <summary>Walks back the last change to the program as a whole.</summary>
        public void UndoConstruct()
        {
            Walked(_history.UndoConstruct());
        }

        /// <summary>Whether there is a change to the program as a whole to walk forward again.</summary>
        public bool CanRedoConstruct()
        {
            return _history.CanRedoConstruct();
        }

        /// <summary>What walking forward again the last change to the program as a whole that was walked back redoes, as the redo arrow names it.</summary>
        public string RedoLabelConstruct()
        {
            return _history.RedoLabelConstruct();
        }

        /// <summary>Walks forward again the last change to the program as a whole that was walked back.</summary>
        public void RedoConstruct()
        {
            Walked(_history.RedoConstruct());
        }

        /// <summary>Has the program edited and rewired and the screen built again after a step back or forward made <paramref name="edit" />.</summary>
        void Walked<T>(Edit<T> edit)
        {
            if (edit == null)
                return;

            // A logic system swapped back or forth brings its own date, so it does not read as changed since.
            var replaced = edit as ReplaceSystem;
            var standing = replaced == null ? null : _program.FindSystem(replaced.Identity);
            if (standing != null)
                _dates.Know(standing);

            _program.Edited();
            _program.Rewire();
            Invalidate();
        }

        /// <summary>Asks <paramref name="question" /> about <paramref name="named" />, with <paramref name="warning" /> under it, and deletes only on the answer that says so.</summary>
        /// <remarks><paramref name="arguments" /> fills the warning's slots and may be null.</remarks>
        public void Deleting(string question, string named, string warning, Arguments arguments, Action delete)
        {
            arguments = arguments ?? new Arguments();
            arguments.Set("named", named ?? "");

            Ask($"{question}\n\n**{{named}}**{(string.IsNullOrEmpty(warning) ? "" : $"\n\n{warning}")}", arguments);
            Answer(Texts.DeleteKeep, () => { }, true);
            Answer(Texts.DeleteConfirm, delete);
        }

        /// <summary>A bulleted list of <paramref name="count" /> names for a question, each in its own slot of <paramref name="arguments" />.</summary>
        /// <remarks>
        /// Past <see cref="Listed" /> lines the last one counts the rest, and only the names shown are asked of
        /// <paramref name="name" />.
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
        /// <remarks>Clears every undo history, and ends the runs under way.</remarks>
        public string Apply(string text)
        {
            var trial = new Automation();
            var trouble = ProgramReader.Load(text, trial, _world.Values);
            if (trouble.Length > 0)
                return trouble;

            var switches = BlockSwitches.Of(_program);

            var systems = _program.Systems;
            for (var i = systems.Count - 1; i >= 0; i--)
                Drop(systems[i]);

            _history.Clear();

            _program.Discard();
            string warning;
            ProgramReader.Load(text, _program, _world.Values, out warning, _world);
            BlockSwitches.Put(switches, _program);

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
            _history.Forget(system);
        }

        /// <summary>Lets go of every page and choice the editor kept about <paramref name="system" />, which has just been taken out of the program.</summary>
        /// <remarks>Its history is kept, so a logic system put back by undo comes back with its own way back.</remarks>
        public void Gone(AutomationSystem system)
        {
            var sequences = system.Sequences;
            for (var i = sequences.Count - 1; i >= 0; i--)
                ForgetSequence(sequences[i]);

            _standIns.Remove(system.Id);

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
            // Every system, since a change such as a rename moves none of the counts the tick goes by.
            Stamp(true);
            if (_structureDirty)
                Rebuild();

            // After a rebuild too, so a title that follows the choice catches up with this command rather than a frame later.
            if (Current.TakeRetitle())
                Current.RefreshHeading(_screen.Heading);

            Walking();
        }

        /// <summary>
        /// Rebuilds the screen from the page on top of the stack.
        /// </summary>
        void Rebuild()
        {
            Rebuilds++;

            // Before the page is built, which reads what is wrong, so an edit just made is checked now rather than on the next tick.
            _runner.Checking();

            var page = Current;

            // Cleared in place: a target holds this list across a refresh and compares its contents.
            _screen.Heading.Clear();
            page.Heading(_screen.Heading);
            _screen.Reheaded();

            _screen.CanGoBack = _stack.Count > 1;
            _screen.Back = _screen.CanGoBack ? Act(CommandKind.Back, Leave) : null;
            _screen.Library = page.Library;
            _screen.Flip = page.CanSwitch ? Act(CommandKind.Switch, () => page.Switch(!page.SwitchIsOn)) : null;
            _screen.SwitchAbout = page.SwitchAbout;

            // Read once per build: only another page's switch locks it.
            _screen.SwitchLocked = page.SwitchLocked;
            _screen.Runs = page.Runs;
            _screen.RunLocked = page.RunLocked;
            _screen.Stops = page.Stops;
            _screen.Deletes = page.Deletes;
            _screen.DeleteAbout = page.DeleteAbout;
            _screen.DeleteLocked = page.DeleteLocked;

            // Made whether or not there is anything to walk back; the tick decides whether it is offered.
            _screen.HasHistory = page.HasHistory;
            _undo = page.HasHistory ? Act(CommandKind.Undo, page.Undo) : null;
            _redo = page.HasHistory ? Act(CommandKind.Redo, page.Redo) : null;
            _primary = Act(CommandKind.Primary, page.Primary);

            page.Build();

            // A page that opened another as it was built is never shown.
            if (Current != page)
            {
                Rebuild();
                return;
            }
            _bulletin.Say();

            // Everything, not just what the cadence is due to say: the page may be new.
            page.Resay();
            page.Refresh();

            _screen.Subtitle = page.Subtitle;
            _screen.SwitchIsOn = page.SwitchIsOn;
            _screen.Standing = page.Standing;
            Walking();
            BuildActions();

            _structureDirty = false;
        }


        /// <summary>
        /// Rebuilds the screen-level actions, replacing the previous set outright.
        /// </summary>
        /// <remarks>Command ids are issued afresh on every rebuild, so a leftover entry would press nothing.</remarks>
        void BuildActions()
        {
            _actionBar.Clear();

            if (DeveloperMode)
            {
                _actionBar.Add(new ChoiceEntry(Texts.ScriptTitle, Current is ScriptPage,
                    Act(CommandKind.ShowScript, ShowScript)));
                _actionBar.Add(new ChoiceEntry(Texts.MemberAuditTitle, false, Act(CommandKind.CheckMembers, CheckMembers)));

                var keepHere = (Current as LogicSystemPage)?.KeepHere;
                if (keepHere != null)
                    _actionBar.Add(keepHere);
            }

            _screen.ShowActions(_actionBar);
        }

        /// <summary>Says, under the box pasted into, where text from outside the game comes in; the first paste while this editor lives, and no other.</summary>
        public void Pasted()
        {
            if (_toldPasting || Input == null || Input.Focused == null)
                return;

            _toldPasting = true;
            new Popup
            {
                Style = Styles.Popup,
                Anchor = Input.Focused,
                Base = { Gap = 4f },
                Placement = FixedSize.Width(360f),
                Content = new Label { Style = Styles.TableTalk, TextWrapping = TextWrapping.Wrap, Text = Texts.WritingPasteNote }
            }.Open();
        }

        /// <summary>Raised when every word the editor shows has been read again in the language now spoken.</summary>
        public event Action Reworded;

        /// <summary>How many times the editor's words have been read again in the language spoken, so a view that keeps words knows when they are old.</summary>
        public int Rewordings { get; private set; }

        /// <summary>Has every word the editor shows read again in the language now spoken, the codex's included.</summary>
        public void Reword()
        {
            Rewordings++;
            Codex.Reword();
            Guide?.Show(Texts);
            Invalidate();
            Reworded?.Invoke();
        }

        /// <summary>Opens the guide to rich text beside whatever screen is showing.</summary>
        public void OpenGuide()
        {
            if (Guide == null)
                Guide = new GuideWindow(this);

            Windows?.Open(Guide);
        }

        /// <summary>
        /// Opens the listing of what this construct's automation is saved as.
        /// </summary>
        /// <remarks>Opens whether or not <see cref="DeveloperMode" /> is on; whoever offers the way in decides that.</remarks>
        public void ShowScript()
        {
            if (Current is ScriptPage)
                return;

            Push(new ScriptPage(this));
        }

        /// <summary>Writes each block member on this construct that the codex has no text for to the game's log, and says how many.</summary>
        public void CheckMembers()
        {
            var missing = new MemberAudit(this).Undescribed();
            foreach (var row in missing)
                GameLog.Say($"No member text: {row}");

            Windows?.Open(new MemberAuditDialog(Texts, missing.Count == 0 ? Texts.MemberAuditClean : Template.Fill(Texts.MemberAuditDone, "count", missing.Count.ToString())));
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
        public void NewSequence(AutomationSystem system)
        {
            NewSequence(system, null);
        }

        /// <summary>
        /// Makes a sequence in <paramref name="system" />, stood where <paramref name="placed" /> says among its sequences, as one
        /// step back with making it, and opens it; after the others, under no category, where that is null.
        /// </summary>
        public void NewSequence(AutomationSystem system, Func<Sequence, Arrangement> placed)
        {
            var sequence = _program.AddSequence(system);
            sequence.Name = Name(sequence);
            var made = new MakeSequence(_program, sequence, ChangeKind.NewSequence);
            if (placed == null)
                Do(system, made);
            else
                Do(system, new WithMade(made, new Rearrange(ChangeKind.NewSequence, owner => owner.SequenceList, Arrangement.Of(system.SequenceList),
                    placed(sequence), sequence, null)));

            Invalidate();
            PushMade(new SequencePage(this, sequence));
        }

        /// <summary>Opens <paramref name="page" /> on something just made and given a name of its own, with that name in hand to be given.</summary>
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

        /// <summary>
        /// Gives <paramref name="system" /> slots and a first installation where it has none, switched off to be filled in: a
        /// logic system keeps at least one.
        /// </summary>
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
            SystemInstance made = system.AddInstance();
            made.Name = name;
            made.Enabled = false;
            _program.Edited();
            _program.Rewire();
        }

        /// <summary>Adds another installation of <paramref name="system" />, named by the first number free and switched off to be filled in, and shows the system through it.</summary>
        public SystemInstance Another(AutomationSystem system)
        {
            var name = SystemSummary.NewInstanceName(Texts, system);
            SystemInstance made = system.AddInstance();
            made.Name = name;
            made.Enabled = false;
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
        /// <remarks>A name that points at nothing is left out, so fewer blocks may come back than names.</remarks>
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
        /// <remarks>What the sequence already did stands, and the sequence stays switched on.</remarks>
        public void StopNow(Sequence sequence)
        {
            if (Elsewhere != null)
            {
                Elsewhere(sequence, StandIn(sequence.System), true);
                return;
            }

            SequenceRunner state = _runner.Get(sequence, StandIn(sequence.System));
            if (state != null)
                state.Interrupt(StopCause.Hand, 0);
        }

        /// <summary>Runs <paramref name="sequence" /> by hand, at once, where it is or on the construct <see cref="Elsewhere" /> runs it on.</summary>
        /// <remarks>Runs on the installation the editor shows; a sequence already running counts it as a skip.</remarks>
        public void RunNow(Sequence sequence)
        {
            if (Elsewhere != null)
            {
                Elsewhere(sequence, StandIn(sequence.System), false);
                return;
            }

            _runner.Call(sequence, StandIn(sequence.System), true);
        }

        /// <summary>
        /// Gives <paramref name="state" /> of <paramref name="instance" /> of <paramref name="system" /> the value
        /// <paramref name="value" />, here and on the construct <see cref="HeldElsewhere" /> keeps it on. Nothing watching the
        /// state reacts.
        /// </summary>
        public void Hold(AutomationSystem system, SystemInstance instance, State state, Literal value)
        {
            _runner.States.Write(system, instance, state, value);
            if (HeldElsewhere != null)
                HeldElsewhere(system, instance, state, value);

            _runner.Revise();
        }

        /// <summary>Takes a sequence out of its logic system, with the triggers that run it, as a step its undo takes back.</summary>
        /// <remarks>Done at once, with no question, and every page showing the sequence goes with it.</remarks>
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
            // Read afresh however soon after it last closed.
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
            Resumes?.Invoke();
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
                Resumes?.Invoke();
                UnwindTo(1);
            });
            Answer(Texts.ButtonLeaveAlone, () => { }, true);
        }

        /// <summary>Takes <paramref name="system" /> out of the program, with its sequences, its triggers and its installations, as a step home's undo takes back.</summary>
        /// <remarks>Done at once, with no question; undo brings it back switched off.</remarks>
        public void DeleteSystem(AutomationSystem system)
        {
            Do(new TakeSystemOut(system, ChangeKind.DeleteSystem));
            Gone(system);
        }

        /// <summary>Takes <paramref name="instance" />, one of the installations of <paramref name="system" />, out of it, as a step home's undo takes back.</summary>
        /// <remarks>Done at once, with no question, and nothing is taken where it is the only installation.</remarks>
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

            Answer(Texts.BlueprintKeepRunning, () => { }, true);
            Answer(Texts.BlueprintSwitchThemOff, delegate
            {
                system.Enabled = false;
                Invalidate();
            });
        }

        /// <summary>Switches <paramref name="instance" /> of <paramref name="system" /> on or off, while the system is on.</summary>
        public void SwitchInstance(AutomationSystem system, SystemInstance instance, bool on)
        {
            if (!system.Enabled)
                return;

            // Revised at once, so an open screen drops the reason it was switched off.
            instance.Enabled = on;
            _runner.Revise();
        }

        /// <summary>What pressing the switch of <paramref name="system" /> would do, said as a note on it.</summary>
        public string SwitchAbout(AutomationSystem system)
        {
            if (!system.IsBlueprint)
                return StatusRow.Switching(Texts, true, system.Enabled);

            return system.Enabled ? Texts.Codex.RowSwitchBlueprintOff : Texts.Codex.RowSwitchBlueprintOn;
        }

        /// <summary>Takes back the last change to <paramref name="sequence" /> as though it was never made, where it is the last change to its system.</summary>
        /// <remarks>For a step added a moment ago and left at once.</remarks>
        public bool Withdraw(Sequence sequence)
        {
            var system = sequence.System;
            var last = system == null ? null : _history.Last(system) as OnSequence;
            if (last == null || last.Sequence != sequence || !_history.Withdraw(system, last))
                return false;

            Settled(last);
            return true;
        }

        /// <summary>Takes <paramref name="sequence" /> out of the program without asking, and any page showing it.</summary>
        /// <remarks>For one made a moment ago and left untouched: the step that made it is withdrawn rather than walked back.</remarks>
        public void TakeOut(Sequence sequence)
        {
            var system = sequence.System;
            var last = system == null ? null : _history.Last(system);
            var slotted = last as Reslotted<AutomationSystem>;
            var made = (slotted != null ? slotted.Edit : last) as MakeSequence;
            if (made == null || made.Sequence != sequence || !_history.Withdraw(system, last))
                _program.RemoveSequence(sequence);

            ForgetSequence(sequence);
            _program.Rewire();
        }

        /// <summary>Takes <paramref name="system" /> out of the program without asking, with everything in it and any page showing it.</summary>
        /// <remarks>For one made a moment ago and left untouched: the step that made it is withdrawn rather than walked back.</remarks>
        public void TakeOut(AutomationSystem system)
        {
            var made = _history.LastConstruct() as MakeSystem;
            if (made == null || made.System != system || !_history.WithdrawConstruct(made))
            {
                Drop(system);
                return;
            }

            Gone(system);
            _history.Forget(system);
            _program.Rewire();
        }

        /// <summary>Takes <paramref name="instance" /> out of <paramref name="system" /> without asking.</summary>
        /// <remarks>For one made a moment ago that was left without anything done to it: the step that made it is withdrawn.</remarks>
        public void TakeOut(AutomationSystem system, SystemInstance instance)
        {
            var made = _history.LastConstruct() as MakeInstallation;
            if (made == null || made.Instance != instance || !_history.WithdrawConstruct(made))
                system.RemoveInstance(instance);

            _standIns.Remove(system.Id);
            _program.Rewire();
            _structureDirty = true;
        }

        /// <summary>Adds a copy of <paramref name="sequence" /> to its logic system, and opens it for whatever is to be different.</summary>
        public void Duplicate(Sequence sequence)
        {
            var copy = _program.Duplicate(sequence);
            copy.Name = Name(copy);
            Do(copy.System, new MakeSequence(_program, copy, ChangeKind.CopySequence));

            PushMade(new SequencePage(this, copy));
        }

        /// <summary>
        /// Names a new sequence by the first free number, counting on from the sequences its system already has.
        /// </summary>
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
