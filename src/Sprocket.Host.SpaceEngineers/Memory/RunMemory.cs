using System;
using System.Collections.Generic;
using ProtoBuf;

namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>What one sequence on one installation remembers: whether it is running and where, what holds it, what stopped it last, and its counts.</summary>
    [ProtoContract]
    public sealed class RunMemory
    {
        /// <summary>The logic system's identity.</summary>
        [ProtoMember(1)]
        public Guid System { get; set; }

        /// <summary>The installation's number, or nought for a logic system that has none.</summary>
        [ProtoMember(2)]
        public int Instance { get; set; }

        /// <summary>The sequence's number.</summary>
        [ProtoMember(3)]
        public int Sequence { get; set; }

        /// <summary>Whether a run of it is under way.</summary>
        [ProtoMember(4)]
        public bool Running { get; set; }

        /// <summary>The step executing or waiting, or minus one at rest.</summary>
        [ProtoMember(5, IsRequired = true)]
        public int Step { get; set; } = -1;

        /// <summary>Whether the run is held at an await or a wait.</summary>
        [ProtoMember(6)]
        public bool Waiting { get; set; }

        /// <summary>How long the wait at the current step is, before it is pinned to a tick.</summary>
        [ProtoMember(7)]
        public int WaitTicks { get; set; }

        /// <summary>The tick the wait at the current step ends on, or minus one before it is pinned.</summary>
        [ProtoMember(8, IsRequired = true)]
        public int WaitUntil { get; set; } = -1;

        /// <summary>Whether an await is being looked at.</summary>
        [ProtoMember(9)]
        public bool Holding { get; set; }

        /// <summary>The tick the await began on.</summary>
        [ProtoMember(10)]
        public int HoldSince { get; set; }

        /// <summary>How many ticks the await was expected to take, or negative where nothing could say.</summary>
        [ProtoMember(11)]
        public int HoldExpected { get; set; }

        /// <summary>Whether the run under way was started by hand.</summary>
        [ProtoMember(12)]
        public bool ByHand { get; set; }

        /// <summary>What stopped the last run from outside, by its kept number: nought nothing, one a sequence, two a trigger, three a hand, four a switch.</summary>
        [ProtoMember(13)]
        public int StoppedBy { get; set; }

        /// <summary>The step the last run was at when something stopped it.</summary>
        [ProtoMember(14)]
        public int StoppedAt { get; set; }

        /// <summary>The number of the sequence that stopped the last run, where one did.</summary>
        [ProtoMember(15)]
        public int StoppedFrom { get; set; }

        /// <summary>What the last run met that ended it, or null.</summary>
        [ProtoMember(17)]
        public FaultMemory Met { get; set; }

        /// <summary>The tick the last run started on, or minus one.</summary>
        [ProtoMember(18, IsRequired = true)]
        public int LastFired { get; set; } = -1;

        /// <summary>How many runs have started.</summary>
        [ProtoMember(19)]
        public int Fired { get; set; }

        /// <summary>How often its trigger fired while it was already running.</summary>
        [ProtoMember(20)]
        public int Skipped { get; set; }

        /// <summary>The tick its trigger last fired while it was already running, or minus one.</summary>
        [ProtoMember(21, IsRequired = true)]
        public int LastSkipped { get; set; } = -1;

        /// <summary>How often something called it while it could not run.</summary>
        [ProtoMember(22)]
        public int Refused { get; set; }

        /// <summary>The acts started since the last await that it waits on, by step.</summary>
        [ProtoMember(23)]
        public List<int> Outstanding { get; set; } = new List<int>();

        /// <summary>The sequences started since the last await that it waits on, by number.</summary>
        [ProtoMember(24)]
        public List<int> Started { get; set; } = new List<int>();

        /// <summary>What it remembers of each block of the acts it waits on.</summary>
        [ProtoMember(25)]
        public List<ActMemory> Acts { get; set; } = new List<ActMemory>();

        /// <summary>The tick conflicts were last looked for, or the mark of never.</summary>
        [ProtoMember(26, IsRequired = true)]
        public int ConflictsAt { get; set; }

        /// <summary>Where it is looked at every ten ticks while it runs, on the rota.</summary>
        [ProtoMember(27)]
        public ShiftMemory Sampling { get; set; }

        /// <summary>Where its next step is looked at, on the rota.</summary>
        [ProtoMember(28)]
        public ShiftMemory Busy { get; set; }

        /// <summary>The tick it was last looked at on, which what is left of a wait is counted from.</summary>
        [ProtoMember(29)]
        public int Looked { get; set; }

        /// <summary>What the program said was wrong with it when it was last checked, or null.</summary>
        [ProtoMember(30)]
        public FaultMemory Found { get; set; }

        /// <summary>Why the last run gave up before its last step, at which step and on which block, or null.</summary>
        [ProtoMember(31)]
        public FaultMemory Quit { get; set; }
    }
}
