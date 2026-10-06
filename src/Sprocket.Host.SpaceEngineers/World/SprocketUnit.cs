using VRage.Game.ModAPI;
using System.Collections.Generic;
using System;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Host.SpaceEngineers.Localization;

// The Ingame one, which is where action arguments live. Aliased rather than imported whole, the
// way TriggerAction does, because the Ingame namespaces collide with the mod-side ones.
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One Sprocket, on one construct: its world, its program, the runtime that runs it, and the editor.
    /// </summary>
    /// <remarks>
    /// The program and its runtime belong to the block, not to the screen: the session ticks them whether
    /// or not anything is open, and the screen is only a way to look at them. Owned by the screen, a
    /// sequence would run only while somebody was watching it, and an airlock is worth nothing on those
    /// terms.
    /// </remarks>
    public sealed class SprocketUnit : IDisposable
    {
        /// <summary>
        /// Where the program sits in the block's mod storage.
        /// </summary>
        /// <remarks>
        /// Mod storage is shared with every other mod on the entity, so the key has to be Sprocket's and has to
        /// never change: a new one would read as an empty Sprocket and quietly discard somebody's ship.
        /// <para>
        /// <b>It also has to be claimed in <c>Content/Data/EntityComponents.sbc</c>, and the two must
        /// match.</b> The game writes out only the keys some mod has registered there and drops the rest
        /// with a line in the log, so an unclaimed key works perfectly all session and vanishes on save.
        /// </para>
        /// </remarks>
        public static readonly Guid ProgramKey = new Guid("9F1E4A2C-7D53-4B18-9C6E-2A0D5F3B81E7");

        static readonly Guid StateKey = new Guid("C4B7D2F6-1A85-4E39-8D07-6B92F5C1E4A3");


        IMyTerminalBlock _block;

        Backups _backups;

        Shelf _shelf;

        readonly List<long> _backedGrids = new List<long>();

        readonly bool _copy;

        readonly GridWorld _world;
        readonly Automation _program = new Automation();
        readonly AutomationRunner _runner;

        readonly Texts _texts;
        readonly string _author;
        readonly bool _developerMode;

        SprocketEditor _editor;

        /// <summary>
        /// Whether this block already carries a program, without building anything to find out.
        /// </summary>
        /// <remarks>
        /// Asked of every programmable block in the world as it comes alive, so it has to be one lookup and
        /// no allocation. A block that answers false is left entirely alone.
        /// </remarks>
        public static bool HasProgram(IMyTerminalBlock block)
        {
            MyModStorageComponentBase storage = block == null ? null : block.Storage;
            if (storage == null)
                return false;

            string text;
            return storage.TryGetValue(ProgramKey, out text) && !string.IsNullOrEmpty(text);
        }

        /// <summary>The Sprocket a block carries, reading its program out of the block.</summary>
        /// <remarks>
        /// <paramref name="rota" /> is the world's, shared by every Sprocket in it. See <see cref="Rota" />.
        /// <paramref name="texts" /> are what its editor says, in the player's language.
        /// </remarks>
        public SprocketUnit(IMyTerminalBlock block, Settings settings, Rota rota, Texts texts, Backups backups = null)
            : this(block, settings, rota, texts, false, Stored(block, ProgramKey), Stored(block, StateKey))
        {
            _backups = backups;
        }

        /// <summary>
        /// A copy of the Sprocket another machine keeps on <paramref name="block" />, made from what that
        /// machine wrote out, to be edited here.
        /// </summary>
        /// <remarks>
        /// On a rota of its own that nothing turns, so every sequence it holds waits forever and none of
        /// them acts. The editor is the same editor, which is what lets a client edit as a host does.
        /// </remarks>
        public static SprocketUnit Copy(IMyTerminalBlock block, Settings settings, Texts texts, string program,
            string state)
        {
            return new SprocketUnit(block, settings, new Rota(), texts, true, program, state);
        }

        /// <summary>
        /// This Sprocket built again from <paramref name="program" /> and <paramref name="switches" />,
        /// carrying on with whatever it was running.
        /// </summary>
        /// <remarks>
        /// What a client's edit becomes on the server. The runs are carried across the way a save and a
        /// reload carries them, which is already built to meet a program changed in between: a step
        /// that is no longer there, or a sequence that may no longer run, is not resumed.
        /// <para>
        /// This one is let go of: the caller puts the new one in its place and disposes of this.
        /// </para>
        /// </remarks>
        public SprocketUnit Rebuilt(string program, string switches, Settings settings, Rota rota)
        {
            var running = StateText.Running(_program, _runner);
            var rebuilt = new SprocketUnit(_block, settings, rota, _texts, false, program,
                switches + "\n" + running);
            rebuilt._backups = _backups;
            rebuilt._shelf = _shelf;
            rebuilt.Showing = Showing;
            if (_editor != null)
                rebuilt.Editor.Habits = _editor.Habits;

            // What this one had wired goes with it. A Sprocket that believed it had just been loaded
            // would write no slot, and would read a button the client has just added - and nobody has
            // wired yet - as one somebody cleared, and drop it.
            rebuilt._adopted = _adopted;
            rebuilt._wanted.AddRange(_wanted);
            return rebuilt;
        }

        SprocketUnit(IMyTerminalBlock block, Settings settings, Rota rota, Texts texts, bool copy, string program,
            string state)
        {
            _block = block;
            _copy = copy;
            _world = new GridWorld(block);
            _runner = new AutomationRunner(_program, _world, rota);
            _runner.QuickestLook = settings == null ? 1 : settings.QuickestLook;

            _texts = texts;
            _developerMode = settings != null && settings.DeveloperMode;

            // The player's own name is only where a namespace starts; choosing another is theirs to do.
            // What the game draws in front of it is not part of that name - see Player.Plain.
            _author = MyAPIGateway.Multiplayer == null ? null : Player.Plain(MyAPIGateway.Multiplayer.MyName);

            Load(program, state);

            // Asked once and then listened for. Whether this machine is working decides whether the
            // automation runs at all, and it changes when the lights go out rather than on a tick -
            // so asking the block sixty times a second would be two questions for one answer that
            // hardly ever moves. Released in Dispose, because a listener left on a block the game has
            // let go of is a leak that holds the whole unit alive.
            _runner.Live = _block.IsWorking;
            _block.IsWorkingChanged += Working;
            _world.AnchorRemoved += Removed;
        }

        /// <summary>
        /// The machine losing or regaining power, or being switched off.
        /// </summary>
        /// <remarks>
        /// The automation holds while it is not working, rather than stopping: a cycle interrupted by
        /// a blackout carries on when the lights come back, which is the same promise as one
        /// interrupted by a reload. The programmable block refuses to instantiate a script on a block
        /// that is not working, and this is the same rule for a machine that is already running one.
        /// </remarks>
        void Working(IMyCubeBlock block)
        {
            _runner.Live = _block.IsWorking;
        }

        /// <summary>The programmable block the automation is kept in: the construct's hero.</summary>
        /// <remarks>Another programmable block on the construct takes over when this one is gone - see <see cref="Seek" />.</remarks>
        public IMyTerminalBlock Block => _block;

        /// <summary>Whether the construct has no programmable block left to keep the automation in, which ends it.</summary>
        public bool Lost { get; private set; }

        /// <summary>Raised when the hero is removed from the construct, from the grid's own event; answered with <see cref="Rehome" />.</summary>
        /// <remarks>
        /// The grid is still changing when it is raised, so whatever answers it calls <see cref="Rehome" /> once the grid has
        /// finished: the session through the game's own invoke on the game thread, the harness on its next tick.
        /// </remarks>
        public event Action<SprocketUnit> HeroRemoved;

        /// <summary>The player's library, which the editor is given; null on a machine that keeps none.</summary>
        public Shelf Shelf
        {
            get { return _shelf; }
            set
            {
                _shelf = value;
                if (_editor != null)
                    _editor.Shelf = value;
            }
        }

        /// <summary>The editor, made the first time it is asked for, building its first screen out of whatever the program says by then.</summary>
        public SprocketEditor Editor
        {
            get
            {
                if (_editor != null)
                    return _editor;

                _editor = new SprocketEditor(_world, _program, _runner, _texts);
                _editor.Backups = _backups;
                _editor.Shelf = _shelf;

                // A copy's buttons are written where the Sprocket is kept, which is asked by whoever lent it.
                if (!_copy)
                    _editor.Wire = Wire;

                if (_author != null)
                    _editor.Author = _author;

                _editor.DeveloperMode = _developerMode;
                return _editor;
            }
        }

        /// <summary>The program this Sprocket runs and keeps.</summary>
        public Automation Program => _program;

        /// <summary>Whether anything has asked for the editor yet, and so made one.</summary>
        public bool HasEditor => _editor != null;

        public GridWorld World => _world;

        public AutomationRunner Runner => _runner;

        int _reconciled = -1;

        bool _adopted;

        bool _read;

        /// <summary>What a key names: a sequence, which one of its system, and what to do to it.</summary>
        struct Target
        {
            public Sequence Sequence;
            public SystemInstance Instance;
            public bool Stops;
        }

        readonly Dictionary<string, Target> _byKey =
            new Dictionary<string, Target>(StringComparer.Ordinal);

        /// <summary>
        /// What goes in a slot: which system, which one of it, which sequence.
        /// </summary>
        /// <remarks>
        /// <b>The system is named by its identity</b>, because a number cannot say which system it means once
        /// systems are exported, imported, or on constructs that join, and a name changes. So a button keeps
        /// working through a rename. Built once, at bind, and read back verbatim.
        /// </remarks>
        static string KeyOf(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            return system.Identity.ToString("D") + "/" + (instance == null ? 0 : instance.Number) + "/" + sequence.Number;
        }

        /// <summary>
        /// The same, for a button that stops the sequence rather than starting it.
        /// </summary>
        /// <remarks>
        /// The verb is on the end rather than the front, so every key written before stopping existed
        /// still says what it always said. A slot is never rewritten, so that is not a courtesy - it
        /// is the only way the old ones keep working.
        /// </remarks>
        static string KeyOf(AutomationSystem system, SystemInstance instance, Sequence sequence,
            bool stops)
        {
            var key = KeyOf(system, instance, sequence);
            return stops ? key + "/stop" : key;
        }

        /// <summary>
        /// What goes in a slot for a trigger that acts itself: which system, which one of it, and the trigger part.
        /// </summary>
        /// <remarks>
        /// By the part's name, because a trigger has no number and its part is what each instance answers. Renaming the part
        /// changes what is wanted, and the slot is written again with the rest of the wiring. Marked <c>part:</c>, because a
        /// part may be called anything, a sequence's number and <c>2/stop</c> among it.
        /// </remarks>
        static string KeyOf(AutomationSystem system, SystemInstance instance, RequiredTrigger trigger)
        {
            return system.Identity.ToString("D") + "/" + (instance == null ? 0 : instance.Number) + "/part:" + trigger.Part;
        }

        readonly List<Sequence> _runs = new List<Sequence>();

        /// <summary>The sequences of <paramref name="system" />, and the runtime's sequence for each trigger that acts itself.</summary>
        List<Sequence> Runs(AutomationSystem system)
        {
            _runs.Clear();

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                _runs.Add(sequences[i]);

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var alone = _runner.Alone(system, triggers[i]);
                if (alone != null)
                    _runs.Add(alone);
            }

            return _runs;
        }

        /// <summary>A block fired one of the actions Sprocket put in it.</summary>
        public void Fire(string key)
        {
            // Said every time, because a press that does nothing is otherwise invisible: this says whether it arrived.
            Target target;
            if (!_byKey.TryGetValue(key, out target))
            {
                Told.Say("a button fired " + key + ", which nothing here answers");
                return;
            }

            Told.Say("a button fired " + key + ", " + (target.Stops ? "stopping " : "running ")
                + (!target.Sequence.IsAlone ? "sequence " + target.Sequence.Number : "what its trigger does")
                + (target.Instance == null ? "" : " on " + target.Instance.Number));

            if (target.Stops)
                _runner.Halt(target.Sequence, target.Instance, Stopper.Trigger, 0);
            else
                _runner.Call(target.Sequence, target.Instance);
        }

        /// <summary>
        /// Rebuilds what the keys name, from the program.
        /// </summary>
        /// <remarks>
        /// Cleared rather than merged, so a deleted sequence stops answering. Runs with the rest of
        /// the reconcile, on the same one-integer signal, because a sequence or one of a system
        /// appears and disappears only by an edit to the program.
        /// </remarks>
        void Reindex()
        {
            _byKey.Clear();

            for (var s = 0; s < _program.Systems.Count; s++)
            {
                AutomationSystem system = _program.Systems[s];
                var sequences = system.Sequences;
                var instances = system.Uses;

                var triggers = system.Triggers;
                for (var t = 0; t < triggers.Count; t++)
                {
                    var needed = triggers[t] as RequiredTrigger;
                    var alone = needed == null ? null : _runner.Alone(system, needed);
                    for (var n = 0; alone != null && n < instances.Count; n++)
                    {
                        var target = new Target();
                        target.Sequence = alone;
                        target.Instance = instances[n];
                        _byKey[KeyOf(system, instances[n], needed)] = target;
                    }
                }

                for (var q = 0; q < sequences.Count; q++)
                {
                    for (var n = 0; n < instances.Count; n++)
                        Register(system, instances[n], sequences[q]);
                }
            }
        }

        /// <summary>
        /// Both names a sequence answers to: the one that runs it and the one that stops it.
        /// </summary>
        /// <remarks>
        /// Both always, rather than only the ones some trigger asks for, because a key is a name and
        /// not a permission - a slot naming one is a slot somebody made, whatever the program has
        /// since become.
        /// </remarks>
        void Register(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            var target = new Target();
            target.Sequence = sequence;
            target.Instance = instance;
            _byKey[KeyOf(system, instance, sequence)] = target;

            var halting = new Target();
            halting.Sequence = sequence;
            halting.Instance = instance;
            halting.Stops = true;
            _byKey[KeyOf(system, instance, sequence, true)] = halting;
        }

        /// <summary>
        /// What the program wants wired right now: this button of this panel runs this sequence.
        /// </summary>
        struct Want
        {
            public long PanelId;

            /// <summary>Which of the block's toolbars, from 1.</summary>
            public int Toolbar;

            /// <summary>Where in that toolbar's items, from 0, across its pages.</summary>
            public int Slot;

            public string Key;

            /// <summary>What the trigger named, so the block can be resolved again without a search.</summary>
            public string Reference;

            /// <summary>Which sequence on which one of its system, so reachability is reported for each one.</summary>
            public long Start;

            public bool Same(Want other)
            {
                return PanelId == other.PanelId && Toolbar == other.Toolbar && Slot == other.Slot
                    && string.Equals(Key, other.Key, StringComparison.Ordinal);
            }
        }

        List<Want> _wanted = new List<Want>();

        List<Want> _wanting = new List<Want>();

        /// <summary>
        /// Wires a button the moment the program asks for one, and never touches it again.
        /// </summary>
        /// <remarks>
        /// Sprocket is told nothing about this: core says a sequence is started by a block, and this
        /// side works out that a button panel needs a slot set for that to be true. The program's
        /// revision is the whole signal, so an ordinary tick is one integer comparison, and reading a
        /// toolbar is never on the tick path.
        /// <para>
        /// <b>Assignment happens once, where the block is bound, and then the game owns the slot.</b>
        /// Sprocket's part afterwards is to answer when the action is invoked. Anything that kept
        /// asserting what a slot should hold is Sprocket arguing with a player who can edit that slot
        /// whenever they like - and losing the argument is the only acceptable outcome, so it is
        /// better not to have it.
        /// </para>
        /// <para>
        /// A diff rather than a sweep, because the interesting case is subtraction: somebody who picks
        /// button 2 after button 1 leaves button 1 wired to something nothing references, which is a
        /// button on their ship that does something they cannot see.
        /// </para>
        /// <para>
        /// <b>A slot somebody else is using is left alone.</b> Overwriting it would configure Sprocket
        /// by breaking their ship, which is worse than not working.
        /// </para>
        /// </remarks>
        void Reconcile()
        {
            // Wiring, not Revision. Revision is about declarations moving; this is about somebody
            // having said what a trigger is, which is the thing that has to be acted on - and which
            // happens even when the trigger ends up identical to what it was.
            if (_program.Wiring == _reconciled)
                return;

            _reconciled = _program.Wiring;

            // Somebody has just said what a trigger is, so what is wired has changed under whatever a
            // screen is showing - including by Sprocket writing a slot itself a moment ago. Read it
            // again, or a sequence the player has just mended goes on being reported as unstartable
            // and parked a second later.
            _world.LookAgain();

            Reindex();
            Wanted(_wanting);

            // Nothing is written for a program that has just been loaded. Every button it asks for was
            // wired by the session that built it and the game has owned those slots ever since; a slot
            // that is empty now is somebody having cleared it by hand, which is an instruction and not
            // a gap to fill.
            if (_adopted)
            {
                Release();
                Take();
            }
            else
            {
                _adopted = true;
                Mend();
            }

            // Two lists swapped rather than one rebuilt, so comparing costs nothing. The previous set is all that is
            // remembered about wiring: what a panel actually holds is the panel's business, and the game's.
            List<Want> was = _wanted;
            _wanted = _wanting;
            _wanting = was;
        }

        /// <summary>
        /// Every button the program's triggers ask for, once per one of each system.
        /// </summary>
        /// <remarks>
        /// Per one of a system rather than per sequence, because a button belongs to one of them:
        /// the panel by the front airlock starts the front airlock, and the panel by the port one
        /// starts the port one. <see cref="SystemInstance.Resolve" /> is what turns the block
        /// slots a sequence names into the blocks this particular one uses, and a slot nobody
        /// filled resolves to what the system was built with - which is right for the first of
        /// them and an honest failure for a later one.
        /// </remarks>
        void Wanted(List<Want> into)
        {
            into.Clear();

            for (var s = 0; s < _program.Systems.Count; s++)
            {
                AutomationSystem system = _program.Systems[s];
                var sequences = Runs(system);
                var instances = system.Uses;

                for (var q = 0; q < sequences.Count; q++)
                {
                    for (var n = 0; n < instances.Count; n++)
                        WantedFor(into, system, instances[n], sequences[q]);
                }
            }
        }

        /// <summary>
        /// The buttons one sequence asks for on one of its system.
        /// </summary>
        /// <remarks>
        /// Every trigger that runs the sequence, not the first of them. Two buttons starting one
        /// sequence is the ordinary case - the one inside the airlock and the one outside - and a
        /// slot is wanted for each, on its own panel.
        /// </remarks>
        void WantedFor(List<Want> into, AutomationSystem system, SystemInstance instance,
            Sequence sequence)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (!triggers[i].IsAbout(sequence))
                    continue;

                var needed = triggers[i] as RequiredTrigger;
                if (needed != null)
                    WantedForTrigger(into, system, instance, sequence, needed);
            }
        }

        /// <summary>The toolbar slots <paramref name="instance" /> answers <paramref name="needed" /> with, each a want.</summary>
        void WantedForTrigger(List<Want> into, AutomationSystem system, SystemInstance instance,
            Sequence sequence, RequiredTrigger needed)
        {
            var blocks = _answering;
            var positions = _answeringAt;
            _program.Answers(system, instance, needed.Part, blocks, positions);

            for (var i = 0; i < blocks.Count && i < positions.Count; i++)
            {
                var at = positions[i];
                var slot = Toolbars.Index(ToolbarOf(blocks[i], at.Toolbar), at);
                if (slot < 0)
                    continue;

                var want = new Want();
                want.PanelId = PanelId(blocks[i]);
                want.Toolbar = at.Toolbar;
                want.Slot = slot;
                want.Key = needed.Acts != null ? KeyOf(system, instance, needed) : KeyOf(system, instance, sequence, needed.Stops);
                want.Reference = blocks[i];
                want.Start = GridWorld.Start(sequence.Id, instance);
                into.Add(want);
            }
        }

        readonly List<string> _answering = new List<string>();
        readonly List<ToolbarSlot> _answeringAt = new List<ToolbarSlot>();

        static bool Holds(List<Want> wants, Want one)
        {
            for (var i = 0; i < wants.Count; i++)
            {
                if (wants[i].Same(one))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Whether the block this want names is arranged to start exactly this.
        /// </summary>
        /// <remarks>Read off the slot itself: Sprocket's action carrying this want's key.</remarks>
        bool Wired(Want want)
        {
            GridEntity grid = _runner.Bindings.Entity(want.Reference);
            if (grid == null)
                return true;

            IMyToolbar held = ToolbarOf(want);
            MyToolbarItemInfo present;
            return held != null && held.TryGetItem(want.Slot, out present) && Asked(held, present, want);
        }

        readonly Dictionary<string, int> _claims = new Dictionary<string, int>(StringComparer.Ordinal);

        readonly Dictionary<string, int> _needed = new Dictionary<string, int>(StringComparer.Ordinal);

        /// <summary>Which key a trigger's slot holds: the one that runs, or the stopping one.</summary>
        string Wanting(AutomationSystem system, SystemInstance instance, Sequence sequence,
            Trigger trigger)
        {
            if (trigger.Stops)
                return KeyOf(system, instance, sequence, true);

            var needed = trigger as RequiredTrigger;
            return needed != null && needed.Acts != null ? KeyOf(system, instance, needed) : KeyOf(system, instance, sequence);
        }

        void Claims()
        {
            _claims.Clear();

            // Every slot of every page of every toolbar, since a trigger may be on any of them: a cockpit's target lock is
            // its third toolbar, and a timer runs all of its pages.
            for (var i = 0; i < _world.Entities.Count; i++)
            {
                GridEntity grid = _world.Entities[i];
                for (var t = 1; Toolbars.Of(grid.Block, t) != null; t++)
                    Claims(Toolbars.Of(grid.Block, t));
            }
        }

        /// <summary>Counts what each slot of <paramref name="toolbar" /> holds that answers this program.</summary>
        void Claims(IMyToolbar toolbar)
        {
            for (var slot = 0; slot < toolbar.ItemCount; slot++)
            {
                MyToolbarItemInfo present;
                if (!toolbar.TryGetItem(slot, out present))
                    continue;

                // Sprocket's action counts only where it points here. One pointed at another Sprocket runs that one's
                // program, and one pointed at a block that is gone runs nothing at all, so counting either would be this
                // program reporting a button that answers somebody else - or nobody - as its own.
                if (!Mine(present))
                    continue;

                _arguments.Clear();
                Toolbars.Parameters(toolbar, slot, _arguments);
                if (_arguments.Count == 0)
                    continue;

                var key = _arguments[0].Value as string;
                if (string.IsNullOrEmpty(key))
                    continue;

                Claimed(key);
            }
        }

        void Claimed(string key)
        {
            int held;
            _claims[key] = _claims.TryGetValue(key, out held) ? held + 1 : 1;
        }

        /// <summary>
        /// Whether enough of this construct answers what a sequence says it needs.
        /// </summary>
        /// <remarks>
        /// Nothing records which slot answers which declaration, so this is a count: three declared
        /// triggers and two slots means one of them is unanswered, and which one is a question only the
        /// person standing in front of it can settle.
        /// </remarks>
        bool Answered(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            // Counted per key rather than per sequence: a button that stops this sequence names it
            // by a different key from one that runs it, and one slot cannot answer for both.
            _needed.Clear();

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (!(triggers[i] is RequiredTrigger) || !triggers[i].IsAbout(sequence))
                    continue;

                var wanted = Wanting(system, instance, sequence, triggers[i]);

                int asked;
                _needed[wanted] = _needed.TryGetValue(wanted, out asked) ? asked + 1 : 1;
            }

            if (_needed.Count == 0)
                return true;

            foreach (var pair in _needed)
            {
                int found;
                _claims.TryGetValue(pair.Key, out found);
                if (found < pair.Value)
                    return false;
            }

            return true;
        }

        void Reach()
        {
            _unstartable.Clear();
            _reachable.Clear();
            _why.Clear();
            Claims();

            // What the construct has to provide, counted. A sequence nothing answers is reported the
            // same way one whose button was cleared is.
            for (var s = 0; s < _program.Systems.Count; s++)
            {
                AutomationSystem system = _program.Systems[s];
                var sequences = Runs(system);
                var instances = system.Uses;

                for (var q = 0; q < sequences.Count; q++)
                {
                    for (var n = 0; n < instances.Count; n++)
                    {
                        if (Answered(system, instances[n], sequences[q]))
                            continue;

                        var start = GridWorld.Start(sequences[q].Id, instances[n]);
                        if (_unstartable.Contains(start))
                            continue;

                        _unstartable.Add(start);
                        _why[start] = Unanswer(system, instances[n], sequences[q]);
                    }
                }
            }

            for (var w = 0; w < _wanted.Count; w++)
            {
                // A slot that no longer holds what the program wants leaves the program naming it: the trigger is still the
                // reference, and what it names is reported as not answering, until picking it again writes it back.
                Want want = _wanted[w];
                if (Wired(want))
                    _reachable.Add(want.Start);
            }

            for (var w = 0; w < _wanted.Count; w++)
            {
                Want want = _wanted[w];
                if (_reachable.Contains(want.Start) || _unstartable.Contains(want.Start))
                    continue;

                _unstartable.Add(want.Start);
                _why[want.Start] = Unanswer(ToolbarOf(want), want.Slot, want.Key, want.Reference);
            }

            _world.Unstartable(_unstartable, _why);
        }

        /// <summary>Whose button leaves <paramref name="sequence" /> unstarted on <paramref name="instance" />, and what its slot holds instead.</summary>
        /// <remarks>Asked only of a sequence already found unanswered, which is after the wiring changes and never on a tick.</remarks>
        Unanswered Unanswer(AutomationSystem system, SystemInstance instance, Sequence sequence)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var needed = triggers[i] as RequiredTrigger;
                if (needed == null || !needed.IsAbout(sequence))
                    continue;

                var wanted = Wanting(system, instance, sequence, needed);
                var blocks = _answering;
                var positions = _answeringAt;
                _program.Answers(system, instance, needed.Part, blocks, positions);
                for (var s = 0; s < blocks.Count && s < positions.Count; s++)
                {
                    IMyToolbar toolbar = ToolbarOf(blocks[s], positions[s].Toolbar);
                    var why = Unanswer(toolbar, Toolbars.Index(toolbar, positions[s]), wanted, blocks[s]);
                    if (why.Why != Unwired.Unknown)
                        return why;
                }
            }

            return new Unanswered(null, Unwired.Unknown);
        }

        /// <summary>What the slot of the button of <paramref name="reference" /> holds, where it is not <paramref name="wanted" />.</summary>
        Unanswered Unanswer(IMyToolbar toolbar, int slot, string wanted, string reference)
        {
            var block = _program.Called(reference, _world.Values);
            if (toolbar == null || slot < 0)
                return new Unanswered(block, Unwired.Unknown);

            MyToolbarItemInfo present;
            if (!toolbar.TryGetItem(slot, out present))
                return new Unanswered(block, Unwired.Cleared);

            return new Unanswered(block, KeyIn(toolbar, slot, present) == wanted ? Unwired.Unknown : Unwired.Taken);
        }

        /// <summary>The key a slot holds where it holds this Sprocket's action, or null.</summary>
        string KeyIn(IMyToolbar toolbar, int slot, MyToolbarItemInfo present)
        {
            if (!Mine(present))
                return null;

            _arguments.Clear();
            Toolbars.Parameters(toolbar, slot, _arguments);
            return _arguments.Count == 0 ? null : _arguments[0].Value as string;
        }

        readonly List<long> _unstartable = new List<long>();
        readonly HashSet<long> _reachable = new HashSet<long>();
        readonly Dictionary<long, Unanswered> _why = new Dictionary<long, Unanswered>();

        /// <summary>
        /// Rewrites the slots that are Sprocket's and say nothing it understands.
        /// </summary>
        /// <remarks>
        /// The one exception to leaving a loaded program's slots alone, and it is not a loophole in
        /// that rule - it is the rule's missing case. Three things a slot can be, and only one of them
        /// is a person's decision:
        /// <list type="bullet">
        /// <item>empty, or holding somebody else's action - a person changed it, so it stands;</item>
        /// <item>Sprocket's, with a key that resolves - working, so leave it;</item>
        /// <item><b>Sprocket's, with a key that means nothing</b> - nobody typed that, and nobody could.</item>
        /// </list>
        /// The third can only be Sprocket's own writing gone stale: a key format that has changed
        /// underneath it, or a payload of the wrong type entirely. Leaving it would leave somebody's
        /// buttons permanently dead with no way back but re-picking each one, and calling that
        /// "respecting their choice" would be dishonest about whose writing it is.
        /// <para>
        /// Which makes this the migration path for the key, once and for all. Change what a key looks
        /// like and every slot in every saved world mends itself on the next load, with nothing to
        /// version and nothing to ask anybody.
        /// </para>
        /// </remarks>
        void Mend()
        {
            for (var w = 0; w < _wanting.Count; w++)
            {
                Want want = _wanting[w];

                IMyToolbar toolbar = ToolbarOf(want);
                MyToolbarItemInfo present;

                if (toolbar == null || !toolbar.TryGetItem(want.Slot, out present)
                    || (!Mine(present) && !Orphaned(present)))
                {
                    continue;
                }

                // One of Sprocket's that already says something this program can resolve is left alone.
                // An orphan is rewritten whatever its key says, because the id is the broken part:
                // the block it names is gone, so nothing it says can be honoured by anybody.
                if (Mine(present) && Understood(toolbar, want.Slot))
                    continue;

                if (!Toolbars.Write(toolbar, want.Slot, TriggerAction.Slot(_block.EntityId),
                        TriggerAction.Argument(want.Key)))
                {
                    Told.Say("the write was refused mending slot " + want.Slot);
                    continue;
                }

                Told.Say("mended slot " + want.Slot + ", now " + want.Key);
            }
        }

        /// <summary>Whether a slot of Sprocket's carries a key this program can resolve.</summary>
        /// <remarks>
        /// Read once per wanted button on the first pass after a load, never on a tick. A slot holds
        /// whatever was written into it rather than what the action declares, so anything that is not
        /// a key this program knows answers no - wrong type included.
        /// </remarks>
        bool Understood(IMyToolbar toolbar, int slot)
        {
            _arguments.Clear();
            Toolbars.Parameters(toolbar, slot, _arguments);

            if (_arguments.Count == 0)
                return false;

            var key = _arguments[0].Value as string;
            return !string.IsNullOrEmpty(key) && _byKey.ContainsKey(key);
        }

        readonly List<TerminalActionParameter> _arguments = new List<TerminalActionParameter>();

        /// <summary>
        /// Lets go of every button the program no longer asks for.
        /// </summary>
        /// <remarks>
        /// Only ever clears a slot Sprocket still holds. Somebody who re-pointed the slot themselves
        /// has said what they want it to do, and taking it back would be Sprocket arguing with them.
        /// </remarks>
        void Release()
        {
            for (var i = 0; i < _wanted.Count; i++)
            {
                Want had = _wanted[i];
                if (Holds(_wanting, had))
                    continue;

                IMyToolbar toolbar = ToolbarOf(had);
                MyToolbarItemInfo present;

                if (toolbar == null || !toolbar.TryGetItem(had.Slot, out present)
                    || !Asked(toolbar, present, had))
                {
                    // Already empty, already somebody else's, or no longer what this want asked for.
                    // None of those is Sprocket's to clear, and whoever changed it has said what they
                    // wanted it to do.
                    continue;
                }

                Toolbars.Clear(toolbar, had.Slot);
                Told.Say("released slot " + had.Slot + " on panel " + had.PanelId);
            }
        }

        /// <summary>Claims every button the program asks for and Sprocket does not already hold.</summary>
        void Take()
        {
            for (var w = 0; w < _wanting.Count; w++)
            {
                Want want = _wanting[w];

                // Only what is newly asked for. A button somebody cleared is no longer asked for at
                // all - the trigger stopped naming it - so there is nothing here that could put one
                // back, and picking it again is an honest new want.
                if (Holds(_wanted, want))
                    continue;

                IMyToolbar toolbar = ToolbarOf(want);
                if (toolbar == null)
                    continue;

                MyToolbarItemInfo present;
                var filled = toolbar.TryGetItem(want.Slot, out present);

                // Already exactly this.
                if (filled && Asked(toolbar, present, want))
                    continue;

                if (filled && !Mine(present) && !Orphaned(present))
                {
                    // Somebody's panel already does something. Overwriting it would configure
                    // Sprocket by breaking their ship, which is worse than not working - and another
                    // Sprocket holding it is somebody, whose program that button belongs to.
                    Told.Say("slot " + want.Slot + " in use by " + present.Kind + " " + present.ActionId);
                    continue;
                }

                if (!Toolbars.Write(toolbar, want.Slot, TriggerAction.Slot(_block.EntityId), TriggerAction.Argument(want.Key)))
                {
                    Told.Say("the write was refused for slot " + want.Slot);
                    continue;
                }

                Told.Say("wired slot " + want.Slot + " to " + want.Key);
            }
        }

        /// <summary>Whether a slot already holds exactly what a want asks for.</summary>
        /// <remarks>
        /// Sprocket's own action is exactly this only when it carries this want's key. The action on
        /// this block with any other key is this block's writing for something else - a program it has
        /// replaced, or a sequence it no longer has - and is to be written over, not taken as done.
        /// </remarks>
        bool Asked(IMyToolbar toolbar, MyToolbarItemInfo present, Want want)
        {
            if (!Mine(present))
                return false;

            _arguments.Clear();
            Toolbars.Parameters(toolbar, want.Slot, _arguments);

            return _arguments.Count > 0
                && string.Equals(_arguments[0].Value as string, want.Key, StringComparison.Ordinal);
        }

        /// <summary>
        /// Whether a slot holds Sprocket's action pointed at <em>this</em> Sprocket.
        /// </summary>
        /// <remarks>
        /// The action's name alone is not enough, and answering with it was a real fault: cut the
        /// programmable block and paste it back and the game gives it a new id, so every slot on the
        /// construct names a block that is gone. Reading only the name, Sprocket found its own action
        /// in each of them and reported every button as wired while none of them did anything.
        /// <para>
        /// The id it needs was in the slot the whole time, because Sprocket writes its own block into
        /// the action it puts there.
        /// </para>
        /// </remarks>
        bool Mine(MyToolbarItemInfo present)
        {
            return TriggerAction.IsOurs(present) && present.BlockEntityId == _block.EntityId;
        }

        /// <summary>
        /// Whether a slot holds Sprocket's action pointed at a block that is no longer there.
        /// </summary>
        /// <remarks>
        /// Wiring nothing can honour, and nobody decided on: the block it names is gone, so there is
        /// no Sprocket left for that button to reach and no owner to argue with. It is adopted rather
        /// than reported, which is what makes a programmable block survive being cut and pasted, or
        /// ground down while another holds the same systems.
        /// <para>
        /// A live block is somebody's, even when that somebody is another Sprocket, and a slot that is
        /// simply empty is somebody having cleared it - which is an instruction. Neither is touched.
        /// </para>
        /// </remarks>
        bool Orphaned(MyToolbarItemInfo present)
        {
            return TriggerAction.IsOurs(present)
                && present.BlockEntityId != _block.EntityId
                && !Toolbars.Exists(present.BlockEntityId);
        }

        long PanelId(string reference)
        {
            GridEntity grid = _runner.Bindings.Entity(reference);
            return grid == null ? 0 : grid.Block.EntityId;
        }

        IMyToolbar ToolbarOf(string reference, int toolbar)
        {
            GridEntity grid = _runner.Bindings.Entity(reference);
            return grid == null ? null : ToolbarOfBlock(grid.Block, toolbar);
        }

        /// <summary>The toolbar a want is on.</summary>
        IMyToolbar ToolbarOf(Want want)
        {
            for (var i = 0; i < _world.Entities.Count; i++)
            {
                GridEntity grid = _world.Entities[i];
                if (grid.Block.EntityId == want.PanelId)
                    return ToolbarOfBlock(grid.Block, want.Toolbar);
            }

            return null;
        }

        /// <summary>Toolbar <paramref name="toolbar" /> of <paramref name="block" />, from 1, whatever block it is, or null where it has none.</summary>
        static IMyToolbar ToolbarOfBlock(IMyTerminalBlock block, int toolbar)
        {
            return Toolbars.Of(block, toolbar);
        }

        public void Tick(int frame)
        {
            // A copy leaves the toolbars to the machine that keeps the Sprocket, which is the one that
            // writes them and the one that knows whether they still hold what it wrote.
            if (!_copy)
                Touch();

            _runner.Tick(frame);

            if (Showing && _editor != null)
                _editor.Tick(frame);
        }

        /// <summary>Brings the toolbars into line with the program, and reads them where something asked.</summary>
        void Touch()
        {
            Reconcile();

            // Only when something asks. Reading a toolbar is the expensive thing Sprocket does, and what
            // it is read for is what a screen says about a sequence - so a screen asks when it opens,
            // and can ask again when somebody suspects another player has wired something. Nothing is
            // read on a timer, and nothing is read while nobody is looking.
            //
            // Firing does not depend on any of it: a slot invokes Sprocket's action itself and arrives
            // carrying its key, so a button works whether or not anything has ever looked at it.
            if (_asserted.Count > 0)
                Assert();

            if (_world.TakeLook() || !_read)
            {
                _read = true;
                Reach();
            }
        }

        /// <summary>Writes every slot the program wants on <paramref name="panel" /> on the next pass, whatever the slot holds by then.</summary>
        /// <remarks>
        /// A person picking a button in the editor, after being asked about anything its slot already held. That pick is the
        /// instruction a slot changed by hand waits for, so it is written even where Sprocket believes it already did.
        /// </remarks>
        public void Wire(long panel)
        {
            if (!_asserted.Contains(panel))
                _asserted.Add(panel);
        }

        /// <summary>Writes the slots <see cref="Wire" /> asked for that do not hold what the program wants of them.</summary>
        /// <remarks>
        /// After the program's own pass, so what is wanted is what the edit that picked the button left. A panel nothing wants
        /// yet stays asked for: a trigger joins the program only once what it does is chosen, some screens after its button.
        /// </remarks>
        void Assert()
        {
            _answered.Clear();
            for (var w = 0; w < _wanted.Count; w++)
            {
                Want want = _wanted[w];
                if (!_asserted.Contains(want.PanelId))
                    continue;

                if (!_answered.Contains(want.PanelId))
                    _answered.Add(want.PanelId);

                IMyToolbar toolbar = ToolbarOf(want);
                if (toolbar == null)
                    continue;

                MyToolbarItemInfo present;
                if (toolbar.TryGetItem(want.Slot, out present) && Asked(toolbar, present, want))
                    continue;

                if (!Toolbars.Write(toolbar, want.Slot, TriggerAction.Slot(_block.EntityId), TriggerAction.Argument(want.Key)))
                {
                    Told.Say("the write was refused for slot " + want.Slot);
                    continue;
                }

                Told.Say("wired slot " + want.Slot + " to " + want.Key + ", as picked");
            }

            if (_answered.Count == 0)
                return;

            for (var i = 0; i < _answered.Count; i++)
                _asserted.Remove(_answered[i]);

            _world.LookAgain();
        }

        readonly List<long> _asserted = new List<long>();
        readonly List<long> _answered = new List<long>();

        /// <summary>Whether this Sprocket's screen is open in front of somebody.</summary>
        /// <remarks>
        /// Set by the screen as it opens and closes. The editor has nothing to do while nobody is
        /// looking, so it is ticked only while this holds; the program runs either way.
        /// </remarks>
        public bool Showing { get; set; }

        /// <summary>What a block holds under one of Sprocket's keys, or empty.</summary>
        static string Stored(IMyTerminalBlock block, Guid key)
        {
            MyModStorageComponentBase storage = block.Storage;
            string text;
            return storage != null && storage.TryGetValue(key, out text) && text != null ? text : "";
        }

        /// <summary>
        /// Reads the program, and what is switched off, out of the text they were written as.
        /// </summary>
        /// <remarks>
        /// Nothing written is a Sprocket nobody has written a program on yet, which is an empty ship
        /// rather than an error.
        /// </remarks>
        void Load(string text, string state)
        {
            if (string.IsNullOrEmpty(text))
                return;

            string warning;
            var trouble = ProgramText.Load(text, _program, _world.Values, out warning);

            if (warning.Length > 0)
                Told.Say(_block.CustomName + " has a program with a warning: " + warning);

            // What is switched off, read after the program, because it names what the program
            // declared. A construct that has nothing parked has nothing here at all.
            if (!string.IsNullOrEmpty(state))
            {
                var wrong = StateText.Load(state, _program, _runner);
                if (wrong.Length > 0)
                    Told.Say(_block.CustomName + " has switches that would not read: " + wrong);
            }

            if (trouble.Length == 0)
                return;

            // Logged here; the editor says it on its notice and shows the text on the script page, where it is mended.
            // Nothing of the program was kept, and it is marked not to be saved over, which is what protects somebody's work.
            Told.Say(_block.CustomName + " has a program that would not read: " + trouble);
        }

        /// <summary>
        /// Writes the program into the hero, so the world save carries it.
        /// </summary>
        /// <remarks>
        /// Written whenever the program might have changed, and as the world saves, from the session's object builder: the
        /// game asks for that before it writes the entities, and runs a session component's own save after it has.
        /// </remarks>
        public void Persist()
        {
            // The machine that keeps the Sprocket writes it; a copy is only somebody's view of it.
            if (_copy)
                return;

            // Into the hero as the construct stands now, which may have changed since the last tick.
            Seek();
            if (Lost)
                return;

            MyModStorageComponentBase storage = Attached(_block);
            if (storage == null)
                return;

            // A failed load is failed, and nothing is written over it. What lifts that is the person
            // answering the question their first edit raises - see SprocketEditor - so by the time
            // anything reaches here the decision has been taken, one way or the other.
            if (!_program.Intact)
                return;

            string program;
            string state;
            Written(out program, out state);
            storage.SetValue(ProgramKey, program);
            // What is switched off is kept under a key of its own, apart from the program somebody shares. The key
            // is claimed in EntityComponents.sbc, without which it would work all session and vanish on save.
            storage.SetValue(StateKey, state);
        }

        /// <summary>Keeps a backup of the logic systems the library has no equal of, as this construct's automation is lost.</summary>
        /// <remarks>
        /// A logic system whose blueprint in <paramref name="shelf" /> has the same identity and the same change date is left out,
        /// since the library has it already; where that is every one, no backup is kept. Only the logic systems are kept: their
        /// installations stay behind with their blocks.
        /// </remarks>
        public void BackUp(Backups backups, DateTime saved, Shelf shelf)
        {
            if (!_program.Intact)
                return;

            // Whatever was changed a moment ago is dated before it is compared.
            if (_editor != null)
                _editor.Stamp(true);

            var unkept = new List<AutomationSystem>();
            var systems = _program.Systems;
            for (var i = 0; i < systems.Count; i++)
            {
                var kept = shelf.Find(systems[i].Identity);
                if (kept == null || kept.Changed != systems[i].Changed)
                    unkept.Add(systems[i]);
            }

            if (unkept.Count == 0)
                return;

            _world.GridIds(_backedGrids);
            backups.Take(_world.Name, saved, _backedGrids, ProgramText.Blueprints(unkept));
        }

        /// <summary>
        /// Writes a program and its switches into a client's copy of a block, as the server wrote them.
        /// </summary>
        /// <remarks>
        /// The game copies, pastes and blueprints a grid from the copy on the machine doing it, and it
        /// does not carry a block's storage from the server to the clients. A client's copy of the block
        /// holds what the server last sent here, so a grid a client copies takes its programs along.
        /// </remarks>
        public static void Store(IMyTerminalBlock block, string program, string switches)
        {
            MyModStorageComponentBase storage = Attached(block);
            if (storage == null)
                return;

            storage.SetValue(ProgramKey, program);
            storage.SetValue(StateKey, switches);
        }

        /// <summary>The storage on a block, attached where it has none yet; or null, which is logged.</summary>
        static MyModStorageComponentBase Attached(IMyTerminalBlock block)
        {
            MyModStorageComponentBase storage = block.Storage;

            // Shared with every other mod on this block, so one may already be here - and if it is, it is
            // the one to use rather than one to replace. Assigning it is what registers it with the
            // block's component container, which is what makes the game write it out at all; the getter
            // only answers once that has happened, so it is worth asking again rather than assuming.
            if (storage == null)
            {
                block.Storage = new MyModStorageComponent();
                storage = block.Storage;
            }

            // Logged rather than thrown. This runs on the way out of a screen and from the world save,
            // and taking the game down over it would cost far more than the program does.
            if (storage == null)
                Told.Say("mod storage would not attach to " + block.CustomName + "; program not saved");

            return storage;
        }

        /// <summary>
        /// Runs a sequence by hand, or stops it, named the way another machine names it: the system by
        /// identity, and the one of it and the sequence by number.
        /// </summary>
        /// <remarks>
        /// What a client running something from its copy arrives as. Nothing happens where what it names
        /// is not here, which is a change of the client's the server has not built yet.
        /// </remarks>
        public void Order(Guid system, int instance, int sequence, bool stops)
        {
            AutomationSystem named = _program.FindSystem(system);
            SystemInstance one = named == null ? null : named.FindInstance(instance);
            Sequence which = named == null ? null : named.FindSequence(sequence);
            if (one == null || which == null)
            {
                Told.Say(_block.CustomName + " has no " + (named == null ? "such system" : which == null ? "such sequence" : "such instance")
                    + " for an order to " + (stops ? "stop" : "run"));
                return;
            }

            if (!stops)
            {
                _runner.Call(which, one, true);
                return;
            }

            var state = _runner.Get(which, one);
            if (state != null)
                state.Interrupt(Stopper.Hand, 0);
        }

        /// <summary>
        /// The program, and what is switched off, as they are written into the block: what a copy on
        /// another machine is made from.
        /// </summary>
        public void Written(out string program, out string state)
        {
            // Whatever was changed a moment ago is dated before it is written.
            if (_editor != null)
                _editor.Stamp(true);

            program = ProgramText.Save(_program, _world.Values);
            state = StateText.Save(_program, _runner);
        }

        /// <summary>Has the editor build its words again, after the language changes; an editor not yet made builds them when it is.</summary>
        public void Reword()
        {
            if (_editor != null)
                _editor.Reword();
        }

        /// <summary>Keeps the automation in another programmable block on the construct, now that the hero is gone, answering whether it moved.</summary>
        /// <remarks>
        /// Where no programmable block is left the automation is <see cref="Lost" />, and whoever holds this lets it go.
        /// </remarks>
        public bool Rehome()
        {
            if (!Seek())
                return false;

            Persist();
            return true;
        }

        void Removed()
        {
            if (HeroRemoved != null)
                HeroRemoved(this);
        }

        /// <summary>
        /// Moves into another programmable block on the construct where the hero has gone, answering whether it moved.
        /// </summary>
        /// <remarks>
        /// Asked when the hero is removed and as the world saves, never on a tick. The hero found again under its own id is
        /// the same block brought back as a new object, which a grid splitting does; it is taken as it is. Otherwise the first
        /// programmable block found becomes the hero, and where there is none the automation is lost.
        /// </remarks>
        bool Seek()
        {
            if (Lost)
                return false;

            GridEntity found = _world.FindEntity(_block.EntityId);
            if (found != null)
            {
                if (found.Block == _block)
                    return false;

                Move(found.Block);
                return true;
            }

            IReadOnlyList<GridEntity> entities = _world.Entities;
            for (var i = 0; i < entities.Count; i++)
            {
                if (!(entities[i].Block is IMyProgrammableBlock))
                    continue;

                Move(entities[i].Block);
                return true;
            }

            Told.Say(_block.CustomName + " is gone, and its construct has no programmable block left to keep the automation");
            Lost = true;
            return false;
        }

        /// <summary>Makes <paramref name="block" /> the hero: the automation is kept in it from here on, and its buttons name it.</summary>
        void Move(IMyTerminalBlock block)
        {
            Told.Say(_block.CustomName + " is gone; " + block.CustomName + " keeps the automation now");

            _block.IsWorkingChanged -= Working;
            _block = block;
            _block.IsWorkingChanged += Working;
            _runner.Live = _block.IsWorking;
            _world.Anchor(block);

            // The construct's buttons still name the block that is gone, so the next pass mends them as it does after a load.
            _adopted = false;
            _reconciled = int.MinValue;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _block.IsWorkingChanged -= Working;
            _world.AnchorRemoved -= Removed;
            _runner.Retire();
            _world.Dispose();
        }
    }
}
