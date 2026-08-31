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
            Heading = new List<HeadingPart>();
        }

        /// <summary>
        /// What this screen is called, in order, one part at a time.
        /// </summary>
        /// <remarks>
        /// Never empty: every screen has at least one part, and most have exactly one. A sequence has two
        /// because it is two things - what starts it, and what it then does - and naming the second used
        /// to replace both.
        /// <para>
        /// <b>There is deliberately no joined-up string of the whole thing here.</b> One was easy to add
        /// and every target would have drawn it, because drawing one string is easier than drawing several
        /// - and the whole reason this is a list is that a heading has to show which part of it is
        /// somebody's to change. An API that offers the easy wrong thing gets the easy wrong thing.
        /// </para>
        /// </remarks>
        public List<HeadingPart> Heading { get; private set; }

        /// <summary>Context the title cannot carry - a count, a live status.</summary>
        public string Subtitle { get; set; }

        /// <summary>Whether any part of the heading is somebody's to change.</summary>
        /// <remarks>
        /// Naming belongs on the heading because the heading is what the name is for. It replaced a
        /// naming box that sat inside the sequence, under a heading that already showed the name - two
        /// places for one thing, one announcing it and one setting it. Whether it is a pencil beside the
        /// words, a long press, or typing straight over them is the target's business.
        /// </remarks>
        public bool CanRename
        {
            get
            {
                for (var i = 0; i < Heading.Count; i++)
                {
                    if (Heading[i].CanRename)
                        return true;
                }

                return false;
            }
        }

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

        public bool HasSwitch => SwitchCommand != 0;

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

        public bool HasPrimary => !string.IsNullOrEmpty(PrimaryText);

        /// <summary>What to show, in order.</summary>
        public ObservableList<EditorComponent> Components { get; private set; }

        /// <summary>
        /// Screen-level actions: the roots to switch between, and starting a sequence
        /// from scratch. A target renders these wherever it puts navigation.
        /// </summary>
        public ObservableList<ChoiceEntry> Actions { get; private set; }

    }
}
