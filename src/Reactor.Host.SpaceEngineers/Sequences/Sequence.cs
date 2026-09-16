using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.Sequences
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

        public Sequence(int id)
        {
            Id = id;
            Enabled = true;
            Annotation = "";
        }

        /// <summary>
        /// A handle for this session only.
        /// </summary>
        /// <remarks>
        /// <b>Never saved.</b> It is unique across the whole program, which is what the runner, the editor's
        /// history and its commands need to tell sequences apart while it runs, and it means nothing after a
        /// reload. What a sequence is, to a save or a button, is its <see cref="Number" /> within its system.
        /// </remarks>
        public int Id { get; private set; }

        /// <summary>
        /// Which sequence of its system this is: 1, 2, 3.
        /// </summary>
        /// <remarks>
        /// Counted within the system rather than across the program, so a system carries its numbers with it
        /// when it is exported and imported, and a button naming a system and a number still means the same
        /// sequence. Chosen from the numbers the system already has - see
        /// <see cref="AutomationSystem.NextSequenceNumber" /> - and never changed afterwards, so a rename
        /// cannot unwire anything.
        /// </remarks>
        public int Number { get; internal set; }

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

        public bool HasName => !string.IsNullOrEmpty(Name);

        /// <summary>
        /// The first thing that starts this sequence, or null where nothing does.
        /// </summary>
        /// <remarks>
        /// Asked of the system, which is where triggers live. A sequence with nothing to start it is
        /// ordinary rather than broken: somebody is halfway through building it, or it is only ever
        /// reached from another sequence.
        /// </remarks>
        public Trigger Trigger => System == null ? null : System.TriggerFor(Number);

        public IList<Step> Steps => _steps;

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
        public bool CanRun => Enabled && (System == null || System.Enabled);
    }
}
