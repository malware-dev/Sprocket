using System.Collections.Generic;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// The construct: everything on it, searchable.
    /// </summary>
    /// <remarks>
    /// The root of the whole editor. Nobody thinks "I want to author a sequence" - they
    /// think about a door - so the first thing anyone sees is their own machine,
    /// never an empty program.
    /// </remarks>
    sealed class ShipPage : EditorPage
    {
        readonly List<IDevice> _all = new List<IDevice>();
        readonly List<IDevice> _filtered = new List<IDevice>();
        readonly List<DeviceKind> _kinds = new List<DeviceKind>();

        string _searchText = "";
        string _kind = "";

        readonly List<BlockFilter> _filters = new List<BlockFilter>();
        readonly List<BlockEntry> _rows = new List<BlockEntry>();
        BlockSearchComponent _search;
        BlockListComponent _list;

        public ShipPage(ReactorEditor editor) : base(editor)
        {
        }

        public override string Title { get { return "Your ship"; } }

        public override string Subtitle
        {
            get
            {
                if (_filtered.Count == _all.Count)
                    return _all.Count == 1 ? "1 thing" : _all.Count + " things";
                return _filtered.Count + " of " + _all.Count;
            }
        }

        /// <summary>
        /// Hands back the same two components every time, with new contents.
        /// </summary>
        /// <remarks>
        /// Typing in the search box changes which blocks are listed, which runs this
        /// again. Making a new search component each time would mean a target
        /// rebuilding the very text field being typed into.
        /// </remarks>
        public override void Build(List<EditorComponent> components)
        {
            Collect();

            if (_search == null)
                _search = new BlockSearchComponent(_searchText, 0);

            _search.Text = _searchText;
            _search.Command = Editor.RegisterText(OnSearch);

            _filters.Clear();
            _filters.Add(new BlockFilter("All", _kind.Length == 0, Editor.Register(ClearKind)));
            for (var i = 0; i < _kinds.Count; i++)
            {
                var id = _kinds[i].Id;
                _filters.Add(new BlockFilter(_kinds[i].DisplayName, _kind == id,
                    Editor.Register(delegate { SetKind(id); })));
            }
            _search.Filters.Reset(_filters);
            components.Add(_search);

            if (_list == null)
            {
                _list = new BlockListComponent(BlockListMode.Navigate);
                _list.Fills = true;
                _list.EmptyText = "Nothing on the ship matches that.";
            }

            _rows.Clear();
            for (var i = 0; i < _filtered.Count; i++)
            {
                var device = _filtered[i];
                var id = device.Id;
                _rows.Add(new BlockEntry(id, device.Name, device.Kind.DisplayName,
                    Editor.Register(delegate { Open(id); })));
            }
            _list.Entries.Reset(_rows);
            components.Add(_list);
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
                entry.SequenceCount = Editor.Program.CountReferencing(entry.DeviceId);
            }
        }

        void Collect()
        {
            _all.Clear();
            var devices = Editor.World.Devices;
            for (var i = 0; i < devices.Count; i++)
                _all.Add(devices[i]);

            _kinds.Clear();
            DeviceQuery.KindsPresent(_all, _kinds);

            _filtered.Clear();
            DeviceQuery.Filter(_all, _searchText, _kind, _filtered);
        }

        /// <summary>Takes new search text, and ignores text that is not new.</summary>
        void OnSearch(string value)
        {
            value = value ?? "";
            if (value == _searchText)
                return;

            _searchText = value;
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

        void Open(long deviceId)
        {
            Editor.Push(new DevicePage(Editor, deviceId));
        }
    }
}
