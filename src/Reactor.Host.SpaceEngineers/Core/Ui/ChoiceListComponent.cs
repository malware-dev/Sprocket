namespace Reactor.Ui
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
        }

        public ObservableList<ChoiceEntry> Entries { get; private set; }

        public string EmptyText
        {
            get { return _emptyText; }
            set { Set(ref _emptyText, value, "EmptyText"); }
        }
    }
}
