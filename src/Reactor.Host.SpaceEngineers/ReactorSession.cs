using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.Components;
using VRage.Utils;

namespace Reactor.Host.SpaceEngineers
{
    /// <summary>
    /// Owns the Reactors on this client, and gives them a clock.
    /// </summary>
    /// <remarks>
    /// A unit runs whether or not anybody is looking at it, which is the whole point of automation, so the
    /// ticking lives here and not in the screen. The screen is a way to look at a unit and nothing more.
    /// </remarks>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ReactorSession : MySessionComponentBase
    {
        static ReactorSession _current;

        readonly Dictionary<long, ReactorUnit> _units = new Dictionary<long, ReactorUnit>();

        ReactorScreen _screen;
        long _open;

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
        public static ReactorUnit Adopt(IMyTerminalBlock block)
        {
            if (_current == null || block == null || !IsServer)
                return null;

            return _current.UnitFor(block);
        }

        /// <summary>
        /// Whether this machine is the one that runs things.
        /// </summary>
        /// <remarks>
        /// Reactor's runtime lives on the server and nowhere else, which is also where the game raises a
        /// button press: <c>MyButtonPanel.ActivateButton</c> is a server event, so a client never hears one.
        /// A client running its own copy would be a second airlock nobody asked for, opening the same doors
        /// a moment apart.
        ///
        /// True in single player, where the client is the server.
        /// </remarks>
        static bool IsServer
        {
            get
            {
                return MyAPIGateway.Multiplayer == null || MyAPIGateway.Multiplayer.IsServer;
            }
        }

        ReactorUnit UnitFor(IMyTerminalBlock block)
        {
            ReactorUnit unit;
            if (_units.TryGetValue(block.EntityId, out unit))
                return unit;

            unit = new ReactorUnit(block);
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
            ReactorUnit unit = Adopt(block);
            if (unit == null)
            {
                // Said out loud rather than silently doing nothing. A client editing a program the server
                // has never heard of is worse than a client being told it cannot yet, and getting the edits
                // onto a wire is its own piece of work.
                if (MyAPIGateway.Utilities != null)
                    MyAPIGateway.Utilities.ShowNotification("Reactor cannot be edited from a client yet.", 4000);

                return;
            }

            _current.Show(block, unit);
        }

        void Show(IMyTerminalBlock block, ReactorUnit unit)
        {
            if (_screen == null || _open != block.EntityId)
            {
                if (_screen != null)
                    _screen.CloseNow();

                _screen = new ReactorScreen(unit);
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
            foreach (KeyValuePair<long, ReactorUnit> unit in _units)
                unit.Value.Tick(frame);
        }

        /// <summary>
        /// Writes every program back to its block before the world is written out.
        /// </summary>
        /// <remarks>
        /// The screen already does this as it closes, which covers the ordinary way somebody finishes. This
        /// covers saving with the editor still open, and costs a string per Reactor.
        /// </remarks>
        public override void SaveData()
        {
            foreach (KeyValuePair<long, ReactorUnit> unit in _units)
                unit.Value.Persist();
        }

        protected override void UnloadData()
        {
            if (_screen != null)
                _screen.CloseNow();

            foreach (KeyValuePair<long, ReactorUnit> unit in _units)
                unit.Value.Dispose();

            _units.Clear();
            _screen = null;
            _current = null;
        }
    }

    /// <summary>
    /// The button on a programmable block that opens Reactor.
    /// </summary>
    /// <remarks>
    /// Controls are global per block type rather than per block, so this runs exactly once per mod load,
    /// driven off the first programmable block to come alive. See <see cref="ReactorBlock" /> for why it
    /// cannot be done any earlier than that.
    /// </remarks>
    static class ReactorControls
    {
        static bool _done;

        public static void DoOnce()
        {
            if (_done)
                return;

            _done = true;

            IMyTerminalControlButton open = MyAPIGateway.TerminalControls
                .CreateControl<IMyTerminalControlButton, IMyProgrammableBlock>("Reactor_Open");

            open.Title = MyStringId.GetOrCompute("Reactor");
            open.Tooltip = MyStringId.GetOrCompute("Open the Reactor editor.");
            open.SupportsMultipleBlocks = false;
            open.Visible = block => true;
            open.Enabled = block => true;
            open.Action = ReactorSession.Open;

            MyAPIGateway.TerminalControls.AddControl<IMyProgrammableBlock>(open);
        }
    }
}
