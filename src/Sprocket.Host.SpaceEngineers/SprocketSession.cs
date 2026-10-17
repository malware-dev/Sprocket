using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Network;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Host.SpaceEngineers.Features.Gravity;
using Sprocket.Host.SpaceEngineers.Features.Doors;
using Lumel.Text;

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

        static SprocketSession _current;

        readonly Backups _backups = new Backups(new WorldFiles());

        readonly CarrierFiles _carried = new CarrierFiles();

        readonly Memories _memories = new Memories(new GameBinary());

        readonly List<ConstructMemory> _remembered = new List<ConstructMemory>();

        readonly List<DoorMemory> _closing = new List<DoorMemory>();

        Carrier _carrier;

        bool _carries;

        readonly LibraryShelf _shelf = new LibraryShelf(new GlobalFiles(), GameLog.Say);

        readonly Preferences _preferences = new Preferences(new GlobalFiles(), GameLog.Say);

        readonly ShortfallMarks _marks = new ShortfallMarks();

        readonly Rota _rota = new Rota();

        Roster _roster;

        AutoClose _doors;

        SprocketScreen _screen;
        long _open;

        SprocketUnit _shown;

        NetworkChannel _channel;

        Handover _handover;

        SprocketUnit _copy;

        long _copyOf;

        /// <summary>The last copy this client edited, kept after it was handed back so its way back is there if it is lent again unchanged; or null.</summary>
        SprocketUnit _rested;

        long _restedOf;

        /// <summary>The program <see cref="_rested" /> handed back, which the server lends again where nobody changed it since.</summary>
        string _restedProgram;

        long _asked;

        readonly Dictionary<long, EditorHabits> _habits = new Dictionary<long, EditorHabits>();

        /// <summary>This client's player's settings, as the server last lent them; the world's own are on the server.</summary>
        readonly PlayerMemory _mine = new PlayerMemory();

        Interval _keeping = new Interval(Keeping);

        Interval _reclaiming = new Interval(Reclaiming);

        readonly List<IMyPlayer> _players = new List<IMyPlayer>();

        readonly List<IMyCubeGrid> _group = new List<IMyCubeGrid>();

        Settings _settings;

        readonly Texts _texts = new Texts();

        GameTexts _gameTexts;

        GameCodex _codex;

        /// <summary>
        /// The settings the world runs by: read the first time anything wants to know where this machine runs the world, and
        /// as the server last said on a client, every default until it has.
        /// </summary>
        /// <remarks>
        /// Not in <see cref="LoadData" />: mod storage is not reliably there that early, and a read
        /// that quietly answered every default would be indistinguishable from a file saying nothing.
        /// By the time a block adopts itself the world is up.
        /// </remarks>
        Settings Asked => _settings ?? (_settings = IsServer ? Settings.Read() : new Settings(false));

        /// <summary>What the mod says, in the player's language, or null before the session has loaded.</summary>
        /// <remarks>Static for the reason <see cref="_current" /> is: a block's own component reaches the session only through statics, and it registers the terminal button and toolbar action in these words.</remarks>
        public static Texts Texts => _current?._texts;

        /// <summary>The field boxes a gravity generator's fit is showing, or null outside a session.</summary>
        /// <remarks>Static for the reason <see cref="Texts" /> is: the gravity generator's buttons reach the session only through statics.</remarks>
        public static ShortfallMarks Marks => _current?._marks;

        /// <summary>Every door's auto-close delay, or null outside a session.</summary>
        /// <remarks>Static for the reason <see cref="Texts" /> is: a door's slider and its own component reach the session only through statics.</remarks>
        public static AutoClose Doors => _current?._doors;

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
            return _current == null ? null : _current._roster.Kept(block);
        }

        /// <summary>
        /// Whether this machine is the one running programs.
        /// </summary>
        /// <remarks>
        /// For anything that has to tell "there is no Sprocket here" from "this machine is not the one
        /// keeping it": a display drawn on a client has no program to read, and saying the construct has
        /// no automation would be a lie in the one place a person would believe it.
        /// </remarks>
        public static bool Runs => IsServer;

        /// <summary>
        /// Every Sprocket mechanically connected to this block, in no particular order.
        /// </summary>
        /// <remarks>
        /// The same reach a Sprocket itself has - <c>GridGroups</c> on the mechanical group, which is
        /// what a player means by "this ship" - so a display on a rotor head lists the Sprocket in the
        /// hull. Filled into a list the caller owns, because this is asked on a cadence forever.
        /// </remarks>
        public static void OnConstruct(IMyCubeBlock block, List<SprocketUnit> into)
        {
            into.Clear();

            IMyCubeGrid grid = block?.CubeGrid;
            if (_current == null || grid == null || MyAPIGateway.GridGroups == null)
                return;

            _current._group.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, _current._group);

            foreach (SprocketUnit unit in _current._roster.All)
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
            MyAPIGateway.TerminalControls.CustomControlGetter += GravityFieldButtons.Arrange;
            MyAPIGateway.TerminalControls.CustomControlGetter += DoorControls.Arrange;

            if (IsServer)
            {
                FindBackups();
                FindCarrier();
            }

            // Who shares a construct's blocks with whom moves with the factions, which no block reports.
            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Factions != null)
                MyAPIGateway.Session.Factions.FactionStateChanged += Refactioned;

            if (MyAPIGateway.Utilities != null && !MyAPIGateway.Utilities.IsDedicated)
            {
                FindShelf();
                _preferences.Read();
            }
        }

        /// <summary>The name the game knows the player <paramref name="identity" /> by, online or not; empty where it knows none.</summary>
        /// <remarks>Asked only as the resume screen is made, so the game's list of identities is walked a handful of times a session.</remarks>
        static string Named(long identity)
        {
            if (MyAPIGateway.Players == null)
                return "";

            var identities = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(identities);
            foreach (var known in identities)
            {
                if (known.IdentityId == identity)
                    return known.DisplayName;
            }

            return "";
        }

        /// <summary>A faction joined, left, made peace or went to war, which may change what every construct shares with whom.</summary>
        void Refactioned(MyFactionStateChange change, long fromFaction, long toFaction, long playerId, long senderId)
        {
            foreach (SprocketUnit unit in _roster.All)
                unit.World.Reshare();
        }

        /// <summary>Reads the player's library out of the global store, once, as the world starts.</summary>
        /// <remarks>A file that will not read is logged and listed as it is; an index that will not read is never written over.</remarks>
        void FindShelf()
        {
            var trouble = _shelf.Read();
            if (trouble.Length > 0)
                GameLog.Say(trouble);

            if (!_shelf.Intact)
                GameLog.Say("the library is not kept this session");
            else
                GameLog.Say($"read {_shelf.All.Count} blueprints from the library");
        }

        /// <summary>Reads the index of the world's backups out of world storage, once, as the world starts.</summary>
        /// <remarks>
        /// An index that will not read is logged and never written over: the backups go on being taken this session, and are
        /// not kept.
        /// </remarks>
        void FindBackups()
        {
            var trouble = _backups.Read();
            GameLog.Say(trouble.Length > 0 ? $"{trouble}; the backups are not kept this session" : $"read {_backups.All.Count} backups");
        }

        /// <summary>Unpacks the files in the world's carrier, once, as the world starts.</summary>
        /// <remarks>Files that will not read are logged and ignored, and the next save writes the files over them.</remarks>
        void FindCarrier()
        {
            var found = Carrier.Find();
            var trouble = found == null ? "" : _carried.Unpack(found.Text);
            GameLog.Say(trouble.Length > 0
                ? $"{trouble}; what the carrier held is ignored, and the next save writes the files over it"
                : $"read {_carried.Count} files from the carrier");

            _carrier = found;
            _carries = true;

            _memories.Read(_carried.Has(Memories.File) ? _carried.Read(Memories.File) : null, GameFrame());
            if (_memories.Trouble.Length > 0)
                GameLog.Say(_memories.Trouble);
        }

        /// <inheritdoc />
        public override void LoadData()
        {
            // Static because Space Engineers constructs a session component and hands it to nothing: a terminal
            // control, a screen host and a block's own component reach it only through statics. It holds the one
            // instance, which is an ordinary object.
            _current = this;

            // Before Speak, which rewords every Sprocket.
            _roster = new Roster(() => Asked, _rota, _texts, _memories, _backups, Frame, Later)
            {
                Shelf = _shelf,
                Spreading = Spread,
                Emptied = id => _handover.Tell(id, "", ""),
                Dropped = unit =>
                {
                    if (_shown == unit && _screen != null)
                        _screen.CloseNow();
                }
            };

            Speak();

            _handover = new Handover(Found, Given, Refused, Foreign, Changed, Stored, Player, () => Asked.Written(),
                Settled, written => Rule(Settings.Of(written)));
            _doors = new AutoClose(_rota, Frame, _memories.Closing);
            _channel = AutoClose.Carry(Handover.Carry(NetworkChannel.Create(GetType()))).Build();
            _handover.Listen(_channel);
            _doors.Listen(_channel);

            GameLog.Say(IsServer
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
            _codex = new GameCodex(ModContext.ModItem);
            _texts.Book.Pages = _codex;
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
            _codex.Load(MyAPIGateway.Session.Config.Language);
            _texts.Book.Load(_gameTexts);

            foreach (SprocketUnit unit in _roster.All)
                unit.Reword();

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
                : MyAPIGateway.Entities.GetEntityById(_roster.Key(id)) as IMyProgrammableBlock;

            return block == null ? null : Adopt(block);
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
                GameLog.Say($"block {id} was lent but is not here, so it goes straight back");
                _handover.Hand(id);
                return;
            }

            if (_copy != null)
                _copy.Dispose();

            // The copy handed back last, lent again with nothing changed since, is taken up again with its way back; anything
            // else is built afresh, since a history of what somebody else's change replaced cannot be walked.
            var again = _rested != null && _restedOf == id && _restedProgram == program;
            var copy = again ? _rested : SprocketUnit.Copy(block, Asked, _texts, program, state);
            if (!again && _rested != null)
                _rested.Dispose();

            _rested = null;
            _restedProgram = null;
            _copy = copy;
            _copyOf = id;
            _handover.Kept(id, copy);

            if (again)
            {
                copy.Restate(state);
                Lent(block, id, copy);
                return;
            }

            copy.Editor.Habits = HabitsFor(id);

            // Running something by hand runs it where it is kept. What is not built there yet - a
            // change still on its way - is simply not found, and nothing happens.
            copy.Editor.Elsewhere = (sequence, instance, stops) =>
            {
                _handover.Keep(id, copy);
                _handover.Run(id, sequence, instance, stops);
            };

            // And a picked button is written there too, after the program that wants it.
            copy.Editor.Wire = panel =>
            {
                _handover.Keep(id, copy);
                _handover.Wire(id, panel);
            };
            copy.Editor.Unwire = () => _handover.Unwire(id);

            Lent(block, id, copy);
        }

        /// <summary>Shows <paramref name="copy" />, lent of <paramref name="block" />, and hands it back when it is closed, keeping it for its way back.</summary>
        void Lent(IMyTerminalBlock block, long id, SprocketUnit copy)
        {
            Show(Opened(block), copy, delegate
            {
                // What changed since the last comparison goes before the Sprocket is handed back,
                // because a change arriving from somebody who no longer holds it is not taken.
                _handover.Keep(id, copy);
                _handover.Hand(id);
                if (_copy == copy)
                    _copy = null;

                string program;
                string state;
                copy.Written(out program, out state);
                if (_rested != null && _rested != copy)
                    _rested.Dispose();

                _rested = copy;
                _restedOf = id;
                _restedProgram = program;
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
            SprocketUnit was = _roster.Kept(id);
            if (was == null)
            {
                GameLog.Say($"a change arrived for block {id}, which carries no Sprocket here");
                return;
            }

            if (_roster.Replace(was, program, switches) != null)
                GameLog.Say($"{was.Block.CustomName} was built again from the change, and saved");
        }

        /// <summary>Whether the player at this machine may change the world's settings now: an admin, or higher, with creative tools on or in a creative world.</summary>
        /// <remarks>
        /// The game gives a mod no way to ask about creative tools directly. Copy and paste is on for the local player exactly
        /// while they have creative tools on, or the world is creative and allows it.
        /// </remarks>
        static bool Administers()
        {
            var session = MyAPIGateway.Session;
            return session != null && session.PromoteLevel >= MyPromoteLevel.Admin && (session.EnableCopyPaste || session.CreativeMode);
        }

        /// <summary>Changes the world's settings to <paramref name="next" />: at once where this machine runs the world, by asking the server otherwise.</summary>
        void ChangeSettings(Settings next)
        {
            if (!IsServer)
            {
                _handover.Propose(next.Written());
                return;
            }

            Rule(next);
            _handover.Rule();
        }

        /// <summary>Runs the world by <paramref name="next" /> from now on, keeps each level that changed in its own file, and has every Sprocket here follow.</summary>
        /// <remarks>On the machine that runs the world.</remarks>
        void Rule(Settings next)
        {
            var was = Asked;
            _settings = next;
            next.Keep(was);
            foreach (var unit in _roster.All)
                unit.Apply(next);
        }

        /// <summary>The server's settings arriving at this client, which it takes as its own, the Sprocket it shows included.</summary>
        void Settled(string written)
        {
            _settings = Settings.Of(written);
            if (_copy != null)
                _copy.Apply(_settings);
        }

        /// <summary>The settings in this world of the player with <paramref name="identity" />; on a client, which knows its own alone, its own.</summary>
        PlayerMemory Player(long identity)
        {
            return IsServer ? _memories.Player(identity) : _mine;
        }

        /// <summary>How this player has been working in the editor for a block, made the first time it is asked.</summary>
        EditorHabits HabitsFor(long id)
        {
            EditorHabits habits;
            if (!_habits.TryGetValue(id, out habits))
            {
                habits = new EditorHabits();
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
            _handover.Tell(unit.Block.EntityId, program, StateText.Save(unit.Program, null, false));
        }

        /// <summary>What the server wrote into a block, arriving at this client for its copy of the block.</summary>
        void Stored(long id, string program, string switches)
        {
            var block = MyAPIGateway.Entities == null
                ? null
                : MyAPIGateway.Entities.GetEntityById(id) as IMyTerminalBlock;

            if (block != null)
                BlockStorage.Store(block, program, switches);
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

        /// <summary>Tells the player that the automation on <paramref name="id" />'s construct belongs to its owners, and opens nothing.</summary>
        void Foreign(long id)
        {
            var block = MyAPIGateway.Entities == null ? null : MyAPIGateway.Entities.GetEntityById(id) as IMyTerminalBlock;
            var construct = block == null || block.CubeGrid == null ? "" : block.CubeGrid.CustomName;
            Say(Template.Fill(_texts.TerminalForeign, "construct", construct));
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

            return _current._roster.Adopt(block);
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
        public static void Open(IMyTerminalBlock block)
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

            var player = MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null ? 0L : MyAPIGateway.Session.Player.IdentityId;
            if (!unit.Owns(player))
            {
                _current.Foreign(block.EntityId);
                return;
            }

            var id = multiplayer?.MyId ?? 0UL;
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
            unit.Preferences = _preferences;
            unit.Editor.PlayerName = Named;
            var player = MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null ? 0L : MyAPIGateway.Session.Player.IdentityId;
            unit.Viewer = Player(player);
            unit.Editor.Preferred = IsServer ? null : (Action)(() => _handover.Prefer(_mine.Ticks));
            unit.Administers = Administers;
            unit.WorldSettings = () => Asked;
            unit.ChangeSettings = ChangeSettings;
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

        /// <summary>Draws the boxes a gravity generator's fit is showing, while there are any.</summary>
        public override void Draw()
        {
            if (MyAPIGateway.Session != null)
                _marks.Draw(MyAPIGateway.Session.ElapsedPlayTime);
        }

        /// <inheritdoc />
        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session == null)
                return;

            int frame = Frame();

            // Ticked for its editor, which is all a copy has that moves, and compared with what the
            // server was last sent on a cadence of its own. A copy lent to look at sends nothing.
            if (_copy != null)
            {
                _copy.Tick(frame);
                _copy.Show(frame);
                if (_keeping.Due(frame) && _copy.Editor.Holder == null)
                    _handover.Keep(_copyOf, _copy);
            }

            if (IsServer && _reclaiming.Due(frame))
                _handover.Reclaim(Connected);

            // The rota holds a door's coming close as well as the Sprockets' work, so it runs while either is there.
            if (_roster.Count == 0 && _rota.Count == 0)
                return;

            _roster.Tick(frame);
        }

        /// <summary>Runs <paramref name="action" /> once the game has finished its current event.</summary>
        /// <remarks>A grid raises its events while it is still changing.</remarks>
        static void Later(Action action)
        {
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.InvokeOnGameThread(action);
        }

        /// <summary>Sprocket's clock, which the rota counts in: the game's frame carried on from where the world was saved.</summary>
        int Frame()
        {
            return _memories.Now(GameFrame());
        }

        /// <summary>The frame the game is on, which starts again from nought with every session.</summary>
        static int GameFrame()
        {
            return MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;
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

            foreach (var player in _players)
            {
                if (player.SteamUserId == id)
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
            _roster.Persist();
            Remember();
            Carry();
            return base.GetObjectBuilder();
        }

        /// <summary>Writes what every live Sprocket is doing into the carrier's files, whole, as the world saves.</summary>
        void Remember()
        {
            if (!_carries || !IsServer)
                return;

            _remembered.Clear();
            foreach (SprocketUnit unit in _roster.All)
            {
                if (!unit.Lost)
                    _remembered.Add(unit.Memory);
            }

            _closing.Clear();
            if (_doors != null)
                _doors.Remember(_closing);

            _carried.Write(Memories.File, _memories.Write(GameFrame(), _remembered, _closing));
            _remembered.Clear();
            _closing.Clear();
        }

        /// <summary>Packs the carrier's files into the world's carrier where they changed, making it where the world has none yet.</summary>
        /// <remarks>
        /// Once a save, and only after a change, which costs what packing the files costs and nothing during play. A world that
        /// never had a file written is given no carrier.
        /// </remarks>
        void Carry()
        {
            if (!_carries || !IsServer || !_carried.Changed)
                return;

            if (_carrier == null)
                _carrier = Carrier.Make();

            _carrier.Write(_carried.Pack());
        }

        /// <inheritdoc />
        protected override void UnloadData()
        {
            if (MyAPIGateway.TerminalControls != null)
            {
                MyAPIGateway.TerminalControls.CustomControlGetter -= SprocketControls.Arrange;
                MyAPIGateway.TerminalControls.CustomControlGetter -= GravityFieldButtons.Arrange;
                MyAPIGateway.TerminalControls.CustomControlGetter -= DoorControls.Arrange;
            }

            if (_gameTexts != null && MyAPIGateway.Gui != null)
                MyAPIGateway.Gui.GuiControlRemoved -= Closed;

            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Factions != null)
                MyAPIGateway.Session.Factions.FactionStateChanged -= Refactioned;

            _gameTexts = null;

            if (_screen != null)
                _screen.CloseNow();

            if (_copy != null)
                _copy.Dispose();

            if (_rested != null && _rested != _copy)
                _rested.Dispose();

            if (_roster != null)
                _roster.Dispose();

            if (_doors != null)
                _doors.Dispose();

            if (_channel != null)
                _channel.Dispose();

            _roster = null;
            _screen = null;
            _shown = null;
            _copy = null;
            _rested = null;
            _channel = null;
            _handover = null;
            _doors = null;
            _settings = null;
            _current = null;
        }
    }
}
