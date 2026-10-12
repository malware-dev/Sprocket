using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>What a block said was wrong: its cause by its kept spelling, its own words, and whether it ended the run.</summary>
    [ProtoContract]
    public sealed class TroubleMemory
    {
        /// <summary>The cause, as its kept spelling, or empty for none.</summary>
        [ProtoMember(1)]
        public string Cause { get; set; } = "";

        /// <summary>The block's own words, or empty.</summary>
        [ProtoMember(2)]
        public string About { get; set; } = "";

        /// <summary>Whether it ends a run rather than holding it.</summary>
        [ProtoMember(3)]
        public bool Ends { get; set; }
    }
}
