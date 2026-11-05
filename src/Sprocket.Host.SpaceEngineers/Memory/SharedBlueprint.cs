using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>A blueprint one player has shared in this world, kept by the server.</summary>
    [ProtoContract]
    public sealed class SharedBlueprint
    {
        /// <summary>The identity of the player who shared it.</summary>
        [ProtoMember(1)]
        public long Sharer { get; set; }

        /// <summary>The blueprint, as a library file holds it.</summary>
        [ProtoMember(2)]
        public string Blueprint { get; set; } = "";

        /// <summary>Who it is for.</summary>
        [ProtoMember(3)]
        public ShareReach Reach { get; set; }

        /// <summary>The identities of the players it is for, where <see cref="Reach" /> names players.</summary>
        [ProtoMember(4)]
        public List<long> Players { get; set; } = new List<long>();

        /// <summary>Sprocket's clock when it was first shared or its blueprint last changed.</summary>
        [ProtoMember(5)]
        public int Changed { get; set; }

        /// <summary>The blueprint's identity, as its text says.</summary>
        /// <remarks>Read from the text and never stored.</remarks>
        [ProtoIgnore]
        public Guid Identity { get; set; }

        /// <summary>What the blueprint is called, as its text says.</summary>
        /// <remarks>Read from the text and never stored.</remarks>
        [ProtoIgnore]
        public string Name { get; set; } = "";
    }
}
