using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>One installation that wants looking at, and what is wrong with it, as the status panel words it.</summary>
    public struct StatusItem
    {
        /// <summary>How much it wants looking at.</summary>
        public StatusLevel Level;

        /// <summary>The logic system it is an installation of, or null for a paused construct.</summary>
        public AutomationSystem System;

        /// <summary>The installation, or null where its logic system is no blueprint.</summary>
        public SystemInstance Instance;

        /// <summary>The sequence at fault, or null where a halt says why it stands still.</summary>
        public Sequence Sequence;

        /// <summary>What is wrong with <see cref="Sequence" />.</summary>
        public SequenceFault Fault;

        /// <summary>Why it was switched off because it could not work, or null.</summary>
        public Halt Halt;

        /// <summary>The bindings its blocks are named through.</summary>
        public Bindings Bindings;

        /// <summary>The tick it first wanted looking at, so the newest is shown first among its level.</summary>
        public int Since;

        /// <summary>Whether <paramref name="other" /> is the same installation wanting looking at for the same reason.</summary>
        public bool Same(StatusItem other)
        {
            return Level == other.Level && System == other.System && Instance == other.Instance && Sequence == other.Sequence
                && Fault.Same(other.Fault) && Halt == other.Halt;
        }
    }
}
