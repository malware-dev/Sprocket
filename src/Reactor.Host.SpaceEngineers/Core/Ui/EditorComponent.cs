namespace Reactor.Ui
{
    /// <summary>
    /// One high-level piece of the editor.
    /// </summary>
    /// <remarks>
    /// These are specialized and domain-shaped on purpose. A generic vocabulary of
    /// buttons, rows and labels would force every target to reassemble a list of
    /// blocks out of primitives, and would drag layout into core - which is the one
    /// place it must never live.
    ///
    /// Components are long-lived. A page creates them once and thereafter changes
    /// their values and their lists, which is what lets a target build its controls
    /// once and keep them - along with focus, carets and scroll positions.
    ///
    /// The set is kept small deliberately. Every component added is work multiplied
    /// by the number of targets, so a new one has to earn its place.
    /// </remarks>
    public abstract class EditorComponent : Observable
    {
        bool _fills;

        /// <summary>
        /// Whether this component should take whatever space is left and scroll
        /// inside itself, rather than the screen scrolling as a whole.
        /// </summary>
        /// <remarks>
        /// Set on the one long list a screen is really about. Without it a target
        /// scrolls the entire screen, which drags the search box and the filters off
        /// the top along with the rows they are filtering.
        ///
        /// At most one component per screen should set this. A screen with none - a
        /// form, a page of sections - simply scrolls as a whole, which is right for it.
        /// </remarks>
        public bool Fills
        {
            get { return _fills; }
            set { Set(ref _fills, value, "Fills"); }
        }
    }
}
