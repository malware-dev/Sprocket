using System.Collections.Generic;
using System.ComponentModel;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Lists
{
    /// <summary>
    /// Named things that are doing something, one line each.
    /// </summary>
    /// <remarks>
    /// Serves the systems on a ship and the sequences inside one, because they are the same
    /// thing to look at: a name, what it is doing now, when it last did anything, and whether
    /// something is wrong with it.
    ///
    /// That is not a slide towards generic widgets. It is still shaped by this domain - live
    /// automation with a heartbeat and a problem - rather than being a list control that
    /// would leave every target reassembling one out of primitives.
    /// </remarks>
    public sealed class StatusListComponent : EditorComponent
    {
        string _emptyText;

        readonly List<StatusEntry> _heard = new List<StatusEntry>();

        public StatusListComponent()
        {
            Entries = new ObservableList<StatusEntry>();
            Entries.Changed += Listed;
        }

        /// <summary>
        /// Repeats what a row says as something this list said, so a target listens once.
        /// </summary>
        /// <remarks>
        /// A row's values are written every tick from live state and announce only the ones that
        /// actually moved, which is what a target needs; without this it would have to listen to every
        /// row on the list and let go of each one as the list changed. Here that is one place instead
        /// of one per target.
        /// <para>
        /// The rows are replaced whenever the list is rebuilt, so what to listen to is taken from the
        /// contents each time rather than tracked row by row. A row still listened to after it has gone
        /// would be held by this component for as long as the screen lives.
        /// </para>
        /// </remarks>
        void Listed()
        {
            for (int i = 0; i < _heard.Count; i++)
                _heard[i].PropertyChanged -= Said;

            _heard.Clear();

            foreach (StatusEntry entry in Entries)
            {
                entry.PropertyChanged += Said;
                _heard.Add(entry);
            }

            Raise(nameof(Entries));
        }

        void Said(object sender, PropertyChangedEventArgs e)
        {
            // Coarsely, and on purpose: "something on this list is different now". Which row and which
            // value is detail nothing here would do anything with - see ObservableList, which says the
            // same about its own event.
            Raise(nameof(Entries));
        }

        public ObservableList<StatusEntry> Entries { get; private set; }


        public string EmptyText
        {
            get { return _emptyText; }
            set { Set(ref _emptyText, value, nameof(EmptyText)); }
        }
    }
}
