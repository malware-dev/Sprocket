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
            Components = new List<EditorComponent>();
            Actions = new List<ChoiceEntry>();
        }

        public string Title { get; set; }

        /// <summary>Context the title cannot carry - a count, a live status.</summary>
        public string Subtitle { get; set; }

        public bool CanGoBack { get; set; }

        public int BackCommand { get; set; }

        /// <summary>What to show, in order.</summary>
        public List<EditorComponent> Components { get; private set; }

        /// <summary>
        /// Screen-level actions: the roots to switch between, and starting a sequence
        /// from scratch. A target renders these wherever it puts navigation.
        /// </summary>
        public List<ChoiceEntry> Actions { get; private set; }

        /// <summary>
        /// Bumped whenever the shape of the screen changes.
        /// </summary>
        /// <remarks>
        /// Creating native controls every beat is exactly what a target cannot
        /// afford, so this is the signal to rebuild them. While it holds steady the
        /// component objects stay identical and only their values change, and a
        /// target can simply re-read them into the controls it already made.
        /// </remarks>
        public int StructureVersion { get; set; }
    }
}
