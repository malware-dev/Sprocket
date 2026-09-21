using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Lists;

namespace Sprocket.Host.SpaceEngineers.Picking
{
    /// <summary>
    /// One thing being picked, and what there is to pick from.
    /// </summary>
    /// <remarks>
    /// A stage says what is being asked and what the answers are. It says nothing about where
    /// that appears or what it is made of.
    ///
    /// <see cref="CanNarrow"/> is a capability rather than a search box: this stage can be
    /// narrowed by typing, and whether that is worth offering - a list of four verbs does not
    /// need it, several hundred blocks does - is the target's judgement about its own medium.
    /// </remarks>
    public sealed class PickStage
    {
        public PickStage(string title)
        {
            Title = title;
            Filters = new List<KindFilter>();
            Options = new List<ChoiceEntry>();
            Entries = new List<PickOption>();
        }

        public string Title { get; set; }

        /// <summary>Context under the title, such as how many are picked.</summary>
        public string Subtitle { get; set; }

        /// <summary>What the recessed well under the list is headed, or empty where there is none.</summary>
        public string WellTitle { get; set; }

        /// <summary>Whether picking one un-picks the last, or adds to them.</summary>
        public bool AllowsMany { get; set; }

        /// <summary>
        /// Whether the options in this stage come with pictures.
        /// </summary>
        /// <remarks>
        /// A fact about the answers, not an instruction to draw anything. A target uses it to
        /// decide whether to keep room for them; how big they are, or whether to show them at
        /// all, was never core's to say - and a game with no artwork sets it nowhere and gets
        /// screens with no gap where pictures would have been.
        ///
        /// Read off everything the stage could offer rather than what is currently listed, so
        /// narrowing to a handful of things that happen to have no picture does not take the
        /// column away mid-search.
        /// </remarks>
        public bool HasIcons { get; set; }

        /// <summary>Whether this stage can be narrowed by typing.</summary>
        public bool CanNarrow => NarrowCommand != 0;

        /// <summary>What it has been narrowed to so far.</summary>
        public string NarrowText { get; set; }

        /// <summary>Where to send typing, or zero if it cannot be narrowed.</summary>
        public int NarrowCommand { get; set; }

        /// <summary>Ways to narrow by kind, built from what is actually here.</summary>
        /// <summary>
        /// What is typed into the box that narrows the list of kinds, or empty.
        /// </summary>
        /// <remarks>
        /// The kinds are as long as a construct is varied - a station lists every sort of block on
        /// it - so the menu that picks one needs narrowing for the same reason the blocks do.
        /// </remarks>
        public string KindText { get; set; }

        /// <summary>Where a keystroke in that box goes, or zero where there is no such box.</summary>
        public int KindCommand { get; set; }

        /// <summary>Whether a kind is in force, which is when there is something to clear.</summary>
        public bool IsNarrowed { get; set; }

        /// <summary>Puts the kinds back to all of them, or zero where there is nothing to put back.</summary>
        public int ClearCommand { get; set; }

        public List<KindFilter> Filters { get; private set; }

        /// <summary>
        /// Choices about the picking itself rather than about the things - whether any of them
        /// is enough, or all of them together.
        /// </summary>
        public List<ChoiceEntry> Options { get; private set; }

        public List<PickOption> Entries { get; private set; }

        /// <summary>Shown instead of the choices when nothing matches.</summary>
        public string EmptyText { get; set; }
    }
}
