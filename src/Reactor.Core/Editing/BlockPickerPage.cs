using System;
using System.Collections.Generic;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// Picks the blocks that fill a slot.
    /// </summary>
    /// <remarks>
    /// Only blocks that would leave the sequence sayable are listed at all, so an
    /// impossible combination is unreachable rather than rejected - there is nothing
    /// to explain and nothing to recover from. On top of that the list is searchable
    /// and filterable, because a construct with several hundred blocks makes an
    /// unsorted list useless no matter how correct it is.
    ///
    /// Choices apply immediately. The program is live, so there is nothing to commit
    /// - and a confirm button would imply there was.
    /// </remarks>
    sealed class BlockPickerPage : EditorPage
    {
        readonly string _title;
        readonly IList<long> _selection;
        readonly bool _forEvents;
        readonly List<IDevice> _candidates = new List<IDevice>();
        readonly List<IDevice> _filtered = new List<IDevice>();
        readonly List<DeviceKind> _kinds = new List<DeviceKind>();

        readonly string _primaryText;
        readonly Action _primary;

        string _search = "";
        string _kind = "";
        BlockListComponent _list;

        public BlockPickerPage(ReactorEditor editor, string title, IList<long> selection, bool forEvents,
            string primaryText = null, Action primary = null)
            : base(editor)
        {
            _title = title;
            _selection = selection;
            _forEvents = forEvents;
            _primaryText = string.IsNullOrEmpty(primaryText) ? "Done" : primaryText;
            _primary = primary;
        }

        public override string Title { get { return _title; } }

        /// <summary>
        /// Appears once something is chosen, and not before - which doubles as the
        /// nudge that you are expected to choose something.
        /// </summary>
        /// <remarks>
        /// Nothing is being committed here; choices took effect as they were made.
        /// This exists because the user still needs to be told they have finished,
        /// and going back cannot be the thing that tells them - going back has to
        /// keep meaning "return to where I came from".
        /// </remarks>
        public override string PrimaryText
        {
            get { return _selection.Count > 0 ? _primaryText : ""; }
        }

        public override void Primary()
        {
            if (_primary != null)
                _primary();
            else
                Editor.Pop();
        }

        public override string Subtitle
        {
            get
            {
                // Says outright that it takes more than one. Picking several is the
                // ordinary case - it is what keeps looping out of the language - so
                // it should not have to be discovered.
                if (_selection.Count == 0)
                    return "Pick one or more";
                return _selection.Count == 1
                    ? "1 picked — pick more if you want"
                    : _selection.Count + " picked";
            }
        }

        public override void Build(EditorScreen screen)
        {
            Collect();

            var search = new BlockSearchComponent(_search, Editor.RegisterText(OnSearch));
            search.Filters.Add(new BlockFilter("All", _kind.Length == 0, Editor.Register(ClearKind)));
            for (var i = 0; i < _kinds.Count; i++)
            {
                var id = _kinds[i].Id;
                search.Filters.Add(new BlockFilter(_kinds[i].DisplayName, _kind == id,
                    Editor.Register(delegate { SetKind(id); })));
            }
            screen.Components.Add(search);

            _list = new BlockListComponent(BlockListMode.Choose);
            _list.EmptyText = "Nothing here can go together with what you've already picked.";

            for (var i = 0; i < _filtered.Count; i++)
            {
                var device = _filtered[i];
                var id = device.Id;
                var entry = new BlockEntry(id, device.Name, device.Kind.DisplayName,
                    Editor.Register(delegate { Toggle(id); }));
                entry.IsChosen = Contains(_selection, id);
                _list.Entries.Add(entry);
            }

            screen.Components.Add(_list);
        }

        public override void Refresh()
        {
            if (_list == null)
                return;

            for (var i = 0; i < _list.Entries.Count; i++)
            {
                var entry = _list.Entries[i];
                var device = Editor.World.FindDevice(entry.DeviceId);
                if (device == null)
                    continue;

                entry.Status = device.Status;
                entry.IsActive = device.IsActive;
            }
        }

        void Collect()
        {
            _candidates.Clear();

            // Anything already picked stays listed even when the search would hide
            // it, so a choice can always be undone from where it was made.
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
            DeviceQuery.Filter(_candidates, _search, _kind, _filtered);
        }

        void Toggle(long deviceId)
        {
            if (Contains(_selection, deviceId))
                _selection.Remove(deviceId);
            else
                _selection.Add(deviceId);

            // The set of compatible blocks shrinks the moment something is picked,
            // so what remains on offer has to be recomputed rather than left stale.
            Editor.Invalidate();
        }

        void OnSearch(string value)
        {
            _search = value;
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
