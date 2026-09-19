using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Editing
{
    /// <summary>
    /// The way back for one level of the program: what was done to each thing, and what was taken
    /// back from it.
    /// </summary>
    /// <remarks>
    /// One of these per level somebody edits, because undo means "take back the last thing I did to
    /// the thing I am looking at" - and somebody looking at a system did not do the last thing to a
    /// sequence inside it. Both levels want the same handful of operations, which is why this is a
    /// type rather than a second copy of them.
    /// <para>
    /// Here rather than on the model, because it is a fact about somebody's editing session and not
    /// about the program. A system that is exported, shared or saved should carry what it is, never
    /// how its author got there.
    /// </para>
    /// <para>
    /// Kept per thing and across navigation, so backing out to look at something else and coming
    /// back does not silently throw the safety net away. Keyed by the thing itself rather than by an
    /// id, because a stack is only of any interest while what it is about still exists.
    /// </para>
    /// </remarks>
    sealed class History<T> where T : class
    {
        /// <summary>
        /// How far back one thing can be walked.
        /// </summary>
        /// <remarks>
        /// Deep enough that nobody reaches the end of it while fixing a mistake, shallow enough
        /// that a long session cannot grow without bound. Sprocket runs inside a game.
        /// </remarks>
        const int Depth = 50;

        readonly Dictionary<T, List<Edit<T>>> _done = new Dictionary<T, List<Edit<T>>>();

        /// <summary>
        /// What has been taken back and could be put back again.
        /// </summary>
        /// <remarks>
        /// Emptied by the next edit, which is the standard behaviour and the right one: once
        /// somebody has walked back three steps and then done something different, the three they
        /// walked back are a road not taken. Offering to redo into a sequence that has moved on
        /// since would put back a step at an index that means something else now.
        /// </remarks>
        readonly Dictionary<T, List<Edit<T>>> _undone = new Dictionary<T, List<Edit<T>>>();

        /// <summary>
        /// Makes a change and remembers how to take it back. False where it changed nothing.
        /// </summary>
        /// <remarks>
        /// Applying and recording are one call so they cannot come apart - an edit applied without
        /// being recorded is a hole in the way back, and one recorded without being applied is
        /// worse.
        /// <para>
        /// Coalescing folds a run of the same kind of change into one. Typing a name arrives a
        /// letter at a time, and fifty entries all saying "renaming" would be a stack somebody has
        /// to press through rather than a way back.
        /// </para>
        /// </remarks>
        public bool Did(T subject, Edit<T> edit, bool coalesce)
        {
            if (!edit.Changes)
                return false;

            edit.Apply(subject);

            // A new edit is a new branch, so whatever was walked back is gone.
            _undone.Remove(subject);

            var stack = Stack(_done, subject);
            if (!coalesce || stack.Count == 0 || stack[stack.Count - 1].Label != edit.Label)
            {
                stack.Add(edit);

                if (stack.Count > Depth)
                    stack.RemoveAt(0);
            }

            return true;
        }

        public bool CanUndo(T subject)
        {
            return Waiting(_done, subject) != null;
        }

        public bool CanRedo(T subject)
        {
            return Waiting(_undone, subject) != null;
        }

        /// <summary>What undoing would undo, in the words it happened in.</summary>
        public string UndoLabel(T subject)
        {
            var edit = Waiting(_done, subject);
            return edit == null ? "" : edit.Label;
        }

        /// <summary>What redoing would put back, in the words it happened in.</summary>
        public string RedoLabel(T subject)
        {
            var edit = Waiting(_undone, subject);
            return edit == null ? "" : edit.Label;
        }

        /// <summary>Takes back the last change, and says which one it was, or null.</summary>
        public Edit<T> Undo(T subject)
        {
            var edit = Take(_done, subject);
            if (edit == null)
                return null;

            edit.Undo(subject);
            Stack(_undone, subject).Add(edit);
            return edit;
        }

        /// <summary>
        /// Drops what can no longer be walked back, from the top down.
        /// </summary>
        /// <remarks>
        /// The program is not the editor's alone - see <see cref="Edit{T}.Undoable" /> for the case
        /// this exists for. Asked before anything is read as well as before anything is done, so the
        /// arrows never name a change that would not happen, and a press never quietly does nothing.
        /// <para>
        /// Only from the top, because nothing below is reachable until what is above it has gone,
        /// and it is asked the moment it becomes so. Both ways are asked at once: a change from
        /// outside can invalidate a step back and the step forward that was waiting under it, and
        /// they are separate stacks.
        /// </para>
        /// <para>
        /// A screen asks this every tick, through the words on its arrows, so what an edit does to
        /// answer has to be cheap: a walk of the triggers a system has and a comparison of what one
        /// names. Nothing here reads a toolbar or builds a string.
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

        /// <summary>
        /// Puts back the last thing undone, and says which one it was, or null.
        /// </summary>
        /// <remarks>
        /// The same <c>Apply</c> the original edit ran, not a third code path. An edit that could be
        /// applied, reversed, and then not applied again the same way would be a stranger bug than
        /// either half alone.
        /// </remarks>
        public Edit<T> Redo(T subject)
        {
            var edit = Take(_undone, subject);
            if (edit == null)
                return null;

            edit.Apply(subject);
            Stack(_done, subject).Add(edit);
            return edit;
        }

        public void Forget(T subject)
        {
            _done.Remove(subject);
            _undone.Remove(subject);
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
