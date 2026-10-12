using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>The game's binary serialization, in base-64 because the world keeps text.</summary>
    public sealed class GameBinary : IBinary
    {
        /// <inheritdoc />
        public string Pack(WorldMemory memory)
        {
            return Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(memory));
        }

        /// <inheritdoc />
        public WorldMemory Unpack(string text)
        {
            return MyAPIGateway.Utilities.SerializeFromBinary<WorldMemory>(Convert.FromBase64String(text));
        }
    }
}
