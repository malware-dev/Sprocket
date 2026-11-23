using System;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>What one installation holds in one of its system's states, where a step has set it.</summary>
    [ProtoContract]
    public sealed class StateMemory
    {
        /// <summary>The logic system's identity.</summary>
        [ProtoMember(1)]
        public Guid System { get; set; }

        /// <summary>The installation's number, or nought for a logic system that has none.</summary>
        [ProtoMember(2)]
        public int Instance { get; set; }

        /// <summary>The state's name.</summary>
        [ProtoMember(3)]
        public string State { get; set; } = "";

        /// <summary>The choice it holds, or empty where it holds a flag.</summary>
        [ProtoMember(4)]
        public string Choice { get; set; } = "";

        /// <summary>The flag it holds, where it holds no choice and its state holds a flag.</summary>
        [ProtoMember(5)]
        public bool Flag { get; set; }

        /// <summary>The number or length it holds, as the state's type reads it: an integer as it is, a real's double as its bits, a length as its ticks.</summary>
        [ProtoMember(6)]
        public long Bits { get; set; }

        /// <summary>The text it holds, where its state holds text.</summary>
        [ProtoMember(7)]
        public string Text { get; set; } = "";
    }
}
