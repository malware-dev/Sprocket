using Reactor.Host.SpaceEngineers.Screen;
namespace Reactor.Host.SpaceEngineers.Lists
{
    /// <summary>
    /// A short list of named options - what can happen, what to do, how often.
    /// </summary>
    /// <remarks>
    /// Short by construction: it only ever contains what the blocks in the sequence
    /// can actually do, so there is never an invalid option to offer or explain.
    /// </remarks>
    public sealed class ChoiceListComponent : EditorComponent
    {
        string _emptyText;

        public ChoiceListComponent()
        {
            Entries = new ObservableList<ChoiceEntry>();
            Entries.Changed += Listed;
        }

        public ObservableList<ChoiceEntry> Entries { get; private set; }

        /// <summary>
        /// Says the list is different now, so a target listens to this rather than to every row.
        /// </summary>
        /// <remarks>
        /// A choice's own values are written when the page builds them and not after, so there is
        /// nothing here to repeat for each one - only the list's own shape to pass on.
        /// </remarks>
        void Listed()
        {
            Raise(nameof(Entries));
        }

        public string EmptyText
        {
            get { return _emptyText; }
            set { Set(ref _emptyText, value, nameof(EmptyText)); }
        }
    }
}
