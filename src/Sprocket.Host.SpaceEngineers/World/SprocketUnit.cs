using Sprocket.Host.SpaceEngineers.Memory;
using System.Collections.Generic;
using System;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sandbox.ModAPI;
using VRage.Game.Components;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One Sprocket, on one construct: its world, its program, the runtime that runs it, and the editor.
    /// </summary>
    /// <remarks>The session ticks it whether or not a screen is open on it.</remarks>
    public sealed class SprocketUnit : IDisposable
    {
        IMyTerminalBlock _block;

        Backups _backups;

        LibraryShelf _shelf;

        Preferences _preferences;

        PlayerMemory _viewer;

        Func<bool> _administers;

        Func<Settings> _worldSettings;

        Action<Settings> _changeSettings;

        readonly bool _copy;

        readonly BlockKinds _kinds;

        readonly GridWorld _world;
        readonly Automation _program = new Automation();
        readonly AutomationRunner _runner;
        readonly HostBlocks _holders;
        readonly Ownership _hands;
        readonly SlotKeys _keys;
        readonly WiredButtons _buttons;
        readonly UnreachableSequences _reach;

        readonly Texts _texts;
        readonly ConstructMemory _memory;
        readonly string _author;
        bool _developerMode;

        SprocketEditor _editor;

        /// <summary>The Sprocket a block carries, reading its program out of the block and what it is doing out of <paramref name="memory" />.</summary>
        /// <remarks><paramref name="rota" /> and <paramref name="kinds" /> are the world's, shared by every Sprocket in it.</remarks>
        public SprocketUnit(IMyTerminalBlock block, Settings settings, Rota rota, BlockKinds kinds, Texts texts, ConstructMemory memory,
            Backups backups = null)
            : this(block, settings, rota, kinds, texts, false, BlockStorage.Stored(block, BlockStorage.ProgramKey),
                BlockStorage.Stored(block, BlockStorage.StateKey), memory)
        {
            _backups = backups;
        }

        /// <summary>
        /// A copy of the Sprocket another machine keeps on <paramref name="block" />, made from what that
        /// machine wrote out, to be edited here, its blocks of the world's <paramref name="kinds" />.
        /// </summary>
        /// <remarks>On a rota of its own that nothing turns, so none of its sequences acts.</remarks>
        public static SprocketUnit Copy(IMyTerminalBlock block, Settings settings, BlockKinds kinds, Texts texts, string program,
            string state)
        {
            return new SprocketUnit(block, settings, new Rota(), kinds, texts, true, program, state,
                new ConstructMemory { Keeper = block.EntityId });
        }

        /// <summary>
        /// This Sprocket built again from <paramref name="program" /> and <paramref name="switches" />,
        /// carrying on with whatever it was running.
        /// </summary>
        /// <remarks>Called on the server for a client's edit. The caller puts the new one in this one's place and disposes of this.</remarks>
        public SprocketUnit Rebuilt(string program, string switches, Settings settings, Rota rota)
        {
            var rebuilt = new SprocketUnit(_block, settings, rota, _kinds, _texts, false, program, switches, _memory);
            rebuilt._backups = _backups;
            rebuilt._shelf = _shelf;
            rebuilt._preferences = _preferences;
            rebuilt._viewer = _viewer;
            rebuilt.Showing = Showing;
            if (_editor != null)
                rebuilt.Editor.Habits = _editor.Habits;

            // What this one had wired goes with it: a Sprocket that believed it had just been loaded would write no slot.
            rebuilt._buttons.Carry(_buttons);
            return rebuilt;
        }

        SprocketUnit(IMyTerminalBlock block, Settings settings, Rota rota, BlockKinds kinds, Texts texts, bool copy, string program,
            string state, ConstructMemory memory)
        {
            _block = block;
            _memory = memory;
            _program.Memory = memory;
            _copy = copy;
            _kinds = kinds;
            _world = new GridWorld(block, kinds);
            _runner = new AutomationRunner(_program, _world, rota);
            _runner.FastestUpdate = settings?.FastestUpdate ?? 1;
            _keys = new SlotKeys(_program, _runner);
            _buttons = new WiredButtons(_world, _program, _runner, _keys, block);
            _reach = new UnreachableSequences(_world, _program, _runner, _buttons);

            _texts = texts;
            _developerMode = settings != null && settings.DeveloperMode;

            _author = MyAPIGateway.Multiplayer == null ? null : Player.Plain(MyAPIGateway.Multiplayer.MyName);

            Load(program, state);

            // After the load, so reading the program is no change to it. A copy writes nothing.
            if (!copy)
                _program.Changed += Edited;

            // Listened for rather than asked on every tick. Released in Dispose: a listener left on a block the game has let go
            // of holds the whole unit alive.
            _hands = new Ownership(_world, _program, _runner);
            _holders = new HostBlocks(_world);
            _holders.Changed += Working;
            _holders.Gathered += Regathered;
            _holders.Hold(_block);
            _runner.Live = _holders.Working && !_program.Paused;
            _world.AnchorRemoved += Removed;
            _world.AnchorClosed += Closed;
            _world.Renamed += Renamed;
        }

        /// <summary>Raised when what the hero would hold changes: the program or what is switched off, from the program's own event.</summary>
        /// <remarks>Raised in the middle of the change, so whatever writes the hero waits until it is done.</remarks>
        public event Action<SprocketUnit> Changed;

        void Edited()
        {
            if (Changed != null)
                Changed(this);
        }

        /// <summary>Whether any of the construct's programmable blocks working changed, which decides whether the automation runs.</summary>
        void Working()
        {
            _runner.Live = _holders.Working && !_program.Paused;
        }

        /// <summary>Programmable blocks left or joined the construct.</summary>
        void Regathered()
        {
            if (Gathered != null)
                Gathered(this);
        }

        /// <summary>A block on the construct renamed, so the screen names it anew: at once where it is open, as it next opens otherwise.</summary>
        /// <remarks>Only marks the screen to be built again, since the game raises this in the middle of renaming.</remarks>
        void Renamed()
        {
            if (_editor != null)
                _editor.Invalidate();
        }

        /// <summary>The programmable block the automation is kept in: the construct's hero.</summary>
        public IMyTerminalBlock Block => _block;

        /// <summary>What the construct's automation is doing, filed under the block that keeps it, which the world's save writes.</summary>
        public ConstructMemory Memory => _memory;

        /// <summary>Starts its clock at <paramref name="now" />, Sprocket's tick as it is made, so anything before its first tick is told the time.</summary>
        public void Begin(int now)
        {
            _runner.Begin(now);
        }

        /// <summary>Whether the construct has no programmable block left to keep the automation in, which ends it.</summary>
        public bool Lost { get; private set; }

        /// <summary>
        /// Whether the hero was last taken off the construct by itself, destroyed or ground down, rather than closed with its
        /// whole grid; where that left the automation <see cref="Lost" />, it is backed up.
        /// </summary>
        /// <remarks>The game closes the blocks of a grid deleted whole, in creative or by the server's cleanup, without removing any.</remarks>
        public bool Destroyed { get; private set; }

        /// <summary>Raised when the hero is removed from the construct or closed with its grid, from the game's own event; answered with <see cref="Rehome" />.</summary>
        /// <remarks>Raised while the grid is still changing, so whatever answers it calls <see cref="Rehome" /> once the grid has finished.</remarks>
        public event Action<SprocketUnit> HeroRemoved;

        /// <summary>Raised when programmable blocks leave or join the construct.</summary>
        /// <remarks>Raised on a tick, so an answer that builds Sprockets waits for the tick to end.</remarks>
        public event Action<SprocketUnit> Gathered;

        /// <summary>The construct's programmable blocks, the hero among them.</summary>
        public IReadOnlyList<IMyTerminalBlock> Holders => _holders.All;

        /// <summary>The programmable blocks that just left the construct and still stand.</summary>
        public IReadOnlyList<IMyTerminalBlock> Parted => _holders.Parted;

        /// <summary>The player's library, which the editor is given; null on a machine that keeps none.</summary>
        public LibraryShelf Shelf
        {
            get { return _shelf; }
            set
            {
                _shelf = value;
                if (_editor != null)
                    _editor.Shelf = value;
            }
        }

        /// <summary>How the player likes the screens to look, which the editor is given; null on a machine that keeps none.</summary>
        public Preferences Preferences
        {
            get { return _preferences; }
            set
            {
                _preferences = value;
                if (_editor != null)
                    _editor.Preferences = value;
            }
        }

        /// <summary>Whether the player at the screen may change the world's settings now, which the editor is given.</summary>
        public Func<bool> Administers
        {
            get { return _administers; }
            set
            {
                _administers = value;
                if (_editor != null)
                    _editor.Administers = value;
            }
        }

        /// <summary>The settings the world runs by, which the editor is given.</summary>
        public Func<Settings> WorldSettings
        {
            get { return _worldSettings; }
            set
            {
                _worldSettings = value;
                if (_editor != null)
                    _editor.WorldSettings = value;
            }
        }

        /// <summary>Where a change of the world's settings is made, which the editor is given.</summary>
        public Action<Settings> ChangeSettings
        {
            get { return _changeSettings; }
            set
            {
                _changeSettings = value;
                if (_editor != null)
                    _editor.ChangeSettings = value;
            }
        }

        /// <summary>Runs by <paramref name="settings" /> from now on, the world's settings an admin has just changed.</summary>
        public void Apply(Settings settings)
        {
            _runner.FastestUpdate = settings.FastestUpdate;
            _developerMode = settings.DeveloperMode;
            if (_editor == null)
                return;

            _editor.DeveloperMode = _developerMode;
        }

        /// <summary>The settings in this world of the player at the screen, which the editor is given.</summary>
        public PlayerMemory Viewer
        {
            get { return _viewer; }
            set
            {
                _viewer = value;
                if (_editor != null)
                    _editor.Viewer = value;
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
                _editor.Preferences = _preferences;
                _editor.Viewer = _viewer;
                _editor.Administers = _administers;
                _editor.WorldSettings = _worldSettings;
                _editor.ChangeSettings = _changeSettings;

                // A copy's buttons are written where the Sprocket is kept, which is asked by whoever lent it.
                if (!_copy)
                {
                    _editor.Wire = Wire;
                    _editor.Unwire = Unwire;
                    _editor.Returned = _hands.Returned;
                }

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

        /// <summary>The construct it runs on.</summary>
        public GridWorld World => _world;

        /// <summary>What runs its program.</summary>
        public AutomationRunner Runner => _runner;


        /// <summary>A block fired one of the actions Sprocket put in it, carrying <paramref name="key" />.</summary>
        public void Fire(string key)
        {
            _keys.Fire(key);
        }

        /// <summary>Runs its program for the game's frame <paramref name="frame" />, and keeps the toolbars it owns as it wants them.</summary>
        public void Tick(int frame)
        {
            // A copy leaves the toolbars to the machine that keeps the Sprocket.
            if (!_copy)
                Touch();

            _holders.Hold(_block);

            // Two flags read, because a pause is set by an edit or by the construct changing hands, and both land between ticks.
            _runner.Live = _holders.Working && !_program.Paused;
            _runner.Tick(frame);
        }

        /// <summary>Brings an open screen up to the frame <paramref name="frame" />, after the rota has run its sequences for it.</summary>
        /// <remarks>After the runs, so what a run changed on a frame is on the screen drawn for that frame.</remarks>
        public void Show(int frame)
        {
            if (Showing && _editor != null)
                _editor.Tick(frame);
        }

        /// <summary>Whether <paramref name="player" /> may open the automation: one of the construct's big owners, or anybody where nobody owns any of it.</summary>
        public bool Owns(long player)
        {
            return _hands.Owns(player);
        }

        /// <summary>Brings the toolbars into line with the program, and reads them where something asked.</summary>
        void Touch()
        {
            if (_hands.Due)
            {
                _hands.Owned(_block);

                // A hero none of the big owners may use is out of their reach, and gone from the construct as one ground off is.
                if (_world.FindEntity(_block.EntityId) == null)
                    Removed();
            }

            _buttons.Reconcile();
            _reach.Look();
        }

        /// <summary>Takes over, on its next pass, its program's buttons that still name <paramref name="from" />, the block its automation came from.</summary>
        public void TakeOver(long from)
        {
            _buttons.TakeOver(from);
        }

        /// <summary>Writes every slot the program wants on <paramref name="panel" /> on the next pass, whatever the slot holds by then.</summary>
        public void Wire(long panel)
        {
            _buttons.Wire(panel);
        }

        /// <summary>Lets go of every panel <see cref="Wire" /> asked for and no pass has written yet, as a pick is backed out of.</summary>
        public void Unwire()
        {
            _buttons.Unwire();
        }

        /// <summary>Brings the toolbars into line with the program on the next tick, as though its wiring had changed.</summary>
        public void Revisit()
        {
            _buttons.Revisit();
        }

        /// <summary>Whether this Sprocket's screen is open in front of somebody.</summary>
        /// <remarks>Set by the screen as it opens and closes. The editor is ticked only while it holds; the program runs either way.</remarks>
        public bool Showing
        {
            get { return _runner.Looking; }
            set { _runner.Looking = value; }
        }

        /// <summary>
        /// Reads the program, and what is switched off, out of the text they were written as.
        /// </summary>
        void Load(string text, string state)
        {
            if (string.IsNullOrEmpty(text))
                return;

            string warning;
            var trouble = ProgramReader.Load(text, _program, _world.Values, out warning, _world);

            if (warning.Length > 0)
                GameLog.Say($"{Logged(_block)} has a program with a warning: {warning}");

            // Read after the program, because it names what the program declared. Only a copy being edited takes the runs
            // from it, for show.
            if (!string.IsNullOrEmpty(state))
            {
                var wrong = StateText.Load(state, _program, _copy ? _runner : null);
                if (wrong.Length > 0)
                    GameLog.Say($"{Logged(_block)} has switches that would not read: {wrong}");
            }

            if (trouble.Length == 0)
                return;

            // Nothing of the program was kept, and it is marked not to be saved over.
            GameLog.Say($"{Logged(_block)} has a program that would not read: {trouble}");
        }

        /// <summary>How the log names <paramref name="block" />: its name, or where it has none yet, as a dedicated server
        /// loads a world, its type, id and construct.</summary>
        public static string Logged(IMyTerminalBlock block)
        {
            if (!string.IsNullOrEmpty(block.CustomName))
                return block.CustomName;

            var grid = block.CubeGrid == null ? "" : $" on {block.CubeGrid.DisplayName}";
            return $"{block.DefinitionDisplayNameText} {block.EntityId}{grid}";
        }

        /// <summary>
        /// Writes the program into the hero, so the world save carries it.
        /// </summary>
        /// <remarks>
        /// Called whenever the program might have changed, and as the world saves from the session's object builder, which the
        /// game asks for before it writes the entities.
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

            MyModStorageComponentBase storage = BlockStorage.Attached(_block);
            if (storage == null)
                return;

            // Nothing is written over a failed load.
            if (!_program.Intact)
                return;

            if (_editor != null)
                _editor.Stamp(true);

            var program = ProgramWriter.Save(_program, _world.Values);
            var state = StateText.Save(_program, null, false);
            storage.SetValue(BlockStorage.ProgramKey, program);
            // Under a key of its own, apart from the program somebody shares; the key is claimed in EntityComponents.sbc.
            storage.SetValue(BlockStorage.StateKey, state);
        }

        /// <summary>Keeps a backup of the logic systems the library has no equal of, as this construct's automation is lost.</summary>
        /// <remarks>
        /// Leaves out a logic system whose linked blueprint in <paramref name="shelf" /> has its change date, and keeps no backup
        /// where that is every one.
        /// </remarks>
        public void BackUp(Backups backups, DateTime saved, LibraryShelf shelf)
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
                shelf.Link(systems[i]);
                var kept = systems[i].HasOrigin ? shelf.Find(systems[i].Origin) : null;
                if (kept == null || kept.Changed != systems[i].Changed)
                    unkept.Add(systems[i]);
            }

            if (unkept.Count == 0)
                return;

            backups.Take(_world.Name, saved, unkept);
        }

        /// <summary>
        /// Runs a sequence by hand, or stops it, named the way another machine names it: the system by
        /// identity, and the one of it and the sequence by number.
        /// </summary>
        /// <remarks>Called on the server for a client running something from its copy. Nothing happens where what it names is not here.</remarks>
        public void Order(Guid system, int instance, int sequence, bool stops)
        {
            AutomationSystem named = _program.FindSystem(system);
            SystemInstance one = named?.FindInstance(instance);
            Sequence which = named?.FindSequence(sequence);
            if (one == null || which == null)
            {
                GameLog.Say($"{Logged(_block)} has no {(named == null ? "such system" : which == null ? "such sequence" : "such instance")} for an order to {(stops ? "stop" : "run")}");
                return;
            }

            if (!stops)
            {
                _runner.Call(which, one, true);
                return;
            }

            var state = _runner.Get(which, one);
            if (state != null)
                state.Interrupt(StopCause.Hand, 0);
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

            program = ProgramWriter.Save(_program, _world.Values);
            state = StateText.Save(_program, _runner);
        }

        /// <summary>Takes up <paramref name="state" />, the switches and runs the block holds now, over what this copy last knew.</summary>
        /// <remarks>For a copy lent again unchanged: its program is the block's, and only what is switched on may have moved since.</remarks>
        public void Restate(string state)
        {
            StateText.Load(state, _program, _runner);
            _program.Rewire();
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
            Destroyed = true;
            if (HeroRemoved != null)
                HeroRemoved(this);
        }

        void Closed()
        {
            if (HeroRemoved != null)
                HeroRemoved(this);
        }

        /// <summary>
        /// Moves into another programmable block on the construct where the hero has gone, answering whether it moved.
        /// </summary>
        /// <remarks>
        /// Asked when the hero is removed or closed and as the world saves, never on a tick. A grid splitting in the game brings
        /// the hero back under its own id as a new object.
        /// </remarks>
        bool Seek()
        {
            if (Lost)
                return false;

            GridEntity found = _world.FindEntity(_block.EntityId);
            if (found != null)
            {
                if (found.Block == _block)
                {
                    Destroyed = false;
                    return false;
                }

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

            GameLog.Say($"{Logged(_block)} is gone, and its construct has no programmable block left to keep the automation");
            Lost = true;
            Leave(_block);
            return false;
        }

        /// <summary>Makes <paramref name="block" /> the hero: the automation is kept in it from here on, and its buttons name it.</summary>
        void Move(IMyTerminalBlock block)
        {
            GameLog.Say($"{Logged(_block)} is gone; {Logged(block)} keeps the automation now");

            if (block.EntityId != _block.EntityId)
                Leave(_block);

            _block = block;
            _memory.Keeper = block.EntityId;
            _world.Anchor(block);
            _holders.Regather(block);
            Destroyed = false;

            // The construct's buttons still name the block that is gone, so the next pass mends them as it does after a load.
            _buttons.Rehomed(block);
        }

        /// <summary>Clears the automation out of <paramref name="block" />, the keeper it has left, where the block is still in the world.</summary>
        static void Leave(IMyTerminalBlock block)
        {
            if (!block.MarkedForClose && !block.Closed)
                BlockStorage.Clear(block);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _program.Changed -= Edited;
            _holders.Release();

            _world.AnchorRemoved -= Removed;
            _world.AnchorClosed -= Closed;
            _hands.Release();
            _world.Renamed -= Renamed;
            _runner.Retire();
            _world.Dispose();
        }
    }
}
