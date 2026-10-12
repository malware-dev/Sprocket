using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>One player's settings in this world.</summary>
    [ProtoContract]
    public sealed class PlayerMemory
    {
        /// <summary>The player's identity.</summary>
        [ProtoMember(1)]
        public long Player { get; set; }

        /// <summary>Whether the player gives lengths of time in ticks rather than seconds.</summary>
        [ProtoMember(2)]
        public bool Ticks { get; set; }
    }
}
