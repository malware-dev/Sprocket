using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Editing
{
    /// <summary>
    /// One change to something, and how to take it back.
    /// </summary>
    /// <remarks>
    /// An edit changes the objects already there and never rebuilds them: the runner and open pages hold references to
    /// them while sequences run.
    /// </remarks>
    public abstract class Edit<T>
    {
        /// <summary>What this did, as it will be offered back.</summary>
        public abstract ChangeKind Change { get; }

        /// <summary>Makes this change to <paramref name="subject" />.</summary>
        public abstract void Apply(T subject);

        /// <summary>Takes this change back out of <paramref name="subject" />, leaving it as it was before <see cref="Apply" />.</summary>
        public abstract void Undo(T subject);

        /// <summary>
        /// Whether this actually changes anything.
        /// </summary>
        public virtual bool Changes => true;

        /// <summary>
        /// Whether this changes the shape of the screen, rather than only what it says.
        /// </summary>
        /// <remarks>The heading is read every tick, so a change of name alone needs no rebuild.</remarks>
        public virtual bool ChangesShape => true;

        /// <summary>
        /// Whether what this did is still what is there, so undoing it would take back this change
        /// and nothing else.
        /// </summary>
        /// <remarks>An edit that no longer fits what is there is dropped, and the way back stops at it.</remarks>
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
        public virtual bool Held(T subject, bool back)
        {
            return false;
        }

        /// <summary>
        /// Adds to <paramref name="screens" /> each thing with a screen of its own that this changes what is shown on: a
        /// sequence, a slot, an installation, a logic system; the thing it is made to, <paramref name="subject" />, where it
        /// says nothing else.
        /// </summary>
        public virtual void Touches(T subject, IList<object> screens)
        {
            Touch(screens, subject);
        }

        /// <summary>
        /// Says again what this made true of names that later changes may have brought back otherwise, while it stands and
        /// something made before it was walked back.
        /// </summary>
        public virtual void Reassert(T subject)
        {
        }

        /// <summary>
        /// Puts into <paramref name="name" /> the things this change was made to, as <paramref name="subject" /> stands now, with
        /// the change made where <paramref name="done" /> and taken back where not.
        /// </summary>
        public virtual void Name(T subject, bool done, ChangeName name)
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
