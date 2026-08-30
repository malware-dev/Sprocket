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
            Annotation = "";
        }

        public int Id { get; private set; }

        /// <summary>
        /// What somebody wrote about this, in their own words, or empty.
        /// </summary>
        /// <remarks>
        /// Data somebody typed, not a comment: it is part of the model, round-tripped like a name, and
        /// shown by whatever is drawing. What a target labels it on screen - "description", "notes",
        /// anything, in any language - is the host's word and has no bearing on this one.
        /// </remarks>
        public string Annotation { get; set; }

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

        /// <summary>The system this belongs to.</summary>
        public AutomationSystem System { get; internal set; }

        /// <summary>
        /// Whether it may actually run, which needs its own switch and its system's.
        /// </summary>
        /// <remarks>
        /// The two are asked separately everywhere else, because "I parked this one rule" and
        /// "I am rewiring the whole airlock" are different intentions and each has to survive
        /// the other. This is the only place they meet.
        /// </remarks>
        public bool CanRun
        {
            get { return Enabled && (System == null || System.Enabled); }
        }
    }
}
