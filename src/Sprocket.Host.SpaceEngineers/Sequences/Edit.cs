using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

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
    public abstract class Edit<T>
    {
        /// <summary>What this did, as it will be offered back.</summary>
        public abstract Change Change { get; }

        /// <summary>Makes this change to <paramref name="subject" />.</summary>
        public abstract void Apply(T subject);

        /// <summary>Takes this change back out of <paramref name="subject" />, leaving it as it was before <see cref="Apply" />.</summary>
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

        /// <summary>Whether what undoing this left is still what is there, so redoing it would make this change and nothing else.</summary>
        public virtual bool Redoable(T subject)
        {
            return true;
        }

        /// <summary>
        /// Whether taking this back, where <paramref name="back" />, or making it again, waits for a switch now: it changes what
        /// may be running.
        /// </summary>
        /// <remarks>
        /// A step is walked under the rule it was made under, so nothing can be undone that could not be done now. Nothing waits
        /// unless the edit says so.
        /// </remarks>
        public virtual bool Held(T subject, bool back)
        {
            return false;
        }

        /// <summary>
        /// Adds to <paramref name="screens" /> each thing with a screen of its own that this changes what is shown on: a
        /// sequence, a slot, an installation, a logic system; the thing it is made to, <paramref name="subject" />, where it
        /// says nothing else.
        /// </summary>
        /// <remarks>
        /// What undo on a screen walks: the changes that touched it, and with each of those every later change that touched a
        /// screen it touched too, so what is taken back is always what was there.
        /// </remarks>
        public virtual void Touches(T subject, IList<object> screens)
        {
            Touch(screens, subject);
        }

        /// <summary>
        /// Says again what this made true of names that later changes may have brought back otherwise, while it stands and
        /// something made before it was walked back.
        /// </summary>
        /// <remarks>A name given a slot is the logic system's, so a step brought back by undo names the slot as it is called now.</remarks>
        public virtual void Reassert(T subject)
        {
        }

        /// <summary>Adds <paramref name="screen" /> to <paramref name="screens" />, once.</summary>
        protected static void Touch(IList<object> screens, object screen)
        {
            if (screen != null && !screens.Contains(screen))
                screens.Add(screen);
        }

        /// <summary>A list of its own holding the same ids as <paramref name="ids" />.</summary>
        protected static List<string> CopyOf(IList<string> ids)
        {
            var copy = new List<string>();
            for (var i = 0; i < ids.Count; i++)
                copy.Add(ids[i]);
            return copy;
        }

        /// <summary>Empties <paramref name="target" /> and fills it with the ids in <paramref name="source" />, keeping the list itself.</summary>
        protected static void Refill(IList<string> target, IList<string> source)
        {
            target.Clear();
            for (var i = 0; i < source.Count; i++)
                target.Add(source[i]);
        }

        /// <summary>Whether two lists hold the same ids in the same order.</summary>
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

        /// <summary>A list of its own holding the same arguments as <paramref name="arguments" />, and an empty one where there are none.</summary>
        protected static List<Literal> CopyOf(IList<Literal> arguments)
        {
            var copy = new List<Literal>();
            if (arguments == null)
                return copy;

            for (var i = 0; i < arguments.Count; i++)
                copy.Add(arguments[i]);

            return copy;
        }

        /// <summary>Empties <paramref name="target" /> and fills it with the arguments in <paramref name="source" />, keeping the list itself.</summary>
        protected static void Refill(IList<Literal> target, IList<Literal> source)
        {
            target.Clear();
            for (var i = 0; i < source.Count; i++)
                target.Add(source[i]);
        }

        /// <summary>Whether two lists of arguments say the same thing.</summary>
        /// <remarks>
        /// By how each one reads, which is what a value is for: two that write themselves the same
        /// way are the same value, and comparing the kinds as well would call a step changed for a
        /// difference nothing can see.
        /// </remarks>
        protected static bool Same(IList<Literal> left, IList<Literal> right)
        {
            if (left.Count != right.Count)
                return false;

            for (var i = 0; i < left.Count; i++)
            {
                if (left[i].Said != right[i].Said)
                    return false;
            }

            return true;
        }
    }
}
