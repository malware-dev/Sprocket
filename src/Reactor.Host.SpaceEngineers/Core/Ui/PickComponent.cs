using System.Collections.Generic;

namespace Reactor.Ui
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
