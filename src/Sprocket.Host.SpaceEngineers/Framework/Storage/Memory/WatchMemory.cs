using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>What one trigger on one installation remembers: what it last read, when it last fell due, how often it fired, and its place on the rota.</summary>
    [ProtoContract]
    public sealed class WatchMemory
    {
        /// <summary>The logic system's identity.</summary>
        [ProtoMember(1)]
        public Guid System { get; set; }

        /// <summary>The installation's number, or nought for a logic system that has none.</summary>
        [ProtoMember(2)]
        public int Instance { get; set; }

        /// <summary>The trigger's place among its system's triggers, counted from one.</summary>
        [ProtoMember(3)]
        public int Trigger { get; set; }

        /// <summary>Whether the comparison held on all of the blocks, or on their sum: one or nought, or minus one before the first look.</summary>
        [ProtoMember(4, IsRequired = true)]
        public int All { get; set; } = -1;

        /// <summary>The tick a timer last fell due on, or minus one before its first look.</summary>
        [ProtoMember(5, IsRequired = true)]
        public int Due { get; set; } = -1;

        /// <summary>How many times it has fired.</summary>
        [ProtoMember(6)]
        public int Fired { get; set; }

        /// <summary>Whether the comparison held at the last look, on any block or on what it asks of them all.</summary>
        [ProtoMember(7)]
        public bool Live { get; set; }

        /// <summary>What it read of each block at its last look.</summary>
        [ProtoMember(8)]
        public List<ReadingMemory> Readings { get; set; } = new List<ReadingMemory>();

        /// <summary>Its place on the rota.</summary>
        [ProtoMember(9)]
        public ShiftMemory Shift { get; set; }
    }
}
