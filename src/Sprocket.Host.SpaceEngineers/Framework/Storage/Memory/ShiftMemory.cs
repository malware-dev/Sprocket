using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>Where one piece of repeating work stood on the rota: its place, how often it came round, and the tick it was due on.</summary>
    [ProtoContract]
    public sealed class ShiftMemory
    {
        /// <summary>Its place in the turn.</summary>
        [ProtoMember(1)]
        public int Place { get; set; }

        /// <summary>How often it came round: one, ten or a hundred ticks, or nought where it was off the rota or due once.</summary>
        [ProtoMember(2)]
        public int Period { get; set; }

        /// <summary>The tick it was due on once, where <see cref="Once" /> says it was.</summary>
        [ProtoMember(3)]
        public int At { get; set; }

        /// <summary>Whether it was due once, on <see cref="At" />.</summary>
        [ProtoMember(4)]
        public bool Once { get; set; }
    }
}
