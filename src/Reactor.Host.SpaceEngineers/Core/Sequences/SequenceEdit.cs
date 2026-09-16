using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.Core.Sequences
{
    /// <summary>
    /// One change to a sequence, and how to take it back.
    /// </summary>
    /// <remarks>
    /// Edits mutate the sequence that is already there rather than rebuilding it. That is not
    /// a detail: Reactor is live, sequences run while they are being edited, and the runner
    /// and the open pages hold references to the very objects being changed. Anything that
    /// reconstructed the sequence in order to undo would leave all of them pointing at
    /// orphans.
    ///
    /// Each edit carries what it needs to reverse itself - the step it removed, the name it
    /// replaced - so undoing never has to go looking. That is the same shape a change needs to
    /// be sent to a server, which is where this has to end up anyway.
    ///
    /// The obvious risk is an <see cref="Undo"/> that does not exactly reverse its
    /// <see cref="Apply"/>, and it is a real one, because undo only ever runs when somebody is
    /// already recovering from a mistake. It is answered by a round-trip test per edit rather
    /// than by being careful: apply it, undo it, and the sequence must equal what it was.
    /// </remarks>
    abstract class SequenceEdit
    {
        /// <summary>What this did, in the words it will be offered back in.</summary>
        public abstract string Label { get; }

        public abstract void Apply(Sequence sequence);

        public abstract void Undo(Sequence sequence);

        /// <summary>
        /// Whether this actually changes anything.
        /// </summary>
        /// <remarks>
        /// Going to a picker and coming back having settled on what was already there is not
        /// an edit, and recording it would offer to undo something nobody did.
        /// </remarks>
        public virtual bool Changes => true;

        /// <summary>
        /// Whether this changes the shape of the screen, rather than only what it says.
        /// </summary>
        /// <remarks>
        /// Renaming does not. Rebuilding for it would recreate the field being typed into and
        /// throw away the caret mid-word, and the heading is read every tick anyway.
        /// </remarks>
        public virtual bool ChangesShape => true;

        protected static List<string> CopyOf(IList<string> ids)
        {
            var copy = new List<string>();
            for (var i = 0; i < ids.Count; i++)
                copy.Add(ids[i]);
            return copy;
        }

        protected static void Refill(IList<string> target, IList<string> source)
        {
            target.Clear();
            for (var i = 0; i < source.Count; i++)
                target.Add(source[i]);
        }

        protected static bool Same(IList<string> left, IList<string> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var i = 0; i < left.Count; i++)
            {
                if (left[i] != right[i])
                    return false;
            }
            return true;
        }
    }
}
