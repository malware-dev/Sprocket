using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    sealed class AddStep : SequenceEdit
    {
        readonly Change _change;
        readonly int _index;
        readonly List<Step> _steps = new List<Step>();

        /// <summary>
        /// One or more lines added as one edit.
        /// </summary>
        /// <remarks>
        /// Several, because a step and the await after it are one thing somebody did and have to be
        /// one thing they can take back. Undoing half of it would leave an await waiting for nothing.
        /// </remarks>
        public AddStep(Change change, int index, params Step[] steps)
        {
            _change = change;
            _index = index;
            for (var i = 0; i < steps.Length; i++)
                _steps.Add(steps[i]);
        }

        /// <inheritdoc />
        public override Change Change => _change;

        /// <inheritdoc />
        public override void Apply(Sequence sequence)
        {
            for (var i = 0; i < _steps.Count; i++)
                sequence.Steps.Insert(_index + i, _steps[i]);
        }

        /// <inheritdoc />
        public override void Undo(Sequence sequence)
        {
            for (var i = _steps.Count - 1; i >= 0; i--)
                sequence.Steps.RemoveAt(_index + i);
        }
    }
}
