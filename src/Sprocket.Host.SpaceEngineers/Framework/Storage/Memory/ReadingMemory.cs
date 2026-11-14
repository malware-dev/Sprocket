using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>What a trigger read of one block at its last look, and what rules its moment out, by the name the program gives the block.</summary>
    [ProtoContract]
    public sealed class ReadingMemory
    {
        /// <summary>The block, by the name the program gives it.</summary>
        [ProtoMember(1)]
        public string Block { get; set; } = "";

        /// <summary>Whether the comparison held on it.</summary>
        [ProtoMember(2)]
        public bool Held { get; set; }

        /// <summary>Whether it has been read at all, which an edge needs two looks to know.</summary>
        [ProtoMember(3)]
        public bool Known { get; set; }

        /// <summary>What the reading that rules its moment out remembers of the block, or null.</summary>
        [ProtoMember(4)]
        public ReaderMemory Ruling { get; set; }

        /// <summary>What the watched signal remembers of the block, or null.</summary>
        [ProtoMember(5)]
        public ReaderMemory Seen { get; set; }

        /// <summary>What the value read remembers of the block, or null.</summary>
        [ProtoMember(6)]
        public ReaderMemory Gauge { get; set; }
    }
}
