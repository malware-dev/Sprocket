using Sprocket.Host.SpaceEngineers.Framework.Running;

namespace Sprocket.Host.SpaceEngineers.Features.StatusPanel
{
    /// <summary>One installation that wants looking at, and what is wrong with it, in what the status panel words it from.</summary>
    /// <remarks>Holds names and numbers rather than words, so it travels to a client and is worded there in its player's language.</remarks>
    public struct StatusItem
    {
        /// <summary>What it wants looking at for.</summary>
        public StatusLevel Level;

        /// <summary>What the player called its logic system, or what its blocks suggest, or empty where neither says anything.</summary>
        public string Logic;

        /// <summary>Its logic system's number, which names it where nothing else does.</summary>
        public int Number;

        /// <summary>Whether it is one of a blueprint's installations rather than a logic system of its own.</summary>
        public bool Blueprint;

        /// <summary>What the player called the installation, or empty.</summary>
        public string Installation;

        /// <summary>Where the installation stands among its blueprint's, from 1.</summary>
        public int Position;

        /// <summary>What the sequence at fault is called, or empty where it has no name.</summary>
        public string Sequence;

        /// <summary>Whether the fault is in a trigger's own act rather than a sequence.</summary>
        public bool Alone;

        /// <summary>The row of the step at fault, or 0 where no step is.</summary>
        public int Row;

        /// <summary>Whether it was switched off because it could not work, with <see cref="Fault" /> saying why.</summary>
        public bool Halted;

        /// <summary>What is wrong, or why it was switched off.</summary>
        public SequenceFault Fault;

        /// <summary>The tick it first wanted looking at, so the newest is shown first among its level.</summary>
        public int Since;

        /// <summary>Whether <paramref name="other" /> is the same installation wanting looking at for the same reason.</summary>
        public bool Same(StatusItem other)
        {
            return Level == other.Level && Logic == other.Logic && Number == other.Number && Blueprint == other.Blueprint
                && Installation == other.Installation && Position == other.Position && Sequence == other.Sequence && Alone == other.Alone
                && Row == other.Row && Halted == other.Halted && Fault.Same(other.Fault) && Fault.Which == other.Fault.Which;
        }
    }
}
