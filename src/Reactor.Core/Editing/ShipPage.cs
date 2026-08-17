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

        string _search = "";
        string _kind = "";
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

            _list = new BlockListComponent(BlockListMode.Navigate);
            _list.EmptyText = "Nothing on the ship matches that.";
            for (var i = 0; i < _filtered.Count; i++)
            {
                var device = _filtered[i];
                var id = device.Id;
                _list.Entries.Add(new BlockEntry(id, device.Name, device.Kind.DisplayName,
                    Editor.Register(delegate { Open(id); })));
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

                entry.Name = device.Name;
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
            DeviceQuery.Filter(_all, _search, _kind, _filtered);
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

        void Open(long deviceId)
        {
            Editor.Push(new DevicePage(Editor, deviceId));
        }
    }
}
