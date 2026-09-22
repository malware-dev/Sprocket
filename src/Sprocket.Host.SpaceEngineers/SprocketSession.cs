using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Terminal;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Runtime;

namespace Sprocket.Host.SpaceEngineers
{
    /// <summary>
    /// Owns the Sprockets on this client, and gives them a clock.
    /// </summary>
    /// <remarks>
    /// A unit runs whether or not anybody is looking at it, which is the whole point of automation, so the
    /// ticking lives here and not in the screen. The screen is a way to look at a unit and nothing more.
    /// </remarks>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SprocketSession : MySessionComponentBase
    {
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

        Settings _settings;

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

        /// <summary>Scratch for the construct walk. Reused, because a display asks on a cadence.</summary>
        readonly List<IMyCubeGrid> _group = new List<IMyCubeGrid>();

        /// <summary>
        /// Registers the trigger action, on every machine.
        /// </summary>
        /// <remarks>
        /// Here rather than in <see cref="LoadData" />, where <c>MyAPIGateway.TerminalControls</c>
        /// does not exist yet - registering there is a null reference that takes the session down
        /// before the world loads.
        /// <para>
        /// And not behind <see cref="IsServer" /> with the rest of Sprocket: a slot stores the
        /// action's id and each machine resolves it to draw the slot, so a client that resolves
        /// nothing draws somebody's wired button as unassigned. See <c>TriggerAction</c>.
        /// </para>
        /// </remarks>
        public override void BeforeStart()
        {
            TriggerAction.Register();
        }

        public override void LoadData()
        {
            _current = this;
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

            unit = new SprocketUnit(block, Asked, _rota);
            _units.Add(block.EntityId, unit);
            return unit;
        }

        /// <summary>
        /// Opens the editor on the construct this block sits on.
        /// </summary>
        /// <remarks>
        /// The terminal is closed rather than left standing behind, because two screens describing the same
        /// ship is clutter and the one underneath cannot be reached anyway while this one has focus.
        /// </remarks>
        internal static void Open(IMyTerminalBlock block)
        {
            SprocketUnit unit = Adopt(block);
            if (unit == null)
            {
                // Said out loud rather than silently doing nothing. A client editing a program the server
                // has never heard of is worse than a client being told it cannot yet, and getting the edits
                // onto a wire is its own piece of work.
                if (MyAPIGateway.Utilities != null)
                    MyAPIGateway.Utilities.ShowNotification("Automation cannot be edited from a client yet.", 4000);

                return;
            }

            _current.Show(block, unit);
        }

        void Show(IMyTerminalBlock block, SprocketUnit unit)
        {
            if (_screen == null || _open != block.EntityId)
            {
                if (_screen != null)
                    _screen.CloseNow();

                _screen = new SprocketScreen(unit);
                _open = block.EntityId;
            }

            if (MyAPIGateway.Gui != null)
                MyAPIGateway.Gui.CloseTerminalScreen();

            _screen.Open();
        }

        public override void UpdateAfterSimulation()
        {
            if (_units.Count == 0 || MyAPIGateway.Session == null)
                return;

            int frame = MyAPIGateway.Session.GameplayFrameCounter;
            foreach (KeyValuePair<long, SprocketUnit> unit in _units)
                unit.Value.Tick(frame);

            // After every Sprocket has ticked, so each one's bindings are current when the rota calls
            // its sequences.
            _rota.Run(frame);
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
                unit.Value.Persist();
        }

        protected override void UnloadData()
        {
            if (_screen != null)
                _screen.CloseNow();

            foreach (KeyValuePair<long, SprocketUnit> unit in _units)
                unit.Value.Dispose();

            _units.Clear();
            _screen = null;
            _settings = null;
            _current = null;
        }
    }
}
