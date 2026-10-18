using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.Network
{
    partial class NetworkChannel
    {
        class MessageModel<TModel> : IMessageModel where TModel : struct
        {
            readonly object _bufferLock = new object();
            readonly List<DataModelReceivedFn<TModel>> _handlers;
            readonly ReadDataModelFn<TModel> _readFn;
            readonly SizeOfDataModelFn<TModel> _sizeOfFn;
            readonly WriteDataModelFn<TModel> _writeFn;
            byte[] _buffer;

            public MessageModel(Type type, SizeOfDataModelFn<TModel> sizeOfFn, ReadDataModelFn<TModel> readFn, WriteDataModelFn<TModel> writeFn)
            {
                Type = type;
                TypeId = type.FullName.GetHashCode();
                _sizeOfFn = sizeOfFn;
                _readFn = readFn;
                _writeFn = writeFn;
                _handlers = new List<DataModelReceivedFn<TModel>>();
            }

            public int TypeId { get; }
            public Type Type { get; }

            public void DidReceive(NetworkChannel network, ushort channelId, byte[] data, ref int offset, ulong senderId, bool fromServer)
            {
                TModel msg;
                _readFn(data, ref offset, out msg);
                foreach (var handler in _handlers) handler(channelId, msg, senderId, fromServer);
            }

            public void AddListener(DataModelReceivedFn<TModel> handler)
            {
                _handlers.Add(handler);
            }

            public void RemoveListener(DataModelReceivedFn<TModel> handler)
            {
                _handlers.Remove(handler);
            }

            // The size of the [typeId][payload] inner message a recipient dispatches.
            public int InnerSize(ref TModel model)
            {
                return _sizeOfFn(ref model) + 4;
            }

            // Writes the [typeId][payload] inner message into data, outside the channel's send buffer.
            public void WriteInner(ref TModel model, byte[] data, ref int offset)
            {
                ByteCodec.Write(TypeId, data, ref offset);
                _writeFn(ref model, data, ref offset);
            }

            public void Send(ushort channelId, ref TModel model, ulong recipientId, bool reliable, bool includingSelf = false)
            {
                lock (_bufferLock)
                {
                    var size = _sizeOfFn(ref model) + 4;
                    if (_buffer == null || _buffer.Length < size)
                        _buffer = new byte[size];
                    var offset = 0;
                    ByteCodec.Write(TypeId, _buffer, ref offset);
                    _writeFn(ref model, _buffer, ref offset);

                    switch (recipientId)
                    {
                        case 0UL:
                            Network.SendMessageToOthers(channelId, _buffer, reliable);
                            if (includingSelf)
                            {
                                if (Network.IsServer)
                                    Network.SendMessageToServer(channelId, _buffer, reliable);
                                else if (MyAPIGateway.Session.LocalHumanPlayer != null)
                                {
                                    var localPlayerId = MyAPIGateway.Session.LocalHumanPlayer.SteamUserId;
                                    Network.SendMessageTo(channelId, _buffer, localPlayerId, reliable);
                                }
                            }

                            break;
                        case ulong.MaxValue:
                            Network.SendMessageToServer(channelId, _buffer, reliable);
                            break;
                        default:
                            Network.SendMessageTo(channelId, _buffer, recipientId, reliable);
                            break;
                    }
                }
            }
        }
    }
}
