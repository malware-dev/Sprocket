using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Phosphor
{
    /// <summary>
    /// Stops the game acting on keys while somebody is typing, and makes sure it starts again.
    /// <para>
    /// <b>This is server business, and that is not a preference.</b>
    /// <c>MyVisualScriptLogicProvider.SetPlayerInputBlacklistState</c> raises a multiplayer event, so a
    /// client cannot suspend its own input by asking itself. The client works out which controls to suspend,
    /// because only it has the key bindings, and sends the list; the server applies it.
    /// </para>
    /// <para>
    /// <b>The heartbeat is why this is safe.</b> A player whose game crashes mid-edit would otherwise be
    /// unable to move for as long as that world lives, with nothing left running to say otherwise. So the
    /// client repeats itself while it edits, and the server lifts anything it has not heard from. Everything
    /// here is built for that case rather than for the tidy one.
    /// </para>
    /// <para>
    /// Raw key reads are unaffected by any of this, which is what makes it workable: the game stops walking
    /// the player forward while W still arrives as the letter w.
    /// </para>
    /// </summary>
    public sealed class InputSuspension
    {
        /// <summary>Distinct enough not to collide with another mod's channel on the same world.</summary>
        private const ushort ChannelId = 0x5243;

        private const byte MessageSuspend = 1;
        private const byte MessageResume = 2;

        /// <summary>About a second between heartbeats, and three before the server gives up on one.</summary>
        private const int HeartbeatTicks = 60;
        private const int LapseTicks = 180;

        private readonly Action<ushort, byte[], ulong, bool> _handler;

        /// <summary>What the server suspended for a player, so exactly that is lifted again.</summary>
        private sealed class Suspension
        {
            public int LastHeard;
            public readonly List<string> Controls = new List<string>();
        }

        private readonly Dictionary<long, Suspension> _suspended = new Dictionary<long, Suspension>();
        private readonly List<long> _lapsed = new List<long>();
        private readonly List<string> _controls = new List<string>();

        private bool _active;
        private int _lastSent;

        public InputSuspension()
        {
            _handler = OnMessage;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(ChannelId, _handler);
        }

        /// <summary>True while this client believes its input is suspended.</summary>
        public bool IsActive
        {
            get { return _active; }
        }

        /// <summary>
        /// Unregisters and lifts everything still standing. The blacklist is process state rather than world
        /// state, so leaving one behind would follow the player into the next world they load.
        /// </summary>
        public void Dispose()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(ChannelId, _handler);

            foreach (KeyValuePair<long, Suspension> pair in _suspended)
                Apply(pair.Key, pair.Value.Controls, false);

            _suspended.Clear();
            _active = false;
        }

        // ---- client ----

        /// <summary>
        /// Asks the server to suspend this player's controls. Does nothing if already asked.
        /// </summary>
        /// <param name="suspendLook">
        /// Whether aiming goes too. True while a mouse pointer is up, since the movement that would turn the
        /// player's head is what moves the pointer instead.
        /// </param>
        public void Begin(bool suspendLook)
        {
            if (_active)
                return;

            long player = LocalPlayerId();
            if (player == 0)
                return;

            InputNames.Collect(_controls, suspendLook);

            _active = true;
            _lastSent = Tick();
            Send(MessageSuspend, player, _controls, true);
        }

        /// <summary>Tells the server to lift the suspension. Safe to call when there is none.</summary>
        public void End()
        {
            if (!_active)
                return;

            _active = false;

            long player = LocalPlayerId();
            if (player != 0)
                Send(MessageResume, player, null, true);
        }

        /// <summary>
        /// Call every tick. Repeats the request often enough that the server keeps it, and does the server's
        /// own sweep when this is the server. Both halves live here because in single player they are the
        /// same process and splitting them would only invent a difference.
        /// </summary>
        public void Update()
        {
            int now = Tick();

            if (_active)
            {
                long player = LocalPlayerId();

                if (player == 0)
                {
                    // No player to suspend for any more; better to release than to hold something open.
                    _active = false;
                }
                else if (now - _lastSent >= HeartbeatTicks)
                {
                    _lastSent = now;

                    // A bare keep-alive. The server kept the controls from the reliable first message, so
                    // resending them every second would be bytes spent saying nothing new.
                    Send(MessageSuspend, player, null, false);
                }
            }

            if (!MyAPIGateway.Multiplayer.IsServer)
                return;

            _lapsed.Clear();
            foreach (KeyValuePair<long, Suspension> pair in _suspended)
                if (now - pair.Value.LastHeard > LapseTicks)
                    _lapsed.Add(pair.Key);

            for (int i = 0; i < _lapsed.Count; i++)
            {
                long player = _lapsed[i];

                Suspension suspension;
                if (_suspended.TryGetValue(player, out suspension))
                    Apply(player, suspension.Controls, false);

                _suspended.Remove(player);
                MyLog.Default.WriteLineAndConsole(
                    "[Reactor] input suspension lapsed, lifted for player " + player);
            }
        }

        // ---- server ----

        private void OnMessage(ushort channel, byte[] data, ulong sender, bool fromServer)
        {
            if (!MyAPIGateway.Multiplayer.IsServer || data == null || data.Length < 1)
                return;

            byte kind;
            long player;
            List<string> controls;
            if (!TryRead(data, out kind, out player, out controls))
                return;

            if (kind == MessageResume)
            {
                Suspension existing;
                if (!_suspended.TryGetValue(player, out existing))
                    return;

                Apply(player, existing.Controls, false);
                _suspended.Remove(player);
                return;
            }

            if (kind != MessageSuspend)
                return;

            Suspension suspension;
            if (!_suspended.TryGetValue(player, out suspension))
            {
                // A heartbeat that arrives before the first real request is a stray, because heartbeats are
                // unreliable and can overtake. Only a message carrying controls may open a session.
                if (controls == null || controls.Count == 0)
                    return;

                suspension = new Suspension();
                suspension.Controls.AddRange(controls);
                _suspended[player] = suspension;
                Apply(player, suspension.Controls, true);
            }

            suspension.LastHeard = Tick();
        }

        private static void Apply(long player, List<string> controls, bool suspend)
        {
            if (controls == null)
                return;

            // The third argument is inverted: true removes the control from the blacklist.
            for (int i = 0; i < controls.Count; i++)
                MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(controls[i], player, !suspend);
        }

        // ---- wire ----

        private void Send(byte kind, long player, List<string> controls, bool reliable)
        {
            byte[] data = Write(kind, player, controls);

            // Hosting our own game, so the server is this process. Handing the bytes straight to the handler
            // keeps single player on exactly the same path as a client, rather than on a shortcut that would
            // be the only one ever exercised until somebody finally tries a server.
            if (MyAPIGateway.Multiplayer.IsServer)
            {
                OnMessage(ChannelId, data, 0UL, false);
                return;
            }

            MyAPIGateway.Multiplayer.SendMessageToServer(ChannelId, data, reliable);
        }

        private static byte[] Write(byte kind, long player, List<string> controls)
        {
            StringBuilder joined = new StringBuilder();
            int count = controls == null ? 0 : controls.Count;

            for (int i = 0; i < count; i++)
            {
                if (i > 0)
                    joined.Append('\n');
                joined.Append(controls[i]);
            }

            byte[] names = Encoding.UTF8.GetBytes(joined.ToString());
            byte[] data = new byte[1 + 8 + names.Length];

            data[0] = kind;
            byte[] id = BitConverter.GetBytes(player);
            for (int i = 0; i < 8; i++)
                data[1 + i] = id[i];
            for (int i = 0; i < names.Length; i++)
                data[9 + i] = names[i];

            return data;
        }

        private static bool TryRead(byte[] data, out byte kind, out long player, out List<string> controls)
        {
            kind = 0;
            player = 0;
            controls = null;

            if (data.Length < 9)
                return false;

            kind = data[0];
            player = BitConverter.ToInt64(data, 1);

            if (data.Length == 9)
                return true;

            string joined = Encoding.UTF8.GetString(data, 9, data.Length - 9);
            if (joined.Length == 0)
                return true;

            controls = new List<string>(joined.Split('\n'));
            return true;
        }

        private static long LocalPlayerId()
        {
            IMyPlayer player = MyAPIGateway.Session == null ? null : MyAPIGateway.Session.LocalHumanPlayer;
            return player == null ? 0 : player.IdentityId;
        }

        private static int Tick()
        {
            return MyAPIGateway.Session == null ? 0 : MyAPIGateway.Session.GameplayFrameCounter;
        }
    }
}
