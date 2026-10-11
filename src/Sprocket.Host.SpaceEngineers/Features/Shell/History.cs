using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>
    /// The way back for one kind of thing: what was done to each one, and what was taken back from it.
    /// </summary>
    /// <remarks>
    /// One per thing undo is about: a logic system, whose history holds everything done to it on any of its screens, and the
    /// construct, whose history holds the logic systems and installations made, put on and taken out. Undo takes back the last
    /// thing done to the thing being worked on, in the order it was done.
    /// <para>
    /// Here rather than on the model, because it is a fact about somebody's editing session and not about the program. A
    /// system that is exported, shared or saved should carry what it is, never how its author got there.
    /// </para>
    /// <para>
    /// Kept per thing and across navigation, so backing out to look at something else and coming back does not silently
    /// throw the safety net away. Keyed by the thing itself rather than by an id, because a stack is only of any interest
    /// while what it is about still exists, or may be brought back.
    /// </para>
    /// </remarks>
    public sealed class History<T> where T : class
    {
        const int Depth = 100;

        readonly Dictionary<T, List<Edit<T>>> _done = new Dictionary<T, List<Edit<T>>>();

        readonly Dictionary<T, List<Edit<T>>> _undone = new Dictionary<T, List<Edit<T>>>();

        /// <summary>Makes a change and remembers how to take it back. False where it changed nothing.</summary>
        /// <remarks>
        /// Applying and recording are one call so they cannot come apart: an edit applied without being recorded is a hole in
        /// the way back, and one recorded without being applied is worse.
        /// </remarks>
        public bool Did(T subject, Edit<T> edit)
        {
            if (!edit.Changes)
                return false;

            edit.Apply(subject);

            // A new edit is a new branch, so whatever was walked back is gone.
            _undone.Remove(subject);

            var stack = Stack(_done, subject);
            stack.Add(edit);
            if (stack.Count > Depth)
                stack.RemoveAt(0);

            return true;
        }

        /// <summary>Whether there is a change to <paramref name="subject" /> to take back now: one there, and not waiting for a switch.</summary>
        public bool CanUndo(T subject)
        {
            var edit = Waiting(_done, subject);
            return edit != null && !edit.Held(subject, true);
        }

        /// <summary>Whether there is a change to <paramref name="subject" /> taken back to make again now: one there, and not waiting for a switch.</summary>
        public bool CanRedo(T subject)
        {
            var edit = Waiting(_undone, subject);
            return edit != null && !edit.Held(subject, false);
        }

        /// <summary>What undoing would undo, or <see cref="Change.None" /> where there is nothing.</summary>
        public Change Undoes(T subject)
        {
            var edit = Waiting(_done, subject);
            return edit == null ? Change.None : edit.Change;
        }

        /// <summary>What redoing would put back, or <see cref="Change.None" /> where there is nothing.</summary>
        public Change Redoes(T subject)
        {
            var edit = Waiting(_undone, subject);
            return edit == null ? Change.None : edit.Change;
        }

        /// <summary>Takes back the last change, and says which one it was, or null where there is none, or it waits for a switch.</summary>
        public Edit<T> Undo(T subject)
        {
            if (!CanUndo(subject))
                return null;

            var edit = Take(_done, subject);
            edit.Undo(subject);
            Stack(_undone, subject).Add(edit);
            return edit;
        }

        /// <summary>
        /// Puts back the last thing undone, and says which one it was, or null where there is none, or it waits for a switch.
        /// </summary>
        /// <remarks>
        /// The same <c>Apply</c> the original edit ran, not a third code path. An edit that could be applied, reversed, and
        /// then not applied again the same way would be a stranger bug than either half alone.
        /// </remarks>
        public Edit<T> Redo(T subject)
        {
            if (!CanRedo(subject))
                return null;

            var edit = Take(_undone, subject);
            edit.Apply(subject);
            Stack(_done, subject).Add(edit);
            return edit;
        }

        /// <summary>The last change made to <paramref name="subject" />, or null.</summary>
        public Edit<T> Last(T subject)
        {
            List<Edit<T>> stack;
            return _done.TryGetValue(subject, out stack) && stack.Count > 0 ? stack[stack.Count - 1] : null;
        }

        /// <summary>Takes <paramref name="edit" /> back and forgets it, where it is the last change made to <paramref name="subject" />; false where it is not.</summary>
        /// <remarks>For something made a moment ago and left without anything done to it: leaving it is a cancel, not a step to walk.</remarks>
        public bool Withdraw(T subject, Edit<T> edit)
        {
            List<Edit<T>> stack;
            if (!_done.TryGetValue(subject, out stack) || stack.Count == 0 || stack[stack.Count - 1] != edit)
                return false;

            stack.RemoveAt(stack.Count - 1);
            edit.Undo(subject);
            return true;
        }

        /// <summary>Forgets everything done to <paramref name="subject" />.</summary>
        public void Forget(T subject)
        {
            _done.Remove(subject);
            _undone.Remove(subject);
        }

        /// <summary>Forgets everything done to everything.</summary>
        public void Clear()
        {
            _done.Clear();
            _undone.Clear();
        }

        /// <summary>
        /// Drops what can no longer be walked back, from the top down.
        /// </summary>
        /// <remarks>
        /// The program is not the editor's alone - see <see cref="Edit{T}.Undoable" /> for the case this exists for. Asked
        /// before anything is read as well as before anything is done, so the arrows never name a change that would not
        /// happen, and a press never quietly does nothing.
        /// <para>
        /// Only from the top, because nothing below is reachable until what is above it has gone, and it is asked the moment
        /// it becomes so. Both ways are asked at once: a change from outside can invalidate a step back and the step forward
        /// that was waiting under it, and they are separate stacks.
        /// </para>
        /// <para>
        /// A screen asks this every tick, through the words on its arrows, so what an edit does to answer has to be cheap: a
        /// walk of the triggers a system has and a comparison of what one names. Nothing here reads a toolbar or builds a
        /// string.
        /// </para>
        /// </remarks>
        void Settle(T subject)
        {
            Settle(_done, subject, true);
            Settle(_undone, subject, false);
        }

        static void Settle(Dictionary<T, List<Edit<T>>> stacks, T subject, bool back)
        {
            List<Edit<T>> stack;
            if (!stacks.TryGetValue(subject, out stack))
                return;

            while (stack.Count > 0)
            {
                var last = stack.Count - 1;
                var fits = back ? stack[last].Undoable(subject) : stack[last].Redoable(subject);

                if (fits)
                    return;

                stack.RemoveAt(last);
            }
        }

        Edit<T> Waiting(Dictionary<T, List<Edit<T>>> stacks, T subject)
        {
            Settle(subject);

            List<Edit<T>> stack;
            if (!stacks.TryGetValue(subject, out stack) || stack.Count == 0)
                return null;

            return stack[stack.Count - 1];
        }

        Edit<T> Take(Dictionary<T, List<Edit<T>>> stacks, T subject)
        {
            Settle(subject);

            List<Edit<T>> stack;
            if (!stacks.TryGetValue(subject, out stack) || stack.Count == 0)
                return null;

            var last = stack.Count - 1;
            var edit = stack[last];
            stack.RemoveAt(last);
            return edit;
        }

        static List<Edit<T>> Stack(Dictionary<T, List<Edit<T>>> stacks, T subject)
        {
            List<Edit<T>> stack;
            if (!stacks.TryGetValue(subject, out stack))
            {
                stack = new List<Edit<T>>();
                stacks[subject] = stack;
            }

            return stack;
        }
    }
}
