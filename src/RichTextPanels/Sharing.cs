using System;
using Lumel;
using Sandbox.ModAPI;

namespace RichTextPanels
{
    /// <summary>Every panel's own settings, its vertical alignment and its fitting: set from the terminal on any machine, kept in the block, and shared by the server.</summary>
    /// <remarks>
    ///     A block's mod storage is saved with the world and sent to a client with the grid, and nothing after that: so a
    ///     client's change goes to the server, and the server tells every client what the block keeps now. A message is
    ///     the block's id, eight bytes, which setting, one, and its value, one.
    /// </remarks>
    public class Sharing
    {
        /// <summary>The mod's message channel, picked at random to stand clear of other mods' channels.</summary>
        const ushort Channel = 47293;

        const int Length = 10;
        const byte Upright = 0;
        const byte Fit = 1;

        /// <summary>Starts hearing what arrives on the mod's channel.</summary>
        public void Attach()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Channel, Received);
        }

        /// <summary>Stops hearing it.</summary>
        public void Detach()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Channel, Received);
        }

        /// <summary>Has <paramref name="block" /> keep its text at <paramref name="align" />, as its terminal control does on whichever machine it was set.</summary>
        /// <remarks>
        ///     A client keeps it in its own copy of the block at once, so the control and the screen show what was set, and
        ///     asks the server, which has the last word.
        /// </remarks>
        public void SetVertical(IMyTerminalBlock block, Align align)
        {
            Vertical.Store(block, align);
            Send(Written(block.EntityId, Upright, (byte)align));
        }

        /// <summary>Has <paramref name="block" /> fit its text to its screen or not, as its terminal control does on whichever machine it was set.</summary>
        /// <remarks>Kept at once and sent to the server, as <see cref="SetVertical" /> is.</remarks>
        public void SetFitting(IMyTerminalBlock block, bool fit)
        {
            Fitting.Store(block, fit);
            Send(Written(block.EntityId, Fit, fit ? (byte)1 : (byte)0));
        }

        static void Send(byte[] message)
        {
            if (MyAPIGateway.Multiplayer.IsServer)
                MyAPIGateway.Multiplayer.SendMessageToOthers(Channel, message);
            else
                MyAPIGateway.Multiplayer.SendMessageToServer(Channel, message);
        }

        /// <summary>
        ///     A setting arriving: at the server, taken from a player who may change the block and passed on to every
        ///     client; at a client, what the server's block keeps now.
        /// </summary>
        void Received(ushort channel, byte[] message, ulong sender, bool fromServer)
        {
            if (message == null || message.Length != Length || message[8] != Upright && message[8] != Fit)
                return;

            var block = MyAPIGateway.Entities.GetEntityById(BitConverter.ToInt64(message, 0)) as IMyTerminalBlock;
            if (block == null)
                return;

            if (!MyAPIGateway.Multiplayer.IsServer)
            {
                if (fromServer)
                    Keep(block, message);

                return;
            }

            var identity = MyAPIGateway.Players.TryGetIdentityId(sender);
            if (identity == 0L || !block.HasPlayerAccess(identity))
                return;

            Keep(block, message);
            MyAPIGateway.Multiplayer.SendMessageToOthers(Channel, message);
        }

        static void Keep(IMyTerminalBlock block, byte[] message)
        {
            if (message[8] == Upright)
                Vertical.Store(block, (Align)message[9]);
            else
                Fitting.Store(block, message[9] != 0);
        }

        static byte[] Written(long id, byte setting, byte value)
        {
            var message = new byte[Length];
            var bytes = BitConverter.GetBytes(id);
            Array.Copy(bytes, message, bytes.Length);
            message[8] = setting;
            message[9] = value;
            return message;
        }
    }
}
