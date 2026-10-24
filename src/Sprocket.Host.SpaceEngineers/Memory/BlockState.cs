using System.Collections.Generic;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>What a construct's block keeps beside its program: only what anyone building a copy from a blueprint needs.</summary>
    /// <remarks>Persisted in the block, and so in every blueprint of the construct; nothing is added to it without the developer's yes.</remarks>
    [ProtoContract]
    public sealed class BlockState
    {
        /// <summary>Each logic system and installation switched off; everything not listed is on.</summary>
        [ProtoMember(1)]
        public List<SwitchMemory> Switches { get; set; } = new List<SwitchMemory>();
    }
}
