using Reactor.Host.SpaceEngineers.Screen;
namespace Reactor.Host.SpaceEngineers.Lists
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

        public StatusListComponent()
        {
            Entries = new ObservableList<StatusEntry>();
        }

        public ObservableList<StatusEntry> Entries { get; private set; }


        public string EmptyText
        {
            get { return _emptyText; }
            set { Set(ref _emptyText, value, "EmptyText"); }
        }
    }
}
