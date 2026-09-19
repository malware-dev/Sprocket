using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One change to something, and how to take it back.
    /// </summary>
    /// <remarks>
    /// Edits change the thing that is already there rather than rebuilding it. That is not a
    /// detail: Sprocket is live, sequences run while they are being edited, and the runner and
    /// the open pages hold references to the very objects being changed. Anything that
    /// reconstructed one in order to undo would leave all of them pointing at orphans.
    ///
    /// Each edit carries what it needs to reverse itself - the step it removed, the name it
    /// replaced, the trigger it swapped out - so undoing never has to go looking. That is the
    /// same shape a change needs to be sent to a server, which is where this has to end up
    /// anyway.
    ///
    /// The obvious risk is an <see cref="Undo"/> that does not exactly reverse its
    /// <see cref="Apply"/>, and it is a real one, because undo only ever runs when somebody is
    /// already recovering from a mistake. It is answered by a round-trip test per edit rather
    /// than by being careful: apply it, undo it, and the thing must equal what it was.
    /// </remarks>
    abstract class Edit<T>
    {
        /// <summary>What this did, in the words it will be offered back in.</summary>
        public abstract string Label { get; }

        public abstract void Apply(T subject);

        public abstract void Undo(T subject);

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

        /// <summary>
        /// Whether what this did is still what is there, so undoing it would take back this change
        /// and nothing else.
        /// </summary>
        /// <remarks>
        /// The program is not the editor's alone. The host takes a block out of a trigger when
        /// somebody clears that button in the game - see <c>SprocketUnit.Drop</c> - and that is a
        /// decision rather than a mistake. Walking back over it would undo something the person
        /// meant, and putting back a trigger for a button that now does something else would claim a
        /// slot they had taken away.
        /// <para>
        /// So an edit that no longer fits what is there is dropped rather than applied, and nothing
        /// fails: the way back simply stops at the point where somebody else's decision begins.
        /// </para>
        /// </remarks>
        public virtual bool Undoable(T subject)
        {
            return true;
        }

        /// <summary>The same in the other direction: whether what undoing left is still there.</summary>
        public virtual bool Redoable(T subject)
        {
            return true;
        }

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
