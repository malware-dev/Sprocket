using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// Something the machine does: a trigger and the ordered steps it runs.
    /// </summary>
    /// <remarks>
    /// A sequence can be understood on its own, without reading any other sequence.
    /// That is what lets someone grow a program a piece at a time instead of holding
    /// the whole thing in their head, and it is the main thing separating this from a
    /// script.
    ///
    /// The name is optional. An unnamed sequence reads as its own sentence, which is
    /// enough for a reflex like "when the sensor trips, turn the lights on". A name
    /// buys two things and is worth it for neither reason alone: it makes a list of
    /// twenty sequences legible, and it lets more than one trigger start the same
    /// one. Naming is never required to get anything done.
    /// </remarks>
    public sealed class Sequence
    {
        readonly List<Step> _steps = new List<Step>();

        public Sequence(int id, Trigger trigger)
        {
            Id = id;
            Trigger = trigger;
            Enabled = true;
        }

        public int Id { get; private set; }

        /// <summary>What the user calls this, e.g. "Let me out". May be empty.</summary>
        public string Name { get; set; }

        public bool HasName
        {
            get { return !string.IsNullOrEmpty(Name); }
        }

        public Trigger Trigger { get; set; }

        public IList<Step> Steps { get { return _steps; } }

        /// <summary>
        /// Whether it may run. Switching a sequence off to see what changes is a
        /// normal way to work out what it was doing, so this is not deletion.
        /// </summary>
        public bool Enabled { get; set; }
    }
}
