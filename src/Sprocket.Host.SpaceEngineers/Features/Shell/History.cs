using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>
    /// The way back for one kind of thing: what was done to each one, and what was taken back from it, walked from any of the
    /// screens it is shown on.
    /// </summary>
    /// <remarks>
    /// One per thing undo is about: a logic system, whose history holds everything done to it on any of its screens, and the
    /// construct, whose history holds the logic systems and installations made, put on and taken out.
    /// <para>
    /// Each change keeps the screens it touched - a sequence, a slot, an installation, the logic system itself, as
    /// <see cref="Edit{T}.Touches" /> says - and undo pressed on a screen takes back the last change that touched it, the way
    /// each file in an editor has its own undo. One list underneath all of them, in the order things were done, so what is
    /// taken back is always exactly what was there: a change that touched the screen and others too goes back with every later
    /// change on any of those others, as one step, and comes back with them the same way.
    /// </para>
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

        readonly Dictionary<T, List<Entry>> _done = new Dictionary<T, List<Entry>>();

        readonly Dictionary<T, List<List<Entry>>> _undone = new Dictionary<T, List<List<Entry>>>();

        readonly List<Entry> _walk = new List<Entry>();
        readonly List<object> _reach = new List<object>();
        readonly List<int> _groups = new List<int>();
        Entry _target;

        /// <summary>Makes a change and remembers how to take it back. False where it changed nothing.</summary>
        /// <remarks>
        /// Applying and recording are one call so they cannot come apart: an edit applied without being recorded is a hole in
        /// the way back, and one recorded without being applied is worse. What it touched is asked once it is made, when
        /// everything it brings about with it has been worked out.
        /// </remarks>
        public bool Did(T subject, Edit<T> edit)
        {
            if (!edit.Changes)
                return false;

            edit.Apply(subject);

            var entry = new Entry(edit);
            edit.Touches(subject, entry.Screens);

            // A new change is a new branch for every screen it touched, and for whatever taken back leaned on what goes.
            Branch(subject, entry.Screens);

            var stack = Stack(_done, subject);
            stack.Add(entry);
            if (stack.Count > Depth)
                stack.RemoveAt(0);

            return true;
        }

        /// <summary>Whether there is a change on <paramref name="screen" /> to take back now: one there, and not waiting for a switch.</summary>
        /// <remarks>A screen of null is every screen, the whole of the thing.</remarks>
        public bool CanUndo(T subject, object screen)
        {
            return Back(subject, screen) && !Holds(subject, true);
        }

        /// <summary>Whether there is a change on <paramref name="screen" /> taken back to make again now: one there, and not waiting for a switch.</summary>
        public bool CanRedo(T subject, object screen)
        {
            return Forward(subject, screen) && !Holds(subject, false);
        }

        /// <summary>What undoing on <paramref name="screen" /> would undo, or <see cref="Change.None" /> where there is nothing.</summary>
        public Change Undoes(T subject, object screen)
        {
            return Back(subject, screen) ? _walk[0].Edit.Change : Change.None;
        }

        /// <summary>What redoing on <paramref name="screen" /> would put back, or <see cref="Change.None" /> where there is nothing.</summary>
        public Change Redoes(T subject, object screen)
        {
            return Forward(subject, screen) ? _target.Edit.Change : Change.None;
        }

        /// <summary>
        /// Adds to <paramref name="screens" /> the screens other than <paramref name="screen" /> whose changes undoing there takes
        /// back too, where <paramref name="back" />, or redoing makes again.
        /// </summary>
        public void Also(T subject, object screen, bool back, IList<object> screens)
        {
            if (!(back ? Back(subject, screen) : Forward(subject, screen)))
                return;

            for (var i = 0; i < _walk.Count; i++)
            {
                if (screen != null && _walk[i].Screens.Contains(screen))
                    continue;

                var touched = _walk[i].Screens;
                for (var k = 0; k < touched.Count; k++)
                {
                    if (touched[k] != screen && !screens.Contains(touched[k]))
                        screens.Add(touched[k]);
                }
            }
        }

        /// <summary>
        /// Takes back the last change on <paramref name="screen" />, with every later change it takes along, and says which
        /// it was, or null where there is none, or it waits for a switch.
        /// </summary>
        public Edit<T> Undo(T subject, object screen)
        {
            if (!CanUndo(subject, screen))
                return null;

            // Latest first, the walk being in the order the changes were made.
            var done = _done[subject];
            for (var i = _walk.Count - 1; i >= 0; i--)
            {
                _walk[i].Edit.Undo(subject);
                done.Remove(_walk[i]);
            }

            Stack(_undone, subject).Add(new List<Entry>(_walk));
            var edit = _walk[0].Edit;
            Reassert(subject);
            return edit;
        }

        /// <summary>
        /// Puts back the last thing undone on <paramref name="screen" />, with whatever was taken back that it leans on, and
        /// says which it was, or null where there is none, or it waits for a switch.
        /// </summary>
        /// <remarks>
        /// The same <c>Apply</c> the original edit ran, not a third code path. An edit that could be applied, reversed, and
        /// then not applied again the same way would be a stranger bug than either half alone.
        /// </remarks>
        public Edit<T> Redo(T subject, object screen)
        {
            if (!CanRedo(subject, screen))
                return null;

            var undone = _undone[subject];
            var done = Stack(_done, subject);
            for (var g = _groups.Count - 1; g >= 0; g--)
                undone.RemoveAt(_groups[g]);

            for (var i = 0; i < _walk.Count; i++)
            {
                _walk[i].Edit.Apply(subject);
                done.Add(_walk[i]);
            }

            var edit = _target.Edit;
            Reassert(subject);
            return edit;
        }

        /// <summary>The last change made to <paramref name="subject" />, or null.</summary>
        public Edit<T> Last(T subject)
        {
            List<Entry> stack;
            return _done.TryGetValue(subject, out stack) && stack.Count > 0 ? stack[stack.Count - 1].Edit : null;
        }

        /// <summary>Takes <paramref name="edit" /> back and forgets it, where it is the last change made to <paramref name="subject" />; false where it is not.</summary>
        /// <remarks>For something made a moment ago and left without anything done to it: leaving it is a cancel, not a step to walk.</remarks>
        public bool Withdraw(T subject, Edit<T> edit)
        {
            List<Entry> stack;
            if (!_done.TryGetValue(subject, out stack) || stack.Count == 0 || stack[stack.Count - 1].Edit != edit)
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
        /// Whether there is a change on <paramref name="screen" /> to take back, leaving in the walk that change and every later
        /// one going back with it, in the order they were made.
        /// </summary>
        /// <remarks>
        /// Every later change that touched a screen one already in the walk touched goes too, so nothing left standing was made
        /// over something taken away. Asked every tick through the words on the arrows, so it allocates nothing.
        /// </remarks>
        bool Back(T subject, object screen)
        {
            _walk.Clear();
            Settle(subject);

            List<Entry> done;
            if (!_done.TryGetValue(subject, out done))
                return false;

            var from = done.Count - 1;
            while (from >= 0 && screen != null && !done[from].Screens.Contains(screen))
                from--;

            if (from < 0)
                return false;

            _reach.Clear();
            for (var i = from; i < done.Count; i++)
            {
                if (i > from && !Shares(done[i].Screens, _reach))
                    continue;

                _walk.Add(done[i]);
                Join(done[i].Screens, _reach);
            }

            return true;
        }

        /// <summary>
        /// Whether there is a change taken back on <paramref name="screen" /> to make again, leaving in the walk it and every
        /// change taken back after it that it leans on, in the order they were first made.
        /// </summary>
        /// <remarks>
        /// What was taken back later was made earlier, so a later group sharing a screen with the walk comes back first. The
        /// indices of the groups walked are kept in <see cref="_groups" />, in the order they were taken back.
        /// </remarks>
        bool Forward(T subject, object screen)
        {
            _walk.Clear();
            _groups.Clear();
            Settle(subject);

            List<List<Entry>> undone;
            if (!_undone.TryGetValue(subject, out undone))
                return false;

            var from = undone.Count - 1;
            while (from >= 0 && screen != null && !Touched(undone[from], screen))
                from--;

            if (from < 0)
                return false;

            _target = undone[from][0];
            _reach.Clear();
            for (var g = from; g < undone.Count; g++)
            {
                if (g > from && !Shares(undone[g], _reach))
                    continue;

                _groups.Add(g);
                for (var i = 0; i < undone[g].Count; i++)
                    Join(undone[g][i].Screens, _reach);
            }

            for (var g = _groups.Count - 1; g >= 0; g--)
            {
                var group = undone[_groups[g]];
                for (var i = 0; i < group.Count; i++)
                    _walk.Add(group[i]);
            }

            return true;
        }

        /// <summary>Whether a change in the walk waits for a switch, taking it back where <paramref name="back" />, or making it again.</summary>
        bool Holds(T subject, bool back)
        {
            for (var i = 0; i < _walk.Count; i++)
            {
                if (_walk[i].Edit.Held(subject, back))
                    return true;
            }

            return false;
        }

        /// <summary>Has every change still standing say again the names it gave, after something made before one of them was walked.</summary>
        void Reassert(T subject)
        {
            List<Entry> done;
            if (!_done.TryGetValue(subject, out done))
                return;

            for (var i = 0; i < done.Count; i++)
                done[i].Edit.Reassert(subject);
        }

        /// <summary>
        /// Forgets what was taken back from the screens <paramref name="screens" />, and anything taken back before it that
        /// leaned on it, a new change having made a new branch there.
        /// </summary>
        void Branch(T subject, IList<object> screens)
        {
            List<List<Entry>> undone;
            if (!_undone.TryGetValue(subject, out undone))
                return;

            _reach.Clear();
            Join(screens, _reach);

            // Last taken back first: a group taken back before one that goes leans on it where they share a screen.
            for (var g = undone.Count - 1; g >= 0; g--)
            {
                if (!Shares(undone[g], _reach))
                    continue;

                for (var i = 0; i < undone[g].Count; i++)
                    Join(undone[g][i].Screens, _reach);

                undone.RemoveAt(g);
            }
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
        /// that was waiting under it, and they are separate stacks. Of what was taken back together, only the first is asked:
        /// each after it is made over the ones before, which are not there to ask about until they are made again.
        /// </para>
        /// <para>
        /// A screen asks this every tick, through the words on its arrows, so what an edit does to answer has to be cheap: a
        /// walk of the triggers a system has and a comparison of what one names. Nothing here reads a toolbar or builds a
        /// string.
        /// </para>
        /// </remarks>
        void Settle(T subject)
        {
            List<Entry> done;
            if (_done.TryGetValue(subject, out done))
            {
                while (done.Count > 0 && !done[done.Count - 1].Edit.Undoable(subject))
                    done.RemoveAt(done.Count - 1);
            }

            List<List<Entry>> undone;
            if (_undone.TryGetValue(subject, out undone))
            {
                while (undone.Count > 0 && !undone[undone.Count - 1][0].Edit.Redoable(subject))
                    undone.RemoveAt(undone.Count - 1);
            }
        }

        static bool Touched(List<Entry> group, object screen)
        {
            for (var i = 0; i < group.Count; i++)
            {
                if (group[i].Screens.Contains(screen))
                    return true;
            }

            return false;
        }

        static bool Shares(List<Entry> group, List<object> screens)
        {
            for (var i = 0; i < group.Count; i++)
            {
                if (Shares(group[i].Screens, screens))
                    return true;
            }

            return false;
        }

        static bool Shares(IList<object> some, List<object> screens)
        {
            for (var i = 0; i < some.Count; i++)
            {
                if (screens.Contains(some[i]))
                    return true;
            }

            return false;
        }

        static void Join(IList<object> some, List<object> into)
        {
            for (var i = 0; i < some.Count; i++)
            {
                if (!into.Contains(some[i]))
                    into.Add(some[i]);
            }
        }

        static List<TItem> Stack<TItem>(Dictionary<T, List<TItem>> stacks, T subject)
        {
            List<TItem> stack;
            if (!stacks.TryGetValue(subject, out stack))
            {
                stack = new List<TItem>();
                stacks[subject] = stack;
            }

            return stack;
        }

        /// <summary>A change made, and the screens it touched.</summary>
        sealed class Entry
        {
            public Entry(Edit<T> edit)
            {
                Edit = edit;
            }

            public Edit<T> Edit { get; }

            public List<object> Screens { get; } = new List<object>();
        }
    }
}
