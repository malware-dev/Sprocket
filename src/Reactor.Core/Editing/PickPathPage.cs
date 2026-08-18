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
    sealed class PickPathPage : EditorPage
    {
        /// <summary>Above this many rows, a pane earns a search box of its own.</summary>
        const int SearchThreshold = 8;

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
        PathComponent _path;

        public PickPathPage(ReactorEditor editor, string title, IList<long> selection, bool forEvents,
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

            _path = new PathComponent();
            _path.Fills = true;
            _path.Panes.Add(BuildBlocks());

            // The second pane exists only once there is something for it to be about.
            if (_selection.Count > 0)
                _path.Panes.Add(BuildWhat());

            components.Add(_path);
        }

        public override void Refresh()
        {
            if (_path == null || _path.Panes.Count == 0)
                return;

            var blocks = _path.Panes[0];
            for (var i = 0; i < blocks.Entries.Count && i < _filtered.Count; i++)
            {
                var device = Editor.World.FindDevice(_filtered[i].Id);
                if (device == null)
                    continue;

                blocks.Entries[i].Status = device.Status;
                blocks.Entries[i].IsActive = device.IsActive;
            }
        }

        PathPane BuildBlocks()
        {
            var pane = new PathPane("Blocks");
            pane.AllowsMany = true;
            pane.Subtitle = _selection.Count == 0
                ? "none picked"
                : _selection.Count == 1 ? "1 picked" : _selection.Count + " picked";
            pane.EmptyText = "Nothing here can go together with what you have already picked.";

            if (_candidates.Count > SearchThreshold)
            {
                pane.SearchText = _blockSearch;
                pane.SearchCommand = Editor.RegisterText(OnBlockSearch);
            }

            pane.Filters.Add(new BlockFilter("All", _kind.Length == 0, Editor.Register(ClearKind)));
            for (var i = 0; i < _kinds.Count; i++)
            {
                var id = _kinds[i].Id;
                pane.Filters.Add(new BlockFilter(_kinds[i].DisplayName, _kind == id,
                    Editor.Register(delegate { SetKind(id); })));
            }

            // Only worth asking once there is more than one, and only where the
            // distinction means anything - a step always acts on all its blocks.
            if (_getAll != null && _selection.Count > 1)
            {
                var all = _getAll();
                pane.Options.Add(new ChoiceEntry("any one of them is enough", !all,
                    Editor.Register(delegate { SetAll(false); })));
                pane.Options.Add(new ChoiceEntry("all of them, together", all,
                    Editor.Register(delegate { SetAll(true); })));
            }

            for (var i = 0; i < _filtered.Count; i++)
            {
                var device = _filtered[i];
                var id = device.Id;
                var entry = new PathEntry(device.Name, Editor.Register(delegate { Toggle(id); }));
                entry.Secondary = device.Kind.DisplayName;
                entry.Status = device.Status;
                entry.IsActive = device.IsActive;
                entry.IsChosen = Contains(_selection, id);
                pane.Entries.Add(entry);
            }

            return pane;
        }

        PathPane BuildWhat()
        {
            var pane = new PathPane(_forEvents ? "What happens" : "What to do");
            var names = new List<string>();
            var ids = new List<string>();

            if (_forEvents)
            {
                var events = Vocabulary.EventsFor(Editor.World, _selection);
                for (var i = 0; i < events.Count; i++)
                {
                    names.Add(events[i].Text);
                    ids.Add(events[i].Id);
                }
            }
            else
            {
                var actions = Vocabulary.ActionsFor(Editor.World, _selection);
                for (var i = 0; i < actions.Count; i++)
                {
                    names.Add(actions[i].Text);
                    ids.Add(actions[i].Id);
                }
            }

            // A choice the blocks can no longer honour is dropped rather than left
            // showing, so "Done" never appears over a sentence that could not run.
            var chosen = _getChosen();
            if (!ids.Contains(chosen))
            {
                chosen = "";
                _setChosen("");
            }

            pane.EmptyText = _forEvents
                ? "Those blocks have nothing in common that can happen to them."
                : "Those blocks have nothing in common that can be done to them.";

            if (names.Count > SearchThreshold)
            {
                pane.SearchText = _whatSearch;
                pane.SearchCommand = Editor.RegisterText(OnWhatSearch);
            }

            for (var i = 0; i < names.Count; i++)
            {
                if (!DeviceQuery.MatchesText(names[i], _whatSearch))
                    continue;

                var id = ids[i];
                var entry = new PathEntry(names[i], Editor.Register(delegate { Choose(id); }));
                entry.IsChosen = id == chosen;
                pane.Entries.Add(entry);
            }

            return pane;
        }

        void Collect()
        {
            _candidates.Clear();

            // Anything already picked stays listed even when the search would hide it,
            // so a choice can always be undone from where it was made.
            for (var i = 0; i < _selection.Count; i++)
            {
                var device = Editor.World.FindDevice(_selection[i]);
                if (device != null)
                    _candidates.Add(device);
            }

            var compatible = Vocabulary.CompatibleWith(Editor.World, _selection, _forEvents);
            for (var i = 0; i < compatible.Count; i++)
                _candidates.Add(compatible[i]);

            _kinds.Clear();
            DeviceQuery.KindsPresent(_candidates, _kinds);

            _filtered.Clear();
            DeviceQuery.Filter(_candidates, _blockSearch, _kind, _filtered);
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
