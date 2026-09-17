using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Screen;
using Reactor.Host.SpaceEngineers.Layout;
using Reactor.Host.SpaceEngineers.Controls;
using Reactor.Host.SpaceEngineers.Look;

namespace Reactor.Host.SpaceEngineers.Lists
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

        /// <summary>
        /// Told rather than asked. The rows announce their own values and the list announces its shape,
        /// so there is nothing here to read on a frame where nothing has happened.
        /// </summary>
        protected override bool Stale
        {
            get { return false; }
        }

        public override Element Root
        {
            get { return _stack; }
        }

        protected override void Refresh()
        {
            if (_revision != _component.Entries.Revision)
            {
                _revision = _component.Entries.Revision;
                Rebuild();
            }

            for (int i = 0; i < _rows.Count && i < _component.Entries.Count; i++)
                _rows[i].Update(_component.Entries[i]);
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
