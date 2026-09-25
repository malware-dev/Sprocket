using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;

namespace Sprocket.Host.SpaceEngineers.Lists
{
    /// <summary>Named things that are doing something, one line each.</summary>
    public sealed class StatusListView : ComponentView
    {
        readonly StatusListComponent _component;
        readonly VStack _stack = new VStack { Spacing = 8f };
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

        /// <summary>
        /// What is said when there is nothing on the list, while there is nothing on it.
        /// </summary>
        /// <remarks>
        /// Held, because the words are a value core can change like any other and this view is told
        /// rather than asked - and it is written where the rows are written rather than only where
        /// they are built.
        /// </remarks>
        Label _empty;

        protected override void Refresh()
        {
            if (_revision != _component.Entries.Revision)
            {
                _revision = _component.Entries.Revision;
                Rebuild();
            }

            if (_empty != null)
                _empty.Text = _component.EmptyText;

            for (int i = 0; i < _rows.Count && i < _component.Entries.Count; i++)
                _rows[i].Update(_component.Entries[i]);
        }

        void Rebuild()
        {
            _rows.Clear();

            // Let go of with the rest: a held label that is no longer in the layout is a thing being
            // written to that nobody can see.
            _empty = null;
            _stack.Children.Clear();

            if (_component.Entries.Count == 0)
            {
                // A list with nothing to say about being empty takes no room at all. Two lists on one
                // screen means one of them is often empty and not worth a line - see SystemPage, where
                // the list below says the one thing there is to say.
                if (_component.EmptyText.Length == 0)
                    return;

                _empty = new Label { Style = Styles.Empty, Wraps = true, Text = _component.EmptyText };
                _stack.Children.Add(_empty);
                return;
            }

            for (int i = 0; i < _component.Entries.Count; i++)
            {
                var row = new StatusRow(this);
                _rows.Add(row);
                _stack.Children.Add(row.Root);
            }
        }
    }
}
