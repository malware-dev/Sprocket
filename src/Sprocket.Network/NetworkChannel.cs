using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

// ReSharper disable PossibleNullReferenceException

namespace Sprocket.Network
{
    /// <summary>One network message channel, carrying every message type registered on it.</summary>
    /// <remarks>A mod uses one channel for all its message types, so it takes one channel id.</remarks>
    public partial class NetworkChannel : IDisposable
    {
        // Marks a SendToNearby envelope: origin, radius, and an inner message. It only has to differ from real message type
        // ids, which are FullName.GetHashCode() values.
        const int ProximityEnvelopeTypeId = 0x00C0FFEE;

        readonly ushort _channelId;

        readonly Dictionary<int, IMessageModel> _dataModels;

        NetworkChannel(ushort channelId, Dictionary<int, IMessageModel> dataModels)
        {
            _dataModels = dataModels;
            _channelId = channelId;
            Network.RegisterSecureMessageHandler(_channelId, OnMessageReceived);
        }

        /// <summary>The network every channel sends through, the game's unless a test puts another in its place.</summary>
        // Static because the game's network is one per process, and the harness swaps it for every channel at once.
        public static INetworkProxy Network { get; set; } = new GameNetwork();

        /// <summary>Unregisters the channel's message handler.</summary>
        public void Dispose()
        {
            Network.UnregisterSecureMessageHandler(_channelId, OnMessageReceived);
        }

        /// <summary>A builder for a channel whose id is drawn from the full name of <paramref name="type" />.</summary>
        /// <remarks>The id must not collide with another loaded mod's.</remarks>
        /// <param name="type">A type of the mod's own.</param>
        /// <returns>The builder.</returns>
        public static Builder Create(Type type)
        {
            var channelId = (ushort)(type.FullName.GetHashCode() & 0xFFFF);
            return new Builder(channelId);
        }

        /// <summary>A builder for a channel whose id is drawn from <paramref name="channelName" />.</summary>
        /// <remarks>The id must not collide with another loaded mod's.</remarks>
        /// <param name="channelName">A name unique to the mod.</param>
        /// <returns>The builder.</returns>
        public static Builder Create(string channelName)
        {
            var channelId = (ushort)(channelName.GetHashCode() & 0xFFFF);
            return new Builder(channelId);
        }

        /// <summary>A builder for the channel <paramref name="channelId" />.</summary>
        /// <remarks>The id must not collide with another loaded mod's.</remarks>
        /// <param name="channelId">The channel id.</param>
        /// <returns>The builder.</returns>
        public static Builder Create(ushort channelId)
        {
            return new Builder(channelId);
        }

        void OnMessageReceived(ushort channelId, byte[] data, ulong senderId, bool fromServer)
        {
            var pos = 0;
            int typeId;
            if (!ByteCodec.TryReadInt32(data, ref pos, out typeId))
            {
                MyLog.Default.Warning($"NetworkChannel received malformed message on channel {_channelId}.");
                return;
            }

            if (typeId == ProximityEnvelopeTypeId)
            {
                OnProximityEnvelope(channelId, data, ref pos, senderId);
                return;
            }

            IMessageModel dataModel;
            if (!_dataModels.TryGetValue(typeId, out dataModel))
            {
                MyLog.Default.Warning($"NetworkChannel received message for unregistered type ID {typeId} on channel {_channelId}.");
                return;
            }

            dataModel.DidReceive(this, channelId, data, ref pos, senderId, fromServer);
        }

        // On the server only: forwards the inner message of a SendToNearby envelope to every player in range but the sender.
        void OnProximityEnvelope(ushort channelId, byte[] data, ref int pos, ulong senderId)
        {
            Vector3D origin;
            double radius;
            int reliableFlag;
            if (!ByteCodec.TryReadVector3D(data, ref pos, out origin)) return;
            if (!ByteCodec.TryReadDouble(data, ref pos, out radius)) return;
            if (!ByteCodec.TryReadInt32(data, ref pos, out reliableFlag)) return;
            if (!Network.IsServer) return;

            int innerLen = data.Length - pos;
            if (innerLen <= 0) return;
            var inner = new byte[innerLen];
            Array.Copy(data, pos, inner, 0, innerLen);
            RelayToNearby(channelId, inner, origin, radius, reliableFlag != 0, senderId);
        }

        // On the server only, since it needs every player's position; does nothing where MyAPIGateway.Players is null.
        void RelayToNearby(ushort channelId, byte[] inner, Vector3D origin, double radius, bool reliable, ulong excludeSteamId)
        {
            if (MyAPIGateway.Players == null) return;
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            double r2 = radius * radius;
            foreach (var p in players)
            {
                if (p == null || p.SteamUserId == excludeSteamId) continue;
                if (Vector3D.DistanceSquared(p.GetPosition(), origin) > r2) continue;
                Network.SendMessageTo(channelId, inner, p.SteamUserId, reliable);
            }
        }

        /// <summary>Sends a message to every player within <paramref name="radius" /> meters of <paramref name="origin" />.</summary>
        /// <remarks>From a client the message goes by way of the server; recipients receive the plain message.</remarks>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="model">The message.</param>
        /// <param name="origin">Where the radius is measured from.</param>
        /// <param name="radius">How far a recipient may be, in meters.</param>
        /// <param name="reliable">Whether delivery is ensured.</param>
        /// <param name="includingSelf">Whether the sender receives it too.</param>
        /// <exception cref="InvalidOperationException">The type is not registered on this channel.</exception>
        public void SendToNearby<T>(T model, Vector3D origin, double radius, bool reliable, bool includingSelf = false) where T : struct
        {
            var type = typeof(T);
            var typeId = type.FullName.GetHashCode();
            IMessageModel dataModel;
            if (!_dataModels.TryGetValue(typeId, out dataModel))
                throw new InvalidOperationException($"Type {type.FullName} is not registered on this channel.");
            var typed = dataModel as MessageModel<T>;
            if (typed == null)
                throw new InvalidOperationException($"Type {type.FullName} has an invalid data model info.");

            // Inner = the normal [typeId][payload] bytes a recipient dispatches as type T.
            var innerLen = typed.InnerSize(ref model);
            var inner = new byte[innerLen];
            var ip = 0;
            typed.WriteInner(ref model, inner, ref ip);

            if (Network.IsServer)
            {
                // Already the relay hub: forward straight to in-range players (minus self).
                RelayToNearby(_channelId, inner, origin, radius, reliable, LocalSteamId());
            }
            else
            {
                // Wrap inner in a proximity envelope and ship it to the server to relay.
                var env = new byte[4 + 24 + 8 + 4 + innerLen];
                var ep = 0;
                ByteCodec.Write(ProximityEnvelopeTypeId, env, ref ep);
                ByteCodec.Write(origin, env, ref ep);
                ByteCodec.Write(radius, env, ref ep);
                ByteCodec.Write(reliable ? 1 : 0, env, ref ep);
                Array.Copy(inner, 0, env, ep, innerLen);
                Network.SendMessageToServer(_channelId, env, reliable);
            }

            if (includingSelf)
            {
                if (Network.IsServer)
                    Network.SendMessageToServer(_channelId, inner, reliable);
                else
                {
                    var selfId = LocalSteamId();
                    if (selfId != 0UL)
                        Network.SendMessageTo(_channelId, inner, selfId, reliable);
                }
            }
        }

        static ulong LocalSteamId()
        {
            var p = MyAPIGateway.Session != null ? MyAPIGateway.Session.LocalHumanPlayer : null;
            return p?.SteamUserId ?? 0UL;
        }

        /// <summary>Registers a handler for messages of type <typeparamref name="T" /> on this channel.</summary>
        /// <remarks>The type must be registered with <see cref="Builder.UsingModel{TModel}" />.</remarks>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="handler">What is called for each message.</param>
        /// <exception cref="InvalidOperationException">The type is not registered on this channel.</exception>
        public void ListenFor<T>(DataModelReceivedFn<T> handler) where T : struct
        {
            var type = typeof(T);
            var typeId = type.FullName.GetHashCode();
            IMessageModel dataModel;
            if (!_dataModels.TryGetValue(typeId, out dataModel))
                throw new InvalidOperationException($"Type {type.FullName} is not registered on this channel.");
            var typedDataModel = dataModel as MessageModel<T>;
            if (typedDataModel == null)
                throw new InvalidOperationException($"Type {type.FullName} has an invalid data model info.");
            typedDataModel.AddListener(handler);
        }

        /// <summary>Unregisters a handler registered with <see cref="ListenFor{T}" />.</summary>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="handler">The handler registered.</param>
        /// <exception cref="InvalidOperationException">The type is not registered on this channel.</exception>
        public void StopListeningFor<T>(DataModelReceivedFn<T> handler) where T : struct
        {
            var type = typeof(T);
            var typeId = type.FullName.GetHashCode();
            IMessageModel dataModel;
            if (!_dataModels.TryGetValue(typeId, out dataModel))
                throw new InvalidOperationException($"Type {type.FullName} is not registered on this channel.");
            var typedDataModel = dataModel as MessageModel<T>;
            if (typedDataModel == null)
                throw new InvalidOperationException($"Type {type.FullName} has an invalid data model info.");
            typedDataModel.RemoveListener(handler);
        }

        /// <summary>Sends a message to one player.</summary>
        /// <remarks>The type must be registered with <see cref="Builder.UsingModel{TModel}" />.</remarks>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="model">The message.</param>
        /// <param name="recipientId">The recipient's Steam ID.</param>
        /// <param name="reliable">Whether delivery is ensured.</param>
        /// <exception cref="InvalidOperationException">The type is not registered on this channel.</exception>
        public void Send<T>(T model, ulong recipientId, bool reliable) where T : struct
        {
            var type = typeof(T);
            var typeId = type.FullName.GetHashCode();
            IMessageModel dataModel;
            if (!_dataModels.TryGetValue(typeId, out dataModel))
                throw new InvalidOperationException($"Type {type.FullName} is not registered on this channel.");
            var typedDataModel = dataModel as MessageModel<T>;
            if (typedDataModel == null)
                throw new InvalidOperationException($"Type {type.FullName} has an invalid data model info.");
            typedDataModel.Send(_channelId, ref model, recipientId, reliable);
        }

        /// <summary>Sends a message to every other player.</summary>
        /// <remarks>The type must be registered with <see cref="Builder.UsingModel{TModel}" />.</remarks>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="model">The message.</param>
        /// <param name="reliable">Whether delivery is ensured.</param>
        /// <param name="includingSelf">Whether the sender receives it too.</param>
        /// <exception cref="InvalidOperationException">The type is not registered on this channel.</exception>
        public void Broadcast<T>(T model, bool reliable, bool includingSelf = false) where T : struct
        {
            var type = typeof(T);
            var typeId = type.FullName.GetHashCode();
            IMessageModel dataModel;
            if (!_dataModels.TryGetValue(typeId, out dataModel))
                throw new InvalidOperationException($"Type {type.FullName} is not registered on this channel.");
            var typedDataModel = dataModel as MessageModel<T>;
            if (typedDataModel == null)
                throw new InvalidOperationException($"Type {type.FullName} has an invalid data model info.");
            typedDataModel.Send(_channelId, ref model, 0UL, reliable, includingSelf);
        }

        /// <summary>Sends a message to the server.</summary>
        /// <remarks>The type must be registered with <see cref="Builder.UsingModel{TModel}" />.</remarks>
        /// <typeparam name="T">The message type.</typeparam>
        /// <param name="model">The message.</param>
        /// <param name="reliable">Whether delivery is ensured.</param>
        /// <exception cref="InvalidOperationException">The type is not registered on this channel.</exception>
        public void SendToServer<T>(T model, bool reliable) where T : struct
        {
            var type = typeof(T);
            var typeId = type.FullName.GetHashCode();
            IMessageModel dataModel;
            if (!_dataModels.TryGetValue(typeId, out dataModel))
                throw new InvalidOperationException($"Type {type.FullName} is not registered on this channel.");
            var typedDataModel = dataModel as MessageModel<T>;
            if (typedDataModel == null)
                throw new InvalidOperationException($"Type {type.FullName} has an invalid data model info.");
            typedDataModel.Send(_channelId, ref model, ulong.MaxValue, reliable);
        }
    }
}
