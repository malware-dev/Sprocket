using System;
using System.Collections.Generic;

namespace Sprocket.Network
{
    partial class NetworkChannel
    {
        /// <summary>Registers the message types of a <see cref="NetworkChannel" /> and builds it.</summary>
        public struct Builder
        {
            readonly ushort _channelId;
            readonly Dictionary<int, IMessageModel> _registeredTypes;

            /// <summary>A builder for the channel <paramref name="channelId" />.</summary>
            /// <param name="channelId">The channel id.</param>
            public Builder(ushort channelId)
            {
                _channelId = channelId;
                _registeredTypes = new Dictionary<int, IMessageModel>();
            }

            Builder(ushort channelId, Dictionary<int, IMessageModel> registeredTypes)
            {
                _channelId = channelId;
                _registeredTypes = registeredTypes;
            }

            /// <summary>Registers the message type <typeparamref name="TModel" /> with how it is measured, read and written.</summary>
            /// <typeparam name="TModel">The message type.</typeparam>
            /// <param name="sizeOfFn">How many bytes a message takes.</param>
            /// <param name="readFn">What reads a message.</param>
            /// <param name="writeFn">What writes a message.</param>
            /// <returns>The builder, with the type registered.</returns>
            /// <exception cref="InvalidOperationException">The type is registered already, or its id is another type's.</exception>
            public Builder UsingModel<TModel>(SizeOfDataModelFn<TModel> sizeOfFn, ReadDataModelFn<TModel> readFn, WriteDataModelFn<TModel> writeFn) where TModel : struct
            {
                var type = typeof(TModel);
                var typeId = type.FullName.GetHashCode();
                IMessageModel existingType;
                if (_registeredTypes.TryGetValue(typeId, out existingType))
                {
                    if (existingType.Type != type)
                        throw new InvalidOperationException($"Type ID conflict: {type.FullName} and {existingType.Type.FullName} have the same type ID {typeId}.");
                    throw new InvalidOperationException($"Type {type.FullName} is already registered on this channel.");
                }

                var dataModelInfo = new MessageModel<TModel>(type, sizeOfFn, readFn, writeFn);
                _registeredTypes.Add(typeId, dataModelInfo);
                return new Builder(_channelId, _registeredTypes);
            }

            /// <summary>The channel, with every type registered on the builder.</summary>
            /// <returns>The channel.</returns>
            /// <exception cref="InvalidOperationException">The id is zero, the builder was not made by a constructor, or no type is registered.</exception>
            public NetworkChannel Build()
            {
                if (_channelId == 0)
                    throw new InvalidOperationException("Channel ID cannot be zero.");
                if (_registeredTypes == null)
                    throw new InvalidOperationException("This builder was not properly initialized.");
                if (_registeredTypes.Count == 0)
                    throw new InvalidOperationException("No types registered for this channel.");
                return new NetworkChannel(_channelId, _registeredTypes);
            }
        }
    }
}
