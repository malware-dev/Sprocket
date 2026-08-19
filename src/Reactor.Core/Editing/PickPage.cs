using System;
using System.Collections.Generic;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// Picks blocks and then what happens to them, with both stages on screen at once.
    /// </summary>
    /// <remarks>
    /// This replaces a pair of screens shown one after the other, which hid the first
    /// answer the moment it asked the second. Side by side, the whole path reads at a
    /// glance and changing an earlier answer is a glance to the left.
    ///
    /// Choices apply as they are made - the program is live, so there is nothing to
    /// commit. "Done" exists only to say the user has finished, because going back has
    /// to keep meaning "return to where I came from".
    /// </remarks>
    sealed class PickPage : EditorPage
    {
        readonly string _title;
        readonly IList<long> _selection;
        readonly bool _forEvents;
        readonly Func<string> _getChosen;
        readonly Action<string> _setChosen;
        readonly Action _onComplete;
        readonly Func<bool> _getAll;
        readonly Action<bool> _setAll;

        readonly List<IDevice> _candidates = new List<IDevice>();
        readonly List<IDevice> _filtered = new List<IDevice>();
        readonly List<DeviceKind> _kinds = new List<DeviceKind>();

        string _blockSearch = "";
        string _kind = "";
        string _whatSearch = "";
        readonly List<string> _names = new List<string>();
        readonly List<string> _ids = new List<string>();
        readonly List<int> _matched = new List<int>();

        PickComponent _pick;
        PickStage _blocks;
        PickStage _what;

        public PickPage(ReactorEditor editor, string title, IList<long> selection, bool forEvents,
            Func<string> getChosen, Action<string> setChosen, Action onComplete,
            Func<bool> getAll = null, Action<bool> setAll = null)
            : base(editor)
        {
            _title = title;
            _selection = selection;
            _forEvents = forEvents;
            _getChosen = getChosen;
            _setChosen = setChosen;
            _onComplete = onComplete;
            _getAll = getAll;
            _setAll = setAll;
        }

        public override string Title { get { return _title; } }

        public override string Subtitle
        {
            get
            {
                if (_selection.Count == 0)
                    return "Start by picking one or more blocks";
                return IsComplete ? "" : "Now pick what happens";
            }
        }

        public override string PrimaryText { get { return IsComplete ? "Done" : ""; } }

        public override void Primary()
        {
            _onComplete();
        }

        bool IsComplete
        {
            get { return _selection.Count > 0 && !string.IsNullOrEmpty(_getChosen()); }
        }

        public override void Build(List<EditorComponent> components)
        {
            Collect();

            if (_pick == null)
            {
                _pick = new PickComponent();
                _pick.Fills = true;

                _blocks = new PickStage("Blocks");
                _blocks.AllowsMany = true;
                _blocks.EmptyText = _forEvents
                    ? "Nothing here has anything that can happen to it."
                    : "Nothing here has anything that can be done to it.";
                _pick.Add(_blocks);
            }

            var changed = FillBlocks();

            // The second stage exists only once there is something for it to be about.
            var wanted = _selection.Count > 0;
            if (wanted && _what == null)
            {
                _what = new PickStage(_forEvents ? "What happens" : "What to do");
                _what.EmptyText = _forEvents
                    ? "Those blocks have nothing in common that can happen to them."
                    : "Those blocks have nothing in common that can be done to them.";
                _pick.Add(_what);
            }
            else if (!wanted && _what != null)
            {
                _pick.RemoveLast();
                _what = null;
            }

            if (_what != null)
                changed |= FillWhat();

            if (changed)
                _pick.Changed();

            components.Add(_pick);
        }

        /// <summary>
        /// Brings live state up to date, and says nothing when there is nothing to say.
        /// </summary>
        /// <remarks>
        /// Runs on every tick, so it compares before it writes and only speaks up when
        /// something actually moved. A target then does one comparison of its own and stops.
        /// </remarks>
        public override void Refresh()
        {
            if (_blocks == null)
                return;

            var changed = false;
            for (var i = 0; i < _blocks.Entries.Count && i < _filtered.Count; i++)
            {
                var device = Editor.World.FindDevice(_filtered[i].Id);
                if (device == null)
                    continue;

                var entry = _blocks.Entries[i];
                if (entry.Status != device.Status)
                {
                    entry.Status = device.Status;
                    changed = true;
                }

                if (entry.IsActive != device.IsActive)
                {
                    entry.IsActive = device.IsActive;
                    changed = true;
                }
            }

            if (changed)
                _pick.Changed();
        }

        bool FillBlocks()
        {
            var changed = false;

            var subtitle = _selection.Count == 0
                ? "none picked"
                : _selection.Count == 1 ? "1 picked" : _selection.Count + " picked";

            if (_blocks.Subtitle != subtitle)
            {
                _blocks.Subtitle = subtitle;
                changed = true;
            }

            // Always offered, never conditioned on how long the list is. Whether a way to
            // narrow is worth showing - and at what point a list is long enough to need one -
            // is the target looking at its own screen, not core counting rows.
            if (_blocks.NarrowText != _blockSearch)
            {
                _blocks.NarrowText = _blockSearch;
                changed = true;
            }

            var narrow = Editor.RegisterText(OnBlockSearch);
            if (_blocks.NarrowCommand != narrow)
            {
                _blocks.NarrowCommand = narrow;
                changed = true;
            }

            changed |= FillFilters();
            changed |= FillOptions();

            changed |= Resize(_blocks.Entries, _filtered.Count);
            for (var i = 0; i < _filtered.Count; i++)
            {
                var device = _filtered[i];
                var id = device.Id;

                changed |= Apply(_blocks.Entries[i], device.Name, device.Kind.DisplayName,
                    device.Status, device.IsActive, Contains(_selection, id),
                    Editor.Register(delegate { Toggle(id); }));
            }

            return changed;
        }

        bool FillFilters()
        {
            var changed = Resize(_blocks.Filters, _kinds.Count + 1);

            changed |= Apply(_blocks.Filters[0], "Anything", _candidates.Count, _kind.Length == 0,
                Editor.Register(ClearKind));

            for (var i = 0; i < _kinds.Count; i++)
            {
                var id = _kinds[i].Id;
                changed |= Apply(_blocks.Filters[i + 1], _kinds[i].DisplayName, CountOfKind(id),
                    _kind == id, Editor.Register(delegate { SetKind(id); }));
            }

            return changed;
        }

        /// <summary>
        /// Only worth asking once there is more than one, and only where the distinction means
        /// anything - a step always acts on all its blocks.
        /// </summary>
        bool FillOptions()
        {
            var wanted = _getAll != null && _selection.Count > 1;
            var changed = Resize(_blocks.Options, wanted ? 2 : 0);
            if (!wanted)
                return changed;

            var all = _getAll();
            changed |= Apply(_blocks.Options[0], "any one of them is enough", !all,
                Editor.Register(delegate { SetAll(false); }));
            changed |= Apply(_blocks.Options[1], "all of them, together", all,
                Editor.Register(delegate { SetAll(true); }));

            return changed;
        }

        bool FillWhat()
        {
            _names.Clear();
            _ids.Clear();

            if (_forEvents)
            {
                var events = Vocabulary.EventsFor(Editor.World, _selection);
                for (var i = 0; i < events.Count; i++)
                {
                    _names.Add(events[i].Text);
                    _ids.Add(events[i].Id);
                }
            }
            else
            {
                var actions = Vocabulary.ActionsFor(Editor.World, _selection);
                for (var i = 0; i < actions.Count; i++)
                {
                    _names.Add(actions[i].Text);
                    _ids.Add(actions[i].Id);
                }
            }

            // A choice the blocks can no longer honour is dropped rather than left showing, so
            // "Done" never appears over a sentence that could not run.
            var chosen = _getChosen();
            if (!_ids.Contains(chosen))
            {
                chosen = "";
                _setChosen("");
            }

            var changed = false;
            if (_what.NarrowText != _whatSearch)
            {
                _what.NarrowText = _whatSearch;
                changed = true;
            }

            var narrow = Editor.RegisterText(OnWhatSearch);
            if (_what.NarrowCommand != narrow)
            {
                _what.NarrowCommand = narrow;
                changed = true;
            }

            _matched.Clear();
            for (var i = 0; i < _names.Count; i++)
            {
                if (DeviceQuery.MatchesText(_names[i], _whatSearch))
                    _matched.Add(i);
            }

            changed |= Resize(_what.Entries, _matched.Count);
            for (var i = 0; i < _matched.Count; i++)
            {
                var at = _matched[i];
                var id = _ids[at];
                changed |= Apply(_what.Entries[i], _names[at], "", "", false, id == chosen,
                    Editor.Register(delegate { Choose(id); }));
            }

            return changed;
        }

        static bool Apply(PickOption entry, string text, string secondary, string status,
            bool active, bool chosen, int command)
        {
            var changed = false;

            if (entry.Text != text) { entry.Text = text; changed = true; }
            if (entry.Secondary != secondary) { entry.Secondary = secondary; changed = true; }
            if (entry.Status != status) { entry.Status = status; changed = true; }
            if (entry.IsActive != active) { entry.IsActive = active; changed = true; }
            if (entry.IsChosen != chosen) { entry.IsChosen = chosen; changed = true; }
            if (entry.Command != command) { entry.Command = command; changed = true; }

            return changed;
        }

        static bool Apply(BlockFilter filter, string label, int count, bool current, int command)
        {
            var changed = false;

            if (filter.Label != label) { filter.Label = label; changed = true; }
            if (filter.Count != count) { filter.Count = count; changed = true; }
            if (filter.IsCurrent != current) { filter.IsCurrent = current; changed = true; }
            if (filter.Command != command) { filter.Command = command; changed = true; }

            return changed;
        }

        static bool Apply(ChoiceEntry option, string text, bool current, int command)
        {
            var changed = false;

            if (option.Text != text) { option.Text = text; changed = true; }
            if (option.IsCurrent != current) { option.IsCurrent = current; changed = true; }
            if (option.Command != command) { option.Command = command; changed = true; }

            return changed;
        }

        static bool Resize(List<PickOption> list, int count)
        {
            var changed = false;
            while (list.Count < count) { list.Add(new PickOption("", 0)); changed = true; }
            while (list.Count > count) { list.RemoveAt(list.Count - 1); changed = true; }
            return changed;
        }

        static bool Resize(List<BlockFilter> list, int count)
        {
            var changed = false;
            while (list.Count < count) { list.Add(new BlockFilter("", 0, false, 0)); changed = true; }
            while (list.Count > count) { list.RemoveAt(list.Count - 1); changed = true; }
            return changed;
        }

        static bool Resize(List<ChoiceEntry> list, int count)
        {
            var changed = false;
            while (list.Count < count) { list.Add(new ChoiceEntry("", false, 0)); changed = true; }
            while (list.Count > count) { list.RemoveAt(list.Count - 1); changed = true; }
            return changed;
        }

        int CountOfKind(string kindId)
        {
            var count = 0;
            for (var i = 0; i < _candidates.Count; i++)
            {
                if (_candidates[i].Kind.Id == kindId)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// Works out what is on offer, which does not depend on what has been picked.
        /// </summary>
        /// <remarks>
        /// The ship is the ship. Ticking a block narrows what can be said about it in the
        /// next stage and nothing else, so nobody ever watches the list of their own blocks
        /// shrink underneath them.
        ///
        /// Narrowing is the one thing that can hide a row, and a picked block is exempt: a
        /// choice has to be undoable from where it was made, and a tick you cannot find is a
        /// tick you cannot clear. Order is the ship's order throughout, so nothing moves when
        /// it is picked either.
        /// </remarks>
        void Collect()
        {
            _candidates.Clear();

            var capable = Vocabulary.Capable(Editor.World, _forEvents);
            for (var i = 0; i < capable.Count; i++)
                _candidates.Add(capable[i]);

            _kinds.Clear();
            DeviceQuery.KindsPresent(_candidates, _kinds);

            _filtered.Clear();
            for (var i = 0; i < _candidates.Count; i++)
            {
                var device = _candidates[i];
                if (Contains(_selection, device.Id)
                    || DeviceQuery.Matches(device, _blockSearch, _kind))
                    _filtered.Add(device);
            }
        }

        void Toggle(long deviceId)
        {
            if (Contains(_selection, deviceId))
                _selection.Remove(deviceId);
            else
                _selection.Add(deviceId);

            // What is on offer changes the moment something is picked, so the whole
            // path is rebuilt rather than left stale.
            Editor.Invalidate();
        }

        void Choose(string id)
        {
            _setChosen(id);
            Editor.Invalidate();
        }

        void SetAll(bool all)
        {
            _setAll(all);
            Editor.Invalidate();
        }

        /// <summary>
        /// Takes new search text, and ignores text that is not new.
        /// </summary>
        /// <remarks>
        /// A target that puts our own value back into its search box, and reports that
        /// as an edit, must not cost a rebuild. Some raise the change notification
        /// after the assignment has returned, so the target cannot always tell its own
        /// echo apart from a keystroke - and a rebuild would rewrite the box, which
        /// echoes again. This is where that has to stop, because every target would
        /// otherwise have to know not to start it.
        /// </remarks>
        void OnBlockSearch(string value)
        {
            value = value ?? "";
            if (value == _blockSearch)
                return;

            _blockSearch = value;
            Editor.Invalidate();
        }

        void OnWhatSearch(string value)
        {
            value = value ?? "";
            if (value == _whatSearch)
                return;

            _whatSearch = value;
            Editor.Invalidate();
        }

        void ClearKind()
        {
            SetKind("");
        }

        void SetKind(string kindId)
        {
            _kind = kindId;
            Editor.Invalidate();
        }

        static bool Contains(IList<long> ids, long id)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                    return true;
            }
            return false;
        }
    }
}
