using System.Collections.Generic;

namespace Reactor.Ui
{
    /// <summary>
    /// Everything on screen right now.
    /// </summary>
    /// <remarks>
    /// One screen at a time, with a back stack. That is a deliberate floor rather
    /// than a limitation: the editor has to work wherever it is embedded, and a
    /// layout assuming room for a sidebar plus a detail pane plus a sequence list is an
    /// assumption the smallest target cannot honor. A roomy target is free to render
    /// two levels of the stack side by side - nothing in core depends on it.
    /// </remarks>
    public sealed class EditorScreen
    {
        public EditorScreen()
        {
            Components = new ObservableList<EditorComponent>();
            Actions = new ObservableList<ChoiceEntry>();
        }

        public string Title { get; set; }

        /// <summary>Context the title cannot carry - a count, a live status.</summary>
        public string Subtitle { get; set; }

        public bool CanGoBack { get; set; }

        public int BackCommand { get; set; }

        /// <summary>
        /// The obvious next move, if this screen has one - "Next", "Done".
        /// </summary>
        /// <remarks>
        /// Deliberately separate from going back, which always means exactly what it
        /// says: return to the screen you came from. Overloading back with "finished"
        /// breaks the one navigation idea everybody already has, and leaves someone
        /// who genuinely wants to retreat with nowhere to press.
        ///
        /// Empty when there is nothing to move on to - which on a picker also means
        /// "you have not chosen anything yet".
        /// </remarks>
        public string PrimaryText { get; set; }

        public int PrimaryCommand { get; set; }

        public bool HasPrimary { get { return !string.IsNullOrEmpty(PrimaryText); } }

        /// <summary>What to show, in order.</summary>
        public ObservableList<EditorComponent> Components { get; private set; }

        /// <summary>
        /// Screen-level actions: the roots to switch between, and starting a sequence
        /// from scratch. A target renders these wherever it puts navigation.
        /// </summary>
        public ObservableList<ChoiceEntry> Actions { get; private set; }

    }
}
