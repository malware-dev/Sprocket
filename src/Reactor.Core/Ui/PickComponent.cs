using System.Collections.Generic;

namespace Reactor.Ui
{
    /// <summary>One thing that can be picked.</summary>
    /// <remarks>
    /// Deliberately a superset rather than one type per kind of thing. Picking blocks uses the
    /// kind, the live status and the count; picking what happens to them uses none of those.
    /// One type means a target writes one of these once.
    /// </remarks>
    public sealed class PickOption
    {
        public PickOption(string text, int command)
        {
            Text = text;
            Command = command;
        }

        public string Text { get; set; }

        /// <summary>Something quieter about it, such as a block's kind.</summary>
        public string Secondary { get; set; }

        /// <summary>Live state, in the user's words. Empty for things that have none.</summary>
        public string Status { get; set; }

        /// <summary>Whether the thing is doing something right now.</summary>
        public bool IsActive { get; set; }

        /// <summary>A count worth showing, or zero for none.</summary>
        public int Badge { get; set; }

        public bool IsChosen { get; set; }

        public int Command { get; set; }
    }

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
            Filters = new List<BlockFilter>();
            Options = new List<ChoiceEntry>();
            Entries = new List<PickOption>();
        }

        public string Title { get; set; }

        /// <summary>Context under the title, such as how many are picked.</summary>
        public string Subtitle { get; set; }

        /// <summary>Whether picking one un-picks the last, or adds to them.</summary>
        public bool AllowsMany { get; set; }

        /// <summary>Whether this stage can be narrowed by typing.</summary>
        public bool CanNarrow { get { return NarrowCommand != 0; } }

        /// <summary>What it has been narrowed to so far.</summary>
        public string NarrowText { get; set; }

        /// <summary>Where to send typing, or zero if it cannot be narrowed.</summary>
        public int NarrowCommand { get; set; }

        /// <summary>Ways to narrow by kind, built from what is actually here.</summary>
        public List<BlockFilter> Filters { get; private set; }

        /// <summary>
        /// Choices about the picking itself rather than about the things - whether any of them
        /// is enough, or all of them together.
        /// </summary>
        public List<ChoiceEntry> Options { get; private set; }

        public List<PickOption> Entries { get; private set; }

        /// <summary>Shown instead of the choices when nothing matches.</summary>
        public string EmptyText { get; set; }
    }

    /// <summary>
    /// Picking several things in an order, where each one narrows what the next can be.
    /// </summary>
    /// <remarks>
    /// Blocks, and then what can happen to them - the second exists only because of the first,
    /// and cannot be asked before it. That dependency is the whole of what this says.
    ///
    /// It says nothing about columns. This host draws the stages side by side because a wide
    /// screen makes the earlier answers worth keeping in view, and because changing an earlier
    /// answer should be a glance to the left. Somewhere narrower may ask one at a time, and
    /// somewhere else may do something nobody here has thought of. None of that is core's to
    /// decide, and the word "pane" appearing in this file was core deciding it.
    /// </remarks>
    public sealed class PickComponent : EditorComponent
    {
        readonly List<PickStage> _stages = new List<PickStage>();

        /// <summary>In order, each narrowing the next.</summary>
        public IList<PickStage> Stages { get { return _stages; } }

        /// <summary>
        /// Bumped whenever the stages are replaced.
        /// </summary>
        /// <remarks>
        /// A target keeps its controls between changes and needs to know when what it is
        /// showing has actually moved on. Without it the choice is between rebuilding
        /// everything on every tick and never noticing anything - and rebuilding on every
        /// click is what takes the row out from under somebody's second click.
        /// </remarks>
        public int Revision { get; private set; }

        /// <summary>
        /// Says that something about the stages is different from last time.
        /// </summary>
        /// <remarks>
        /// Called only when something actually differs. A page that walks its stages every
        /// tick and finds them unchanged says nothing, and a target that sees the same number
        /// as last time does nothing at all - one comparison and it stops.
        /// </remarks>
        public void Changed()
        {
            Revision++;
        }

        /// <summary>Adds a stage. Stages are made once and thereafter updated.</summary>
        public void Add(PickStage stage)
        {
            _stages.Add(stage);
            Revision++;
        }

        /// <summary>Drops the last stage, when there is no longer anything for it to be about.</summary>
        public void RemoveLast()
        {
            if (_stages.Count == 0)
                return;

            _stages.RemoveAt(_stages.Count - 1);
            Revision++;
        }
    }
}
