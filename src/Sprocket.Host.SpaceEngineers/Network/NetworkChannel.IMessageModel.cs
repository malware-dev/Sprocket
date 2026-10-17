using System;

namespace Sprocket.Host.SpaceEngineers.Network
{
    partial class NetworkChannel
    {
        interface IMessageModel
        {
            int TypeId { get; }
            Type Type { get; }

            void DidReceive(NetworkChannel network, ushort channelId, byte[] data, ref int offset, ulong senderId, bool fromServer);
        }
    }
}
