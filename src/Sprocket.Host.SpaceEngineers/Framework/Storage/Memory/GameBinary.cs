using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>The game's binary serialization, in base-64 because the world and a block store text.</summary>
    public sealed class GameBinary : IBinary
    {
        /// <inheritdoc />
        public string Pack<T>(T stored)
        {
            return Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(stored));
        }

        /// <inheritdoc />
        public T Unpack<T>(string text)
        {
            return MyAPIGateway.Utilities.SerializeFromBinary<T>(Convert.FromBase64String(text));
        }
    }
}
