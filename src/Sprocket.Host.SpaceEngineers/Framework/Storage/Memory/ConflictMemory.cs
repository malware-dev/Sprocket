using System;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>Two versions of one logic system, waiting for the player to keep one.</summary>
    [ProtoContract]
    public sealed class ConflictMemory
    {
        /// <summary>The identity of the version that was here first.</summary>
        [ProtoMember(1)]
        public Guid System { get; set; }

        /// <summary>The identity given to the version that arrived.</summary>
        [ProtoMember(2)]
        public Guid Waiting { get; set; }
    }
}
