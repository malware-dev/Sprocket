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

        /// <summary>
        /// The name somebody typed for this screen's subject, or empty when the heading is
        /// worked out instead.
        /// </summary>
        /// <remarks>
        /// Named for the game's own term. Space Engineers calls a player-supplied name a block's
        /// CustomName throughout its API, and a mod that invents a different word for the same
        /// idea makes its author translate every time they read it.
        /// </remarks>
        /// <remarks>
        /// Separate from <see cref="Title"/> because they are not the same thing. The title is
        /// always something to read - a name if there is one, a description of the thing
        /// otherwise - while this is only ever what a person typed. A target offering to
        /// change the name needs to know which it is looking at, or it prefills its box with a
        /// sentence nobody wrote and calls it their name.
        /// </remarks>
        public string CustomName { get; set; }

        public bool CanRename { get { return RenameCommand != 0; } }

        /// <summary>
        /// Send a new name here, or empty to go back to the worked-out heading.
        /// </summary>
        /// <remarks>
        /// Zero when this screen's heading is not somebody's to change.
        ///
        /// This replaced a naming box that sat inside the sequence, under a heading that
        /// already showed the name - two places for one thing, one of them announcing it and
        /// one of them setting it. Naming belongs on the heading because the heading is what
        /// the name is for. Whether that is a pencil beside it, a long press, or typing
        /// straight over it is the target's business.
        /// </remarks>
        public int RenameCommand { get; set; }

        /// <summary>What to ask when offering the rename.</summary>
        public string RenamePrompt { get; set; }

        /// <summary>
        /// Switches this screen's subject on or off. Zero when it has no such switch.
        /// </summary>
        /// <remarks>
        /// A system's switch, where a sequence's lives on the sentence itself. Same idea at a
        /// larger scale: rewiring an airlock means stopping the airlock, not stopping three
        /// sequences one at a time - and a half-stopped one, with the inner door's rule off
        /// and the outer door's rule still live, is worse than either state.
        /// </remarks>
        public int SwitchCommand { get; set; }

        public bool HasSwitch { get { return SwitchCommand != 0; } }

        /// <summary>Whether it is currently allowed to run.</summary>
        public bool SwitchIsOn { get; set; }

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
