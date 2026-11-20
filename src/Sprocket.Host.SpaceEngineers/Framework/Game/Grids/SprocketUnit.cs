using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Players;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using System.Collections.Generic;
using System;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Grids
{
    /// <summary>
    /// One Sprocket, on one construct: its world, its program, the runtime that runs it, and the editor.
    /// </summary>
    /// <remarks>The session ticks it whether or not a screen is open on it.</remarks>
    public sealed partial class SprocketUnit : IDisposable
    {
        IMyTerminalBlock _block;

        Backups _backups;

        LibraryShelf _shelf;


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
        readonly Rota _rota;
        readonly Shift _stirred;
        readonly Ownership _hands;
        readonly SlotKeys _keys;
        readonly WiredButtons _buttons;
        readonly UnreachableSequences _reach;

        readonly Texts _texts;
        readonly IBinary _binary;
        readonly ConstructMemory _memory;
        readonly string _author;
        bool _developerMode;

        SprocketEditor _editor;

        /// <summary>The Sprocket a block carries, reading its program out of the block and what it is doing out of <paramref name="memory" />.</summary>
        /// <remarks><paramref name="rota" /> and <paramref name="kinds" /> are the world's, shared by every Sprocket in it.</remarks>
        public SprocketUnit(IMyTerminalBlock block, Settings settings, Rota rota, BlockKinds kinds, Texts texts, IBinary binary,
            ConstructMemory memory, Backups backups = null)
            : this(block, settings, rota, kinds, texts, binary, false, BlockStorage.Stored(block, BlockStorage.ProgramKey),
                BlockStorage.Stored(block, BlockStorage.StateKey), memory)
        {
            _backups = backups;
        }

        /// <summary>
        /// A copy of the Sprocket another machine stores on <paramref name="block" />, made from what that
        /// machine wrote out, to be edited here, its blocks of the world's <paramref name="kinds" />: its program, what is
        /// switched off, and what its construct's memory holds, packed by <paramref name="binary" />.
        /// </summary>
        /// <remarks>On a rota of its own that nothing turns, so none of its sequences acts.</remarks>
        public static SprocketUnit Copy(IMyTerminalBlock block, Settings settings, BlockKinds kinds, Texts texts, IBinary binary,
            string program, string switches, string memory)
        {
            return new SprocketUnit(block, settings, new Rota(), kinds, texts, binary, true, program, switches,
                Unpacked(binary, memory, block.EntityId));
        }

        /// <summary>The construct's memory <paramref name="packed" /> holds, or a fresh one filed under <paramref name="storage" /> where it holds none.</summary>
        static ConstructMemory Unpacked(IBinary binary, string packed, long storage)
        {
            if (string.IsNullOrEmpty(packed))
                return new ConstructMemory { Storage = storage };

            try
            {
                return binary.Unpack<ConstructMemory>(packed);
            }
            catch (Exception exception)
            {
                GameLog.Say($"block {storage} came with a memory that would not read, so its copy starts from none: {exception.Message}");
                return new ConstructMemory { Storage = storage };
            }
        }

        /// <summary>
        /// A Sprocket on <paramref name="block" />, a programmable block that parted from the construct of
        /// <paramref name="from" />, holding a copy of its program and switches and remembering what <paramref name="memory" />
        /// holds.
        /// </summary>
        /// <remarks>The copy is made in memory; nothing is written into <paramref name="block" /> until the program changes.</remarks>
        public static SprocketUnit PartedFrom(IMyTerminalBlock block, SprocketUnit from, Settings settings, Rota rota, ConstructMemory memory)
        {
            var parted = new SprocketUnit(block, settings, rota, from._kinds, from._texts, from._binary, false, null, null, memory, from);
            parted._backups = from._backups;
            return parted;
        }

        /// <summary>
        /// This Sprocket built again from <paramref name="program" /> and <paramref name="switches" />,
        /// carrying on with whatever it was running.
        /// </summary>
        /// <remarks>Called on the server for a client's edit. The caller puts the new one in this one's place and disposes of this.</remarks>
        public SprocketUnit Rebuilt(string program, string switches, Settings settings, Rota rota)
        {
            var rebuilt = new SprocketUnit(_block, settings, rota, _kinds, _texts, _binary, false, program, switches, _memory);
            rebuilt._backups = _backups;
            rebuilt._shelf = _shelf;
            rebuilt._viewer = _viewer;
            rebuilt.Showing = Showing;
            if (_editor != null)
                rebuilt.Editor.Habits = _editor.Habits;

            // What this one had wired goes with it: a Sprocket that believed it had just been loaded would write no slot.
            rebuilt._buttons.Carry(_buttons);
            return rebuilt;
        }

        SprocketUnit(IMyTerminalBlock block, Settings settings, Rota rota, BlockKinds kinds, Texts texts, IBinary binary, bool copy,
            string program, string state, ConstructMemory memory, SprocketUnit copying = null)
        {
            _block = block;
            _binary = binary;
            _memory = memory;
            _program.Memory = memory;
            _copy = copy;
            _kinds = kinds;
            _world = new GridWorld(block, kinds);
            _runner = new AutomationRunner(_program, _world, rota);
            _runner.FastestUpdate = settings?.FastestUpdate ?? 1;
            Logged(settings != null && settings.DebugLog);
            _keys = new SlotKeys(_program, _runner);
            _buttons = new WiredButtons(_world, _program, _runner, _keys, block);
            _reach = new UnreachableSequences(_world, _program, _runner, _buttons);

            _texts = texts;
            _developerMode = settings != null && settings.DeveloperMode;

            _author = MyAPIGateway.Multiplayer == null ? null : Player.Plain(MyAPIGateway.Multiplayer.MyName);

            if (copying != null)
                ProgramCopy.Into(copying._program, copying._world.Values, _program, _world.Values);
            else
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
            _runner.Live = Runs;
            _world.AnchorRemoved += Removed;
            _world.AnchorClosed += Rehoming;
            _world.Renamed += Renamed;

            // Brought up to date on the tick after anything changes, and never on a tick where nothing did.
            _rota = rota;
            _stirred = new Shift(rota.Join(), Pass);
            _world.Stirred += Stir;
            _program.Changed += Stir;
            _program.Reshaped += Stir;
            Stir();
        }

        /// <summary>Has this brought up to date with its construct and program first thing on the next tick.</summary>
        /// <remarks>Asked by every change to either, often in the middle of it; however many come in a tick, the next tick does it once.</remarks>
        public void Stir()
        {
            _rota.Next(_stirred);
        }

        /// <summary>Brings this up to date with its construct and program on <paramref name="tick" />, before anything else due on it.</summary>
        void Pass(int tick)
        {
            Bring();
            _runner.Prepare();

            // The hero's grid still loading says nothing as it arrives, so it is looked for again on each tick until it is read.
            if (_hands.Due && !_world.Holds(_block.EntityId))
                Stir();
        }

        /// <summary>Brings the toolbars, the holders and whether the automation runs up to date with the construct as it stands.</summary>
        void Bring()
        {
            // A copy leaves the toolbars to the machine that stores the Sprocket.
            if (!_copy)
                Touch();

            _holders.Hold(_block);
            _runner.Live = Runs;
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
            _runner.Live = Runs;
        }

        /// <summary>Whether the automation runs: a programmable block on the construct works, and it is not paused.</summary>
        bool Runs => _holders.Working && !_program.Paused;

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
                _editor.Renamed();
        }

        /// <summary>The programmable block the automation is stored in: the construct's hero.</summary>
        public IMyTerminalBlock Block => _block;

        /// <summary>What the construct's automation is doing, filed under the block that stores it, which the world's save writes.</summary>
        public ConstructMemory Memory => _memory;

        /// <summary>Starts its clock at <paramref name="now" />, Sprocket's tick as it is made, so anything before its first tick is told the time.</summary>
        public void Begin(int now)
        {
            _runner.Begin(now);
        }

        /// <summary>Whether the construct has no programmable block left to store the automation in, which ends it.</summary>
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

        /// <summary>The player's library, which the editor is given; null on a machine that stores none.</summary>
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
            Logged(settings.DebugLog);
            _developerMode = settings.DeveloperMode;
            if (_editor == null)
                return;

            _editor.DeveloperMode = _developerMode;
        }

        /// <summary>Gives the runner a log of what its runs do where <paramref name="on" />, keeping the one it has, and takes it away otherwise.</summary>
        void Logged(bool on)
        {
            if (!on)
                _runner.Log = null;
            else if (_runner.Log == null)
                _runner.Log = new RunLog(GameLog.Say, () => Logged(_block));
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
                _editor.Viewer = _viewer;
                _editor.Administers = _administers;
                _editor.WorldSettings = _worldSettings;
                _editor.ChangeSettings = _changeSettings;

                // A copy's buttons are written where the Sprocket is stored, which is asked by whoever lent it.
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

        /// <summary>The program this Sprocket runs and stores.</summary>
        public Automation Program => _program;

        /// <summary>
        /// Takes in a change made to <see cref="Program" /> in place, as a construct parting or joining makes: the program is
        /// checked and rewired, written into the hero once the change is done, and any screen on it is made anew.
        /// </summary>
        /// <remarks>A screen open on this is closed by whoever made the change, as for a Sprocket built again.</remarks>
        public void Reworked()
        {
            if (_editor != null)
            {
                var habits = _editor.Habits;
                _editor = null;
                Editor.Habits = habits;
            }

            _program.Edited();
            _program.Rewire();
        }

        /// <summary>Makes <see cref="Program" /> a copy of <paramref name="program" />, whose values are this one's, in place.</summary>
        /// <remarks>
        /// Every run is let go of, and made again on the next tick from the construct's memory. The caller then says it
        /// changed, with <see cref="Reworked" />.
        /// </remarks>
        public void Overwrite(Automation program)
        {
            _program.Discard();
            ProgramCopy.Into(program, _world.Values, _program, _world.Values);
            _runner.Forget();
        }

        /// <summary>Whether anything has asked for the editor yet, and so made one.</summary>
        public bool HasEditor => _editor != null;

        /// <summary>The construct it runs on.</summary>
        public GridWorld World => _world;

        /// <summary>What runs its program.</summary>
        public AutomationRunner Runner => _runner;


        /// <summary>A mod raised the moment <paramref name="key" /> on <paramref name="block" />, one of this construct's.</summary>
        public void Raised(IMyTerminalBlock block, MemberKey key)
        {
            _runner.Raised(block.EntityId, key);
        }

        /// <summary>A block fired one of the actions Sprocket put in it, carrying <paramref name="key" />.</summary>
        public void Fire(string key)
        {
            _keys.Fire(key);
        }

        /// <summary>Brings a copy up to the game's frame <paramref name="frame" />, its rota being one nothing runs.</summary>
        /// <remarks>Every tick, for a copy only: one exists while somebody has its screen open.</remarks>
        public void Tick(int frame)
        {
            Bring();
            _runner.Tick(frame);
        }

        /// <summary>Brings an open screen up to the frame <paramref name="frame" />, after the rota has run its sequences for it.</summary>
        /// <remarks>
        /// After the runs, so what a run changed on a frame is on the screen drawn for that frame. Asked only while the screen
        /// is open, and stirs this for the next tick, since what is done on an open screen, such as a resume, changes whether
        /// the automation runs without saying so.
        /// </remarks>
        public void Show(int frame)
        {
            if (!Showing || _editor == null)
                return;

            _editor.Tick(frame);
            Stir();
        }

        /// <summary>
        /// Whether <paramref name="player" /> may open the automation through the block <paramref name="opened" /> on the
        /// construct: they may use the block, and they are one of the construct's big owners or one of them owns it; or they
        /// are an admin using every terminal.
        /// </summary>
        /// <remarks>Where nobody owns any of the construct, anybody may.</remarks>
        public bool MayOpen(long opened, long player)
        {
            IMyTerminalBlock block = _world.Find(opened);
            if (block == null)
                return false;

            return Access.Shared(block, player) && (_world.IsBigOwner(player) || _world.IsMajor(block)) || Access.UsesEveryTerminal(player);
        }

        /// <summary>Brings the toolbars into line with the program, and reads them where something asked.</summary>
        void Touch()
        {
            // Looked at again on each pass, which comes every tick only while the hero is not yet on the construct read, as
            // its grid loads; the game's own events say when it has gone.
            if (_hands.Due && _world.Holds(_block.EntityId))
                _hands.Owned(_world.FindMajor(_block.EntityId));

            _buttons.Reconcile();
            _reach.Look();
        }

        /// <summary>Takes over, on its next pass, its program's buttons that still name <paramref name="from" />, the block its automation came from.</summary>
        public void TakeOver(long from)
        {
            _buttons.TakeOver(from);
            Stir();
        }

        /// <summary>Writes every slot the program wants on <paramref name="panel" /> on the next pass, whatever the slot holds by then.</summary>
        public void Wire(long panel)
        {
            _buttons.Wire(panel);
            Stir();
        }

        /// <summary>Lets go of every panel <see cref="Wire" /> asked for and no pass has written yet, as a pick is backed out of.</summary>
        public void Unwire()
        {
            _buttons.Unwire();
            Stir();
        }

        /// <summary>Brings the toolbars into line with the program on the next tick, as though its wiring had changed.</summary>
        public void Revisit()
        {
            _buttons.Revisit();
            Stir();
        }

        /// <summary>Whether this Sprocket's screen is open in front of somebody.</summary>
        /// <remarks>Set by the screen as it opens and closes. The editor is ticked only while it holds; the program runs either way.</remarks>
        public bool Showing
        {
            get { return _runner.Looking; }
            set
            {
                if (_runner.Looking == value)
                    return;

                _runner.Looking = value;
                if (ShowingChanged != null)
                    ShowingChanged(this);
            }
        }

        /// <summary>Raised when the screen opens or closes on this Sprocket.</summary>
        public event Action<SprocketUnit> ShowingChanged;

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

            // An installation or a sequence read without a name is given the one it was shown as, written at the next save.
            if (GivenNames.Give(_program, _texts))
                _program.Edited();

            // Read after the program, because it names what the program declared. Only a copy being edited takes the runs
            // from it, for show.
            if (!string.IsNullOrEmpty(state))
            {
                var wrong = BlockSwitches.Load(state, _program, _binary);
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
            // The machine that stores the Sprocket writes it; a copy is only somebody's view of it.
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
            var state = BlockSwitches.Save(_program, _binary);
            storage.SetValue(BlockStorage.ProgramKey, program);
            // Under a key of its own, apart from the program somebody shares; the key is claimed in EntityComponents.sbc.
            storage.SetValue(BlockStorage.StateKey, state);
        }

        /// <summary>Stores a backup of the logic systems the library has no equal of, as this construct's automation is lost.</summary>
        /// <remarks>
        /// Leaves out a logic system whose linked blueprint in <paramref name="shelf" /> has its change date, and stores no backup
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

            backups.Take(_world.Name, saved, unkept, _world.Values);
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
        /// Gives the state named <paramref name="state" /> of installation <paramref name="instance" /> of
        /// <paramref name="system" /> the value <paramref name="value" />, as a player did by hand on their copy; only while
        /// that installation or its logic system is switched off, and only a value the state can hold.
        /// </summary>
        /// <remarks>Called on the server. Nothing watching the state reacts.</remarks>
        public void Hold(Guid system, int instance, string state, Literal value)
        {
            var named = _program.FindSystem(system);
            var one = named?.FindInstance(instance);
            var held = named?.FindState(state);
            if (one == null || held == null)
            {
                GameLog.Say($"{Logged(_block)} has no {(named == null ? "such system" : one == null ? "such instance" : "such state")} for a state changed by hand");
                return;
            }

            if (named.Enabled && one.Enabled)
            {
                GameLog.Say($"{Logged(_block)} refused a state changed by hand: {named.Name} #{instance} is switched on");
                return;
            }

            if (!StateValues.Fits(named, held, value))
            {
                GameLog.Say($"{Logged(_block)} refused a state changed by hand: {held.Name} cannot hold {value.Said}");
                return;
            }

            _runner.States.Write(named, one, held, value);
        }

        /// <summary>Starts paused automation again as it was.</summary>
        /// <remarks>Called on the server for a client that resumed it on its copy.</remarks>
        public void Resume()
        {
            _program.PausedFrom.Clear();
            _runner.Live = Runs;
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
            state = BlockSwitches.Save(_program, _binary);
            Wrote(program, state);
        }

        /// <summary>The program as just written, <paramref name="program" />, and its switches, <paramref name="state" />, for a local build to check.</summary>
        /// <remarks>Has no body in anything packed for the game, where the call compiles to nothing.</remarks>
        partial void Wrote(string program, string state);

        /// <summary>The construct's memory, packed: what a copy on another machine shows of its runs, triggers, owners and versions.</summary>
        public string Remembered()
        {
            return _binary.Pack(_memory);
        }

        /// <summary>The codec this Sprocket packs what it stores with.</summary>
        public IBinary Binary => _binary;

        /// <summary>
        /// Takes up <paramref name="switches" />, what the block holds switched off now, and <paramref name="memory" />, what its
        /// construct remembers now, over what this copy last knew.
        /// </summary>
        /// <remarks>For a copy lent again unchanged: its program is the block's, and only what is switched on or off and running may have moved since.</remarks>
        public void Restate(string switches, string memory)
        {
            BlockSwitches.Reload(switches, _program, _binary);

            var fresh = Unpacked(_binary, memory, _block.EntityId);
            _runner.Forget();
            _memory.Runs = fresh.Runs;
            _memory.Watches = fresh.Watches;
            _memory.Conflicts = fresh.Conflicts;
            _memory.Owners = fresh.Owners;
            _memory.PausedFrom = fresh.PausedFrom;
            _memory.States = fresh.States;
            _program.Rewire();
        }

        /// <summary>Has the editor build its words again, after the language changes; an editor not yet made builds them when it is.</summary>
        public void Reword()
        {
            if (_editor != null)
                _editor.Reword();
        }

        /// <summary>Stores the automation in another programmable block on the construct, now that the hero is gone, answering whether it moved.</summary>
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

        /// <summary>Has the roster find another block to store the automation, as the hero is closed.</summary>
        void Rehoming()
        {
            if (HeroRemoved != null)
                HeroRemoved(this);
        }

        /// <summary>
        /// Moves into another programmable block on the construct, whoever owns it, where the hero has gone, answering whether
        /// it moved.
        /// </summary>
        /// <remarks>
        /// Asked when the hero is removed or closed, and as the world saves, never on a tick. A grid splitting in the game
        /// brings the hero back under its own id as a new object.
        /// </remarks>
        bool Seek()
        {
            if (Lost)
                return false;

            IMyTerminalBlock storage = _world.FindStorage(_block.EntityId);
            if (storage != null)
            {
                if (storage == _block)
                {
                    Destroyed = false;
                    return false;
                }

                Move(storage);
                return true;
            }

            GameLog.Say($"{Logged(_block)} is gone, and its construct has no programmable block left to keep the automation");
            Lost = true;
            Leave(_block);
            return false;
        }

        /// <summary>Makes <paramref name="block" /> the hero: the automation is stored in it from here on, and its buttons name it.</summary>
        void Move(IMyTerminalBlock block)
        {
            GameLog.Say($"{Logged(_block)} is gone; {Logged(block)} keeps the automation now");

            if (block.EntityId != _block.EntityId)
                Leave(_block);

            _block = block;
            _memory.Storage = block.EntityId;
            _world.Anchor(block);
            _holders.Regather(block);
            Destroyed = false;

            // The construct's buttons still name the block that is gone, so the next pass mends them as it does after a load.
            _buttons.Rehomed(block);
            Stir();
        }

        /// <summary>Clears the automation out of <paramref name="block" />, the block it was stored in, where the block is still in the world.</summary>
        static void Leave(IMyTerminalBlock block)
        {
            if (!block.MarkedForClose && !block.Closed)
                BlockStorage.Clear(block);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _program.Changed -= Edited;
            _program.Changed -= Stir;
            _program.Reshaped -= Stir;
            _world.Stirred -= Stir;
            _rota.Leave(_stirred);
            _holders.Release();

            _world.AnchorRemoved -= Removed;
            _world.AnchorClosed -= Rehoming;
            _hands.Release();
            _world.Renamed -= Renamed;
            _runner.Retire();
            _world.Dispose();
        }
    }
}
