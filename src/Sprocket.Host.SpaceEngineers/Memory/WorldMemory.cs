using System.Collections.Generic;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>Everything a world remembers of Sprocket beyond the programs: every construct's runtime state, and every player's settings.</summary>
    /// <remarks>A member's number is a stored format: never reused, never given another type. Number 4 is retired.</remarks>
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

        /// <summary>The blueprints shared in this world.</summary>
        [ProtoMember(5)]
        public List<SharedBlueprint> Shares { get; set; } = new List<SharedBlueprint>();

        /// <summary>The identities of the players an admin has stopped sharing.</summary>
        [ProtoMember(6)]
        public List<long> Barred { get; set; } = new List<long>();
    }
}
