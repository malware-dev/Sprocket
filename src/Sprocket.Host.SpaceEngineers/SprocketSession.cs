using System;
using System.Collections.Generic;
using System.IO;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Network;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.Terminal;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Words;

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
        /// <summary>
        /// How often the copy is compared with what was last sent, in ticks: twice a second.
        /// </summary>
        /// <remarks>
        /// Not every tick, because comparing writes the whole program out, and a change applied on this
        /// client at once can reach the server a moment later without anybody waiting on it. Closing the
        /// editor sends whatever is left.
        /// </remarks>
        const int Keeping = 30;

        /// <summary>
        /// How often the server looks for holders who have gone, in ticks: every five seconds.
        /// </summary>
        /// <remarks>
        /// A Sprocket left held by somebody who crashed waits this long at most, which is far less than
        /// anybody takes to walk over and try it. Only while something is held.
        /// </remarks>
        const int Reclaiming = 300;

        /// <summary>The session component the game made, for the callers that have no instance of it.</summary>
        /// <remarks>
        /// A session component is constructed by Space Engineers and handed to nothing: a terminal
        /// control, a screen host and a block's own component are all reached through statics of the
        /// game's own. This holds the one instance so those can find the units it owns, and holds an
        /// ordinary object rather than being one.
        /// </remarks>
        static SprocketSession _current;

        readonly Dictionary<long, SprocketUnit> _units = new Dictionary<long, SprocketUnit>();

        /// <summary>The world's turn, shared by every Sprocket in it, so their waits spread across the ticks.</summary>
        readonly Rota _rota = new Rota();

        SprocketScreen _screen;
        long _open;

        /// <summary>The Sprocket the open screen shows, which a screen kept for a block may no longer.</summary>
        SprocketUnit _shown;

        /// <summary>
        /// The mod's one channel between machines.
        /// </summary>
        /// <remarks>
        /// Built as the session loads, on every machine, because a machine hears only what arrives after
        /// its handler is registered, and the first claim can come as soon as a client is in the world.
        /// </remarks>
        NetworkChannel _channel;

        Handover _handover;

        /// <summary>The copy of a Sprocket the server keeps, being edited on this client, or null.</summary>
        SprocketUnit _copy;

        /// <summary>The block the copy is of, by entity id.</summary>
        long _copyOf;

        /// <summary>How this player works in each block's editor, carried from one copy to the next.</summary>
        readonly Dictionary<long, Habits> _habits = new Dictionary<long, Habits>();

        Interval _keeping = new Interval(Keeping);

        Interval _reclaiming = new Interval(Reclaiming);

        /// <summary>Scratch for the players connected, read when leases are checked.</summary>
        readonly List<IMyPlayer> _players = new List<IMyPlayer>();

        /// <summary>Scratch for the construct walk. Reused, because a display asks on a cadence.</summary>
        readonly List<IMyCubeGrid> _group = new List<IMyCubeGrid>();

        Settings _settings;

        /// <summary>What every Sprocket's editor says, in the player's language.</summary>
        readonly Texts _texts = new Texts();

        /// <summary>The mod's texts in the game's own table, where the player's language is loaded; null on a machine with no player.</summary>
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
        }

        public override void LoadData()
        {
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

        /// <summary>The Sprocket a programmable block carries, by entity id, made where it has none; or null.</summary>
        /// <remarks>On the server, for a client's claim, so only a programmable block is ever taken on.</remarks>
        SprocketUnit Found(long id)
        {
            var block = MyAPIGateway.Entities == null
                ? null
                : MyAPIGateway.Entities.GetEntityById(id) as IMyProgrammableBlock;

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

            Show(block, copy, delegate
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
            was.Dispose();
            rebuilt.Persist();
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
                Say("This block's automation cannot be edited.");
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

            Show(block, copy, delegate
            {
                copy.Dispose();
                if (_copy == copy)
                    _copy = null;
            });
        }

        /// <summary>Says something to the player in a box over whatever screen is open.</summary>
        /// <remarks>
        /// Not the HUD's notifications: this answers a press in the terminal, and the HUD does not
        /// draw while the terminal is open.
        /// </remarks>
        static void Say(string what)
        {
            if (MyAPIGateway.Utilities != null)
                MyAPIGateway.Utilities.ShowMissionScreen("Automation", null, null, what);
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

        SprocketUnit UnitFor(IMyTerminalBlock block)
        {
            SprocketUnit unit;
            if (_units.TryGetValue(block.EntityId, out unit))
                return unit;

            unit = new SprocketUnit(block, Asked, _rota, _texts);
            _units.Add(block.EntityId, unit);
            return unit;
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
                _current._handover.Ask(block.EntityId, name);
                return;
            }

            SprocketUnit unit = Adopt(block);
            if (unit == null)
                return;

            var id = multiplayer == null ? 0UL : multiplayer.MyId;
            long lent = block.EntityId;
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
            if (_screen == null || _open != block.EntityId || _shown != unit)
            {
                if (_screen != null)
                    _screen.CloseNow();

                _screen = new SprocketScreen(unit, closed);
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
        /// Writes every program back to its block before the world is written out.
        /// </summary>
        /// <remarks>
        /// The screen already does this as it closes, which covers the ordinary way somebody finishes. This
        /// covers saving with the editor still open, and costs a string per Sprocket.
        /// </remarks>
        public override void SaveData()
        {
            foreach (KeyValuePair<long, SprocketUnit> unit in _units)
            {
                unit.Value.Persist();
                Spread(unit.Value);
            }
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
