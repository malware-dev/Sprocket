using System.Collections.Generic;
using Reactor.Ui;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Ui;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Sequences;

namespace Reactor.Host.SpaceEngineers.Presentation.Lists
{
    /// <summary>Named things that are doing something, one line each.</summary>
    public sealed class StatusListView : ComponentView
    {
        readonly StatusListComponent _component;
        readonly Stack _stack = Widgets.Column(Space.Row);
        readonly List<StatusRow> _rows = new List<StatusRow>();

        int _revision = -1;

        public StatusListView(StatusListComponent component)
        {
            _component = component;
        }

        public override Element Root
        {
            get { return _stack; }
        }

        public override void Update(int tick)
        {
            if (_revision != _component.Entries.Revision)
            {
                _revision = _component.Entries.Revision;
                Rebuild();
            }

            for (int i = 0; i < _rows.Count && i < _component.Entries.Count; i++)
                _rows[i].Update(_component.Entries[i]);
        }

        public override void Collect(List<Hit> hits)
        {
            for (int i = 0; i < _rows.Count && i < _component.Entries.Count; i++)
            {
                StatusEntry entry = _component.Entries[i];
                _rows[i].Collect(hits, entry);
            }
        }

        void Rebuild()
        {
            _rows.Clear();
            _stack.Children.Clear();

            if (_component.Entries.Count == 0)
            {
                Label empty = Widgets.Label(_component.EmptyText, Palette.Muted, Lettering.Small);
                empty.Padding = new Thickness(Space.Row, Space.Row, 0f, 0f);
                _stack.Children.Add(empty);
                return;
            }

            for (int i = 0; i < _component.Entries.Count; i++)
            {
                var row = new StatusRow();
                _rows.Add(row);
                _stack.Children.Add(row.Root);
            }
        }
    }
}
