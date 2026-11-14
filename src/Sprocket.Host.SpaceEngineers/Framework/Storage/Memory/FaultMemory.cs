using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>What a run met that ended it: the problem by its number, the step, the block, and what the block said.</summary>
    [ProtoContract]
    public sealed class FaultMemory
    {
        /// <summary>The problem as an earlier build kept it, by a word, or empty; read, never written.</summary>
        [ProtoMember(1)]
        public string Spelled { get; set; } = "";

        /// <summary>The step it was met at.</summary>
        [ProtoMember(2, IsRequired = true)]
        public int Step { get; set; }

        /// <summary>The block or slot it is about, by its reference, or empty.</summary>
        [ProtoMember(3)]
        public string Which { get; set; } = "";

        /// <summary>What the block said, or null.</summary>
        [ProtoMember(4)]
        public TroubleMemory Because { get; set; }

        /// <summary>The problem, by the number set on its <c>SequenceProblem</c> value.</summary>
        /// <remarks>Kept as a whole number, so a build that does not know a newer problem reads it as none rather than failing.</remarks>
        [ProtoMember(5)]
        public int Problem { get; set; }
    }
}
