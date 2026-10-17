using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>The game's multiplayer API as an <see cref="INetworkProxy" />.</summary>
    public class GameNetwork : INetworkProxy
    {
        /// <inheritdoc />
        public bool IsHeadlessServer => MyAPIGateway.Utilities.IsDedicated;

        /// <inheritdoc />
        public bool IsServer => MyAPIGateway.Multiplayer.IsServer;

        /// <inheritdoc />
        public bool IsClient => !MyAPIGateway.Utilities.IsDedicated;

        /// <inheritdoc />
        public void RegisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(id, messageHandler);
        }

        /// <inheritdoc />
        public void UnregisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler)
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(id, messageHandler);
        }

        /// <inheritdoc />
        public void SendMessageTo(ushort channelId, byte[] data, ulong recipientId, bool reliable)
        {
            MyAPIGateway.Multiplayer.SendMessageTo(channelId, data, recipientId, reliable);
        }

        /// <inheritdoc />
        public void SendMessageToOthers(ushort channelId, byte[] data, bool reliable)
        {
            MyAPIGateway.Multiplayer.SendMessageToOthers(channelId, data, reliable);
        }

        /// <inheritdoc />
        public void SendMessageToServer(ushort channelId, byte[] data, bool reliable)
        {
            MyAPIGateway.Multiplayer.SendMessageToServer(channelId, data, reliable);
        }
    }
}
