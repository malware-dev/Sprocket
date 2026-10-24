using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>The game's binary serialization, in base-64 because the world and a block keep text.</summary>
    public sealed class GameBinary : IBinary
    {
        /// <inheritdoc />
        public string Pack<T>(T kept)
        {
            return Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(kept));
        }

        /// <inheritdoc />
        public T Unpack<T>(string text)
        {
            return MyAPIGateway.Utilities.SerializeFromBinary<T>(Convert.FromBase64String(text));
        }
    }
}
