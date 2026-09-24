using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Picking
{
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
        public IList<PickStage> Stages => _stages;

        /// <summary>
        /// Whether the pick is finished after its stages rather than on its own: by giving what is
        /// chosen the values it takes, and saying so.
        /// </summary>
        public bool Finishes
        {
            get { return _finishes; }
            set { Set(ref _finishes, value, nameof(Finishes)); }
        }

        bool _finishes;

        /// <summary>
        /// Where what is in the values goes as it changes, written the way a program writes values, or
        /// zero where nothing is listening.
        /// </summary>
        /// <remarks>
        /// So leaving can tell values that were changed from values that were only looked at. Nothing
        /// is given to what is chosen until the pick is finished.
        /// </remarks>
        public int EnteredCommand
        {
            get { return _enteredCommand; }
            set { Set(ref _enteredCommand, value, nameof(EnteredCommand)); }
        }

        int _enteredCommand;

        /// <summary>
        /// The stage whose box takes the caret when its page is first shown, or minus one where none
        /// does.
        /// </summary>
        /// <remarks>
        /// The column standing for what was pressed to open the pick. Only once, on arriving: a page
        /// gone back to is where it was left.
        /// </remarks>
        public int Opens
        {
            get { return _opens; }
            set { Set(ref _opens, value, nameof(Opens)); }
        }

        int _opens = -1;

        /// <summary>Whether the values open with the caret in the first of them.</summary>
        /// <remarks>For somebody who came to change a step's values rather than what it does or to what.</remarks>
        public bool OpensOnValues
        {
            get { return _opensOnValues; }
            set { Set(ref _opensOnValues, value, nameof(OpensOnValues)); }
        }

        bool _opensOnValues;

        /// <summary>
        /// Which chosen thing <see cref="Fields" /> are for, or empty while nothing that can be
        /// finished is chosen.
        /// </summary>
        /// <remarks>
        /// So a target can keep what was entered for each thing while the pick is open, and give it
        /// back on returning to one. Only ever compared: what it says is nobody's business.
        /// </remarks>
        public string Given
        {
            get { return _given; }
            set { Set(ref _given, value, nameof(Given)); }
        }

        string _given;

        /// <summary>
        /// The first stage asked on the page showing now. Those before it were answered on an earlier
        /// page, and the last of them is what the way back to it is about.
        /// </summary>
        /// <remarks>
        /// A pick that asks more than one screen holds goes on in pages, each taking up where the last
        /// left off - what a trigger does, and then the action it does.
        /// </remarks>
        public int Page
        {
            get { return _page; }
            set { Set(ref _page, value, nameof(Page)); }
        }

        int _page;

        /// <summary>Where going on to the page after goes, or zero where nothing chosen has a page after it.</summary>
        public int Onward
        {
            get { return _onward; }
            set { Set(ref _onward, value, nameof(Onward)); }
        }

        int _onward;

        /// <summary>Where going back to the page before goes, or zero on the first page.</summary>
        public int Returns
        {
            get { return _returns; }
            set { Set(ref _returns, value, nameof(Returns)); }
        }

        int _returns;

        /// <summary>What each value the chosen thing takes asks for, one field each, in order.</summary>
        public List<Field> Fields { get; } = new List<Field>();

        /// <summary>What the fields open holding where nothing has been entered for the chosen thing, one per field.</summary>
        // ReSharper disable once CollectionNeverQueried.Global - read by PickView, one per field, as it fills them.
        public List<string> Filled { get; } = new List<string>();

        /// <summary>Bumped whenever <see cref="Given" /> or <see cref="Fields" /> change.</summary>
        /// <remarks>
        /// So a target fills its fields again only when there is something new to fill them with, and
        /// what somebody is typing is left alone by every other change to the pick.
        /// </remarks>
        public int Restated { get; private set; }

        /// <summary>Says that what the chosen thing takes has changed.</summary>
        public void Restate()
        {
            Restated++;
        }

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
        /// as last time does nothing at all - one relation and it stops.
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

        /// <summary>
        /// Comes back to the page starting at <paramref name="page" />, whose stages end before
        /// <paramref name="count" />, and answers whether anything had to go.
        /// </summary>
        /// <remarks>What later pages asked goes, and so do the values they were given.</remarks>
        public bool Return(int page, int count)
        {
            Page = page;
            if (_stages.Count <= count)
                return false;

            while (_stages.Count > count)
                RemoveLast();

            Given = null;
            Fields.Clear();
            Filled.Clear();
            Restate();
            return true;
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
