using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>What a run met that ended it: the problem by its kept spelling, the step, the block, and what the block said.</summary>
    [ProtoContract]
    public sealed class FaultMemory
    {
        /// <summary>The problem, as its kept spelling.</summary>
        [ProtoMember(1)]
        public string Problem { get; set; } = "";

        /// <summary>The step it was met at.</summary>
        [ProtoMember(2, IsRequired = true)]
        public int Step { get; set; }

        /// <summary>The block or slot it is about, by its reference, or empty.</summary>
        [ProtoMember(3)]
        public string Which { get; set; } = "";

        /// <summary>What the block said, or null.</summary>
        [ProtoMember(4)]
        public TroubleMemory Because { get; set; }
    }
}
