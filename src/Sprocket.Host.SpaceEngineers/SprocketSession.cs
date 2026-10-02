using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Network;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Terminal;

namespace Sprocket.Host.SpaceEngineers
{
    /// <summary>
    /// Owns the Sprockets this machine runs, and the copy of one it is editing, and gives them a clock.
    /// </summary>
    /// <remarks>
    /// A unit runs whether or not anybody is looking at it, which is the whole point of automation, so the
    /// ticking lives here and not in the screen. The screen is a way to look at a unit and nothing more.
    /// <para>
    /// Sprockets run on the server alone. A client edits a copy the server lends it - see
    /// <see cref="Handover" /> - and the copy runs nothing.
    /// </para>
    /// </remarks>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SprocketSession : MySessionComponentBase
    {
        const int Keeping = 30;

        const int Reclaiming = 300;

        const string Library = "Library.sprocket";

        static SprocketSession _current;

        readonly Dictionary<long, SprocketUnit> _units = new Dictionary<long, SprocketUnit>();

        readonly Dictionary<long, long> _moved = new Dictionary<long, long>();

        readonly List<KeyValuePair<long, SprocketUnit>> _saving = new List<KeyValuePair<long, SprocketUnit>>();

        readonly Backups _backups = new Backups();

        Carrier _carrier;

        bool _carries;

        readonly Shelf _shelf = new Shelf();

        bool _shelves;

        readonly Rota _rota = new Rota();

        SprocketScreen _screen;
        long _open;

        SprocketUnit _shown;

        NetworkChannel _channel;

        Handover _handover;

        SprocketUnit _copy;

        long _copyOf;

        long _asked;

        readonly Dictionary<long, Habits> _habits = new Dictionary<long, Habits>();

        Interval _keeping = new Interval(Keeping);

        Interval _reclaiming = new Interval(Reclaiming);

        readonly List<IMyPlayer> _players = new List<IMyPlayer>();

        readonly List<IMyCubeGrid> _group = new List<IMyCubeGrid>();

        Settings _settings;

        readonly Texts _texts = new Texts();

        GameTexts _gameTexts;

        /// <summary>
        /// What the player asked for, read the first time anything wants to know.
        /// </summary>
        /// <remarks>
        /// Not in <see cref="LoadData" />: mod storage is not reliably there that early, and a read
        /// that quietly answered every default would be indistinguishable from a file saying nothing.
        /// By the time a block adopts itself the world is up.
        /// </remarks>
        Settings Asked
        {
            get { return _settings ?? (_settings = Settings.Read()); }
        }

        /// <summary>What the mod says, in the player's language, or null before the session has loaded.</summary>
        /// <remarks>Static for the reason <see cref="_current" /> is: a block's own component reaches the session only through statics, and it registers the terminal button and toolbar action in these words.</remarks>
        public static Texts Texts => _current == null ? null : _current._texts;

        /// <summary>Whether the player asked for the developer tools, read once a world has loaded.</summary>
        /// <remarks>Static for the reason <see cref="Texts" /> is: a block's own component reads it as it makes the terminal controls.</remarks>
        public static bool DeveloperMode => _current != null && _current.Asked.DeveloperMode;

        /// <summary>
        /// The unit a block carries, or null. Never creates one.
        /// </summary>
        /// <remarks>
        /// What a fired toolbar slot resolves through: the slot names the block, and the block is
        /// the only thing that says which program it belongs to.
        /// </remarks>
        public static SprocketUnit UnitOf(IMyTerminalBlock block)
        {
            SprocketUnit unit;
            return _current != null && block != null
                && _current._units.TryGetValue(block.EntityId, out unit) ? unit : null;
        }

        /// <summary>
        /// Whether this machine is the one running programs.
        /// </summary>
        /// <remarks>
        /// For anything that has to tell "there is no Sprocket here" from "this machine is not the one
        /// keeping it": a display drawn on a client has no program to read, and saying the construct has
        /// no automation would be a lie in the one place a person would believe it.
        /// </remarks>
        internal static bool Runs => IsServer;

        /// <summary>
        /// Every Sprocket mechanically connected to this block, in no particular order.
        /// </summary>
        /// <remarks>
        /// The same reach a Sprocket itself has - <c>GridGroups</c> on the mechanical group, which is
        /// what a player means by "this ship" - so a display on a rotor head lists the Sprocket in the
        /// hull. Filled into a list the caller owns, because this is asked on a cadence forever.
        /// </remarks>
        internal static void OnConstruct(IMyCubeBlock block, List<SprocketUnit> into)
        {
            into.Clear();

            IMyCubeGrid grid = block == null ? null : block.CubeGrid;
            if (_current == null || grid == null || MyAPIGateway.GridGroups == null)
                return;

            _current._group.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, _current._group);

            foreach (SprocketUnit unit in _current._units.Values)
            {
                IMyTerminalBlock owner = unit.Block;
                if (owner == null || owner.CubeGrid == null)
                    continue;

                if (_current._group.Contains(owner.CubeGrid))
                    into.Add(unit);
            }
        }

        /// <summary>Puts the automation button where a programmable block's controls are shown.</summary>
        /// <remarks>
        /// Here rather than in <see cref="LoadData" />, where <c>MyAPIGateway.TerminalControls</c> does not
        /// exist yet. Subscribing adds nothing to the block type, unlike adding a control, so it is safe
        /// before any programmable block has come alive. See <c>SprocketControls</c>.
        /// </remarks>
        public override void BeforeStart()
        {
            MyAPIGateway.TerminalControls.CustomControlGetter += SprocketControls.Arrange;

            if (IsServer)
                FindBackups();

            if (MyAPIGateway.Utilities != null && !MyAPIGateway.Utilities.IsDedicated)
                FindShelf();
        }

        /// <summary>Reads the player's library out of the global store, once, as the world starts, and keeps it written as it changes.</summary>
        /// <remarks>A library that will not read is logged and never written over: what is put in it this session is not kept.</remarks>
        void FindShelf()
        {
            var files = MyAPIGateway.Utilities;
            string trouble;
            try
            {
                if (!files.FileExistsInGlobalStorage(Library))
                {
                    trouble = "";
                }
                else
                {
                    using (TextReader reader = files.ReadFileInGlobalStorage(Library))
                        trouble = _shelf.Read(reader.ReadToEnd());
                }
            }
            catch (Exception failed)
            {
                trouble = "the library will not open - " + failed.Message;
            }

            if (trouble.Length > 0)
            {
                Told.Say(trouble + "; the library is not kept this session");
                return;
            }

            _shelves = true;
            _shelf.Changed += Shelve;
            Told.Say("read " + _shelf.All.Count + " blueprints from the library");
        }

        /// <summary>Writes the player's library into the global store, as it is now.</summary>
        /// <remarks>Whenever a blueprint is put in or taken out, which a person does by hand.</remarks>
        void Shelve()
        {
            if (!_shelves || !_shelf.Intact)
                return;

            try
            {
                using (TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(Library))
                    writer.Write(_shelf.Write());
            }
            catch (Exception failed)
            {
                Told.Say("the library will not write - " + failed.Message);
            }
        }

        /// <summary>Reads the world's backups out of its carrier, once, as the world starts.</summary>
        /// <remarks>
        /// Backups that will not read, or a world with more than one carrier, are logged and never written over: the
        /// backups go on being taken this session, and are not kept.
        /// </remarks>
        void FindBackups()
        {
            Carrier found;
            var trouble = Carrier.Find(out found);
            if (trouble.Length == 0 && found != null)
                trouble = _backups.Read(found.Text);

            if (trouble.Length > 0)
            {
                Told.Say(trouble + "; the backups are not kept this session");
                return;
            }

            _carrier = found;
            _carries = true;
            Told.Say("read " + _backups.All.Count + " backups");
        }

        public override void LoadData()
        {
            // Static because Space Engineers constructs a session component and hands it to nothing: a terminal
            // control, a screen host and a block's own component reach it only through statics. It holds the one
            // instance, which is an ordinary object.
            _current = this;

            Speak();

            _handover = new Handover(Found, Given, Refused, Changed, Stored);
            _channel = Handover.Carry(NetworkChannel.Create(GetType())).Build();
            _handover.Listen(_channel);

            Told.Say(IsServer
                ? "this machine runs the Sprockets"
                : "this machine is a client; the Sprockets run on the server");
        }

        /// <summary>Loads the mod's texts in the player's language, and listens for the language to change.</summary>
        /// <remarks>A dedicated server has nobody to read them, and says them as they were written.</remarks>
        void Speak()
        {
            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated || MyAPIGateway.Gui == null
                || MyAPIGateway.Session == null || MyAPIGateway.Session.Config == null)
                return;

            _gameTexts = new GameTexts(Path.Combine(ModContext.ModPath, "Data", "Localization"));
            Relanguage();
            MyAPIGateway.Gui.GuiControlRemoved += Closed;
        }

        /// <summary>Loads the texts again where the language has changed.</summary>
        /// <remarks>
        /// Asked whenever a screen closes, because the options screen closing is when a new language takes
        /// and the game announces nothing more particular. One comparison each time.
        /// </remarks>
        void Closed(object screen)
        {
            if (_gameTexts != null && MyAPIGateway.Session != null && MyAPIGateway.Session.Config != null
                && MyAPIGateway.Session.Config.Language != _gameTexts.Language)
                Relanguage();
        }

        /// <summary>Reads the texts in the player's language, and has every editor build its words again.</summary>
        void Relanguage()
        {
            _gameTexts.Load(MyAPIGateway.Session.Config.Language);
            _texts.Book.Load(_gameTexts);

            foreach (KeyValuePair<long, SprocketUnit> unit in _units)
                unit.Value.Reword();

            if (_copy != null)
                _copy.Reword();
        }

        /// <summary>The Sprocket of the construct a programmable block is on, by entity id, made where it has none; or null.</summary>
        /// <remarks>
        /// On the server, for a client's claim, so only a programmable block is ever taken on. An id the automation has
        /// moved on from is followed to the block it moved to, so a client that was lent it goes on reaching it.
        /// </remarks>
        SprocketUnit Found(long id)
        {
            var block = MyAPIGateway.Entities == null
                ? null
                : MyAPIGateway.Entities.GetEntityById(Key(id)) as IMyProgrammableBlock;

            return block == null ? null : Adopt(block);
        }

        /// <summary>The id the automation once kept under <paramref name="id" /> is kept under now.</summary>
        /// <remarks>Leases and a client's copy keep the id they were given, so what they send is followed through every move since.</remarks>
        long Key(long id)
        {
            long next;
            while (_moved.TryGetValue(id, out next))
                id = next;

            return id;
        }

        /// <summary>
        /// The server lending this client a Sprocket: the editor opens on a copy of it.
        /// </summary>
        /// <remarks>
        /// Handed straight back where the block is not here, which is a client that has not been sent the
        /// grid yet, or one that has lost it since asking.
        /// </remarks>
        void Given(long id, string program, string state)
        {
            var block = MyAPIGateway.Entities == null
                ? null
                : MyAPIGateway.Entities.GetEntityById(id) as IMyTerminalBlock;

            if (block == null)
            {
                Told.Say("block " + id + " was lent but is not here, so it goes straight back");
                _handover.Hand(id);
                return;
            }

            if (_copy != null)
                _copy.Dispose();

            var copy = SprocketUnit.Copy(block, Asked, _texts, program, state);
            copy.Editor.Habits = HabitsFor(id);
            _copy = copy;
            _copyOf = id;
            _handover.Kept(id, copy);

            // Running something by hand runs it where it is kept. What is not built there yet - a
            // change still on its way - is simply not found, and nothing happens.
            copy.Editor.Elsewhere = delegate(Sequence sequence, SystemInstance instance, bool stops)
            {
                _handover.Keep(id, copy);
                _handover.Run(id, sequence, instance, stops);
            };

            // And a picked button is written there too, after the program that wants it.
            copy.Editor.Wire = delegate(long panel)
            {
                _handover.Keep(id, copy);
                _handover.Wire(id, panel);
            };

            Show(Opened(block), copy, delegate
            {
                // What changed since the last comparison goes before the Sprocket is handed back,
                // because a change arriving from somebody who no longer holds it is not taken.
                _handover.Keep(id, copy);
                _handover.Hand(id);
                copy.Dispose();
                if (_copy == copy)
                    _copy = null;
            });
        }

        /// <summary>
        /// A client's change arriving at the server: the Sprocket is built again from it, carrying on
        /// with what it was running, and written into the block.
        /// </summary>
        /// <remarks>
        /// Left as it was where the client's text will not read, which is logged: a program half read
        /// is one nothing must be saved over.
        /// </remarks>
        void Changed(long id, string program, string switches)
        {
            id = Key(id);
            SprocketUnit was;
            if (!_units.TryGetValue(id, out was))
            {
                Told.Say("a change arrived for block " + id + ", which carries no Sprocket here");
                return;
            }

            SprocketUnit rebuilt = was.Rebuilt(program, switches, Asked, _rota);
            if (!rebuilt.Program.Intact)
            {
                Told.Say("a change to " + was.Block.CustomName + " would not read, and was not taken");
                rebuilt.Dispose();
                return;
            }

            _units[id] = rebuilt;
            rebuilt.HeroRemoved += Removed;
            was.Dispose();
            rebuilt.Persist();
            File(id, rebuilt);
            Spread(rebuilt);
            Told.Say(was.Block.CustomName + " was built again from the change, and saved");
        }

        /// <summary>How this player has been working in the editor for a block, made the first time it is asked.</summary>
        Habits HabitsFor(long id)
        {
            Habits habits;
            if (!_habits.TryGetValue(id, out habits))
            {
                habits = new Habits();
                _habits.Add(id, habits);
            }

            return habits;
        }

        /// <summary>Tells every client what the server wrote into a Sprocket's block.</summary>
        /// <remarks>
        /// The program and its switches, and never what is running: a grid pasted from a client's copy
        /// starts as a fresh Sprocket rather than halfway through somebody else's run. Told only where
        /// it changed since the last telling, so the world save costs a comparison per Sprocket.
        /// </remarks>
        void Spread(SprocketUnit unit)
        {
            if (!IsServer || !unit.Program.Intact)
                return;

            string program;
            string state;
            unit.Written(out program, out state);
            _handover.Tell(unit.Block.EntityId, program, StateText.Save(unit.Program, null));
        }

        /// <summary>What the server wrote into a block, arriving at this client for its copy of the block.</summary>
        void Stored(long id, string program, string switches)
        {
            var block = MyAPIGateway.Entities == null
                ? null
                : MyAPIGateway.Entities.GetEntityById(id) as IMyTerminalBlock;

            if (block != null)
                SprocketUnit.Store(block, program, switches);
        }

        /// <summary>
        /// The server turning this client down, which it says rather than doing nothing, and where
        /// somebody else is editing it, opening it to look at.
        /// </summary>
        /// <remarks>
        /// What opens is the Sprocket as it stood when this client asked. The holder's edits reach the
        /// server, not the ones looking on.
        /// </remarks>
        void Refused(long id, string holder, string program, string state)
        {
            var block = MyAPIGateway.Entities == null
                ? null
                : MyAPIGateway.Entities.GetEntityById(id) as IMyTerminalBlock;

            if (holder.Length == 0 || program.Length == 0 || block == null)
            {
                Say(_texts.TerminalRefused);
                return;
            }

            Look(block, holder, program, state);
        }

        /// <summary>
        /// Opens the editor on a copy to look at while <paramref name="holder" /> edits the real one.
        /// </summary>
        /// <remarks>
        /// The editor is what says who is editing, at the head of every page, so there is no message
        /// beside it: this is pressed in the terminal, where nothing on the HUD can be seen.
        /// </remarks>
        void Look(IMyTerminalBlock block, string holder, string program, string state)
        {
            if (_copy != null)
                _copy.Dispose();

            long id = block.EntityId;
            var copy = SprocketUnit.Copy(block, Asked, _texts, program, state);
            copy.Editor.Habits = HabitsFor(id);
            copy.Editor.Holder = holder;
            _copy = copy;
            _copyOf = id;

            Show(Opened(block), copy, delegate
            {
                copy.Dispose();
                if (_copy == copy)
                    _copy = null;
            });
        }

        /// <summary>The programmable block this client asked to open, where it is still here; otherwise <paramref name="lent" />, the one the server lent.</summary>
        IMyTerminalBlock Opened(IMyTerminalBlock lent)
        {
            var asked = MyAPIGateway.Entities == null ? null : MyAPIGateway.Entities.GetEntityById(_asked) as IMyTerminalBlock;
            return asked ?? lent;
        }

        /// <summary>Says something to the player in a box over whatever screen is open.</summary>
        /// <remarks>
        /// Not the HUD's notifications: this answers a press in the terminal, and the HUD does not
        /// draw while the terminal is open.
        /// </remarks>
        void Say(string what)
        {
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.ShowMissionScreen(_texts.TerminalOpen, null, null, what);
        }

        /// <summary>
        /// Takes on a block that carries a program, so it starts running.
        /// </summary>
        /// <remarks>
        /// Static because the caller is a game logic component on the block, which has no way to reach a
        /// session component otherwise. Answers null off the main path rather than throwing: a block can
        /// come alive during a session teardown and that is not a thing to crash over.
        /// </remarks>
        public static SprocketUnit Adopt(IMyTerminalBlock block)
        {
            if (_current == null || block == null || !IsServer)
                return null;

            return _current.UnitFor(block);
        }

        /// <summary>
        /// Whether this machine is the one that runs things.
        /// </summary>
        /// <remarks>
        /// Sprocket's runtime lives on the server and nowhere else, which is also where the game raises a
        /// button press: <c>MyButtonPanel.ActivateButton</c> is a server event, so a client never hears one.
        /// A client running its own copy would be a second airlock nobody asked for, opening the same doors
        /// a moment apart.
        ///
        /// True in single player, where the client is the server.
        /// </remarks>
        static bool IsServer => MyAPIGateway.Multiplayer == null || MyAPIGateway.Multiplayer.IsServer;

        /// <summary>The Sprocket of the construct <paramref name="block" /> is on, made with it as the hero where the construct has none.</summary>
        /// <remarks>
        /// A construct has one Sprocket, so a programmable block holding a program of its own on a construct that already
        /// has one is not taken on; it is logged, and its program is left where it is.
        /// </remarks>
        SprocketUnit UnitFor(IMyTerminalBlock block)
        {
            SprocketUnit unit = UnitOn(block);
            if (unit != null)
            {
                if (unit.Block != block && SprocketUnit.HasProgram(block))
                {
                    Told.Say(block.CustomName + " holds a program on a construct whose automation " + unit.Block.CustomName
                        + " keeps; it was not taken on");
                }

                return unit;
            }

            unit = new SprocketUnit(block, Asked, _rota, _texts, _backups);
            unit.HeroRemoved += Removed;
            _units.Add(block.EntityId, unit);
            return unit;
        }

        /// <summary>A Sprocket's hero removed from its construct, answered once the game is back on its own thread.</summary>
        /// <remarks>
        /// Raised inside the grid's own event, while the grid is still changing; the game's invoke on the game thread runs the
        /// move after it has finished, so nothing of Sprocket's waits on a tick for it.
        /// </remarks>
        void Removed(SprocketUnit unit)
        {
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.InvokeOnGameThread(delegate { Rehome(unit); });
        }

        /// <summary>Moves a Sprocket into another programmable block on its construct, where it is still this session's.</summary>
        void Rehome(SprocketUnit unit)
        {
            long was = unit.Block.EntityId;
            SprocketUnit held;
            if (!_units.TryGetValue(was, out held) || held != unit)
                return;

            unit.Rehome();
            File(was, unit);
        }

        /// <summary>Files a Sprocket kept under <paramref name="was" /> under the block that holds it now, or lets it go where it is lost.</summary>
        /// <remarks>
        /// A moved Sprocket is told to every client, so their copies of the construct keep the program in the block that holds
        /// it, and the id it moved from is remembered for the leases and copies still holding it.
        /// </remarks>
        void File(long was, SprocketUnit unit)
        {
            if (unit.Lost)
            {
                // Taken now, from what it was running, because nothing holds the automation after this.
                unit.BackUp(_backups, DateTime.UtcNow, true);
                _units.Remove(was);
                if (_shown == unit && _screen != null)
                    _screen.CloseNow();

                unit.Dispose();
                return;
            }

            long now = unit.Block.EntityId;
            if (now == was)
                return;

            _units.Remove(was);
            _units[now] = unit;
            _moved[was] = now;
            Spread(unit);
        }

        /// <summary>The Sprocket of the construct <paramref name="block" /> is on, or null where it has none.</summary>
        /// <remarks>One lookup per Sprocket in the world, asked when a block is opened or comes alive with a program, never on a tick.</remarks>
        SprocketUnit UnitOn(IMyTerminalBlock block)
        {
            SprocketUnit unit;
            if (_units.TryGetValue(block.EntityId, out unit))
                return unit;

            foreach (KeyValuePair<long, SprocketUnit> each in _units)
            {
                if (each.Value.World.FindEntity(block.EntityId) != null)
                    return each.Value;
            }

            return null;
        }

        /// <summary>
        /// Opens the editor on the construct this block sits on.
        /// </summary>
        /// <remarks>
        /// The terminal is closed rather than left standing behind, because two screens describing the same
        /// ship is clutter and the one underneath cannot be reached anyway while this one has focus.
        /// <para>
        /// A client asks the server for it and opens when the server answers. The server's own player
        /// opens it at once, holding it the way a client would, so nobody else can edit it meanwhile.
        /// </para>
        /// </remarks>
        internal static void Open(IMyTerminalBlock block)
        {
            if (_current == null || block == null)
                return;

            IMyMultiplayer multiplayer = MyAPIGateway.Multiplayer;
            var name = multiplayer == null ? "" : multiplayer.MyName;
            if (!IsServer)
            {
                // The server answers with the block that keeps the automation; the editor is still opened from this one.
                _current._asked = block.EntityId;
                _current._handover.Ask(block.EntityId, name);
                return;
            }

            SprocketUnit unit = Adopt(block);
            if (unit == null)
                return;

            var id = multiplayer == null ? 0UL : multiplayer.MyId;
            long lent = unit.Block.EntityId;
            Handover handover = _current._handover;

            string holder;
            if (!handover.Lend(lent, id, name, out holder))
            {
                string program;
                string state;
                unit.Written(out program, out state);
                _current.Look(block, holder, program, state);
                return;
            }

            _current.Show(block, unit, delegate
            {
                handover.Take(lent, id);
                _current.Spread(unit);
            });
        }

        void Show(IMyTerminalBlock block, SprocketUnit unit, Action closed)
        {
            unit.Shelf = _shelf;
            if (_screen == null || _open != block.EntityId || _shown != unit)
            {
                if (_screen != null)
                    _screen.CloseNow();

                _screen = new SprocketScreen(unit, block, closed);
                _open = block.EntityId;
                _shown = unit;
            }

            if (MyAPIGateway.Gui != null)
                MyAPIGateway.Gui.CloseTerminalScreen();

            _screen.Open();
        }

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session == null)
                return;

            int frame = MyAPIGateway.Session.GameplayFrameCounter;

            // Ticked for its editor, which is all a copy has that moves, and compared with what the
            // server was last sent on a cadence of its own. A copy lent to look at sends nothing.
            if (_copy != null)
            {
                _copy.Tick(frame);
                if (_keeping.Due(frame) && _copy.Editor.Holder == null)
                    _handover.Keep(_copyOf, _copy);
            }

            if (IsServer && _reclaiming.Due(frame))
                _handover.Reclaim(Connected);

            if (_units.Count == 0)
                return;

            foreach (KeyValuePair<long, SprocketUnit> unit in _units)
                unit.Value.Tick(frame);

            // After every Sprocket has ticked, so each one's bindings are current when the rota calls
            // its sequences.
            _rota.Run(frame);
        }

        /// <summary>Whether the player with <paramref name="id" /> is connected to this server.</summary>
        /// <remarks>
        /// The server's own player counts, which is a host holding a Sprocket from its own screen. The
        /// list is read afresh for each lease, every few seconds, and only while a lease stands.
        /// </remarks>
        bool Connected(ulong id)
        {
            if (MyAPIGateway.Multiplayer != null && id == MyAPIGateway.Multiplayer.MyId)
                return true;

            if (MyAPIGateway.Players == null)
                return true;

            _players.Clear();
            MyAPIGateway.Players.GetPlayers(_players);

            for (var i = 0; i < _players.Count; i++)
            {
                if (_players[i].SteamUserId == id)
                    return true;
            }

            return false;
        }



        /// <summary>
        /// Writes every program into its hero as the world saves, before the entities are written out.
        /// </summary>
        /// <remarks>
        /// The game asks for a session component's object builder before it writes the entities, and runs its
        /// <c>SaveData</c> after, so this is the last moment a block's storage can still reach this save. The screen also
        /// writes as it closes; this covers saving with the editor open, and costs a string per Sprocket.
        /// </remarks>
        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            // From a list of its own, because writing a Sprocket finds its hero first, and one that has moved is filed again.
            _saving.Clear();
            _saving.AddRange(_units);
            for (var i = 0; i < _saving.Count; i++)
            {
                SprocketUnit unit = _saving[i].Value;
                unit.Persist();
                File(_saving[i].Key, unit);
                if (unit.Lost)
                    continue;

                Spread(unit);
                unit.BackUp(_backups, DateTime.UtcNow, false);
            }

            _saving.Clear();
            Carry();
            return base.GetObjectBuilder();
        }

        /// <summary>Writes the backups into the world's carrier, making it where the world has none yet.</summary>
        /// <remarks>
        /// Once a save, from every backup, which costs what writing out the programs costs and nothing during play. A world
        /// with no backups is given no carrier; one that has a carrier has it written, emptied where every backup was forgotten.
        /// </remarks>
        void Carry()
        {
            if (!_carries || !IsServer || (_carrier == null && _backups.All.Count == 0))
                return;

            if (_carrier == null)
                _carrier = Carrier.Make();

            _carrier.Write(_backups.Write());
        }

        protected override void UnloadData()
        {
            if (MyAPIGateway.TerminalControls != null)
                MyAPIGateway.TerminalControls.CustomControlGetter -= SprocketControls.Arrange;

            if (_gameTexts != null && MyAPIGateway.Gui != null)
                MyAPIGateway.Gui.GuiControlRemoved -= Closed;

            _gameTexts = null;

            if (_screen != null)
                _screen.CloseNow();

            if (_copy != null)
                _copy.Dispose();

            foreach (KeyValuePair<long, SprocketUnit> unit in _units)
                unit.Value.Dispose();

            if (_channel != null)
                _channel.Dispose();

            _units.Clear();
            _screen = null;
            _shown = null;
            _copy = null;
            _channel = null;
            _handover = null;
            _settings = null;
            _current = null;
        }
    }
}
