using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>What a block said was wrong: its cause by its number, its own words, and whether it ended the run.</summary>
    [ProtoContract]
    public sealed class TroubleMemory
    {
        /// <summary>The cause as an earlier build kept it, by a word, or empty; read, never written.</summary>
        [ProtoMember(1)]
        public string Spelled { get; set; } = "";

        /// <summary>The block's own words, or empty.</summary>
        [ProtoMember(2)]
        public string About { get; set; } = "";

        /// <summary>Whether it ends a run rather than holding it.</summary>
        [ProtoMember(3)]
        public bool Ends { get; set; }

        /// <summary>The cause, by the number set on its <c>Cause</c> value, or nought for none.</summary>
        /// <remarks>Kept as a whole number, so a build that does not know a newer cause reads it as none rather than failing.</remarks>
        [ProtoMember(4)]
        public int Cause { get; set; }
    }
}
