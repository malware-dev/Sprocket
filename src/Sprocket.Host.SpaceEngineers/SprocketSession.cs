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
    /// Sprockets run on the server alone; a client edits a copy the server lends it, and the copy runs nothing.
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

        SprocketUnit _rested;

        long _restedOf;

        string _restedProgram;

        long _asked;

        readonly Dictionary<long, EditorHabits> _habits = new Dictionary<long, EditorHabits>();

        readonly PlayerMemory _mine = new PlayerMemory();

        Interval _keeping = new Interval(Keeping);

        Interval _reclaiming = new Interval(Reclaiming);

        readonly List<IMyPlayer> _players = new List<IMyPlayer>();

        readonly List<IMyCubeGrid> _group = new List<IMyCubeGrid>();

        Settings _settings;

        readonly Texts _texts = new Texts();

        GameTexts _gameTexts;

        GameCodex _codex;

        /// <summary>The settings the world runs by, read on first use on the server and as the server last sent them on a client.</summary>
        /// <remarks>Not to be asked from <see cref="LoadData" />, where mod storage is not reliably there yet.</remarks>
        Settings Asked => _settings ?? (_settings = IsServer ? Settings.Read() : new Settings(false));

        /// <summary>What the mod says, in the player's language, or null before the session has loaded.</summary>
        public static Texts Texts => _current?._texts;

        /// <summary>The field boxes a gravity generator's fit is showing, or null outside a session.</summary>
        public static ShortfallMarks Marks => _current?._marks;

        /// <summary>Every door's auto-close delay, or null outside a session.</summary>
        public static AutoClose Doors => _current?._doors;

        /// <summary>Whether the player asked for the developer tools, read once a world has loaded.</summary>
        public static bool DeveloperMode => _current != null && _current.Asked.DeveloperMode;

        /// <summary>The Sprocket of the construct <paramref name="block" /> is on, or null; never creates one.</summary>
        /// <remarks>Asked when a slot is pressed, never on a tick.</remarks>
        public static SprocketUnit On(IMyTerminalBlock block)
        {
            return _current == null || block == null ? null : _current._roster.On(block);
        }

        /// <summary>Whether this machine is the one running programs.</summary>
        public static bool Runs => IsServer;

        /// <summary>Every Sprocket mechanically connected to this block, in no particular order.</summary>
        /// <remarks>Clears and fills <paramref name="into" />, allocating nothing.</remarks>
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
        public override void BeforeStart()
        {
            // MyAPIGateway.TerminalControls does not exist yet in LoadData. Subscribing adds nothing to
            // the block type, so it is safe before any programmable block has come alive.
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
        /// <remarks>Walks the game's whole list of identities; asked only as the resume screen is made.</remarks>
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
        void FindBackups()
        {
            var trouble = _backups.Read();
            GameLog.Say(trouble.Length > 0 ? $"{trouble}; the backups are not kept this session" : $"read {_backups.All.Count} backups");
        }

        /// <summary>Unpacks the files in the world's carrier, once, as the world starts.</summary>
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
            // Space Engineers hands a session component to nothing, so terminal controls, screen hosts and
            // block components reach it only through this static.
            _current = this;

            // Made before Speak, which rewords every Sprocket.
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

            // On every machine: a server presses slots, and a client draws them.
            SignalAction.Begin(_texts);

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
        /// <remarks>On a dedicated server the texts stay as written.</remarks>
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
        /// <remarks>Called whenever any screen closes, since the game announces no language change; costs one comparison.</remarks>
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
        /// <remarks>Runs on the server, for a client's claim.</remarks>
        SprocketUnit Found(long id)
        {
            var block = MyAPIGateway.Entities == null
                ? null
                : MyAPIGateway.Entities.GetEntityById(_roster.Key(id)) as IMyProgrammableBlock;

            return block == null ? null : Adopt(block);
        }

        /// <summary>The server lending this client a Sprocket, which opens the editor on a copy of it.</summary>
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

            // A copy's undo history holds only while nobody else has changed the program since it was handed back.
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

            // Runs on the server, where a change still on its way is not found yet.
            copy.Editor.Elsewhere = (sequence, instance, stops) =>
            {
                _handover.Keep(id, copy);
                _handover.Run(id, sequence, instance, stops);
            };

            // The program that wants the button reaches the server first.
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
                // Kept before handed back: the server ignores a change from somebody who no longer holds it.
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

        /// <summary>A client's change arriving at the server, which builds the Sprocket again from it and writes it into the block.</summary>
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
        static bool Administers()
        {
            // The game gives a mod no way to ask about creative tools. Copy and paste is on for the local
            // player exactly while they have creative tools on, or the world is creative and allows it.
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
        /// <remarks>Called on the machine that runs the world.</remarks>
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

        /// <summary>The settings in this world of the player with <paramref name="identity" />, or on a client its own player's.</summary>
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
        /// <remarks>Sends only what changed since the last telling, so a world save costs a comparison per Sprocket.</remarks>
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

        /// <summary>The server turning this client down, which says so or, where somebody else is editing it, opens it to look at.</summary>
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

        /// <summary>Tells the player that the automation on <paramref name="id" />'s construct belongs to its owners.</summary>
        void Foreign(long id)
        {
            var block = MyAPIGateway.Entities == null ? null : MyAPIGateway.Entities.GetEntityById(id) as IMyTerminalBlock;
            var construct = block == null || block.CubeGrid == null ? "" : block.CubeGrid.CustomName;
            Say(Template.Fill(_texts.TerminalForeign, "construct", construct));
        }

        /// <summary>Opens the editor on a copy to look at while <paramref name="holder" /> edits the real one.</summary>
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
        void Say(string what)
        {
            // A mission screen, since the HUD does not draw while the terminal is open.
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.ShowMissionScreen(_texts.TerminalOpen, null, null, what);
        }

        /// <summary>Takes on a block that carries a program, so it starts running.</summary>
        /// <remarks>Returns null outside a session or on a client.</remarks>
        public static SprocketUnit Adopt(IMyTerminalBlock block)
        {
            if (_current == null || block == null || !IsServer)
                return null;

            return _current._roster.Adopt(block);
        }

        /// <summary>Whether this machine is the one that runs things.</summary>
        /// <remarks>True in single player.</remarks>
        static bool IsServer => MyAPIGateway.Multiplayer == null || MyAPIGateway.Multiplayer.IsServer;

        /// <summary>Opens the editor on the construct this block sits on.</summary>
        /// <remarks>On a client the editor opens only once the server answers.</remarks>
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

            // A copy with a holder is only being looked at, and sends nothing.
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
        /// <remarks>The server's own player counts. Reads the player list afresh, every few seconds while a lease stands.</remarks>
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



        /// <summary>Writes every program into its hero as the world saves, before the entities are written out.</summary>
        /// <remarks>
        /// The game asks for this before writing the entities and runs <c>SaveData</c> after, so this is the last moment a
        /// block's storage reaches the save. Costs a string per Sprocket.
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
        /// <remarks>Runs once a save and packs only after a change, so it costs nothing during play.</remarks>
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

            SignalAction.End();

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
