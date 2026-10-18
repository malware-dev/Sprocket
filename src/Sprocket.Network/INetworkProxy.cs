using System;

namespace Sprocket.Network
{
    /// <summary>The game's multiplayer messaging, as a mod uses it.</summary>
    public interface INetworkProxy
    {
        /// <summary>Whether this process is a dedicated server.</summary>
        bool IsHeadlessServer { get; }

        /// <summary>Whether this process is the multiplayer server.</summary>
        bool IsServer { get; }

        /// <summary>Whether this process has a player, which a hosted server does too.</summary>
        bool IsClient { get; }

        /// <summary>Registers a secure message handler for a channel.</summary>
        /// <param name="id">The channel.</param>
        /// <param name="messageHandler">What is called for each message on it.</param>
        void RegisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler);

        /// <summary>Unregisters a secure message handler from a channel.</summary>
        /// <param name="id">The channel.</param>
        /// <param name="messageHandler">The handler registered.</param>
        void UnregisterSecureMessageHandler(ushort id, Action<ushort, byte[], ulong, bool> messageHandler);

        /// <summary>Sends a message to one player.</summary>
        /// <param name="channelId">The channel.</param>
        /// <param name="data">The message.</param>
        /// <param name="recipientId">The recipient's Steam ID.</param>
        /// <param name="reliable">Whether delivery is ensured.</param>
        void SendMessageTo(ushort channelId, byte[] data, ulong recipientId, bool reliable);

        /// <summary>Sends a message to every player but this one.</summary>
        /// <param name="channelId">The channel.</param>
        /// <param name="data">The message.</param>
        /// <param name="reliable">Whether delivery is ensured.</param>
        void SendMessageToOthers(ushort channelId, byte[] data, bool reliable);

        /// <summary>Sends a message to the server.</summary>
        /// <param name="channelId">The channel.</param>
        /// <param name="data">The message.</param>
        /// <param name="reliable">Whether delivery is ensured.</param>
        void SendMessageToServer(ushort channelId, byte[] data, bool reliable);
    }
}