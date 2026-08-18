using System.Collections.Generic;

namespace Reactor.Ui
{
    /// <summary>One row in a pane.</summary>
    /// <remarks>
    /// Deliberately a superset rather than one type per kind of pane. A blocks pane
    /// uses the kind, the live status and the badge; an actions pane uses none of
    /// them. One row type means a target writes one row renderer, and a pane added
    /// later costs nothing.
    /// </remarks>
    public sealed class PathEntry
    {
        public PathEntry(string text, int command)
        {
            Text = text;
            Command = command;
        }

        public string Text { get; set; }

        /// <summary>Quieter line beneath, such as a block's kind.</summary>
        public string Secondary { get; set; }

        /// <summary>Live state, in the user's words. Empty for things that have none.</summary>
        public string Status { get; set; }

        /// <summary>Whether the thing is doing something right now.</summary>
        public bool IsActive { get; set; }

        /// <summary>A count worth showing beside the row, or zero for none.</summary>
        public int Badge { get; set; }

        public bool IsChosen { get; set; }

        public int Command { get; set; }
    }

    /// <summary>
    /// One column of the path.
    /// </summary>
    /// <remarks>
    /// Each pane searches, filters and scrolls on its own. That matters more than it
    /// sounds: the panes hold lists of wildly different lengths - several hundred
    /// blocks beside four verbs - and a shared scrollbar or a shared search box would
    /// be wrong for one of them whichever way it was tuned.
    /// </remarks>
    public sealed class PathPane
    {
        public PathPane(string title)
        {
            Title = title;
            Filters = new List<BlockFilter>();
            Options = new List<ChoiceEntry>();
            Entries = new List<PathEntry>();
        }

        public string Title { get; set; }

        /// <summary>Context under the title, such as how many are picked.</summary>
        public string Subtitle { get; set; }

        /// <summary>Whether picking one un-picks the last, or adds to them.</summary>
        public bool AllowsMany { get; set; }

        /// <summary>Current search text. Ignored when <see cref="SearchCommand"/> is zero.</summary>
        public string SearchText { get; set; }

        /// <summary>Where to send typing, or zero if this pane has no search.</summary>
        public int SearchCommand { get; set; }

        public List<BlockFilter> Filters { get; private set; }

        /// <summary>
        /// Choices about the pane itself rather than its contents - "any one of them"
        /// against "all of them, together".
        /// </summary>
        public List<ChoiceEntry> Options { get; private set; }

        public List<PathEntry> Entries { get; private set; }

        /// <summary>Shown instead of the rows when nothing matches.</summary>
        public string EmptyText { get; set; }
    }

    /// <summary>
    /// A choice made in stages, with every stage still on screen.
    /// </summary>
    /// <remarks>
    /// Picking blocks and then picking what happens to them used to be two screens,
    /// one after the other, which hid the first answer the moment the second was
    /// asked. Here the panes sit side by side: the path so far is readable at a
    /// glance, and going back to change an earlier answer is a glance to the left
    /// rather than a retreat through screens.
    ///
    /// The number of panes is not fixed. Two is what the editor needs today - blocks,
    /// then what happens - but conditions and states will want a third and a fourth,
    /// so a target should lay these out as a row that scrolls sideways rather than
    /// assuming it can fit them all.
    /// </remarks>
    public sealed class PathComponent : EditorComponent
    {
        public PathComponent()
        {
            Panes = new List<PathPane>();
        }

        public List<PathPane> Panes { get; private set; }
    }
}
