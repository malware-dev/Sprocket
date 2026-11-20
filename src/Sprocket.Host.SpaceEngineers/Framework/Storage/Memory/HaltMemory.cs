using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>Why Sprocket switched something off: the problem by its number, where it happened, and what the block said.</summary>
    [ProtoContract]
    public sealed class HaltMemory
    {
        /// <summary>The problem as an earlier build stored it, by a word, or empty; read, never written.</summary>
        [ProtoMember(1)]
        public string Spelled { get; set; } = "";

        /// <summary>The number of the sequence it happened in, or nought.</summary>
        [ProtoMember(2)]
        public int Sequence { get; set; }

        /// <summary>The step it happened at, or minus one.</summary>
        [ProtoMember(3, IsRequired = true)]
        public int Step { get; set; } = -1;

        /// <summary>The block it is about, by its reference, or empty.</summary>
        [ProtoMember(4)]
        public string Block { get; set; } = "";

        /// <summary>What the block said, or null.</summary>
        [ProtoMember(5)]
        public TroubleMemory Because { get; set; }

        /// <summary>The problem, by the number set on its <c>SequenceProblem</c> value.</summary>
        /// <remarks>Stored as a whole number, so a build that does not know a newer problem reads it as none rather than failing.</remarks>
        [ProtoMember(6)]
        public int Problem { get; set; }
    }
}
