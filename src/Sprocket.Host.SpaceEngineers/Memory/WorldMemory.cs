using System.Collections.Generic;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>Everything a world remembers of Sprocket beyond the programs: every construct's runtime state, every player's settings and every door's coming close.</summary>
    /// <remarks>A member's number is a stored format: never reused, never given another type.</remarks>
    [ProtoContract]
    public sealed class WorldMemory
    {
        /// <summary>Sprocket's clock when the world was saved, which it carries on from after a load.</summary>
        [ProtoMember(1)]
        public int Clock { get; set; }

        /// <summary>Each construct's runtime state, filed under the block keeping its automation.</summary>
        [ProtoMember(2)]
        public List<ConstructMemory> Constructs { get; set; } = new List<ConstructMemory>();

        /// <summary>Each player's settings.</summary>
        [ProtoMember(3)]
        public List<PlayerMemory> Players { get; set; } = new List<PlayerMemory>();

        /// <summary>Each door whose close was coming.</summary>
        [ProtoMember(4)]
        public List<DoorMemory> Doors { get; set; } = new List<DoorMemory>();
    }
}
