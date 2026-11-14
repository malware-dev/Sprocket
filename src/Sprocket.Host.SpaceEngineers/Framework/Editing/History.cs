using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Editing
{
    /// <summary>The undo and redo history of each thing of one kind, walked from any of the screens it is shown on.</summary>
    public sealed class History<T> where T : class
    {
        const int Depth = 100;

        readonly Dictionary<T, List<Entry>> _done = new Dictionary<T, List<Entry>>();

        readonly Dictionary<T, List<List<Entry>>> _undone = new Dictionary<T, List<List<Entry>>>();

        readonly List<Entry> _walk = new List<Entry>();
        readonly List<object> _reach = new List<object>();
        readonly List<int> _groups = new List<int>();
        Entry _target;

        /// <summary>Makes a change and records how to take it back, and that the screen numbered <paramref name="maker" /> made it; false where it changed nothing.</summary>
        public bool Did(T subject, Edit<T> edit, int maker)
        {
            if (!edit.Changes)
                return false;

            edit.Apply(subject);

            var entry = new Entry(edit) { Maker = maker };
            edit.Touches(subject, entry.Screens);

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

        /// <summary>The change undoing on <paramref name="screen" /> would undo, or null where there is nothing.</summary>
        public Edit<T> Undoes(T subject, object screen)
        {
            return Back(subject, screen) ? _walk[0].Edit : null;
        }

        /// <summary>The change redoing on <paramref name="screen" /> would put back, or null where there is nothing.</summary>
        public Edit<T> Redoes(T subject, object screen)
        {
            return Forward(subject, screen) ? _target.Edit : null;
        }

        /// <summary>
        /// Adds to <paramref name="screens" /> the screens other than <paramref name="screen" /> whose changes undoing there takes
        /// back too, where <paramref name="back" />, or redoing makes again.
        /// </summary>
        public void Also(T subject, object screen, bool back, IList<object> screens)
        {
            if (!(back ? Back(subject, screen) : Forward(subject, screen)))
                return;

            foreach (var entry in _walk)
            {
                if (screen != null && entry.Screens.Contains(screen))
                    continue;

                var touched = entry.Screens;
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
        public Edit<T> Redo(T subject, object screen)
        {
            if (!CanRedo(subject, screen))
                return null;

            var undone = _undone[subject];
            var done = Stack(_done, subject);
            for (var g = _groups.Count - 1; g >= 0; g--)
                undone.RemoveAt(_groups[g]);

            foreach (var entry in _walk)
            {
                entry.Edit.Apply(subject);
                done.Add(entry);
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
        /// <remarks>For something just made and left untouched, where leaving it is a cancel.</remarks>
        public bool Withdraw(T subject, Edit<T> edit)
        {
            List<Entry> stack;
            if (!_done.TryGetValue(subject, out stack) || stack.Count == 0 || stack[stack.Count - 1].Edit != edit)
                return false;

            stack.RemoveAt(stack.Count - 1);
            edit.Undo(subject);
            return true;
        }

        /// <summary>Whether a change the screen numbered <paramref name="maker" /> made still stands, in anything, and could be taken back now: not one waiting for a switch.</summary>
        public bool MadeBy(int maker)
        {
            foreach (var pair in _done)
            {
                foreach (var entry in pair.Value)
                {
                    if (entry.Maker == maker && !entry.Edit.Held(pair.Key, true))
                        return true;
                }
            }

            return false;
        }

        /// <summary>Has the changes the screen numbered <paramref name="maker" /> made stand as made by none, so nothing takes them back as its own.</summary>
        public void Disown(int maker)
        {
            foreach (var done in _done.Values)
            {
                foreach (var entry in done)
                {
                    if (entry.Maker == maker)
                        entry.Maker = 0;
                }
            }
        }

        /// <summary>
        /// Takes back and forgets every change the screen numbered <paramref name="maker" /> made that still stands, the last
        /// first, leaving the changes other screens made after them; one that no longer fits what is there, or waits for a
        /// switch, stays, and so does every one made before it. Each change taken back is added to <paramref name="discarded" />.
        /// </summary>
        public void Discard(int maker, IList<Edit<T>> discarded)
        {
            foreach (var pair in _done)
            {
                var done = pair.Value;
                var any = false;
                for (var i = done.Count - 1; i >= 0; i--)
                {
                    if (done[i].Maker != maker)
                        continue;

                    if (!done[i].Edit.Undoable(pair.Key) || done[i].Edit.Held(pair.Key, true))
                        break;

                    done[i].Edit.Undo(pair.Key);
                    discarded.Add(done[i].Edit);
                    done.RemoveAt(i);
                    any = true;
                }

                if (any)
                    Reassert(pair.Key);
            }
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
        /// <remarks>Asked every tick through the words on the arrows, so it allocates nothing.</remarks>
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
                foreach (var entry in group)
                    _walk.Add(entry);
            }

            return true;
        }

        /// <summary>Whether a change in the walk waits for a switch, taking it back where <paramref name="back" />, or making it again.</summary>
        bool Holds(T subject, bool back)
        {
            foreach (var entry in _walk)
            {
                if (entry.Edit.Held(subject, back))
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

            foreach (var entry in done)
                entry.Edit.Reassert(subject);
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

            for (var g = undone.Count - 1; g >= 0; g--)
            {
                if (!Shares(undone[g], _reach))
                    continue;

                for (var i = 0; i < undone[g].Count; i++)
                    Join(undone[g][i].Screens, _reach);

                undone.RemoveAt(g);
            }
        }

        /// <summary>Drops what can no longer be walked back or forward, from the top down.</summary>
        /// <remarks>Asked every tick through the words on the arrows, so an edit's answer must be cheap and build no string.</remarks>
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
            foreach (var entry in group)
            {
                if (entry.Screens.Contains(screen))
                    return true;
            }

            return false;
        }

        static bool Shares(List<Entry> group, List<object> screens)
        {
            foreach (var entry in group)
            {
                if (Shares(entry.Screens, screens))
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

            /// <summary>The number of the screen that made the change and answers for it on leaving, or 0 where it is kept.</summary>
            public int Maker { get; set; }
        }
    }
}
