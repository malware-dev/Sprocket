using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>A door set to close by itself, saved while its close was coming.</summary>
    [ProtoContract]
    public sealed class DoorMemory
    {
        /// <summary>The door's entity id.</summary>
        [ProtoMember(1)]
        public long Door { get; set; }

        /// <summary>The tick of Sprocket's clock it closes on.</summary>
        [ProtoMember(2, IsRequired = true)]
        public int Due { get; set; }
    }
}
