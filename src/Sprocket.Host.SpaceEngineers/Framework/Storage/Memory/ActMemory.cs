using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>What a run remembers of one block of an act it waits on: whether the block said it was done, and what it was last seen doing.</summary>
    [ProtoContract]
    public sealed class ActMemory
    {
        /// <summary>The act's step.</summary>
        [ProtoMember(1)]
        public int Step { get; set; }

        /// <summary>The block's place among the act's blocks.</summary>
        [ProtoMember(2)]
        public int Index { get; set; }

        /// <summary>Whether the block said it was done with what it was told, short of what the act waits for.</summary>
        [ProtoMember(3)]
        public bool Done { get; set; }

        /// <summary>What the act's action remembers of the block, or null where it remembers nothing.</summary>
        [ProtoMember(4)]
        public ReaderMemory Seen { get; set; }
    }
}
