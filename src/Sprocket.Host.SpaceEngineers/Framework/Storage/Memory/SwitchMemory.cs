using System;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>One logic system or installation switched off, and why, where Sprocket switched it off.</summary>
    [ProtoContract]
    public sealed class SwitchMemory
    {
        /// <summary>The logic system's identity.</summary>
        [ProtoMember(1)]
        public Guid System { get; set; }

        /// <summary>The installation's number, or nought for the logic system itself.</summary>
        [ProtoMember(2)]
        public int Instance { get; set; }

        /// <summary>Why Sprocket switched it off, or null where a person did.</summary>
        [ProtoMember(3)]
        public HaltMemory Halt { get; set; }
    }
}
