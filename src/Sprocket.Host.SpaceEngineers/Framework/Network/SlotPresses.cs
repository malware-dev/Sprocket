using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using System;
using Sandbox.ModAPI;
using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Framework.Network
{
    /// <summary>
    /// A press of a toolbar slot holding Sprocket's action, fired where the automation runs: at once on the server, and sent to
    /// it from a client.
    /// </summary>
    /// <remarks>
    /// A button panel's press is sent to the server by the game itself, and the action runs there. A ship's toolbar, in a
    /// cockpit, a seat or a remote control, runs what its slot holds on the pressing player's own machine and sends nothing,
    /// so on a dedicated server the press has to be carried. The server takes it only from a player the block lets use it,
    /// as the game does for a button panel.
    /// </remarks>
    public sealed class SlotPresses
    {
        readonly Func<long, IMyTerminalBlock> _find;
        readonly Func<IMyTerminalBlock, ulong, bool> _may;
        readonly Action<IMyTerminalBlock, string> _fire;

        NetworkChannel _channel;

        /// <summary>
        /// Presses that find a block by entity id with <paramref name="find" />, ask <paramref name="may" /> whether the player a
        /// message came from may use it, and fire a slot's key on it with <paramref name="fire" />.
        /// </summary>
        public SlotPresses(Func<long, IMyTerminalBlock> find, Func<IMyTerminalBlock, ulong, bool> may, Action<IMyTerminalBlock, string> fire)
        {
            _find = find;
            _may = may;
            _fire = fire;
        }

        /// <summary>Registers what a press sends on the channel being built, and answers the builder.</summary>
        public static NetworkChannel.Builder Carry(NetworkChannel.Builder builder)
        {
            return builder.UsingModel<SlotPress>(SlotPress.SizeOf, SlotPress.Read, SlotPress.Write);
        }

        /// <summary>Starts hearing presses on <paramref name="channel" />, which is built with <see cref="Carry" />.</summary>
        public void Listen(NetworkChannel channel)
        {
            _channel = channel;
            _channel.ListenFor<SlotPress>(Arrived);
        }

        /// <summary>Fires <paramref name="key" /> on <paramref name="block" /> where this machine is the server, and sends it there otherwise.</summary>
        public void Pressed(IMyTerminalBlock block, string key)
        {
            if (NetworkChannel.Network.IsServer)
            {
                _fire(block, key);
                return;
            }

            GameLog.Say($"sending a press of a toolbar slot of block {block.EntityId} to the server");
            _channel?.SendToServer(new SlotPress { Block = block.EntityId, Key = key }, true);
        }

        /// <summary>A press arriving at the server, fired where the player it came from may use the block.</summary>
        void Arrived(ushort channel, SlotPress press, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer || string.IsNullOrEmpty(press.Key))
                return;

            var block = _find(press.Block);
            if (block == null)
                return;

            if (!_may(block, sender))
            {
                GameLog.Say($"Player {sender} pressed a toolbar slot of block {press.Block}, and was ignored: they may not use it");
                return;
            }

            _fire(block, press.Key);
        }
    }
}
