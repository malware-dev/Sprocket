using System.Collections.Generic;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>What one construct's automation is doing at a moment of time.</summary>
    [ProtoContract]
    public sealed class ConstructMemory
    {
        /// <summary>The id of the programmable block storing the automation, which this is filed under.</summary>
        [ProtoMember(1)]
        public long Storage { get; set; }

        /// <summary>The big owners the automation was last confirmed for.</summary>
        [ProtoMember(2)]
        public List<long> Owners { get; set; } = new List<long>();

        /// <summary>Who the automation belonged to when it paused, empty while it is not paused.</summary>
        [ProtoMember(3)]
        public List<long> PausedFrom { get; set; } = new List<long>();

        /// <summary>One for each sequence on each installation that has ever been looked at.</summary>
        [ProtoMember(4)]
        public List<RunMemory> Runs { get; set; } = new List<RunMemory>();

        /// <summary>One for each trigger on each installation that watches or keeps time.</summary>
        [ProtoMember(5)]
        public List<WatchMemory> Watches { get; set; } = new List<WatchMemory>();

        /// <summary>The logic systems with two versions waiting for a choice.</summary>
        [ProtoMember(6)]
        public List<ConflictMemory> Conflicts { get; set; } = new List<ConflictMemory>();

        /// <summary>What each installation holds in each of its system's states that a step has set.</summary>
        [ProtoMember(7)]
        public List<StateMemory> States { get; set; } = new List<StateMemory>();
    }
}
